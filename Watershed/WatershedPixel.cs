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

        public WatershedPixel(  byte grey, int region)
        {
            this.grey = grey;
            this.region = region;
        }

    }
}
