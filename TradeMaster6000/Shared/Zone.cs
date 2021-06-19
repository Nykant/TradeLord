using System;
using System.Collections.Generic;
using System.Text;

namespace TradeMaster6000.Shared
{
    public class Zone
    {
        public int Id { get; set; }
        public int EndIndex { get; set; }
        public string InstrumentSymbol { get; set; }
        public decimal Top { get; set; }
        public decimal Bottom { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
