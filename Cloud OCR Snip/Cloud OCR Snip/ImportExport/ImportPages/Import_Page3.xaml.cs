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
    /// インポート ページ3
    /// 機能：インポートする設定の選択
    /// （Import_Page3.xaml の相互作用ロジック）
    /// </summary>
    public partial class Import_Page3 : Page
    {
        public Import_Page3(Dictionary<string, object> setting_data)
        {
            InitializeComponent();

            // 設定データ（アプリ設定）からユーザー設定を取り出す
            setting = setting_data;
            Dictionary<string, object> user_settings = setting;
            List<string> splitted_user_setting_section_keys = Functions.USER_SETTING_SECTION_KEY.Split(":").ToList();
            foreach (string key in splitted_user_setting_section_keys)
            {
                user_settings = (Dictionary<string, object>)user_settings[key];
            }

            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("import/window_title") + " - " + executing_assembly.GetName().Name;
            product_name_label.Content = executing_assembly.GetName().Name;
            // 引き継げない設定のチェックボックスを無効化する
            if (user_settings.ContainsKey("hotkey1_mainkey") == false)
            {
                inherit_setting_selection_checkbox_application_setting.IsChecked = false;
                inherit_setting_selection_checkbox_application_setting.IsEnabled = false;
            }
            if (user_settings.ContainsKey("transcription_service_credential") == false)
            {
                inherit_setting_selection_checkbox_transcription_service_credential.IsChecked = false;
                inherit_setting_selection_checkbox_transcription_service_credential.IsEnabled = false;
            }
            if (user_settings.ContainsKey("transcription_service_settings") == false)
            {
                inherit_setting_selection_checkbox_transcription_service_settings.IsChecked = false;
                inherit_setting_selection_checkbox_transcription_service_settings.IsEnabled = false;
            }
        }

        Dictionary<string, object> setting; // 設定データ

        /// <summary>
        /// 設定項目のチェックボックスがクリックされた際の処理
        /// </summary>
        private void Inherit_setting_selection_checkbox_Click(object sender, RoutedEventArgs e)
        {
            next_button.IsEnabled = inherit_setting_selection_checkbox_application_setting.IsChecked == true || inherit_setting_selection_checkbox_transcription_service_credential.IsChecked == true || inherit_setting_selection_checkbox_transcription_service_settings.IsChecked == true;
        }

        /// <summary>
        /// 次へボタンがクリックされた際の処理
        /// </summary>
        private void Next_button_Click(object sender, RoutedEventArgs e)
        {
            // 設定データ（アプリ設定）からユーザー設定を取り出す
            Dictionary<string, object> user_settings = setting;
            List<string> splitted_user_setting_section_keys = Functions.USER_SETTING_SECTION_KEY.Split(":").ToList();
            foreach (string key in splitted_user_setting_section_keys)
            {
                user_settings = (Dictionary<string, object>)user_settings[key];
            }
            // 選択された設定項目のデータを設定データから引き継ぐ
            Dictionary<string, object> settings = Functions.GetUserSettings();
            // 文字読み取りサービスの種類
            if ((string)user_settings["transcription_service"] == string.Empty)
            {
                settings["transcription_service"] = string.Empty;
            }
            else
            {
                settings["transcription_service"] = Convert.ToBase64String(Functions.Protect(Encoding.UTF8.GetBytes((string)user_settings["transcription_service"])));
            }
            // アプリケーション設定全般
            if (inherit_setting_selection_checkbox_application_setting.IsChecked == true)
            {
                // この処理で引き継がない設定のリスト
                List<string> exclusion_list = new List<string>
                {
                    "search_service_url",
                    "transcription_service",
                    "transcription_service_credential",
                    "transcription_service_settings"
                };
                foreach (string key in settings.Keys)
                {
                    if (user_settings.ContainsKey(key) == true && exclusion_list.Contains(key) == false)
                    {
                        settings[key] = user_settings[key];
                    }
                }
                // 検索サービスのURLは暗号化した上で保存する
                if ((string)user_settings["search_service_url"] == string.Empty)
                {
                    settings["search_service_url"] = string.Empty;
                }
                else
                {
                    settings["search_service_url"] = Convert.ToBase64String(Functions.Protect(Encoding.UTF8.GetBytes((string)user_settings["search_service_url"])));
                }
            }
            Functions.SetUserSettings(settings);
            // 文字読み取りサービスの認証情報
            if (inherit_setting_selection_checkbox_transcription_service_credential.IsChecked == true)
            {
                // #OK# → エクスポート可能にする、#NG# → エクスポート不可にする
                Functions.SetTranscriptionServiceCredential(((string)user_settings["transcription_service_credential"]).Substring("#OO#".Length), ((string)user_settings["transcription_service_credential"]).Substring(0, "#OO#".Length) == "#OK#");
            }
            // 文字読み取りサービスのオプション設定
            if (inherit_setting_selection_checkbox_transcription_service_settings.IsChecked == true)
            {
                TranscriptionService.Service transcription_service = Functions.GetTranscriptionService((string)user_settings["transcription_service"]);
                // オプション設定データを各サービスに渡す（データの処理は各サービスによって異なる）
                transcription_service.Settings = (Dictionary<string, object>)user_settings["transcription_service_settings"];
            }
            // 次のページに進む
            NavigationService.Navigate(new Import_Page4());
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
