using KiteConnect;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Server.Tasks;
using TradeMaster6000.Shared;
using System.Collections.Concurrent;
using TradeMaster6000.Server.Helpers;

namespace TradeMaster6000.Server.Hubs
{

    public class OrderHub : Hub
    {
        private readonly ITradeOrderHelper tradeOrderHelper;
        private readonly IInstrumentHelper instrumentHelper;
        private readonly ITradeLogHelper tradeLogHelper;
        private readonly ITickerService tickerService;
        private readonly IOrderManagerService orderManagerService;
        private readonly IBackgroundJobClient backgroundJob;
        private readonly ICandleDbHelper candleDbHelper;
        private readonly IZoneService zoneService;
        private readonly IZoneDbHelper zoneDbHelper;
        private readonly ITimeHelper timeHelper;
        private readonly ITradeabilityService tradeabilityService;
        private readonly ILogger<OrderHub> logger;

        public OrderHub(ITickerService tickerService, IServiceProvider serviceProvider, IOrderManagerService orderManagerService, IBackgroundJobClient backgroundJob, ICandleDbHelper candleDbHelper, IZoneService zoneService, IZoneDbHelper zoneDbHelper, ITimeHelper timeHelper, ILogger<OrderHub> logger, ITradeabilityService tradeabilityService)
        {
            this.zoneDbHelper = zoneDbHelper;
            this.tickerService = tickerService;
            this.orderManagerService = orderManagerService;
            this.backgroundJob = backgroundJob;
            this.candleDbHelper = candleDbHelper;
            this.zoneService = zoneService;
            this.timeHelper = timeHelper;
            this.logger = logger;
            this.tradeabilityService = tradeabilityService;
            tradeOrderHelper = serviceProvider.GetRequiredService<ITradeOrderHelper>();
            tradeLogHelper = serviceProvider.GetRequiredService<ITradeLogHelper>();
            instrumentHelper = serviceProvider.GetRequiredService<IInstrumentHelper>();
        }

        public async Task GetZoneCandles()
        {
            await Clients.Caller.SendAsync("ReceiveZoneCandles", zoneService.GetZoneCandles());
        }

        public async Task LoadExcelCandles()
        {
            await candleDbHelper.LoadExcelCandles();
        }

        public async Task StartCandleMagic()
        {
            
            if (!tickerService.IsCandlesRunning())
            {
                await tickerService.RunCandles();
            }
        }

        public void StopCandleMagic()
        {
            tickerService.StopCandles();
        }

        public async Task StartZoneService()
        {
            await zoneService.StartZoneServiceOnce();
        }

        public void StartTrader()
        {
            DateTime current = timeHelper.CurrentTime();
            DateTime opening = timeHelper.OpeningTime();
            TimeSpan duration = timeHelper.GetDuration(opening, current);

            backgroundJob.Schedule(() => RunTrader(), duration);
        }

        public async Task MarkCandlesUnused()
        {
            await candleDbHelper.MarkAllCandlesUnused();
            logger.LogInformation("done marking candles as unused");
        }

        public async Task RunTrader()
        {
            if (!tickerService.IsCandlesRunning() && !zoneService.IsZoneServiceRunning())
            {
                await tickerService.RunCandles();
                await zoneService.StartZoneService();
            }
        }

        public void StopTrader()
        {
            tradeabilityService.Stop();
            tickerService.StopCandles();
            zoneService.CancelToken();
        }

        public async Task GetZones()
        {
            await Clients.Caller.SendAsync("ReceiveZones", await zoneDbHelper.GetZones());
        }

        public async Task GetTickerLogs()
        {
            await Clients.Caller.SendAsync("ReceiveTickerLogs", tickerService.GetSomeLogs());
        }

        public async Task GetOrderHistory()
        {
            await Clients.Caller.SendAsync("ReceiveOrderHistory", await tradeOrderHelper.GetTradeOrders());
        }

        public async Task GetInstruments()
        {
            await Clients.Caller.SendAsync("ReceiveInstruments", await instrumentHelper.GetTradeInstruments());
        }

        public async Task GetOrder(int id)
        {
            await Clients.Caller.SendAsync("ReceiveOrder", await tradeOrderHelper.GetTradeOrder(id));
        }

        public async Task Update()
        {
            await Clients.Caller.SendAsync("ReceiveOrders", await tradeOrderHelper.GetRunningTradeOrders());
        }

        public async Task GetLogs(int orderId)
        {
            await Clients.Caller.SendAsync("ReceiveLogs", await tradeLogHelper.GetTradeLogs(orderId));
        }

        public async Task GetCandles(string tradingSymbol)
        {
            var instruments = await instrumentHelper.GetTradeInstruments();
            foreach(var instrument in instruments)
            {
                if(instrument.TradingSymbol == tradingSymbol)
                {
                    await Clients.Caller.SendAsync("ReceiveCandles", await candleDbHelper.GetCandles(instrument.Token));
                    break;
                }
            }
        }

        public async Task StopOrderWork(int id)
        {
            var orders = await tradeOrderHelper.GetRunningTradeOrders();
            foreach (var order in orders)
            {
                if (order.Id == id)
                {
                    orderManagerService.CancelToken(id);
                }
            }
        }
    }
}