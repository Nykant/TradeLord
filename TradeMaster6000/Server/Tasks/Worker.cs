using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Extensions;
using TradeMaster6000.Server.Hubs;
using TradeMaster6000.Server.Models;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Tasks
{
    public class Worker : IWorker
    {
        private readonly ILogger<Worker> logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private IHubCallerClients clients;
        private static TickHub _tickhub;
        private readonly IKiteService kiteService;
        private static Kite kite;
        private static List<KiteConnect.Tick> ticks;
        private readonly IConfiguration configuration;
        private static string orderId_e;
        private static string orderId_t;
        private static string orderId_s;
        private static DateTime? orderFilledTime;
        private static decimal entry;
        private static decimal high = 0;
        private static decimal low = 0;
        private static decimal target = 0;
        private static int zonewidth;
        private static int quantity;
        private static decimal open;
        private static decimal close;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService)
        {
            this.logger = logger;
            this.configuration = configuration;
            _contextAccessor = contextAccessor;
            this.kiteService = kiteService;
            ticks = new List<KiteConnect.Tick>();
        }


        public async Task StartTicker(IHubCallerClients clients, TickHub tickHub, TradeOrder order, CancellationToken cancellationToken)
        {
            _tickhub = tickHub;

            kite = kiteService.GetKite();

            Ticker ticker = new Ticker(configuration.GetValue<string>("APIKey"), _contextAccessor.HttpContext.Session.Get<string>(configuration.GetValue<string>("AccessToken")));

            this.clients = clients;
            ticker.OnTick += onTick;
            //ticker.OnOrderUpdate += OnOrderUpdate;
            //ticker.OnReconnect += onReconnect;
            //ticker.OnNoReconnect += oNoReconnect;
            //ticker.OnError += onError;
            //ticker.OnClose += onClose;
            //ticker.OnConnect += onConnect;

            ticker.EnableReconnect(Interval: 5, Retries: 50);
            ticker.Connect();

            ticker.Subscribe(Tokens: new UInt32[] { order.Instrument.Id });
            ticker.SetMode(Tokens: new UInt32[] { order.Instrument.Id }, Mode: Constants.MODE_FULL);

            await _tickhub.AddLog($"log: order with id:{order.Id} starting...");
            //await clients.Caller.SendAsync("ReceiveLog", $"log: order with id:{order.Id} starting...");
            
            zonewidth = (int)order.Entry - order.StopLoss;
            quantity = order.Risk / zonewidth;

            Dictionary<string, dynamic> response = kite.PlaceOrder(
                 Exchange: order.Instrument.Exchange,
                 TradingSymbol: order.Instrument.TradingSymbol,
                 TransactionType: order.TransactionType.ToString(),
                 Quantity: quantity,
                 Price: order.Entry,
                 Product: order.Product.ToString(),
                 OrderType: order.OrderType.ToString(),
                 Validity: Constants.VALIDITY_DAY,
                 Variety: order.Variety.ToString()
             );

            await _tickhub.AddLog($"log: order placed... {DateTime.Now}");

            response.TryGetValue("data", out dynamic value);
            Dictionary<string, dynamic> data = value;
            data.TryGetValue("order_id", out dynamic value2);
            orderId_e = value2;

            bool tickerOpen = false;
            bool orderFilling = false;
            bool findingHighLow = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                var orderHistory = kite.GetOrderHistory(orderId_e);

                while (!tickerOpen)
                {
                    DateTime GST = DateTime.Now;
                    DateTime IST = GST.AddHours(5).AddMinutes(30);
                    if(IST.Hour == 8 && IST.Minute == 59)
                    {
                        await _tickhub.AddLog($"log: ticker open... market: {IST} server: {DateTime.Now}");
                        tickerOpen = true;
                    }
                    Thread.Sleep(500);
                }
                while (orderFilling == false)
                {
                    var orderHistory2 = kite.GetOrderHistory(orderId_e);
                    if (orderHistory2[orderHistory2.Count - 1].FilledQuantity > 0)
                    {
                        entry = orderHistory2[orderHistory2.Count - 1].AveragePrice;
                        orderFilledTime = orderHistory2[orderHistory2.Count - 1].OrderTimestamp;
                        orderFilling = true;
                    }
                    Thread.Sleep(500);
                }
                while (orderFilling == true)
                {
                    await _tickhub.AddLog($"log: order filling... {DateTime.Now}");

                    bool isTime15 = false;
                    while (!isTime15)
                    {
                        DateTime GMT = DateTime.Now;
                        DateTime IST = GMT.AddHours(5).AddMinutes(30);
                        if (IST.Hour == 9 && IST.Minute == 15)
                        {
                            await _tickhub.AddLog($"log: ticker open... market: {IST} server: {DateTime.Now}");
                            isTime15 = true;
                        }
                        if(IST.Hour == 9 && IST.Minute == 1 && IST.Second == 0 || IST.Hour == 9 && IST.Minute == 1 && IST.Second == 1 || IST.Hour == 9 && IST.Minute == 1 && IST.Second == 2)
                        {
                            high = ticks[ticks.Count - 1].High;
                            low = ticks[ticks.Count - 1].Low;
                            open = ticks[ticks.Count - 1].Open;
                            close = ticks[ticks.Count - 1].Close;
                        }
                    }

                    Task.WaitAll(PlaceTarget(order), PlaceStopLoss());
                }
                Thread.Sleep(1000);
            }

            ticker.Close();
            await _tickhub.AddLog($"log: order with id:{order.Id} stopped...");
            //await clients.Caller.SendAsync("ReceiveLog", $"log: order with id:{order.Id} stopped...");
        }

        private async Task PlaceTarget(TradeOrder order)
        {
            await _tickhub.AddLog($"log: placing target... {DateTime.Now}");
            target = (order.RxR * zonewidth) + entry;

            bool placingTarget = true;
            while (placingTarget)
            {
                Dictionary<string, dynamic> orderReponse = kite.PlaceOrder(
                     Exchange: order.Instrument.Exchange,
                     TradingSymbol: order.Instrument.TradingSymbol,
                     TransactionType: order.TransactionType.ToString(),
                     Quantity: quantity,
                     Price: target,
                     Product: order.Product.ToString(),
                     OrderType: order.OrderType.ToString(),
                     Validity: Constants.VALIDITY_DAY,
                     Variety: order.Variety.ToString()
                 );

                orderReponse.TryGetValue("data", out dynamic value3);
                Dictionary<string, dynamic> date2 = value3;
                date2.TryGetValue("order_id", out dynamic value4);
                orderId_t = value4;

                bool checkingProximity = true;
                decimal proximity = (target - entry) * (decimal)0.8;
                while (checkingProximity)
                {
                    var orderHistory3 = kite.GetOrderHistory(orderId_e);
                    if (ticks[ticks.Count - 1].High >= (proximity))
                    {
                        if (orderHistory3[orderHistory3.Count - 1].FilledQuantity < quantity)
                        {
                            kite.ModifyOrder(
                                orderId_t,
                                null,
                                order.Instrument.Exchange,
                                order.TradeSymbol.ToString(),
                                Constants.TRANSACTION_TYPE_SELL,
                                orderHistory3[orderHistory3.Count - 1].FilledQuantity.ToString(),
                                target,
                                order.Product.ToString(),
                                order.OrderType.ToString(),
                                Constants.VALIDITY_DAY,
                                null,
                                null,
                                order.Variety.ToString());
                        }
                        checkingProximity = false;
                    }
                }
            }
        }
        private async Task PlaceStopLoss()
        {
            //await _tickhub.AddLog($"log: placing stop loss... {DateTime.Now}");

            //var start = (DateTime)ticks[2].Timestamp;
            //var end = (DateTime)ticks[2].Timestamp;
            //end.AddMinutes(1);

            //await _tickhub.AddLog($"log: order filled at... {entry} - price");
            //await _tickhub.AddLog($"log: order filled at... {orderFilledTime} - kite time");
            //await _tickhub.AddLog($"log: order filled at... {DateTime.Now} - server time");
            //findingHighLow = true;
            //while (findingHighLow)
            //{
            //    if (DateTime.Compare(end, (DateTime)ticks[ticks.Count - 1].Timestamp) <= 0)
            //    {
            //        findingHighLow = false;
            //    }
            //    if (ticks[ticks.Count - 1].Low < low)
            //    {
            //        low = ticks[ticks.Count - 1].Low;
            //    }
            //    if (ticks[ticks.Count - 1].High > high)
            //    {
            //        high = ticks[ticks.Count - 1].High;
            //    }
            //}
        }
        //maybe the start ticker needs a while loop or do ticker.close after source.cancel, maybe stopticker has source as parameter. maybe start ticker has source parameter
        //public async Task StopTicker()
        //{


        //    logger.LogInformation("ticker closed");
        //}
        private static void onTick(KiteConnect.Tick TickData)
        {
            ticks.Add(TickData);
            //await _tickhub.SendTick(TickData.LastPrice, clients);
            //await clients.All.SendAsync("ReceiveTick", TickData.LastPrice);
        }
        //private static void OnOrderUpdate(Order OrderData)
        //{
        //    orderDataList.Add(OrderData);
        //}
    }

    public interface IWorker
    {
        Task StartTicker(IHubCallerClients clients, TickHub tickHub, TradeOrder order, CancellationToken cancellationToken);
        //Task StopTicker();
    }
}
