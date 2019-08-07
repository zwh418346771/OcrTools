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

        public MainWindowViewModel()
        {
            NormalRadioButton = true;
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
                OcrResult = string.Empty;
                var API_KEY = "gGRQ28PMwDf6nStqSND7Pa3A";
                var SECRET_KEY = "u9j4NdWAGwK7kKjL7GWt5qmSe4EnCcYw";
                string imagePath = string.Empty;
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
                client.Timeout = 60000;  // 修改超时时间
                Microsoft.Win32.OpenFileDialog openFiledlg = new Microsoft.Win32.OpenFileDialog();
                openFiledlg.Filter = "(*.jpg,*.png,*.jpeg,*.bmp,*.gif)|*.jpg;*.png;*.jpeg;*.bmp;*.gif|All files(*.*)|*.*";
                openFiledlg.FilterIndex = 1;
                openFiledlg.RestoreDirectory = true;
                if (openFiledlg.ShowDialog() == true)
                {
                    imagePath = openFiledlg?.FileName;
                }

                var image = File.ReadAllBytes(imagePath);

                List<Root> rootList = new List<Root>();
                string jsonStr = string.Empty;
                try
                {
                    if (NormalRadioButton == true)
                    {
                        jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(client.GeneralBasic(image));
                    }
                    else
                    {
                        jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(client.Accurate(image));
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

                foreach (var item in root.words_result)
                {
                    OcrResult = OcrResult + item.words + "\n";
                }
            });
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(int Description, int ReservedValue);

        public bool IsConnectInternet()
        {
            int Description = 0;
            return InternetGetConnectedState(Description, 0);
        }

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
    }
}
