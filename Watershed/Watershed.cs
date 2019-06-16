using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watershed
{
    public class Watershed
    {
        WatershedPixel[] orgWatershedPixes;
        LinkedList<WatershedPixel> watershedPixelLinkedList = new LinkedList<WatershedPixel>();
        int stride;

        List<WatershedPixel> cacheNeighbourWPixelList = new List<WatershedPixel>();

        void CreateWatershedPixelList(Bitmap bitmap)
        {
            unsafe
            {
                stride = bitmap.Width;
                Rectangle srcBitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

                //将Bitmap锁定到系统内存中,获得BitmapData
                BitmapData srcBmData = bitmap.LockBits(srcBitmapRect,
                    ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

                byte* srcPtr = (byte*)(void*)(srcBmData.Scan0);
                byte* rowHeadPtr;
                byte grey;
                WatershedPixel wPixel;
                List<WatershedPixel> watershedPixelList = new List<WatershedPixel>();

                for (int i = 0; i < bitmap.Height; i++)
                {
                    rowHeadPtr = srcPtr + srcBmData.Stride * i;

                    for (int j = 0; j < bitmap.Width; j++)
                    {
                        grey = *(rowHeadPtr + j);

                        wPixel = new WatershedPixel(grey, 0);
                        watershedPixelList.Add(wPixel);
                    }
                }

                orgWatershedPixes = watershedPixelList.ToArray();
                watershedPixelList.Sort((a, b) => a.grey.CompareTo(b.grey));
                CreateWatershedPixelLinkedList(watershedPixelList);

                bitmap.UnlockBits(srcBmData);
            }
        }

        void CreateWatershedPixelLinkedList(List<WatershedPixel> watershedPixelList)
        {
            for(int i=0; i< watershedPixelList.Count; i++)
            {
                watershedPixelLinkedList.AddLast(watershedPixelList[i]);
            }
        }


        void dd()
        {


        }



        List<WatershedPixel> GetNeighbourWPixels(WatershedPixel centerWPixel)
        {
            cacheNeighbourWPixelList.Clear();

            int x = centerWPixel.x;
            int y = centerWPixel.y;
            int centerIdx = y * stride + x;

            int idx2 = centerIdx - stride - 1;
            if (IsVaildNeighbourWPixelIdx(idx2, x - 1, y - 1))
                cacheNeighbourWPixelList.Add(orgWatershedPixes[idx2]);

            int idx3 = idx2 + 1;
            if (IsVaildNeighbourWPixelIdx(idx3, x, y - 1))
                cacheNeighbourWPixelList.Add(orgWatershedPixes[idx3]);

            int idx4 = idx3 + 1;
            if (IsVaildNeighbourWPixelIdx(idx4, x + 1, y - 1))
                cacheNeighbourWPixelList.Add(orgWatershedPixes[idx4]);

            int idx5 = centerIdx + 1;
            if (IsVaildNeighbourWPixelIdx(idx5, x + 1, y))
                cacheNeighbourWPixelList.Add(orgWatershedPixes[idx5]);

            int idx6 = centerIdx + stride + 1;
            if (IsVaildNeighbourWPixelIdx(idx6, x + 1, y + 1))
                cacheNeighbourWPixelList.Add(orgWatershedPixes[idx6]);

            int idx7 = idx6 - 1;
            if (IsVaildNeighbourWPixelIdx(idx7, x, y + 1))
                cacheNeighbourWPixelList.Add(orgWatershedPixes[idx7]);

            int idx0 = idx7 - 1;
            if (IsVaildNeighbourWPixelIdx(idx0, x - 1, y + 1))
                cacheNeighbourWPixelList.Add(orgWatershedPixes[idx0]);

            int idx1 = centerIdx - 1;
            if (IsVaildNeighbourWPixelIdx(idx1, x - 1, y))
                cacheNeighbourWPixelList.Add(orgWatershedPixes[idx1]);

            return cacheNeighbourWPixelList;
        }

        bool IsVaildNeighbourWPixelIdx(int idx, int x, int y)
        {
            if (idx < 0 || idx >= orgWatershedPixes.Length)
                return false;

            if (orgWatershedPixes[idx].y != y || orgWatershedPixes[idx].x != x)
                return false;

            return true;
        }



    }
}
