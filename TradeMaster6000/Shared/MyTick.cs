using System;
using System.Collections.Generic;
using System.Text;

namespace TradeMaster6000.Shared
{
    public class MyTick
    {
        public int Id { get; set; }
        public decimal LTP { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public uint InstrumentToken { get; set; }
    }
}
