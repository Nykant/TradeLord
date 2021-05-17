using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.DataHelpers
{
    public class InstrumentHelper
    {
        private readonly IDbContextFactory<TradeDbContext> contextFactory;
        public InstrumentHelper(IInstrumentService instrumentService, IDbContextFactory<TradeDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
            LoadInstruments(instrumentService.GetInstruments());
        }

        public async Task<List<TradeInstrument>> GetTradeInstruments()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.TradeInstruments.ToListAsync();
            }
        }

        private void LoadInstruments(List<TradeInstrument> list)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                if (context.TradeInstruments.Count() == 0)
                {
                    foreach (var instrument in list)
                    {
                        context.TradeInstruments.Add(instrument);
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
