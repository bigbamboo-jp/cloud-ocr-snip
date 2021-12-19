using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// メインウィンドウ
    /// 機能：アプリケーションの基盤
    /// （MainWindow.xaml の相互作用ロジック）
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 初期設定が済んでいない場合に初期設定画面を表示する
            if ((string)Functions.GetUserSettings()["transcription_service"] == string.Empty)
            {
                new InitialSetting().Show();
                notify_icon = new System.Windows.Forms.NotifyIcon();
                return;
            }

            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            Title = executing_assembly.GetName().Name;

            // タスクトレイアイコンを表示する
            System.Windows.Forms.ContextMenuStrip menu_strip = new System.Windows.Forms.ContextMenuStrip();
            using Stream icon_stream = Application.GetResourceStream(new Uri("pack://application:,,,/Icons/WhiteIcon.ico")).Stream;
            notify_icon = new System.Windows.Forms.NotifyIcon
            {
                Text = (string)FindResource("task_tray_icon/help_message"),
                Icon = new System.Drawing.Icon(icon_stream),
                Visible = true,
                ContextMenuStrip = menu_strip
            };
            notify_icon.MouseClick += new System.Windows.Forms.MouseEventHandler(Notify_icon_MouseClick);
            System.Windows.Forms.ToolStripMenuItem read_from_clipboard_image_item = new System.Windows.Forms.ToolStripMenuItem
            {
                Text = (string)FindResource("task_tray_icon/read_from_clipboard_image_text")
            };
            // クリップボードに画像が含まれているかに応じてアイテムの有効・無効を切り替える
            void Read_from_clipboard_image_item_Paint(object sender, EventArgs e)
            {
                ((System.Windows.Forms.ToolStripMenuItem)sender).Enabled = Clipboard.ContainsImage();
            }
            read_from_clipboard_image_item.Paint += new System.Windows.Forms.PaintEventHandler(Read_from_clipboard_image_item_Paint);
            read_from_clipboard_image_item.Click += new EventHandler(Read_from_clipboard_image_item_Click);
            menu_strip.Items.Add(read_from_clipboard_image_item);
            System.Windows.Forms.ToolStripMenuItem read_from_image_file_item = new System.Windows.Forms.ToolStripMenuItem
            {
                Text = (string)FindResource("task_tray_icon/read_from_image_file_text")
            };
            read_from_image_file_item.Click += new EventHandler(Read_from_image_file_item_Click);
            menu_strip.Items.Add(read_from_image_file_item);
            System.Windows.Forms.ToolStripSeparator separator_1 = new System.Windows.Forms.ToolStripSeparator();
            menu_strip.Items.Add(separator_1);
            System.Windows.Forms.ToolStripMenuItem settings_item = new System.Windows.Forms.ToolStripMenuItem
            {
                Text = (string)FindResource("task_tray_icon/settings_text")
            };
            settings_item.Click += new EventHandler(Settings_item_Click);
            menu_strip.Items.Add(settings_item);
            System.Windows.Forms.ToolStripMenuItem product_information_item = new System.Windows.Forms.ToolStripMenuItem
            {
                Text = (string)FindResource("task_tray_icon/product_information_text")
            };
            ;
            product_information_item.Click += new EventHandler(Product_information_item_Click);
            menu_strip.Items.Add(product_information_item);
            System.Windows.Forms.ToolStripSeparator separator_2 = new System.Windows.Forms.ToolStripSeparator();
            menu_strip.Items.Add(separator_2);
            System.Windows.Forms.ToolStripMenuItem exit_item = new System.Windows.Forms.ToolStripMenuItem
            {
                Text = (string)FindResource("task_tray_icon/exit_text")
            };
            exit_item.Click += new EventHandler(Exit_item_Click);
            menu_strip.Items.Add(exit_item);

            // ショートカットキーを登録する
            ComponentDispatcher.ThreadPreprocessMessage += Window_KeyDown;
            window_handle = new WindowInteropHelper(this).Handle;
            Set_up_hotkey();
        }

        private System.Windows.Forms.NotifyIcon notify_icon; // タスクトレイアイコン

        private static IntPtr window_handle; // ウィンドウのハンドル

        /// <summary>
        /// ショートカットキーの登録を行うメソッド
        /// </summary>
        public static void Set_up_hotkey(bool reset = false)
        {
            if (reset == true)
            {
                Disable_hotkey();
            }
            Dictionary<string, object> settings = Functions.GetUserSettings();
            for (int i = 1; i <= 2; i++)
            {
                // 有効に設定されているショートカットキーだけを登録する
                // ※キー番号が 1以上 → 有効、0以下 → 無効
                if ((int)settings["hotkey" + i.ToString() + "_mainkey"] > 0)
                {
                    Functions.SetUpHotkey(window_handle, Functions.HOTKEY_ID[i - 1], (ModifierKeys)(int)settings["hotkey" + i + "_modifierkey"], (Key)(int)settings["hotkey" + i + "_mainkey"]);
                }
            }
        }

        /// <summary>
        /// ショートカットキーの登録解除を行うメソッド
        /// </summary>
        public static void Disable_hotkey()
        {
            // 全てのショートカットキーの登録を解除する
            for (int i = 1; i <= 2; i++)
            {
                Functions.DisableHotkey(window_handle, Functions.HOTKEY_ID[i - 1]);
            }
        }

        /// <summary>
        /// ショートカットキーが押された際の処理
        /// </summary>
        private void Window_KeyDown(ref MSG msg, ref bool handled)
        {
            if (msg.message != Functions.WM_HOTKEY)
            {
                return;
            }
            int hotkey_id = msg.wParam.ToInt32();
            if (hotkey_id == Functions.HOTKEY_ID[00])
            {
                // 「画面を撮影して文字を読み取る場合のショートカットキー」が押された場合
                if (Functions.displaying_shoot == false)
                {
                    Functions.DetectScreenshotText();
                }
            }
            else if (hotkey_id == Functions.HOTKEY_ID[01])
            {
                // 「クリップボードの画像から文字を読み取る場合のショートカットキー」が押された場合
                Read_from_clipboard_image_item_Click(null, null);
            }
        }

        /// <summary>
        /// ウィンドウが閉じられた（＝アプリケーションが終了する）際の処理
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            // ショートカットキーの登録を解除する
            Disable_hotkey();
            ComponentDispatcher.ThreadPreprocessMessage -= Window_KeyDown;
            // タスクトレイアイコンを非表示にする
            notify_icon.Dispose();
        }

        /// <summary>
        /// タスクトレイアイコンがクリックされた際の処理
        /// </summary>
        private void Notify_icon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                // スクリーンショットを撮影後その画像から文字を読み取る
                if (Functions.displaying_shoot == false)
                {
                    Functions.DetectScreenshotText();
                }
            }
        }

        /// <summary>
        /// タスクトレイアイコンの「クリップボードの画像から読み取る」アイテムがクリックされた際の処理
        /// </summary>
        private void Read_from_clipboard_image_item_Click(object sender, EventArgs e)
        {
            // クリップボードから画像を取得してその画像から文字を読み取る
            IDataObject cb = Clipboard.GetDataObject();
            System.Drawing.Bitmap image = (System.Drawing.Bitmap)cb.GetData(typeof(System.Drawing.Bitmap));
            if (image != null)
            {
                Functions.Result_Show(image, data_source: 2);
            }
        }

        /// <summary>
        /// タスクトレイアイコンの「画像ファイルから読み取る」アイテムがクリックされた際の処理
        /// </summary>
        private void Read_from_image_file_item_Click(object sender, EventArgs e)
        {
            // ユーザーが選択した画像ファイルを読み込んでその画像から文字を読み取る
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Filter = (string)FindResource("file_dialog/load_image_filter"),
                FilterIndex = -1,
                Title = (string)FindResource("file_dialog/load_image_title")
            };
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.Drawing.Bitmap image;
                try
                {
                    image = new System.Drawing.Bitmap(ofd.FileName);
                }
                catch (ArgumentException)
                {
                    // 読み込んだファイルが画像ファイルでない場合
                    return;
                }
                Functions.Result_Show(image, data_source: 3);
            }
        }

        /// <summary>
        /// タスクトレイアイコンの「設定」アイテムがクリックされた際の処理
        /// </summary>
        private void Settings_item_Click(object sender, EventArgs e)
        {
            Functions.Settings_Show();
        }

        /// <summary>
        /// タスクトレイアイコンの「この製品について」アイテムがクリックされた際の処理
        /// </summary>
        private void Product_information_item_Click(object sender, EventArgs e)
        {
            Functions.ProductInformation_Show();
        }

        /// <summary>
        /// タスクトレイアイコンの「終了」アイテムがクリックされた際の処理
        /// </summary>
        private void Exit_item_Click(object sender, EventArgs e)
        {
            // アプリケーションを終了する
            Application.Current.Shutdown();
        }
    }
}
