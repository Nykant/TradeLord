using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TradeMaster6000.Shared
{
    public class TradeOrder
    {
        public string StopLoss { get; set; }
        public string Entry { get; set; }
        public string TakeProfit { get; set; }
        public int Id { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
    }
}
