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

        public OrderHub(IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService, IInstrumentService instrumentService, IDbContextFactory<TradeDbContext> contextFactory)
        {
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            this.kiteService = kiteService;
            this.contextFactory = contextFactory;
            instrumentHelper = new InstrumentHelper(instrumentService, this.contextFactory);
            tradeOrderHelper = new TradeOrderHelper(this.contextFactory);
            tradeLogHelper = new TradeLogHelper(this.contextFactory);
            runningOrders = new List<TradeOrder>();
        }

        // start order work with inputs from user 
        public async Task StartOrderWork(TradeOrder order)
        {
            OrderWork orderWork = new OrderWork(_configuration, _contextAccessor, kiteService, tradeOrderHelper, tradeLogHelper, this);
            order.TokenSource = new CancellationTokenSource();

            foreach(var instrument in await instrumentHelper.GetTradeInstruments())
            {
                if(instrument.TradingSymbol == order.TradingSymbol)
                {
                    order.Instrument = instrument;
                }
            }

            order.Status = Status.STARTING;
            var tradeorder = await tradeOrderHelper.AddTradeOrder(order);
            order = tradeorder;
            runningOrders.Add(order);

            await Clients.Caller.SendAsync("ReceiveList", await tradeOrderHelper.GetTradeOrders()).ConfigureAwait(false);

            await Task.Run(async () =>
            {
                await orderWork.StartWork(order);
                await StopOrderWork(order.Id);
                await tradeLogHelper.AddLog(order.Id, $"order stopped...").ConfigureAwait(false);
                order.Status = Status.DONE;
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