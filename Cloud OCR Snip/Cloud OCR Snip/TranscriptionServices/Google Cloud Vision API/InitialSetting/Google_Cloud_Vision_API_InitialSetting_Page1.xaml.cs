using Markdig.Wpf;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace TranscriptionService
{
    /// <summary>
    /// Google Cloud Vision API 初期設定ページ1
    /// 機能：サービスの詳しい説明の表示
    /// （Google_Cloud_Vision_API_InitialSetting_Page1.xaml の相互作用ロジック）
    /// </summary>
    public partial class Google_Cloud_Vision_API_InitialSetting_Page1 : Page
    {
        public Google_Cloud_Vision_API_InitialSetting_Page1(bool initial_setting)
        {
            InitializeComponent();

            initialization = initial_setting;
            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("google_cloud_vision_api/initial_setting/window_title") + " - " + executing_assembly.GetName().Name;
            // リッチテキストボックスにサービスの説明文を表示する
            FlowDocument service_description = Markdown.ToFlowDocument((string)FindResource("google_cloud_vision_api/initial_setting_1/service_description"));
            service_description.FontSize = 20.0;
            SubscribeToAllHyperlinks(service_description);
            service_description_rich_text_box.Document = service_description;
        }

        private bool initialization; // 初期設定かどうかのフラグ

        /// <summary>
        /// 次へボタンがクリックされた際の処理
        /// </summary>
        private void Next_button_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoForward == true)
            {
                // これまでに次のページに進んだことがある場合
                NavigationService.GoForward();
            }
            else
            {
                // これまでに次のページに進んだことがない場合
                NavigationService.Navigate(new Google_Cloud_Vision_API_InitialSetting_Page2(initialization));
            }
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
