using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.DataHelpers
{
    public class TickDbHelper : ITickDbHelper
    {
        private readonly IDbContextFactory<TradeDbContext> contextFactory;
        public TickDbHelper(IDbContextFactory<TradeDbContext> dbContextFactory)
        {
            contextFactory = dbContextFactory;
        }

        public async Task Add(MyTick tick)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                context.Ticks.Add(tick);
                await context.SaveChangesAsync();
            }
        }
        public async Task<List<MyTick>> Get(uint token)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Ticks.Where(x => x.InstrumentToken == token && DateTime.Compare(x.EndTime, DateTime.Now) > 0).ToListAsync();
            }
        }
        public async Task<MyTick> GetLast(uint token)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                var ticks = await context.Ticks.ToListAsync();
                ticks.Reverse();
                foreach(var tick in ticks)
                {
                    if(tick.InstrumentToken == token)
                    {
                        return tick;
                    }
                }
            }
            return default;
        }
        public async Task<bool> Exists(uint token)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                if (await context.Ticks.FirstOrDefaultAsync(x => x.InstrumentToken == token) != default)
                {
                    return true;
                }
            }
            return false;
        }
        public async Task Flush()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                foreach(var tick in context.Ticks)
                {
                    if(DateTime.Compare(tick.EndTime, DateTime.Now) < 0)
                    {
                        context.Remove(tick);
                    }
                }
                await context.SaveChangesAsync();
            }
        }
        public async Task<bool> Any()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Ticks.AnyAsync();
            }
        }
    }
    public interface ITickDbHelper
    {
        Task<List<MyTick>> Get(uint token);
        Task Add(MyTick tick);
        Task Flush();
        Task<bool> Any();
        Task<MyTick> GetLast(uint token);
        Task<bool> Exists(uint token);
    }
}
