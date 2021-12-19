using System.Windows.Controls;
using System.Windows.Navigation;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// インポート/エクスポート メニュー ホスト
    /// 機能：インポート/エクスポート機能のページを表示する
    /// （ImportExportMenuHost.xaml の相互作用ロジック）
    /// </summary>
    public partial class ImportExportMenuHost : NavigationWindow
    {
        public ImportExportMenuHost(Page page)
        {
            InitializeComponent();

            // 指定されたページを表示する
            NavigationService.Navigate(page);
        }
    }
}
