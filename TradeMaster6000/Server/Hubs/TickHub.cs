﻿using KiteConnect;
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

    public class TickHub : Hub
    {
        private static List<TradeOrder> orderList = new List<TradeOrder>();
        private static List<string> logList = new List<string>();
        public static List<Order> orderDataList;
        private readonly ILogger<OrderWork> logger;
        private readonly IKiteService kiteService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IInstrumentService instrumentService;
        private static int OrderCount { get; set; }

        public TickHub(ILogger<OrderWork> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService, IInstrumentService instrumentService)
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

            // set order instrument with tradingsymbol input from user
            foreach(var instrument in instrumentService.GetInstruments())
            {
                if(instrument.TradingSymbol == order.TradeSymbol.ToString())
                {
                    order.Instrument = instrument;
                }
            }

            // add the OrderWork instance to a list (which stays for application lifetime)
            orderList.Add(order);
            OrderCount = OrderCount + 1;

            await Clients.Caller.SendAsync("ReceiveList", orderList);

            // get available threads and log amount
            ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
            AddLog($"log: maximum number of threads available: {workerThreads}");

            // start work in that instance which runs 
            await Task.Run( () => 
            {
                orderWork.StartWork(Clients, this, order, order.TokenSource.Token);
                AddLog($"log: Task {Task.CurrentId} has finished");
            });
        }
        public async Task GetInstruments()
        {
            await Clients.Caller.SendAsync("ReceiveInstruments", instrumentService.GetInstruments());
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