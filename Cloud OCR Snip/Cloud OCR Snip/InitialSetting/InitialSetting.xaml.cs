using System;
using System.Windows.Navigation;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// 初期設定ページホスト
    /// 機能：初期設定ページの表示
    /// （InitialSetting.xaml の相互作用ロジック）
    /// </summary>
    public partial class InitialSetting : NavigationWindow
    {
        public InitialSetting()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ウィンドウが閉じられた際の処理
        /// </summary>
        private void NavigationWindow_Closed(object sender, EventArgs e)
        {
            // 設定が完了していない状況で初期設定ウィンドウが閉じられた場合、アプリケーションを終了する
            if ((string)Functions.GetUserSettings()["transcription_service"] == string.Empty)
            {
                Environment.Exit(0);
            }
        }
    }
}
