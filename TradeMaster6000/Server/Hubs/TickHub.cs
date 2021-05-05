﻿using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Server.Tasks;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Hubs
{
    public class TickHub : Hub
    {
        private static List<TradeOrder> orderList = new List<TradeOrder>();
        //private readonly IWorker worker;
        //private readonly IWorker2 worker2;
        private readonly ILogger<Worker> logger;
        private readonly IKiteService kiteService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        // måske gem tickers her inde i stedet for i worker
        private static int OrderCount { get; set; }
        // kan bruge samme worker 

        public TickHub(/*IWorker worker, IWorker2 worker2, */ILogger<Worker> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService)
        {
            //this.worker = worker;
            //this.worker2 = worker2;
            this.logger = logger;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            this.kiteService = kiteService;
        }

        public async Task StartTicker(TradeOrder tradeOrder)
        {
            Worker worker = new Worker(logger, _configuration, _contextAccessor, kiteService);
            var order = new TradeOrder {
                 Entry = tradeOrder.Entry,
                  StopLoss = tradeOrder.StopLoss,
                   TakeProfit = tradeOrder.TakeProfit,
                Id = OrderCount,
                TokenSource = new CancellationTokenSource()
            };
            OrderCount = OrderCount + 1;
            orderList.Add(order);

            await Clients.Caller.SendAsync("ReceiveList", orderList);

            await Task.Run(async () => 
            {
                await worker.StartTicker(Clients, this, order, order.TokenSource.Token);
            });
        }

        public async Task GetOrders()
        {
            await Clients.Caller.SendAsync("ReceiveList", orderList);
        }

        public async Task StopTicker(TradeOrder tradeOrder)
        {
            if(orderList.Count > 0)
            {
                for (int i = 0; i < orderList.Count; i++)
                {
                    if (orderList[i].Id == tradeOrder.Id)
                    {
                        orderList[i].TokenSource.Cancel();
                        orderList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public async Task SendTick(decimal tick, IHubCallerClients clients)
        {
            await clients.All.SendAsync("ReceiveTick", tick);
        }

        //public async Task StartTicker2()
        //{
        //    //source2 = new CancellationTokenSource();

        //    await worker2.StartTicker(Clients, this);
        //}

        //public async Task StopTicker2()
        //{
        //    //source2.Cancel();

        //    await worker2.StopTicker();
        //}

        //public async Task SendTick2(decimal tick, IHubCallerClients clients)
        //{
        //    await clients.All.SendAsync("ReceiveTick2", tick);
        //}
    }

    //public interface ITickHub
    //{
    //    public Task StartTicker();
    //    public Task StopTicker();
    //    public Task SendTick(decimal tick, IHubCallerClients clients);
    //    public Task StartTicker2();
    //    public Task StopTicker2();
    //    public Task SendTick2(decimal tick, IHubCallerClients clients);
    //}
}


// public async Task GetTicks(CancellationToken cancellationToken)
//{
//    ticker.OnTick += onTick;
//    //ticker.OnOrderUpdate += OnOrderUpdate;
//    //ticker.OnReconnect += onReconnect;
//    //ticker.OnNoReconnect += oNoReconnect;
//    //ticker.OnError += onError;
//    //ticker.OnClose += onClose;
//    //ticker.OnConnect += onConnect;

//    ticker.EnableReconnect(Interval: 5, Retries: 50);
//    ticker.Connect();

//    ticker.Subscribe(Tokens: new UInt32[] { 60417 });
//    ticker.SetMode(Tokens: new UInt32[] { 60417 }, Mode: Constants.MODE_LTP);

//    while (!cancellationToken.IsCancellationRequested)
//    {

//    }

//    ticker.Close();
//}
//private static void onTick(Tick TickData)
//{
//    //await tickHub.SendTick(TickData.LastPrice);
//}

//public async Task DoWork(CancellationToken cancellationToken)
//{
//    // Create a new Ticker instance
//    Ticker ticker = new Ticker("fpjghegvpdmrifse", Configuration.GetValue<string>("AccessTokenPassword"));

//    ticker.OnTick += onTick;
//    //ticker.OnOrderUpdate += OnOrderUpdate;
//    //ticker.OnReconnect += onReconnect;
//    //ticker.OnNoReconnect += oNoReconnect;
//    //ticker.OnError += onError;
//    //ticker.OnClose += onClose;
//    //ticker.OnConnect += onConnect;

//    ticker.EnableReconnect(Interval: 5, Retries: 50);
//    ticker.Connect();

//    ticker.Subscribe(Tokens: new UInt32[] { 60417 });
//    ticker.SetMode(Tokens: new UInt32[] { 60417 }, Mode: Constants.MODE_LTP);
//    logger.LogInformation($"Subscribed to {60417}");

//    while (!cancellationToken.IsCancellationRequested)
//    {

//    }

//    ticker.Close();
//}
//public async Task SendTick()
//{
//    // Create a new Ticker instance
//    Ticker ticker = new Ticker("fpjghegvpdmrifse", "accesstokenpassword");

//    ticker.OnTick += onTick;
//    //ticker.OnOrderUpdate += OnOrderUpdate;
//    //ticker.OnReconnect += onReconnect;
//    //ticker.OnNoReconnect += oNoReconnect;
//    //ticker.OnError += onError;
//    //ticker.OnClose += onClose;
//    //ticker.OnConnect += onConnect;

//    ticker.EnableReconnect(Interval: 5, Retries: 50);
//    ticker.Connect();

//    ticker.Subscribe(Tokens: new UInt32[] { 60417 });
//    ticker.SetMode(Tokens: new UInt32[] { 60417 }, Mode: Constants.MODE_LTP);
//}