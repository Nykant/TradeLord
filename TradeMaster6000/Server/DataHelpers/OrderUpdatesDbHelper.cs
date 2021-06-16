using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.DataHelpers
{
    public class OrderUpdatesDbHelper : IOrderUpdatesDbHelper
    {
        private readonly IDbContextFactory<TradeDbContext> contextFactory;
        private static SemaphoreSlim semaphore;
        public OrderUpdatesDbHelper(IDbContextFactory<TradeDbContext> dbContextFactory)
        {
            contextFactory = dbContextFactory;
            semaphore = new SemaphoreSlim(1, 1);
        }

        public async Task AddOrUpdate(OrderUpdate update)
        {
            await semaphore.WaitAsync();
            try
            {

                using (var context = contextFactory.CreateDbContext())
                {
                    OrderUpdate updateToUpdate = await context.OrderUpdates.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == update.OrderId);
                    if (updateToUpdate == default)
                    {
                        await context.OrderUpdates.AddAsync(update);
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
            finally
            {
                semaphore.Release();
            }
        }
        public async Task<OrderUpdate> Get(string orderId)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.OrderUpdates.FindAsync(orderId);
            }
        }
    }
    public interface IOrderUpdatesDbHelper
    {
        Task AddOrUpdate(OrderUpdate update);
        Task<OrderUpdate> Get(string orderId);
    }
}
