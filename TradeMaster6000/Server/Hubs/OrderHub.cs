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
using TradeMaster6000.Server.Services;
using TradeMaster6000.Server.Tasks;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Hubs
{

    public class OrderHub : Hub
    {
        private static List<TradeOrder> orderList = new List<TradeOrder>();
        private static List<string> logList = new List<string>();
        private static List<Order> orderDataList = new List<Order>();
        private readonly ILogger<OrderWork> logger;
        private readonly IKiteService kiteService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IInstrumentService instrumentService;
        private static int OrderCount { get; set; }

        public OrderHub(ILogger<OrderWork> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService, IInstrumentService instrumentService)
        {
            this.logger = logger;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            this.kiteService = kiteService;
            this.instrumentService = instrumentService;
        }

        // start order work with inputs from user 
        public async Task StartOrderWork(TradeOrder order)
        {
            // new instance of OrderWork class
            OrderWork orderWork = new OrderWork(logger, _configuration, _contextAccessor, kiteService);
            order.TokenSource = new CancellationTokenSource();
            order.Id = OrderCount;

            var instruments = instrumentService.GetInstruments();
            foreach(var instrument in instruments)
            {
                if(instrument.TradingSymbol == order.TradingSymbol)
                {
                    order.Instrument = instrument;
                }
            }

            // add the OrderWork instance to a list (which stays for application lifetime)
            orderList.Add(order);
            OrderCount = OrderCount + 1;

            // send list to client
            await Clients.Caller.SendAsync("ReceiveList", orderList);

            // start work in that instance which runs on the thread pool
            await Task.Run( () => 
            {
                orderWork.StartWork(Clients, this, order, order.TokenSource.Token);
            });
        }
        public async Task GetInstruments()
        {
            var instruments = instrumentService.GetInstruments();
            await Clients.Caller.SendAsync("ReceiveInstruments", instruments);
        }

        public async Task GetOrders()
        {
            await Clients.Caller.SendAsync("ReceiveList", orderList);
        }
        public async Task GetOrderData()
        {
            await Clients.Caller.SendAsync("ReceiveOrderData", orderDataList);
        }
        public async Task GetLogList()
        {
            await Clients.Caller.SendAsync("ReceiveLogs", logList);
        }
        public void AddLog(string log)
        {
            logList.Add(log);
        }
        public void AddOrderUpdate(Order orderData)
        {
            orderDataList.Add(orderData);
        }


        public async Task StopOrderWork(int id)
        {
            if(orderList.Count > 0)
            {
                for (int i = 0; i < orderList.Count; i++)
                {
                    if (orderList[i].Id == id)
                    {
                        orderList[i].TokenSource.Cancel();
                        orderList.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}