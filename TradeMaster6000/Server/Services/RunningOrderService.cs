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
        private static List<TradeOrder> Orders { get; set; }
        private readonly ITradeOrderHelper tradeOrderHelper;
        public RunningOrderService(ITradeOrderHelper tradeOrderHelper)
        {
            Orders = new List<TradeOrder>();
            this.tradeOrderHelper = tradeOrderHelper;
        }

        public void Add(TradeOrder order)
        {
            Orders.Add(order);
        }

        public void Remove(TradeOrder order)
        {
            Orders.Remove(order);
        }

        public async Task UpdateOrders()
        {
            var orders = await tradeOrderHelper.GetTradeOrders();
            foreach(var order in orders)
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < Orders.Count; i++)
                    {
                        if (order.Id == Orders[i].Id)
                        {
                            Orders[i].QuantityFilled = order.QuantityFilled;
                            Orders[i].Status = order.Status;
                        }
                    }
                }).ConfigureAwait(false);
            }
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
        Task UpdateOrders();
    }
}
