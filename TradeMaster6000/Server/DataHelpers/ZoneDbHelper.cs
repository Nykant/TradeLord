using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.DataHelpers
{
    public class ZoneDbHelper : IZoneDbHelper
    {
        private IDbContextFactory<TradeDbContext> ContextFactory { get; }
        public ZoneDbHelper(IDbContextFactory<TradeDbContext> ContextFactory)
        {
            this.ContextFactory = ContextFactory;
        }

        public async Task<List<Zone>> GetZones()
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                return await context.Zones.ToListAsync();
            }
        }

        public async Task Add(Zone zone)
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                await context.AddAsync(zone);
                await context.SaveChangesAsync();
            }
        }
    }
    public interface IZoneDbHelper
    {
        Task Add(Zone zone);
        Task<List<Zone>> GetZones();
    }
}
