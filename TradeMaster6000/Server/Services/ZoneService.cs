using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Server.Helpers;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Services
{
    public class ZoneService : IZoneService
    {
        private readonly ICandleDbHelper candleHelper;
        private readonly IZoneDbHelper zoneHelper;
        private readonly ITimeHelper timeHelper;
        private readonly ILogger<ZoneService> logger;
        private static SemaphoreSlim semaphoreSlim;
        public ZoneService(ICandleDbHelper candleDbHelper, IZoneDbHelper zoneDbHelper, ITimeHelper timeHelper, ILogger<ZoneService> logger)
        {
            this.timeHelper = timeHelper;
            this.logger = logger;
            candleHelper = candleDbHelper;
            zoneHelper = zoneDbHelper;
            semaphoreSlim = new SemaphoreSlim(1, 1);
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task Start(List<TradeInstrument> instruments, int timeFrame)
        {
            logger.LogInformation("zone service starting");
            List<Task> tasks = new List<Task>();
            for(int i = 0; i < instruments.Count; i++)
            {
                if(instruments[i].Token == 5633)
                {
                    tasks.Add(ZoneFinder(instruments[i], timeFrame));
                }
            }
            await Task.WhenAll(tasks);
            logger.LogInformation("zone service done");
        }

        private async Task ZoneFinder(TradeInstrument instrument, int timeFrame)
        {
            logger.LogInformation($"processor id: {Thread.GetCurrentProcessorId()} --- managed thread id: {Thread.CurrentThread.ManagedThreadId} --- timestamp: {DateTime.Now} --- description: zone finder started");
            //await semaphoreSlim.WaitAsync();
            try
            {
                List<Candle> candles = await candleHelper.GetCandles(instrument.Token);
                candles = candles.OrderBy(x => x.Timestamp).ToList();
                if (candles.Count == 0)
                {
                    goto Ending;
                }

                List<Candle> newCandles = new List<Candle>();
                DateTime time = timeHelper.OpeningTime();
                while (true)
                {
                    if (candles[0].Timestamp.Hour == time.Hour && candles[0].Timestamp.Minute == time.Minute)
                    {
                        break;
                    }
                    time = time.AddMinutes(1);
                }

                Candle temp = new ();
                int emptyCounter = 0;
                int i = 0;
                int n = candles.Count;
                int candleCounter = 0;
                while (i < n)
                {
                    if (candles[i].Timestamp.Hour == time.Hour && candles[i].Timestamp.Minute == time.Minute)
                    {
                        candleCounter++;

                        if (temp.High < candles[i].High || temp.High == default)
                        {
                            temp.High = candles[i].High;
                        }
                        if (temp.Low > candles[i].Low || temp.Low == default)
                        {
                            temp.Low = candles[i].Low;
                        }

                        if (emptyCounter + candleCounter == timeFrame)
                        {
                            temp.InstrumentToken = candles[i].InstrumentToken;
                            temp.Timestamp = candles[i - (timeFrame - 1)].Timestamp;
                            temp.Open = candles[i - (timeFrame - 1)].Open;
                            temp.Close = candles[i].Close;
                            newCandles.Add(temp);
                            candleCounter = 0;
                            emptyCounter = 0;
                            temp = new();
                        }

                        i++;
                    }
                    else
                    {
                        emptyCounter++;
                        if(candleCounter > 0)
                        {
                            if (emptyCounter + candleCounter == timeFrame)
                            {
                                if (temp.High != default)
                                {
                                    temp.InstrumentToken = candles[i].InstrumentToken;
                                    newCandles.Add(temp);
                                    candleCounter = 0;
                                    emptyCounter = 0;
                                    temp = new();
                                }
                            }
                        }
                        else
                        {
                            if(emptyCounter == timeFrame)
                            {
                                emptyCounter = 0;
                                temp = new();
                            }
                        }
                    }

                    time = time.AddMinutes(1);
                }

                logger.LogInformation($"processor id: {Thread.GetCurrentProcessorId()} --- managed thread id: {Thread.CurrentThread.ManagedThreadId} --- timestamp: {DateTime.Now} --- description: finished making new candles. amount: {newCandles.Count}");

                int index = 0;

                Repeat:;

                if (index >= newCandles.Count)
                {
                    goto Ending;
                }

                FittyCandle fittyCandle = await Task.Run(() => FittyFinder(newCandles, index));
                if (fittyCandle == default)
                {
                    goto Ending;
                }

                Zone zone = await Task.Run(() => FindZone(newCandles, fittyCandle, instrument.TradingSymbol));
                if (zone == default)
                {
                    index = index + 6;
                    goto Repeat;
                }
                else
                {
                    index = zone.EndIndex + 6;
                    await zoneHelper.Add(zone).ConfigureAwait(false);
                    logger.LogInformation($"processor id: {Thread.GetCurrentProcessorId()} --- managed thread id: {Thread.CurrentThread.ManagedThreadId} --- timestamp: {DateTime.Now} --- description: added zone: {zone.Id}");
                    goto Repeat;
                }

                Ending:;

                logger.LogInformation($"processor id: {Thread.GetCurrentProcessorId()} --- managed thread id: {Thread.CurrentThread.ManagedThreadId} --- timestamp: {DateTime.Now} --- description: zone finder done");
            }
            catch (Exception e)
            {
                logger.LogInformation($"processor id: {Thread.GetCurrentProcessorId()} --- managed thread id: {Thread.CurrentThread.ManagedThreadId} --- timestamp: {DateTime.Now} --- description: there was an error: {e.Message}");
            }
            finally
            {

                //semaphoreSlim.Release();
            }
        }

        private Zone FindZone(List<Candle> candles, FittyCandle fittyCandle, string symbol)
        {
            logger.LogInformation($"processor id: {Thread.GetCurrentProcessorId()} --- managed thread id: {Thread.CurrentThread.ManagedThreadId} --- timestamp: {DateTime.Now} --- description: started finding zone");
            HalfZone up = new HalfZone();
            HalfZone down = new HalfZone();

            down = FindDown(candles, fittyCandle);

            up = FindUp(candles, fittyCandle);

            REPEAT:;

            if (up == default || down == default)
            {
                return default;
            }

            if (up.ExplosiveCandle.HL_Diff < (down.BiggestBaseDiff * (decimal)1.2))
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

            Zone zone = new Zone { From = down.Timestamp, To = up.Timestamp, InstrumentSymbol = symbol, EndIndex = up.ExplosiveCandle.Index };

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
            for (int i = fittyCandle.Index - 1; i >= 0 && Math.Abs(i - fittyCandle.Index) <= 5; i--)
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
                    halfZone.Timestamp = candles[i].Timestamp;
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Candle = candles[i],
                        HL_Diff = diff,
                        RangeFromFitty = Math.Abs(fittyCandle.Index - i)
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
            for (int i = fittyCandle.Index - 1; i <= 0 && Math.Abs(i - fittyCandle.Index) <= 5; i--)
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
                    halfZone.Timestamp = candles[i].Timestamp;
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Candle = candles[i],
                        HL_Diff = diff,
                        RangeFromFitty = Math.Abs(fittyCandle.Index - i)
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
            for (int i = fittyCandle.Index + 1, n = candles.Count; i < n && Math.Abs(i - fittyCandle.Index) <= 5; i++)
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
                    halfZone.Timestamp = candles[i].Timestamp;
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Index = i,
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
            for (int i = fittyCandle.Index + 1, n = candles.Count; i < n && Math.Abs(i - fittyCandle.Index) <= 5; i++)
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
                    halfZone.Timestamp = candles[i].Timestamp;
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Index = i,
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
            logger.LogInformation($"processor id: {Thread.GetCurrentProcessorId()} --- managed thread id: {Thread.CurrentThread.ManagedThreadId} --- timestamp: {DateTime.Now} --- description: starting fitty finder");
            for (int i = index, n = candles.Count; i < n; i++)
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
        Task Start(List<TradeInstrument> instruments, int timeFrame);
    }
}
