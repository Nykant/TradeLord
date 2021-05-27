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
        private readonly object orderkey = new ();
        private readonly object firstorderkey = new();
        private readonly object tickkey = new();
        private readonly object tickerlogkey = new();
        private readonly object startkey = new();

        private IConfiguration Configuration { get; set; }
        private readonly IKiteService kiteService;
        private readonly IInstrumentHelper instrumentHelper;
        private readonly ITimeHelper timeHelper;
        private readonly ICandleDbHelper candleHelper;
        private Ticker Ticker { get; set; }
        private ConcurrentDictionary<string, Order> OrderUpdates { get; set; }
        private ConcurrentDictionary<uint, Tick> Ticks { get; set; }
        private ConcurrentQueue<TickerLog> TickerLogs { get; set; }
        public TickerService(IConfiguration configuration, IKiteService kiteService, IInstrumentHelper instrumentHelper, ITimeHelper timeHelper, ICandleDbHelper candleHelper)
        {
            this.kiteService = kiteService;
            Configuration = configuration;
            this.instrumentHelper = instrumentHelper;
            this.timeHelper = timeHelper;
            this.candleHelper = candleHelper;
            TickerLogs = new ();
            Ticks = new ();
            OrderUpdates = new ();
        }

        public void Start()
        {
            lock (startkey)
            {
                if(Ticker == null)
                {
                    var accessToken = kiteService.GetAccessToken();
                    // new ticker instance 
                    Ticker = new Ticker(Configuration.GetValue<string>("APIKey"), accessToken);

                    // ticker event handlers
                    Ticker.OnTick += OnTick;
                    Ticker.OnOrderUpdate += OnOrderUpdate;
                    Ticker.OnNoReconnect += OnNoReconnect;
                    Ticker.OnError += OnError;
                    Ticker.OnReconnect += OnReconnect;
                    Ticker.OnClose += OnClose;
                    Ticker.OnConnect += OnConnect;

                    // set ticker settings
                    Ticker.EnableReconnect(Interval: 5, Retries: 50);
                    Ticker.Connect();
                }
            }
        }

        public void StartWithCandles()
        {
            lock (startkey)
            {
                if (!Ticker.IsConnected)
                {
                    var accessToken = kiteService.GetAccessToken();
                    // new ticker instance 
                    Ticker = new Ticker(Configuration.GetValue<string>("APIKey"), accessToken);

                    // ticker event handlers
                    Ticker.OnTick += OnTick;
                    Ticker.OnOrderUpdate += OnOrderUpdate;
                    Ticker.OnNoReconnect += OnNoReconnect;
                    Ticker.OnError += OnError;
                    Ticker.OnReconnect += OnReconnect;
                    Ticker.OnClose += OnClose;
                    Ticker.OnConnect += OnConnect;

                    // set ticker settings
                    Ticker.EnableReconnect(Interval: 5, Retries: 50);
                    Ticker.Connect();

                    //if (TokenSource == null)
                    //{
                    //    TokenSource = new CancellationTokenSource();
                    //    Task.Run(() => RefreshTickerToken(TokenSource.Token)).ConfigureAwait(false);
                    //    InitializeCandles(TokenSource.Token).ConfigureAwait(false);
                    //}
                }
            }
        }

        public async Task InitializeCandles(CancellationToken token)
        {
            while (!await timeHelper.IsMarketOpen())
            {
                if (token.IsCancellationRequested)
                {
                    goto Ending;
                }
                await Task.Delay(5000, token);
            }

            await Task.Run(() => AnalyzeCandles(token), token).ConfigureAwait(false);

            Ending:;
        }

        public async Task AnalyzeCandles(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var instrument in await instrumentHelper.GetTradeInstruments())
                {
                    await Task.Run(() =>
                    {
                        Analyze(instrument, token).ConfigureAwait(false);
                    }, token).ConfigureAwait(false);
                }
            }
        }

        public async Task Analyze(TradeInstrument instrument, CancellationToken token)
        {
            Candle candle;
            Stopwatch stopwatch;
            decimal ltp;
            while (!token.IsCancellationRequested)
            {
                candle = new Candle() { TradeInstrument = instrument, From = DateTime.Now };

                stopwatch = new();
                stopwatch.Start();

                candle.Open = LastTick(instrument.Token).LastPrice;
                candle.High = candle.Open;
                candle.Low = candle.Open;
                while (stopwatch.Elapsed.TotalMinutes < 1)
                {
                    ltp = LastTick(instrument.Token).LastPrice;
                    if (candle.High < ltp)
                    {
                        candle.High = ltp;
                    }
                    if (candle.Low > ltp)
                    {
                        candle.Low = ltp;
                    }
                    await Task.Delay(500, token);
                }
                candle.Close = LastTick(instrument.Token).LastPrice;
                candle.To = DateTime.Now;
                stopwatch.Stop();

                await candleHelper.AddCandle(candle).ConfigureAwait(false);
            }
        }

        public void SetTicker(Ticker ticker)
        {
            Ticker = ticker;
        }

        public Tick LastTick(uint token)
        {
            foreach(var tick in Ticks.Reverse())
            {
                if (tick.Key == token)
                {
                    return tick.Value;
                }
            }

            return new Tick();
        }
        public Order GetOrder(string id)
        {
            foreach(var update in OrderUpdates.Reverse())
            {
                if (update.Key == id)
                {
                    return update.Value;
                }
            }

            try
            {
                var order = kiteService.GetKite().GetOrderHistory(id)[^1];
                OrderUpdates.TryAdd(order.OrderId, order);
                return order;
            }
            catch { };

            return new Order();
        }
        public bool AnyOrder(string id)
        {
            foreach (var update in OrderUpdates.Reverse())
            {
                if (update.Key == id)
                {
                    return true;
                }
            }

            try 
            {
                var orderH = kiteService.GetKite().GetOrderHistory(id);
                if (orderH.Count > 0)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        public List<TickerLog> GetTickerLogs()
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

        // events
        private void OnTick(Tick tickData)
        {
            bool found = false;

            foreach(var tick in Ticks.Reverse())
            {
                if(tick.Key == tickData.InstrumentToken)
                {
                    Ticks.TryUpdate(tick.Key, tickData, tick.Value);
                    found = true;
                }
            }

            if (!found)
            {
                Ticks.TryAdd(tickData.InstrumentToken, tickData);
            }
        }
        private void OnOrderUpdate(Order orderData)
        {

            bool found = false;

            foreach (var update in OrderUpdates.Reverse())
            {
                if (update.Key == orderData.OrderId)
                {
                    OrderUpdates.TryUpdate(update.Key, orderData, update.Value);
                    found = true;
                }
            }

            if (!found)
            {
                OrderUpdates.TryAdd(orderData.OrderId, orderData);
            }

        }
        private void OnError(string message)
        {
            lock (tickerlogkey)
            {
                TickerLogs.Enqueue(new()
                {
                    Log = message,
                    Timestamp = DateTime.Now,
                    LogType = LogType.Error
                });
            }
        }
        private void OnClose()
        {
            lock (tickerlogkey)
            {
                TickerLogs.Enqueue(new()
                {
                    Log = "ticker connection closed...",
                    Timestamp = DateTime.Now,
                    LogType = LogType.Close
                });
            }
        }
        private void OnReconnect()
        {
            lock (tickerlogkey)
            {
                TickerLogs.Enqueue(new()
                {
                    Log = "ticker connection reconnected...",
                    Timestamp = DateTime.Now,
                    LogType = LogType.Reconnect
                });
            }
        }
        private void OnNoReconnect()
        {
            lock (tickerlogkey)
            {
                TickerLogs.Enqueue(new()
                {
                    Log = "ticker connection failed to reconnect...",
                    Timestamp = DateTime.Now,
                    LogType = LogType.NoReconnect
                });
            }
        }
        private void OnConnect()
        {
            lock (tickerlogkey)
            {
                TickerLogs.Enqueue(new()
                {
                    Log = "ticker connected...",
                    Timestamp = DateTime.Now,
                    LogType = LogType.Connect
                });
            }
        }
    }
    public interface ITickerService
    {
        Order GetOrder(string id);
        Tick LastTick(uint token);
        void Subscribe(uint token);
        void UnSubscribe(uint token);
        void Start();
        void StartWithCandles();
        bool AnyOrder(string id);
        void Stop();
        List<TickerLog> GetTickerLogs();
        void SetTicker(Ticker ticker);
    }
}
