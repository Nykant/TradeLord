using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TradeMaster6000.Shared
{
    public class TradeOrder
    {
        public int Id { get; set; }
        public decimal StopLoss { get; set; }
        public decimal Entry { get; set; }
        public decimal Risk { get; set; }
        public int RxR { get; set; }
        public TradeInstrument Instrument {get; set;}
        public TransactionType TransactionType { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
    }
    public enum TransactionType
    {
        BUY,
        SELL
    }
}
