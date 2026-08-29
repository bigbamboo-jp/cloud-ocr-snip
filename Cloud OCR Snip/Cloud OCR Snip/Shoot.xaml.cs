// Copyright information
//   © astel-labs.net.
//   Licence: New BSD License (https://opensource.org/licenses/BSD-3-Clause)

using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfPoint = System.Windows.Point;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// 1台のディスプレイを覆う撮影オーバーレイ。
    /// 選択範囲そのものはShootSessionが物理スクリーン座標で管理する。
    /// </summary>
    public partial class Shoot : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WM_DPICHANGED = 0x02E0;
        private const int RGN_DIFF = 4;
        private const int SELECTION_BORDER_THICKNESS = 2;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private readonly ShootSession _session;
        private readonly Rectangle _monitor_bounds;
        private readonly Rectangle _virtual_bounds;
        private readonly bool _live_capture_mode;
        private readonly bool _register_escape_hotkey;
        private IntPtr _window_handle;
        private Window? _selection_border_window;
        private IntPtr _selection_border_handle;
        private HwndSource? _hwnd_source;
        private uint _dpi = 96;
        private double _dip_scale = 1.0;
        private bool _closing;
        private bool _closed;
        private bool _escape_hotkey_registered;
        private bool _use_region_selection;

        internal bool IsClosed => _closed;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr window, IntPtr region, bool redraw);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

        [DllImport("gdi32.dll")]
        private static extern int CombineRgn(
            IntPtr destination,
            IntPtr source1,
            IntPtr source2,
            int combine_mode);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out NativePoint point);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        private enum WS_EX : long
        {
            TRANSPARENT = 0x00000020,
            TOOLWINDOW = 0x00000080,
            NOACTIVATE = 0x08000000,
        }

        public Shoot(
            ShootSession session,
            Rectangle monitor_bounds,
            Rectangle virtual_bounds,
            BitmapSource? background_image,
            bool live_capture_mode,
            bool register_escape_hotkey)
        {
            InitializeComponent();

            _session = session;
            _monitor_bounds = monitor_bounds;
            _virtual_bounds = virtual_bounds;
            _live_capture_mode = live_capture_mode;
            _register_escape_hotkey = register_escape_hotkey;

            // ライブ撮影だけ透過ウィンドウを使用する。事前撮影画像を表示する方式では
            // 不要な透過合成を避け、フォーク元最新版の描画性能改善を維持する。
            this.AllowsTransparency = live_capture_mode;

            var executing_assembly = Assembly.GetExecutingAssembly();
            this.Title = executing_assembly.GetName().Name;

            if (background_image != null)
            {
                BitmapSource? monitor_background = CreateMonitorBackground(background_image);
                if (monitor_background != null)
                {
                    this.Background = new ImageBrush(monitor_background)
                    {
                        Stretch = Stretch.Fill
                    };
                }
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // WindowInteropHelper.Handleは表示前には0になる場合があるため、Loaded後に取得する。
            _window_handle = new WindowInteropHelper(this).Handle;
            if (_window_handle == IntPtr.Zero)
            {
                // Show()のLoaded中に同期キャンセルすると、表示中のオーバーレイ列挙と競合するため遅延させる。
                Dispatcher.BeginInvoke(new Action(_session.Cancel));
                return;
            }

            if (_register_escape_hotkey && !_escape_hotkey_registered)
            {
                ComponentDispatcher.ThreadPreprocessMessage += Window_KeyDown;
                bool registration_failed = Functions.SetUpHotkey(
                    _window_handle,
                    Functions.HOTKEY_ID[10],
                    (ModifierKeys)0,
                    Key.Escape);
                if (registration_failed)
                    ComponentDispatcher.ThreadPreprocessMessage -= Window_KeyDown;
                else
                    _escape_hotkey_registered = true;
            }

            var old_style = GetWindowLongPtr(_window_handle, GWL_EXSTYLE);
            var new_style = new IntPtr(
                old_style.ToInt64() |
                (long)WS_EX.NOACTIVATE |
                (long)WS_EX.TOOLWINDOW);
            SetWindowLongPtr(_window_handle, GWL_EXSTYLE, new_style);

            _hwnd_source = HwndSource.FromHwnd(_window_handle);
            _hwnd_source?.AddHook(WindowProc);

            // WPFのLeft/Top/Width/Heightではなく、物理ピクセルのBoundsで配置する。
            SetNativeWindowBounds();
            UpdateDpiAndGeometry();

            // ライブ撮影ではWPFの全画面Geometryを毎回描き直さず、ネイティブリージョンで
            // 選択範囲を切り抜く。準備できない環境では従来のWPF描画へフォールバックする。
            if (_live_capture_mode)
                _use_region_selection = CreateSelectionBorderWindow();

            this.Opacity = 1.0;
        }

        private BitmapSource? CreateMonitorBackground(BitmapSource source)
        {
            int source_x = _monitor_bounds.Left - _virtual_bounds.Left;
            int source_y = _monitor_bounds.Top - _virtual_bounds.Top;
            if (source_x < 0 || source_y < 0 ||
                source_x + _monitor_bounds.Width > source.PixelWidth ||
                source_y + _monitor_bounds.Height > source.PixelHeight)
                return null;

            var cropped = new CroppedBitmap(
                source,
                new Int32Rect(
                    source_x,
                    source_y,
                    _monitor_bounds.Width,
                    _monitor_bounds.Height));
            cropped.Freeze();
            return cropped;
        }

        private void SetNativeWindowBounds()
        {
            SetWindowPos(
                _window_handle,
                HWND_TOPMOST,
                _monitor_bounds.Left,
                _monitor_bounds.Top,
                _monitor_bounds.Width,
                _monitor_bounds.Height,
                SWP_NOACTIVATE | SWP_SHOWWINDOW);

            if (_selection_border_handle != IntPtr.Zero)
            {
                SetWindowPos(
                    _selection_border_handle,
                    HWND_TOPMOST,
                    _monitor_bounds.Left,
                    _monitor_bounds.Top,
                    _monitor_bounds.Width,
                    _monitor_bounds.Height,
                    SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }

        private void UpdateDpiAndGeometry()
        {
            _dpi = GetDpiForWindow(_window_handle);
            if (_dpi == 0)
                _dpi = 96;

            _dip_scale = 96.0 / _dpi;
            double width = _monitor_bounds.Width * _dip_scale;
            double height = _monitor_bounds.Height * _dip_scale;

            // PathのGeometryはWPFのDIP、ネイティブウィンドウのBoundsは物理ピクセルで設定する。
            this.Width = width;
            this.Height = height;
            this.LayoutRoot.Width = width;
            this.LayoutRoot.Height = height;

            var protruding_width = this.Path1.StrokeThickness / 2.0;
            this.ScreenArea.Geometry1 = new RectangleGeometry(new Rect(
                -protruding_width,
                -protruding_width,
                width + (protruding_width * 2.0),
                height + (protruding_width * 2.0)));

            SetNativeWindowBounds();
        }

        internal void SetSelection(Rectangle? capture_area)
        {
            if (!capture_area.HasValue)
            {
                if (_use_region_selection)
                    ClearSelectionRegion();
                this.ScreenArea.Geometry2 = Geometry.Empty;
                return;
            }

            Rectangle clipped = Rectangle.Intersect(
                _monitor_bounds,
                capture_area.Value);
            if (clipped.Width <= 0 || clipped.Height <= 0)
            {
                if (_use_region_selection)
                    ClearSelectionRegion();
                this.ScreenArea.Geometry2 = Geometry.Empty;
                return;
            }

            if (_use_region_selection)
            {
                UpdateSelectionRegion(clipped);
                return;
            }

            var selection = new Rect(
                (clipped.Left - _monitor_bounds.Left) * _dip_scale,
                (clipped.Top - _monitor_bounds.Top) * _dip_scale,
                clipped.Width * _dip_scale,
                clipped.Height * _dip_scale);
            this.ScreenArea.Geometry2 = new RectangleGeometry(selection);
        }

        /// <summary>
        /// ライブ撮影用の白い選択枠を、再描画を伴わないネイティブリージョンで用意する。
        /// </summary>
        private bool CreateSelectionBorderWindow()
        {
            try
            {
                _selection_border_window = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = false,
                    ResizeMode = ResizeMode.NoResize,
                    Background = System.Windows.Media.Brushes.White,
                    Topmost = true,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    Owner = this,
                };

                var helper = new WindowInteropHelper(_selection_border_window);
                _selection_border_handle = helper.EnsureHandle();
                if (_selection_border_handle == IntPtr.Zero)
                {
                    CloseSelectionBorderWindow();
                    return false;
                }

                IntPtr old_style = GetWindowLongPtr(_selection_border_handle, GWL_EXSTYLE);
                IntPtr new_style = new IntPtr(
                    old_style.ToInt64() |
                    (long)WS_EX.NOACTIVATE |
                    (long)WS_EX.TOOLWINDOW |
                    (long)WS_EX.TRANSPARENT);
                SetWindowLongPtr(_selection_border_handle, GWL_EXSTYLE, new_style);

                // 白い矩形が一瞬見えないよう、空リージョンを設定してから表示する。
                ApplyRegion(_selection_border_handle, CreateRectRgn(0, 0, 0, 0));
                _selection_border_window.Show();
                SetNativeWindowBounds();
                return true;
            }
            catch (Exception)
            {
                CloseSelectionBorderWindow();
                ClearSelectionRegion();
                return false;
            }
        }

        private void UpdateSelectionRegion(Rectangle clipped)
        {
            int left = clipped.Left - _monitor_bounds.Left;
            int top = clipped.Top - _monitor_bounds.Top;
            int right = clipped.Right - _monitor_bounds.Left;
            int bottom = clipped.Bottom - _monitor_bounds.Top;

            // 暗転オーバーレイから選択範囲を切り抜き、背後の画面をそのまま見せる。
            SetRegion(
                _window_handle,
                new Rectangle(0, 0, _monitor_bounds.Width, _monitor_bounds.Height),
                Rectangle.FromLTRB(left, top, right, bottom));

            // 枠は選択範囲の外側へ物理ピクセル単位で描く。
            SetRegion(
                _selection_border_handle,
                Rectangle.FromLTRB(
                    left - SELECTION_BORDER_THICKNESS,
                    top - SELECTION_BORDER_THICKNESS,
                    right + SELECTION_BORDER_THICKNESS,
                    bottom + SELECTION_BORDER_THICKNESS),
                Rectangle.FromLTRB(left, top, right, bottom));
        }

        private void ClearSelectionRegion()
        {
            ApplyRegion(_window_handle, IntPtr.Zero);
            ApplyRegion(_selection_border_handle, CreateRectRgn(0, 0, 0, 0));
        }

        private static void SetRegion(IntPtr window_handle, Rectangle outer, Rectangle inner)
        {
            if (window_handle == IntPtr.Zero)
                return;

            IntPtr outer_region = CreateRectRgn(outer.Left, outer.Top, outer.Right, outer.Bottom);
            IntPtr inner_region = CreateRectRgn(inner.Left, inner.Top, inner.Right, inner.Bottom);
            IntPtr combined_region = CreateRectRgn(0, 0, 0, 0);
            try
            {
                CombineRgn(combined_region, outer_region, inner_region, RGN_DIFF);
                ApplyRegion(window_handle, combined_region);
                combined_region = IntPtr.Zero;
            }
            finally
            {
                Functions.DeleteObject(outer_region);
                Functions.DeleteObject(inner_region);
                if (combined_region != IntPtr.Zero)
                    Functions.DeleteObject(combined_region);
            }
        }

        private static void ApplyRegion(IntPtr window_handle, IntPtr region)
        {
            // SetWindowRgn成功時はリージョンの所有権がWindowsへ移る。
            bool applied = window_handle != IntPtr.Zero && SetWindowRgn(window_handle, region, true) != 0;
            if (!applied && region != IntPtr.Zero)
                Functions.DeleteObject(region);
        }

        private void CloseSelectionBorderWindow()
        {
            Window? selection_border_window = _selection_border_window;
            if (selection_border_window == null)
                return;

            try
            {
                selection_border_window.Close();
                _selection_border_window = null;
                _selection_border_handle = IntPtr.Zero;
            }
            catch (Exception)
            {
                // 参照を保持し、Ownerである撮影オーバーレイの終了処理にも閉鎖を委ねる。
            }
        }

        internal void HideForCapture()
        {
            _selection_border_window?.Hide();
            Hide();
        }

        internal void ReleaseSelectionCapture()
        {
            if (this.Path1.IsMouseCaptured)
                this.Path1.ReleaseMouseCapture();
        }

        internal static WpfPoint GetPhysicalCursorPosition()
        {
            if (!GetCursorPos(out NativePoint point))
                return new WpfPoint();

            return new WpfPoint(point.X, point.Y);
        }

        private IntPtr WindowProc(
            IntPtr hwnd,
            int message,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (!_closing && message == WM_DPICHANGED)
            {
                Dispatcher.BeginInvoke(new Action(UpdateDpiAndGeometry));
            }

            return IntPtr.Zero;
        }

        private void DrawingPath_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (_session.BeginSelection(MouseButton.Left))
                this.Path1.CaptureMouse();
        }

        private void DrawingPath_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            _session.CompleteSelection(MouseButton.Left, GetPhysicalCursorPosition());
        }

        private void DrawingPath_MouseMove(object sender, MouseEventArgs e)
        {
            _session.UpdateSelection(GetPhysicalCursorPosition());
        }

        private void DrawingPath_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (_session.BeginSelection(MouseButton.Right))
                this.Path1.CaptureMouse();
        }

        private void DrawingPath_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            _session.CompleteSelection(MouseButton.Right, GetPhysicalCursorPosition());
        }

        private void Window_KeyDown(ref MSG msg, ref bool handled)
        {
            if (msg.message != Functions.WM_HOTKEY ||
                msg.wParam.ToInt32() != Functions.HOTKEY_ID[10])
                return;

            handled = true;
            _session.Cancel();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _closed = true;
            _closing = true;
            _session.Cancel();
            ReleaseSelectionCapture();
            _hwnd_source?.RemoveHook(WindowProc);
            CloseSelectionBorderWindow();

            if (_escape_hotkey_registered)
            {
                Functions.DisableHotkey(_window_handle, Functions.HOTKEY_ID[10]);
                ComponentDispatcher.ThreadPreprocessMessage -= Window_KeyDown;
                _escape_hotkey_registered = false;
            }
        }
    }
}
