using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// インポート ページ4
    /// 機能：インポート完了の通知
    /// （Import_Page4.xaml の相互作用ロジック）
    /// </summary>
    public partial class Import_Page4 : Page
    {
        public Import_Page4()
        {
            InitializeComponent();

            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("import/window_title") + " - " + executing_assembly.GetName().Name;
            product_name_label.Content = executing_assembly.GetName().Name;
        }

        /// <summary>
        /// OKボタンがクリックされた際の処理
        /// </summary>
        private void Close_button_Click(object sender, RoutedEventArgs e)
        {
            Window parent_window = Window.GetWindow(this);
            // 完了フラグをオンにする
            parent_window.DialogResult = true;
            parent_window.Close();
        }
    }
}
