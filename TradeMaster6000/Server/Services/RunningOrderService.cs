using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Services
{
    public class RunningOrderService : IRunningOrderService
    {
        private ConcurrentDictionary<int, TradeOrder> Orders { get; set; }
        private readonly ITradeOrderHelper tradeOrderHelper;
        public RunningOrderService(ITradeOrderHelper tradeOrderHelper)
        {
            Orders = new ();
            this.tradeOrderHelper = tradeOrderHelper;
        }

        public void Add(TradeOrder order)
        {
            Orders.TryAdd(order.Id, order);
        }

        public void Remove(int id)
        {
            Orders.TryRemove(id, out TradeOrder value);
        }

        public async Task UpdateOrders()
        {
            var orders = await tradeOrderHelper.GetRunningTradeOrders();

            foreach (var order in orders)
            {
                Orders.TryUpdate(order.Id, order, Orders[order.Id]);
            }
        }

        public List<TradeOrder> Get()
        {
            List<TradeOrder> orders = new();
            foreach(var valuepair in Orders)
            {
                orders.Add(valuepair.Value);
            }
            return orders;
        }
    }
    public interface IRunningOrderService
    {
        void Add(TradeOrder order);
        void Remove(int id);
        List<TradeOrder> Get();
        Task UpdateOrders();
    }
}
