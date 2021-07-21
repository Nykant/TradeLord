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
    public class TradeabilityService : ITradeabilityService
    {
        private readonly ICandleDbHelper candleDbHelper;
        private readonly IZoneDbHelper zoneDbHelper;
        private readonly ITimeHelper timeHelper;
        private readonly ILogger<TradeabilityService> logger;
        private readonly IBackgroundJobClient backgroundJobClient;
        private static readonly CancellationGod tradeabilityServiceCancel = new CancellationGod();
        public TradeabilityService(ICandleDbHelper candleDbHelper, IZoneDbHelper zoneDbHelper, ITimeHelper timeHelper, ILogger<TradeabilityService> logger, IBackgroundJobClient backgroundJobClient)
        {
            this.candleDbHelper = candleDbHelper;
            this.zoneDbHelper = zoneDbHelper;
            this.timeHelper = timeHelper;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
        }

        public void Start()
        {
            //tradeabilityServiceCancel.HangfireId = backgroundJobClient.Enqueue(() => );
        }

        public async Task StartTradeability()
        {
            tradeabilityServiceCancel.Source = new CancellationTokenSource();
            await TradeabilityCheck(tradeabilityServiceCancel.Source.Token);
        }

        public void Stop()
        {
            tradeabilityServiceCancel.Source.Cancel();
            //backgroundJobClient.Delete(tradeabilityServiceCancel.HangfireId);
        }

        private async Task TradeabilityCheck(CancellationToken token)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<Zone> allzones = await zoneDbHelper.GetZones();
            List<Candle> candles = await candleDbHelper.GetNonBaseCandles();
            List<Zone> zones = allzones.Where(x => x.Tradeable == false && x.Entry).ToList();
            List<Zone> updatedZones = new List<Zone>();

            foreach (var zone in zones)
            {
                Zone updated = zone;

                // 15htf
                if (zone.Timeframe == 5)
                {
                    List<Zone> zones15 = allzones.Where(x => x.InstrumentToken == zone.InstrumentToken && x.Timeframe == 15 && !x.Entry).OrderBy(x => x.To).ToList();
                    if (zones15.Count > 0)
                    {
                        Zone motherZone = await Task.Run(() => zones15.Find(x => DateTime.Compare(x.From, zone.From) <= 0 && DateTime.Compare(x.To, zone.To) >= 0));
                        if (motherZone != default)
                        {
                            Zone opposite = await Task.Run(() => FindOpposite(motherZone, zones15.Where(x => DateTime.Compare(motherZone.From, x.From) > 0).OrderBy(x => x.To).ToList()));
                            Zone next = await Task.Run(() => FindNext(motherZone, zones15.Where(x => DateTime.Compare(motherZone.To.AddMinutes(motherZone.Timeframe), x.From) < 0).OrderBy(x => x.To).ToList()));
                            if (opposite != default)
                            {
                                List<Candle> candles15 = candles.Where(x => x.InstrumentToken == opposite.InstrumentToken && DateTime.Compare(x.Timestamp, opposite.ExplosiveEndTime.AddMinutes(opposite.Timeframe)) > 0 && x.Timeframe == opposite.Timeframe).OrderBy(x => x.Timestamp).ToList();

                                updated.Tradeable = IsTradeable(candles15, motherZone, next, opposite);
                            }
                        }
                    }
                    if (updated.Tradeable)
                    {
                        goto Ending;
                    }
                }


                // 30htf
                if (zone.Timeframe == 10)
                {
                    List<Zone> zones30 = allzones.Where(x => x.InstrumentToken == zone.InstrumentToken && x.Timeframe == 30 && !x.Entry).OrderBy(x => x.To).ToList();
                    if (zones30.Count > 0)
                    {
                        Zone motherZone = await Task.Run(() => zones30.Find(x => DateTime.Compare(x.From, zone.From) <= 0 && DateTime.Compare(x.To, zone.To) >= 0));
                        if (motherZone != default)
                        {
                            Zone opposite = await Task.Run(() => FindOpposite(motherZone, zones30.Where(x => DateTime.Compare(motherZone.From, x.From) > 0).OrderBy(x => x.To).ToList()));
                            Zone next = await Task.Run(() => FindNext(motherZone, zones30.Where(x => DateTime.Compare(motherZone.To.AddMinutes(motherZone.Timeframe), x.From) < 0).OrderBy(x => x.To).ToList()));
                            if (opposite != default)
                            {
                                List<Candle> candles30 = candles.Where(x => x.InstrumentToken == opposite.InstrumentToken && DateTime.Compare(x.Timestamp, opposite.ExplosiveEndTime.AddMinutes(opposite.Timeframe)) > 0 && x.Timeframe == opposite.Timeframe).OrderBy(x => x.Timestamp).ToList();

                                updated.Tradeable = IsTradeable(candles30, motherZone, next, opposite);
                            }
                        }
                    }
                    if (updated.Tradeable)
                    {
                        goto Ending;
                    }
                }


                // 45htf
                if (zone.Timeframe == 15 || zone.Timeframe == 5)
                {
                    List<Zone> zones45 = allzones.Where(x => x.InstrumentToken == zone.InstrumentToken && x.Timeframe == 45 && !x.Entry).OrderBy(x => x.To).ToList();
                    if (zones45.Count > 0)
                    {
                        Zone motherZone = await Task.Run(() => zones45.Find(x => DateTime.Compare(x.From, zone.From) <= 0 && DateTime.Compare(x.To, zone.To) >= 0));
                        if (motherZone != default)
                        {
                            Zone opposite = await Task.Run(() => FindOpposite(motherZone, zones45.Where(x => DateTime.Compare(motherZone.From, x.From) > 0).OrderBy(x => x.To).ToList()));
                            Zone next = await Task.Run(() => FindNext(motherZone, zones45.Where(x => DateTime.Compare(motherZone.To.AddMinutes(motherZone.Timeframe), x.From) < 0).OrderBy(x => x.To).ToList()));
                            if (opposite != default)
                            {
                                List<Candle> candles45 = candles.Where(x => x.InstrumentToken == opposite.InstrumentToken && DateTime.Compare(x.Timestamp, opposite.ExplosiveEndTime.AddMinutes(opposite.Timeframe)) > 0 && x.Timeframe == opposite.Timeframe).OrderBy(x => x.Timestamp).ToList();

                                updated.Tradeable = IsTradeable(candles45, motherZone, next, opposite);
                            }
                        }
                    }
                    if (updated.Tradeable)
                    {
                        goto Ending;
                    }
                }


                // 60htf
                if (zone.Timeframe == 5 || zone.Timeframe == 10 || zone.Timeframe == 15)
                {
                    List<Zone> zones60 = allzones.Where(x => x.InstrumentToken == zone.InstrumentToken && x.Timeframe == 60 && !x.Entry).OrderBy(x => x.To).ToList();
                    if (zones60.Count > 0)
                    {
                        Zone motherZone = await Task.Run(() => zones60.Find(x => DateTime.Compare(x.From, zone.From) <= 0 && DateTime.Compare(x.To, zone.To) >= 0));
                        if (motherZone != default)
                        {
                            Zone opposite = await Task.Run(() => FindOpposite(motherZone, zones60.Where(x => DateTime.Compare(motherZone.From, x.From) > 0).OrderBy(x => x.To).ToList()));
                            Zone next = await Task.Run(() => FindNext(motherZone, zones60.Where(x => DateTime.Compare(motherZone.To.AddMinutes(motherZone.Timeframe), x.From) < 0).OrderBy(x => x.To).ToList()));
                            if (opposite != default)
                            {
                                List<Candle> candles60 = candles.Where(x => x.InstrumentToken == opposite.InstrumentToken && DateTime.Compare(x.Timestamp, opposite.ExplosiveEndTime.AddMinutes(opposite.Timeframe)) > 0 && x.Timeframe == opposite.Timeframe).OrderBy(x => x.Timestamp).ToList();

                                updated.Tradeable = IsTradeable(candles60, motherZone, next, opposite);
                            }
                        }
                    }
                }

                if (zone.Timeframe == 30)
                {
                    List<Zone> zones120 = allzones.Where(x => x.InstrumentToken == zone.InstrumentToken && x.Timeframe == 120 && !x.Entry).OrderBy(x => x.To).ToList();
                    if (zones120.Count > 0)
                    {
                        Zone motherZone = await Task.Run(() => zones120.Find(x => DateTime.Compare(x.From, zone.From) <= 0 && DateTime.Compare(x.To, zone.To) >= 0));
                        if (motherZone != default)
                        {
                            Zone opposite = await Task.Run(() => FindOpposite(motherZone, zones120.Where(x => DateTime.Compare(motherZone.From, x.From) > 0).OrderBy(x => x.To).ToList()));
                            Zone next = await Task.Run(() => FindNext(motherZone, zones120.Where(x => DateTime.Compare(motherZone.To.AddMinutes(motherZone.Timeframe), x.From) < 0).OrderBy(x => x.To).ToList()));
                            if (opposite != default)
                            {
                                List<Candle> candles120 = candles.Where(x => x.InstrumentToken == opposite.InstrumentToken && DateTime.Compare(x.Timestamp, opposite.ExplosiveEndTime.AddMinutes(opposite.Timeframe)) > 0 && x.Timeframe == opposite.Timeframe).OrderBy(x => x.Timestamp).ToList();

                                updated.Tradeable = IsTradeable(candles120, motherZone, next, opposite);
                            }
                        }
                    }
                }

                if (zone.Timeframe == 60)
                {
                    List<Zone> zones240 = allzones.Where(x => x.InstrumentToken == zone.InstrumentToken && x.Timeframe == 240 && !x.Entry).OrderBy(x => x.To).ToList();
                    if (zones240.Count > 0)
                    {
                        Zone motherZone = await Task.Run(() => zones240.Find(x => DateTime.Compare(x.From, zone.From) <= 0 && DateTime.Compare(x.To, zone.To) >= 0));
                        if (motherZone != default)
                        {
                            Zone opposite = await Task.Run(() => FindOpposite(motherZone, zones240.Where(x => DateTime.Compare(motherZone.From, x.From) > 0).OrderBy(x => x.To).ToList()));
                            Zone next = await Task.Run(() => FindNext(motherZone, zones240.Where(x => DateTime.Compare(motherZone.To.AddMinutes(motherZone.Timeframe), x.From) < 0).OrderBy(x => x.To).ToList()));
                            if (opposite != default)
                            {
                                List<Candle> candles240 = candles.Where(x => x.InstrumentToken == opposite.InstrumentToken && DateTime.Compare(x.Timestamp, opposite.ExplosiveEndTime.AddMinutes(opposite.Timeframe)) > 0 && x.Timeframe == opposite.Timeframe).OrderBy(x => x.Timestamp).ToList();

                                updated.Tradeable = IsTradeable(candles240, motherZone, next, opposite);
                            }
                        }
                    }
                }

                Ending:;

                if (updated.Tradeable)
                {
                    updatedZones.Add(updated);
                }
            }

            if (updatedZones.Count > 0)
            {
                await zoneDbHelper.Update(updatedZones);
            }

            stopwatch.Stop();
            logger.LogInformation($"Tradeability check done - elapsed miliseconds: {stopwatch.ElapsedMilliseconds}");
        }

        private Zone FindOpposite(Zone motherZone, List<Zone> prevHtfZones)
        {
            for (int i = prevHtfZones.Count - 1; i >= 0; i--)
            {
                // if motherzone is opposite to prev zone
                if (prevHtfZones[i].SupplyDemand != motherZone.SupplyDemand)
                {
                    return prevHtfZones[i];
                }
            }
            return default;
        }

        private Zone FindNext(Zone motherZone, List<Zone> nextHtfZones)
        {
            for (int i = 0, n = nextHtfZones.Count; i < n; i++)
            {
                // if motherzone is opposite to prev zone
                if (nextHtfZones[i].SupplyDemand == motherZone.SupplyDemand)
                {
                    return nextHtfZones[i];
                }
            }
            return default;
        }

        private bool IsTradeable(List<Candle> candles, Zone motherZone, Zone next, Zone opposite)
        {
            if (motherZone.SupplyDemand == SupplyDemand.Demand)
            {
                for (int i = 0, n = candles.Count; i < n; i++)
                {
                    if (candles[i].High > opposite.Top)
                    {
                        if (DateTime.Compare(candles[i].Timestamp, motherZone.To.AddMinutes(motherZone.Timeframe)) > 0)
                        {
                            if (next != default)
                            {
                                if (DateTime.Compare(candles[i].Timestamp, next.From) < 0)
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0, n = candles.Count; i < n; i++)
                {
                    if (candles[i].Low < opposite.Bottom)
                    {
                        if (DateTime.Compare(candles[i].Timestamp, motherZone.To.AddMinutes(motherZone.Timeframe)) > 0)
                        {
                            if (next != default)
                            {
                                if (DateTime.Compare(candles[i].Timestamp, next.From) < 0)
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
    public interface ITradeabilityService
    {
        void Start();
        void Stop();
        Task StartTradeability();
    }
}
