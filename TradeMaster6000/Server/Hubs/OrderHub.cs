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
        private IKiteService KiteService { get; set; }

        public OrderHub(ITickerService tickerService, IServiceProvider serviceProvider, IKiteService kiteService, IOrderManagerService orderManagerService, IBackgroundJobClient backgroundJob, ICandleDbHelper candleDbHelper, IZoneService zoneService, IZoneDbHelper zoneDbHelper)
        {
            this.zoneDbHelper = zoneDbHelper;
            this.tickerService = tickerService;
            this.orderManagerService = orderManagerService;
            this.backgroundJob = backgroundJob;
            this.candleDbHelper = candleDbHelper;
            this.zoneService = zoneService;
            KiteService = kiteService;
            tradeOrderHelper = serviceProvider.GetRequiredService<ITradeOrderHelper>();
            tradeLogHelper = serviceProvider.GetRequiredService<ITradeLogHelper>();
            instrumentHelper = serviceProvider.GetRequiredService<IInstrumentHelper>();
        }

        public async Task GetZoneCandles()
        {
            await Clients.Caller.SendAsync("ReceiveZoneCandles", zoneService.GetZoneCandles());
        }

        public void LoadExcelCandles()
        {
            candleDbHelper.LoadExcelCandles();
        }

        public async Task NewOrder(TradeOrder order)
        {
            await orderManagerService.StartOrder(order);
        }

        public async Task AutoOrders()
        {

            if (!tickerService.IsOrderUpdateOn())
            {
                tickerService.StartOrderUpdatesManager();
            }
            await orderManagerService.AutoOrders(5).ConfigureAwait(false);
        }

        public async Task AutoUltraOrders()
        {
            await orderManagerService.AutoOrders(20).ConfigureAwait(false);
        }

        public void StartCandleMagic()
        {
            
            if (!tickerService.IsCandlesRunning())
            {
                
            }
        }

        public void StopCandleMagic()
        {
            tickerService.StopCandles();
        }

        public async Task StartZoneService()
        {
            await zoneService.StartZoneService();
        }

        public async Task StartTrader()
        {
            if(!tickerService.IsCandlesRunning() && !zoneService.IsZoneServiceRunning())
            {
                await tickerService.RunCandles();
                await zoneService.StartZoneService();
            }
        }

        public async Task GetZones()
        {
            await Clients.Caller.SendAsync("ReceiveZones", await zoneDbHelper.GetZones());
        }

        public async Task GetTick(string symbol)
        {
            var kite = KiteService.GetKite();
            if(kite != null)
            {
                TradeInstrument tradeInstrument = new();
                var instruments = await instrumentHelper.GetTradeInstruments();
                foreach (var instrument in instruments)
                {
                    if (instrument.TradingSymbol == symbol)
                    {
                        tradeInstrument = instrument;
                        break;
                    }
                }
                var dick = kite.GetLTP(new[] { tradeInstrument.Token.ToString() });
                dick.TryGetValue(tradeInstrument.Token.ToString(), out LTP value);
                await Clients.Caller.SendAsync("ReceiveTick", value.LastPrice);
            }
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