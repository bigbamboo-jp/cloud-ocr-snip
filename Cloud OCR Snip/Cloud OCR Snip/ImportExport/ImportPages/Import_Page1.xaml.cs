using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// インポート ページ1
    /// 機能：設定持ち出しファイルの読み込み
    /// （Import_Page1.xaml の相互作用ロジック）
    /// </summary>
    public partial class Import_Page1 : Page
    {
        public Import_Page1()
        {
            InitializeComponent();

            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("import/window_title") + " - " + executing_assembly.GetName().Name;
            product_name_label.Content = executing_assembly.GetName().Name;
        }

        /// <summary>
        /// 参照ボタンがクリックされた際の処理
        /// </summary>
        private void Setting_file_browse_button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog
            {
                InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Filter = (string)FindResource("file_dialog/load_setting_file_filter"),
                FilterIndex = -1,
                Title = (string)FindResource("file_dialog/load_setting_file_title")
            };
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                setting_file_path_text_box.Text = ofd.FileName;
            }
        }

        /// <summary>
        /// ファイルパスのテキストボックスのデータが変更された際の処理
        /// </summary>
        private void Setting_file_path_text_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            next_button.IsEnabled = string.IsNullOrWhiteSpace(setting_file_path_text_box.Text) == false;
        }

        /// <summary>
        /// 次へボタンがクリックされた際の処理
        /// </summary>
        private void Next_button_Click(object sender, RoutedEventArgs e)
        {
            string setting_file_path = setting_file_path_text_box.Text;
            // ファイルが存在するか確認する
            if (File.Exists(setting_file_path) == false)
            {
                MessageBox.Show((string)FindResource("file_dialog/file_not_found_error_message"), (string)FindResource("file_dialog/file_not_found_error_title"));
                return;
            }
            // 指定されたファイルパスが適切かどうかチェックする
            if (Path.GetExtension(setting_file_path) != Functions.SETTING_TAKEOVER_FILE_EXTENSION)
            {
                MessageBox.Show((string)FindResource("file_dialog/file_type_error_message"), (string)FindResource("file_dialog/file_type_error_title"));
                return;
            }
            // ファイルを読み込んでデシリアライズする
            string file_data = File.ReadAllText(setting_file_path);
            SettingFile setting_file_data = JsonConvert.DeserializeObject<SettingFile>(file_data);

            // 読み込んだファイルに互換性があるか確認する
            if (setting_file_data.FileType != Functions.SETTING_TAKEOVER_FILE_FILE_TYPE)
            {
                MessageBox.Show((string)FindResource("file_dialog/data_compatibility_error_message"), (string)FindResource("file_dialog/data_compatibility_error_title"));
                return;
            }

            if (setting_file_data.Encrypted == true)
            {
                // 次のページ（パスワード入力）に進む
                NavigationService.Navigate(new Import_Page2(setting_file_data));
            }
            else
            {
                // 設定データを取り出してデシリアライズする
                Dictionary<string, object> setting_data = (Dictionary<string, object>)Functions.JsonConvert_DeserializeObject(JToken.Parse(setting_file_data.Data));

                // 読み込んだ設定データに互換性があるか確認する
                if ((string)setting_data["file_type"] != Functions.CONFIG_FILE_FILE_TYPE)
                {
                    MessageBox.Show((string)FindResource("file_dialog/data_compatibility_error_message"), (string)FindResource("file_dialog/data_compatibility_error_title"));
                    return;
                }

                // 次のページ（インポートする設定の選択）に進む
                NavigationService.Navigate(new Import_Page3(setting_data));
            }
        }

        /// <summary>
        /// キャンセルボタンがクリックされた際の処理
        /// </summary>
        private void Cancel_button_Click(object sender, RoutedEventArgs e)
        {
            Window parent_window = Window.GetWindow(this);
            parent_window.Close();
        }
    }
}
