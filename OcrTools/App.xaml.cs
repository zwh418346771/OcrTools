using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace OcrTools
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private System.Threading.Mutex mutex = new System.Threading.Mutex(false, "OcrTools");

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string path = string.Empty;
            var name = "OcrTools";
            Process[] ps = Process.GetProcessesByName(name);
            string currentPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            foreach (Process p in ps)
            {
                path = p.MainModule.FileName.ToString();
                SetForegroundWindow(p.MainWindowHandle);
            }

            if (!mutex.WaitOne(0, false))
            {
                mutex.Close();
                mutex = null;
                Current.Shutdown();
                return;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Close();
            }
        }
    }
}
