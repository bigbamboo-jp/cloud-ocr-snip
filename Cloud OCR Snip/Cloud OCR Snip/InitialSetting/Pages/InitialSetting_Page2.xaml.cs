using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// 初期設定ページ2
    /// 機能：文字読み取りサービスの選択
    /// （InitialSetting_Page2.xaml の相互作用ロジック）
    /// </summary>
    public partial class InitialSetting_Page2 : Page
    {
        public InitialSetting_Page2()
        {
            InitializeComponent();

            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("initial_setting/window_title") + " - " + executing_assembly.GetName().Name;
            product_name_label.Content = executing_assembly.GetName().Name;
            transcription_service_combobox.ItemsSource = Functions.transcription_services;
        }

        /// <summary>
        /// 設定インポートボタンがクリックされた際の処理
        /// </summary>
        private async void Settings_import_button_Click(object sender, RoutedEventArgs e)
        {
            // インポートメニューを表示する
            Window parent_window = Window.GetWindow(this);
            parent_window.Hide();
            bool imported = (bool)new ImportExportMenuHost(new Import_Page1()).ShowDialog();
            // インポートの結果に応じて次のステップに進む
            Dictionary<string, object> settings = Functions.GetUserSettings();
            if (imported == true && (string)settings["transcription_service"] != string.Empty)
            {
                // インポートによって文字読み取りサービスが選択された場合
                parent_window.Close();
                if (Functions.GetTranscriptionServiceCredential() == string.Empty)
                {
                    // 次のページ（文字読み取りサービスの認証情報の設定）に進む
                    byte[] transcription_service_bytes = Functions.Unprotect(Convert.FromBase64String((string)settings["transcription_service"]));
                    TranscriptionService.Service transcription_service = Functions.GetTranscriptionService(Encoding.UTF8.GetString(transcription_service_bytes));
                    Page next_page = transcription_service.GetCredentialSettingsPage(initial_setting: true);
                    new TranscriptionService.TranscriptionServiceSettingHost(next_page, initial_setting: true).ShowDialog();
                }
                // ウィンドウが閉じられた際の処理が始まるまで待機する
                await Task.Delay(10);
                // アプリケーションを再起動する
                Functions.RestartApplication();
            }
            else
            {
                // 文字読み取りサービスが選択されなかった場合
                parent_window.Show();
            }
        }

        /// <summary>
        /// 次へボタンがクリックされた際の処理
        /// </summary>
        private async void Next_button_Click(object sender, RoutedEventArgs e)
        {
            // 選択された文字読み取りサービスを保存してウィンドウを閉じる
            Functions.SetUserSettings(Convert.ToBase64String(Functions.Protect(Encoding.UTF8.GetBytes((string)transcription_service_combobox.SelectedItem))), "transcription_service");
            Window parent_window = Window.GetWindow(this);
            parent_window.Close();

            if (Functions.GetTranscriptionServiceCredential() == string.Empty)
            {
                // 次のページ（文字読み取りサービスの初期設定ページ）に進む
                TranscriptionService.Service transcription_service = Functions.GetTranscriptionService((string)transcription_service_combobox.SelectedItem);
                Page next_page = transcription_service.GetInitialSettingPage();
                new TranscriptionService.TranscriptionServiceSettingHost(next_page, initial_setting: true).ShowDialog();
            }
            // ウィンドウが閉じられた際の処理が始まるまで待機する
            await Task.Delay(10);
            // アプリケーションを再起動する
            Functions.RestartApplication();
        }

        /// <summary>
        /// 選択中の文字読み取りサービスが変更された際の処理
        /// </summary>
        private void Transcription_service_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 文字読み取りサービスの説明文を取得して表示する
            TranscriptionService.Service transcription_service = Functions.GetTranscriptionService((string)transcription_service_combobox.SelectedItem);
            string transcription_service_explanation = transcription_service.Explanation;
            if (transcription_service_explanation == null)
            {
                // 説明文が指定されていない場合
                transcription_service_explanation_text_block.Text = string.Empty;
            }
            else
            {
                // 説明文が指定されている場合
                transcription_service_explanation_text_block.Text = transcription_service_explanation;
            }
        }
    }
}
