using KiteConnect;
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

namespace TradeMaster6000.Server.Hubs
{

    public class OrderHub : Hub
    {
        private readonly ITradeOrderHelper tradeOrderHelper;
        private readonly IInstrumentHelper instrumentHelper;
        private readonly ITradeLogHelper tradeLogHelper;
        private readonly ITickerService tickerService;
        private readonly IRunningOrderService running;
        private readonly IOrderManagerService orderManagerService;
        private IKiteService KiteService { get; set; }

        public OrderHub(ITickerService tickerService, IServiceProvider serviceProvider, IRunningOrderService runningOrderService, IKiteService kiteService, IOrderManagerService orderManagerService)
        {
            this.tickerService = tickerService;
            this.orderManagerService = orderManagerService;
            running = runningOrderService;
            KiteService = kiteService;
            tradeOrderHelper = serviceProvider.GetRequiredService<ITradeOrderHelper>();
            tradeLogHelper = serviceProvider.GetRequiredService<ITradeLogHelper>();
            instrumentHelper = serviceProvider.GetRequiredService<IInstrumentHelper>();
        }

        public async Task NewOrder(TradeOrder order)
        {
            await orderManagerService.StartOrder(order).ConfigureAwait(false);
        }

        public async Task AutoOrders()
        {
            await orderManagerService.AutoOrders().ConfigureAwait(false);
        }

        public async Task StartMagic()
        {
            await tickerService.Start();
            await tickerService.StartCandles();
        }

        public async Task GetTick(string symbol)
        {
            var kite = KiteService.GetKite();
            if(kite != null)
            {
                TradeInstrument tradeInstrument = new();
                var instruments = await instrumentHelper.GetTradeInstruments();
                await Task.Run(() =>
                {
                    foreach (var instrument in instruments)
                    {
                        if (instrument.TradingSymbol == symbol)
                        {
                            tradeInstrument = instrument;
                            break;
                        }
                    }
                });

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

        public async Task GetOrders()
        {
            await Task.Run(async()=> await running.UpdateOrders());
            await Clients.Caller.SendAsync("ReceiveOrders", running.Get());
        }

        public async Task GetRunningLogs()
        {
            await Clients.Caller.SendAsync("ReceiveRunningLogs", running.GetLogs());
        }

        public async Task GetLogs(int orderId)
        {
            await Clients.Caller.SendAsync("ReceiveLogs", await tradeLogHelper.GetTradeLogs(orderId));
        }

        public async Task StopOrderWork(int id)
        {
            await Task.Run(()=>running.StopOrder(id)).ConfigureAwait(false);
        }
    }
}