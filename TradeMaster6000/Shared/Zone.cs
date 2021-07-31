using System;
using System.Collections.Generic;
using System.Text;

namespace TradeMaster6000.Shared
{
    public class Zone
    {
        public int Id { get; set; }
        public string InstrumentSymbol { get; set; }
        public uint InstrumentToken { get; set; }
        public decimal Top { get; set; }
        public decimal Bottom { get; set; }
        public ZoneType ZoneType { get; set; }
        public StayAway StayAway { get; set; }
        public SupplyDemand SupplyDemand { get; set; }
        public int Timeframe { get; set; }
        public DateTime Created { get; set; }
        public bool Entry { get; set; }
        public bool Tested { get; set; } = false;
        public bool Broken { get; set; } = false;
        public bool TradeOn { get; set; } = false;
        public bool Tradeable { get; set; } = false;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DateTime ExplosiveEndTime { get; set; }
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
    public enum SupplyDemand
    {
        Supply,
        Demand
    }
}
