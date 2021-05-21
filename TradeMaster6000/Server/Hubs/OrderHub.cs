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
        private readonly IServiceProvider serviceProvider;
        private readonly IRunningOrderService running;
        private IKiteService KiteService { get; set; }
        private Kite Kite { get; set; }

        public OrderHub(ITickerService tickerService, IServiceProvider serviceProvider, IRunningOrderService runningOrderService, IKiteService kiteService)
        {
            this.tickerService = tickerService;
            this.serviceProvider = serviceProvider;
            running = runningOrderService;
            KiteService = kiteService;
            tradeOrderHelper = serviceProvider.GetRequiredService<ITradeOrderHelper>();
            tradeLogHelper = serviceProvider.GetRequiredService<ITradeLogHelper>();
            instrumentHelper = serviceProvider.GetRequiredService<IInstrumentHelper>();
            Kite = KiteService.GetKite();
        }

        public async Task StartOrderWork(TradeOrder order)
        {
            OrderWork orderWork = new OrderWork(this, serviceProvider);
            order.TokenSource = new CancellationTokenSource();

            await Task.Run(async() =>
            {
                foreach (var instrument in await instrumentHelper.GetTradeInstruments())
                {
                    if (instrument.TradingSymbol == order.TradingSymbol)
                    {
                        order.Instrument = instrument;
                        break;
                    }
                }
            });

            if (!tickerService.IsStarted())
            {
                tickerService.Start();
            }

            var tradeorder = await tradeOrderHelper.AddTradeOrder(order);
            order.Id = tradeorder.Id;

            running.Add(order);
            await Clients.Caller.SendAsync("ReceiveOrders", running.Get());

            tickerService.Subscribe(order.Instrument.Token);

            await Task.Run(async () =>
            {
                await orderWork.StartWork(order, order.TokenSource.Token);
                await StopOrderWork(order.Id);
                await tradeLogHelper.AddLog(order.Id, $"order stopped...").ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task GetTick(string symbol)
        {
            TradeInstrument tradeInstrument = new TradeInstrument();
            foreach(var instrument in await instrumentHelper.GetTradeInstruments())
            {
                if(instrument.TradingSymbol == symbol)
                {
                    tradeInstrument = instrument;
                }
            }
            var dick = Kite.GetLTP(new[] { tradeInstrument.Token.ToString() });
            dick.TryGetValue(tradeInstrument.Token.ToString(), out LTP value);
            await Clients.Caller.SendAsync("ReceiveTick", value.LastPrice);
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
            await Task.Run(async() => {
                await running.UpdateOrders();
                await Clients.Caller.SendAsync("ReceiveOrders", running.Get());
            }).ConfigureAwait(false);
        }

        public async Task GetLogs(int orderId)
        {
            await Clients.Caller.SendAsync("ReceiveLogs", await tradeLogHelper.GetTradeLogs(orderId));
        }

        public async Task StopOrderWork(int id)
        {
            await Task.Run(() =>
            {
                var orders = running.Get();
                var tOrder = new TradeOrder();
                var found = false;
                foreach (var order in orders)
                {
                    if(order.Id == id)
                    {
                        tOrder = order;
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    tOrder.TokenSource.Cancel();
                    running.Remove(tOrder);
                }

            }).ConfigureAwait(false);
        }
    }
}