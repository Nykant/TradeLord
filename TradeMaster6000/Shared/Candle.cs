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
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DateTime Kill { get; set; }
    }
}
