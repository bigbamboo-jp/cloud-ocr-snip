using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// エクスポート ページ3
    /// 機能：設定持ち出しファイルの情報の入力
    /// （Export_Page3.xaml の相互作用ロジック）
    /// </summary>
    public partial class Export_Page3 : Page
    {
        public Export_Page3(SettingFile setting_file_data)
        {
            InitializeComponent();

            setting_file = setting_file_data;
            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("export/window_title") + " - " + executing_assembly.GetName().Name;
            product_name_label.Content = executing_assembly.GetName().Name;
        }

        SettingFile setting_file; // 設定持ち出しファイルのデータ

        /// <summary>
        /// 参照ボタンがクリックされた際の処理
        /// </summary>
        private void Setting_file_browse_button_Click(object sender, RoutedEventArgs e)
        {
            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog
            {
                FileName = string.Format((string)FindResource("file_dialog/save_setting_file_default_file_name"), executing_assembly.GetName().Name),
                Filter = (string)FindResource("file_dialog/save_setting_file_filter"),
                FilterIndex = 1,
                Title = (string)FindResource("file_dialog/save_setting_file_title")
            };
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                setting_file_path_text_box.Text = sfd.FileName;
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
            // 指定されたファイルパスが適切かどうかチェックする
            if (Path.GetExtension(setting_file_path) != Functions.SETTING_TAKEOVER_FILE_EXTENSION)
            {
                MessageBox.Show((string)FindResource("file_dialog/file_type_error_message"), (string)FindResource("file_dialog/file_type_error_title"));
                return;
            }
            // 設定持ち出しファイルのデータをJSONにシリアライズして書き込む
            string file_data = JsonConvert.SerializeObject(setting_file, Formatting.Indented);
            File.WriteAllText(setting_file_path, file_data);
            // 次のページに進む
            NavigationService.Navigate(new Export_Page4());
        }

        /// <summary>
        /// 戻るボタンがクリックされた際の処理
        /// </summary>
        private void Back_button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
