using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Services
{
    public class RunningOrderService : IRunningOrderService
    {
        private ConcurrentDictionary<int, TradeOrder> Orders { get; set; }
        private ConcurrentQueue<SomeLog> RunningLogs { get; set; }
        private readonly ITradeOrderHelper tradeOrderHelper;
        public RunningOrderService(ITradeOrderHelper tradeOrderHelper)
        {
            Orders = new ();
            RunningLogs = new();
            this.tradeOrderHelper = tradeOrderHelper;
        }

        public void Add(TradeOrder order)
        {
            if(!Orders.TryAdd(order.Id, order))
            {
                AddLog($"couldn't add order -> {order.Id}", LogType.RunningOrder);
            }
        }

        public void Remove(int id)
        {
            foreach(var order in Orders)
            {
                if(order.Key == id)
                {
                    if (!Orders.TryRemove(order))
                    {
                        AddLog($"couldn't remove order -> {id}", LogType.RunningOrder);
                    }
                    break;
                }
            }
        }

        public async Task UpdateOrders()
        {
            foreach (var order in await tradeOrderHelper.GetRunningTradeOrders())
            {
                try
                {
                    Orders.TryGetValue(order.Id, out TradeOrder value);
                    if(value != null)
                    {
                        order.TokenSource = value.TokenSource;
                        Orders.TryUpdate(order.Id, order, Orders[order.Id]);
                    }
                }
                catch (Exception e)
                {
                    AddLog($"{order.Id} -> {e.Message}", LogType.Exception);
                }
            }
        }

        private void AddLog(string log, LogType type)
        {
            RunningLogs.Enqueue(new()
            {
                Log = log,
                Timestamp = DateTime.Now,
                LogType = type
            });
        }

        public List<SomeLog> GetLogs()
        {
            return RunningLogs.ToList();
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
        List<SomeLog> GetLogs();
    }
}
