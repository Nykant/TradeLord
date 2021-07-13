using System;
using System.Collections.Generic;
using System.Text;

namespace TradeMaster6000.Shared
{
    public class ZoneFactors
    {
        public string Name { get; set; }
        public double ExplosiveFactor { get; set; }
        public double PreExplosiveFactor { get; set; }
        public int NoOfCandles { get; set; }
        public double ZoneWidthFactor { get; set; }
        public double PreBaseWidthFactor { get; set; }
        public double TestingFactor { get; set; }
        public double SelfTestingFactor { get; set; }
        public double PreBaseOvershootFactor { get; set; }
    }
}
