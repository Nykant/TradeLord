using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
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
        private readonly ILogger<OrderWork> logger;
        private readonly IKiteService kiteService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ITradeOrderHelper tradeOrderHelper;
        private readonly IInstrumentHelper instrumentHelper;
        private readonly ITradeLogHelper tradeLogHelper;
        private static List<TradeOrder> runningOrders;

        public OrderHub(ILogger<OrderWork> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService, ITradeOrderHelper tradeOrderHelper, IInstrumentHelper instrumentHelper, ITradeLogHelper tradeLogHelper)
        {
            this.logger = logger;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            this.kiteService = kiteService;
            this.tradeOrderHelper = tradeOrderHelper;
            this.instrumentHelper = instrumentHelper;
            this.tradeLogHelper = tradeLogHelper;
            runningOrders = new List<TradeOrder>();
        }

        // start order work with inputs from user 
        public async Task StartOrderWork(TradeOrder order)
        {
            OrderWork orderWork = new OrderWork(logger, _configuration, _contextAccessor, kiteService, tradeOrderHelper);
            order.TokenSource = new CancellationTokenSource();

            foreach(var instrument in await instrumentHelper.GetTradeInstruments())
            {
                if(instrument.TradingSymbol == order.TradingSymbol)
                {
                    order.Instrument = instrument;
                }
            }

            var tradeorder = tradeOrderHelper.AddTradeOrder(order);

            order.Id = tradeorder.Id;
            runningOrders.Add(order);

            await Clients.Caller.SendAsync("ReceiveList", await tradeOrderHelper.GetTradeOrders()).ConfigureAwait(false);

            await Task.Run(async ()
                => await orderWork.StartWork(Clients, this, order, order.TokenSource.Token)
                // what to do after task is done

                ).ConfigureAwait(false);
        }
        public async Task GetInstruments()
        {
            await Clients.Caller.SendAsync("ReceiveInstruments", await instrumentHelper.GetTradeInstruments());
        }

        public async Task GetOrders()
        {
            await Clients.Caller.SendAsync("ReceiveList", await tradeOrderHelper.GetTradeOrders());
        }

        public async Task GetLogList()
        {
            await Clients.Caller.SendAsync("ReceiveLogs", await tradeLogHelper.GetTradeLogs());
        }

        public async Task StopOrderWork(int id)
        {
            if(runningOrders.Count > 0)
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
        }
    }
}