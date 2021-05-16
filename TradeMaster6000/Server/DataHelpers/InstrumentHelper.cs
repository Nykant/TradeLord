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
    public class InstrumentHelper : IInstrumentHelper
    {
        private readonly TradeDbContext context;
        public InstrumentHelper(IInstrumentService instrumentService, TradeDbContext tradeDbContext)
        {
            instrumentService.LoadInstruments();
            context = tradeDbContext;
        }

        public async Task<List<TradeInstrument>> GetTradeInstruments()
        {
            return await context.TradeInstruments.ToListAsync();
        }

    }
    public interface IInstrumentHelper
    {
        Task<List<TradeInstrument>> GetTradeInstruments();
    }
}
