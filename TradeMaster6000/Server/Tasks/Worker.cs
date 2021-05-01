using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Extensions;
using TradeMaster6000.Server.Hubs;

namespace TradeMaster6000.Server.Tasks
{
    public class Worker : IWorker
    {
        private readonly ILogger<Worker> logger;
        private static TickHub tickHub;
        private Ticker ticker;
        private IHttpContextAccessor _contextAccessor;
        private static IHubCallerClients clients;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor)
        {
            this.logger = logger;
            Configuration = configuration;
            tickHub = new TickHub(this);
            _contextAccessor = contextAccessor;
            ticker = new Ticker(Configuration.GetValue<string>("APIKey"), _contextAccessor.HttpContext.Session.Get<string>(Configuration.GetValue<string>("AccessTokenPassword")));
        }

        public IConfiguration Configuration { get; }

        public async Task StartTicker(CancellationToken cancellationToken, IHubCallerClients clients)
        {
            Worker.clients = clients;
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

        }
        public async Task StopTicker()
        {
            ticker.Close();
        }
        private static async void onTick(Tick TickData)
        {
            await tickHub.SendTick(TickData.LastPrice, clients);
        }

        //private static void OnOrderUpdate(Order OrderData)
        //{
        //    Console.WriteLine("OrderUpdate " + Utils.JsonSerialize(OrderData));
        //}
    }

    public interface IWorker
    {
        Task StartTicker(CancellationToken cancellationToken, IHubCallerClients clients);
        Task StopTicker();
    }
}
