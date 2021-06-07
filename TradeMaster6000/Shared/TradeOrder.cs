using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel.DataAnnotations;


namespace TradeMaster6000.Shared
{
    public class TradeOrder
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [Range(1, 99999), ]
        public decimal StopLoss { get; set; }
        [Required]
        [Range(1, 99999)]
        public decimal Entry { get; set; }
        [Required]
        [Range(1, 9999)]
        public decimal Risk { get; set; }
        [Required]
        [Range(1, 100)]
        public int RxR { get; set; }
        [Required]
        public string TradingSymbol { get; set; }

        [Required]
        public TransactionType TransactionType { get; set; }
        public Status Status { get; set; }
        public int QuantityFilled { get; set; }
        public int Quantity { get; set; }
        public string EntryId { get; set; }
        public string SLMId { get; set; }
        public string TargetId { get; set; }
        public decimal Target { get; set; }
        public string SLMStatus { get; set; }
        public string EntryStatus { get; set; }
        public string TargetStatus { get; set; }
        public string ExitTransactionType { get; set; }
        public decimal ZoneWidth { get; set; }
        public bool PreSLMCancelled { get; set; } = false;
        public bool IsOrderFilling { get; set; } = false;
        public bool RegularSlmPlaced { get; set; } = false;
        public bool TargetHit { get; set; } = false;
        public bool TargetPlaced { get; set; } = false;
        public bool SquaredOff { get; set; } = false;
        public TradeInstrument Instrument { get; set; }
        public string JobId { get; set; }
        public List<TradeLog> TradeLogs { get; set; }
    }

    public enum TransactionType
    {
        BUY,
        SELL
    }
    public enum Status
    {
        STARTING,
        RUNNING,
        STOPPING,
        DONE
    }
}
