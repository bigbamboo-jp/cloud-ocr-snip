using Markdig.Wpf;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace TranscriptionService
{
    /// <summary>
    /// Google Cloud Vision API 初期設定ページ3
    /// 機能：サービスアカウントの設定の変更
    /// （Google_Cloud_Vision_API_InitialSetting_Page3.xaml の相互作用ロジック）
    /// </summary>
    public partial class Google_Cloud_Vision_API_InitialSetting_Page3 : Page
    {
        public Google_Cloud_Vision_API_InitialSetting_Page3(bool initial_setting, string service_account_key)
        {
            InitializeComponent();

            initialization = initial_setting;
            if (initialization == false)
            {
                // 初期設定でない場合、オプション設定ボタンを非表示にする
                option_setting_button.Visibility = Visibility.Hidden;
            }
            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("google_cloud_vision_api/initial_setting/window_title") + " - " + executing_assembly.GetName().Name;
            service_account_key_data = service_account_key;
            // リッチテキストボックスにコメントを表示する
            FlowDocument instruction_comment = Markdown.ToFlowDocument((string)FindResource("google_cloud_vision_api/initial_setting_3/instruction_comment"));
            instruction_comment.FontSize = 20.0;
            instruction_comment_rich_text_box.Document = instruction_comment;
            // リッチテキストボックスにキーのエクスポート設定の説明文を表示する
            FlowDocument key_export_setting_description = Markdown.ToFlowDocument((string)FindResource("google_cloud_vision_api/initial_setting_3/key_export_setting_description"));
            key_export_setting_description.FontSize = 20.0;
            key_export_setting_description_rich_text_box.Document = key_export_setting_description;
        }

        private bool initialization; // 初期設定かどうかのフラグ

        private string service_account_key_data; // サービスアカウントキー

        /// <summary>
        /// 閉じるボタンがクリックされた際の処理
        /// </summary>
        private void Close_button_Click(object sender, RoutedEventArgs e)
        {
            if (key_export_setting_checkbox.IsChecked == true)
            {
                // エクスポートが許可された場合、その設定でサービスアカウントキーを再度保存する
                Functions.SetTranscriptionServiceCredential(service_account_key_data, true);
            }
            Unloaded -= Page_Unloaded;
            service_account_key_data = string.Empty;
            Window parent_window = Window.GetWindow(this);
            parent_window.Close();
        }

        /// <summary>
        /// オプション設定ボタンがクリックされた際の処理
        /// </summary>
        private void Option_setting_button_Click(object sender, RoutedEventArgs e)
        {
            if (key_export_setting_checkbox.IsChecked == true)
            {
                // エクスポートが許可された場合、その設定でサービスアカウントキーを再度保存する
                Functions.SetTranscriptionServiceCredential(service_account_key_data, true);
            }
            Unloaded -= Page_Unloaded;
            service_account_key_data = string.Empty;
            // 次のページ（オプション設定）に進む
            NavigationService.Navigate(new Google_Cloud_Vision_API_InitialSetting_Page4(initialization));
        }

        /// <summary>
        /// ページの読み込みが解除された（＝ウィンドウが閉じられた）際の処理
        /// </summary>
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (key_export_setting_checkbox.IsChecked == true)
            {
                // エクスポートが許可された場合、その設定でサービスアカウントキーを再度保存する
                Functions.SetTranscriptionServiceCredential(service_account_key_data, true);
                // 保存された設定を通知する
                MessageBox.Show((string)FindResource("google_cloud_vision_api/initial_setting_3/key_export_setting_notification_message_allow"), (string)FindResource("google_cloud_vision_api/initial_setting_3/key_export_setting_notification_title_allow"));
            }
            else
            {
                // 保存された設定を通知する
                MessageBox.Show((string)FindResource("google_cloud_vision_api/initial_setting_3/key_export_setting_notification_message_disallow"), (string)FindResource("google_cloud_vision_api/initial_setting_3/key_export_setting_notification_title_disallow"));
            }
            service_account_key_data = string.Empty;
        }
    }
}
