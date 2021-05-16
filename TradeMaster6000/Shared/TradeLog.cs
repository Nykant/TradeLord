using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TradeMaster6000.Shared
{
    public class TradeLog
    {
        [Key]
        public int Id { get; set; }
        public TradeOrder TradeOrder { get; set; }
        public int TradeOrderId { get; set; }
        public string Log { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
