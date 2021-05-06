using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Services
{
    public class InstrumentService : IInstrumentService
    {
        private List<TradeInstrument> instruments;
        public InstrumentService()
        {
            instruments = new List<TradeInstrument>();
            instruments.Add(new TradeInstrument
            {
                Exchange = "NSE",
                Id = 60417,
                TradingSymbol = "ASIANPAINT"
            });
            instruments.Add(new TradeInstrument
            {
                Exchange = "NSE",
                Id = 628225,
                TradingSymbol = "CENTRUM"
            });
            instruments.Add(new TradeInstrument
            {
                Exchange = "NSE",
                Id = 524545,
                TradingSymbol = "CHENNPETRO"
            });
        }

        public List<TradeInstrument> GetInstruments()
        {
            return instruments;
        }

    }

    public interface IInstrumentService
    {
        List<TradeInstrument> GetInstruments();

    }
}
