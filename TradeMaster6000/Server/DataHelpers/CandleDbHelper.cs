using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.DataHelpers
{
    public class CandleDbHelper : ICandleDbHelper
    {
        private readonly IDbContextFactory<TradeDbContext> contextFactory;

        public CandleDbHelper(IDbContextFactory<TradeDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<List<Candle>> GetCandles()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Candles.ToListAsync();
            }
        }

        public async Task<List<Candle>> GetCandles(uint instrumentToken)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Candles.Where(x=>x.TradeInstrument.Token == instrumentToken).ToListAsync();
            }
        }

        public async Task AddCandle(Candle candle)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                await context.Candles.AddAsync(candle);
                await context.SaveChangesAsync();
            }
        }

    }
    public interface ICandleDbHelper
    {
        Task AddCandle(Candle candle);
        Task<List<Candle>> GetCandles();
        Task<List<Candle>> GetCandles(uint instrumentToken);
    }
}
