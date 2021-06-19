using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TradeMaster6000.Shared
{
    public class CancellationGod
    {
        public CancellationTokenSource Source { get; set; }
        public string HangfireId { get; set; }
    }
}
