using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly object startkey = new();

        private IConfiguration Configuration { get; }
        private readonly IKiteService kiteService;
        private readonly IInstrumentHelper instrumentHelper;
        private readonly ITimeHelper timeHelper;
        private readonly ICandleDbHelper candleHelper;
        private readonly ITickDbHelper tickDbHelper;
        private readonly IOrderUpdatesDbHelper updatesHelper;

        private Ticker Ticker { get; set; }
        private ConcurrentQueue<SomeLog> TickerLogs { get; set; }
        private bool CandlesRunning { get; set; }
        public TickerService(IConfiguration configuration, IKiteService kiteService, IInstrumentHelper instrumentHelper, ITimeHelper timeHelper, ICandleDbHelper candleHelper, ITickDbHelper tickDbHelper, IOrderUpdatesDbHelper orderUpdatesDbHelper)
        {
            this.kiteService = kiteService;
            Configuration = configuration;
            this.instrumentHelper = instrumentHelper;
            this.timeHelper = timeHelper;
            this.candleHelper = candleHelper;
            this.tickDbHelper = tickDbHelper;
            this.updatesHelper = orderUpdatesDbHelper;
            TickerLogs = new ();
        }

        public async Task Start()
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

                await Task.Factory.StartNew(async()=> await StartFlushing(), TaskCreationOptions.LongRunning).ConfigureAwait(false);
            }
        }

        public async Task StartFlushing()
        {
            while(Ticker != null)
            {
                await tickDbHelper.Flush();
                await Task.Delay(10000);
            }
        }

        public async Task StartCandles()
        {
            if (!CandlesRunning)
            {
                await Task.Factory.StartNew(async()=> await RunCandles(), TaskCreationOptions.LongRunning).ConfigureAwait(false);
            }
        }

        public async Task RunCandles()
        {
            CandlesRunning = true;
            await candleHelper.Flush();

            while (!timeHelper.IsPreMarketOpen())
            {
                Thread.Sleep(10000);
            }

            while (!timeHelper.IsMarketOpen())
            {
                Thread.Sleep(1000);
            }

            while (!await tickDbHelper.Any())
            {
                Thread.Sleep(1000);
            }

            await Task.Run(async()=> await AnalyzeCandles());
        }

        public async Task AnalyzeCandles()
        {
            int i = 0;
            foreach (var instrument in await instrumentHelper.GetTradeInstruments())
            {
                try
                {
                    Subscribe(instrument.Token);
                    await Task.Factory.StartNew(async() => await Analyze(instrument), TaskCreationOptions.LongRunning).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    AddLog($"{e.Message}...", LogType.Exception);
                }

                if (i > 10)
                {
                    break;
                }
                i++;
            }
        }

        public async Task Analyze(TradeInstrument instrument)
        {
            AddLog($"analysing: {instrument.Token}...", LogType.Notification);
            while (timeHelper.IsMarketEnded())
            {
                var candle = new Candle() { InstrumentToken = instrument.Token, From = DateTime.Now, Kill = DateTime.Now.AddDays(1) };
                await Task.Delay(59500);
                var ticks = await tickDbHelper.Get(instrument.Token);
                candle.To = DateTime.Now;
                await Task.Run(() =>
                {
                    foreach (var tick in ticks)
                    {
                        if (candle.High < tick.LTP)
                        {
                            candle.High = tick.LTP;
                        }
                        if (candle.Low > tick.LTP)
                        {
                            candle.Low = tick.LTP;
                        }
                    }
                    candle.Open = ticks[0].LTP;
                    candle.Close = ticks[^1].LTP;
                });

                await candleHelper.AddCandle(candle).ConfigureAwait(false);
            }
            CandlesRunning = false;
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
                await updatesHelper.AddOrUpdate(newOrderUpdate);
                return newOrderUpdate;
            }
            catch (KiteException e)
            {
                AddLog(e.Message, LogType.Exception);
                return default;
            };
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
            Ticker.SetMode(Tokens: new UInt32[] { token }, Mode: Constants.MODE_LTP);
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
            await tickDbHelper.Add(new MyTick
            {
                InstrumentToken = tickData.InstrumentToken,
                LTP = tickData.LastPrice,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddMinutes(1)
            });
        }

        private async void OnOrderUpdate(Order orderData)
        {
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
        Task Start();
        Task StartCandles();
        void Stop();
        List<SomeLog> GetSomeLogs();
        void SetTicker(Ticker ticker);
    }
}
