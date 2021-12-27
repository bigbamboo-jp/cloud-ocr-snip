using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// 設定ウィンドウ
    /// 機能：ユーザー設定の変更、個別の設定メニューへのリンク
    /// （Settings.xaml の相互作用ロジック）
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();

            // 文字読み取り設定タブを静的にする
            static_transcription_settings_tabitem = transcription_settings_tabitem;
            // デザイナーでの表示と実際の表示のずれを直す
            double title_bar_height_in_designer = 15.96;
            Height += System.Windows.Forms.SystemInformation.CaptionHeight - title_bar_height_in_designer;
            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            Title = (string)FindResource("settings/window_title") + " - " + executing_assembly.GetName().Name;
            product_name_label.Content = executing_assembly.GetName().Name;
            language_combobox.ItemsSource = Functions.GetAvailableLanguages();
            transcription_service_combobox.ItemsSource = Functions.transcription_services;
            // ショートカットキーとして利用できるキーを取得する
            List<Key> hotkey_mainkeys = new List<Key>();
            foreach (Key mainkey in Enum.GetValues(typeof(Key)))
            {
                switch (mainkey)
                {
                    // リストから除外するキー（修飾キーは別で設定するため）
                    case Key.None:
                    case Key.RightAlt:
                    case Key.LeftAlt:
                    case Key.RightCtrl:
                    case Key.LeftCtrl:
                    case Key.RightShift:
                    case Key.LeftShift:
                    case Key.RWin:
                    case Key.LWin:
                        break;
                    default:
                        hotkey_mainkeys.Add(mainkey);
                        break;
                }
            }
            hk1_hotkey_mainkey_combobox.ItemsSource = hotkey_mainkeys.Distinct();
            hk2_hotkey_mainkey_combobox.ItemsSource = hotkey_mainkeys.Distinct();
            Load_settings();
        }

        public static TabItem static_transcription_settings_tabitem; // 文字読み取り設定タブ

        /// <summary>
        /// ウィンドウに表示されている状態で設定を更新するメソッド
        /// </summary>
        private void Save_settings()
        {
            Dictionary<string, object> settings = Functions.GetUserSettings();
            // ショートカットキー
            for (int i = 1; i <= 2; i++)
            {
                // 修飾キー
                int hotkey_modifierkeys = 0;
                CheckBox hotkey_modifierkey_checkbox_win = FindName("hk" + i.ToString() + "_hotkey_modifierkey_checkbox_win") as CheckBox;
                if ((bool)hotkey_modifierkey_checkbox_win.IsChecked)
                {
                    hotkey_modifierkeys += (int)ModifierKeys.Windows;
                }
                CheckBox hotkey_modifierkey_checkbox_shift = FindName("hk" + i.ToString() + "_hotkey_modifierkey_checkbox_shift") as CheckBox;
                if ((bool)hotkey_modifierkey_checkbox_shift.IsChecked)
                {
                    hotkey_modifierkeys += (int)ModifierKeys.Shift;
                }
                CheckBox hotkey_modifierkey_checkbox_ctrl = FindName("hk" + i.ToString() + "_hotkey_modifierkey_checkbox_ctrl") as CheckBox;
                if ((bool)hotkey_modifierkey_checkbox_ctrl.IsChecked)
                {
                    hotkey_modifierkeys += (int)ModifierKeys.Control;
                }
                CheckBox hotkey_modifierkey_checkbox_alt = FindName("hk" + i.ToString() + "_hotkey_modifierkey_checkbox_alt") as CheckBox;
                if ((bool)hotkey_modifierkey_checkbox_alt.IsChecked)
                {
                    hotkey_modifierkeys += (int)ModifierKeys.Alt;
                }
                settings["hotkey" + i.ToString() + "_modifierkey"] = hotkey_modifierkeys;
                // メインキー
                ComboBox hotkey_mainkey_combobox = FindName("hk" + i.ToString() + "_hotkey_mainkey_combobox") as ComboBox;
                int hotkey_mainkey = (int)(Key)hotkey_mainkey_combobox.SelectedItem;
                CheckBox hotkey_enable_checkbox = FindName("hk" + i.ToString() + "_hotkey_enable_checkbox") as CheckBox;
                if (hotkey_enable_checkbox.IsChecked == true)
                {
                    // 有効に設定されている場合
                    settings["hotkey" + i.ToString() + "_mainkey"] = hotkey_mainkey;
                }
                else
                {
                    // 無効に設定されている場合
                    settings["hotkey" + i.ToString() + "_mainkey"] = hotkey_mainkey * -1;
                }
            }
            // 言語
            settings["language"] = ((KeyValuePair<string, string>)language_combobox.SelectedItem).Key;
            // 読み取り結果を変更した場合に使用するデータの設定（デフォルト設定）
            settings["result_data_use_default_setting"] = data_selection_setting_checkbox.IsChecked;
            // 画面撮影の方法
            settings["live_capture_mode"] = screen_shooting_live_capture_method_radio_button.IsChecked;
            // ウェブ検索サービスのURL
            if (web_search_service_url_text_box.Text == string.Empty)
            {
                settings["search_service_url"] = string.Empty;
            }
            else
            {
                // 暗号化して保存
                settings["search_service_url"] = Convert.ToBase64String(Functions.Protect(Encoding.UTF8.GetBytes(web_search_service_url_text_box.Text)));
            }
            Functions.SetUserSettings(settings);
        }

        /// <summary>
        /// 保存されている設定をウィンドウに表示するメソッド
        /// </summary>
        private void Load_settings()
        {
            Dictionary<string, object> settings = Functions.GetUserSettings();
            // ショートカットキー
            for (int i = 1; i <= 2; i++)
            {
                // 修飾キー
                int hotkey_modifierkeys = (int)settings["hotkey" + i.ToString() + "_modifierkey"];
                CheckBox hotkey_modifierkey_checkbox_win = FindName("hk" + i.ToString() + "_hotkey_modifierkey_checkbox_win") as CheckBox;
                if (hotkey_modifierkeys >= (int)ModifierKeys.Windows)
                {
                    hotkey_modifierkey_checkbox_win.IsChecked = true;
                    hotkey_modifierkeys -= (int)ModifierKeys.Windows;
                }
                else
                {
                    hotkey_modifierkey_checkbox_win.IsChecked = false;
                }
                CheckBox hotkey_modifierkey_checkbox_shift = FindName("hk" + i.ToString() + "_hotkey_modifierkey_checkbox_shift") as CheckBox;
                if (hotkey_modifierkeys >= (int)ModifierKeys.Shift)
                {
                    hotkey_modifierkey_checkbox_shift.IsChecked = true;
                    hotkey_modifierkeys -= (int)ModifierKeys.Shift;
                }
                else
                {
                    hotkey_modifierkey_checkbox_shift.IsChecked = false;
                }
                CheckBox hotkey_modifierkey_checkbox_ctrl = FindName("hk" + i.ToString() + "_hotkey_modifierkey_checkbox_ctrl") as CheckBox;
                if (hotkey_modifierkeys >= (int)ModifierKeys.Control)
                {
                    hotkey_modifierkey_checkbox_ctrl.IsChecked = true;
                    hotkey_modifierkeys -= (int)ModifierKeys.Control;
                }
                else
                {
                    hotkey_modifierkey_checkbox_ctrl.IsChecked = false;
                }
                CheckBox hotkey_modifierkey_checkbox_alt = FindName("hk" + i.ToString() + "_hotkey_modifierkey_checkbox_alt") as CheckBox;
                if (hotkey_modifierkeys >= (int)ModifierKeys.Alt)
                {
                    hotkey_modifierkey_checkbox_alt.IsChecked = true;
                }
                else
                {
                    hotkey_modifierkey_checkbox_alt.IsChecked = false;
                }
                // ショートカットキーの有効/無効
                int hotkey_mainkey = (int)settings["hotkey" + i.ToString() + "_mainkey"];
                CheckBox hotkey_enable_checkbox = FindName("hk" + i.ToString() + "_hotkey_enable_checkbox") as CheckBox;
                if (hotkey_mainkey < 0)
                {
                    hotkey_mainkey = Math.Abs(hotkey_mainkey);
                    hotkey_enable_checkbox.IsChecked = false;
                }
                else
                {
                    hotkey_enable_checkbox.IsChecked = true;
                }
                Hotkey_enable_checkbox_Click(hotkey_enable_checkbox, null);
                // メインキー
                ComboBox hotkey_mainkey_combobox = FindName("hk" + i.ToString() + "_hotkey_mainkey_combobox") as ComboBox;
                hotkey_mainkey_combobox.SelectedItem = (Key)hotkey_mainkey;
            }
            // 言語
            language_combobox.SelectedItem = new KeyValuePair<string, string>((string)settings["language"], Functions.GetAvailableLanguages()[(string)settings["language"]]);
            // 読み取り結果を変更した場合に使用するデータの設定（デフォルト設定）
            data_selection_setting_checkbox.IsChecked = (bool)settings["result_data_use_default_setting"];
            // 画面撮影の方法
            screen_shooting_crop_method_radio_button.IsChecked = (bool)settings["live_capture_mode"] == false;
            screen_shooting_live_capture_method_radio_button.IsChecked = (bool)settings["live_capture_mode"];
            // ウェブ検索サービスのURL
            byte[] search_service_url_bytes = Functions.Unprotect(Convert.FromBase64String((string)settings["search_service_url"]));
            if (search_service_url_bytes == null)
            {
                // 設定データが破損している場合
                web_search_service_url_text_box.Text = "https://_/search?q={0}";
            }
            else
            {
                web_search_service_url_text_box.Text = Encoding.UTF8.GetString(search_service_url_bytes);
            }
            // 文字読み取りサービス
            byte[] transcription_service_bytes = Functions.Unprotect(Convert.FromBase64String((string)settings["transcription_service"]));
            if (transcription_service_bytes == null)
            {
                // 設定データが破損している場合
                Load_transcription_service_settings(null);
            }
            else
            {
                Load_transcription_service_settings(Encoding.UTF8.GetString(transcription_service_bytes));
            }
        }

        /// <summary>
        /// 保存されている文字読み取りサービスの設定をウィンドウに表示するメソッド
        /// </summary>
        private void Load_transcription_service_settings(string transcription_service_name)
        {
            transcription_service_combobox.SelectedItem = transcription_service_name;
            if (transcription_service_name == null)
            {
                // 使用する文字読み取りサービスが選択されていない場合
                transcription_service_configure_button.Visibility = Visibility.Hidden;
            }
            else
            {
                // 使用する文字読み取りサービスが選択されている場合
                transcription_service_configure_button.Visibility = Visibility.Visible;
            }
            if (Functions.GetTranscriptionServiceCredential() == string.Empty)
            {
                // 文字読み取りサービスの初期設定が済んでいない場合
                transcription_service_combobox.IsEnabled = true;
                transcription_service_configure_button.Content = FindResource("settings/transcription_tab_configure_button_text_in_unset");
                transcription_service_option_settings_button.Visibility = Visibility.Hidden;
                TranscriptionService.Service transcription_service = Functions.GetTranscriptionService(transcription_service_name);
                transcription_service_option_settings_button.IsEnabled = transcription_service.GetOptionSettingPage(false) != null;
                transcription_service_button_partition1.Visibility = Visibility.Hidden;
                transcription_service_clear_settings_button.Visibility = Visibility.Hidden;
                transcription_service_help_label1.Visibility = Visibility.Hidden;
            }
            else
            {
                // 文字読み取りサービスの初期設定が済んでいる場合
                transcription_service_combobox.IsEnabled = false;
                transcription_service_configure_button.Content = FindResource("settings/transcription_tab_configure_button_text_in_configured");
                transcription_service_option_settings_button.Visibility = Visibility.Visible;
                TranscriptionService.Service transcription_service = Functions.GetTranscriptionService(transcription_service_name);
                transcription_service_option_settings_button.IsEnabled = transcription_service.GetOptionSettingPage(false) != null;
                transcription_service_button_partition1.Visibility = Visibility.Visible;
                transcription_service_clear_settings_button.Visibility = Visibility.Visible;
                transcription_service_help_label1.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// OKボタンがクリックされた際の処理
        /// </summary>
        private void Save_button_Click(object sender, RoutedEventArgs e)
        {
            // ウェブ検索サービスのURLが適切かどうか確認する
            if (string.IsNullOrWhiteSpace(web_search_service_url_text_box.Text) == true)
            {
                MessageBox.Show((string)FindResource("settings/web_search_service_url_empty_message"), (string)FindResource("settings/web_search_service_url_empty_title"));
                return;
            }
            else if (web_search_service_url_text_box.Text.StartsWith("http://") == false && web_search_service_url_text_box.Text.StartsWith("https://") == false)
            {
                MessageBox.Show((string)FindResource("web_search_service_url_protocol_error_message"), (string)FindResource("web_search_service_url_protocol_error_title"));
                return;
            }
            else if (web_search_service_url_text_box.Text.Contains("{0}") == false)
            {
                MessageBox.Show((string)FindResource("settings/web_search_service_url_search_word_uninserted_message"), (string)FindResource("settings/web_search_service_url_search_word_uninserted_title"));
                return;
            }
            bool restart_required = false;
            // 現在の設定と異なる言語が選ばれている場合、本当に言語を変更するか確認する
            if (((KeyValuePair<string, string>)language_combobox.SelectedItem).Key != (string)Functions.GetUserSettings()["language"])
            {
                if (MessageBox.Show((string)FindResource("settings/language_change_confirmation_message"), (string)FindResource("settings/language_change_confirmation_title"), MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }
                restart_required = true;
            }
            Close();
            Save_settings();
            // 言語が変更された場合にアプリケーションを再起動する
            if (restart_required == true)
            {
                Functions.RestartApplication();
            }
            else
            {
                MainWindow.Set_up_hotkey(reset: true);
            }
        }

        /// <summary>
        /// キャンセルボタンがクリックされた際の処理
        /// </summary>
        private void Cancel_button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// ウェブ検索設定テストボタンがクリックされた際の処理
        /// </summary>
        private void Web_search_test_button_Click(object sender, RoutedEventArgs e)
        {
            // ウェブ検索サービスのURLが適切かどうか確認する
            if (string.IsNullOrWhiteSpace(web_search_service_url_text_box.Text) == true)
            {
                MessageBox.Show((string)FindResource("settings/web_search_service_url_empty_message"), (string)FindResource("settings/web_search_service_url_empty_title"));
                return;
            }
            else if (web_search_service_url_text_box.Text.StartsWith("http://") == false && web_search_service_url_text_box.Text.StartsWith("https://") == false)
            {
                MessageBox.Show((string)FindResource("web_search_service_url_protocol_error_message"), (string)FindResource("web_search_service_url_protocol_error_title"));
                return;
            }
            else if (web_search_service_url_text_box.Text.Contains("{0}") == false)
            {
                MessageBox.Show((string)FindResource("settings/web_search_service_url_search_word_uninserted_message"), (string)FindResource("settings/web_search_service_url_search_word_uninserted_title"));
                return;
            }
            // 入力されたURLでウェブ検索をテストする
            const string TEST_SEARCH_WORD = "Wikipedia";
            string url = string.Format(web_search_service_url_text_box.Text, WebUtility.UrlEncode(TEST_SEARCH_WORD));
            Functions.AccessWebsite(url);
        }

        /// <summary>
        /// ウェブ検索設定リセットボタンがクリックされた際の処理
        /// </summary>
        private void Web_search_reset_button_Click(object sender, RoutedEventArgs e)
        {
            web_search_service_url_text_box.Text = Functions.DEFAULT_SEARCH_SERVICE_URL;
        }

        /// <summary>
        /// ショートカットキーの有効/無効が切り替えられた際の処理
        /// </summary>
        private void Hotkey_enable_checkbox_Click(object sender, RoutedEventArgs e)
        {
            // チェックボックスの状態に応じてショートカットキーの設定項目を有効化/無効化する
            int hotkey_number = int.Parse(((CheckBox)sender).Name[2..((CheckBox)sender).Name.IndexOf("_")]);
            CheckBox hotkey_modifierkey_checkbox_win = FindName("hk" + hotkey_number.ToString() + "_hotkey_modifierkey_checkbox_win") as CheckBox;
            hotkey_modifierkey_checkbox_win.IsEnabled = (bool)((CheckBox)sender).IsChecked;
            CheckBox hotkey_modifierkey_checkbox_shift = FindName("hk" + hotkey_number.ToString() + "_hotkey_modifierkey_checkbox_shift") as CheckBox;
            hotkey_modifierkey_checkbox_shift.IsEnabled = (bool)((CheckBox)sender).IsChecked;
            CheckBox hotkey_modifierkey_checkbox_ctrl = FindName("hk" + hotkey_number.ToString() + "_hotkey_modifierkey_checkbox_ctrl") as CheckBox;
            hotkey_modifierkey_checkbox_ctrl.IsEnabled = (bool)((CheckBox)sender).IsChecked;
            CheckBox hotkey_modifierkey_checkbox_alt = FindName("hk" + hotkey_number.ToString() + "_hotkey_modifierkey_checkbox_alt") as CheckBox;
            hotkey_modifierkey_checkbox_alt.IsEnabled = (bool)((CheckBox)sender).IsChecked;
            ComboBox hotkey_mainkey_combobox = FindName("hk" + hotkey_number.ToString() + "_hotkey_mainkey_combobox") as ComboBox;
            hotkey_mainkey_combobox.IsEnabled = (bool)((CheckBox)sender).IsChecked;
        }

        /// <summary>
        /// 文字読み取りサービスの基本設定ボタンがクリックされた際の処理
        /// </summary>
        private void Transcription_service_configure_button_Click(object sender, RoutedEventArgs e)
        {
            TranscriptionService.Service transcription_service = Functions.GetTranscriptionService((string)transcription_service_combobox.SelectedItem);
            Page setting_page;
            if (transcription_service_clear_settings_button.Visibility == Visibility.Visible)
            {
                // 文字読み取りサービスの初期設定が済んでいる場合
                setting_page = transcription_service.GetCredentialSettingsPage(false);
            }
            else
            {
                // 文字読み取りサービスの初期設定が済んでいない場合
                setting_page = transcription_service.GetInitialSettingPage();
            }
            // 文字読み取りサービスの設定メニューを表示する
            new TranscriptionService.TranscriptionServiceSettingHost(setting_page, false).ShowDialog();
            Load_transcription_service_settings((string)transcription_service_combobox.SelectedItem);
        }

        /// <summary>
        /// 文字読み取りサービスのオプション設定ボタンがクリックされた際の処理
        /// </summary>
        private void Transcription_service_option_settings_button_Click(object sender, RoutedEventArgs e)
        {
            // 文字読み取りサービスのオプション設定メニューを表示する
            TranscriptionService.Service transcription_service = Functions.GetTranscriptionService((string)transcription_service_combobox.SelectedItem);
            Page setting_page = transcription_service.GetOptionSettingPage(false);
            new TranscriptionService.TranscriptionServiceSettingHost(setting_page, false).ShowDialog();
            Load_transcription_service_settings((string)transcription_service_combobox.SelectedItem);
        }

        /// <summary>
        /// 文字読み取りサービスの設定消去ボタンがクリックされた際の処理
        /// </summary>
        private void Transcription_service_clear_settings_button_Click(object sender, RoutedEventArgs e)
        {
            // 本当に文字読み取りサービスの設定を消去するか確認する
            if (MessageBox.Show((string)FindResource("settings/transcription_service_settings_clear_confirmation_message"), (string)FindResource("settings/transcription_service_settings_clear_confirmation_title"), MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            // 文字読み取りサービスの設定を消去する
            Functions.ClearTranscriptionServiceSettings();
            Load_transcription_service_settings((string)transcription_service_combobox.SelectedItem);
        }

        /// <summary>
        /// 選択中の文字読み取りサービスが変更された際の処理
        /// </summary>
        private void Transcription_service_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((string)transcription_service_configure_button.Content == null)
            {
                // 初期化時に呼び出された場合
                return;
            }
            if (transcription_service_combobox.Text != (string)transcription_service_combobox.SelectedItem && (string)transcription_service_combobox.SelectedItem != null)
            {
                // 本当に文字読み取りサービスを変更するか確認する
                if (MessageBox.Show((string)FindResource("settings/transcription_service_change_confirmation_message"), (string)FindResource("settings/transcription_service_change_confirmation_title"), MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    if (transcription_service_combobox.Text == string.Empty)
                    {
                        // 変更される前に何も選択されていなかった場合
                        transcription_service_combobox.SelectedItem = null;
                    }
                    else
                    {
                        transcription_service_combobox.SelectedItem = transcription_service_combobox.Text;
                    }
                    return;
                }
                // 文字読み取りサービスを変更する
                Functions.SetUserSettings(Convert.ToBase64String(Functions.Protect(Encoding.UTF8.GetBytes((string)transcription_service_combobox.SelectedItem))), "transcription_service");
                Load_transcription_service_settings((string)transcription_service_combobox.SelectedItem);
            }
        }

        /// <summary>
        /// 設定インポートボタンがクリックされた際の処理
        /// </summary>
        private void Settings_import_button_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            bool imported = (bool)new ImportExportMenuHost(new Import_Page1()).ShowDialog();
            if (imported == true)
            {
                // インポートが行われた場合にアプリケーションを再起動する
                Close();
                Functions.RestartApplication();
            }
            else
            {
                // インポートが行われなかった場合
                Show();
            }
        }

        /// <summary>
        /// 設定エクスポートボタンがクリックされた際の処理
        /// </summary>
        private void Settings_export_button_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            new ImportExportMenuHost(new Export_Page1()).ShowDialog();
            Show();
        }
    }
}
