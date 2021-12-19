using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// エクスポート ページ2
    /// 機能：エクスポートデータの暗号化
    /// （Export_Page2.xaml の相互作用ロジック）
    /// </summary>
    public partial class Export_Page2 : Page
    {
        public Export_Page2(Dictionary<string, object> settings)
        {
            InitializeComponent();

            exporting_settings = settings;
            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("export/window_title") + " - " + executing_assembly.GetName().Name;
            product_name_label.Content = executing_assembly.GetName().Name;
        }

        Dictionary<string, object> exporting_settings; // エクスポートデータ

        /// <summary>
        /// 暗号化オンのラジオボタンがチェックされた際の処理
        /// </summary>
        private void Encryption_setting_radio_button_enable_Checked(object sender, RoutedEventArgs e)
        {
            if (file_pass_password_box != null)
            {
                file_pass_password_box.IsEnabled = true;
                next_button.IsEnabled = file_pass_password_box.Password.Length >= 12;
            }
        }

        /// <summary>
        /// 暗号化オフのラジオボタンがチェックされた際の処理
        /// </summary>
        private void Encryption_setting_radio_button_disable_Checked(object sender, RoutedEventArgs e)
        {
            if (file_pass_password_box != null)
            {
                file_pass_password_box.IsEnabled = false;
                next_button.IsEnabled = true;
            }
        }

        /// <summary>
        /// パスワードボックスのデータが変更された際の処理
        /// </summary>
        private void File_pass_password_box_PasswordChanged(object sender, RoutedEventArgs e)
        {
            next_button.IsEnabled = encryption_setting_radio_button_disable.IsChecked == true || file_pass_password_box.Password.Length >= 12;
        }

        /// <summary>
        /// 次へボタンがクリックされた際の処理
        /// </summary>
        private void Next_button_Click(object sender, RoutedEventArgs e)
        {
            // エクスポートする設定をJSONにシリアライズする
            string setting_data = JsonConvert.SerializeObject(exporting_settings, Formatting.Indented);
            if (encryption_setting_radio_button_enable.IsChecked == true)
            {

                // 暗号化が有効になっている場合、エクスポートデータをパスワードで暗号化する
                setting_data = Encryption.AESThenHMAC.SimpleEncryptWithPassword(setting_data, file_pass_password_box.Password);

            }
            // 設定持ち出しファイルのデータの作成
            SettingFile setting_file_data = new SettingFile
            {
                FileType = Functions.SETTING_TAKEOVER_FILE_FILE_TYPE,
                Encrypted = encryption_setting_radio_button_enable.IsChecked == true,
                Data = setting_data
            };
            // 次のページに進む
            NavigationService.Navigate(new Export_Page3(setting_file_data));
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
