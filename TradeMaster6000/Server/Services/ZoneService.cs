using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Services
{
    public class ZoneService : IZoneService
    {
        private readonly ICandleDbHelper candleHelper;
        public ZoneService(ICandleDbHelper candleDbHelper)
        {
            candleHelper = candleDbHelper;
        }

        public async Task Start(List<TradeInstrument> instruments)
        {

        }

        private async Task ZoneFinder(TradeInstrument instrument)
        {
            int index = 0;
            List<Candle> candles = await candleHelper.GetCandles(instrument.Token);

            Repeat:;
            FittyCandle fittyCandle = await Task.Run(() => FittyFinder(candles, index));
            index = fittyCandle.Index;
            if(fittyCandle == default)
            {
                goto Ending;
            }

            Zone zone = await Task.Run(() => FindZone(candles, fittyCandle));
            if(zone == default)
            {
                goto Repeat;
            }

            if(index == candles.Count - 1)
            {
                goto Ending;
            }
            else
            {
                goto Repeat;
            }

            Ending:;
        }

        private Zone FindZone(List<Candle> candles, FittyCandle fittyCandle)
        {
            ExplosiveCandle up;
            ExplosiveCandle down;
            Parallel.Invoke(
                () => down = FindDown(candles, fittyCandle),
                () => up = FindUp(candles, fittyCandle));


            return default;
        }

        private ExplosiveCandle FindDown(List<Candle> candles, FittyCandle fittyCandle)
        {
            Candle biggestBase = fittyCandle.Candle;
            decimal biggestDiff = Math.Abs(biggestBase.High - biggestBase.Low);
            for (int i = fittyCandle.Index; i < candles.Count && Math.Abs(i - fittyCandle.Index) <= 5; i++)
            {
                decimal diff = Math.Abs(candles[i].High - candles[i].Low);
                decimal diffx = diff * (decimal)1.2;
                if (diffx > biggestDiff)
                {
                    return new ExplosiveCandle
                    {
                        Candle = candles[i],
                        HL_Diff = diffx,
                        RangeFromFitty = Math.Abs(i - fittyCandle.Index)
                    };
                }
                else if (diff > biggestDiff)
                {
                    biggestDiff = diff;
                }
            }
            return default;
        }

        private ExplosiveCandle FindUp(List<Candle> candles, FittyCandle fittyCandle)
        {
            Candle biggestBase = fittyCandle.Candle;
            decimal biggestDiff = Math.Abs(biggestBase.High - biggestBase.Low);
            for (int i = fittyCandle.Index; i >= 0 && Math.Abs(i - fittyCandle.Index) <= 5; i++)
            {
                decimal diff = Math.Abs(candles[i].High - candles[i].Low);
                decimal diffx = diff * (decimal)1.2;
                if (diffx > biggestDiff)
                {
                    return new ExplosiveCandle
                    {
                        Candle = candles[i],
                        HL_Diff = diffx,
                        RangeFromFitty = Math.Abs(i - fittyCandle.Index)
                    };
                }
                else if(diff > biggestDiff)
                {
                    biggestDiff = diff;
                }
            }
            return default;
        }

        private FittyCandle FittyFinder(List<Candle> candles, int index)
        {
            for(int i = index; i < candles.Count; i++)
            {
                var fitty = (candles[i].High - candles[i].Low) * (decimal)0.5;
                if(Math.Abs(candles[i].Open - candles[i].Close) <= fitty)
                {
                    return new FittyCandle { Candle = candles[i], Index = i };
                }
            }
            return default;
        }
    }
    public interface IZoneService
    {
        Task Start(List<TradeInstrument> instruments);
    }
}
