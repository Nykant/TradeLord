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
        private readonly IDbContextFactory<TradeDbContext> contextFactory;
        private readonly object tradelock = new object();

        public TradeOrderHelper(IDbContextFactory<TradeDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<TradeOrder> GetTradeOrder(int id)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.TradeOrders.FindAsync(id);
            }
        }

        public async Task<List<TradeOrder>> GetTradeOrders()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.TradeOrders.ToListAsync();
            }
        }

        public async Task UpdateTradeOrder(TradeOrder tradeOrder)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                context.TradeOrders.Update(tradeOrder);
                await context.SaveChangesAsync();
            }
        }

        public async Task<TradeOrder> AddTradeOrder(TradeOrder tradeOrder)
        {
            TradeOrder order;

            using (var context = contextFactory.CreateDbContext())
            {
                order = context.TradeOrders.Add(tradeOrder).Entity;
                await context.SaveChangesAsync();
            }

            return order;
        }
    }

    public interface ITradeOrderHelper
    {
        Task<TradeOrder> AddTradeOrder(TradeOrder tradeOrder);
        Task UpdateTradeOrder(TradeOrder tradeOrder);
        Task<TradeOrder> GetTradeOrder(int id);
        Task<List<TradeOrder>> GetTradeOrders();

    }
}
