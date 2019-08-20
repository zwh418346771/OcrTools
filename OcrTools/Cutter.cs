using System;
using System.Drawing;
using System.Windows.Forms;

namespace OcrTools
{
    public partial class Cutter : Form
    {
        private bool isCatchStart = false;

        // 截图起点
        private Point startPoint;
        // 矩形起点
        private int rectX;
        private int rectY;
        // 矩形宽高
        private int width;
        private int height;

        // 确认按钮
        private Button btnOK = null;
        // 取消按钮
        private Button btnCancel = null;

        private Bitmap bmp;
        public Cutter()
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;

            // 双缓冲
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint |
                           ControlStyles.AllPaintingInWmPaint,
                           true);
            this.UpdateStyles();

            // 鼠标样式
            Cursor = Cursors.Cross;
            this.ClientSize = new Size(837, 513);
            this.Name = "Cutter";
            this.MouseDown += new MouseEventHandler(this.Cutter_MouseDown);
            this.MouseMove += new MouseEventHandler(this.Cutter_MouseMove);
            this.MouseUp += new MouseEventHandler(this.Cutter_MouseUp);
            this.ResumeLayout(false);
        }
        // 鼠标按下
        private void Cutter_MouseDown(object sender, MouseEventArgs e)
        {
            Controls.Clear();
            if (e.Button == MouseButtons.Left)
            {
                if (!isCatchStart)
                {
                    isCatchStart = true;

                    startPoint = new Point(e.X, e.Y);
                }
            }
        }
        // 鼠标移动
        private void Cutter_MouseMove(object sender, MouseEventArgs e)
        {
            if (isCatchStart)
            {
                // 初始化矩形区域
                rectX = Math.Min(startPoint.X, e.X);
                rectY = Math.Min(startPoint.Y, e.Y);
                width = Math.Abs(e.X - startPoint.X);
                height = Math.Abs(e.Y - startPoint.Y);

                Rectangle rect = new Rectangle(rectX, rectY, width, height);
                Pen pen = new Pen(Color.Gray, 1);

                Invalidate();
                Update();
                Graphics g = CreateGraphics();
                g.DrawRectangle(pen, rect);
            }
        }
        // 鼠标松开
        private void Cutter_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
                {
                    isCatchStart = false;

                    Update();

                    // 保存图片到图片框
                    bmp = new Bitmap(width, height);
                    Graphics g = Graphics.FromImage(bmp);
                    g.DrawImage(BackgroundImage, new Rectangle(0, 0, width, height), new Rectangle(rectX, rectY, width, height), GraphicsUnit.Pixel);
                    MainWindowViewModel.catchBmp = bmp;

                    // 确定按钮
                    btnOK = new Button();
                    btnOK.Location = new Point(e.X, e.Y);
                    btnOK.Size = new Size(50, 25);
                    btnOK.Text = "确认";
                    btnOK.Click += (sende, ee) => DialogResult = DialogResult.OK;
                    Controls.Add(btnOK);
                    Update();
                    // 取消按钮
                    btnCancel = new Button();
                    btnCancel.Location = new Point(e.X + 50, e.Y);
                    btnCancel.Size = new Size(50, 25);
                    btnCancel.Text = "取消";
                    btnCancel.Click += BtnCancel_Click;
                    Controls.Add(btnCancel);
                    Update();

                    // 绘制矩形区域
                    Rectangle rect = new Rectangle(rectX, rectY, width, height);
                    Pen pen = new Pen(Color.Gray, 1);
                    g = this.CreateGraphics();
                    g.DrawRectangle(pen, rect);
                    Cursor = Cursors.Default;
                }
            }
            catch (Exception)
            {
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Close();
            bmp.Dispose();
        }
    }
}
