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
        // entry zone factors
        private double entryExplosiveFactor = 1.2;
        private double entryPreExplosiveFactor = 1.2;
        private int entryNoOfCandles = 6;
        private double entryZoneWidthFactor = 1.2;
        private double entryPreBaseWidthFactor = 0.5;
        private double entryTestingfactor = 0.1;
        private double entrySelfTestingFactor = 0.1;
        private double entryPreBaseOvershootFactor = 0.1;

        // curve zone factors
        private double curveExplosiveFactor = 0.8;
        private double curvePreExplosiveFactor = 0.8;
        private int curveNoOfCandles = 19;
        private double curveZoneWidthFactor = 0.4;
        private double curvePrebasewidthfactor = 0.2;
        private double curveTestingfactor = -0.9;
        private double curveSelfTestingFactor = -0.9;
        private double curvePreBaseOvershootFactor = 0.0;

        // -------------------------------------------

        private readonly ICandleDbHelper candleHelper;
        private readonly IZoneDbHelper zoneHelper;
        private readonly ITimeHelper timeHelper;
        private readonly IInstrumentHelper instrumentHelper;
        private readonly ILogger<ZoneService> logger;
        private readonly IBackgroundJobClient backgroundJobClient;
        private static SemaphoreSlim semaphoreSlim;
        private static readonly CancellationGod ZoneServiceCancel = new CancellationGod();
        private static readonly CancellationGod TransformCancel = new CancellationGod();
        private static bool zoneServiceRunning { get; set; } = false;

        private static readonly List<Candle> zoneCandles = new List<Candle>();

        public ZoneService(ICandleDbHelper candleDbHelper, IZoneDbHelper zoneDbHelper, ITimeHelper timeHelper, ILogger<ZoneService> logger, IInstrumentHelper instrumentHelper, IBackgroundJobClient backgroundJobClient)
        {
            this.timeHelper = timeHelper;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            this.instrumentHelper = instrumentHelper;
            candleHelper = candleDbHelper;
            zoneHelper = zoneDbHelper;
            semaphoreSlim = new SemaphoreSlim(30, 30);
        }

        public List<Candle> GetZoneCandles()
        {
            return zoneCandles;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task StartOnce(List<TradeInstrument> instruments, int timeFrame, CancellationToken token)
        {
            try
            {
                zoneServiceRunning = true;
                logger.LogInformation($"zone service starting");

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                List<Task> tasks = new List<Task>();
                List<Candle> candles = await candleHelper.GetUnusedNonBaseCandles();
                for (int i = 0; i < instruments.Count; i++)
                {
                    tasks.Add(ZoneFinder(candles.Where(x => x.InstrumentToken == instruments[i].Token).ToList(), timeFrame, instruments[i]));
                }
                await Task.WhenAll(tasks);

                candles = await candleHelper.GetAll5minCandles();
                List<Zone> unbrokenzones = await zoneHelper.GetUnbrokenZones();
                List<Candle> dynamicCandles;
                List<Zone> updatedZones = new List<Zone>();

                foreach (var zone in unbrokenzones)
                {
                    dynamicCandles = candles.Where(x => x.InstrumentToken == zone.InstrumentToken && DateTime.Compare(x.Timestamp, zone.ExplosiveEndTime.AddMinutes(zone.Timeframe)) > 0).ToList();
                    if (zone.SupplyDemand == SupplyDemand.Demand)
                    {
                        int result = await Task.Run(() => RallyForwardTest(dynamicCandles, zone.Top + (Math.Abs(zone.Top - zone.Bottom) / 10), zone.Bottom));
                        if (result == 1)
                        {
                            zone.Tested = true;
                            updatedZones.Add(zone);
                        }
                        else if (result == 2)
                        {
                            zone.Tested = true;
                            zone.Broken = true;
                            updatedZones.Add(zone);
                        }
                    }
                    else
                    {
                        int result = await Task.Run(() => DropForwardTest(dynamicCandles, zone.Bottom - (Math.Abs(zone.Top - zone.Bottom) / 10), zone.Top));
                        if (result == 1)
                        {
                            zone.Tested = true;
                            updatedZones.Add(zone);
                        }
                        else if (result == 2)
                        {
                            zone.Tested = true;
                            zone.Broken = true;
                            updatedZones.Add(zone);
                        }
                        updatedZones.Add(zone);
                    }
                }

                if (updatedZones.Count > 0)
                {
                    await zoneHelper.Update(updatedZones).ConfigureAwait(false);
                }

                logger.LogInformation($"zone service done - time elapsed: {stopwatch.ElapsedMilliseconds}");
                stopwatch.Stop();

                zoneServiceRunning = false;
            }
            catch (Exception e)
            {
                logger.LogInformation(e.Message);
            }

        }

        [AutomaticRetry(Attempts = 0)]
        public async Task Start(List<TradeInstrument> instruments, int timeFrame, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    zoneServiceRunning = true;
                    logger.LogInformation($"zone service starting");

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    List<Task> tasks = new List<Task>();
                    List<Candle> candles = await candleHelper.GetUnusedNonBaseCandles();
                    for (int i = 0; i < instruments.Count; i++)
                    {
                        tasks.Add(ZoneFinder(candles.Where(x => x.InstrumentToken == instruments[i].Token).ToList(), timeFrame, instruments[i]));
                    }
                    await Task.WhenAll(tasks);

                    candles = await candleHelper.GetAll5minCandles();
                    List<Zone> unbrokenzones = await zoneHelper.GetUnbrokenZones();
                    List<Candle> dynamicCandles;
                    List<Zone> updatedZones = new List<Zone>();
                    double testingfactor;
                    foreach (var zone in unbrokenzones)
                    {

                        if (zone.Timeframe == 5)
                        {
                            testingfactor = entryTestingfactor;
                        }
                        else
                        {
                            testingfactor = curveTestingfactor;
                        }

                        dynamicCandles = candles.Where(x => x.InstrumentToken == zone.InstrumentToken && DateTime.Compare(x.Timestamp, zone.ExplosiveEndTime.AddMinutes(zone.Timeframe)) > 0).ToList();
                        if (zone.SupplyDemand == SupplyDemand.Demand)
                        {
                            int result = await Task.Run(() => RallyForwardTest(dynamicCandles, zone.Top + (Math.Abs(zone.Top - zone.Bottom) * (decimal)testingfactor), zone.Bottom));
                            if (result == 1)
                            {
                                zone.Tested = true;
                                updatedZones.Add(zone);
                            }
                            else if (result == 2)
                            {
                                zone.Tested = true;
                                zone.Broken = true;
                                updatedZones.Add(zone);
                            }
                        }
                        else
                        {
                            int result = await Task.Run(() => DropForwardTest(dynamicCandles, zone.Bottom - (Math.Abs(zone.Top - zone.Bottom) * (decimal)testingfactor), zone.Top));
                            if (result == 1)
                            {
                                zone.Tested = true;
                                updatedZones.Add(zone);
                            }
                            else if (result == 2)
                            {
                                zone.Tested = true;
                                zone.Broken = true;
                                updatedZones.Add(zone);
                            }
                            updatedZones.Add(zone);
                        }
                    }

                    if (updatedZones.Count > 0)
                    {
                        await zoneHelper.Update(updatedZones).ConfigureAwait(false);
                    }

                    logger.LogInformation($"zone service done - time elapsed: {stopwatch.ElapsedMilliseconds}");
                    stopwatch.Stop();
                    await Task.Delay(60000);

                    zoneServiceRunning = false;
                }
                catch (Exception e)
                {
                    logger.LogInformation(e.Message);
                }
            }
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
            TransformCancel.Source = new CancellationTokenSource();
            TransformCancel.HangfireId = backgroundJobClient.Enqueue(() => StartTransform(TransformCancel.Source.Token));
        }

        public async Task StartZoneServiceOnce()
        {
            ZoneServiceCancel.Source = new CancellationTokenSource();
            List<TradeInstrument> instruments = await instrumentHelper.GetTradeInstruments();
            await StartOnce(instruments, 5, ZoneServiceCancel.Source.Token);
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
            Candle current = new();
            int i = 0;
            int n = candles.Count;
            int candleCounter = 0;
            int emptyCounter = 0;
            while (i < n)
            {
                current = candles[i];
                if (current.Timestamp.Hour == time.Hour && current.Timestamp.Minute == time.Minute)
                {
                    candleCounter++;

                    if (temp.High == 0)
                    {
                        temp.Open = current.Open;
                        temp.Timestamp = current.Timestamp;
                        temp.Low = current.Low;
                        temp.High = current.High;
                        temp.InstrumentToken = current.InstrumentToken;
                        temp.Timeframe = timeframe;
                    }

                    if (temp.Low > current.Low)
                    {
                        temp.Low = current.Low;
                    }
                    if (temp.High < current.High)
                    {
                        temp.High = current.High;
                    }

                    temp.Close = current.Close;

                    if (candleCounter + emptyCounter == timeframe)
                    {
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

                    if(emptyCounter + candleCounter == timeframe)
                    {
                        if(candleCounter > 0)
                        {
                            newCandles.Add(temp);
                            candleCounter = 0;
                            emptyCounter = 0;
                            temp = new();
                        }
                    }

                    if (current.Timestamp.Hour == 9 && current.Timestamp.Minute == 15)
                    {
                        time.AddDays(1);
                        time = new DateTime(time.Year, time.Month, time.Day, 9, 14, 00);
                        candleCounter = 0;
                        emptyCounter = 0;
                        temp = new();
                    }
                }
                time = time.AddMinutes(1);
            }

            return newCandles.OrderBy(x => x.Timestamp).ToList();
        }

        public async Task StartTransform(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                TransformCancel.Source = new CancellationTokenSource();
                List<Candle> candles = await candleHelper.GetUnusedCandles(1);
                List<TradeInstrument> instruments = await instrumentHelper.GetTradeInstruments();
                List<Task> tasks = new List<Task>();
                foreach (var instrument in instruments)
                {
                    tasks.Add(Transform(instrument.Token, candles.Where(x => x.InstrumentToken == instrument.Token).ToList()));
                }
                await Task.WhenAll(tasks);
                stopwatch.Stop();
                logger.LogInformation($"transform done - estimated time: {stopwatch.ElapsedMilliseconds}");
                await Task.Delay(60000);
            }
        }

        private async Task Transform(uint token, List<Candle> candles)
        {
            List<Candle> updatecandles = new List<Candle>();
            List<Candle> baseCandles = new List<Candle>();
            List<Candle> candles15 = new List<Candle>();
            List<Candle> candles30 = new List<Candle>();
            List<Candle> candles45 = new List<Candle>();
            List<Candle> candles60 = new List<Candle>();
            if (candles.Count < 5)
            {
                goto Ending;
            }
            if(candles.Count > 4)
            {
                var list = candles.Where(x => x.UsedBy5 == false).OrderBy(x => x.Timestamp).ToList();
                if (list.Count > 0)
                {
                    baseCandles = await Task.Run(() => TransformCandles(list, 5));
                    if (baseCandles.Count > 0)
                    {
                        for (int i = 0, n = candles.Count; i < n; i++)
                        {
                            if (DateTime.Compare(candles[i].Timestamp, baseCandles[^1].Timestamp.AddMinutes(5)) < 0)
                            {
                                candles[i].UsedBy5 = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            if(candles.Count > 14)
            {
                var list = candles.Where(x => x.UsedBy15 == false).OrderBy(x => x.Timestamp).ToList();
                if (list.Count > 0)
                {
                    candles15 = await Task.Run(() => TransformCandles(list, 15));
                    if (candles15.Count > 0)
                    {
                        for (int i = 0, n = candles.Count; i < n; i++)
                        {
                            if (DateTime.Compare(candles[i].Timestamp, candles15[^1].Timestamp.AddMinutes(15)) < 0)
                            {
                                candles[i].UsedBy15 = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                        baseCandles.AddRange(candles15);
                    }
                }
            }
            if(candles.Count > 29)
            {
                var list = candles.Where(x => x.UsedBy30 == false).OrderBy(x => x.Timestamp).ToList();
                if (list.Count > 0)
                {
                    candles30 = await Task.Run(() => TransformCandles(list, 30));
                    if (candles30.Count > 0)
                    {
                        for (int i = 0, n = candles.Count; i < n; i++)
                        {
                            if (DateTime.Compare(candles[i].Timestamp, candles30[^1].Timestamp.AddMinutes(30)) < 0)
                            {
                                candles[i].UsedBy30 = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                        baseCandles.AddRange(candles30);
                    }
                }
            }
            if(candles.Count > 44)
            {
                var list = candles.Where(x => x.UsedBy45 == false).OrderBy(x => x.Timestamp).ToList();
                if(list.Count > 0)
                {
                    candles45 = await Task.Run(() => TransformCandles(list, 45));
                    if (candles45.Count > 0)
                    {
                        for (int i = 0, n = candles.Count; i < n; i++)
                        {
                            if (DateTime.Compare(candles[i].Timestamp, candles45[^1].Timestamp.AddMinutes(45)) < 0)
                            {
                                candles[i].UsedBy45 = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                        baseCandles.AddRange(candles45);
                    }
                }
            }
            if(candles.Count > 59)
            {
                var list = candles.Where(x => x.UsedBy60 == false).OrderBy(x => x.Timestamp).ToList();
                if(list.Count > 0)
                {
                    candles60 = await Task.Run(() => TransformCandles(list, 60));
                    if (candles60.Count > 0)
                    {
                        for (int i = 0, n = candles.Count; i < n; i++)
                        {
                            if (DateTime.Compare(candles[i].Timestamp, candles60[^1].Timestamp.AddMinutes(60)) < 0)
                            {
                                candles[i].UsedBy60 = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                        baseCandles.AddRange(candles60);
                    }
                }
            }
            if(candles.Count > 0)
            {
                for (int i = 0, n = candles.Count; i < n; i++)
                {
                    if (candles[i].UsedBy5 && candles[i].UsedBy15 && candles[i].UsedBy30 && candles[i].UsedBy45 && candles[i].UsedBy60)
                    {
                        candles[i].Used = true;
                    }
                }
            }
            await semaphoreSlim.WaitAsync();
            try
            {
                if(baseCandles.Count > 0)
                {
                    await candleHelper.Add(baseCandles).ConfigureAwait(false);
                }
                await candleHelper.Update(candles).ConfigureAwait(false);
            }
            finally
            {
                semaphoreSlim.Release();
            }
            Ending:;
        }

        private async Task ZoneFinder(List<Candle> candles, int timeFrame, TradeInstrument instrument)
        {
            List<Zone> baseZones = new List<Zone>();
            List<Zone> zones15 = new List<Zone>();
            List<Zone> zones30 = new List<Zone>();
            List<Zone> zones45 = new List<Zone>();
            List<Zone> zones60 = new List<Zone>();
            List<Zone> newzones = new List<Zone>();
            List<Candle> updatedcandles = new List<Candle>();
            List<Candle> baseCandles = candles.Where(x => x.Timeframe == timeFrame).OrderBy(x => x.Timestamp).ToList();
            List<Candle> candles15 = candles.Where(x => x.Timeframe == 15).OrderBy(x => x.Timestamp).ToList();
            List<Candle> candles30 = candles.Where(x => x.Timeframe == 30).OrderBy(x => x.Timestamp).ToList();
            List<Candle> candles45 = candles.Where(x => x.Timeframe == 45).OrderBy(x => x.Timestamp).ToList();
            List<Candle> candles60 = candles.Where(x => x.Timeframe == 60).OrderBy(x => x.Timestamp).ToList();
            if(baseCandles.Count > 0)
            {
                baseZones = await Task.Run(() => MakeZones(baseCandles, instrument.TradingSymbol, instrument.Token, timeFrame));
            }
            if(candles15.Count > 0)
            {
                zones15 = await Task.Run(() => MakeZones(candles15, instrument.TradingSymbol, instrument.Token, 15));
            }
            if (candles30.Count > 0)
            {
                zones30 = await Task.Run(() => MakeZones(candles30, instrument.TradingSymbol, instrument.Token, 30));
            }
            if (candles45.Count > 0)
            {
                zones45 = await Task.Run(() => MakeZones(candles45, instrument.TradingSymbol, instrument.Token, 45));
            }
            if (candles60.Count > 0)
            {
                zones60 = await Task.Run(() => MakeZones(candles60, instrument.TradingSymbol, instrument.Token, 60));
            }

            if (baseZones.Count > 0)
            {
                for(int i = 0, n = baseCandles.Count; i < n; i++)
                {
                    if(DateTime.Compare(baseCandles[i].Timestamp, baseZones[^1].To.AddMinutes(baseZones[^1].Timeframe)) < 0)
                    {
                        baseCandles[i].Used = true;
                        updatedcandles.Add(baseCandles[i]);
                    }
                }
            }
            if (zones15.Count > 0)
            {
                for (int i = 0, n = candles15.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles15[i].Timestamp, zones15[^1].To.AddMinutes(zones15[^1].Timeframe)) < 0)
                    {
                        candles15[i].Used = true;
                        updatedcandles.Add(candles15[i]);
                    }
                }
                baseZones.AddRange(zones15);
            }
            if (zones30.Count > 0)
            {
                for (int i = 0, n = candles30.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles30[i].Timestamp, zones30[^1].To.AddMinutes(zones30[^1].Timeframe)) < 0)
                    {
                        candles30[i].Used = true;
                        updatedcandles.Add(candles30[i]);
                    }
                }
                baseZones.AddRange(zones30);
            }
            if (zones45.Count > 0)
            {
                for (int i = 0, n = candles45.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles45[i].Timestamp, zones45[^1].To.AddMinutes(zones45[^1].Timeframe)) < 0)
                    {
                        candles45[i].Used = true;
                        updatedcandles.Add(candles45[i]);
                    }
                }
                baseZones.AddRange(zones45);
            }
            if (zones60.Count > 0)
            {
                for (int i = 0, n = candles60.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles60[i].Timestamp, zones60[^1].To.AddMinutes(zones60[^1].Timeframe)) < 0)
                    {
                        candles60[i].Used = true;
                        updatedcandles.Add(candles60[i]);
                    }
                }
                baseZones.AddRange(zones60);
            }

            await semaphoreSlim.WaitAsync();
            try
            {
                if(baseZones.Count > 0)
                {
                    await zoneHelper.Add(baseZones);
                }
                if(updatedcandles.Count > 0)
                {
                    await candleHelper.Update(updatedcandles);
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
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

            zones = zones.OrderBy(x => x.To).ToList();
            return zones;
        }

        private int RallyForwardTest(List<Candle> candles, decimal tested, decimal broken)
        {
            bool testedbool = false;
            bool brokenbool = false;
            for (int i = 0, n = candles.Count; i < n; i++)
            {
                if (broken > candles[i].Low)
                {
                    brokenbool = true;
                }
                if (tested >= candles[i].Low)
                {
                    testedbool = true;
                }

                if(brokenbool && testedbool)
                {
                    return 2;
                }
            }
            if (testedbool)
            {
                return 1;
            }
            return 0;
        }

        private int DropForwardTest(List<Candle> candles, decimal tested, decimal broken)
        {
            bool testedbool = false;
            bool brokenbool = false;
            for (int i = 0, n = candles.Count; i < n; i++)
            {
                if (broken < candles[i].High)
                {
                    brokenbool = true;
                }
                if (tested <= candles[i].High)
                {
                    testedbool = true;
                }

                if (brokenbool && testedbool)
                {
                    return 2;
                }
            }
            if (testedbool)
            {
                return 1;
            }
            return 0;
        }

        private decimal RallyExplosiveWidthBackward(List<Candle> candles, int explosiveIndex, decimal zoneLow, decimal overshoot)
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

                if (candles[i].High >= overshoot)
                {
                    return default;
                }
            }

            return default;
        }

        private decimal DropExplosiveWidthBackward(List<Candle> candles, int explosiveIndex, decimal zoneHigh, decimal overshoot)
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

                if (candles[i].Low <= overshoot)
                {
                    return default;
                }
            }

            return default;
        }

        private decimal RallyExplosiveWidthForward(List<Candle> candles, int explosiveIndex, decimal zoneHigh, decimal overshoot)
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

                if (candles[i].Low <= overshoot)
                {
                    return default;
                }
            }

            return default;
        }
        private decimal DropExplosiveWidthForward(List<Candle> candles, int explosiveIndex, decimal zoneLow, decimal overshoot)
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

                if (candles[i].High >= overshoot)
                {
                    return default;
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
            double explosiveFactor;
            double preExplosiveFactor;
            int noOfCandles;
            double zoneWidthFactor;
            double preBaseWidthFactor;
            double selfTestingFactor;
            double preBaseOvershootFactor;
            if (timeframe == 5)
            {
                explosiveFactor = entryExplosiveFactor;
                preExplosiveFactor = entryPreExplosiveFactor;
                noOfCandles = entryNoOfCandles;
                zoneWidthFactor = entryZoneWidthFactor;
                preBaseWidthFactor = entryPreBaseWidthFactor;
                selfTestingFactor = entrySelfTestingFactor;
                preBaseOvershootFactor = entryPreBaseOvershootFactor;
            }
            else
            {
                explosiveFactor = curveExplosiveFactor;
                preExplosiveFactor = curvePreExplosiveFactor;
                noOfCandles = curveNoOfCandles;
                zoneWidthFactor = curveZoneWidthFactor;
                preBaseWidthFactor = curvePrebasewidthfactor;
                selfTestingFactor = curveSelfTestingFactor;
                preBaseOvershootFactor = curvePreBaseOvershootFactor;
            }

            HalfZone forward = FindForward(candles, fittyCandle, noOfCandles, explosiveFactor);

            if(forward == default)
            {
                return default;
            }

            HalfZone backward = FindBackwards(candles, fittyCandle, forward.Top, forward.Bottom, forward.BiggestBaseDiff, noOfCandles, preExplosiveFactor);

            if(backward == default)
            {
                return default;
            }

            if(backward.BiggestBaseDiff > forward.BiggestBaseDiff)
            {
                forward = FindForward(candles, fittyCandle, backward.Top, backward.Bottom, backward.BiggestBaseDiff, noOfCandles, explosiveFactor);

                if (forward == default)
                {
                    return default;
                }
            }

            int range = forward.ExplosiveCandle.RangeFromFitty + backward.ExplosiveCandle.RangeFromFitty - 1;
            if(range > noOfCandles)
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

            var zoneWidthX2 = Math.Abs(zone.Top - zone.Bottom) * (decimal)zoneWidthFactor;
            var zoneWidthX05 = Math.Abs(zone.Top - zone.Bottom) * (decimal)preBaseWidthFactor;
            if (backward.ExplosiveCandle.Candle.Open < backward.ExplosiveCandle.Candle.Close)
            {
                if (forward.ExplosiveCandle.Candle.Open < forward.ExplosiveCandle.Candle.Close)
                {
                    if (backward.ExplosiveCandle.Candle.High > (zone.Top + (Math.Abs(zone.Top - zone.Bottom) * (decimal)selfTestingFactor)))
                    {
                        return default;
                    }
                    if (forward.ExplosiveCandle.Candle.Low < zone.Bottom)
                    {
                        return default;
                    }

                    if (zoneWidthX2 <= RallyExplosiveWidthForward(candles, forward.ExplosiveCandle.Index, zone.Top, zone.Bottom)
                        && zoneWidthX05 <= RallyExplosiveWidthBackward(candles, backward.ExplosiveCandle.Index, zone.Bottom , zone.Top + (Math.Abs(zone.Top - zone.Bottom) * (decimal)preBaseOvershootFactor)))
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
                    var one = DropExplosiveWidthForward(candles, forward.ExplosiveCandle.Index, zone.Bottom, zone.Top);
                    var two = RallyExplosiveWidthBackward(candles, backward.ExplosiveCandle.Index, zone.Bottom, zone.Top);
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

                    if (zoneWidthX2 < RallyExplosiveWidthForward(candles, forward.ExplosiveCandle.Index, zone.Top, zone.Bottom)
                        && zoneWidthX05 < DropExplosiveWidthBackward(candles, backward.ExplosiveCandle.Index, zone.Top, zone.Bottom))
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
                    if (backward.ExplosiveCandle.Candle.Low < (zone.Bottom - (Math.Abs(zone.Top - zone.Bottom) * (decimal)selfTestingFactor)))
                    {
                        return default;
                    }
                    if (forward.ExplosiveCandle.Candle.High > zone.Top)
                    {
                        return default;
                    }

                    if (zoneWidthX2 < DropExplosiveWidthForward(candles, forward.ExplosiveCandle.Index, zone.Bottom, zone.Top)
                        && zoneWidthX05 < DropExplosiveWidthBackward(candles, backward.ExplosiveCandle.Index, zone.Top, zone.Bottom - (Math.Abs(zone.Top - zone.Bottom) * (decimal)preBaseOvershootFactor)))
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

        private HalfZone FindBackwards(List<Candle> candles, FittyCandle fittyCandle, decimal top, decimal bottom, decimal biggestBaseDiff, int noOfCandles, double preExplosiveFactor)
        {
            HalfZone halfZone = new HalfZone { Top = top, Bottom = bottom, Timestamp = fittyCandle.Candle.Timestamp, BiggestBaseDiff = biggestBaseDiff };
            decimal baseDiffx;
            decimal candleDiff;
            for (int i = fittyCandle.Index - 1; i >= 0 && Math.Abs(i - fittyCandle.Index) < noOfCandles; i--)
            {
                if (IsFitty(candles[i]))
                {
                    return default;
                }

                candleDiff = Math.Abs(candles[i].Low - candles[i].High);
                baseDiffx = halfZone.BiggestBaseDiff * (decimal)preExplosiveFactor;
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

        private HalfZone FindForward(List<Candle> candles, FittyCandle fittyCandle, int noofcandles, double explosiveFactor)
        {
            decimal baseDiffx;
            decimal candleDiff;
            HalfZone halfZone = new HalfZone { Top = fittyCandle.Candle.High, Bottom = fittyCandle.Candle.Low, Timestamp = fittyCandle.Candle.Timestamp, BiggestBaseDiff = Math.Abs(fittyCandle.Candle.Low - fittyCandle.Candle.High) };
            for (int i = fittyCandle.Index + 1, n = candles.Count; i < n && Math.Abs(i - fittyCandle.Index) < noofcandles; i++)
            {
                candleDiff = Math.Abs(candles[i].Low - candles[i].High);
                if (IsFitty(candles[i]))
                {
                    goto SKIP;
                }

                baseDiffx = halfZone.BiggestBaseDiff * (decimal)explosiveFactor;
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

        private HalfZone FindForward(List<Candle> candles, FittyCandle fittyCandle, decimal top, decimal bottom, decimal biggestBaseDiff, int noOfCandles, double explosiveFactor)
        {
            decimal baseDiffx;
            decimal candleDiff;
            HalfZone halfZone = new HalfZone { Top = top, Bottom = bottom, Timestamp = fittyCandle.Candle.Timestamp, BiggestBaseDiff = biggestBaseDiff };
            for (int i = fittyCandle.Index + 1, n = candles.Count; i < n && Math.Abs(i - fittyCandle.Index) < noOfCandles; i++)
            {
                candleDiff = Math.Abs(candles[i].Low - candles[i].High);
                if (IsFitty(candles[i]))
                {
                    goto SKIP;
                }

                baseDiffx = halfZone.BiggestBaseDiff * (decimal)explosiveFactor;
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
        Task StartZoneServiceOnce();
    }
}
