using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// 結果ウィンドウ
    /// 機能：文字読み取り結果の表示及び編集
    /// （Result.xaml の相互作用ロジック）
    /// </summary>
    public partial class Result : Window
    {
        public Result(string result_text, int data_source = 0)
        {
            InitializeComponent();

            // 文字読み取り結果がnullの場合にエラーモードにする
            if (result_text == null)
            {
                result_text = "";
                data_source = -1;
            }

            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            Title = (string)FindResource("result/window_title") + " - " + executing_assembly.GetName().Name;
            data_selection_setting_checkbox.IsChecked = (bool)Functions.GetUserSettings()["result_data_use_default_setting"];

            data_acquisition_method = data_source;
            retry_button.IsEnabled = data_acquisition_method != 0;
            // 文字読み取り結果の先頭と末尾から改行を削除する
            result_text = result_text.Trim('\r', '\n');
            result_textbox.Tag = result_text;
            if (result_text == string.Empty)
            {
                // 文字読み取り結果が空文字列の場合
                result_textbox.Text = (string)FindResource("result/no_data_message");
                result_textbox.IsEnabled = false;
            }
            else
            {
                // 文字読み取り結果が空文字列でない場合
                result_textbox.Text = result_text;
            }
        }

        private int data_acquisition_method; // 文字読み取りの方法（画像のソース）

        /// <summary>
        /// ウィンドウが読み込まれた際の処理
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (data_acquisition_method == -1)
            {
                // エラーモードになっている場合
                Close();
                return;
            }
            // ウィンドウを前面に出す
            Activate();
        }

        /// <summary>
        /// 再試行ボタンがクリックされた際の処理
        /// </summary>
        private async void Retry_button_Click(object sender, RoutedEventArgs e)
        {
            // 文字読み取りの方法に応じて再試行する
            switch (data_acquisition_method)
            {
                case 1:
                    // スクリーンショットを撮影後その画像から文字を読み取る
                    if (Functions.displaying_shoot == false)
                    {
                        WindowState previous_WindowState = WindowState;
                        WindowState = WindowState.Minimized;
                        // ウィンドウの最小化が完了するまで待機する
                        await Task.Delay(200);
                        if (await Functions.DetectScreenshotText() == true)
                        {
                            WindowState = previous_WindowState;
                        }
                        else
                        {
                            Close();
                        }
                    }
                    break;
                case 2:
                    // クリップボードから画像を取得してその画像から文字を読み取る
                    IDataObject cb = Clipboard.GetDataObject();
                    System.Drawing.Bitmap clipboard_image = (System.Drawing.Bitmap)cb.GetData(typeof(System.Drawing.Bitmap));
                    if (clipboard_image == null)
                    {
                        // クリップボードから画像を取得できなかった場合
                        MessageBox.Show((string)FindResource("result/clipboard_image_acquisition_error_message"), (string)FindResource("result/clipboard_image_acquisition_error_title"));
                    }
                    else
                    {
                        // クリップボードから画像を取得できた場合
                        Close();
                        await Functions.Result_Show(clipboard_image, data_source: 2);
                    }
                    break;
                case 3:
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
                        System.Drawing.Bitmap file_image;
                        try
                        {
                            file_image = new System.Drawing.Bitmap(ofd.FileName);
                        }
                        catch (ArgumentException)
                        {
                            // 読み込んだファイルが画像ファイルでない場合
                            return;
                        }
                        Close();
                        await Functions.Result_Show(file_image, data_source: 3);
                    }
                    break;
            }
        }

        /// <summary>
        /// 改行削除ボタンがクリックされた際の処理
        /// </summary>
        private void Remove_newlines_button_Click(object sender, RoutedEventArgs e)
        {
            // 結果テキストボックスで選択中のテキストから改行を削除する
            if (string.IsNullOrEmpty(result_textbox.SelectedText) == false)
            {
                result_textbox.SelectedText = result_textbox.SelectedText.Replace("\r", "").Replace("\n", "");
                result_textbox.SelectionLength = 0;
            }
        }

        /// <summary>
        /// コピーボタンがクリックされた際の処理
        /// </summary>
        private async void Copy_button_Click(object sender, RoutedEventArgs e)
        {
            // データ選択チェックボックスの状態に応じて使用するデータを変える
            string text;
            if (data_selection_setting_checkbox.IsChecked == true && (string)result_textbox.Tag != string.Empty)
            {
                // 編集されたデータ
                text = result_textbox.Text;
            }
            else
            {
                // 元のデータ
                text = (string)result_textbox.Tag;
            }
            try
            {
                Clipboard.SetText(text);
            }
            catch (COMException)
            {
                MessageBox.Show((string)FindResource("result/clipboard_copy_error_message"), (string)FindResource("result/clipboard_copy_error_title"));
            }
            // 一定時間ボタンのテキストを変える
            copy_button.Content = FindResource("result/command_bar_copy_button_text_copy_complete");
            int random_number = new Random().Next();
            ((Button)sender).Tag = random_number;
            await Task.Delay(2000);
            if ((int)((Button)sender).Tag == random_number)
            {
                // 一定時間内にもう一度コピーが行われなかった場合
                copy_button.Content = FindResource("result/command_bar_copy_button_text");
            }
        }

        /// <summary>
        /// 保存ボタンがクリックされた際の処理
        /// </summary>
        private void Save_button_Click(object sender, RoutedEventArgs e)
        {
            // データ選択チェックボックスに状態に応じて使用するデータを変える
            string text;
            if (data_selection_setting_checkbox.IsChecked == true && (string)result_textbox.Tag != string.Empty)
            {
                // 編集されたデータ
                text = result_textbox.Text;
            }
            else
            {
                // 元のデータ
                text = (string)result_textbox.Tag;
            }
            // ユーザーが指定したファイルに書き込む
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog
            {
                FileName = (string)FindResource("result/save_result_default_file_name"),
                Filter = (string)FindResource("result/save_result_filter"),
                FilterIndex = 1,
                Title = (string)FindResource("result/save_result_title")
            };
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, text);
            }
        }

        /// <summary>
        /// ウェブ検索ボタンがクリックされた際の処理
        /// </summary>
        private void Web_search_button_Click(object sender, RoutedEventArgs e)
        {
            // データ選択チェックボックスに状態に応じて使用するデータを変える
            string text, search_service_url;
            if (data_selection_setting_checkbox.IsChecked == true && (string)result_textbox.Tag != string.Empty)
            {
                // 編集されたデータ
                text = result_textbox.Text;
            }
            else
            {
                // 元のデータ
                text = (string)result_textbox.Tag;
            }
            // 読み取った文字を設定されている検索サービスで検索する
            byte[] search_service_url_bytes = Functions.Unprotect(Convert.FromBase64String((string)Functions.GetUserSettings()["search_service_url"]));
            if (search_service_url_bytes == null)
            {
                // 設定データが破損している場合
                search_service_url = "https://_/search?q={0}";
            }
            else
            {
                search_service_url = Encoding.UTF8.GetString(search_service_url_bytes);
            }
            string url = string.Format(search_service_url, WebUtility.UrlEncode(text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", " ")));
            Functions.AccessWebsite(url);
        }

        /// <summary>
        /// 結果テキストボックスで選択中のテキストが変更された際の処理
        /// </summary>
        private void Result_textbox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            remove_newlines_button.IsEnabled = string.IsNullOrEmpty(result_textbox.SelectedText) == false;
        }
    }
}
