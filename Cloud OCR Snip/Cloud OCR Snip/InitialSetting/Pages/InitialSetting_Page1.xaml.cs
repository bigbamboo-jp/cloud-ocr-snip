using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// 初期設定ページ1
    /// 機能：ライセンス条項の表示及び同意の確認
    /// （InitialSetting_Page1.xaml の相互作用ロジック）
    /// </summary>
    public partial class InitialSetting_Page1 : Page
    {
        public InitialSetting_Page1()
        {
            InitializeComponent();

            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            WindowTitle = (string)FindResource("initial_setting/window_title") + " - " + executing_assembly.GetName().Name;
            product_name_label.Content = executing_assembly.GetName().Name;
            terms_text_box.Text = Texts.TERMS;
        }

        /// <summary>
        /// 「同意する」ボタンがクリックされた際の処理
        /// </summary>
        private void Agree_button_Click(object sender, RoutedEventArgs e)
        {
            // 次のページに進む
            NavigationService.Navigate(new InitialSetting_Page2());
        }

        /// <summary>
        /// 「同意しない」ボタンがクリックされた際の処理
        /// </summary>
        private void Disagree_button_Click(object sender, RoutedEventArgs e)
        {
            // アプリケーションを終了する
            Environment.Exit(0);
        }
    }
}
