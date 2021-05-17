using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.DataHelpers
{
    public class InstrumentHelper : IInstrumentHelper
    {
        private IDbContextFactory<TradeDbContext> contextFactory { get; }
        private IInstrumentService instrumentService { get; set; }
        private IServiceProvider service { get; set; }
        public InstrumentHelper(IServiceProvider serviceProvider)
        {
            service = serviceProvider;
            instrumentService = service.GetRequiredService<IInstrumentService>();
            contextFactory = service.GetRequiredService<IDbContextFactory<TradeDbContext>>();
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
                foreach (var instrument in list)
                {
                    if(context.TradeInstruments.FirstOrDefault(x => x.TradingSymbol == instrument.TradingSymbol) == default)
                    {
                        context.TradeInstruments.Add(instrument);
                    }
                }
                context.SaveChanges();
            }
        }
    }
    public interface IInstrumentHelper
    {
        Task<List<TradeInstrument>> GetTradeInstruments();
    }
}
