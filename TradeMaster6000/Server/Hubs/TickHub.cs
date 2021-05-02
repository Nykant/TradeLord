using KiteConnect;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Tasks;

namespace TradeMaster6000.Server.Hubs
{
    public class TickHub : Hub
    {
        private CancellationTokenSource source;
        private CancellationTokenSource source2;
        private CancellationTokenSource source3;
        private readonly IWorker worker;
        private readonly IWorker2 worker2;
        private readonly ILogger<Worker> logger;

        public TickHub(IWorker worker, IWorker2 worker2, ILogger<Worker> logger)
        {
            this.worker = worker;
            this.worker2 = worker2;
            this.logger = logger;
        }

        public async Task StartTicker()
        {
            source = new CancellationTokenSource();

            await worker.StartTicker(source.Token, Clients, this);
        }

        public async Task StopTicker()
        {
            await worker.StopTicker();

            source.Cancel();
        }

        public async Task SendTick(decimal tick, IHubCallerClients clients)
        {
            await clients.All.SendAsync("ReceiveTick", tick);
        }

        public async Task StartTicker2()
        {
            source2 = new CancellationTokenSource();

            await worker2.StartTicker(source2.Token, Clients, this);
        }

        public async Task StopTicker2()
        {
            await worker2.StopTicker();

            source2.Cancel();
        }

        public async Task SendTick2(decimal tick, IHubCallerClients clients)
        {
            await clients.All.SendAsync("ReceiveTick2", tick);
        }
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