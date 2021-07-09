using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TradeMaster6000.Shared
{
    public class Candle
    {
        [Key]
        public int Id { get; set; }
        public uint InstrumentToken { get; set; }
        public decimal Open { get; set; }
        public decimal Low { get; set; }
        public decimal High { get; set; }
        public decimal Close { get; set; }
        public int TicksCount { get; set; }
        public int Timeframe { get; set; } = 1;
        public bool Used { get; set; } = false;
        public bool UsedBy5 { get; set; } = false;
        public bool UsedBy15 { get; set; } = false;
        public bool UsedBy30 { get; set; } = false;
        public bool UsedBy45 { get; set; } = false;
        public bool UsedBy60 { get; set; } = false;
        public DateTime Timestamp { get; set; }
        public DateTime Kill { get; set; }
    }
}
