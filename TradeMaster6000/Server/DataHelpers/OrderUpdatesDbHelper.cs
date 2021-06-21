using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        public OrderUpdatesDbHelper(IDbContextFactory<TradeDbContext> dbContextFactory)
        {
            contextFactory = dbContextFactory;
        }

        public async Task Add(List<OrderUpdate> updates)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                foreach(var update in updates)
                {
                    var order = await context.OrderUpdates.FirstOrDefaultAsync(x => x.OrderId == update.OrderId);
                    if(order == default)
                    {
                        var entry = await context.AddAsync(update);
                        await context.SaveChangesAsync();
                    }
                    else
                    {
                        if(order.FilledQuantity <= update.FilledQuantity)
                        {
                            var entry = context.Update(update);
                            await context.SaveChangesAsync();
                        }
                    }
                }
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
        Task Add(List<OrderUpdate> updates);
        Task<OrderUpdate> Get(string orderId);
    }
}
