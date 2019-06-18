using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watershed
{
    public class ImageWatershed
    {
        int stride;
        Bitmap bitmap;
        Watershed watershed = new Watershed();

        public void Solve(string imgPath)
        {
            bitmap = new Bitmap(Image.FromFile(imgPath));
            Solve(bitmap);
        }

        public void Solve(Bitmap bitmap)
        {
            List<WatershedElem> watershedElemList = CreateWatershedElemList(bitmap);
            watershed.Stride = stride;
            watershed.Solve(watershedElemList);
        }


        List<WatershedElem> CreateWatershedElemList(Bitmap bitmap)
        {
            List<WatershedElem> watershedElemList = new List<WatershedElem>();
            this.bitmap = bitmap;

            unsafe
            {
                stride = bitmap.Width;
                Rectangle srcBitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

                //将Bitmap锁定到系统内存中,获得BitmapData
                BitmapData srcBmData = bitmap.LockBits(srcBitmapRect,
                    ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

                byte* srcPtr = (byte*)(void*)(srcBmData.Scan0);
                byte* rowHeadPtr;
                byte dist;
                WatershedElem elem;

                for (int i = 0; i < bitmap.Height; i++)
                {
                    rowHeadPtr = srcPtr + srcBmData.Stride * i;

                    for (int j = 0; j < bitmap.Width; j++)
                    {
                        dist = *(rowHeadPtr + j);
                        elem = new WatershedElem(j, i, dist, Watershed.InVaildRegion);
                        watershedElemList.Add(elem);
                    }
                }

                bitmap.UnlockBits(srcBmData);        
            }

            return watershedElemList;
        }

        public Bitmap CreateWatershedBitmap()
        {
            Bitmap watershedBitmap = new Bitmap(bitmap.Width, bitmap.Height);
            Graphics g2 = Graphics.FromImage(watershedBitmap);
            g2.DrawImage(bitmap, 0, 0);

            unsafe
            {
                WatershedElem[] orgWatershedElems = watershed.WatershedElems;
                Rectangle srcBitmapRect = new Rectangle(0, 0, watershedBitmap.Width, watershedBitmap.Height);

                //将Bitmap锁定到系统内存中,获得BitmapData
                BitmapData srcBmData = watershedBitmap.LockBits(srcBitmapRect,
                    ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

                byte* srcPtr = (byte*)(void*)(srcBmData.Scan0);
                byte* curtPtr;
                for (int i = 0; i < orgWatershedElems.Length; i++)
                {
                    if (orgWatershedElems[i].region != Watershed.WatershedLineRegion)
                        continue;

                    curtPtr = srcPtr + srcBmData.Stride * orgWatershedElems[i].y + orgWatershedElems[i].x;
                    *curtPtr = 5;
                }

                watershedBitmap.UnlockBits(srcBmData);
            }

            return watershedBitmap;
        }

    }
}
