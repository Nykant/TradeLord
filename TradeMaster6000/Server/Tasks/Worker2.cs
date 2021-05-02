using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Extensions;
using TradeMaster6000.Server.Hubs;

namespace TradeMaster6000.Server.Tasks
{
    public class Worker2 : IWorker2
    {
        private readonly ILogger<Worker2> logger;
        private Ticker ticker;
        private IHttpContextAccessor _contextAccessor;
        private static IHubCallerClients clients;
        private static TickHub _tickhub;

        public Worker2(ILogger<Worker2> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor)
        {
            this.logger = logger;
            Configuration = configuration;
            _contextAccessor = contextAccessor;
        }

        public IConfiguration Configuration { get; }

        public async Task StartTicker(CancellationToken cancellationToken, IHubCallerClients clients, TickHub tickHub)
        {
            _tickhub = tickHub;
                ticker = new Ticker(Configuration.GetValue<string>("APIKey"), _contextAccessor.HttpContext.Session.Get<string>(Configuration.GetValue<string>("AccessToken")));

            Worker2.clients = clients;
            ticker.OnTick += onTick;
            //ticker.OnOrderUpdate += OnOrderUpdate;
            //ticker.OnReconnect += onReconnect;
            //ticker.OnNoReconnect += oNoReconnect;
            //ticker.OnError += onError;
            //ticker.OnClose += onClose;
            //ticker.OnConnect += onConnect;

            ticker.EnableReconnect(Interval: 5, Retries: 50);
            ticker.Connect();

            ticker.Subscribe(Tokens: new UInt32[] { 60417 });
            ticker.SetMode(Tokens: new UInt32[] { 60417 }, Mode: Constants.MODE_LTP);

            logger.LogInformation("ticker started");

        }
        public async Task StopTicker()
        {
            ticker.Close();

            logger.LogInformation("ticker closed");
        }
        private static async void onTick(Tick TickData)
        {
            await _tickhub.SendTick2(TickData.LastPrice, clients);
        }
    }

    public interface IWorker2
    {
        Task StartTicker(CancellationToken cancellationToken, IHubCallerClients clients, TickHub tickHub);
        Task StopTicker();
    }
}
