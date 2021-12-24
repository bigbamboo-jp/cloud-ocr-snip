using Markdig.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace TranscriptionService
{
    /// <summary>
    /// Google Cloud Vision API 初期設定ページ4
    /// 機能：オプション設定の変更
    /// （Google_Cloud_Vision_API_InitialSetting_Page4.xaml の相互作用ロジック）
    /// </summary>
    public partial class Google_Cloud_Vision_API_InitialSetting_Page4 : Page
    {
        public Google_Cloud_Vision_API_InitialSetting_Page4(bool initial_setting)
        {
            InitializeComponent();

            initialization = initial_setting;
            if (initialization == true)
            {
                // 初期設定の場合、キャンセルボタンを非表示にしてその場所にOKボタンを配置する
                cancel_button.Visibility = Visibility.Hidden;
                save_button.Margin = cancel_button.Margin;
            }
            // リッチテキストボックスに言語ヒント設定の参考情報を表示する
            FlowDocument language_hint_setting_reference_information = Markdown.ToFlowDocument((string)FindResource("google_cloud_vision_api/initial_setting_4/language_hint_setting_reference_information"));
            language_hint_setting_reference_information.FontSize = 20.0;
            SubscribeToAllHyperlinks(language_hint_setting_reference_information);
            language_hint_setting_reference_information_rich_text_box.Document = language_hint_setting_reference_information;
            // リッチテキストボックスにAPIエンドポイント設定の参考情報を表示する
            FlowDocument api_endpoint_setting_reference_information = Markdown.ToFlowDocument((string)FindResource("google_cloud_vision_api/initial_setting_4/api_endpoint_setting_reference_information"));
            api_endpoint_setting_reference_information.FontSize = 20.0;
            SubscribeToAllHyperlinks(api_endpoint_setting_reference_information);
            api_endpoint_setting_reference_information_rich_text_box.Document = api_endpoint_setting_reference_information;

            language_hints = new ObservableCollection<string>();
            Load_settings();
        }

        private bool initialization; // 初期設定かどうかのフラグ

        private ObservableCollection<string> language_hints; // ユーザーが入力した言語ヒント

        /// <summary>
        /// 言語ヒント追加ボタンがクリックされた際の処理
        /// </summary>
        private void Language_hint_add_button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(language_hint_additional_text_box.Text) == false)
            {
                if (language_hints.Contains(language_hint_additional_text_box.Text) == false)
                {
                    // まだその言語ヒントが登録されていない場合
                    language_hints.Add(language_hint_additional_text_box.Text);
                    language_hint_additional_text_box.Text = string.Empty;
                }
                else
                {
                    // 既に同じ言語ヒントが登録されている場合
                    language_hint_list_view.SelectedItem = language_hint_additional_text_box.Text;
                    language_hint_additional_text_box.Text = string.Empty;
                }
            }
        }

        /// <summary>
        /// 言語ヒント削除ボタンがクリックされた際の処理
        /// </summary>
        private void Language_hint_delete_button_Click(object sender, RoutedEventArgs e)
        {
            // クリックされたボタンのデータコンテキストになっている言語ヒントをコレクションから削除する
            Button button = (Button)sender;
            language_hints.Remove((string)button.DataContext);
        }

        /// <summary>
        /// OKボタンがクリックされた際の処理
        /// </summary>
        private void Save_button_Click(object sender, RoutedEventArgs e)
        {
            Save_settings();
            Window parent_window = Window.GetWindow(this);
            parent_window.Close();
        }

        /// <summary>
        /// キャンセルボタンがクリックされた際の処理
        /// </summary>
        private void Cancel_button_Click(object sender, RoutedEventArgs e)
        {
            Window parent_window = Window.GetWindow(this);
            parent_window.Close();
        }

        /// <summary>
        /// ウィンドウに表示されている状態で設定を更新するメソッド
        /// </summary>
        private void Save_settings()
        {
            // APIエンドポイント
            string api_endpoint_setting_value;
            if (api_endpoint_text_box.Text == string.Empty)
            {
                // APIエンドポイントが入力されていない場合
                api_endpoint_setting_value = string.Empty;
            }
            else
            {
                // APIエンドポイントが入力された場合
                api_endpoint_setting_value = Convert.ToBase64String(Functions.Protect(Encoding.UTF8.GetBytes(api_endpoint_text_box.Text)));
            }
            Dictionary<string, object> settings = new Dictionary<string, object>
            {
                { "language_hints", language_hints.ToArray() }, // 言語ヒント
                { "api_endpoint", api_endpoint_setting_value }
            };
            Functions.SetTranscriptionServiceSettings(settings);
        }

        /// <summary>
        /// 保存されている設定をウィンドウに表示するメソッド
        /// </summary>
        private void Load_settings()
        {
            Dictionary<string, object> settings = Functions.GetTranscriptionServiceSettings();
            if (settings == null)
            {
                // 設定データがない場合
                return;
            }
            // 言語ヒント
            string[] language_hints_section = ((List<object>)settings["language_hints"]).OfType<string>().ToList().ToArray();
            language_hints = new ObservableCollection<string>(language_hints_section);
            language_hint_list_view.DataContext = language_hints;
            // APIエンドポイント
            if ((string)settings["api_endpoint"] == string.Empty)
            {
                // 設定されていない場合
                api_endpoint_text_box.Text = string.Empty;
            }
            else
            {
                // 設定されている場合
                byte[] api_endpoint_bytes = Functions.Unprotect(Convert.FromBase64String((string)settings["api_endpoint"]));
                if (api_endpoint_bytes == null)
                {
                    // 設定データが破損している場合
                    api_endpoint_text_box.Text = ".googleapis.com";
                }
                else
                {
                    api_endpoint_text_box.Text = Encoding.UTF8.GetString(api_endpoint_bytes);
                }
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
