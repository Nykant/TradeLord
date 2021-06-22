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

        private static readonly List<Candle> zoneCandles = new List<Candle>();

        public ZoneService(ICandleDbHelper candleDbHelper, IZoneDbHelper zoneDbHelper, ITimeHelper timeHelper, ILogger<ZoneService> logger)
        {
            this.timeHelper = timeHelper;
            this.logger = logger;
            candleHelper = candleDbHelper;
            zoneHelper = zoneDbHelper;
            semaphoreSlim = new SemaphoreSlim(1, 1);
        }

        public List<Candle> GetZoneCandles()
        {
            return zoneCandles;
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

                foreach (var candle in newCandles)
                {
                    zoneCandles.Add(candle);
                }

                logger.LogInformation($"processor id: {Thread.GetCurrentProcessorId()} --- managed thread id: {Thread.CurrentThread.ManagedThreadId} --- timestamp: {DateTime.Now} --- description: finished making new candles. amount: {newCandles.Count}");

                int startIndex = 0;

                Repeat:;

                if (startIndex >= newCandles.Count)
                {
                    goto Ending;
                }

                FittyCandle fittyCandle = await Task.Run(() => FittyFinder(newCandles, startIndex));
                if (fittyCandle == default)
                {
                    goto Ending;
                }
                asDFASDF
                Zone zone = await Task.Run(() => FindZone(newCandles, fittyCandle, instrument.TradingSymbol, startIndex));
                if (zone == default)
                {
                    startIndex = startIndex + 1;
                    goto Repeat;
                }
                else
                {
                    startIndex = zone.EndIndex;
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

        private Zone FindZone(List<Candle> candles, FittyCandle fittyCandle, string symbol, int prevEndIndex)
        {
            logger.LogInformation($"processor id: {Thread.GetCurrentProcessorId()} --- managed thread id: {Thread.CurrentThread.ManagedThreadId} --- timestamp: {DateTime.Now} --- description: started finding zone");

            HalfZone forward = FindForward(candles, fittyCandle);

            if(forward == default)
            {
                return default;
            }

            HalfZone backward = FindBackwards(candles, fittyCandle, forward.Top, forward.Bottom, prevEndIndex);

            if(backward == default)
            {
                return default;
            }

            if(backward.BiggestBaseDiff > forward.BiggestBaseDiff)
            {
                forward = FindForward(candles, fittyCandle, backward.Top, backward.Bottom);
            }

            //REPEAT:;

            //if (forward == default || backward == default)
            //{
            //    return default;
            //}

            //if (forward.ExplosiveCandle.HL_Diff < (backward.BiggestBaseDiff * (decimal)1.2))
            //{
            //    forward = Find(candles, fittyCandle, backward.BiggestBaseDiff);
            //    goto REPEAT;
            //}

            //if(backward.ExplosiveCandle.HL_Diff < (forward.BiggestBaseDiff * (decimal)1.2))
            //{
            //    backward = FindDown(candles, fittyCandle, forward.BiggestBaseDiff, index);
            //    goto REPEAT;
            //}

            int range = forward.ExplosiveCandle.RangeFromFitty + backward.ExplosiveCandle.RangeFromFitty;
            if(range > 6)
            {
                return default;
            }

            Zone zone = new Zone { From = backward.Timestamp, To = forward.Timestamp, InstrumentSymbol = symbol, EndIndex = forward.ExplosiveCandle.Index };

            if(forward.Top > backward.Top)
            {
                zone.Top = forward.Top;
            }
            else
            {
                zone.Top = backward.Top;
            }

            if(forward.Bottom < backward.Bottom)
            {
                zone.Bottom = forward.Bottom;
            }
            else
            {
                zone.Bottom = backward.Bottom;
            }

            return zone;
        }

        //private HalfZone FindBackwards(List<Candle> candles, FittyCandle fittyCandle)
        //{
        //    HalfZone halfZone = new HalfZone { Top = fittyCandle.Candle.High, Bottom = fittyCandle.Candle.Low };
        //    for (int i = fittyCandle.Index - 1; i >= 0 && Math.Abs(i - fittyCandle.Index) < 7; i--)
        //    {
        //        if(halfZone.Top < candles[i].High)
        //        {
        //            halfZone.Top = candles[i].High;
        //        }
        //        if(halfZone.Bottom > candles[i].Low)
        //        {
        //            halfZone.Bottom = candles[i].Low;
        //        }

        //        decimal candleDiff = Math.Abs(candles[i].High - candles[i].Low);
        //        halfZone.BiggestBaseDiff = Math.Abs(halfZone.Top - halfZone.Bottom);
        //        decimal baseDiffx = halfZone.BiggestBaseDiff * (decimal)1.2;
        //        if (candleDiff > baseDiffx)
        //        {
        //            halfZone.Timestamp = candles[i].Timestamp;
        //            halfZone.ExplosiveCandle = new ExplosiveCandle
        //            {
        //                Candle = candles[i],
        //                HL_Diff = candleDiff,
        //                RangeFromFitty = Math.Abs(i - fittyCandle.Index)
        //            };
        //            return halfZone;
        //        }
        //    }
        //    return default;
        //}

        private HalfZone FindBackwards(List<Candle> candles, FittyCandle fittyCandle, decimal top, decimal bottom, int prevEndIndex)
        {
            HalfZone halfZone = new HalfZone { Top = top, Bottom = bottom };
            for (int i = fittyCandle.Index - 1; i >= 0 && i > prevEndIndex && Math.Abs(i - fittyCandle.Index) < 7; i--)
            {

                decimal candleDiff = Math.Abs(candles[i].High - candles[i].Low);
                halfZone.BiggestBaseDiff = Math.Abs(halfZone.Top - halfZone.Bottom);
                decimal baseDiffx = halfZone.BiggestBaseDiff * (decimal)1.2;
                if (candleDiff > baseDiffx)
                {
                    halfZone.Timestamp = candles[i].Timestamp;
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Candle = candles[i],
                        HL_Diff = candleDiff,
                        RangeFromFitty = Math.Abs(i - fittyCandle.Index)
                    };
                    return halfZone;
                }

                if (halfZone.Top < candles[i].High)
                {
                    halfZone.Top = candles[i].High;
                }
                if (halfZone.Bottom > candles[i].Low)
                {
                    halfZone.Bottom = candles[i].Low;
                }
            }
            return default;
        }

        private HalfZone FindForward(List<Candle> candles, FittyCandle fittyCandle)
        {
            HalfZone halfZone = new HalfZone { Top = fittyCandle.Candle.High, Bottom = fittyCandle.Candle.Low };
            for (int i = fittyCandle.Index + 1, n = candles.Count; i < n && Math.Abs(i - fittyCandle.Index) < 7; i++)
            {
                decimal candleDiff = Math.Abs(candles[i].High - candles[i].Low);
                halfZone.BiggestBaseDiff = Math.Abs(halfZone.Top - halfZone.Bottom);
                decimal baseDiffx = halfZone.BiggestBaseDiff * (decimal)1.2;
                if (candleDiff > baseDiffx)
                {
                    halfZone.Timestamp = candles[i].Timestamp;
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Index = i,
                        Candle = candles[i],
                        HL_Diff = candleDiff,
                        RangeFromFitty = Math.Abs(i - fittyCandle.Index)
                    };
                    return halfZone;
                }

                if (halfZone.Top < candles[i].High)
                {
                    halfZone.Top = candles[i].High;
                }
                if (halfZone.Bottom > candles[i].Low)
                {
                    halfZone.Bottom = candles[i].Low;
                }
            }
            return default;
        }

        private HalfZone FindForward(List<Candle> candles, FittyCandle fittyCandle, decimal top, decimal bottom)
        {
            HalfZone halfZone = new HalfZone { Top = top, Bottom = bottom };
            for (int i = fittyCandle.Index + 1, n = candles.Count; i < n && Math.Abs(i - fittyCandle.Index) < 7; i++)
            {

                decimal candleDiff = Math.Abs(candles[i].High - candles[i].Low);
                halfZone.BiggestBaseDiff = Math.Abs(halfZone.Top - halfZone.Bottom);
                decimal baseDiffx = halfZone.BiggestBaseDiff * (decimal)1.2;
                if (candleDiff > baseDiffx)
                {
                    halfZone.Timestamp = candles[i].Timestamp;
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Index = i,
                        Candle = candles[i],
                        HL_Diff = candleDiff,
                        RangeFromFitty = Math.Abs(i - fittyCandle.Index)
                    };
                    return halfZone;
                }

                if (halfZone.Top < candles[i].High)
                {
                    halfZone.Top = candles[i].High;
                }
                if (halfZone.Bottom > candles[i].Low)
                {
                    halfZone.Bottom = candles[i].Low;
                }
            }
            return default;
        }

        private FittyCandle FittyFinder(List<Candle> candles, int index)
        {
            logger.LogInformation($"processor id: {Thread.GetCurrentProcessorId()} --- managed thread id: {Thread.CurrentThread.ManagedThreadId} --- timestamp: {DateTime.Now} --- description: starting fitty finder");
            for (int i = index, n = candles.Count; i < n; i++)
            {
                var HLFitty = (candles[i].High - candles[i].Low) * (decimal)0.5;
                var OCDiff = Math.Abs(candles[i].Open - candles[i].Close);
                if (OCDiff < HLFitty)
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
        List<Candle> GetZoneCandles();
    }
}
