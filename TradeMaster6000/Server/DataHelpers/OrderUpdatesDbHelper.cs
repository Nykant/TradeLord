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
        private readonly object key = new object();
        public OrderUpdatesDbHelper(IDbContextFactory<TradeDbContext> dbContextFactory)
        {
            contextFactory = dbContextFactory;
            semaphore = new SemaphoreSlim(1, 1);
        }

        public void AddOrUpdate(OrderUpdate update)
        {
            semaphore.Wait();
            try
            {
                OrderUpdate updateToUpdate;
                using (var context = contextFactory.CreateDbContext())
                {
                    updateToUpdate = context.OrderUpdates.Find(update.OrderId);
                }


                //lock (key)
                //{
                using (var context = contextFactory.CreateDbContext())
                {
                    if (updateToUpdate == null)
                    {
                        context.OrderUpdates.Add(update);
                        context.SaveChanges();
                        goto Ending;
                    }

                    if (updateToUpdate.FilledQuantity <= update.FilledQuantity)
                    {
                        context.OrderUpdates.Update(update);
                        context.SaveChanges();
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
        void AddOrUpdate(OrderUpdate update);
        Task<OrderUpdate> Get(string orderId);
    }
}
