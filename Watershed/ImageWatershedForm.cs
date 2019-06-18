using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Watershed
{
    public partial class ImageWatershedForm : Form
    {
        ImageWatershed imageWatershed;

        public ImageWatershedForm()
        {
            InitializeComponent();

            saveImgDialog.Filter = "png图像文件(*.png)|*.png|jpeg图像文件(*.jpg)|*.jpg";

            imageWatershed = new ImageWatershed();
        }

        private void btnSelectImg_Click(object sender, EventArgs e)
        {
            DialogResult result = openImgFileDialog.ShowDialog();

            if(result == DialogResult.OK)
            {
                textBoxImgPath.Text = openImgFileDialog.FileName;
                imgOrg.Image = Image.FromFile(textBoxImgPath.Text);
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            DateTime startTime = DateTime.Now;
            imageWatershed.Solve(new Bitmap(imgOrg.Image));
            DateTime endTime = DateTime.Now;
            double tm = ExecDateDiff(startTime, endTime);

            imgWatershed.Image = imageWatershed.CreateWatershedBitmap();

            MessageBox.Show(this, "花费时间:" + tm + "毫秒.");
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (imgWatershed.Image != null)
            {
                DialogResult result = saveImgDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    imgWatershed.Image.Save(saveImgDialog.FileName);
                    MessageBox.Show(this, "文件保存成功.");
                }
            }
        }


        public double ExecDateDiff(DateTime dateBegin, DateTime dateEnd)
        {
            TimeSpan ts1 = new TimeSpan(dateBegin.Ticks);
            TimeSpan ts2 = new TimeSpan(dateEnd.Ticks);
            TimeSpan ts3 = ts1.Subtract(ts2).Duration();
            //你想转的格式
            return ts3.TotalMilliseconds;
        }
    }
}
