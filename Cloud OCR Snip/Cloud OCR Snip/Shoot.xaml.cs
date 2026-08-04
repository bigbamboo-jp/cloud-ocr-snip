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

        public Shoot(BitmapSource? background_image = null, bool live_capture_mode = false)
        {
            InitializeComponent();

            _background_image = background_image;
            _live_capture_mode = live_capture_mode;

            // ウィンドウタイトルの設定
            var executing_assembly = Assembly.GetExecutingAssembly();
            this.Title = executing_assembly.GetName().Name;

            // 指定された場合に背景に画像を設定
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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private enum WS_EX : long
        {
            TOOLWINDOW = 0x00000080,
            NOACTIVATE = 0x08000000,
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 全ディスプレイを囲む仮想画面の範囲にウィンドウを配置する
            // WPFのWindowは論理座標を使用するため、SystemParametersの値を使用する
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;

            // ジオメトリサイズの設定
            var protruding_width = this.Path1.StrokeThickness / 2.0;
            this.ScreenArea.Geometry1 = new RectangleGeometry(new Rect(
                -protruding_width,
                -protruding_width,
                this.Width + (protruding_width * 2.0),
                this.Height + (protruding_width * 2.0)));

            // ウィンドウをアクティブにしないように設定
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            var hwnd = helper.Handle;
            var oldStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            var newStyle = new IntPtr(oldStyle.ToInt64() | (long)WS_EX.NOACTIVATE | (long)WS_EX.TOOLWINDOW);
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, newStyle);

            // 背景に画像が設定されていたらウィンドウをアクティブにする
            if (_background_image != null)
                this.Activate();

            // ウィンドウを可視化する
            this.Opacity = 1.0;
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
            // 矩形の描画
            var x = _position.X < point.X ? _position.X : point.X;
            var y = _position.Y < point.Y ? _position.Y : point.Y;
            var width = Math.Abs(point.X - _position.X);
            var height = Math.Abs(point.Y - _position.Y);
            this.ScreenArea.Geometry2 = new RectangleGeometry(new Rect(x, y, width, height));
        }

        private async Task CompleteCapture(Path path, Point point)
        {
            // 画面座標はウィンドウを隠す前に取得する
            var capture_area = GetCaptureArea(point);

            // マウスキャプチャの終了
            _trimEnable = false;
            path.ReleaseMouseCapture();
            this.ScreenArea.Geometry2 = this.ScreenArea.Geometry1;

            bool captured = false;
            if (capture_area.HasValue)
            {
                await Task.Delay(100);

                if (_live_capture_mode)
                {
                    // Topmostの選択ウィンドウ自身を取り込まないよう、一時的に隠す
                    this.Hide();
                    await Task.Delay(100);
                    captured = CaptureCurrentScreen(capture_area.Value);
                }
                else
                {
                    // 選択開始前に取得した画像から切り出すため、タスクバーも保持される
                    captured = CaptureBackgroundImage(capture_area.Value);
                }
            }

            // アプリケーションの終了
            this.DialogResult = captured;
            this.Close();
        }

        private System.Drawing.Rectangle? GetCaptureArea(Point point)
        {
            // 座標変換
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

            System.Drawing.Rectangle virtual_screen = System.Windows.Forms.SystemInformation.VirtualScreen;
            int source_x = capture_area.X - virtual_screen.Left;
            int source_y = capture_area.Y - virtual_screen.Top;
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
                this.DialogResult = false;
                this.Close();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // ホットキーの登録解除
            Functions.DisableHotkey(window_handle, Functions.HOTKEY_ID[10]);
            System.Windows.Interop.ComponentDispatcher.ThreadPreprocessMessage -= this.Window_KeyDown;
        }
    }
}
