using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// エクスポート ページ1
    /// 機能：エクスポートする設定の選択
    /// （Export_Page1.xaml の相互作用ロジック）
    /// </summary>
    public partial class Export_Page1 : Page
    {
        public Export_Page1()
        {
            InitializeComponent();

            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("export/window_title") + " - " + executing_assembly.GetName().Name;
            product_name_label.Content = executing_assembly.GetName().Name;
            if (Functions.ExportTranscriptionServiceCredential() == string.Empty || Functions.ExportTranscriptionServiceCredential() == null)
            {
                // 文字読み取りサービスの認証情報をエクスポートできない場合
                inherit_setting_selection_checkbox_transcription_service_credential.IsChecked = false;
                inherit_setting_selection_checkbox_transcription_service_credential.IsEnabled = false;
                setting_inheritance_option_checkbox_transcription_service_credential.IsChecked = false;
                setting_inheritance_option_checkbox_transcription_service_credential.IsEnabled = false;
            }
            if (Functions.ExportTranscriptionServiceCredential() == string.Empty)
            {
                // 文字読み取りサービスのオプション設定のデータが無い場合
                inherit_setting_selection_checkbox_transcription_service_settings.IsChecked = false;
                inherit_setting_selection_checkbox_transcription_service_settings.IsEnabled = false;
            }
        }

        /// <summary>
        /// 設定項目のチェックボックスがクリックされた際の処理
        /// </summary>
        private void Inherit_setting_selection_checkbox_Click(object sender, RoutedEventArgs e)
        {
            if (inherit_setting_selection_checkbox_transcription_service_credential.IsChecked == true)
            {
                setting_inheritance_option_checkbox_transcription_service_credential.IsEnabled = true;
            }
            else
            {
                setting_inheritance_option_checkbox_transcription_service_credential.IsChecked = false;
                setting_inheritance_option_checkbox_transcription_service_credential.IsEnabled = false;
            }
            next_button.IsEnabled = inherit_setting_selection_checkbox_application_setting.IsChecked == true || inherit_setting_selection_checkbox_transcription_service_credential.IsChecked == true || inherit_setting_selection_checkbox_transcription_service_settings.IsChecked == true;
        }

        /// <summary>
        /// 次へボタンがクリックされた際の処理
        /// </summary>
        private void Next_button_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, object> settings = Functions.GetUserSettings();
            Dictionary<string, object> user_settings;
            // ユーザー設定のデータの作成
            if (inherit_setting_selection_checkbox_application_setting.IsChecked == true)
            {
                // アプリケーションの設定のコピー
                user_settings = new Dictionary<string, object>(settings);
                byte[] search_service_url_bytes = Functions.Unprotect(Convert.FromBase64String((string)user_settings["search_service_url"]));
                if (search_service_url_bytes == null)
                {
                    user_settings["search_service_url"] = string.Empty;
                }
                else
                {
                    user_settings["search_service_url"] = Encoding.UTF8.GetString(search_service_url_bytes);
                }
                user_settings.Remove("transcription_service");
                user_settings.Remove("transcription_service_credential");
                user_settings.Remove("transcription_service_settings");
            }
            else
            {
                user_settings = new Dictionary<string, object>();
            }
            // 使用中の文字読み取りサービスをコピー
            byte[] transcription_service_bytes = Functions.Unprotect(Convert.FromBase64String((string)settings["transcription_service"]));
            if (transcription_service_bytes == null)
            {
                user_settings.Add("transcription_service", string.Empty);
            }
            else
            {
                user_settings.Add("transcription_service", Encoding.UTF8.GetString(transcription_service_bytes));
            }
            if (inherit_setting_selection_checkbox_transcription_service_credential.IsChecked == true)
            {
                // 文字読み取りサービスの認証情報をコピー
                string prefix;
                if (setting_inheritance_option_checkbox_transcription_service_credential.IsChecked == true)
                {
                    // インポート先でのエクスポートが許可される場合
                    prefix = "#OK#";
                }
                else
                {
                    // インポート先でのエクスポートが許可されない場合
                    prefix = "#NG#";
                }
                user_settings.Add("transcription_service_credential", prefix + Functions.ExportTranscriptionServiceCredential());
            }
            if (inherit_setting_selection_checkbox_transcription_service_settings.IsChecked == true)
            {
                // 文字読み取りサービスのオプション設定をコピー
                TranscriptionService.Service transcription_service = Functions.GetTranscriptionService((string)user_settings["transcription_service"]);
                user_settings.Add("transcription_service_settings", transcription_service.Settings);
            }
            // ユーザー設定をアプリ設定の中に入れる
            Dictionary<string, object> app_settings = Functions.GetAppSettings();
            Dictionary<string, object> user_settings_storage = app_settings;
            List<string> splitted_user_setting_section_keys = Functions.USER_SETTING_SECTION_KEY.Split(":").ToList();
            foreach (string key in splitted_user_setting_section_keys.GetRange(0, splitted_user_setting_section_keys.Count - 1))
            {
                user_settings_storage = (Dictionary<string, object>)user_settings_storage[key];
            }
            user_settings_storage[splitted_user_setting_section_keys[splitted_user_setting_section_keys.Count - 1]] = user_settings;
            // 次のページに進む
            NavigationService.Navigate(new Export_Page2(app_settings));
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
