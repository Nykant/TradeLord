using KiteConnect;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TradeMaster6000.Shared
{
    public class OrderUpdate
    {
        [Key]
        public string OrderId { get; set; }
        public uint InstrumentToken { get; set; }
        public decimal AveragePrice { get; set; }
        public int FilledQuantity { get; set; }
        public decimal TriggerPrice { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

    }
}
