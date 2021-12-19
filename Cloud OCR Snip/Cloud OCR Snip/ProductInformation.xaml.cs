using System;
using System.Reflection;
using System.Windows;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// 製品情報ウィンドウ
    /// 機能：製品に関する情報の表示
    /// （ProductInformation.xaml の相互作用ロジック）
    /// </summary>
    public partial class ProductInformation : Window
    {
        public ProductInformation()
        {
            InitializeComponent();

            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            Title = (string)FindResource("product_information/window_title") + " - " + executing_assembly.GetName().Name;
            product_name_label.Content = executing_assembly.GetName().Name;
            version_label.Content = string.Format((string)FindResource("product_information/version_label_text"), executing_assembly.GetName().Version.ToString());
            AssemblyCopyrightAttribute aca = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute));
            if (aca == null)
            {
                // 著作権情報が登録されていない場合
                copyright_label.Content = string.Empty;
            }
            else
            {
                // 著作権情報が登録されている場合
                copyright_label.Content = string.Format((string)FindResource("product_information/copyright_label_text"), aca.Copyright);
            }
            terms_text_box.Text = Texts.TERMS;
            third_party_notification_text_box.Text = Texts.THIRD_PARTY_NOTIFICATION;
        }

        /// <summary>
        /// OKボタンがクリックされた際の処理
        /// </summary>
        private void Close_button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
