using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Services
{
    public class RunningOrderService : IRunningOrderService
    {
        private static List<TradeOrder> Orders { get; set; }
        public RunningOrderService()
        {
            Orders = new List<TradeOrder>();
        }

        public void Add(TradeOrder order)
        {
            Orders.Add(order);
        }

        public void Remove(TradeOrder order)
        {
            Orders.Remove(order);
        }

        public List<TradeOrder> Get()
        {
            return Orders;
        }
    }
    public interface IRunningOrderService
    {
        void Add(TradeOrder order);
        void Remove(TradeOrder order);
        List<TradeOrder> Get();
    }
}
