using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TradeMaster6000.Shared
{
    public class TradeOrder
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public int StopLoss { get; set; }
        public decimal Entry { get; set; }
        public decimal TriggerPrice { get; set; }
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
        REGULAR,
        COVER
    }
    public enum TransactionType
    {
        BUY,
        SELL
    }
    public enum OrderType
    {
        LIMIT,
        SLM
    }
    public enum Product
    {
        CNC,
        MIS,
        NRML
    }
    public enum TradeSymbol
    {
        ASIANPAINT,
        CENTRUM,
        CHENNPETRO
    }
}
