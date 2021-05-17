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
        public decimal StopLoss { get; set; }
        [Required]
        public decimal Entry { get; set; }
        [Required]
        public decimal Risk { get; set; }
        [Required]
        public string TradingSymbol { get; set; }
        [Required]
        public int RxR { get; set; }
        [Required]
        public TransactionType TransactionType { get; set; }
        public Status Status { get; set; }
        public TradeInstrument Instrument {get; set;}
        public CancellationTokenSource TokenSource { get; set; }
        public bool EntryHit { get; set; }
        public bool SLMHit { get; set; }
        public bool TargetHit { get; set; }
        public int QuantityFilled { get; set; }
        public int Quantity { get; set; }
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
