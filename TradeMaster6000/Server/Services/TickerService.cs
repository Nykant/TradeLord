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
        private static object key = new object();

        private IServiceProvider Services { get; }
        private IConfiguration Configuration { get; set; }
        private IHttpContextAccessor ContextAccessor { get; set; }
        private Ticker Ticker { get; set; } = null;
        private List<Order> OrderUpdates { get; set; }
        private List<Tick> Ticks { get; set; }
        private List<string> TickerLogs { get; set; }
        private bool Started { get; set; } = false;
        public TickerService(IServiceProvider services)
        {
            Services = services;
        }

        public void Start()
        {
            lock (key)
            {
                // get DI services
                ContextAccessor = Services.GetRequiredService<IHttpContextAccessor>();
                Configuration = Services.GetRequiredService<IConfiguration>();

                // new ticker instance 
                Ticker = new Ticker(Configuration.GetValue<string>("APIKey"), ContextAccessor.HttpContext.Session.Get<string>(Configuration.GetValue<string>("AccessToken")));

                // ticker event handlers
                Ticker.OnTick += onTick;
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

        private void onTick(Tick tickData)
        {
            Ticks.Add(tickData);
        }

        // events
        private async void OnOrderUpdate(Order orderData)
        {
            await Task.Run(() =>
            {
                bool found = false;
                for (int i = 0; i < OrderUpdates.Count - 1; i++)
                {
                    if (OrderUpdates[i].OrderId == orderData.OrderId)
                    {
                        OrderUpdates[i] = orderData;
                        found = true;
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
        bool IsConnected();
        void Subscribe(uint token);
        void UnSubscribe(uint token);
        void Start();
        bool IsStarted();
    }
}
