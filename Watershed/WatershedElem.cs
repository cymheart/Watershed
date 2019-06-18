using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watershed
{
    public class WatershedElem
    {
        public int x;
        public int y;
        public int dist;
        public int region = 0;
        public LinkedListNode<WatershedElem> node;
        public List<WatershedElem> neighbourElemList = new List<WatershedElem>(8);
        public int curtNeighbourIdx = 0;

        public WatershedElem(int x, int y, int dist, int region = 0)
        {
            this.x = x;
            this.y = y;
            this.dist = dist;
            this.region = region;
        }
    }
}
