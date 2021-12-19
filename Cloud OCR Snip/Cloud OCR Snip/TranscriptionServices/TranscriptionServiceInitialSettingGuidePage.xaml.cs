using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace TranscriptionService
{
    /// <summary>
    /// 文字読み取りサービス 初期設定再開ガイドページ
    /// 機能：ユーザーに初期設定を再開する方法を案内する
    /// （TranscriptionServiceInitialSettingGuidePage.xaml の相互作用ロジック）
    /// </summary>
    public partial class TranscriptionServiceInitialSettingGuidePage : Page
    {
        public TranscriptionServiceInitialSettingGuidePage()
        {
            InitializeComponent();

            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("transcription_service_initial_setting_guide_page/window_title") + " - " + executing_assembly.GetName().Name;
        }

        /// <summary>
        /// OKボタンがクリックされた際の処理
        /// </summary>
        private void Close_button_Click(object sender, RoutedEventArgs e)
        {
            Window parent_window = Window.GetWindow(this);
            parent_window.Close();
        }
    }
}
