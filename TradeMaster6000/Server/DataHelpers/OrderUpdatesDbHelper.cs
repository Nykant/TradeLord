using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.DataHelpers
{
    public class OrderUpdatesDbHelper : IOrderUpdatesDbHelper
    {
        private readonly IDbContextFactory<TradeDbContext> contextFactory;
        public OrderUpdatesDbHelper(IDbContextFactory<TradeDbContext> dbContextFactory)
        {
            contextFactory = dbContextFactory;
        }

        public async Task AddOrUpdate(OrderUpdate update)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                var updateToUpdate = await context.OrderUpdates.FindAsync(update.OrderId);
                if(updateToUpdate == null)
                {
                    context.OrderUpdates.Add(update);
                    await context.SaveChangesAsync();
                    goto Ending;
                }

                if (updateToUpdate.FilledQuantity <= update.FilledQuantity)
                {
                    context.OrderUpdates.Update(update);
                    await context.SaveChangesAsync();
                }

                Ending:;
            }
        }
        public async Task<OrderUpdate> Get(string orderId)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.OrderUpdates.FindAsync(orderId);
            }
        }


        //public async Task<MyTick> GetLast(uint token)
        //{
        //    using (var context = contextFactory.CreateDbContext())
        //    {
        //        var ticks = await context.Ticks.ToListAsync();
        //        ticks.Reverse();
        //        foreach (var tick in ticks)
        //        {
        //            if (tick.InstrumentToken == token)
        //            {
        //                return tick;
        //            }
        //        }
        //    }
        //    return default;
        //}
        //public async Task<bool> Exists(uint token)
        //{
        //    using (var context = contextFactory.CreateDbContext())
        //    {
        //        if (await context.Ticks.FirstOrDefaultAsync(x => x.InstrumentToken == token) != default)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        //public async Task Flush()
        //{
        //    using (var context = contextFactory.CreateDbContext())
        //    {
        //        foreach (var tick in context.Ticks)
        //        {
        //            if (DateTime.Compare(tick.EndTime, DateTime.Now) < 0)
        //            {
        //                context.Remove(tick);
        //            }
        //        }
        //        await context.SaveChangesAsync();
        //    }
        //}
        //public async Task<bool> Any()
        //{
        //    using (var context = contextFactory.CreateDbContext())
        //    {
        //        return await context.Ticks.AnyAsync();
        //    }
        //}
    }
    public interface IOrderUpdatesDbHelper
    {
        Task AddOrUpdate(OrderUpdate update);
        Task<OrderUpdate> Get(string orderId);
    }
}
