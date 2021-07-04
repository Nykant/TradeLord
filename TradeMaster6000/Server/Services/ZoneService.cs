using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly IInstrumentHelper instrumentHelper;
        private readonly ILogger<ZoneService> logger;
        private readonly IBackgroundJobClient backgroundJobClient;
        private static SemaphoreSlim semaphoreSlim;
        private static readonly CancellationGod ZoneServiceCancel = new CancellationGod();
        private static bool zoneServiceRunning = false;

        private static readonly List<Candle> zoneCandles = new List<Candle>();

        public ZoneService(ICandleDbHelper candleDbHelper, IZoneDbHelper zoneDbHelper, ITimeHelper timeHelper, ILogger<ZoneService> logger, IInstrumentHelper instrumentHelper, IBackgroundJobClient backgroundJobClient)
        {
            this.timeHelper = timeHelper;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            this.instrumentHelper = instrumentHelper;
            candleHelper = candleDbHelper;
            zoneHelper = zoneDbHelper;
            semaphoreSlim = new SemaphoreSlim(1, 1);
        }

        public List<Candle> GetZoneCandles()
        {
            return zoneCandles;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task Start(List<TradeInstrument> instruments, int timeFrame, CancellationToken token)
        {
            zoneServiceRunning = true;
            logger.LogInformation($"zone service starting");
            while (!token.IsCancellationRequested)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < instruments.Count; i++)
                {
                    tasks.Add(ZoneFinder(instruments[i], timeFrame));
                }
                await Task.WhenAll(tasks);

                List<Zone> zones = await zoneHelper.GetUntestedZones();
                List<Zone> updatedZones = new List<Zone>();
                foreach(var zone in zones)
                {
                    List<Candle> candles = candleHelper.GetCandles(zone.InstrumentToken, zone.ExplosiveEndTime);
                    if(zone.SupplyDemand == SupplyDemand.Demand)
                    {
                        zone.Tested = await Task.Run(()=>RallyForwardTest(candles, zone.Top + (Math.Abs(zone.Top - zone.Bottom) / 10)));
                        updatedZones.Add(zone);
                    }
                    else
                    {
                        zone.Tested = await Task.Run(()=>DropForwardTest(candles, zone.Bottom - (Math.Abs(zone.Top - zone.Bottom) / 10)));
                        updatedZones.Add(zone);
                    }
                }

                if (updatedZones.Count > 0)
                {
                    await zoneHelper.Update(updatedZones);
                }

                List<Zone> brokenzones = await zoneHelper.GetUnbrokenZones();
                List<Zone> updatedBrokenZones = new List<Zone>();
                foreach (var zone in brokenzones)
                {
                    List<Candle> candles = candleHelper.GetCandles(zone.InstrumentToken, zone.ExplosiveEndTime);
                    if (zone.SupplyDemand == SupplyDemand.Demand)
                    {
                        zone.Tested = await Task.Run(() => RallyForwardBroken(candles, zone.Bottom));
                        updatedBrokenZones.Add(zone);
                    }
                    else
                    {
                        zone.Tested = await Task.Run(() => DropForwardBroken(candles, zone.Top));
                        updatedBrokenZones.Add(zone);
                    }
                }

                if(updatedBrokenZones.Count > 0)
                {
                    await zoneHelper.Update(updatedBrokenZones);
                }

                logger.LogInformation($"zone service done - time elapsed: {stopwatch.ElapsedMilliseconds}");
                stopwatch.Stop();
                await Task.Delay(300000);
            }
            zoneServiceRunning = false;
        }

        public bool IsZoneServiceRunning()
        {
            return zoneServiceRunning;
        }

        public async Task StartZoneService()
        {
            ZoneServiceCancel.Source = new CancellationTokenSource();
            List<TradeInstrument> instruments = await instrumentHelper.GetTradeInstruments();
            ZoneServiceCancel.HangfireId = backgroundJobClient.Enqueue(() => Start(instruments, 5, ZoneServiceCancel.Source.Token));
        }

        public void CancelToken()
        {
            ZoneServiceCancel.Source.Cancel();
            backgroundJobClient.Delete(ZoneServiceCancel.HangfireId);   
        }

        private List<Candle> TransformCandles(List<Candle> candles, int timeframe)
        {
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
            Candle temp = new();
            int emptyCounter = 0;
            int i = 0;
            int n = candles.Count;
            int candleCounter = 0;
            while (i < n)
            {
                if (candles[i].Timestamp.Hour == time.Hour && candles[i].Timestamp.Minute == time.Minute)
                {
                    candleCounter++;

                    if (temp == default)
                    {
                        temp.Open = candles[i].Open;
                        temp.Timestamp = candles[i].Timestamp;
                    }

                    if (temp.Low > candles[i].Low || temp.Low == default)
                    {
                        temp.Low = candles[i].Low;
                    }
                    if (temp.High < candles[i].High || temp.High == default)
                    {
                        temp.High = candles[i].High;
                    }

                    temp.Close = candles[i].Close;

                    if (emptyCounter + candleCounter == timeframe)
                    {
                        temp.InstrumentToken = candles[i].InstrumentToken;
                        temp.Timeframe = timeframe;
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
                    if (candleCounter > 0)
                    {
                        if (emptyCounter + candleCounter == timeframe)
                        {
                            temp.InstrumentToken = candles[i].InstrumentToken;
                            temp.Timeframe = timeframe;
                            newCandles.Add(temp);
                            candleCounter = 0;
                            emptyCounter = 0;
                            temp = new();
                        }
                    }
                    else
                    {
                        if (emptyCounter == timeframe)
                        {
                            emptyCounter = 0;
                        }
                    }
                }
                time = time.AddMinutes(1);
            }

            return newCandles.OrderBy(x => x.Timestamp).ToList();
        }

        private async Task ZoneFinder(TradeInstrument instrument, int timeFrame)
        {
            List<Candle> baseCandles = new List<Candle>();
            List<Candle> candles15 = new List<Candle>();
            List<Candle> candles30 = new List<Candle>();
            List<Candle> candles45 = new List<Candle>();
            List<Candle> candles60 = new List<Candle>();
            List<Zone> baseZones = new List<Zone>();
            List<Zone> candles15Zones = new List<Zone>();
            List<Zone> candles30Zones = new List<Zone>();
            List<Zone> candles45Zones = new List<Zone>();
            List<Zone> candles60Zones = new List<Zone>();
            try
            {
                List<Candle> candles = await candleHelper.GetUnusedCandles(instrument.Token);
                if (candles.Count == 0)
                {
                    goto Ending;
                }

                try
                {

                    baseCandles = await Task.Run(() => TransformCandles(candles, timeFrame));
                    baseZones = await Task.Run(() => MakeZones(baseCandles, instrument.TradingSymbol, instrument.Token, timeFrame));

                    candles15 = await Task.Run(() => TransformCandles(candles, 15));
                    candles15Zones = await Task.Run(() => MakeZones(candles15, instrument.TradingSymbol, instrument.Token, 15));
                    List<Zone> previous15Zones = await zoneHelper.GetZones(15, instrument.Token);
                    for (int k = 0, n = baseZones.Count; k < n; k++)
                    {
                        if (IsTradeable(candles15Zones, baseZones[k], previous15Zones))
                        {
                            baseZones[k].Tradeable = true;
                            goto SKIP;
                        }
                    }

                    candles30 = await Task.Run(() => TransformCandles(candles, 30));
                    candles30Zones = await Task.Run(() => MakeZones(candles30, instrument.TradingSymbol, instrument.Token, 30));
                    List<Zone> previous30Zones = await zoneHelper.GetZones(30, instrument.Token);
                    for (int k = 0, n = baseZones.Count; k < n; k++)
                    {
                        if (IsTradeable(candles30Zones, baseZones[k], previous30Zones))
                        {
                            baseZones[k].Tradeable = true;
                            goto SKIP;
                        }
                    }

                    candles45 = await Task.Run(() => TransformCandles(candles, 45));
                    candles45Zones = await Task.Run(() => MakeZones(candles45, instrument.TradingSymbol, instrument.Token, 45));
                    List<Zone> previous45Zones = await zoneHelper.GetZones(45, instrument.Token);
                    for (int k = 0, n = baseZones.Count; k < n; k++)
                    {
                        if (IsTradeable(candles45Zones, baseZones[k], previous45Zones))
                        {
                            baseZones[k].Tradeable = true;
                            goto SKIP;
                        }
                    }

                    candles60 = await Task.Run(() => TransformCandles(candles, 60));
                    candles60Zones = await Task.Run(() => MakeZones(candles60, instrument.TradingSymbol, instrument.Token, 60));
                    List<Zone> previous60Zones = await zoneHelper.GetZones(60, instrument.Token);
                    for (int k = 0, n = baseZones.Count; k < n; k++)
                    {
                        if (IsTradeable(candles60Zones, baseZones[k], previous60Zones))
                        {
                            baseZones[k].Tradeable = true;
                        }
                    }

                    SKIP:;
                }
                catch (Exception e)
                {
                    logger.LogInformation(e.Message);
                }

                if (baseZones.Count > 0)
                {
                    await zoneHelper.Add(baseZones);
                    await candleHelper.MarkCandlesUsed(zoneHelper.LastZoneEndTime(baseZones), instrument.Token);
                }

                if(candles15Zones.Count > 0)
                {
                    await zoneHelper.Add(candles15Zones);
                }

                if (candles30Zones.Count > 0)
                {
                    await zoneHelper.Add(candles15Zones);
                }

                if (candles45Zones.Count > 0)
                {
                    await zoneHelper.Add(candles45Zones);
                }

                if (candles60Zones.Count > 0)
                {
                    await zoneHelper.Add(candles60Zones);
                }

                Ending:;
            }
            catch (Exception e)
            {
                logger.LogInformation($"processor id: {Thread.GetCurrentProcessorId()} --- managed thread id: {Thread.CurrentThread.ManagedThreadId} --- timestamp: {DateTime.Now} --- description: there was an error: {e.Message}");
            }
        }

        private bool IsTradeable(List<Zone> htfZones, Zone zone, List<Zone> prevHtfZones)
        {
            // for each higher timeframe zone
            for(int i = 0, n = htfZones.Count; i < n; i++)
            {
                // check if the from(timestamp) of lower timeframe zone is later or same time as higher timeframe from
                // and check if to(timestamp) of lower timeframe zone is earlier or same time as higher timeframe to
                if(DateTime.Compare(htfZones[i].From, zone.From) <= 0 && DateTime.Compare(htfZones[i].To, zone.To) >= 0)
                {
                    Zone motherZone = htfZones[i];
                    // motherzone found!
                    try
                    {
                        // checking all higher timeframe zones backward
                        for (int k = prevHtfZones.Count - 1; k >= 0; k--)
                        {
                            // if motherzone is opposite to prev zone
                            if (prevHtfZones[k].SupplyDemand != motherZone.SupplyDemand)
                            {
                                Zone opposite = prevHtfZones[k];
                                List<Candle> candles = candleHelper.GetCandles(zone.InstrumentToken, opposite.To);
                                int count = candles.Count;
                                int z = 0;
                                // if motherzone is a demand zone
                                if (motherZone.SupplyDemand == SupplyDemand.Demand)
                                {

                                    while (DateTime.Compare(candles[z].Timestamp, motherZone.From) < 0)
                                    {
                                        if (candles[z].High > opposite.Top)
                                        {
                                            return false;
                                        }

                                        z++;
                                    }

                                    while (z < count)
                                    {
                                        if (candles[z].High > opposite.Top)
                                        {
                                            return true;
                                        }
                                        z++;
                                    }
                                }
                                // else if motherzone is a supply zone
                                else
                                {

                                    while (DateTime.Compare(candles[z].Timestamp, motherZone.From) < 0)
                                    {
                                        if (candles[z].Low < opposite.Bottom)
                                        {
                                            return false;
                                        }

                                        z++;
                                    }

                                    while (z < count)
                                    {
                                        if (candles[z].Low < opposite.Bottom)
                                        {
                                            return true;
                                        }
                                        z++;
                                    }
                                }
                                return false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogInformation(e.Message);
                    }
                }
            }
            return false;
        }

        private List<Zone> MakeZones(List<Candle> candles, string symbol, uint token, int timeframe)
        {
            List<Zone> zones = new List<Zone>();
            int startIndex = 0;

            Repeat:;

            if (startIndex >= candles.Count)
            {
                goto Ending;
            }

            FittyCandle fittyCandle = FittyFinder(candles, startIndex);
            if (fittyCandle == default)
            {
                goto Ending;
            }
            startIndex = fittyCandle.Index + 1;

            Zone zone = FindZone(candles, fittyCandle, symbol, token, timeframe);
            if (zone == default)
            {
                goto Repeat;
            }
            else
            {
                zones.Add(zone);
                goto Repeat;
            }

            Ending:;

            zones = zones.OrderBy(x => x.From).ToList();
            return zones;
        }

        private bool RallyForwardTest(List<Candle> candles, decimal theLine)
        {
            for (int i = 0, n = candles.Count; i < n; i++)
            {
                if (theLine >= candles[i].Low)
                {
                    return true;
                }
            }
            return false;
        }

        private bool DropForwardTest(List<Candle> candles, decimal theLine)
        {
            for (int i = 0, n = candles.Count; i < n; i++)
            {
                if (theLine <= candles[i].High)
                {
                    return true;
                }
            }
            return false;
        }

        private bool DropForwardBroken(List<Candle> candles, decimal theLine)
        {
            for (int i = 0, n = candles.Count; i < n; i++)
            {
                if (theLine < candles[i].High)
                {
                    return true;
                }
            }
            return false;
        }

        private bool RallyForwardBroken(List<Candle> candles, decimal theLine)
        {
            for (int i = 0, n = candles.Count; i < n; i++)
            {
                if (theLine > candles[i].Low)
                {
                    return true;
                }
            }
            return false;
        }

        private decimal RallyExplosiveWidthBackward(List<Candle> candles, int explosiveIndex, decimal zoneLow)
        {
            decimal low = zoneLow;
            for (int i = explosiveIndex; i >= 0; i--)
            {
                if (IsFitty(candles[i]))
                {
                    return Math.Abs(low - zoneLow);
                }

                if (low > candles[i].Low)
                {
                    low = candles[i].Low;
                }
            }

            return default;
        }

        private decimal DropExplosiveWidthBackward(List<Candle> candles, int explosiveIndex, decimal zoneHigh)
        {
            decimal high = zoneHigh;
            for (int i = explosiveIndex; i >= 0; i--)
            {
                if (IsFitty(candles[i]))
                {
                    return Math.Abs(high - zoneHigh);
                }

                if (high < candles[i].High)
                {
                    high = candles[i].High;
                }
            }

            return default;
        }

        private decimal RallyExplosiveWidthForward(List<Candle> candles, int explosiveIndex, decimal zoneHigh)
        {
            decimal high = zoneHigh;
            for (int i = explosiveIndex, n = candles.Count; i < n; i++)
            {
                if (IsFitty(candles[i]))
                {
                    return Math.Abs(high - zoneHigh);
                }

                if (high < candles[i].High)
                {
                    high = candles[i].High;
                }
            }

            return default;
        }
        private decimal DropExplosiveWidthForward(List<Candle> candles, int explosiveIndex, decimal zoneLow)
        {
            decimal low = zoneLow;
            for (int i = explosiveIndex, n = candles.Count; i < n; i++)
            {
                if (IsFitty(candles[i]))
                {
                    return Math.Abs(low - zoneLow);
                }

                if (low > candles[i].Low)
                {
                    low = candles[i].Low;
                }
            }

            return default;
        }

        private bool IsFitty(Candle candle)
        {
            var hl = Math.Abs(candle.Low - candle.High);
            if(hl > 0)
            {
                if (Math.Abs(candle.Open - candle.Close) <= (hl * (decimal)0.5))
                {
                    return true;
                }
            }
            return false;
        }

        private Zone FindZone(List<Candle> candles, FittyCandle fittyCandle, string symbol, uint token, int timeframe)
        {
            HalfZone forward = FindForward(candles, fittyCandle);

            if(forward == default)
            {
                return default;
            }

            HalfZone backward = FindBackwards(candles, fittyCandle, forward.Top, forward.Bottom, forward.BiggestBaseDiff);

            if(backward == default)
            {
                return default;
            }

            if(backward.BiggestBaseDiff > forward.BiggestBaseDiff)
            {
                forward = FindForward(candles, fittyCandle, backward.Top, backward.Bottom, backward.BiggestBaseDiff);

                if (forward == default)
                {
                    return default;
                }
            }

            int range = forward.ExplosiveCandle.RangeFromFitty + backward.ExplosiveCandle.RangeFromFitty - 1;
            if(range > 6)
            {
                return default;
            }

            Zone zone = new Zone { From = backward.Timestamp, To = forward.Timestamp, InstrumentSymbol = symbol, InstrumentToken = token, Created = timeHelper.CurrentTime(), ExplosiveEndTime = forward.ExplosiveCandle.Candle.Timestamp, Timeframe = timeframe };

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

            var zoneWidthX2 = Math.Abs(zone.Top - zone.Bottom) * (decimal)1.2;
            var zoneWidthX05 = Math.Abs(zone.Top - zone.Bottom) * (decimal)0.5;
            if (backward.ExplosiveCandle.Candle.Open < backward.ExplosiveCandle.Candle.Close)
            {
                if (forward.ExplosiveCandle.Candle.Open < forward.ExplosiveCandle.Candle.Close)
                {
                    if (backward.ExplosiveCandle.Candle.High > (zone.Top + (Math.Abs(zone.Top - zone.Bottom) / 10)))
                    {
                        return default;
                    }
                    if (forward.ExplosiveCandle.Candle.Low < zone.Bottom)
                    {
                        return default;
                    }

                    if (zoneWidthX2 <= RallyExplosiveWidthForward(candles, forward.ExplosiveCandle.Index, zone.Top)
                        && zoneWidthX05 <= RallyExplosiveWidthBackward(candles, backward.ExplosiveCandle.Index, zone.Bottom))
                    {
                        zone.StayAway = StayAway.Good;
                    }
                    else
                    {
                        return default;
                    }

                    zone.SupplyDemand = SupplyDemand.Demand;
                    zone.ZoneType = ZoneType.RBR;
                }
                else
                {
                    if (backward.ExplosiveCandle.Candle.High > zone.Top)
                    {
                        return default;
                    }
                    if (forward.ExplosiveCandle.Candle.High > zone.Top)
                    {
                        return default;
                    }
                    var one = DropExplosiveWidthForward(candles, forward.ExplosiveCandle.Index, zone.Bottom);
                    var two = RallyExplosiveWidthBackward(candles, backward.ExplosiveCandle.Index, zone.Bottom);
                    if (zoneWidthX2 < one
                        && zoneWidthX05 < two)
                    {
                        zone.StayAway = StayAway.Good;
                    }
                    else
                    {
                        return default;
                    }

                    zone.SupplyDemand = SupplyDemand.Supply;
                    zone.ZoneType = ZoneType.RBD;
                }
            }
            else
            {
                if (forward.ExplosiveCandle.Candle.Open < forward.ExplosiveCandle.Candle.Close)
                {
                    if (backward.ExplosiveCandle.Candle.Low < zone.Bottom)
                    {
                        return default;
                    }
                    if (forward.ExplosiveCandle.Candle.Low < zone.Bottom)
                    {
                        return default;
                    }

                    if (zoneWidthX2 < RallyExplosiveWidthForward(candles, forward.ExplosiveCandle.Index, zone.Top)
                        && zoneWidthX05 < DropExplosiveWidthBackward(candles, backward.ExplosiveCandle.Index, zone.Top))
                    {
                        zone.StayAway = StayAway.Good;
                    }
                    else
                    {
                        return default;
                    }

                    zone.SupplyDemand = SupplyDemand.Demand;
                    zone.ZoneType = ZoneType.DBR;
                }
                else
                {
                    if (backward.ExplosiveCandle.Candle.Low < (zone.Bottom - (Math.Abs(zone.Top - zone.Bottom) / 10)))
                    {
                        return default;
                    }
                    if (forward.ExplosiveCandle.Candle.High > zone.Top)
                    {
                        return default;
                    }

                    if (zoneWidthX2 < DropExplosiveWidthForward(candles, forward.ExplosiveCandle.Index, zone.Bottom)
                        && zoneWidthX05 < DropExplosiveWidthBackward(candles, backward.ExplosiveCandle.Index, zone.Top))
                    {
                        zone.StayAway = StayAway.Good;
                    }
                    else
                    {
                        return default;
                    }

                    zone.SupplyDemand = SupplyDemand.Supply;
                    zone.ZoneType = ZoneType.DBD;
                }
            }

            return zone;
        }

        private HalfZone FindBackwards(List<Candle> candles, FittyCandle fittyCandle, decimal top, decimal bottom, decimal biggestBaseDiff)
        {
            HalfZone halfZone = new HalfZone { Top = top, Bottom = bottom, Timestamp = fittyCandle.Candle.Timestamp, BiggestBaseDiff = biggestBaseDiff };
            decimal baseDiffx;
            decimal candleDiff;
            for (int i = fittyCandle.Index - 1; i >= 0 && Math.Abs(i - fittyCandle.Index) < 7; i--)
            {
                if (IsFitty(candles[i]))
                {
                    return default;
                }

                candleDiff = Math.Abs(candles[i].Low - candles[i].High);
                baseDiffx = halfZone.BiggestBaseDiff * (decimal)1.2;
                if (candleDiff > baseDiffx)
                {
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Index = i,
                        Candle = candles[i],
                        HL_Diff = candleDiff,
                        RangeFromFitty = Math.Abs(fittyCandle.Index - i)
                    };
                    return halfZone;
                }

                if(halfZone.BiggestBaseDiff < candleDiff)
                {
                    halfZone.BiggestBaseDiff = candleDiff;
                }

                if (halfZone.Top < candles[i].High)
                {
                    halfZone.Top = candles[i].High;
                }
                if (halfZone.Bottom > candles[i].Low)
                {
                    halfZone.Bottom = candles[i].Low;
                }

                halfZone.Timestamp = candles[i].Timestamp;
            }
            return default;
        }

        private HalfZone FindForward(List<Candle> candles, FittyCandle fittyCandle)
        {
            decimal baseDiffx;
            decimal candleDiff;
            HalfZone halfZone = new HalfZone { Top = fittyCandle.Candle.High, Bottom = fittyCandle.Candle.Low, Timestamp = fittyCandle.Candle.Timestamp, BiggestBaseDiff = Math.Abs(fittyCandle.Candle.Low - fittyCandle.Candle.High) };
            for (int i = fittyCandle.Index + 1, n = candles.Count; i < n && Math.Abs(i - fittyCandle.Index) < 7; i++)
            {
                candleDiff = Math.Abs(candles[i].Low - candles[i].High);
                if (IsFitty(candles[i]))
                {
                    goto SKIP;
                }

                baseDiffx = halfZone.BiggestBaseDiff * (decimal)1.2;
                if (candleDiff > baseDiffx)
                {
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Index = i,
                        Candle = candles[i],
                        HL_Diff = candleDiff,
                        RangeFromFitty = Math.Abs(i - fittyCandle.Index)
                    };
                    return halfZone;
                }

                SKIP:;

                if (halfZone.BiggestBaseDiff < candleDiff)
                {
                    halfZone.BiggestBaseDiff = candleDiff;
                }

                if (halfZone.Top < candles[i].High)
                {
                    halfZone.Top = candles[i].High;
                }
                if (halfZone.Bottom > candles[i].Low)
                {
                    halfZone.Bottom = candles[i].Low;
                }

                halfZone.Timestamp = candles[i].Timestamp;
            }
            return default;
        }

        private HalfZone FindForward(List<Candle> candles, FittyCandle fittyCandle, decimal top, decimal bottom, decimal biggestBaseDiff)
        {
            decimal baseDiffx;
            decimal candleDiff;
            HalfZone halfZone = new HalfZone { Top = top, Bottom = bottom, Timestamp = fittyCandle.Candle.Timestamp, BiggestBaseDiff = biggestBaseDiff };
            for (int i = fittyCandle.Index + 1, n = candles.Count; i < n && Math.Abs(i - fittyCandle.Index) < 7; i++)
            {
                candleDiff = Math.Abs(candles[i].Low - candles[i].High);
                if (IsFitty(candles[i]))
                {
                    goto SKIP;
                }

                baseDiffx = halfZone.BiggestBaseDiff * (decimal)1.2;
                if (candleDiff > baseDiffx)
                {
                    halfZone.ExplosiveCandle = new ExplosiveCandle
                    {
                        Index = i,
                        Candle = candles[i],
                        HL_Diff = candleDiff,
                        RangeFromFitty = Math.Abs(i - fittyCandle.Index)
                    };
                    return halfZone;
                }

                SKIP:;

                if (halfZone.BiggestBaseDiff < candleDiff)
                {
                    halfZone.BiggestBaseDiff = candleDiff;
                }

                if (halfZone.Top < candles[i].High)
                {
                    halfZone.Top = candles[i].High;
                }
                if (halfZone.Bottom > candles[i].Low)
                {
                    halfZone.Bottom = candles[i].Low;
                }

                halfZone.Timestamp = candles[i].Timestamp;
            }
            return default;
        }

        private FittyCandle FittyFinder(List<Candle> candles, int index)
        {
            for (int i = index, n = candles.Count; i < n; i++)
            {
                if (IsFitty(candles[i]))
                {
                    return new FittyCandle { Candle = candles[i], Index = i };
                }
            }
            return default;
        }
    }
    public interface IZoneService
    {
        Task Start(List<TradeInstrument> instruments, int timeFrame, CancellationToken token);
        List<Candle> GetZoneCandles();
        void CancelToken();
        Task StartZoneService();
        bool IsZoneServiceRunning();
    }
}
