using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// インポート ページ2
    /// 機能：設定データの復号
    /// （Import_Page2.xaml の相互作用ロジック）
    /// </summary>
    public partial class Import_Page2 : Page
    {
        public Import_Page2(SettingFile setting_file_data)
        {
            InitializeComponent();

            file_data = setting_file_data;
            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("import/window_title") + " - " + executing_assembly.GetName().Name;
            product_name_label.Content = executing_assembly.GetName().Name;
        }

        SettingFile file_data; // 暗号化された設定データ

        /// <summary>
        /// パスワードボックスのデータが変更された際の処理
        /// </summary>
        private void File_pass_password_box_PasswordChanged(object sender, RoutedEventArgs e)
        {
            next_button.IsEnabled = file_pass_password_box.Password.Length >= 12;
        }

        /// <summary>
        /// 次へボタンがクリックされた際の処理
        /// </summary>
        private void Next_button_Click(object sender, RoutedEventArgs e)
        {

            // 設定データをパスワードで復号する
            string decrypted_setting_file_data = Encryption.AESThenHMAC.SimpleDecryptWithPassword(file_data.Data, file_pass_password_box.Password);

            // 正常に復号できたか確認する
            if (decrypted_setting_file_data == null)
            {
                MessageBox.Show((string)FindResource("import_2/password_error_error_message"), (string)FindResource("import_2/password_error_error_title"));
                file_pass_password_box.Password = string.Empty;
                return;
            }
            // 復号した設定データをデシリアライズする
            Dictionary<string, object> setting_data = (Dictionary<string, object>)Functions.JsonConvert_DeserializeObject(JToken.Parse(decrypted_setting_file_data));

            // 読み込んだ設定データに互換性があるか確認する
            if ((string)setting_data["file_type"] != Functions.CONFIG_FILE_FILE_TYPE)
            {
                MessageBox.Show((string)FindResource("file_dialog/data_compatibility_error_message"), (string)FindResource("file_dialog/data_compatibility_error_title"));
                return;
            }

            // 次のページ（インポートする設定の選択）に進む
            NavigationService.Navigate(new Import_Page3(setting_data));
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
