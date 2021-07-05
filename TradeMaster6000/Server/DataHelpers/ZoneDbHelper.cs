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
            try
            {
                using (var context = ContextFactory.CreateDbContext())
                {
                    return await context.Zones.Where(x => x.Tested == false).ToListAsync();
                }
            }
            catch (Exception e)
            {
                logger.LogInformation(e.Message);
            }
            return default;
        }

        public async Task<List<Zone>> GetUnbrokenZones()
        {
            try
            {
                using (var context = ContextFactory.CreateDbContext())
                {
                    return await context.Zones.Where(x => x.Broken == false).ToListAsync();
                }
            }
            catch (Exception e)
            {
                logger.LogInformation(e.Message);
            }
            return default;
        }

        public async Task<List<Zone>> GetZones(int timeframe, uint token)
        {
            try
            {
                using (var context = ContextFactory.CreateDbContext())
                {
                    return await context.Zones.Where(x => x.Timeframe == timeframe && x.InstrumentToken == token).OrderBy(x => x.From).ToListAsync();
                }
            }
            catch (Exception e)
            {
                logger.LogInformation(e.Message);
            }
            return default;
        }

        public async Task Add(List<Zone> zones)
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                foreach(var zone in zones)
                {
                    await context.Zones.AddAsync(zone);
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task Update(List<Zone> zones)
        {
            try
            {
                using (var context = ContextFactory.CreateDbContext())
                {
                    foreach (var zone in zones)
                    {
                        context.Zones.Update(zone);
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                logger.LogInformation(e.Message);
            }

        }

        public DateTime LastZoneEndTime(List<Zone> zones)
        {
            zones = zones.OrderBy(x => x.To).ToList();
            return zones[^1].To;
        }
    }
    public interface IZoneDbHelper
    {
        Task<List<Zone>> GetUntestedZones();
        Task Add(List<Zone> zones);
        DateTime LastZoneEndTime(List<Zone> zones);
        Task Update(List<Zone> zones);
        Task<List<Zone>> GetZones(int timeframe, uint token);
        Task<List<Zone>> GetUnbrokenZones();
    }
}
