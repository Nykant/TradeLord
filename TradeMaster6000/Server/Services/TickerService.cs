using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Server.Extensions;

namespace TradeMaster6000.Server.Services
{
    public class TickerService : ITickerService
    {
        private readonly object key = new ();

        private IConfiguration Configuration { get; set; }
        private readonly IKiteService kiteService;
        private static Ticker Ticker { get; set; }
        private static List<Order> OrderUpdates { get; set; } = new List<Order>();
        private static List<Order> FirstOrderUpdates { get; set; } = new List<Order>();
        private static List<Tick> Ticks { get; set; } = new List<Tick>();
        private static List<string> TickerLogs { get; set; } = new List<string>();
        private static bool Started { get; set; } = false;
        public TickerService(IConfiguration configuration, IKiteService kiteService)
        {
            this.kiteService = kiteService;
            Configuration = configuration;
        }

        public void Start()
        {
            lock (key)
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

        public Tick LastTick(uint token)
        {
            Tick dick = new ();
            foreach(var tick in Ticks)
            {
                if(tick.InstrumentToken == token)
                {
                    dick = tick;
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

        public void Stop()
        {
            Ticker.Close();
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
            TickerLogs.Add($"order update for order with id: {orderData.OrderId}...");
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
            TickerLogs.Add(message);
        }
        private void OnClose()
        {
            TickerLogs.Add("ticker connection closed...");
        }
        private void OnReconnect()
        {
            TickerLogs.Add("ticker connection reconnected...");
        }
        private void OnNoReconnect()
        {
            TickerLogs.Add("ticker connection failed to reconnect...");
        }
        private void OnConnect()
        {
            TickerLogs.Add("ticker connected...");
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
        bool IsStarted();
        bool AnyOrder(string id);
    }
}
