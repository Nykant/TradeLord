using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IKiteService kiteService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly TradeOrderHelper tradeOrderHelper;
        private readonly InstrumentHelper instrumentHelper;
        private readonly TradeLogHelper tradeLogHelper;
        private static List<TradeOrder> runningOrders;
        private readonly IDbContextFactory<TradeDbContext> contextFactory;
        private readonly ITickerService tickerService;
        private readonly IServiceProvider serviceProvider;

        public OrderHub(ITickerService tickerService, IServiceProvider serviceProvider)
        {
            this.tickerService = tickerService;
            this.serviceProvider = serviceProvider;
        }

        // start order work with inputs from user 
        public async Task StartOrderWork(TradeOrder order)
        {
            OrderWork orderWork = new OrderWork(this, serviceProvider);
            order.TokenSource = new CancellationTokenSource();

            Task t = Task.Run(async() =>
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

            order.Status = Status.STARTING;
            var tradeorder = await tradeOrderHelper.AddTradeOrder(order);
            order = tradeorder;
            runningOrders.Add(order);

            tickerService.Subscribe(order.Instrument.Token);

            await Clients.Caller.SendAsync("ReceiveList", runningOrders).ConfigureAwait(false);

            await t;
            await Task.Run(async () =>
            {
                await orderWork.StartWork(order);
                await StopOrderWork(order.Id);
                order.Status = Status.DONE;
                await tradeLogHelper.AddLog(order.Id, $"order stopped...").ConfigureAwait(false);
                await tradeOrderHelper.UpdateTradeOrder(order).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task GetInstruments()
        {
            await Clients.Caller.SendAsync("ReceiveInstruments", await instrumentHelper.GetTradeInstruments());
        }

        public async Task GetOrders()
        {
            await Clients.Caller.SendAsync("ReceiveList", runningOrders);
        }

        public async Task GetLogs(int orderId)
        {
            var logs = await tradeLogHelper.GetTradeLogs(orderId);
            await Clients.Caller.SendAsync("ReceiveLogs", logs);
        }

        public async Task StopOrderWork(int id)
        {
            await Task.Run(() =>
            {
                if (runningOrders.Count > 0)
                {
                    for (int i = 0; i < runningOrders.Count; i++)
                    {
                        if (runningOrders[i].Id == id)
                        {
                            runningOrders[i].TokenSource.Cancel();
                            runningOrders.RemoveAt(i);
                            break;
                        }
                    }
                }
            }).ConfigureAwait(false);
        }
    }
}