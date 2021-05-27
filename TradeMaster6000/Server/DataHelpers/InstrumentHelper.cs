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
        private IDbContextFactory<TradeDbContext> ContextFactory { get; }
        private IInstrumentService InstrumentService { get; set; }
        private IServiceProvider Service { get; set; }
        public InstrumentHelper(IServiceProvider serviceProvider)
        {
            Service = serviceProvider;
            InstrumentService = Service.GetRequiredService<IInstrumentService>();
            ContextFactory = Service.GetRequiredService<IDbContextFactory<TradeDbContext>>();
            LoadInstruments(InstrumentService.GetInstruments());
        }

        public async Task<List<TradeInstrument>> GetTradeInstruments()
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                return await context.TradeInstruments.ToListAsync();
            }
        }

        private void LoadInstruments(List<TradeInstrument> list)
        {
            using (var context = ContextFactory.CreateDbContext())
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
