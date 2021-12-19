using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 言語データを読み込む
            Functions.LoadLanguageData();

            // [多重起動チェック]
            // ミューテックスを作成する
            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            var mutex = new Mutex(false, executing_assembly.GetName().Name);
            // ミューテックスの所有権を要求する
            if (!mutex.WaitOne(0, false))
            {
                // 既に同じプログラムが起動している場合にアプリケーションを終了する
                MessageBox.Show((string)FindResource("other/multiple_startup_error_message"), (string)FindResource("other/multiple_startup_error_title") + " - " + executing_assembly.GetName().Name);
                Environment.Exit(0);
            }
        }
    }
}
