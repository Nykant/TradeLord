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

        public OrderHub(ITickerService tickerService, IServiceProvider serviceProvider, IRunningOrderService runningOrderService, IKiteService kiteService)
        {
            this.tickerService = tickerService;
            this.serviceProvider = serviceProvider;
            running = runningOrderService;
            KiteService = kiteService;
            tradeOrderHelper = serviceProvider.GetRequiredService<ITradeOrderHelper>();
            tradeLogHelper = serviceProvider.GetRequiredService<ITradeLogHelper>();
            instrumentHelper = serviceProvider.GetRequiredService<IInstrumentHelper>();
        }

        public async Task StartOrderWork(TradeOrder order)
        {
            OrderWork orderWork = new (serviceProvider);
            order.TokenSource = new CancellationTokenSource();

            foreach (var instrument in await instrumentHelper.GetTradeInstruments())
            {
                if (instrument.TradingSymbol == order.TradingSymbol)
                {
                    order.Instrument = instrument;
                    break;
                }
            }

            var tradeorder = await tradeOrderHelper.AddTradeOrder(order);
            order.Id = tradeorder.Id;
            running.Add(order);

            while (!KiteService.IsKiteConnected())
            {
                if (order.TokenSource.Token.IsCancellationRequested)
                {
                    running.Remove(order.Id);
                    goto Ending;
                }
                Thread.Sleep(2000);
            }

            tickerService.Start();
            tickerService.Subscribe(order.Instrument.Token);

            try
            {
                await Task.Run(async()=> await orderWork.StartWork(order, order.TokenSource.Token));
                tickerService.UnSubscribe(order.Instrument.Token);
                running.Remove(order.Id);
                if (running.Get().Count == 0)
                {
                    tickerService.Stop();
                }
                await tradeLogHelper.AddLog(order.Id, $"order stopped...").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await tradeLogHelper.AddLog(order.Id, $"some error happened lol: {e.Message}...").ConfigureAwait(false);
                try
                {
                    running.Remove(order.Id);
                }
                catch { }
            }

            Ending:;
        }

        public async Task StartMagic()
        {
            tickerService.Start();
            await tickerService.StartCandles();
        }

        public async Task GetTick(string symbol)
        {
            TradeInstrument tradeInstrument = new ();
            foreach(var instrument in await instrumentHelper.GetTradeInstruments())
            {
                if(instrument.TradingSymbol == symbol)
                {
                    tradeInstrument = instrument;
                }
            }

            var kite = KiteService.GetKite();
            if(kite != null)
            {
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
            await running.UpdateOrders();
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

        public void StopOrderWork(int id)
        {
            var orders = running.Get();
            var tOrder = new TradeOrder();
            var found = false;
            foreach (var order in orders)
            {
                if (order.Id == id)
                {
                    tOrder = order;
                    found = true;
                    break;
                }
            }
            if (found)
            {
                tOrder.TokenSource.Cancel();
            }
        }
    }
}