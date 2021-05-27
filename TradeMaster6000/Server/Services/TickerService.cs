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
        private List<Order> OrderUpdates { get; set; }
        private List<Order> FirstOrderUpdates { get; set; }
        private List<Tick> Ticks { get; set; }
        private List<TickerLog> TickerLogs { get; set; }
        public TickerService(IConfiguration configuration, IKiteService kiteService, IInstrumentHelper instrumentHelper, ITimeHelper timeHelper, ICandleDbHelper candleHelper)
        {
            this.kiteService = kiteService;
            Configuration = configuration;
            this.instrumentHelper = instrumentHelper;
            this.timeHelper = timeHelper;
            this.candleHelper = candleHelper;
            TickerLogs = new List<TickerLog>();
            Ticks = new List<Tick>();
            FirstOrderUpdates = new List<Order>();
            OrderUpdates = new List<Order>();
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
            Tick dick = new ();
            List<Tick> ticks = Ticks;

            if(ticks.Count > 0)
            {
                for (int i = ticks.Count - 1; i >= 0; i--)
                {
                    if (ticks[i].InstrumentToken == token)
                    {
                        dick = ticks[i];
                        break;
                    }
                }
            }
            return dick;
        }
        public Order GetOrder(string id)
        {
            Order order = new ();
            bool gotit = false;
            List<Order> updates = OrderUpdates;

            if (updates.Count > 0)
            {
                for (int i = updates.Count - 1; i >= 0; i--)
                {
                    if (updates[i].OrderId == id)
                    {
                        order = updates[i];
                        gotit = true;
                        break;
                    }
                }
            }
            if (!gotit)
            {
                List<Order> firstUpdates = FirstOrderUpdates;

                bool gotthat = false;
                foreach (var firstUpdate in firstUpdates)
                {
                    if (firstUpdate.OrderId == id)
                    {
                        order = firstUpdate;
                        gotthat = true;
                        break;
                    }
                }
                if (!gotthat)
                {
                    var kite = kiteService.GetKite();
                    var orderH = kite.GetOrderHistory(id);
                    order = orderH[^1];
                    lock (firstorderkey)
                    {
                        FirstOrderUpdates.Add(order);
                    }
                }
            }
            return order;
        }
        public bool AnyOrder(string id)
        {
            bool any = false;
            List<Order> updates = OrderUpdates;

            foreach (var update in updates)
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
            List<Tick> ticks;
            lock (tickkey)
            {
                ticks = Ticks;

                bool found = false;
                if (ticks.Count > 0)
                {
                    for (int i = 0; i < ticks.Count; i++)
                    {
                        if (ticks[i].InstrumentToken == tickData.InstrumentToken)
                        {

                            Ticks[i] = tickData;

                            found = true;
                            break;
                        }
                    }
                }
                if (!found)
                {
                    Ticks.Add(tickData);
                }
            }
        }
        private void OnOrderUpdate(Order orderData)
        {
            List<Order> updates;
            lock (orderkey)
            {
                updates = OrderUpdates;

                bool found = false;
                if (updates.Count > 0)
                {
                    for (int i = updates.Count - 1; i > 0; i--)
                    {
                        if (updates[i].OrderId == orderData.OrderId)
                        {
                            if (updates[i].FilledQuantity > orderData.FilledQuantity)
                            {
                                found = true;
                                break;
                            }
                            else
                            {
                                OrderUpdates[i] = orderData;

                                found = true;
                                break;
                            }
                        }
                    }
                }
                if (!found)
                {
                    OrderUpdates.Add(orderData);
                }
            }
        }
        private void OnError(string message)
        {
            lock (tickerlogkey)
            {
                TickerLogs.Add(new()
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
                TickerLogs.Add(new()
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
                TickerLogs.Add(new()
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
                TickerLogs.Add(new()
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
                TickerLogs.Add(new()
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
