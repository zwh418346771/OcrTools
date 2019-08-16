using MahApps.Metro.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
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

        Cutter cutter = null;

        public static Bitmap catchBmp = null;

        private void btnHigh_Click(object sender, RoutedEventArgs e)
        {
            cutter = new Cutter();
            // 隐藏原窗口
            Hide();
            Thread.Sleep(1200);

            // 设置截图窗口的背景图片
            Bitmap bmp = new Bitmap(Screen.AllScreens[0].Bounds.Width, Screen.AllScreens[0].Bounds.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(new System.Drawing.Point(0, 0), new System.Drawing.Point(0, 0), new System.Drawing.Size(bmp.Width, bmp.Height));
            cutter.BackgroundImage = bmp;

            // 显示原窗口
            Show();

            // 显示截图窗口
            cutter.WindowState = FormWindowState.Maximized;
            cutter.ShowDialog();
            ScreenAndOcr();
        }

        private void ScreenAndOcr()
        {
            this.Dispatcher.Invoke(new Action(delegate
            {
                Task.Run(() =>
                {
                    if (!IsConnectInternet())
                    {
                        System.Windows.MessageBox.Show("网络未链接！");
                        return;
                    }
                    OcrResult = string.Empty;
                    Baidu.Aip.Ocr.Ocr client = null;
                    try
                    {
                        client = new Baidu.Aip.Ocr.Ocr(Constant.API_KEY, Constant.SECRET_KEY);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("创建连接出错" + ex);
                        return;
                    }
                    client.Timeout = 60000;
                    MemoryStream ms = new MemoryStream();
                    catchBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    byte[] bytes = ms.GetBuffer();
                    ms.Close();

                    List<Root> rootList = new List<Root>();
                    string jsonStr = string.Empty;
                    try
                    {
                        rbtnNormal.Dispatcher.Invoke(new Action(delegate
                        {
                            if (rbtnNormal.IsChecked == true)
                            {
                                jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(client.GeneralBasic(bytes));
                            }
                            else
                            {
                                jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(client.Accurate(bytes));
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Ocr获取结果失败" + ex);
                        return;
                    }
                    Root root = null;
                    if (!string.IsNullOrWhiteSpace(jsonStr))
                    {
                        root = JsonConvert.DeserializeObject<Root>(jsonStr);
                    }

                    foreach (var item in root.words_result)
                    {
                        OcrResult = OcrResult + item.words + "\n";
                    }
                    this.txtResult.Dispatcher.Invoke(new Action(delegate
                    {
                        txtResult.Text = OcrResult;
                    }));
                });
            }));
            
        }

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
    }
}
