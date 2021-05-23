using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
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
        private readonly object key = new ();

        private IConfiguration Configuration { get; set; }
        private readonly IKiteService kiteService;
        private readonly IInstrumentHelper instrumentHelper;
        private readonly ITimeHelper timeHelper;
        private readonly ICandleDbHelper candleHelper;
        private static Ticker Ticker { get; set; }
        private static List<Order> OrderUpdates { get; set; }
        private static List<Order> FirstOrderUpdates { get; set; }
        private static List<Tick> Ticks { get; set; }
        private static List<Candle> Candles { get; set; }
        private static List<TickerLog> TickerLogs { get; set; }
        private static bool Started { get; set; } = false;
        private static CancellationTokenSource TokenSource { get; set; } = null;
        public TickerService(IConfiguration configuration, IKiteService kiteService, IInstrumentHelper instrumentHelper, ITimeHelper timeHelper, ICandleDbHelper candleHelper)
        {
            this.kiteService = kiteService;
            Configuration = configuration;
            this.instrumentHelper = instrumentHelper;
            this.timeHelper = timeHelper;
            this.candleHelper = candleHelper;
            TickerLogs = new List<TickerLog>();
            Ticks = new List<Tick>();
            Candles = new List<Candle>();
            FirstOrderUpdates = new List<Order>();
            OrderUpdates = new List<Order>();
        }

        public void Start()
        {
            lock (key)
            {
                if (!Started)
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
                    Started = true;
                }
            }
        }

        public void StartWithCandles()
        {
            lock (key)
            {
                if (!Started)
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

                    if(TokenSource == null)
                    {
                        TokenSource = new CancellationTokenSource();
                        InitializeCandles(TokenSource.Token).ConfigureAwait(false);
                    }

                    Started = true;
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
                await Task.Delay(5000);
            }

            await Task.Run(() => AnalyzeCandles(token)).ConfigureAwait(false);

            Ending:;
        }

        public async Task AnalyzeCandles(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach(var instrument in await instrumentHelper.GetTradeInstruments())
                {
                    await Task.Run(() =>
                    {
                        Analyze(token, instrument).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
            }
        }

        public async Task Analyze(CancellationToken token, TradeInstrument instrument)
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
                    if(candle.Low > ltp)
                    {
                        candle.Low = ltp;
                    }
                    await Task.Delay(500);
                }
                candle.Close = LastTick(instrument.Token).LastPrice;
                candle.To = DateTime.Now;
                stopwatch.Stop();

                await candleHelper.AddCandle(candle).ConfigureAwait(false);
            }
        }

        public Tick LastTick(uint token)
        {
            Tick dick = new ();
            for(int i = Ticks.Count; i > 0; i--)
            {
                if(Ticks[i].InstrumentToken == token)
                {
                    dick = Ticks[i];
                    break;
                }
            }

            return dick;
        }
        public Order GetOrder(string id)
        {
            Order order = new ();
            bool gotit = false;
            foreach(var update in OrderUpdates)
            {
                if(update.OrderId == id)
                {
                    order = update;
                    gotit = true;
                    break;
                }
            }
            if (!gotit)
            {
                bool gotthat = false;
                foreach(var firstUpdate in FirstOrderUpdates)
                {
                    if(firstUpdate.OrderId == id)
                    {
                        order = firstUpdate;
                        gotthat = true;
                        break;
                    }
                }
                if (!gotthat)
                {
                    var kite = kiteService.GetKite();
                    kite.SetAccessToken(kiteService.GetAccessToken());
                    var orderH = kite.GetOrderHistory(id);
                    order = orderH[^1];
                    FirstOrderUpdates.Add(order);
                }
            }
            return order;
        }
        public bool AnyOrder(string id)
        {
            bool any = false;
            foreach (var update in OrderUpdates)
            {
                if (update.OrderId == id)
                {
                    any = true;
                    break;
                }
            }
            if (!any)
            {
                var kite = kiteService.GetKite();
                kite.SetAccessToken(kiteService.GetAccessToken());
                var orderH = kite.GetOrderHistory(id);
                if(orderH.Count > 0)
                {
                    any = true;
                }
            }
            return any;
        }

        public List<TickerLog> GetTickerLogs()
        {
            return TickerLogs;
        }

        public void Stop()
        {
            Ticker.Close();
            if(TokenSource != null)
            {
                TokenSource.Cancel();
            }
            Started = false;
        }

        public bool IsStarted()
        {
            return Started;
        }

        public bool IsConnected()
        {
            return Ticker.IsConnected;
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
        private async void OnTick(Tick tickData)
        {
            await Task.Run(() =>
            {
                bool found = false;
                for (int i = 0; i < Ticks.Count; i++)
                {
                    if (Ticks[i].InstrumentToken == tickData.InstrumentToken)
                    {
                        Ticks[i] = tickData;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Ticks.Add(tickData);
                }
            }).ConfigureAwait(false);
        }
        private async void OnOrderUpdate(Order orderData)
        {
            TickerLogs.Add(new () {
                Log = $"order update for order with id: {orderData.OrderId}...",
                Timestamp = DateTime.Now,
                LogType = LogType.Order
            });

            await Task.Run(() =>
            {
                bool found = false;
                for (int i = 0; i < OrderUpdates.Count; i++)
                {
                    if (OrderUpdates[i].OrderId == orderData.OrderId)
                    {
                        OrderUpdates[i] = orderData;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    OrderUpdates.Add(orderData);
                }
            }).ConfigureAwait(false);
        }
        private void OnError(string message)
        {
            TickerLogs.Add(new()
            {
                Log = message,
                Timestamp = DateTime.Now,
                LogType = LogType.Error
            });
        }
        private void OnClose()
        {
            TickerLogs.Add(new()
            {
                Log = "ticker connection closed...",
                Timestamp = DateTime.Now,
                LogType = LogType.Close
            });
        }
        private void OnReconnect()
        {
            TickerLogs.Add(new()
            {
                Log = "ticker connection reconnected...",
                Timestamp = DateTime.Now,
                LogType = LogType.Reconnect
            });
        }
        private void OnNoReconnect()
        {
            TickerLogs.Add(new()
            {
                Log = "ticker connection failed to reconnect...",
                Timestamp = DateTime.Now,
                LogType = LogType.NoReconnect
            });
        }
        private void OnConnect()
        {
            TickerLogs.Add(new()
            {
                Log = "ticker connected...",
                Timestamp = DateTime.Now,
                LogType = LogType.Connect
            });
        }
    }
    public interface ITickerService
    {
        Order GetOrder(string id);
        Tick LastTick(uint token);
        bool IsConnected();
        void Subscribe(uint token);
        void UnSubscribe(uint token);
        void Start();
        void StartWithCandles();
        bool IsStarted();
        bool AnyOrder(string id);
        void Stop();
        List<TickerLog> GetTickerLogs();
    }
}
