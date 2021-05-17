using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.DataHelpers
{
    public class TradeLogHelper : ITradeLogHelper
    {
        private readonly IDbContextFactory<TradeDbContext> contextFactory;

        public TradeLogHelper(IDbContextFactory<TradeDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<List<TradeLog>> GetTradeLogs(int orderId)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.TradeLogs.Where(x => x.TradeOrderId == orderId).ToListAsync();
            }
        }

        public async Task AddLog(int tradeOrderId, string log)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                await context.TradeLogs.AddAsync(
                    new TradeLog
                    {
                        TradeOrderId = tradeOrderId,
                        Log = log,
                        Timestamp = DateTime.Now
                    }
                );
                await context.SaveChangesAsync();
            }
        }
    }
    public interface ITradeLogHelper
    {
        Task<List<TradeLog>> GetTradeLogs(int id);
        Task AddLog(int tradeOrderId, string log);
    }
}
