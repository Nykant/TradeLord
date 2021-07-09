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

        public async Task Start()
        {
            tradeabilityServiceCancel.Source = new CancellationTokenSource();
            await TradeabilityCheck(tradeabilityServiceCancel.Source.Token);
        }

        public void Stop()
        {
            tradeabilityServiceCancel.Source.Cancel();
            backgroundJobClient.Delete(tradeabilityServiceCancel.HangfireId);
        }

        private async Task TradeabilityCheck(CancellationToken token)
        {
            //while (!token.IsCancellationRequested)
            //{
            List<Zone> zones = await zoneDbHelper.GetUntestedUntradeableZones(5);
            List<Zone> updatedZones = new List<Zone>();

            foreach (var zone in zones)
            {
                Zone updated = zone;

                // 15htf
                List<Zone> zones15 = await zoneDbHelper.GetZones(15, zone.InstrumentToken);
                if (zones15.Count > 0)
                {
                    Zone motherZone = await Task.Run(() => zones15.Find(x => DateTime.Compare(x.From, zone.From) <= 0 && DateTime.Compare(x.To, zone.To) >= 0));
                    if (motherZone != default)
                    {
                        Zone opposite = await Task.Run(() => FindOpposite(motherZone, zones15.Where(x => DateTime.Compare(motherZone.From, x.From) > 0).OrderBy(x => x.To).ToList()));
                        Zone next = await Task.Run(() => FindNext(motherZone, zones15.Where(x => DateTime.Compare(motherZone.To.AddMinutes(motherZone.Timeframe), x.From) < 0).OrderBy(x => x.To).ToList()));
                        if (opposite != default)
                        {
                            List<Candle> candles15 = await candleDbHelper.GetCandlesAfter(opposite.InstrumentToken, opposite.ExplosiveEndTime.AddMinutes(opposite.Timeframe), opposite.Timeframe);

                            updated.Tradeable = IsTradeable(candles15, motherZone, next, opposite);
                        }
                    }
                }
                if (updated.Tradeable)
                {
                    goto Ending;
                }

                // 30htf
                List<Zone> zones30 = await zoneDbHelper.GetZones(30, zone.InstrumentToken);
                if (zones30.Count > 0)
                {
                    Zone motherZone = await Task.Run(() => zones30.Find(x => DateTime.Compare(x.From, zone.From) <= 0 && DateTime.Compare(x.To, zone.To) >= 0));
                    if (motherZone != default)
                    {
                        Zone opposite = await Task.Run(() => FindOpposite(motherZone, zones30.Where(x => DateTime.Compare(motherZone.From, x.From) > 0).OrderBy(x => x.To).ToList()));
                        Zone next = await Task.Run(() => FindNext(motherZone, zones30.Where(x => DateTime.Compare(motherZone.To.AddMinutes(motherZone.Timeframe), x.From) < 0).OrderBy(x => x.To).ToList()));
                        if (opposite != default)
                        {
                            List<Candle> candles30 = await candleDbHelper.GetCandlesAfter(opposite.InstrumentToken, opposite.ExplosiveEndTime.AddMinutes(opposite.Timeframe), opposite.Timeframe);

                            updated.Tradeable = IsTradeable(candles30, motherZone, next, opposite);
                        }
                    }
                }
                if (updated.Tradeable)
                {
                    goto Ending;
                }

                // 45htf
                List<Zone> zones45 = await zoneDbHelper.GetZones(45, zone.InstrumentToken);
                if (zones45.Count > 0)
                {
                    Zone motherZone = await Task.Run(() => zones45.Find(x => DateTime.Compare(x.From, zone.From) <= 0 && DateTime.Compare(x.To, zone.To) >= 0));
                    if (motherZone != default)
                    {
                        Zone opposite = await Task.Run(() => FindOpposite(motherZone, zones45.Where(x => DateTime.Compare(motherZone.From, x.From) > 0).OrderBy(x => x.To).ToList()));
                        Zone next = await Task.Run(() => FindNext(motherZone, zones45.Where(x => DateTime.Compare(motherZone.To.AddMinutes(motherZone.Timeframe), x.From) < 0).OrderBy(x => x.To).ToList()));
                        if (opposite != default)
                        {
                            List<Candle> candles45 = await candleDbHelper.GetCandlesAfter(opposite.InstrumentToken, opposite.ExplosiveEndTime.AddMinutes(opposite.Timeframe), opposite.Timeframe);

                            updated.Tradeable = IsTradeable(candles45, motherZone, next, opposite);
                        }
                    }
                }
                if (updated.Tradeable)
                {
                    goto Ending;
                }

                // 60htf
                List<Zone> zones60 = await zoneDbHelper.GetZones(60, zone.InstrumentToken);
                if (zones45.Count > 0)
                {
                    Zone motherZone = await Task.Run(() => zones60.Find(x => DateTime.Compare(x.From, zone.From) <= 0 && DateTime.Compare(x.To, zone.To) >= 0));
                    if (motherZone != default)
                    {
                        Zone opposite = await Task.Run(() => FindOpposite(motherZone, zones60.Where(x => DateTime.Compare(motherZone.From, x.From) > 0).OrderBy(x => x.To).ToList()));
                        Zone next = await Task.Run(() => FindNext(motherZone, zones60.Where(x => DateTime.Compare(motherZone.To.AddMinutes(motherZone.Timeframe), x.From) < 0).OrderBy(x => x.To).ToList()));
                        if (opposite != default)
                        {
                            List<Candle> candles60 = await candleDbHelper.GetCandlesAfter(opposite.InstrumentToken, opposite.ExplosiveEndTime.AddMinutes(opposite.Timeframe), opposite.Timeframe);

                            updated.Tradeable = IsTradeable(candles60, motherZone, next, opposite);
                        }
                    }
                }
                if (updated.Tradeable)
                {
                    goto Ending;
                }

                Ending:;

                if (updated.Tradeable)
                {
                    updatedZones.Add(updated);
                }
            }

            await zoneDbHelper.Add(updatedZones).ConfigureAwait(false);

            //await Task.Delay(200000);
            //}
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
        Task Start();
        void Stop();
    }
}
