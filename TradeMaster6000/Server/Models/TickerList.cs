using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TradeMaster6000.Server.Models
{
    public class TickerList
    {
        public int Id { get; set; }
        public List<KiteConnect.Tick> Ticks { get; set; }
    }
}
