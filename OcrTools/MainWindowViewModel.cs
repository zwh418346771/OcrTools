using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace OcrTools
{
    class MainWindowViewModel : BindableBase
    {
        private int sliderValue;
        /// <summary>
        /// 
        /// </summary>
        public int SliderValue
        {
            get { return sliderValue; }
            set { SetProperty(ref sliderValue, value); }
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

                GetOcrJsonAndPrint(client, bytes);
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
            catch (Exception ex)
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
                Microsoft.Win32.OpenFileDialog openFiledlg = new Microsoft.Win32.OpenFileDialog();
                openFiledlg.Filter = "(*.jpg,*.png,*.jpeg,*.bmp,*.gif)|*.jpg;*.png;*.jpeg;*.bmp;*.gif|All files(*.*)|*.*";
                openFiledlg.FilterIndex = 1;
                openFiledlg.RestoreDirectory = true;
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

                GetOcrJsonAndPrint(client,bytes);
            });
        }

        private void GetOcrJsonAndPrint(Baidu.Aip.Ocr.Ocr client,byte[] bytes)
        {
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

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(int Description, int ReservedValue);

        public bool IsConnectInternet()
        {
            int Description = 0;
            return InternetGetConnectedState(Description, 0);
        }

        #region Entity
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
    }
}
