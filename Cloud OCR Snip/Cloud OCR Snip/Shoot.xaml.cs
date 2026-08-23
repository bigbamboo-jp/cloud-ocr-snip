// Copyright information
//   © astel-labs.net.
//   Licence: New BSD License (https://opensource.org/licenses/BSD-3-Clause)

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// (無題)
    /// 機能：範囲を限定したスクリーンショットの撮影
    /// ソース：https://astel-labs.net/blog/diary/2012/07/21-1.html
    /// メモ：可能な限りオリジナルにコーディングスタイルを合わせる
    /// （Shoot.xaml の相互作用ロジック）
    /// </summary>
    public partial class Shoot : Window
    {
        private Point _position;
        private bool _trimEnable = false;
        private int _mouseButton = 0;
        private readonly BitmapSource? _background_image;
        private readonly bool _live_capture_mode;
        private readonly System.Drawing.Rectangle _background_area; // 背景画像を撮影したときの仮想画面の範囲

        public Shoot(BitmapSource? background_image = null, bool live_capture_mode = false, System.Drawing.Rectangle background_area = default)
        {
            InitializeComponent();

            _background_image = background_image;
            _live_capture_mode = live_capture_mode;
            _background_area = background_area;

            // 透過ウィンドウはGPUによる描画が使えず、画面全体の描画がとても遅くなる
            // 切り取り方式は画面全体を撮影済みの画像で覆うため透過が不要なので、透過にしない
            // （ライブ撮影方式は背景を透かす必要があるため、透過のままにする）
            this.AllowsTransparency = _live_capture_mode;

            // ウィンドウが作られた直後、表示される前に配置する（既定の位置で一瞬表示されるのを防ぐ）
            this.SourceInitialized += this.Window_SourceInitialized;

            // ウィンドウタイトルの設定
            var executing_assembly = Assembly.GetExecutingAssembly();
            this.Title = executing_assembly.GetName().Name;

            // 指定された場合に背景に画像を設定（切り取り方式の場合）
            // 設定しない場合は背景が透過したままになり、背景の動きが見える（ライブ撮影方式の場合）
            if (_background_image != null)
                this.Background = new ImageBrush(_background_image);

            // ホットキーの登録
            var host = new System.Windows.Interop.WindowInteropHelper(this);
            window_handle = host.Handle;
            System.Windows.Interop.ComponentDispatcher.ThreadPreprocessMessage += this.Window_KeyDown;
            Functions.SetUpHotkey(window_handle, Functions.HOTKEY_ID[10], (ModifierKeys)0, Key.Escape);
        }

        public System.Drawing.Bitmap? image;

        private readonly IntPtr window_handle;
        private IntPtr shoot_window_handle = IntPtr.Zero; // 表示後に取得したこのウィンドウのハンドル
        private long last_dpi_changed_tick = 0; // 直近でDPIの変更通知を受けた時刻
        private int continuous_dpi_changed_count = 0; // 連続してDPIの変更通知を受けた回数
        private int coverage_retry_count = 0; // 仮想画面全体を覆うために配置し直した回数
        private System.Windows.Threading.DispatcherTimer? coverage_check_timer; // 配置を確認するタイマー
        private bool shooting_finished = false; // 撮影またはキャンセルが確定したかのフラグ
        private bool use_region_selection = false; // 選択範囲をウィンドウの形（リージョン）で表すかのフラグ
        private Window? selection_border_window; // 選択範囲の枠を表示するウィンドウ
        private IntPtr selection_border_handle = IntPtr.Zero; // 選択範囲の枠のウィンドウハンドル

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int fnCombineMode);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private const int GWL_EXSTYLE = -20;
        private enum WS_EX : long
        {
            TRANSPARENT = 0x00000020,
            TOOLWINDOW = 0x00000080,
            NOACTIVATE = 0x08000000,
        }

        private const int DPI_CHANGED_LIMIT = 8; // 連続してDPIの変更通知を受けたときに配置し直す回数の上限
        private const long DPI_CHANGED_INTERVAL = 1000; // 連続した通知とみなす間隔（ミリ秒）
        private const int COVERAGE_CHECK_INTERVAL = 500; // 配置を確認するまでの時間（ミリ秒）
        private const int COVERAGE_RETRY_LIMIT = 3; // 仮想画面全体を覆うために配置し直す回数の上限
        private const int SELECTION_BORDER_THICKNESS = 2; // 選択範囲の枠の太さ（物理ピクセル）
        private const int RGN_DIFF = 4; // リージョンの差分結合
        private const int SW_HIDE = 0; // ウィンドウを非表示にする
        private const int HIDE_WAIT_TIME = 33; // 隠した後に撮影するまでの待ち時間（ミリ秒）
                                               // 実測では隠した直後に画面へ反映されるが、余裕を持たせている

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOOWNERZORDER = 0x0200;

        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            // ウィンドウをアクティブにしないように設定
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            var hwnd = helper.Handle;
            shoot_window_handle = hwnd;
            var oldStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            var newStyle = new IntPtr(oldStyle.ToInt64() | (long)WS_EX.NOACTIVATE | (long)WS_EX.TOOLWINDOW);
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, newStyle);

            // ウィンドウのサイズに合わせて描画範囲を追従させる
            this.LayoutRoot.SizeChanged += this.LayoutRoot_SizeChanged;
            // 表示先のディスプレイのDPIが変わった場合に配置し直す
            this.DpiChanged += this.Window_DpiChanged;
            // ディスプレイの数や配置が変わった場合に対応する
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += this.SystemEvents_DisplaySettingsChanged;

            // 全ディスプレイを囲む仮想画面の範囲にウィンドウを配置する
            ArrangeToVirtualScreen();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ライブ撮影方式では、選択範囲をウィンドウの形で表す方式を用意する
            // WPFの描画内容を変えると画面全体の再描画が発生して、選択中の動きがぎこちなくなるため
            if (_live_capture_mode == true)
                use_region_selection = CreateSelectionBorderWindow();

            // 表示された後にも配置し直す（WPFが表示時に位置とサイズを設定し直すため）
            ArrangeToVirtualScreen();

            // 切り取り方式の場合はウィンドウをアクティブにする
            // ライブ撮影方式では、撮影対象のウィンドウからフォーカスを奪わないようにアクティブにしない
            if (_live_capture_mode == false)
                this.Activate();

            // ウィンドウを可視化する
            // 透過ウィンドウでない場合はOpacityが効かないため、表示前の配置で対応している
            this.Opacity = 1.0;
        }

        private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            if (shooting_finished == true)
                return;

            // 論理座標と物理ピクセルの倍率が変わるため、選択中の範囲は取り消す
            // （変換前の座標のまま撮影すると、選択した範囲とは別の場所を切り取ってしまう）
            CancelSelection();

            // 前回の通知から時間が空いている場合は、利用者の操作による変更とみなして回数を数え直す
            // 制限するのは、配置し直すことで別のディスプレイのDPIが適用され、通知が連続し続ける場合のみ
            long now = Environment.TickCount64;
            if (now - last_dpi_changed_tick > DPI_CHANGED_INTERVAL)
                continuous_dpi_changed_count = 0;
            last_dpi_changed_tick = now;
            if (continuous_dpi_changed_count >= DPI_CHANGED_LIMIT)
            {
                // 通知が連続している間は配置し直さず、後から配置を確認する
                StartCoverageCheckTimer();
                return;
            }
            continuous_dpi_changed_count++;

            // WPFがDPIの変更通知を受けてウィンドウのサイズを変更するため、その処理の後に配置し直す
            this.Dispatcher.BeginInvoke(new Action(ArrangeToVirtualScreen), System.Windows.Threading.DispatcherPriority.Normal);
        }

        /// <summary>
        /// 仮想画面全体を覆えているかを後から確認するためのタイマーを開始するメソッド
        /// </summary>
        private void StartCoverageCheckTimer()
        {
            if (shooting_finished == true)
                return;

            if (coverage_check_timer == null)
            {
                coverage_check_timer = new System.Windows.Threading.DispatcherTimer();
                coverage_check_timer.Interval = TimeSpan.FromMilliseconds(COVERAGE_CHECK_INTERVAL);
                coverage_check_timer.Tick += this.CoverageCheckTimer_Tick;
            }
            // 動作中の場合はそのままにして、確認を先延ばしにしない
            if (coverage_check_timer.IsEnabled == false)
                coverage_check_timer.Start();
        }

        private void CoverageCheckTimer_Tick(object? sender, EventArgs e)
        {
            coverage_check_timer?.Stop();
            if (shooting_finished == true)
                return;

            if (IsCoveringVirtualScreen() == true)
            {
                // 覆えている場合は、次の変更に備えて回数を数え直す
                coverage_retry_count = 0;
                continuous_dpi_changed_count = 0;
                return;
            }

            // 覆えていない範囲でマウスを押すと、その操作が下のアプリケーションに伝わってしまう
            // 意図しない操作を防ぐため、配置し直して回復を試みる
            if (coverage_retry_count < COVERAGE_RETRY_LIMIT)
            {
                coverage_retry_count++;
                continuous_dpi_changed_count = 0;
                ArrangeToVirtualScreen();
                return;
            }

            // 回復できない場合は、画面を覆ったまま操作を受け付けるより中止する方が安全なため撮影を中止する
            CancelShooting();
        }

        /// <summary>
        /// ウィンドウが仮想画面全体を覆えているかを返すメソッド
        /// </summary>
        private bool IsCoveringVirtualScreen()
        {
            if (shoot_window_handle == IntPtr.Zero)
                return false;
            if (GetWindowRect(shoot_window_handle, out RECT window_rect) == false)
                return false;

            System.Drawing.Rectangle virtual_screen = System.Windows.Forms.SystemInformation.VirtualScreen;
            return window_rect.left <= virtual_screen.Left &&
                window_rect.top <= virtual_screen.Top &&
                window_rect.right >= virtual_screen.Right &&
                window_rect.bottom >= virtual_screen.Bottom;
        }

        private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            // 別のスレッドから通知されるため、UIスレッドで処理する
            this.Dispatcher.BeginInvoke(new Action(DisplaySettingsChanged), System.Windows.Threading.DispatcherPriority.Normal);
        }

        /// <summary>
        /// ディスプレイの数や配置が変わった場合の処理をするメソッド
        /// </summary>
        private void DisplaySettingsChanged()
        {
            if (shooting_finished == true)
                return;

            if (_live_capture_mode == false)
            {
                // 切り取り方式では、撮影済みの背景画像が現在の画面と一致しなくなるため撮影を中止する
                CancelShooting();
                return;
            }

            // 仮想画面の原点が変わると選択開始座標が指す位置もずれるため、選択中の範囲は取り消す
            CancelSelection();

            // ライブ撮影方式では、変更後の仮想画面の範囲に合わせて配置し直す
            ArrangeToVirtualScreen();
        }

        /// <summary>
        /// 選択中の範囲を取り消して、選択前の状態に戻すメソッド
        /// </summary>
        private void CancelSelection()
        {
            if (_trimEnable == false)
                return;

            // マウスキャプチャの終了
            _trimEnable = false;
            this.Path1.ReleaseMouseCapture();
            // 選択範囲の表示を消す（画面全体が選択前の状態に戻る）
            if (use_region_selection == true)
                ClearSelectionRegion();
            else
                this.ScreenArea.Geometry2 = null;
        }

        /// <summary>
        /// 撮影のために、選択画面のウィンドウを画面から隠すメソッド
        /// </summary>
        private void HideWindows()
        {
            if (selection_border_handle != IntPtr.Zero)
                ShowWindow(selection_border_handle, SW_HIDE);
            if (shoot_window_handle != IntPtr.Zero)
                ShowWindow(shoot_window_handle, SW_HIDE);
        }

        /// <summary>
        /// 撮影を中止するメソッド
        /// </summary>
        private void CancelShooting()
        {
            if (shooting_finished == true)
                return;
            shooting_finished = true;

            this.DialogResult = false;
            this.Close();
        }

        private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScreenAreaGeometry();
        }

        /// <summary>
        /// 全ディスプレイを囲む仮想画面の範囲にウィンドウを配置するメソッド
        /// </summary>
        private void ArrangeToVirtualScreen()
        {
            if (shoot_window_handle == IntPtr.Zero || shooting_finished == true)
                return;

            // 仮想画面の範囲を物理ピクセルで取得する
            // 左側または上側にディスプレイがある場合、Left/Topは負の値になる
            System.Drawing.Rectangle virtual_screen = System.Windows.Forms.SystemInformation.VirtualScreen;

            // WPFのLeft/Top/Width/Heightは、このウィンドウが表示されているディスプレイのDPIで
            // 物理ピクセルに換算されるため、ディスプレイごとにDPIが異なる環境では仮想画面全体を覆えない
            // そのため、物理ピクセルのまま配置する
            // あわせて最前面に上げる（他の最前面ウィンドウの下に隠れると、その部分への操作が
            // このウィンドウではなく下のアプリケーションに伝わってしまうため）
            SetWindowPos(
                shoot_window_handle,
                HWND_TOPMOST,
                virtual_screen.Left,
                virtual_screen.Top,
                virtual_screen.Width,
                virtual_screen.Height,
                SWP_NOACTIVATE | SWP_NOOWNERZORDER);

            // 選択範囲の枠のウィンドウも同じ範囲に配置する
            if (selection_border_handle != IntPtr.Zero)
            {
                SetWindowPos(
                    selection_border_handle,
                    HWND_TOPMOST,
                    virtual_screen.Left,
                    virtual_screen.Top,
                    virtual_screen.Width,
                    virtual_screen.Height,
                    SWP_NOACTIVATE | SWP_NOOWNERZORDER);
            }

            // 配置後のサイズに合わせて描画範囲を設定する
            UpdateScreenAreaGeometry();

            // 仮想画面全体を覆えているかを後から確認する
            StartCoverageCheckTimer();
        }

        /// <summary>
        /// ウィンドウ全体を覆うように描画範囲（ジオメトリ）を設定するメソッド
        /// </summary>
        private void UpdateScreenAreaGeometry()
        {
            // 描画はWPFの論理座標で行うため、このウィンドウのDPIで換算されたサイズを使用する
            double width = this.LayoutRoot.ActualWidth;
            double height = this.LayoutRoot.ActualHeight;
            if (width <= 0.0 || height <= 0.0)
            {
                // レイアウトが未確定の場合は、このウィンドウのDPIから換算する
                System.Drawing.Rectangle virtual_screen = System.Windows.Forms.SystemInformation.VirtualScreen;
                DpiScale dpi = VisualTreeHelper.GetDpi(this);
                width = virtual_screen.Width / dpi.DpiScaleX;
                height = virtual_screen.Height / dpi.DpiScaleY;
            }

            // ジオメトリサイズの設定
            var protruding_width = this.Path1.StrokeThickness / 2.0;
            this.ScreenArea.Geometry1 = new RectangleGeometry(new Rect(
                -protruding_width,
                -protruding_width,
                width + (protruding_width * 2.0),
                height + (protruding_width * 2.0)));
        }

        private void DrawingPath_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_trimEnable)
                return;

            var path = sender as Path;
            if (path == null)
                return;

            // 開始座標を取得
            var point = e.GetPosition(path);
            _position = point;

            // マウスキャプチャの設定
            _trimEnable = true;
            _mouseButton = 0;
            path.CaptureMouse();
        }

        private async void DrawingPath_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_trimEnable || _mouseButton != 0)
                return;

            var path = sender as Path;
            if (path == null)
                return;

            // 現在座標を取得
            var point = e.GetPosition(path);

            await CompleteCapture(path, point);
        }

        private void DrawingPath_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_trimEnable)
                return;

            var path = sender as Path;
            if (path == null)
                return;

            // 現在座標を取得
            var point = e.GetPosition(path);

            // キャプチャ領域枠の描画
            DrawStroke(point);
        }

        private void DrawStroke(Point point)
        {
            if (use_region_selection == true)
            {
                // ウィンドウの形（リージョン）で選択範囲を表す
                UpdateSelectionRegion(GetCaptureArea(point));
                return;
            }

            // 矩形の描画
            var x = _position.X < point.X ? _position.X : point.X;
            var y = _position.Y < point.Y ? _position.Y : point.Y;
            var width = Math.Abs(point.X - _position.X);
            var height = Math.Abs(point.Y - _position.Y);
            this.ScreenArea.Geometry2 = new RectangleGeometry(new Rect(x, y, width, height));
        }

        /// <summary>
        /// 選択範囲の枠を表示するウィンドウを用意するメソッド
        /// </summary>
        /// <returns>用意できたか</returns>
        private bool CreateSelectionBorderWindow()
        {
            try
            {
                // 枠は白一色のウィンドウで表し、リージョンで枠の形に切り抜く
                // 描画内容を変えないため、選択範囲が動いても再描画が発生しない
                selection_border_window = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = false,
                    ResizeMode = ResizeMode.NoResize,
                    Background = System.Windows.Media.Brushes.White,
                    Topmost = true,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    // このウィンドウの手前に表示し、このウィンドウと一緒に閉じるようにする
                    Owner = this,
                };
                // 表示する前にウィンドウだけを作成する
                // 先に表示すると、枠の形を設定するまでの間、既定の大きさの白い矩形が見えてしまう
                var helper = new System.Windows.Interop.WindowInteropHelper(selection_border_window);
                selection_border_handle = helper.EnsureHandle();
                if (selection_border_handle == IntPtr.Zero)
                {
                    // 使えないウィンドウを残さないように閉じる
                    CloseSelectionBorderWindow();
                    return false;
                }

                // アクティブにせず、マウス操作も素通りさせる（選択の邪魔をしないため）
                var oldStyle = GetWindowLongPtr(selection_border_handle, GWL_EXSTYLE);
                var newStyle = new IntPtr(oldStyle.ToInt64()
                    | (long)WS_EX.NOACTIVATE | (long)WS_EX.TOOLWINDOW | (long)WS_EX.TRANSPARENT);
                SetWindowLongPtr(selection_border_handle, GWL_EXSTYLE, newStyle);

                // 選択前は何も表示しない状態にしてから表示する
                ClearSelectionRegion();
                selection_border_window.Show();
                return true;
            }
            catch (Exception)
            {
                // 用意できない場合は、これまで通りWPFの図形で選択範囲を描く
                // 表示したままにすると、白いウィンドウが画面に残って撮影にも写り込むため閉じる
                CloseSelectionBorderWindow();
                return false;
            }
        }

        /// <summary>
        /// 選択範囲の枠のウィンドウを閉じるメソッド
        /// </summary>
        private void CloseSelectionBorderWindow()
        {
            selection_border_handle = IntPtr.Zero;
            if (selection_border_window == null)
                return;

            try
            {
                selection_border_window.Close();
            }
            catch (Exception)
            {
                // 閉じられなかった場合は、このウィンドウと一緒に閉じられるのに任せる
            }
            selection_border_window = null;
        }

        /// <summary>
        /// 選択範囲をウィンドウの形（リージョン）で表すメソッド
        /// </summary>
        /// <param name="capture_area">選択範囲（画面の物理ピクセル）</param>
        private void UpdateSelectionRegion(System.Drawing.Rectangle? capture_area)
        {
            if (use_region_selection == false || shoot_window_handle == IntPtr.Zero)
                return;
            if (capture_area.HasValue == false)
            {
                ClearSelectionRegion();
                return;
            }

            // ウィンドウ内の座標に変換する
            if (GetWindowRect(shoot_window_handle, out RECT window_rect) == false)
                return;
            int left = capture_area.Value.Left - window_rect.left;
            int top = capture_area.Value.Top - window_rect.top;
            int right = capture_area.Value.Right - window_rect.left;
            int bottom = capture_area.Value.Bottom - window_rect.top;
            int window_width = window_rect.right - window_rect.left;
            int window_height = window_rect.bottom - window_rect.top;

            // 暗転から選択範囲を切り抜く（切り抜いた部分から画面がそのまま見える）
            SetRegion(shoot_window_handle,
                new RECT { left = 0, top = 0, right = window_width, bottom = window_height },
                new RECT { left = left, top = top, right = right, bottom = bottom });

            // 枠は選択範囲の外側に、太さの分だけ描く
            int thickness = SELECTION_BORDER_THICKNESS;
            SetRegion(selection_border_handle,
                new RECT
                {
                    left = left - thickness,
                    top = top - thickness,
                    right = right + thickness,
                    bottom = bottom + thickness,
                },
                new RECT { left = left, top = top, right = right, bottom = bottom });
        }

        /// <summary>
        /// 選択範囲の表示を消して、選択前の状態に戻すメソッド
        /// </summary>
        private void ClearSelectionRegion()
        {
            // 画面全体を暗転に戻す
            ApplyRegion(shoot_window_handle, IntPtr.Zero);
            // 枠を消す
            ApplyRegion(selection_border_handle, CreateRectRgn(0, 0, 0, 0));
        }

        /// <summary>
        /// ウィンドウにリージョンを設定するメソッド
        /// </summary>
        /// <param name="window_handle">設定先のウィンドウ</param>
        /// <param name="region">設定するリージョン。形を解除する場合はIntPtr.Zero</param>
        private void ApplyRegion(IntPtr window_handle, IntPtr region)
        {
            // 設定に成功した場合、リージョンの所有権はシステムに移るため解放しない
            // 設定できなかった場合は所有権が移らないため、こちらで解放する
            bool applied = window_handle != IntPtr.Zero
                && SetWindowRgn(window_handle, region, true) != 0;
            if (applied == false && region != IntPtr.Zero)
                Functions.DeleteObject(region);
        }

        /// <summary>
        /// 外側の矩形から内側の矩形を除いた形をウィンドウに設定するメソッド
        /// </summary>
        private void SetRegion(IntPtr window_handle, RECT outer, RECT inner)
        {
            if (window_handle == IntPtr.Zero)
                return;

            IntPtr outer_region = CreateRectRgn(outer.left, outer.top, outer.right, outer.bottom);
            IntPtr inner_region = CreateRectRgn(inner.left, inner.top, inner.right, inner.bottom);
            IntPtr region = CreateRectRgn(0, 0, 0, 0);
            try
            {
                CombineRgn(region, outer_region, inner_region, RGN_DIFF);
                ApplyRegion(window_handle, region);
            }
            finally
            {
                Functions.DeleteObject(outer_region);
                Functions.DeleteObject(inner_region);
            }
        }

        private async Task CompleteCapture(Path path, Point point)
        {
            // 画面座標はウィンドウを隠す前に取得する
            var capture_area = GetCaptureArea(point);

            // マウスキャプチャの終了
            _trimEnable = false;
            path.ReleaseMouseCapture();
            if (use_region_selection == true)
                ClearSelectionRegion();
            else
                this.ScreenArea.Geometry2 = this.ScreenArea.Geometry1;

            bool captured = false;
            if (capture_area.HasValue)
            {
                if (_live_capture_mode)
                {
                    // 選択範囲を決めた後の画面を撮影する
                    // Topmostの選択ウィンドウ自身を取り込まないよう、一時的に隠して描画の反映を待つ
                    // WPFのHide()はモーダル表示（ShowDialog）自体を終了させてしまい、
                    // この後にDialogResultを設定できなくなるため、Win32のShowWindowで隠す
                    HideWindows();
                    await Task.Delay(HIDE_WAIT_TIME);
                    captured = CaptureCurrentScreen(capture_area.Value);
                }
                else
                {
                    // 選択開始前に取得した画像から切り出すため、タスクバーも保持される
                    captured = CaptureBackgroundImage(capture_area.Value);
                }
            }

            // アプリケーションの終了
            // 撮影中にキャンセルされた場合は何もしない
            if (shooting_finished == true)
                return;
            shooting_finished = true;
            this.DialogResult = captured;
            this.Close();
        }

        private System.Drawing.Rectangle? GetCaptureArea(Point point)
        {
            // 座標変換
            // PointToScreenはこのウィンドウのDPIで換算するため、変換後は画面の物理ピクセルになる
            var start = PointToScreen(_position);
            var end = PointToScreen(point);

            // キャプチャエリアの取得
            if (start.X == end.X || start.Y == end.Y)
                return null;

            var left = Math.Min(start.X, end.X);
            var top = Math.Min(start.Y, end.Y);
            var right = Math.Max(start.X, end.X);
            var bottom = Math.Max(start.Y, end.Y);
            var x = (int)Math.Floor(left);
            var y = (int)Math.Floor(top);
            var width = (int)Math.Ceiling(right) - x;
            var height = (int)Math.Ceiling(bottom) - y;
            if (width <= 0 || height <= 0)
                return null;

            return new System.Drawing.Rectangle(x, y, width, height);
        }

        private bool CaptureCurrentScreen(System.Drawing.Rectangle capture_area)
        {
            try
            {
                // スクリーンイメージの取得
                image = new System.Drawing.Bitmap(
                    capture_area.Width,
                    capture_area.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var graph = System.Drawing.Graphics.FromImage(image))
                {
                    // オーバーレイを隠した後の画面をコピーする
                    graph.CopyFromScreen(
                        capture_area.Location,
                        new System.Drawing.Point(),
                        capture_area.Size);
                }
                return true;
            }
            catch
            {
                image?.Dispose();
                image = null;
                return false;
            }
        }

        private bool CaptureBackgroundImage(System.Drawing.Rectangle capture_area)
        {
            if (_background_image == null)
                return false;

            // 現在の仮想画面ではなく、背景画像を撮影したときの範囲を基準にする
            // （撮影後にディスプレイの配置が変わっても、誤った位置を切り出さないようにする）
            int source_x = capture_area.X - _background_area.X;
            int source_y = capture_area.Y - _background_area.Y;
            if (source_x < 0 || source_y < 0 ||
                source_x + capture_area.Width > _background_image.PixelWidth ||
                source_y + capture_area.Height > _background_image.PixelHeight)
                return false;

            try
            {
                // BitmapSourceの画素形式をSystem.Drawing.Bitmapと同じBGRA32に揃える
                BitmapSource source = _background_image;
                if (source.Format != PixelFormats.Bgra32)
                {
                    var converted_source = new FormatConvertedBitmap(
                        source,
                        PixelFormats.Bgra32,
                        null,
                        0);
                    converted_source.Freeze();
                    source = converted_source;
                }

                int stride = capture_area.Width * 4;
                byte[] pixels = new byte[stride * capture_area.Height];
                source.CopyPixels(
                    new Int32Rect(source_x, source_y, capture_area.Width, capture_area.Height),
                    pixels,
                    stride,
                    0);

                image = new System.Drawing.Bitmap(
                    capture_area.Width,
                    capture_area.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                System.Drawing.Imaging.BitmapData bitmap_data = image.LockBits(
                    new System.Drawing.Rectangle(0, 0, capture_area.Width, capture_area.Height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    for (int row = 0; row < capture_area.Height; row++)
                    {
                        Marshal.Copy(
                            pixels,
                            row * stride,
                            IntPtr.Add(bitmap_data.Scan0, row * bitmap_data.Stride),
                            stride);
                    }
                }
                finally
                {
                    image.UnlockBits(bitmap_data);
                }
                return true;
            }
            catch
            {
                image?.Dispose();
                image = null;
                return false;
            }
        }

        private void DrawingPath_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_trimEnable)
                return;

            var path = sender as Path;
            if (path == null)
                return;

            // 開始座標を取得
            var point = e.GetPosition(path);
            _position = point;

            // マウスキャプチャの設定
            _trimEnable = true;
            _mouseButton = 1;
            path.CaptureMouse();
        }

        private async void DrawingPath_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_trimEnable || _mouseButton != 1)
                return;

            var path = sender as Path;
            if (path == null)
                return;

            // 現在座標を取得
            var point = e.GetPosition(path);

            await CompleteCapture(path, point);
        }

        private void Window_KeyDown(ref System.Windows.Interop.MSG msg, ref bool handled)
        {
            // エスケープキーが押されたらキャンセルする
            if (msg.message != Functions.WM_HOTKEY)
                return;
            int hotkey_id = msg.wParam.ToInt32();
            if (hotkey_id == Functions.HOTKEY_ID[10]) {
                CancelShooting();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            shooting_finished = true;

            // ホットキーの登録解除
            Functions.DisableHotkey(window_handle, Functions.HOTKEY_ID[10]);
            System.Windows.Interop.ComponentDispatcher.ThreadPreprocessMessage -= this.Window_KeyDown;
            // ディスプレイの構成変更の購読解除（静的なイベントのため、解除しないとこのウィンドウが残り続ける）
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= this.SystemEvents_DisplaySettingsChanged;
            // 選択範囲の枠のウィンドウを閉じる
            CloseSelectionBorderWindow();

            // タイマーの停止（動いたままだとこのウィンドウが残り続ける）
            if (coverage_check_timer != null)
            {
                coverage_check_timer.Stop();
                coverage_check_timer.Tick -= this.CoverageCheckTimer_Tick;
                coverage_check_timer = null;
            }
        }
    }
}
