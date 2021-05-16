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
        private readonly TradeDbContext context;
        public TradeLogHelper(TradeDbContext tradeDbContext)
        {
            context = tradeDbContext;
        }

        public async Task<List<TradeLog>> GetTradeLogs()
        {
            return await context.TradeLogs.ToListAsync();
        }

        public async Task AddLog(int tradeOrderId, string log)
        {
            await context.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = tradeOrderId,
                    Log = log,
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);
        }
    }
    public interface ITradeLogHelper
    {
        Task<List<TradeLog>> GetTradeLogs();
    }
}
