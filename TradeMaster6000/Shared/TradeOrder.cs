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
        public OrderType OrderType { get; set; }
        public Product Product { get; set; }
        public TradeSymbol TradeSymbol { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
        public Variety Variety { get; set; }
    }
    public enum Variety
    {
        amo,
        regular
    }
    public enum TransactionType
    {
        BUY,
        SELL
    }
    public enum OrderType
    {
        LIMIT
    }
    public enum Product
    {
        MIS,
        NRML
    }
    public enum TradeSymbol
    {
        ASIANPAINT,
        APOLLOTYRE
    }
}
