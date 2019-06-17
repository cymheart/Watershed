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
        /// <summary>
        /// 分水岭分界线
        /// </summary>
        const int WatershedLineRegion = -2;

        /// <summary>
        /// 无效区域
        /// </summary>
        const int InVaildRegion = 0;

        WatershedPixel[] orgWatershedPixes;
        LinkedList<WatershedPixel> watershedPixelLinkedList = new LinkedList<WatershedPixel>();
        int stride;
        int maxRegion = 0;

        Stack<WatershedPixel> searchPixelStack = new Stack<WatershedPixel>();

        List<List<WatershedPixel>> waterPixelRegionList = new List<List<WatershedPixel>>(500);
        List<List<WatershedPixel>> backWaterPixelRegionList = new List<List<WatershedPixel>>(500);

        public void Create(string imgPath)
        {
            Bitmap bitmap = new Bitmap(Image.FromFile(imgPath));
            CreateWatershedPixelList(bitmap);
        }

        public void CreateWatershedPixelList(Bitmap bitmap)
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
                        wPixel = new WatershedPixel(j, i, grey, 0);
                        watershedPixelList.Add(wPixel);
                    }
                }

                orgWatershedPixes = watershedPixelList.ToArray();
                watershedPixelList.Sort((a, b) => a.grey.CompareTo(b.grey));
                CreateWatershedPixelLinkedList(watershedPixelList);

                Run();

                byte* curtPtr;
                for (int i = 0; i < orgWatershedPixes.Length; i++)
                {
                    if (orgWatershedPixes[i].region != WatershedLineRegion)
                        continue;

                    curtPtr = srcPtr + srcBmData.Stride * orgWatershedPixes[i].y + orgWatershedPixes[i].x;
                    *curtPtr = 5;
                }

                bitmap.UnlockBits(srcBmData);

                bitmap.Save("d:\\test.png", ImageFormat.Png);
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

        void Run()
        {
            LinkedListNode<WatershedPixel> node;

            while (true)
            {
                node = watershedPixelLinkedList.First;
                if (node == null)
                    break;

                Run(node);
            }
        }


        void Run(LinkedListNode<WatershedPixel> node)
        {
            WatershedPixel wpixel;
            int curtRegion = node.Value.region;
            int curtGrey = node.Value.grey;

            //获取已定义的所有区域
            for (; node != null; node = node.Next)
            {
                wpixel = node.Value;

                if (wpixel.grey > curtGrey)
                    break;

                if (wpixel.region == InVaildRegion)
                    continue;

                if(wpixel.region == WatershedLineRegion)
                {
                    watershedPixelLinkedList.Remove(wpixel.node);
                    wpixel.node = null;
                    continue;
                }
  
                AddWatershedPixelToRegionList(wpixel);
            }

            SpreadRegions();


            //获取没有定义的区域
            node = watershedPixelLinkedList.First;
            while (node != null)
            {
                wpixel = node.Value;

                if (wpixel.grey > curtGrey)
                    break;

                if (wpixel.region == InVaildRegion)
                {
                    curtRegion++;
                    wpixel.region = curtRegion;
                    CreateNeighbourWPixels(wpixel);
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
            CreatePreRegion(wpixel.region);

            if (waterPixelRegionList[wpixel.region] == null)
            {
                waterPixelRegionList[wpixel.region] = new List<WatershedPixel>(500);
                backWaterPixelRegionList[wpixel.region] = new List<WatershedPixel>(500);
            }

            waterPixelRegionList[wpixel.region].Add(wpixel);

            if (maxRegion < wpixel.region + 1)
                maxRegion = wpixel.region + 1;
        }

        void CreatePreRegion(int region)
        {
            if (region < waterPixelRegionList.Count)
                return;

            int count = region + 1 - waterPixelRegionList.Count + 100;
            for (int i = 0; i < count; i++)
            {
                waterPixelRegionList.Add(null);
                backWaterPixelRegionList.Add(null);
            }
        }


        /// <summary>
        /// 逐步增加区域轮廓大小(多个区域同时轮流交替增长边缘)
        /// </summary>
        void SpreadRegions()
        {
            int count = 0;

            while (true)
            {
                count = 0;

                for (int i = 0; i < maxRegion; i++)
                {
                    if (waterPixelRegionList[i] == null || 
                        waterPixelRegionList[i].Count == 0)
                    {
                        count++;
                        continue;
                    }

                    SpreadSearchPixel(i);
                }

                if (count == maxRegion)
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

                if (centerPixel.region == WatershedLineRegion)
                {
                    watershedPixelLinkedList.Remove(centerPixel.node);
                    centerPixel.node = null;
                    continue;
                }

                for (int j = 0; j < centerPixel.neighbourWPixelList.Count; j++)
                {
                    neighbourPixel = centerPixel.neighbourWPixelList[j];

                    if (neighbourPixel.grey == centerPixel.grey)
                    {
                        if (neighbourPixel.region == InVaildRegion)
                        {
                            neighbourPixel.region = centerPixel.region;
                            CreateNeighbourWPixels(neighbourPixel);
                            backWaterPixelRegionList[region].Add(neighbourPixel);
                        }
                        else if (neighbourPixel.region > InVaildRegion &&
                            neighbourPixel.region != centerPixel.region)  //不同区域汇集处
                        {
                            neighbourPixel.region = WatershedLineRegion;
                        }
                    }
                    else if(neighbourPixel.grey > centerPixel.grey)
                    {
                        neighbourPixel.region = centerPixel.region;
                    }
                }

                if (centerPixel.node != null)
                {
                    watershedPixelLinkedList.Remove(centerPixel.node);
                    centerPixel.node = null;
                }
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
                    
                    if(neighbourPixel.grey == wpixel.grey)
                    {
                        if (neighbourPixel.region == InVaildRegion)
                        {
                            neighbourPixel.region = wpixel.region;
                            CreateNeighbourWPixels(neighbourPixel);
                            wpixel.curtNeighbourIdx = i + 1;
                            searchPixelStack.Push(wpixel);
                            wpixel = neighbourPixel;
                            i = wpixel.curtNeighbourIdx - 1;                    
                            continue;
                        }
                        else if(neighbourPixel.region != wpixel.region)     //不同区域汇集处
                        {
                            neighbourPixel.region = WatershedLineRegion;   
                        }
                    }
                    else
                    {
                        neighbourPixel.region = wpixel.region;
                    }
                }

                if (wpixel.node != null)
                {
                    watershedPixelLinkedList.Remove(wpixel.node);
                    wpixel.node = null;
                }

                if (searchPixelStack.Count == 0)
                    break;

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
