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
        private readonly ITradeabilityService tradeabilityService;
        private static SemaphoreSlim semaphoreSlim;
        private static readonly CancellationGod ZoneServiceCancel = new CancellationGod();
        private static readonly CancellationGod TransformCancel = new CancellationGod();
        private static bool ZoneServiceRunning { get; set; } = false;

        private static readonly List<Candle> zoneCandles = new List<Candle>();
        private static readonly ZoneFactors entry = new ZoneFactors
        {
            Name = "entry",
            ExplosiveFactor = 1.2,
            NoOfCandles = 6,
            PreBaseOvershootFactor = 0.1,
            PreBaseWidthFactor = 0.5,
            PreExplosiveFactor = 1.2,
            TestingFactor = 0.1,
            SelfTestingFactor = 0.1,
            ZoneWidthFactor = 1.2
        };
        private static readonly ZoneFactors curve = new ZoneFactors
        {
            Name = "curve",
            ExplosiveFactor = 0.8,
            NoOfCandles = 19,
            PreBaseOvershootFactor = 0.0,
            PreBaseWidthFactor = 0.2,
            PreExplosiveFactor = 0.8,
            TestingFactor = -0.9,
            SelfTestingFactor = -0.9,
            ZoneWidthFactor = 0.4
        };

        public ZoneService(ICandleDbHelper candleDbHelper, IZoneDbHelper zoneDbHelper, ITimeHelper timeHelper, ILogger<ZoneService> logger, IInstrumentHelper instrumentHelper, IBackgroundJobClient backgroundJobClient, ITradeabilityService tradeabilityService)
        {
            this.timeHelper = timeHelper;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            this.instrumentHelper = instrumentHelper;
            this.tradeabilityService = tradeabilityService;
            candleHelper = candleDbHelper;
            zoneHelper = zoneDbHelper;
            semaphoreSlim = new SemaphoreSlim(30, 30);
        }

        public List<Candle> GetZoneCandles()
        {
            return zoneCandles;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task StartOnce(List<TradeInstrument> instruments, CancellationToken token)
        {
            try
            {
                await StartTransform(TransformCancel.Source.Token);

                ZoneServiceRunning = true;
                logger.LogInformation($"zone service starting");

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                List<Task> tasks = new List<Task>();
                List<Candle> candles = await candleHelper.GetUnusedNonBaseCandles();
                for (int i = 0; i < instruments.Count; i++)
                {
                    tasks.Add(ZoneFinder(candles.Where(x => x.InstrumentToken == instruments[i].Token).ToList(), instruments[i]));
                }
                await Task.WhenAll(tasks);

                candles = await candleHelper.GetAll5minCandles();
                List<Zone> unbrokenzones = await zoneHelper.GetUnbrokenZones();
                List<Candle> dynamicCandles;
                List<Zone> updatedZones = new List<Zone>();
                double testingfactor;
                foreach (var zone in unbrokenzones)
                {
                    if (zone.Entry)
                    {
                        testingfactor = entry.TestingFactor;
                    }
                    else
                    {
                        testingfactor = curve.TestingFactor;
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
                    await zoneHelper.Update(updatedZones);
                }

                stopwatch.Stop();
                logger.LogInformation($"zone service done - time elapsed: {stopwatch.ElapsedMilliseconds}");

                await tradeabilityService.StartTradeability();

                ZoneServiceRunning = false;
            }
            catch (Exception e)
            {
                logger.LogInformation(e.Message);
            }
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task Start(List<TradeInstrument> instruments, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await StartTransform(TransformCancel.Source.Token);

                    ZoneServiceRunning = true;
                    logger.LogInformation($"zone service starting");

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    List<Task> tasks = new List<Task>();
                    List<Candle> candles = await candleHelper.GetUnusedNonBaseCandles();
                    for (int i = 0; i < instruments.Count; i++)
                    {
                        tasks.Add(ZoneFinder(candles.Where(x => x.InstrumentToken == instruments[i].Token).ToList(), instruments[i]));
                    }
                    await Task.WhenAll(tasks);

                    candles = await candleHelper.GetAll5minCandles();
                    List<Zone> unbrokenzones = await zoneHelper.GetUnbrokenZones();
                    List<Candle> dynamicCandles;
                    List<Zone> updatedZones = new List<Zone>();
                    double testingfactor;
                    foreach (var zone in unbrokenzones)
                    {

                        if (zone.Entry)
                        {
                            testingfactor = entry.TestingFactor;
                        }
                        else
                        {
                            testingfactor = curve.TestingFactor;
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
                        await zoneHelper.Update(updatedZones);
                    }

                    stopwatch.Stop();
                    logger.LogInformation($"zone service done - time elapsed: {stopwatch.ElapsedMilliseconds}");

                    await tradeabilityService.StartTradeability();

                    await Task.Delay(60000);
                }
                catch (Exception e)
                {
                    logger.LogInformation(e.Message);
                }
            }
            ZoneServiceRunning = false;
        }

        public bool IsZoneServiceRunning()
        {
            return ZoneServiceRunning;
        }

        public async Task StartZoneService()
        {
            ZoneServiceCancel.Source = new CancellationTokenSource();
            List<TradeInstrument> instruments = await instrumentHelper.GetTradeInstruments();
            ZoneServiceCancel.HangfireId = backgroundJobClient.Enqueue(() => Start(instruments, ZoneServiceCancel.Source.Token));
            TransformCancel.Source = new CancellationTokenSource();
        }

        public async Task StartZoneServiceOnce()
        {
            ZoneServiceCancel.Source = new CancellationTokenSource();
            List<TradeInstrument> instruments = await instrumentHelper.GetTradeInstruments();
            ZoneServiceCancel.HangfireId = backgroundJobClient.Enqueue(() => StartOnce(instruments, ZoneServiceCancel.Source.Token));
            TransformCancel.Source = new CancellationTokenSource();
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
                        switch (timeframe)
                        {
                            case 5:
                                temp.UsedForEntry = true;
                                break;
                            case 10:
                                temp.UsedForEntry = true;
                                break;
                            case 15:
                                temp.UsedForEntry = true;
                                temp.UsedForCurve = true;
                                break;
                            case 30:
                                temp.UsedForEntry = true;
                                temp.UsedForCurve = true;
                                break;
                            case 45:
                                temp.UsedForCurve = true;
                                break;
                            case 60:
                                temp.UsedForCurve = true;
                                temp.UsedForEntry = true;
                                break;
                            case 120:
                                temp.UsedForCurve = true;
                                break;
                            case 240:
                                temp.UsedForCurve = true;
                                break;
                        }
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

                    Math.DivRem(current.Timestamp.Minute + 1, timeframe, out int result);
                    if (result == 0)
                    {
                        newCandles.Add(temp);
                        candleCounter = 0;
                        temp = new();
                    }

                    i++;
                }
                else
                {
                    Math.DivRem(current.Timestamp.Minute + 1, timeframe, out int result);
                    if (result == 0)
                    {
                        if(candleCounter > 0)
                        {
                            newCandles.Add(temp);
                            candleCounter = 0;
                            temp = new();
                        }
                    }

                    if (current.Timestamp.Hour == 9 && current.Timestamp.Minute == 15)
                    {
                        if(candleCounter > 0)
                        {
                            newCandles.Add(temp);
                        }
                        time.AddDays(1);
                        time = new DateTime(time.Year, time.Month, time.Day, 9, 14, 00);
                        candleCounter = 0;
                        temp = new();
                    }
                }
                time = time.AddMinutes(1);
            }

            return newCandles.OrderBy(x => x.Timestamp).ToList();
        }

        public async Task StartTransform(CancellationToken token)
        {
            //while (!token.IsCancellationRequested)
            //{
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
            //    await Task.Delay(60000);
            //}
        }

        private async Task Transform(uint token, List<Candle> candles)
        {
            if(candles.Count == 0)
            {
                goto Ending;
            }
            List<Candle> updatedcandles = new List<Candle>();
            List<Candle> candles5 = new List<Candle>();
            List<Candle> candles10 = new List<Candle>();
            List<Candle> candles15 = new List<Candle>();
            List<Candle> candles30 = new List<Candle>();
            List<Candle> candles45 = new List<Candle>();
            List<Candle> candles60 = new List<Candle>();
            List<Candle> candles120 = new List<Candle>();
            List<Candle> candles240 = new List<Candle>();
            var list5 = candles.Where(x => x.UsedBy5 == false).OrderBy(x => x.Timestamp).ToList();
            if (list5.Count > 4)
            {

                    candles5 = await Task.Run(() => TransformCandles(list5, 5));
                    if (candles5.Count > 0)
                    {
                        for (int i = 0, n = candles.Count; i < n; i++)
                        {
                            if (DateTime.Compare(candles[i].Timestamp, candles5[^1].Timestamp.AddMinutes(5)) < 0)
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
            var list10 = candles.Where(x => x.UsedBy10 == false).OrderBy(x => x.Timestamp).ToList();
            if (list10.Count > 9)
            {

                candles10 = await Task.Run(() => TransformCandles(list10, 10));
                    if (candles10.Count > 0)
                    {
                        for (int i = 0, n = candles.Count; i < n; i++)
                        {
                            if (DateTime.Compare(candles[i].Timestamp, candles10[^1].Timestamp.AddMinutes(10)) < 0)
                            {
                                candles[i].UsedBy10 = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                        candles5.AddRange(candles10);
                    }
                
            }
            var list15 = candles.Where(x => x.UsedBy15 == false).OrderBy(x => x.Timestamp).ToList();
            if (list15.Count > 14)
            {

                    candles15 = await Task.Run(() => TransformCandles(list15, 15));
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
                        candles5.AddRange(candles15);
                    }
                
            }
            var list30 = candles.Where(x => x.UsedBy30 == false).OrderBy(x => x.Timestamp).ToList();
            if (list30.Count > 29)
            {

                    candles30 = await Task.Run(() => TransformCandles(list30, 30));
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
                        candles5.AddRange(candles30);
                    }
                
            }
            var list45 = candles.Where(x => x.UsedBy45 == false).OrderBy(x => x.Timestamp).ToList();
            if (list45.Count > 44)
            {

                    candles45 = await Task.Run(() => TransformCandles(list45, 45));
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
                        candles5.AddRange(candles45);
                    }
                
            }
            var list60 = candles.Where(x => x.UsedBy60 == false).OrderBy(x => x.Timestamp).ToList();
            if (list60.Count > 59)
            {

                    candles60 = await Task.Run(() => TransformCandles(list60, 60));
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
                        candles5.AddRange(candles60);
                    }
                
            }
            var list120 = candles.Where(x => x.UsedBy120 == false).OrderBy(x => x.Timestamp).ToList();
            if (list120.Count > 119)
            {

                candles120 = await Task.Run(() => TransformCandles(list120, 120));
                if (candles120.Count > 0)
                {
                    for (int i = 0, n = candles.Count; i < n; i++)
                    {
                        if (DateTime.Compare(candles[i].Timestamp, candles120[^1].Timestamp.AddMinutes(120)) < 0)
                        {
                            candles[i].UsedBy120 = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    candles5.AddRange(candles120);
                }

            }
            var list240 = candles.Where(x => x.UsedBy240 == false).OrderBy(x => x.Timestamp).ToList();
            if (list240.Count > 239)
            {

                candles240 = await Task.Run(() => TransformCandles(list240, 240));
                if (candles240.Count > 0)
                {
                    for (int i = 0, n = candles.Count; i < n; i++)
                    {
                        if (DateTime.Compare(candles[i].Timestamp, candles240[^1].Timestamp.AddMinutes(240)) < 0)
                        {
                            candles[i].UsedBy240 = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    candles5.AddRange(candles240);
                }

            }
            if (candles.Count > 0)
            {
                for (int i = 0, n = candles.Count; i < n; i++)
                {
                    if (candles[i].UsedBy5 || candles[i].UsedBy10 || candles[i].UsedBy15 || candles[i].UsedBy30 || candles[i].UsedBy45 || candles[i].UsedBy60 || candles[i].UsedBy120 || candles[i].UsedBy240)
                    {
                        if(candles[i].UsedBy5 && candles[i].UsedBy10 && candles[i].UsedBy15 && candles[i].UsedBy30 && candles[i].UsedBy45 && candles[i].UsedBy60 && candles[i].UsedBy120 && candles[i].UsedBy240)
                        {
                            candles[i].Used = true;
                        }
                        updatedcandles.Add(candles[i]);
                    }
                }
            }
            await semaphoreSlim.WaitAsync();
            try
            {
                if(candles5.Count > 0)
                {
                    await candleHelper.Add(candles5);
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
            Ending:;
        }

        private async Task ZoneFinder(List<Candle> candles, TradeInstrument instrument)
        {
            List<Zone> zones5 = new List<Zone>();
            List<Zone> zones10 = new List<Zone>();
            List<Zone> zones15curve = new List<Zone>();
            List<Zone> zones15entry = new List<Zone>();
            List<Zone> zones30curve = new List<Zone>();
            List<Zone> zones30entry = new List<Zone>();
            List<Zone> zones45 = new List<Zone>();
            List<Zone> zones60curve = new List<Zone>();
            List<Zone> zones60entry = new List<Zone>();
            List<Zone> zones120 = new List<Zone>();
            List<Zone> zones240 = new List<Zone>();
            List<Zone> newzones = new List<Zone>();
            List<Candle> updatedcandles = new List<Candle>();
            List<Candle> candles5 = candles.Where(x => x.Timeframe == 5).OrderBy(x => x.Timestamp).ToList();
            List<Candle> candles10 = candles.Where(x => x.Timeframe == 10).OrderBy(x => x.Timestamp).ToList();
            List<Candle> candles15 = candles.Where(x => x.Timeframe == 15).OrderBy(x => x.Timestamp).ToList();
            List<Candle> candles30 = candles.Where(x => x.Timeframe == 30).OrderBy(x => x.Timestamp).ToList();
            List<Candle> candles45 = candles.Where(x => x.Timeframe == 45).OrderBy(x => x.Timestamp).ToList();
            List<Candle> candles60 = candles.Where(x => x.Timeframe == 60).OrderBy(x => x.Timestamp).ToList();
            List<Candle> candles120 = candles.Where(x => x.Timeframe == 120).OrderBy(x => x.Timestamp).ToList();
            List<Candle> candles240 = candles.Where(x => x.Timeframe == 60).OrderBy(x => x.Timestamp).ToList();
            if (candles5.Count > 0)
            {
                zones5 = await Task.Run(() => MakeZones(candles5, instrument.TradingSymbol, instrument.Token, 5, entry));
            }
            if (candles10.Count > 0)
            {
                zones10 = await Task.Run(() => MakeZones(candles10, instrument.TradingSymbol, instrument.Token, 10, entry));
            }
            if (candles15.Count > 0)
            {
                List<Candle> candlescurve = candles15.Where(x => x.UsedByCurve == false).OrderBy(x => x.Timestamp).ToList();
                List<Candle> candlesentry = candles15.Where(x => x.UsedByEntry == false).OrderBy(x => x.Timestamp).ToList();
                if(candlescurve.Count > 0)
                {
                    zones15curve = await Task.Run(() => MakeZones(candlescurve, instrument.TradingSymbol, instrument.Token, 15, curve));
                }
                if(candlesentry.Count > 0)
                {
                    zones15entry = await Task.Run(() => MakeZones(candlesentry, instrument.TradingSymbol, instrument.Token, 15, entry));
                }
            }
            if (candles30.Count > 0)
            {
                List<Candle> candlescurve = candles30.Where(x => x.UsedByCurve == false).OrderBy(x => x.Timestamp).ToList();
                List<Candle> candlesentry = candles30.Where(x => x.UsedByEntry == false).OrderBy(x => x.Timestamp).ToList();
                if (candlescurve.Count > 0)
                {
                    zones30curve = await Task.Run(() => MakeZones(candlescurve, instrument.TradingSymbol, instrument.Token, 30, curve));
                }
                if (candlesentry.Count > 0)
                {
                    zones30entry = await Task.Run(() => MakeZones(candlesentry, instrument.TradingSymbol, instrument.Token, 30, entry));
                }
            }
            if (candles45.Count > 0)
            {
                zones45 = await Task.Run(() => MakeZones(candles45, instrument.TradingSymbol, instrument.Token, 45, curve));
            }
            if (candles60.Count > 0)
            {
                List<Candle> candlescurve = candles60.Where(x => x.UsedByCurve == false).OrderBy(x => x.Timestamp).ToList();
                List<Candle> candlesentry = candles60.Where(x => x.UsedByEntry == false).OrderBy(x => x.Timestamp).ToList();
                if (candlescurve.Count > 0)
                {
                    zones60curve = await Task.Run(() => MakeZones(candlescurve, instrument.TradingSymbol, instrument.Token, 60, curve));
                }
                if (candlesentry.Count > 0)
                {
                    zones60entry = await Task.Run(() => MakeZones(candlesentry, instrument.TradingSymbol, instrument.Token, 60, entry));
                }
            }
            if (candles120.Count > 0)
            {
                zones120 = await Task.Run(() => MakeZones(candles120, instrument.TradingSymbol, instrument.Token, 120, curve));
            }
            if (candles240.Count > 0)
            {
                zones240 = await Task.Run(() => MakeZones(candles240, instrument.TradingSymbol, instrument.Token, 240, curve));
            }

            if (zones5.Count > 0)
            {
                for(int i = 0, n = candles5.Count; i < n; i++)
                {
                    if(DateTime.Compare(candles5[i].Timestamp, zones5[^1].To.AddMinutes(zones5[^1].Timeframe)) < 0)
                    {
                        candles5[i].Used = true;
                        updatedcandles.Add(candles5[i]);
                    }
                }
            }
            if (zones10.Count > 0)
            {
                for (int i = 0, n = candles10.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles10[i].Timestamp, zones10[^1].To.AddMinutes(zones10[^1].Timeframe)) < 0)
                    {
                        candles10[i].Used = true;
                        updatedcandles.Add(candles10[i]);
                    }
                }
            }
            if (zones15curve.Count > 0)
            {
                for (int i = 0, n = candles15.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles15[i].Timestamp, zones15curve[^1].To.AddMinutes(zones15curve[^1].Timeframe)) < 0)
                    {
                        candles15[i].UsedByCurve = true;
                    }
                }
                zones5.AddRange(zones15curve);
            }
            if (zones15entry.Count > 0)
            {
                for (int i = 0, n = candles15.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles15[i].Timestamp, zones15entry[^1].To.AddMinutes(zones15entry[^1].Timeframe)) < 0)
                    {
                        candles15[i].UsedByEntry = true;
                    }
                }
                zones5.AddRange(zones15entry);
            }
            if(zones15curve.Count > 0 || zones15entry.Count > 0)
            {
                for (int i = 0, n = candles15.Count; i < n; i++)
                {
                    if(candles15[i].UsedByEntry == true && candles15[i].UsedByCurve == true)
                    {
                        candles15[i].Used = true;
                    }
                    if(candles15[i].Used || candles15[i].UsedByEntry || candles15[i].UsedByCurve)
                    {
                        updatedcandles.Add(candles15[i]);
                    }
                }
            }

            if (zones30curve.Count > 0)
            {
                for (int i = 0, n = candles30.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles30[i].Timestamp, zones30curve[^1].To.AddMinutes(zones30curve[^1].Timeframe)) < 0)
                    {
                        candles30[i].UsedByCurve = true;
                    }
                }
                zones5.AddRange(zones30curve);
            }
            if (zones30entry.Count > 0)
            {
                for (int i = 0, n = candles30.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles30[i].Timestamp, zones30entry[^1].To.AddMinutes(zones30entry[^1].Timeframe)) < 0)
                    {
                        candles30[i].UsedByEntry = true;
                    }
                }
                zones5.AddRange(zones30entry);
            }
            if (zones30curve.Count > 0 || zones30entry.Count > 0)
            {
                for (int i = 0, n = candles30.Count; i < n; i++)
                {
                    if (candles30[i].UsedByEntry == true && candles30[i].UsedByCurve == true)
                    {
                        candles30[i].Used = true;
                    }
                    if (candles30[i].Used || candles30[i].UsedByEntry || candles30[i].UsedByCurve)
                    {
                        updatedcandles.Add(candles30[i]);
                    }
                }
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
                zones5.AddRange(zones45);
            }

            if (zones60curve.Count > 0)
            {
                for (int i = 0, n = candles60.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles60[i].Timestamp, zones60curve[^1].To.AddMinutes(zones60curve[^1].Timeframe)) < 0)
                    {
                        candles60[i].UsedByCurve = true;
                    }
                }
                zones5.AddRange(zones60curve);
            }
            if (zones60entry.Count > 0)
            {
                for (int i = 0, n = candles60.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles60[i].Timestamp, zones60entry[^1].To.AddMinutes(zones60entry[^1].Timeframe)) < 0)
                    {
                        candles60[i].UsedByEntry = true;
                    }
                }
                zones5.AddRange(zones60entry);
            }
            if (zones60curve.Count > 0 || zones60entry.Count > 0)
            {
                for (int i = 0, n = candles60.Count; i < n; i++)
                {
                    if (candles60[i].UsedByEntry == true && candles60[i].UsedByCurve == true)
                    {
                        candles60[i].Used = true;
                    }
                    if (candles60[i].Used || candles60[i].UsedByEntry || candles60[i].UsedByCurve)
                    {
                        updatedcandles.Add(candles60[i]);
                    }
                }
            }

            if (zones120.Count > 0)
            {
                for (int i = 0, n = candles120.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles120[i].Timestamp, zones120[^1].To.AddMinutes(zones120[^1].Timeframe)) < 0)
                    {
                        candles120[i].Used = true;
                        updatedcandles.Add(candles120[i]);
                    }
                }
                zones5.AddRange(zones120);
            }

            if (zones240.Count > 0)
            {
                for (int i = 0, n = candles240.Count; i < n; i++)
                {
                    if (DateTime.Compare(candles240[i].Timestamp, zones240[^1].To.AddMinutes(zones240[^1].Timeframe)) < 0)
                    {
                        candles240[i].Used = true;
                        updatedcandles.Add(candles240[i]);
                    }
                }
                zones5.AddRange(zones240);
            }

            await semaphoreSlim.WaitAsync();
            try
            {
                if(zones5.Count > 0)
                {
                    await zoneHelper.Add(zones5);
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

        private List<Zone> MakeZones(List<Candle> candles, string symbol, uint token, int timeframe, ZoneFactors factors)
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

            Zone zone = FindZone(candles, fittyCandle, symbol, token, timeframe, factors);
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

        private Zone FindZone(List<Candle> candles, FittyCandle fittyCandle, string symbol, uint token, int timeframe, ZoneFactors factors)
        {
            HalfZone forward = FindForward(candles, fittyCandle, factors.NoOfCandles, factors.ExplosiveFactor);

            if(forward == default)
            {
                return default;
            }

            HalfZone backward = FindBackwards(candles, fittyCandle, forward.Top, forward.Bottom, forward.BiggestBaseDiff, factors.NoOfCandles, factors.PreExplosiveFactor);

            if(backward == default)
            {
                return default;
            }

            if(backward.BiggestBaseDiff > forward.BiggestBaseDiff)
            {
                forward = FindForward(candles, fittyCandle, backward.Top, backward.Bottom, backward.BiggestBaseDiff, factors.NoOfCandles, factors.ExplosiveFactor);

                if (forward == default)
                {
                    return default;
                }
            }

            int range = forward.ExplosiveCandle.RangeFromFitty + backward.ExplosiveCandle.RangeFromFitty - 1;
            if(range > factors.NoOfCandles)
            {
                return default;
            }

            Zone zone = new Zone { From = backward.Timestamp, To = forward.Timestamp, InstrumentSymbol = symbol, InstrumentToken = token, Created = timeHelper.CurrentTime(), ExplosiveEndTime = forward.ExplosiveCandle.Candle.Timestamp, Timeframe = timeframe };

            if(factors.Name == "curve")
            {
                zone.Entry = false;
            }
            else if(factors.Name == "entry")
            {
                zone.Entry = true;
            }

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

            var zoneWidthX2 = Math.Abs(zone.Top - zone.Bottom) * (decimal)factors.ZoneWidthFactor;
            var zoneWidthX05 = Math.Abs(zone.Top - zone.Bottom) * (decimal)factors.PreBaseWidthFactor;
            if (backward.ExplosiveCandle.Candle.Open < backward.ExplosiveCandle.Candle.Close)
            {
                if (forward.ExplosiveCandle.Candle.Open < forward.ExplosiveCandle.Candle.Close)
                {
                    if (backward.ExplosiveCandle.Candle.High > (zone.Top + (Math.Abs(zone.Top - zone.Bottom) * (decimal)factors.SelfTestingFactor)))
                    {
                        return default;
                    }
                    if (forward.ExplosiveCandle.Candle.Low < zone.Bottom)
                    {
                        return default;
                    }

                    if (zoneWidthX2 <= RallyExplosiveWidthForward(candles, forward.ExplosiveCandle.Index, zone.Top, zone.Bottom)
                        && zoneWidthX05 <= RallyExplosiveWidthBackward(candles, backward.ExplosiveCandle.Index, zone.Bottom , zone.Top + (Math.Abs(zone.Top - zone.Bottom) * (decimal)factors.PreBaseOvershootFactor)))
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
                    if (backward.ExplosiveCandle.Candle.Low < (zone.Bottom - (Math.Abs(zone.Top - zone.Bottom) * (decimal)factors.SelfTestingFactor)))
                    {
                        return default;
                    }
                    if (forward.ExplosiveCandle.Candle.High > zone.Top)
                    {
                        return default;
                    }

                    if (zoneWidthX2 < DropExplosiveWidthForward(candles, forward.ExplosiveCandle.Index, zone.Bottom, zone.Top)
                        && zoneWidthX05 < DropExplosiveWidthBackward(candles, backward.ExplosiveCandle.Index, zone.Top, zone.Bottom - (Math.Abs(zone.Top - zone.Bottom) * (decimal)factors.PreBaseOvershootFactor)))
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
        Task Start(List<TradeInstrument> instruments, CancellationToken token);
        List<Candle> GetZoneCandles();
        void CancelToken();
        Task StartZoneService();
        bool IsZoneServiceRunning();
        Task StartZoneServiceOnce();
    }
}
