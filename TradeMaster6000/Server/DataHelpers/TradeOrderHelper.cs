using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.DataHelpers
{
    public class TradeOrderHelper : ITradeOrderHelper
    {
        private readonly object updatelock = new object();
        private readonly object addlock = new object();
        private readonly object savelock = new object();
        private readonly TradeDbContext context;

        public TradeOrderHelper(TradeDbContext tradeDbContext, IInstrumentService instrumentService)
        {
            context = tradeDbContext;
            instrumentService.LoadInstruments();
        }

        public async Task<TradeOrder> GetTradeOrder(int id)
        {
            return await context.TradeOrders.FindAsync(id);
        }

        public async Task<List<TradeOrder>> GetTradeOrders()
        {
            return await context.TradeOrders.ToListAsync();
        }

        public void UpdateTradeOrder(TradeOrder tradeOrder)
        {
            lock (updatelock)
            {
                context.TradeOrders.Update(tradeOrder);
            }
            SaveChanges();
        }

        public TradeOrder AddTradeOrder(TradeOrder tradeOrder)
        {
            var response = context.TradeOrders.Add(tradeOrder);
            SaveChanges();
            return response.Entity;
        }

        private void SaveChanges()
        {
            lock (savelock)
            {
                context.SaveChangesAsync();
            }
        }
    }
    public interface ITradeOrderHelper
    {
        TradeOrder AddTradeOrder(TradeOrder tradeOrder);
        void UpdateTradeOrder(TradeOrder tradeOrder);
        Task<TradeOrder> GetTradeOrder(int id);
        Task<List<TradeOrder>> GetTradeOrders();
    }
}
