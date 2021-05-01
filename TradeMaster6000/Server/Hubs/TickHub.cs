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
    public class TickHub : Hub/*, IHostedService*/
    {
        private CancellationTokenSource source;
        private readonly IWorker worker;

        public TickHub(IWorker worker)
        {
            this.worker = worker;
        }
        //public async Task StartAsync(CancellationToken cancellationToken)
        //{
        //    await worker.DoWork(cancellationToken);
        //}

        //public Task StopAsync(CancellationToken cancellationToken)
        //{
        //    return Task.CompletedTask;
        //}

        public async Task StartTicker()
        {
            source = new CancellationTokenSource();

            await worker.StartTicker(source.Token, Clients);
            await SendTick(1, Clients);
        }

        public async Task StopTicker()
        {
            await worker.StopTicker();
            source.Cancel();
        }

        public async Task SendTick(decimal tick, IHubCallerClients clients)
        {
            //logger.LogInformation("");
            //Console.WriteLine($"{TickData.LastPrice}");
            await clients.All.SendAsync("ReceiveTick", tick);
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

    }
    public interface ITickHub
    {
        public Task StartTicker();
        public Task StopTicker();
        public Task SendTick(decimal tick);
        //public Task

    }
}
