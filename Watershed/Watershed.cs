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

        Stack<WatershedPixel> searchPixelStack = new Stack<WatershedPixel>();

        List<List<WatershedPixel>> waterPixelRegionList = new List<List<WatershedPixel>>(500);
        List<List<WatershedPixel>> backWaterPixelRegionList = new List<List<WatershedPixel>>(500);

        void CreateWatershedPixelList(Bitmap bitmap)
        {
            unsafe
            {
                stride = bitmap.Width;
                Rectangle srcBitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

                ////将Bitmap锁定到系统内存中,获得BitmapData
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
                watershedPixelList[i].node = watershedPixelLinkedList.Last;
            }
        }


        void Run(int curtRegion)
        {
            WatershedPixel wpixel;
            LinkedListNode<WatershedPixel> node;

            //获取已定义的所有区域
            if (curtRegion != 0)
            {
                node = watershedPixelLinkedList.First;
                for (; node != null;  node = node.Next)
                {
                    wpixel = node.Value;
                    if (wpixel.region == 0)
                    {
                        node = node.Next;
                        continue;
                    }

                    AddWatershedPixelToRegionList(wpixel);
                }

                SpreadRegions();

            }

            //获取没有定义的区域
            node = watershedPixelLinkedList.First;
            while (node != null)
            {
                wpixel = node.Value;

                if (wpixel.region == 0)
                {
                    curtRegion++;
                    wpixel.region = curtRegion;
                    DeepSearchPixel(wpixel);
                }

                node = watershedPixelLinkedList.First;
            }

        }

        /// <summary>
        /// 添加已定义的区域到列表
        /// </summary>
        /// <param name="wpixel"></param>
        void AddWatershedPixelToRegionList(WatershedPixel wpixel)
        {
            if(waterPixelRegionList[wpixel.region] == null)
            {
                waterPixelRegionList[wpixel.region] = new List<WatershedPixel>(1000);
                backWaterPixelRegionList[wpixel.region] = new List<WatershedPixel>(1000);
            }

            waterPixelRegionList[wpixel.region].Add(wpixel);
        }


        /// <summary>
        /// 逐步增加区域轮廓大小(多个区域同时轮流交替增长边缘)
        /// </summary>
        void SpreadRegions()
        {
            int count = 0;

            while (true)
            {   
                for (int i = 0; i < waterPixelRegionList.Count; i++)
                {
                    if (waterPixelRegionList[i].Count == 0)
                    {
                        count++;
                        continue;
                    }

                    SpreadSearchPixel(i);
                }

                if (count == waterPixelRegionList.Count)
                    break;
            }
        }


        void SpreadSearchPixel(int region)
        {
            WatershedPixel centerPixel, neighbourPixel;

            for (int i = 0; i < waterPixelRegionList[region].Count; i++)
            {
                centerPixel = waterPixelRegionList[region][i];
                CreateNeighbourWPixels(centerPixel);

                for (int j = 0; j < centerPixel.neighbourWPixelList.Count; i++)
                {
                    neighbourPixel = centerPixel.neighbourWPixelList[j];

                    if (neighbourPixel.grey == centerPixel.grey && neighbourPixel.region == 0)
                    {
                        neighbourPixel.region = centerPixel.region;
                        CreateNeighbourWPixels(neighbourPixel);
                        backWaterPixelRegionList[region].Add(neighbourPixel);
                    }
                }

                watershedPixelLinkedList.Remove(centerPixel.node);
            }

            List<WatershedPixel> tmp = waterPixelRegionList[region];
            waterPixelRegionList[region] = backWaterPixelRegionList[region];
            backWaterPixelRegionList[region] = tmp;
            tmp.Clear();
        }


        /// <summary>
        /// 深度搜索扩散区域边缘(区域会最大化扩散自己的边缘)
        /// </summary>
        /// <param name="wpixel"></param>
        void DeepSearchPixel(WatershedPixel wpixel)
        {
            WatershedPixel neighbourPixel;
      
            while (true)
            {
                for(int i = wpixel.curtNeighbourIdx; i < wpixel.neighbourWPixelList.Count; i++)
                {
                    neighbourPixel = wpixel.neighbourWPixelList[i];
                    neighbourPixel.region = wpixel.region;

                    if (neighbourPixel.grey == wpixel.grey && neighbourPixel.region == 0)
                    {
                        neighbourPixel.region = wpixel.region;
                        CreateNeighbourWPixels(neighbourPixel);
                        wpixel.curtNeighbourIdx = i + 1;
                        searchPixelStack.Push(wpixel);
                        wpixel = neighbourPixel;
                        break;
                    }   
                    else if(neighbourPixel.region == 0)
                    {
                        neighbourPixel.region = wpixel.region;
                    }
                }

                watershedPixelLinkedList.Remove(wpixel.node);
                wpixel = searchPixelStack.Pop();
            }
        }



        void CreateNeighbourWPixels(WatershedPixel centerWPixel)
        {
            List<WatershedPixel> neighbourWPixelList = centerWPixel.neighbourWPixelList;

            if (neighbourWPixelList == null || neighbourWPixelList.Count != 0)
                return;

            int x = centerWPixel.x;
            int y = centerWPixel.y;
            int centerIdx = y * stride + x;

            int idx2 = centerIdx - stride - 1;
            if (IsVaildNeighbourWPixelIdx(idx2, x - 1, y - 1))
                neighbourWPixelList.Add(orgWatershedPixes[idx2]);

            int idx3 = idx2 + 1;
            if (IsVaildNeighbourWPixelIdx(idx3, x, y - 1))
                neighbourWPixelList.Add(orgWatershedPixes[idx3]);

            int idx4 = idx3 + 1;
            if (IsVaildNeighbourWPixelIdx(idx4, x + 1, y - 1))
                neighbourWPixelList.Add(orgWatershedPixes[idx4]);

            int idx5 = centerIdx + 1;
            if (IsVaildNeighbourWPixelIdx(idx5, x + 1, y))
                neighbourWPixelList.Add(orgWatershedPixes[idx5]);

            int idx6 = centerIdx + stride + 1;
            if (IsVaildNeighbourWPixelIdx(idx6, x + 1, y + 1))
                neighbourWPixelList.Add(orgWatershedPixes[idx6]);

            int idx7 = idx6 - 1;
            if (IsVaildNeighbourWPixelIdx(idx7, x, y + 1))
                neighbourWPixelList.Add(orgWatershedPixes[idx7]);

            int idx0 = idx7 - 1;
            if (IsVaildNeighbourWPixelIdx(idx0, x - 1, y + 1))
                neighbourWPixelList.Add(orgWatershedPixes[idx0]);

            int idx1 = centerIdx - 1;
            if (IsVaildNeighbourWPixelIdx(idx1, x - 1, y))
                neighbourWPixelList.Add(orgWatershedPixes[idx1]);   
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
