using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watershed
{
    public class WatershedPixel
    {
        public int x;
        public int y;
        public byte grey;
        public int region;
        public LinkedListNode<WatershedPixel> node;
        public List<WatershedPixel> neighbourWPixelList = new List<WatershedPixel>(8);
        public int curtNeighbourIdx = 0;

        public WatershedPixel(int x, int y, byte grey, int region)
        {
            this.x = x;
            this.y = y;
            this.grey = grey;
            this.region = region;
        }

    }
}
