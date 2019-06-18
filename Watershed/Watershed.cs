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
        public const int WatershedLineRegion = -2;

        /// <summary>
        /// 无效区域
        /// </summary>
        public const int InVaildRegion = 0;

        /// <summary>
        /// 跨距
        /// </summary>
        public int Stride = 0;

        WatershedElem[] orgWatershedElems;
        LinkedList<WatershedElem> watershedElemLinkedList = new LinkedList<WatershedElem>(); 
        int maxRegion = 0;

        Stack<WatershedElem> searchElemStack = new Stack<WatershedElem>();
        List<List<WatershedElem>> waterElemRegionList = new List<List<WatershedElem>>(500);
        List<List<WatershedElem>> backWaterElemRegionList = new List<List<WatershedElem>>(500);


        public WatershedElem[] WatershedElems
        {
            get
            {
                return orgWatershedElems;
            }
        }

        public void Solve(List<WatershedElem> watershedElemList)
        {
            maxRegion = 0;
            searchElemStack.Clear();
            CreateWatershedElemDataGroup(watershedElemList);
            _Solve();
        }

        void CreateWatershedElemDataGroup(List<WatershedElem> watershedElemList)
        {
            orgWatershedElems = watershedElemList.ToArray();
            watershedElemList.Sort((a, b) => a.dist.CompareTo(b.dist));
            CreateWatershedElemLinkedList(watershedElemList);
        }

        void CreateWatershedElemLinkedList(List<WatershedElem> watershedElemList)
        {
            for (int i = 0; i < watershedElemList.Count; i++)
            {
                watershedElemLinkedList.AddLast(watershedElemList[i]);
                watershedElemList[i].node = watershedElemLinkedList.Last;
            }
        }

        void _Solve()
        {
            LinkedListNode<WatershedElem> node;

            while (true)
            {
                node = watershedElemLinkedList.First;
                if (node == null)
                    break;

                Run(node);
            }
        }

        void Run(LinkedListNode<WatershedElem> node)
        {
            WatershedElem elem;
            int curtRegion = node.Value.region;
            int curtdist = node.Value.dist;

            //获取已定义的所有区域
            for (; node != null; node = node.Next)
            {
                elem = node.Value;

                if (elem.dist > curtdist)
                    break;

                if (elem.region == InVaildRegion)
                    continue;

                if (elem.region == WatershedLineRegion)
                {
                    watershedElemLinkedList.Remove(elem.node);
                    elem.node = null;
                    continue;
                }

                AddWatershedElemToRegionList(elem);
            }

            SpreadRegions();


            //获取没有定义的区域
            node = watershedElemLinkedList.First;

            while (node != null)
            {
                elem = node.Value;

                if (elem.dist > curtdist)
                    break;

                if (elem.region == InVaildRegion)
                {
                    curtRegion++;
                    elem.region = curtRegion;
                    CreateNeighbourElems(elem);
                    DeepSearchElem(elem);
                }

                node = watershedElemLinkedList.First;
            }
        }

        /// <summary>
        /// 添加已定义的区域到列表
        /// </summary>
        /// <param name="elem"></param>
        void AddWatershedElemToRegionList(WatershedElem elem)
        {
            CreatePreRegion(elem.region);

            if (waterElemRegionList[elem.region] == null)
            {
                waterElemRegionList[elem.region] = new List<WatershedElem>(500);
                backWaterElemRegionList[elem.region] = new List<WatershedElem>(500);
            }

            waterElemRegionList[elem.region].Add(elem);

            if (maxRegion < elem.region + 1)
                maxRegion = elem.region + 1;
        }

        void CreatePreRegion(int region)
        {
            if (region < waterElemRegionList.Count)
                return;

            int count = region + 1 - waterElemRegionList.Count + 100;
            for (int i = 0; i < count; i++)
            {
                waterElemRegionList.Add(null);
                backWaterElemRegionList.Add(null);
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
                    if (waterElemRegionList[i] == null || 
                        waterElemRegionList[i].Count == 0)
                    {
                        count++;
                        continue;
                    }

                    SpreadSearchElem(i);
                }

                if (count == maxRegion)
                    break;
            }
        }


        void SpreadSearchElem(int region)
        {
            WatershedElem centerElem, neighbourElem;

            for (int i = 0; i < waterElemRegionList[region].Count; i++)
            {
                centerElem = waterElemRegionList[region][i];
                CreateNeighbourElems(centerElem);

                if (centerElem.region == WatershedLineRegion && centerElem.node != null)
                {           
                    watershedElemLinkedList.Remove(centerElem.node);
                    centerElem.node = null;
                    continue;
                }

                for (int j = 0; j < centerElem.neighbourElemList.Count; j++)
                {
                    neighbourElem = centerElem.neighbourElemList[j];

                    if (neighbourElem.dist == centerElem.dist)
                    {
                        if (neighbourElem.region == InVaildRegion)
                        {
                            neighbourElem.region = centerElem.region;
                            CreateNeighbourElems(neighbourElem);
                            backWaterElemRegionList[region].Add(neighbourElem);
                        }
                        else if (neighbourElem.region > InVaildRegion &&
                            neighbourElem.region != centerElem.region)  //不同区域汇集处
                        {
                            neighbourElem.region = WatershedLineRegion;
                        }
                    }
                    else if(neighbourElem.dist > centerElem.dist)
                    {
                        neighbourElem.region = centerElem.region;
                    }
                }

                if (centerElem.node != null)
                {
                    watershedElemLinkedList.Remove(centerElem.node);
                    centerElem.node = null;
                }
            }

            List<WatershedElem> tmp = waterElemRegionList[region];
            waterElemRegionList[region] = backWaterElemRegionList[region];
            backWaterElemRegionList[region] = tmp;
            tmp.Clear();
        }


        /// <summary>
        /// 深度搜索扩散区域边缘(区域会最大化扩散自己的边缘)
        /// </summary>
        /// <param name="elem"></param>
        void DeepSearchElem(WatershedElem elem)
        {
            WatershedElem neighbourElem;
      
            while (true)
            {
                for(int i = elem.curtNeighbourIdx; i < elem.neighbourElemList.Count; i++)
                {
                    neighbourElem = elem.neighbourElemList[i];

                    if (neighbourElem.region == WatershedLineRegion && neighbourElem.node != null)
                    {
                        watershedElemLinkedList.Remove(neighbourElem.node);
                        neighbourElem.node = null;
                        continue;
                    }

                    if (neighbourElem.dist == elem.dist)
                    {
                        if (neighbourElem.region == InVaildRegion)
                        {
                            neighbourElem.region = elem.region;
                            CreateNeighbourElems(neighbourElem);
                            elem.curtNeighbourIdx = i + 1;
                            searchElemStack.Push(elem);
                            elem = neighbourElem;
                            i = elem.curtNeighbourIdx - 1;                    
                            continue;
                        }
                        else if(neighbourElem.region > InVaildRegion && 
                            neighbourElem.region != elem.region)     //不同区域汇集处
                        {
                            neighbourElem.region = WatershedLineRegion;   
                        }
                    }
                    else
                    {
                        neighbourElem.region = elem.region;
                    }
                }

                if (elem.node != null)
                {
                    watershedElemLinkedList.Remove(elem.node);
                    elem.node = null;
                }

                if (searchElemStack.Count == 0)
                    break;

                elem = searchElemStack.Pop();
            }
        }

        void CreateNeighbourElems(WatershedElem centerElem)
        {
            List<WatershedElem> neighbourElemList = centerElem.neighbourElemList;

            if (neighbourElemList == null || neighbourElemList.Count != 0)
                return;

            int x = centerElem.x;
            int y = centerElem.y;
            int centerIdx = y * Stride + x;

            int idx2 = centerIdx - Stride - 1;
            if (IsVaildNeighbourElemIdx(idx2, x - 1, y - 1))
                neighbourElemList.Add(orgWatershedElems[idx2]);

            int idx3 = idx2 + 1;
            if (IsVaildNeighbourElemIdx(idx3, x, y - 1))
                neighbourElemList.Add(orgWatershedElems[idx3]);

            int idx4 = idx3 + 1;
            if (IsVaildNeighbourElemIdx(idx4, x + 1, y - 1))
                neighbourElemList.Add(orgWatershedElems[idx4]);

            int idx5 = centerIdx + 1;
            if (IsVaildNeighbourElemIdx(idx5, x + 1, y))
                neighbourElemList.Add(orgWatershedElems[idx5]);

            int idx6 = centerIdx + Stride + 1;
            if (IsVaildNeighbourElemIdx(idx6, x + 1, y + 1))
                neighbourElemList.Add(orgWatershedElems[idx6]);

            int idx7 = idx6 - 1;
            if (IsVaildNeighbourElemIdx(idx7, x, y + 1))
                neighbourElemList.Add(orgWatershedElems[idx7]);

            int idx0 = idx7 - 1;
            if (IsVaildNeighbourElemIdx(idx0, x - 1, y + 1))
                neighbourElemList.Add(orgWatershedElems[idx0]);

            int idx1 = centerIdx - 1;
            if (IsVaildNeighbourElemIdx(idx1, x - 1, y))
                neighbourElemList.Add(orgWatershedElems[idx1]);   
        }

        bool IsVaildNeighbourElemIdx(int idx, int x, int y)
        {
            if (idx < 0 || idx >= orgWatershedElems.Length)
                return false;

            if (orgWatershedElems[idx].y != y || orgWatershedElems[idx].x != x)
                return false;

            return true;
        }
    }
}
