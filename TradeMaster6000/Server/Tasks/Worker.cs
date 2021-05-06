using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Extensions;
using TradeMaster6000.Server.Hubs;
using TradeMaster6000.Server.Models;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Tasks
{
    public class Worker : IWorker
    {
        private readonly ILogger<Worker> logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private IHubCallerClients clients;
        private static TickHub _tickhub;
        private readonly IKiteService kiteService;
        private static Kite kite;
        private static List<KiteConnect.Tick> ticks;
        private readonly IConfiguration configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService)
        {
            this.logger = logger;
            this.configuration = configuration;
            _contextAccessor = contextAccessor;
            this.kiteService = kiteService;
            ticks = new List<KiteConnect.Tick>();
        }


        public async Task StartTicker(IHubCallerClients clients, TickHub tickHub, TradeOrder order, CancellationToken cancellationToken)
        {
            _tickhub = tickHub;
            if (kite == null)
            {
                kite = kiteService.GetKite();
            }

            Ticker ticker = new Ticker(configuration.GetValue<string>("APIKey"), _contextAccessor.HttpContext.Session.Get<string>(configuration.GetValue<string>("AccessToken")));

            this.clients = clients;
            ticker.OnTick += onTick;
            //ticker.OnOrderUpdate += OnOrderUpdate;
            //ticker.OnReconnect += onReconnect;
            //ticker.OnNoReconnect += oNoReconnect;
            //ticker.OnError += onError;
            //ticker.OnClose += onClose;
            //ticker.OnConnect += onConnect;

            ticker.EnableReconnect(Interval: 5, Retries: 50);
            ticker.Connect();

            ticker.Subscribe(Tokens: new UInt32[] { order.Instrument.Id });
            ticker.SetMode(Tokens: new UInt32[] { order.Instrument.Id }, Mode: Constants.MODE_LTP);

            await _tickhub.AddLog($"log: order with id:{order.Id} starting...");
            //await clients.Caller.SendAsync("ReceiveLog", $"log: order with id:{order.Id} starting...");

            decimal lsp = 0;
            // THIS PART MAYBE SHOULD BE IN ITS OWN ASYNC METHOD
            while (!cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation($"test");
                if (ticks.Count > 0)
                {
                    if(lsp != ticks[ticks.Count - 1].LastPrice)
                    {
                        lsp = ticks[ticks.Count - 1].LastPrice;
                        logger.LogInformation($"LSP: {lsp}");
                    }

                    if (lsp >= order.Entry)
                    {
                        //Dictionary<string, dynamic> response = kite.PlaceOrder(
                        //    Exchange: order.Instrument.Exchange,
                        //    TradingSymbol: order.Instrument.TradingSymbol,
                        //    TransactionType: order.TransactionType.ToString(),
                        //    Quantity: order.Quantity,
                        //    Price: order.Entry,

                        //    OrderType: order.OrderType.ToString(),
                        //    Product: order.Product.ToString(),
                        //    StoplossValue: order.StopLoss,
                        //    TriggerPrice: order.TakeProfit
                            //TrailingStoploss: 64.0000m
                        //);
                    }
                }
                Thread.Sleep(1000);
            }

            ticker.Close();
            await _tickhub.AddLog($"log: order with id:{order.Id} stopped...");
            //await clients.Caller.SendAsync("ReceiveLog", $"log: order with id:{order.Id} stopped...");
        }
        //maybe the start ticker needs a while loop or do ticker.close after source.cancel, maybe stopticker has source as parameter. maybe start ticker has source parameter
        //public async Task StopTicker()
        //{


        //    logger.LogInformation("ticker closed");
        //}
        private static void onTick(KiteConnect.Tick TickData)
        {
            ticks.Add(TickData);
            //await _tickhub.SendTick(TickData.LastPrice, clients);
            //await clients.All.SendAsync("ReceiveTick", TickData.LastPrice);
        }
    }

    public interface IWorker
    {
        Task StartTicker(IHubCallerClients clients, TickHub tickHub, TradeOrder order, CancellationToken cancellationToken);
        //Task StopTicker();
    }
}
