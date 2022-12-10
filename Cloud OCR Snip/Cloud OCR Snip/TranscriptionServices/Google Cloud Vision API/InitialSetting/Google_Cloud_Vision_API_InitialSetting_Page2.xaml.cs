using Markdig.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace TranscriptionService
{
    /// <summary>
    /// Google Cloud Vision API 初期設定ページ2
    /// 機能：サービスアカウントの登録
    /// （Google_Cloud_Vision_API_InitialSetting_Page2.xaml の相互作用ロジック）
    /// </summary>
    public partial class Google_Cloud_Vision_API_InitialSetting_Page2 : Page
    {
        public Google_Cloud_Vision_API_InitialSetting_Page2(bool initial_setting)
        {
            InitializeComponent();

            initialization = initial_setting;
            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("google_cloud_vision_api/initial_setting/window_title") + " - " + executing_assembly.GetName().Name;
            // リッチテキストボックスにセットアップガイドを表示する
            FlowDocument setup_guide = Markdown.ToFlowDocument((string)FindResource("google_cloud_vision_api/initial_setting_2/setup_guide"));
            setup_guide.FontSize = 20.0;
            SubscribeToAllHyperlinks(setup_guide);
            setup_guide_rich_text_box.Document = setup_guide;
        }

        private bool initialization; // 初期設定かどうかのフラグ

        private const string KEY_FILE_EXTENSION = ".json"; // キーファイルの拡張子

        /// <summary>
        /// ページが読み込まれた際の処理
        /// </summary>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack == false)
            {
                // 前のページに戻れない場合、戻るボタンのテキストを「キャンセル」にする
                back_button.Content = (string)FindResource("google_cloud_vision_api/initial_setting/cancel_button_text");
            }
        }

        /// <summary>
        /// 参照ボタンがクリックされた際の処理
        /// </summary>
        private void Key_file_browse_button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog
            {
                InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Filter = (string)FindResource("google_cloud_vision_api/initial_setting_2/load_key_file_filter"),
                FilterIndex = -1,
                Title = (string)FindResource("google_cloud_vision_api/initial_setting_2/load_key_file_title")
            };
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // 指定されたファイルパスが適切かどうかチェックする
                if (Path.GetExtension(ofd.FileName) != KEY_FILE_EXTENSION)
                {
                    MessageBox.Show((string)FindResource("file_dialog/file_type_error_message"), (string)FindResource("file_dialog/file_type_error_title"));
                    return;
                }
                key_file_path_text_box.Text = ofd.FileName;
            }
        }

        /// <summary>
        /// ファイルパスのテキストボックスのデータが変更された際の処理
        /// </summary>
        private void Key_file_path_text_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            register_button.IsEnabled = string.IsNullOrWhiteSpace(key_file_path_text_box.Text) == false;
        }

        /// <summary>
        /// 登録ボタンがクリックされた際の処理
        /// </summary>
        private void Register_button_Click(object sender, RoutedEventArgs e)
        {
            string key_file_path = key_file_path_text_box.Text;
            // ファイルが存在するか確認する
            if (File.Exists(key_file_path) == false)
            {
                MessageBox.Show((string)FindResource("file_dialog/file_not_found_error_message"), (string)FindResource("file_dialog/file_not_found_error_title"));
                return;
            }
            // 指定されたファイルパスが適切かどうかチェックする
            if (Path.GetExtension(key_file_path) != KEY_FILE_EXTENSION)
            {
                MessageBox.Show((string)FindResource("file_dialog/file_type_error_message"), (string)FindResource("file_dialog/file_type_error_title"));
                return;
            }
            // サービス設定をデフォルト設定で保存する
            Functions.SetTranscriptionServiceSettings(new Dictionary<string, object> { { "language_hints", Array.Empty<string>() }, { "api_endpoint", string.Empty } });
            // サービスアカウントキーを読み込んで保存する（仮でエクスポート不可に設定）
            string service_account_key = File.ReadAllText(key_file_path);
            Functions.SetTranscriptionServiceCredential(service_account_key, false);
            // 次のページに進む
            NavigationService.Navigate(new Google_Cloud_Vision_API_InitialSetting_Page3(initialization, service_account_key));
        }

        /// <summary>
        /// 戻るボタン（キャンセルボタン）がクリックされた際の処理
        /// </summary>
        private void Back_button_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack == true)
            {
                // 前のページに戻れる場合（戻るボタン）
                NavigationService.GoBack();
            }
            else
            {
                // 前のページに戻れない場合（キャンセルボタン）
                Window parent_window = Window.GetWindow(this);
                parent_window.Close();
            }
        }

        /// <summary>
        /// フロードキュメント内の全てのハイパーリンクにクリック時の処理を設定するメソッド
        /// </summary>
        private void SubscribeToAllHyperlinks(FlowDocument flow_document)
        {
            List<Hyperlink> hyperlinks = GetVisuals(flow_document).OfType<Hyperlink>().ToList();
            foreach (Hyperlink link in hyperlinks)
            {
                // それぞれのハイパーリンクについて、新しく作り直して置き換える（元のハイパーリンクが正常に動作しないため）
                Hyperlink new_link = new Hyperlink(link.Inlines.First())
                {
                    NavigateUri = link.NavigateUri
                };
                new_link.RequestNavigate += new RequestNavigateEventHandler(Hyperlink_RequestNavigate);
                ((Paragraph)link.Parent).Inlines.InsertBefore(link, new_link);
                ((Paragraph)link.Parent).Inlines.Remove(link);
            }
        }

        /// <summary>
        /// 再帰処理でDependencyObject内の全要素を取得するメソッド
        /// </summary>
        private IEnumerable<DependencyObject> GetVisuals(DependencyObject root)
        {
            foreach (DependencyObject child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                yield return child;
                // 再帰的に処理する
                foreach (DependencyObject descendants in GetVisuals(child))
                {
                    yield return descendants;
                }
            }
        }

        /// <summary>
        /// ハイパーリンクがクリックされた際の処理
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Functions.AccessWebsite(e.Uri.AbsoluteUri);
            // 処理済みのフラグをオンにする
            e.Handled = true;
        }
    }
}
