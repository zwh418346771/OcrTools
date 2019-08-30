using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace OcrTools
{
    class MainWindowViewModel : BindableBase
    {
        private WeakReference _viewHolder = null;

        private MainWindow MyWindow
        {
            get
            {
                if (_viewHolder == null || _viewHolder.IsAlive == false)
                {
                    return null;
                }
                return _viewHolder.Target as MainWindow;
            }
            set
            {
                if (value == null)
                {
                    _viewHolder = null;
                    return;
                }
                _viewHolder = new WeakReference(value);
            }
        }

        private int sliderValue;
        /// <summary>
        /// 
        /// </summary>
        public int SliderValue
        {
            get { return sliderValue; }
            set { SetProperty(ref sliderValue, value); }
        }

        private ObservableCollection<Record> recordList;
        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<Record> RecordList
        {
            get { return recordList; }
            set { SetProperty(ref recordList, value); }
        }

        private string ocrResult;
        /// <summary>
        /// 
        /// </summary>
        public string OcrResult
        {
            get { return ocrResult; }
            set { SetProperty(ref ocrResult, value); }
        }

        private string imageUri;
        /// <summary>
        /// 
        /// </summary>
        public string ImageUri
        {
            get { return imageUri; }
            set { SetProperty(ref imageUri, value); }
        }

        private bool normalRadioButton;
        /// <summary>
        /// 
        /// </summary>
        public bool NormalRadioButton
        {
            get { return normalRadioButton; }
            set { SetProperty(ref normalRadioButton, value); }
        }

        private bool plusRadioButton;
        /// <summary>
        /// 
        /// </summary>
        public bool PlusRadioButton
        {
            get { return plusRadioButton; }
            set { SetProperty(ref plusRadioButton, value); }
        }

        private Visibility isUriVisible;
        /// <summary>
        /// 
        /// </summary>
        public Visibility IsUriVisible
        {
            get { return isUriVisible; }
            set { SetProperty(ref isUriVisible, value); }
        }

        private bool isRecordVisible;
        /// <summary>
        /// 
        /// </summary>
        public bool IsRecordVisible
        {
            get { return isRecordVisible; }
            set { SetProperty(ref isRecordVisible, value); }
        }

        private Visibility isProgressVisibility = Visibility.Collapsed;
        /// <summary>
        /// 
        /// </summary>
        public Visibility IsProgressVisibility
        {
            get { return isProgressVisibility; }
            set { SetProperty(ref isProgressVisibility, value); }
        }

        private Baidu.Aip.Ocr.Ocr client;
        /// <summary>
        /// 
        /// </summary>
        public Baidu.Aip.Ocr.Ocr Client
        {
            get { return client; }
            set { SetProperty(ref client, value); }
        }

        public MainWindowViewModel()
        {
            NormalRadioButton = true;
            IsUriVisible = Visibility.Hidden;
            CreatOcrClient();
            RecordList = new ObservableCollection<Record>();
        }

        private Cutter cutter = null;

        public static Bitmap catchBmp = null;
        #region Command

        private DelegateCommand<Window> contentRenderedCommand;
        public DelegateCommand<Window> ContentRenderedCommand
        {
            get
            {
                if (contentRenderedCommand == null)
                {
                    contentRenderedCommand = new DelegateCommand<Window>(ContentRendered);
                }
                return contentRenderedCommand;
            }
        }

        /// <summary>
        /// set the mainwindow
        /// </summary>
        /// <param name="window"></param>
        private void ContentRendered(Window window)
        {
            MyWindow = (MainWindow)window;
            MyWindow.Owner = window.Owner;
        }

        private DelegateCommand screenPicCommand;
        public DelegateCommand ScreenPicCommand
        {
            get
            {
                if (screenPicCommand == null)
                {
                    screenPicCommand = new DelegateCommand(ScreenPic);
                }
                return screenPicCommand;
            }
        }

        private void ScreenPic()
        {
            IsRecordVisible = false;
            cutter = new Cutter();
            // 隐藏原窗口
            MyWindow.Hide();
            Thread.Sleep(1000);

            // 设置截图窗口的背景图片
            Bitmap bmp = new Bitmap(Screen.AllScreens[0].Bounds.Width, Screen.AllScreens[0].Bounds.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(new System.Drawing.Point(0, 0), new System.Drawing.Point(0, 0), new System.Drawing.Size(bmp.Width, bmp.Height));
            cutter.BackgroundImage = bmp;

            // 显示原窗口
            MyWindow.Show();

            // 显示截图窗口
            cutter.WindowState = FormWindowState.Maximized;
            cutter.ShowDialog();
            ScreenAndOcr();
        }

        public void ScreenAndOcr()
        {
            MyWindow.Dispatcher.Invoke(new Action(delegate
            {
                Task.Run(() =>
                {
                    if (!IsConnectInternet())
                    {
                        System.Windows.MessageBox.Show("网络未链接！");
                        return;
                    }
                    MemoryStream ms = new MemoryStream();
                    catchBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    byte[] bytes = ms.GetBuffer();
                    ms.Close();

                    GetOcrJsonAndPrint(Client, bytes);
                });
            }));

        }

        private DelegateCommand getImageFromNetCommand;
        public DelegateCommand GetImageFromNetCommand
        {
            get
            {
                if (getImageFromNetCommand == null)
                {
                    getImageFromNetCommand = new DelegateCommand(GetImageFromNet);
                }
                return getImageFromNetCommand;
            }
        }

        private void GetImageFromNet()
        {
            if (string.IsNullOrWhiteSpace(ImageUri))
            {
                System.Windows.MessageBox.Show("Uri为空！");
                return;
            }
            Task.Run(() =>
            {
                if (!HttpFileExist(ImageUri))
                {
                    System.Windows.MessageBox.Show("此路径不包含有效图片信息！");
                    return;
                }
                WebRequest request = WebRequest.Create(ImageUri);
                WebResponse response = request.GetResponse();
                Stream s = response.GetResponseStream();
                byte[] data = new byte[1024];
                int length = 0;
                MemoryStream ms = new MemoryStream();
                while ((length = s.Read(data, 0, data.Length)) > 0)
                {
                    ms.Write(data, 0, length);
                }
                byte[] bytes = ms.GetBuffer();
                ms.Close();

                GetOcrJsonAndPrint(Client, bytes);
            });
        }

        public static bool HttpFileExist(string fileUrl)
        {
            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.CreateDefault(new Uri(fileUrl));
                httpWebRequest.Method = "HEAD";
                httpWebRequest.Timeout = 1000;
                using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private DelegateCommand showUriTextBoxCommand;
        public DelegateCommand ShowUriTextBoxCommand
        {
            get
            {
                if (showUriTextBoxCommand == null)
                {
                    showUriTextBoxCommand = new DelegateCommand(ShowUriTextBox);
                }
                return showUriTextBoxCommand;
            }
        }

        private void ShowUriTextBox()
        {
            if (IsUriVisible == Visibility.Hidden)
            {
                IsUriVisible = Visibility.Visible;
                ImageUri = String.Empty;
            }
            else
            {
                IsUriVisible = Visibility.Hidden;
            }
        }

        private DelegateCommand recordShowCommand;
        public DelegateCommand RecordShowCommand
        {
            get
            {
                if (recordShowCommand == null)
                {
                    recordShowCommand = new DelegateCommand(RecordShow);
                }
                return recordShowCommand;
            }
        }

        private void RecordShow()
        {
            if (IsRecordVisible == true)
            {
                IsRecordVisible = false;
            }
            else
            {
                IsRecordVisible = false;
                IsRecordVisible = true;
            }
        }

        private DelegateCommand choosePicAndOcrCommand;
        public DelegateCommand ChoosePicAndOcrCommand
        {
            get
            {
                if (choosePicAndOcrCommand == null)
                {
                    choosePicAndOcrCommand = new DelegateCommand(ChoosePicAndOcr);
                }
                return choosePicAndOcrCommand;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="window"></param>
        private void ChoosePicAndOcr()
        {
            Task.Run(() =>
            {
                if (!IsConnectInternet())
                {
                    System.Windows.MessageBox.Show("网络未链接！");
                    return;
                }
                string imagePath = string.Empty;
                Client.Timeout = 60000;  // 修改超时时间
                Microsoft.Win32.OpenFileDialog openFiledlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "(*.jpg,*.png,*.jpeg,*.bmp,*.gif)|*.jpg;*.png;*.jpeg;*.bmp;*.gif|All files(*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = true
                };
                if (openFiledlg.ShowDialog() == true)
                {
                    imagePath = openFiledlg?.FileName;
                }
                else
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    return;
                }
                var bytes = File.ReadAllBytes(imagePath);

                GetOcrJsonAndPrint(Client, bytes);
            });
        }

        #endregion

        #region Method

        private void GetOcrJsonAndPrint(Baidu.Aip.Ocr.Ocr client, byte[] bytes)
        {
            IsProgressVisibility = Visibility.Visible;
            List<Root> rootList = new List<Root>();
            string jsonStr = string.Empty;
            try
            {
                if (NormalRadioButton == true)
                {
                    jsonStr = JsonConvert.SerializeObject(client.GeneralBasic(bytes));
                }
                else
                {
                    jsonStr = JsonConvert.SerializeObject(client.Accurate(bytes));
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ocr获取结果失败" + ex);
                IsProgressVisibility = Visibility.Collapsed;
                return;
            }
            Root root = null;
            if (!string.IsNullOrWhiteSpace(jsonStr))
            {
                root = JsonConvert.DeserializeObject<Root>(jsonStr);
            }
            OcrResult = string.Empty;
            foreach (var item in root.words_result)
            {
                OcrResult = OcrResult + item.words + "\n";
            }
            IsProgressVisibility = Visibility.Collapsed;
            MyWindow.Dispatcher.Invoke(new Action(()=> 
            {
                AddRecord();
            }));
        }

        private void CreatOcrClient()
        {
            string imagePath = string.Empty;
            try
            {
                Client = new Baidu.Aip.Ocr.Ocr(Constant.API_KEY, Constant.SECRET_KEY);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("创建连接出错" + ex);
            }
        }

        private void AddRecord()
        {
            Record record = new Record()
            {
                RecordTime = DateTime.Now.ToShortTimeString(),
                RecordText = OcrResult
            };
            RecordList.Add(record);
            List<Record> reclist = new List<Record>();
            foreach (var item in RecordList)
            {
                reclist.Add(item);
            }
            reclist.Reverse();
            RecordList.Clear();
            foreach (var item in reclist)
            {
                RecordList.Add(item);
            }
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(int Description, int ReservedValue);

        public bool IsConnectInternet()
        {
            int Description = 0;
            return InternetGetConnectedState(Description, 0);
        }

        #endregion

        #region Entity

        #region Ocr_Words
        
        public class Words_resultItem
        {
            /// <summary>
            /// 
            /// </summary>
            public string words { get; set; }
        }

        public class Root
        {
            /// <summary>
            /// 
            /// </summary>
            public Int64 log_id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int words_result_num { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public List<Words_resultItem> words_result { get; set; }
        }
        #endregion

        #region Record
        public class Record
        {
            public string RecordTime { get; set; }
            public string RecordText { get; set; }
        }
        #endregion

        #endregion
    }
}
