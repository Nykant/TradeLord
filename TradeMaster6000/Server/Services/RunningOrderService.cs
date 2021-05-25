using Microsoft.EntityFrameworkCore;
using System;
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
        private readonly object runningorderkey = new();
        private List<TradeOrder> Orders { get; set; }
        private readonly ITradeOrderHelper tradeOrderHelper;
        public RunningOrderService(ITradeOrderHelper tradeOrderHelper)
        {
            Orders = new List<TradeOrder>();
            this.tradeOrderHelper = tradeOrderHelper;
        }

        public void Add(TradeOrder order)
        {
            lock (runningorderkey)
            {
                Orders.Add(order);
            }
        }

        public void Remove(int id)
        {
            lock (runningorderkey)
            {
                foreach (var order in Orders)
                {
                    if (order.Id == id)
                    {
                        Orders.Remove(order);
                        break;
                    }
                }
            }
        }

        public async Task UpdateOrders()
        {
            var orders = await tradeOrderHelper.GetTradeOrders();
            List<TradeOrder> running;
            int count;

            lock (runningorderkey)
            {
                count = Orders.Count;
                running = Orders;
            }

            foreach (var order in orders)
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (order.Id == running[i].Id)
                        {
                            lock (runningorderkey)
                            {
                                Orders[i].QuantityFilled = order.QuantityFilled;
                                Orders[i].Status = order.Status;
                            }
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        public List<TradeOrder> Get()
        {
            lock (runningorderkey)
            {
                return Orders;
            }
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
