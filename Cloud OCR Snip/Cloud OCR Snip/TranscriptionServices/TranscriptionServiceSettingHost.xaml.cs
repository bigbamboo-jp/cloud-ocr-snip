using System.Reflection;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace TranscriptionService
{
    /// <summary>
    /// 文字読み取りサービス 設定ページホスト
    /// 機能：文字読み取りサービスの設定ページを表示する
    /// （TranscriptionServiceSettingHost.xaml の相互作用ロジック）
    /// </summary>
    public partial class TranscriptionServiceSettingHost : NavigationWindow
    {
        public TranscriptionServiceSettingHost(Page page, bool initial_setting)
        {
            InitializeComponent();

            initialization = initial_setting;
            // デザイナーでの表示と実際の表示のずれを直す
            double title_bar_height_in_designer = 0.0;
            Height += System.Windows.Forms.SystemInformation.CaptionHeight - title_bar_height_in_designer;
            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            Title = executing_assembly.GetName().Name;
            // 指定されたページを表示する
            NavigationService.Navigate(page);
        }

        private bool initialization; // 初期設定かどうかのフラグ

        /// <summary>
        /// ウィンドウが閉じられる直前での処理
        /// </summary>
        private void NavigationWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (initialization == true)
            {
                if (NavigationService.Content.GetType() != typeof(TranscriptionServiceInitialSettingGuidePage))
                {
                    // 表示しているページが初期設定再開ガイドページでない場合
                    if (Cloud_OCR_Snip.Functions.GetTranscriptionServiceCredential() == string.Empty)
                    {
                        // 文字読み取りサービスの初期設定が完了していない場合に初期設定再開ガイドページを表示する
                        e.Cancel = true;
                        NavigationService.Navigate(new TranscriptionServiceInitialSettingGuidePage());
                    }
                }
            }
        }
    }
}
