// Copyright information
//   © astel-labs.net.
//   Licence: New BSD License (https://opensource.org/licenses/BSD-3-Clause)

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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

        public Shoot(System.Windows.Media.Imaging.BitmapSource? background_image = null)
        {
            InitializeComponent();

            // ウィンドウタイトルの設定
            var executing_assembly = Assembly.GetExecutingAssembly();
            this.Title = executing_assembly.GetName().Name;

            // 指定された場合に背景に画像を設定
            if (background_image != null)
                this.Background = new ImageBrush((ImageSource)background_image);

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
            // ウィンドウサイズの設定
            this.Left = 0.0;
            this.Top = 0.0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;

            // ジオメトリサイズの設定
            var protruding_width = this.Path1.StrokeThickness / 2.0;
            this.ScreenArea.Geometry1 = new RectangleGeometry(new Rect(-protruding_width, -protruding_width, SystemParameters.PrimaryScreenWidth + (protruding_width * 2), SystemParameters.PrimaryScreenHeight + (protruding_width * 2)));

            // ウィンドウをアクティブにしないように設定
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            var hwnd = helper.Handle;
            var oldStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            var newStyle = new IntPtr(oldStyle.ToInt64() | (long)WS_EX.NOACTIVATE | (long)WS_EX.TOOLWINDOW);
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, newStyle);

            // 背景に画像が設定されていたらウィンドウをアクティブにする
            if (this.Background.GetType() == typeof(ImageBrush))
                this.Activate();

            // ウィンドウを可視化する
            this.Opacity = 100.0;
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

            // マウスキャプチャの終了
            _trimEnable = false;
            path.ReleaseMouseCapture();
            this.ScreenArea.Geometry2 = this.ScreenArea.Geometry1;
            await System.Threading.Tasks.Task.Delay(100);

            // 画面キャプチャ
            CaptureScreen(point);

            // アプリケーションの終了
            this.DialogResult = true;
            this.Close();
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

        private void CaptureScreen(Point point)
        {
            // 座標変換
            var start = PointToScreen(_position);
            var end = PointToScreen(point);

            // キャプチャエリアの取得
            var x = start.X < end.X ? (int)start.X : (int)end.X;
            var y = start.Y < end.Y ? (int)start.Y : (int)end.Y;
            var width = (int)Math.Abs(end.X - start.X);
            var height = (int)Math.Abs(end.Y - start.Y);
            if (width == 0 || height == 0)
                return;

            // スクリーンイメージの取得
            image = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graph = System.Drawing.Graphics.FromImage(image)) {
                // 画面をコピーする
                graph.CopyFromScreen(new System.Drawing.Point(x, y), new System.Drawing.Point(), image.Size);
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

            // マウスキャプチャの終了
            _trimEnable = false;
            path.ReleaseMouseCapture();
            this.ScreenArea.Geometry2 = this.ScreenArea.Geometry1;
            await System.Threading.Tasks.Task.Delay(100);

            // 画面キャプチャ
            CaptureScreen(point);

            // アプリケーションの終了
            this.DialogResult = true;
            this.Close();
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
