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
        public MainWindow()
        {
            DataContext = new MainWindowViewModel();
            InitializeComponent();
            Closing += MainWindow_Closing;
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
            MainWindow main = new MainWindow();
            // 隐藏原窗口
            Hide();
            Thread.Sleep(200);

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
            Task.Run(() =>
            {
                if (!IsConnectInternet())
                {
                    System.Windows.MessageBox.Show("网络未链接！");
                    return;
                }
                OcrResult = string.Empty;
                var API_KEY = "gGRQ28PMwDf6nStqSND7Pa3A";
                var SECRET_KEY = "u9j4NdWAGwK7kKjL7GWt5qmSe4EnCcYw";
                Baidu.Aip.Ocr.Ocr client = null;
                try
                {
                    client = new Baidu.Aip.Ocr.Ocr(API_KEY, SECRET_KEY);
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
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(int Description, int ReservedValue);

        public bool IsConnectInternet()
        {
            int Description = 0;
            return InternetGetConnectedState(Description, 0);
        }
    }
}
