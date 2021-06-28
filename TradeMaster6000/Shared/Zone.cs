using System;
using System.Collections.Generic;
using System.Text;

namespace TradeMaster6000.Shared
{
    public class Zone
    {
        public int Id { get; set; }
        public string InstrumentSymbol { get; set; }
        public decimal Top { get; set; }
        public decimal Bottom { get; set; }
        public ZoneType ZoneType { get; set; }
        public StayAway StayAway { get; set; }
        public bool Tested { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
    public enum ZoneType
    {
        RBR,
        RBD,
        DBR,
        DBD
    }
    public enum StayAway
    {
        Bad,
        Good
    }
}
