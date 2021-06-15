using Hangfire;
using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Server.Extensions;
using TradeMaster6000.Server.Helpers;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Services
{
    public class TickerService : ITickerService
    {
        private readonly ILogger<TickerService> logger;
        private IConfiguration Configuration { get; }
        private readonly IKiteService kiteService;
        private readonly IInstrumentHelper instrumentHelper;
        private readonly ITimeHelper timeHelper;
        private readonly ICandleDbHelper candleHelper;
        private readonly ITickDbHelper tickDbHelper;
        private readonly IOrderUpdatesDbHelper updatesHelper;
        private readonly IBackgroundJobClient backgroundJob;
        private readonly IContextExtension contextExtension;

        private static Ticker Ticker { get; set; }
        private ConcurrentQueue<SomeLog> TickerLogs { get; set; }
        private static bool CandlesRunning { get; set; }
        //private readonly object key = new object();
        public TickerService(IConfiguration configuration, IKiteService kiteService, IInstrumentHelper instrumentHelper, ITimeHelper timeHelper, ICandleDbHelper candleHelper, ITickDbHelper tickDbHelper, IOrderUpdatesDbHelper orderUpdatesDbHelper, IBackgroundJobClient backgroundJob, IContextExtension contextExtension, ILogger<TickerService> logger)
        {
            this.kiteService = kiteService;
            Configuration = configuration;
            this.instrumentHelper = instrumentHelper;
            this.timeHelper = timeHelper;
            this.candleHelper = candleHelper;
            this.tickDbHelper = tickDbHelper;
            this.updatesHelper = orderUpdatesDbHelper;
            this.backgroundJob = backgroundJob;
            this.contextExtension = contextExtension;
            this.logger = logger;
            TickerLogs = new ();
        }

        public void Start()
        {
            if (Ticker == null)
            {
                var accessToken = kiteService.GetAccessToken();
                Ticker = new Ticker(Configuration.GetValue<string>("APIKey"), accessToken);

                Ticker.OnTick += OnTick;
                Ticker.OnOrderUpdate += OnOrderUpdate;
                Ticker.OnNoReconnect += OnNoReconnect;
                Ticker.OnError += OnError;
                Ticker.OnReconnect += OnReconnect;
                Ticker.OnClose += OnClose;
                Ticker.OnConnect += OnConnect;

                Ticker.EnableReconnect(Interval: 5, Retries: 50);
                Ticker.Connect();
            }
        }

        public async Task StartFlushing(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                await tickDbHelper.Flush();
                await Task.Delay(10000);
            }
        }

        public async Task RunCandles(CancellationToken token)
        {
            CandlesRunning = true;
            await candleHelper.Flush();

            while (!timeHelper.IsPreMarketOpen() && !token.IsCancellationRequested)
            {
                await Task.Delay(10000);
            }

            while (!timeHelper.IsMarketOpen() && !token.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }

            var instruments = await instrumentHelper.GetTradeInstruments();
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 30; i++)
            {
                Subscribe(instruments[i].Token);
                tasks.Add(Task.Run(async ()=> await Analyze(instruments[i], token)));
            }

            await Task.WhenAll(tasks);

            Stop();
            CandlesRunning = false;
        }

        public async Task Analyze(TradeInstrument instrument, CancellationToken token)
        {
            DateTime waittime = timeHelper.OpeningTime();
            DateTime current = timeHelper.CurrentTime();
            if (DateTime.Compare(waittime, current) < 0)
            {
                int hour = current.Hour;
                int minute = current.Minute;
                if(minute == 59)
                {
                    hour++;
                    minute = 0;
                }
                waittime = new DateTime(current.Year, current.Month, current.Day, hour, minute, 00);
            }

            List<MyTick> ticks;
            while (!token.IsCancellationRequested)
            {
                ticks = await tickDbHelper.Get(instrument.Token, waittime);
                if(ticks.Count > 0)
                {
                    break;
                }
                await Task.Delay(500);
            }

            Candle previousCandle = new Candle();
            DateTime candleTime = new DateTime();
            TimeSpan oneMin = new TimeSpan(1200);
            TimeSpan duration = new TimeSpan();
            while (!timeHelper.IsMarketEnded() && !token.IsCancellationRequested)
            {
                duration = timeHelper.GetDuration(waittime, timeHelper.CurrentTime());
                candleTime = waittime.Subtract(oneMin);

                await Task.Delay(duration);

                ticks = await tickDbHelper.Get(instrument.Token, candleTime);
                Candle candle = new Candle() { InstrumentToken = instrument.Token, From = waittime, Kill = waittime.AddDays(2) };
                if(ticks.Count > 0)
                {
                    await Task.Run(() =>
                    {
                        candle.High = ticks[0].LTP;
                        candle.Low = ticks[0].LTP;
                        for (int i = 0; i < ticks.Count; i++)
                        {
                            if (candle.High < ticks[i].LTP)
                            {
                                candle.High = ticks[i].LTP;
                            }
                            if (candle.Low > ticks[i].LTP)
                            {
                                candle.Low = ticks[i].LTP;
                            }
                        }
                        candle.Open = ticks[0].LTP;
                        candle.Close = ticks[^1].LTP;
                    });
                }
                else
                {
                    candle.High = previousCandle.Close;
                    candle.Low = previousCandle.Close;
                    candle.Open = previousCandle.Close;
                    candle.Close = previousCandle.Close;
                }

                previousCandle = await candleHelper.AddCandle(candle).ConfigureAwait(false);
                waittime = waittime.AddMinutes(1);
            }

            UnSubscribe(instrument.Token);
        }

        public async Task<OrderUpdate> GetOrder(string id)
        {
            var update = await updatesHelper.Get(id);
            if (update != null)
            {
                return update;
            }

            try
            {
                var order = kiteService.GetKite().GetOrderHistory(id)[^1];
                var newOrderUpdate = new OrderUpdate
                {
                    AveragePrice = order.AveragePrice,
                    FilledQuantity = order.FilledQuantity,
                    InstrumentToken = order.InstrumentToken,
                    OrderId = order.OrderId,
                    Price = order.Price,
                    Quantity = order.Quantity,
                    Status = order.Status,
                    Timestamp = DateTime.Now,
                    TriggerPrice = order.TriggerPrice
                };
                //lock (key)
                //{
                    await updatesHelper.AddOrUpdate(newOrderUpdate);
                //}

                return newOrderUpdate;
            }
            catch (KiteException e)
            {
                AddLog(e.Message, LogType.Exception);
                return default;
            };
        }

        public bool IsCandlesRunning()
        {
            return CandlesRunning;
        }

        public void SetTicker(Ticker ticker)
        {
            Ticker = ticker;
        }

        public List<SomeLog> GetSomeLogs()
        {
            return TickerLogs.ToList();
        }

        public void Stop()
        {
            Ticker.DisableReconnect();
            Ticker.Close();
            Ticker = null;
        }

        public void Subscribe(uint token)
        {
            Ticker.Subscribe(Tokens: new UInt32[] { token });
            Ticker.SetMode(Tokens: new UInt32[] { token }, Mode: Constants.MODE_FULL);
        }

        public void UnSubscribe(uint token)
        {
            Ticker.UnSubscribe(Tokens: new UInt32[] { token });
        }

        private void AddLog(string log, LogType logType)
        {
            TickerLogs.Enqueue(new()
            {
                Log = log,
                Timestamp = DateTime.Now,
                LogType = logType
            });
        }

        // events
        private async void OnTick(Tick tickData)
        {
            DateTime time;
            if(tickData.Timestamp == null)
            {
                time = timeHelper.CurrentTime();
            }
            else
            {
                time = tickData.Timestamp.Value;
            }
            await tickDbHelper.Add(new MyTick
            {
                InstrumentToken = tickData.InstrumentToken,
                LTP = tickData.LastPrice,
                StartTime = time,
                EndTime = time.AddMinutes(2)
            });
        }

        private async void OnOrderUpdate(Order orderData)
        {
            //lock (key)
            //{
                await updatesHelper.AddOrUpdate(new OrderUpdate
                {
                    AveragePrice = orderData.AveragePrice,
                    FilledQuantity = orderData.FilledQuantity,
                    InstrumentToken = orderData.InstrumentToken,
                    OrderId = orderData.OrderId,
                    Price = orderData.Price,
                    Quantity = orderData.Quantity,
                    Status = orderData.Status,
                    Timestamp = DateTime.Now,
                    TriggerPrice = orderData.TriggerPrice
                });
            //}

        }

        private void OnError(string message)
        {
            AddLog(message, LogType.Error);
        }

        private void OnClose()
        {
            AddLog("ticker connection closed...", LogType.Close);
        }

        private void OnReconnect()
        {
            AddLog("ticker connection reconnected...", LogType.Reconnect);
        }

        private void OnNoReconnect()
        {
            AddLog("ticker connection failed to reconnect...", LogType.NoReconnect);
        }

        private void OnConnect()
        {
            AddLog("ticker connected...", LogType.Connect);
        }
    }
    public interface ITickerService
    {
        Task<OrderUpdate> GetOrder(string id);
        void Subscribe(uint token);
        void UnSubscribe(uint token);
        void Start();
        void Stop();
        List<SomeLog> GetSomeLogs();
        void SetTicker(Ticker ticker);
        Task StartFlushing(CancellationToken token);
        Task RunCandles(CancellationToken token);
        bool IsCandlesRunning();
        Task Analyze(TradeInstrument instrument, CancellationToken token);
    }
}
