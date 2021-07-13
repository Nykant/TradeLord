using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ZoneDbHelper> logger;
        public ZoneDbHelper(IDbContextFactory<TradeDbContext> ContextFactory, ILogger<ZoneDbHelper> logger)
        {
            this.logger = logger;
            this.ContextFactory = ContextFactory;
        }

        public async Task<List<Zone>> GetUntestedZones()
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                IQueryable<Zone> zones = context.Zones.Where(x => x.Tested == false);
                var zoneys = await zones.OrderBy(x => x.To).ToListAsync();
                return zoneys;
            }
        }

        public async Task<List<Zone>> GetUntestedUntradeableZones(int timeframe)
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                IQueryable<Zone> zones = context.Zones.Where(x => x.Tested == false && x.Tradeable == false && x.Timeframe == timeframe);
                var zoneys = await zones.OrderBy(x => x.To).ToListAsync();
                return zoneys;
            }
        }

        public async Task<List<Zone>> GetUnbrokenZones()
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                IQueryable<Zone> zones = context.Zones.Where(x => x.Broken == false);
                return await zones.ToListAsync();
            }
        }

        public async Task<List<Zone>> GetZones(int timeframe, uint token)
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                IQueryable<Zone> zones = context.Zones.Where(x => x.Timeframe == timeframe && x.InstrumentToken == token);
                return await zones.OrderBy(x => x.From).ToListAsync();
            }
        }

        public async Task<List<Zone>> GetZones()
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                var zones = context.Zones;
                return await zones.OrderBy(x => x.InstrumentSymbol).ToListAsync();
            }
        }

        public async Task Add(List<Zone> zones)
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                await context.Zones.AddRangeAsync(zones);
                await context.SaveChangesAsync();
            }
        }

        public async Task Update(List<Zone> zones)
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                context.Zones.UpdateRange(zones);
                await context.SaveChangesAsync();
            }
        }

        public DateTime LastZoneEndTime(List<Zone> zones)
        {
            zones = zones.OrderBy(x => x.To).ToList();
            return zones[^1].To.AddMinutes(zones[^1].Timeframe - 1);
        }

        //public async Task<Zone> GetMotherZone(uint token, int timeframe, DateTime from, DateTime to)
        //{
        //    using (var context = ContextFactory.CreateDbContext())
        //    {
        //        return await context.Zones.FirstOrDefaultAsync(x => DateTime.Compare(x.From, from) <= 0 && DateTime.Compare(x.To, to) >= 0);
        //    }
        //}
    }
    public interface IZoneDbHelper
    {
        Task<List<Zone>> GetUntestedZones();
        Task<List<Zone>> GetUntestedUntradeableZones(int timeframe);
        //Task<Zone> GetMotherZone(uint token, int timeframe, DateTime from, DateTime to);
        Task Add(List<Zone> zones);
        DateTime LastZoneEndTime(List<Zone> zones);
        Task Update(List<Zone> zones);
        Task<List<Zone>> GetZones(int timeframe, uint token);
        Task<List<Zone>> GetZones();
        Task<List<Zone>> GetUnbrokenZones();
    }
}
