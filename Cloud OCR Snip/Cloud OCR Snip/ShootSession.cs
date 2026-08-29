using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using WpfPoint = System.Windows.Point;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// 複数のディスプレイ別オーバーレイを管理する撮影セッション。
    /// 選択範囲は常に物理スクリーン座標で保持し、各オーバーレイへ変換して描画する。
    /// </summary>
    public sealed class ShootSession
    {
        private const int HIDE_WAIT_TIME = 33;
        private const int CLOSE_RETRY_LIMIT = 3;
        private const int VK_ESCAPE = 0x1B;
        private const int VK_LBUTTON = 0x01;
        private const int VK_RBUTTON = 0x02;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtual_key);

        private readonly BitmapSource? _background_image;
        private readonly bool _live_capture_mode;
        private readonly Rectangle _virtual_bounds;
        private readonly IReadOnlyList<Rectangle> _monitor_bounds;
        private readonly List<Shoot> _overlays = new();
        private readonly DispatcherTimer _cursor_timer;
        private readonly CancellationTokenSource _capture_cancellation = new();

        private DispatcherFrame? _dispatcher_frame;
        private WpfPoint _selection_start;
        private MouseButton _selection_button;
        private bool _selecting;
        private bool _completed;
        private bool _result;
        private bool _close_retry_scheduled;
        private int _close_retry_count;

        public ShootSession(
            BitmapSource? background_image,
            bool live_capture_mode,
            Rectangle virtual_bounds,
            IReadOnlyList<Rectangle> monitor_bounds)
        {
            _background_image = background_image;
            _live_capture_mode = live_capture_mode;
            _virtual_bounds = virtual_bounds;
            _monitor_bounds = monitor_bounds;

            _cursor_timer = new DispatcherTimer(
                DispatcherPriority.Input)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _cursor_timer.Tick += CursorTimer_Tick;
        }

        public Bitmap? image { get; private set; }

        /// <summary>
        /// 撮影画像の所有権を呼び出し元へ移す。取得後の破棄は呼び出し元が行う。
        /// </summary>
        public Bitmap? TakeImage()
        {
            Bitmap? captured_image = image;
            image = null;
            return captured_image;
        }

        /// <summary>
        /// Window.ShowDialog()と同様に、撮影完了まで現在のDispatcherをモーダルループに入れる。
        /// </summary>
        public bool ShowDialog()
        {
            if (_monitor_bounds.Count == 0)
                return false;

            // ディスプレイ構成変更が表示処理中に通知されても、先にフレームを用意しておけば
            // Cancelから確実にモーダルループを終了できる。
            _dispatcher_frame = new DispatcherFrame();
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            try
            {
                for (int index = 0; index < _monitor_bounds.Count; index++)
                {
                    _overlays.Add(new Shoot(
                        this,
                        _monitor_bounds[index],
                        _virtual_bounds,
                        _background_image,
                        _live_capture_mode,
                        register_escape_hotkey: index == 0));
                }

                // Show中にもDisplaySettingsChangedが処理され得るため、スナップショットを列挙する。
                foreach (Shoot overlay in _overlays.ToArray())
                {
                    if (_completed)
                        break;

                    overlay.Show();
                }

                if (!_completed)
                    _cursor_timer.Start();

                // 背景画像方式では選択ウィンドウへフォーカスを移し、マウス入力を受け取る。
                if (!_completed && _background_image != null && _overlays.Count > 0)
                    _overlays[0].Activate();

                if (!_completed)
                    Dispatcher.PushFrame(_dispatcher_frame);

                return _result;
            }
            finally
            {
                SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
                _cursor_timer.Stop();
                if (!_completed)
                    Finish(false);
            }
        }

        private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher dispatcher = _cursor_timer.Dispatcher;
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                return;

            // セッション開始時の物理座標スナップショットは再利用せず、安全に閉じて
            // 次回撮影時にDisplayHelperから最新の構成を取得する。
            dispatcher.BeginInvoke(new Action(Cancel), DispatcherPriority.Send);
        }

        internal bool BeginSelection(MouseButton button)
        {
            if (_completed || _selecting)
                return false;

            _selection_button = button;
            _selection_start = Shoot.GetPhysicalCursorPosition();
            _selecting = true;
            UpdateSelection(_selection_start);
            return true;
        }

        internal void UpdateSelection(WpfPoint current)
        {
            if (!_selecting || _completed)
                return;

            Rectangle? capture_area = GetCaptureArea(_selection_start, current);
            foreach (Shoot overlay in _overlays)
                overlay.SetSelection(capture_area);
        }

        internal async void CompleteSelection(MouseButton button, WpfPoint current)
        {
            if (!_selecting || _completed || button != _selection_button)
                return;

            _selecting = false;
            _cursor_timer.Stop();
            ReleaseSelectionCapture();

            Rectangle? capture_area = GetCaptureArea(_selection_start, current);
            bool captured = false;

            try
            {
                if (capture_area.HasValue)
                {
                    if (_live_capture_mode)
                    {
                        // オーバーレイ自身をライブ撮影へ含めない。
                        foreach (Shoot overlay in _overlays)
                            overlay.HideForCapture();

                        await Task.Delay(HIDE_WAIT_TIME, _capture_cancellation.Token);
                        if (_completed)
                            return;

                        captured = CaptureCurrentScreen(capture_area.Value);
                    }
                    else
                    {
                        if (_completed)
                            return;

                        // 事前取得画像から切り出すため、タスクバーも保持される。
                        captured = CaptureBackgroundImage(capture_area.Value);
                    }
                }

                if (!_completed)
                    Finish(captured);
            }
            catch (OperationCanceledException) when (_capture_cancellation.IsCancellationRequested)
            {
                // キャンセル時はCancel側のFinishでオーバーレイを閉じる。
            }
            catch (Exception)
            {
                // 非同期イベントから例外を外へ逃がさず、撮影セッションを確実に終了する。
                Finish(false);
            }
        }

        internal void Cancel()
        {
            if (_completed)
                return;

            _selecting = false;
            _cursor_timer.Stop();
            ReleaseSelectionCapture();
            _capture_cancellation.Cancel();
            Finish(false);
        }

        private void CursorTimer_Tick(object? sender, EventArgs e)
        {
            if (_completed)
                return;

            // Escのグローバルホットキー登録に失敗した場合もキャンセルできるようにする。
            if ((GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0)
            {
                Cancel();
                return;
            }

            if (!_selecting)
                return;

            WpfPoint current = Shoot.GetPhysicalCursorPosition();
            UpdateSelection(current);

            int virtual_key = _selection_button == MouseButton.Right
                ? VK_RBUTTON
                : VK_LBUTTON;
            bool button_down = (GetAsyncKeyState(virtual_key) & 0x8000) != 0;
            if (!button_down)
                CompleteSelection(_selection_button, current);
        }

        private Rectangle? GetCaptureArea(WpfPoint start, WpfPoint end)
        {
            int left = (int)Math.Floor(Math.Min(start.X, end.X));
            int top = (int)Math.Floor(Math.Min(start.Y, end.Y));
            int right = (int)Math.Ceiling(Math.Max(start.X, end.X));
            int bottom = (int)Math.Ceiling(Math.Max(start.Y, end.Y));
            int width = right - left;
            int height = bottom - top;

            if (width <= 0 || height <= 0)
                return null;

            Rectangle capture_area = new Rectangle(left, top, width, height);
            capture_area = Rectangle.Intersect(_virtual_bounds, capture_area);
            if (capture_area.Width <= 0 || capture_area.Height <= 0)
                return null;

            return capture_area;
        }

        private void Finish(bool captured)
        {
            if (_completed)
                return;

            _completed = true;
            _result = captured;
            _cursor_timer.Stop();
            _capture_cancellation.Cancel();
            CloseOverlays();

            if (_dispatcher_frame != null)
                _dispatcher_frame.Continue = false;
        }

        private void CloseOverlays()
        {
            var failed_overlays = new List<Shoot>();
            foreach (Shoot overlay in _overlays.ToArray())
            {
                try
                {
                    if (!overlay.IsClosed)
                        overlay.Close();
                }
                catch (Exception)
                {
                    // 1つの終了失敗で他画面の後始末を止めず、失敗対象だけ後で再試行する。
                    if (!overlay.IsClosed)
                        failed_overlays.Add(overlay);
                }
            }

            _overlays.Clear();
            _overlays.AddRange(failed_overlays);

            if (failed_overlays.Count == 0 ||
                _close_retry_scheduled ||
                _close_retry_count >= CLOSE_RETRY_LIMIT)
                return;

            _close_retry_scheduled = true;
            _cursor_timer.Dispatcher.BeginInvoke(new Action(() =>
            {
                _close_retry_scheduled = false;
                _close_retry_count++;
                CloseOverlays();
            }), DispatcherPriority.ContextIdle);
        }

        private void ReleaseSelectionCapture()
        {
            foreach (Shoot overlay in _overlays.ToArray())
                overlay.ReleaseSelectionCapture();
        }

        private bool CaptureCurrentScreen(Rectangle capture_area)
        {
            try
            {
                image = new Bitmap(
                    capture_area.Width,
                    capture_area.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (Graphics graph = Graphics.FromImage(image))
                {
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

        private bool CaptureBackgroundImage(Rectangle capture_area)
        {
            if (_background_image == null)
                return false;

            int source_x = capture_area.X - _virtual_bounds.Left;
            int source_y = capture_area.Y - _virtual_bounds.Top;
            if (source_x < 0 || source_y < 0 ||
                source_x + capture_area.Width > _background_image.PixelWidth ||
                source_y + capture_area.Height > _background_image.PixelHeight)
                return false;

            try
            {
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
                    new Int32Rect(
                        source_x,
                        source_y,
                        capture_area.Width,
                        capture_area.Height),
                    pixels,
                    stride,
                    0);

                image = new Bitmap(
                    capture_area.Width,
                    capture_area.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                System.Drawing.Imaging.BitmapData bitmap_data = image.LockBits(
                    new Rectangle(
                        0,
                        0,
                        capture_area.Width,
                        capture_area.Height),
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
    }
}
