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
            foreach(var instrument in instruments)
            {
                await ZoneFinder(instrument);
            }
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
            HalfZone up = new HalfZone();
            HalfZone down = new HalfZone();
            Parallel.Invoke(
                () => down = FindDown(candles, fittyCandle),
                () => up = FindUp(candles, fittyCandle));

            REPEAT:;
            if(down == default || up == default)
            {
                return default;
            }

            if(up.ExplosiveCandle.HL_Diff < (down.BiggestBaseDiff * (decimal)1.2))
            {
                up = FindUp(candles, fittyCandle, down.BiggestBaseDiff);
                goto REPEAT;
            }

            if(down.ExplosiveCandle.HL_Diff < (up.BiggestBaseDiff * (decimal)1.2))
            {
                down = FindDown(candles, fittyCandle, up.BiggestBaseDiff);
                goto REPEAT;
            }

            int range = up.ExplosiveCandle.RangeFromFitty + down.ExplosiveCandle.RangeFromFitty;
            if(range > 6)
            {
                return default;
            }

            Zone zone = new Zone();

            if(up.Top > down.Top)
            {
                zone.Top = up.Top;
            }
            else
            {
                zone.Top = down.Top;
            }

            if(up.Bottom < down.Bottom)
            {
                zone.Bottom = up.Bottom;
            }
            else
            {
                zone.Bottom = down.Bottom;
            }

            return zone;
        }

        private HalfZone FindDown(List<Candle> candles, FittyCandle fittyCandle)
        {
            HalfZone halfZone = new HalfZone { Top = fittyCandle.Candle.High, Bottom = fittyCandle.Candle.Low };
            halfZone.BiggestBaseDiff = Math.Abs(fittyCandle.Candle.High - fittyCandle.Candle.Low);
            for (int i = fittyCandle.Index; i >= 0 && Math.Abs(i - fittyCandle.Index) <= 5; i--)
            {
                if(halfZone.Top < candles[i].High)
                {
                    halfZone.Top = candles[i].High;
                }
                if(halfZone.Bottom > candles[i].Low)
                {
                    halfZone.Bottom = candles[i].Low;
                }

                decimal diff = Math.Abs(candles[i].High - candles[i].Low);
                decimal diffx = halfZone.BiggestBaseDiff * (decimal)1.2;
                if (diff > diffx)
                {
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Candle = candles[i],
                        HL_Diff = diff,
                        RangeFromFitty = Math.Abs(i - fittyCandle.Index)
                    };
                    return halfZone;
                }
                else if (diff > halfZone.BiggestBaseDiff)
                {
                    halfZone.BiggestBaseDiff = diff;
                }
            }
            return default;
        }

        private HalfZone FindDown(List<Candle> candles, FittyCandle fittyCandle, decimal upBiggestBaseDiff)
        {
            HalfZone halfZone = new HalfZone { Top = fittyCandle.Candle.High, Bottom = fittyCandle.Candle.Low };
            halfZone.BiggestBaseDiff = upBiggestBaseDiff;
            for (int i = fittyCandle.Index; i <= 0 && Math.Abs(i - fittyCandle.Index) <= 5; i--)
            {
                if (halfZone.Top < candles[i].High)
                {
                    halfZone.Top = candles[i].High;
                }
                if (halfZone.Bottom > candles[i].Low)
                {
                    halfZone.Bottom = candles[i].Low;
                }

                decimal diff = Math.Abs(candles[i].High - candles[i].Low);
                decimal diffx = halfZone.BiggestBaseDiff * (decimal)1.2;
                if (diff > diffx)
                {
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Candle = candles[i],
                        HL_Diff = diff,
                        RangeFromFitty = Math.Abs(i - fittyCandle.Index)
                    };
                    return halfZone;
                }
                else if (diff > halfZone.BiggestBaseDiff)
                {
                    halfZone.BiggestBaseDiff = diff;
                }
            }
            return default;
        }

        private HalfZone FindUp(List<Candle> candles, FittyCandle fittyCandle)
        {
            HalfZone halfZone = new HalfZone { Top = fittyCandle.Candle.High, Bottom = fittyCandle.Candle.Low };
            halfZone.BiggestBaseDiff = Math.Abs(fittyCandle.Candle.High - fittyCandle.Candle.Low);
            for (int i = fittyCandle.Index; i < candles.Count && Math.Abs(i - fittyCandle.Index) <= 5; i++)
            {
                if (halfZone.Top < candles[i].High)
                {
                    halfZone.Top = candles[i].High;
                }
                if (halfZone.Bottom > candles[i].Low)
                {
                    halfZone.Bottom = candles[i].Low;
                }

                decimal diff = Math.Abs(candles[i].High - candles[i].Low);
                decimal diffx = halfZone.BiggestBaseDiff * (decimal)1.2;
                if (diff > diffx)
                {
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Candle = candles[i],
                        HL_Diff = diff,
                        RangeFromFitty = Math.Abs(i - fittyCandle.Index)
                    };
                    return halfZone;
                }
                else if (diff > halfZone.BiggestBaseDiff)
                {
                    halfZone.BiggestBaseDiff = diff;
                }
            }
            return default;
        }

        private HalfZone FindUp(List<Candle> candles, FittyCandle fittyCandle, decimal downBiggestBaseDiff)
        {
            HalfZone halfZone = new HalfZone { Top = fittyCandle.Candle.High, Bottom = fittyCandle.Candle.Low };
            halfZone.BiggestBaseDiff = downBiggestBaseDiff;
            for (int i = fittyCandle.Index; i < candles.Count && Math.Abs(i - fittyCandle.Index) <= 5; i++)
            {
                if (halfZone.Top < candles[i].High)
                {
                    halfZone.Top = candles[i].High;
                }
                if (halfZone.Bottom > candles[i].Low)
                {
                    halfZone.Bottom = candles[i].Low;
                }

                decimal diff = Math.Abs(candles[i].High - candles[i].Low);
                decimal diffx = halfZone.BiggestBaseDiff * (decimal)1.2;
                if (diff > diffx)
                {
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Candle = candles[i],
                        HL_Diff = diff,
                        RangeFromFitty = Math.Abs(i - fittyCandle.Index)
                    };
                    return halfZone;
                }
                else if (diff > halfZone.BiggestBaseDiff)
                {
                    halfZone.BiggestBaseDiff = diff;
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
