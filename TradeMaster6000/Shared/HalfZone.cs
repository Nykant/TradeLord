using System;
using System.Collections.Generic;
using System.Text;

namespace TradeMaster6000.Shared
{
    public class HalfZone
    {
        public ExplosiveCandle ExplosiveCandle { get; set; }
        public decimal Top { get; set; }
        public decimal Bottom { get; set; }
        public decimal BiggestBaseDiff { get; set; }
    }
}
