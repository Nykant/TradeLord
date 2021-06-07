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
        private IServiceProvider Service { get; set; }
        public InstrumentHelper(IServiceProvider serviceProvider)
        {
            Service = serviceProvider;
            ContextFactory = Service.GetRequiredService<IDbContextFactory<TradeDbContext>>();
        }

        public async Task<List<TradeInstrument>> GetTradeInstruments()
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                return await context.TradeInstruments.ToListAsync();
            }
        }

        public void LoadInstruments(List<TradeInstrument> list)
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                if (!context.TradeInstruments.Any())
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
    public interface IInstrumentHelper
    {
        Task<List<TradeInstrument>> GetTradeInstruments();
        void LoadInstruments(List<TradeInstrument> list);
    }
}
