using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        System.Threading.Mutex mutex = null; // ミューテックス

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 言語データを読み込む
            Cloud_OCR_Snip.Functions.LoadLanguageData();

            // [多重起動チェック]
            // ミューテックスを作成する
            System.Reflection.Assembly executing_assembly = System.Reflection.Assembly.GetExecutingAssembly();
            mutex = new System.Threading.Mutex(false, executing_assembly.GetName().Name);
            // ミューテックスの所有権を要求する
            if (mutex.WaitOne(0, false) == false)
            {
                // 既に同じプログラムが起動している場合にアプリケーションを終了する
                MessageBox.Show((string)FindResource("other/multiple_startup_error_message"), (string)FindResource("other/multiple_startup_error_title") + " - " + executing_assembly.GetName().Name);
                Environment.Exit(0);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // ミューテックスを開放する
            mutex.ReleaseMutex();
            mutex.Close();

            base.OnExit(e);
        }
    }
}
