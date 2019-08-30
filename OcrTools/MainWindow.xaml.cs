using MahApps.Metro.Controls;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using static OcrTools.MainWindowViewModel;

namespace OcrTools
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static string OcrResult;

        private const int WM_SYSCOMMAND = 0x112;
        private const int MF_STRING = 0x0;
        private const int MF_SEPARATOR = 0x800;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InsertMenuItem(IntPtr hMenu, int uPosition, int uFlags, int uIDNewItem, String ipNewItem);

        private int SYSMENU_ABOUT_ID = 0x1;

        public MainWindow()
        {
            DataContext = new MainWindowViewModel();
            InitializeComponent();
            Closing += MainWindow_Closing;
            SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        public static Bitmap catchBmp = null;

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(int Description, int ReservedValue);

        public bool IsConnectInternet()
        {
            int Description = 0;
            return InternetGetConnectedState(Description, 0);
        }
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            var handle = (new WindowInteropHelper(this)).Handle;
            IntPtr hSysMenu = GetSystemMenu(handle, false);
            AppendMenu(hSysMenu, MF_SEPARATOR, 0, String.Empty);
            AppendMenu(hSysMenu, MF_STRING, SYSMENU_ABOUT_ID, "关于");
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if ((msg == WM_SYSCOMMAND) && ((int)wParam == SYSMENU_ABOUT_ID))
            {
                About about = new About();
                about.ShowDialog();
            }
            return IntPtr.Zero;
        }

        private void Progress_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    var storyboard = Progress.FindResource("ProgressStoryboard") as Storyboard;
                    if (storyboard == null)
                    {
                        return;
                    }

                    if ((bool)e.NewValue)
                    {
                        storyboard.Begin();
                    }
                    else
                    {
                        storyboard.Stop();
                    }
                }));
            });
        }
        
        private void testlist_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (testlist.SelectedIndex == -1)
            {
                return;
            }
            var text = (Record)testlist.SelectedItem;
            Clipboard.SetDataObject(text.RecordText);
            testlist.SelectedIndex = -1;
        }
    }
}
