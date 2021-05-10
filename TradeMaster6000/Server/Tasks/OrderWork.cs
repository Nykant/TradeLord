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
    public class OrderWork
    {
        private readonly ILogger<OrderWork> logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private IHubCallerClients clients;
        private static TickHub _tickhub;
        private readonly IKiteService kiteService;
        private Kite kite;
        private List<KiteConnect.Tick> ticks;
        private readonly IConfiguration configuration;
        private string orderId_e;
        private string orderId_t;
        private string orderId_s;
        private decimal low = 0;
        private decimal target = 0;
        private int zonewidth;
        private int quantity;
        private bool orderFilling;

        public OrderWork(ILogger<OrderWork> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService)
        {
            this.logger = logger;
            this.configuration = configuration;
            _contextAccessor = contextAccessor;
            this.kiteService = kiteService;
            ticks = new List<KiteConnect.Tick>();
        }

        // do the work
        public void StartWork(IHubCallerClients clients, TickHub tickHub, TradeOrder order, CancellationToken cancellationToken)
        {
            _tickhub = tickHub;
            kite = kiteService.GetKite();
            this.clients = clients;

            // new ticker instance 
            Ticker ticker = new Ticker(configuration.GetValue<string>("APIKey"), _contextAccessor.HttpContext.Session.Get<string>(configuration.GetValue<string>("AccessToken")));

            // ticker event handlers
            ticker.OnTick += onTick;
            ticker.OnOrderUpdate += OnOrderUpdate;
            ticker.OnNoReconnect += OnNoReconnect;
            ticker.OnError += OnError;
            ticker.OnReconnect += OnReconnect;
            ticker.OnClose += OnClose;
            ticker.OnConnect += OnConnect;

            // set ticker settings
            ticker.EnableReconnect(Interval: 5, Retries: 50);
            ticker.Connect();
            ticker.Subscribe(Tokens: new UInt32[] { order.Instrument.Id });
            ticker.SetMode(Tokens: new UInt32[] { order.Instrument.Id }, Mode: Constants.MODE_FULL);

            // calculate zonewidth and quantity
            zonewidth = (int)order.Entry - order.StopLoss;
            quantity = order.Risk / zonewidth;

            _tickhub.AddLog($"log: order with id:{order.Id} starting...");

            // place entry limit order
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

            _tickhub.AddLog($"log: order placed... {DateTime.Now}");

            // get order id from place order response
            response.TryGetValue("data", out dynamic value);
            Dictionary<string, dynamic> data = value;
            data.TryGetValue("order_id", out dynamic value1);
            orderId_e = value1;

            bool tickerOpen = false;
            orderFilling = false;
            bool isMarketOpen = false;

            // while order is not stopped in app on orders page.
            while (!cancellationToken.IsCancellationRequested)
            {
                // while ticker is sleeping
                while (!tickerOpen)
                {
                    // check time once in a while, to figure out if it is time to wake up and go to work.
                    DateTime GST = DateTime.Now;
                    DateTime IST = GST.AddHours(5).AddMinutes(30);
                    // if clock is 8:59 or 9 its time to get up and start the day!
                    if (IST.Hour == 8 && IST.Minute == 59 || IST.Hour == 9)
                    {
                        _tickhub.AddLog($"log: pre market opening... market: {IST} server: {DateTime.Now}");
                        tickerOpen = true;
                    }
                    // if it is less than 7 we can sleep for a while longer :)
                    else if (IST.Hour <= 7)
                    {
                        _tickhub.AddLog($"log: market still closed so sleeping for an hour... market: {IST} server: {DateTime.Now}");
                        Thread.Sleep(3600000);
                    }
                    // if it is 8:58 we can snooze for extra suffering...
                    else if (IST.Hour == 8 && IST.Minute <= 58)
                    {
                        _tickhub.AddLog($"log: market still closed but soon opening... snoozing for a min... market: {IST} server: {DateTime.Now}");
                        Thread.Sleep(60000);
                    }
                }

                // while order is not filling
                while (!orderFilling)
                {
                    // check orders filled quantity to see if it started filling
                    var orderHistory = kite.GetOrderHistory(orderId_e);
                    if (orderHistory[orderHistory.Count - 1].FilledQuantity > 0)
                    {
                        _tickhub.AddLog($"log: order filling... {DateTime.Now}");
                        orderFilling = true;
                    }
                    Thread.Sleep(500);
                }
                
                // while pre market is open, but not yet the real deal
                while (!isMarketOpen)
                {
                    DateTime GMT = DateTime.Now;
                    DateTime IST = GMT.AddHours(5).AddMinutes(30);
                    if (IST.Hour == 9 && IST.Minute == 15)
                    {
                        _tickhub.AddLog($"log: market open... market: {IST} server: {DateTime.Now}");
                        isMarketOpen = true;
                    }
                    Thread.Sleep(500);
                }

                // market is open! lets place 2 orders simultaneously shall we? (find the functions below this one)
                Parallel.Invoke(() => PlaceTarget(order), () => PlaceStopLoss(order));

                bool hit = false;
                // rest of the time while order is running, check if target or slm is getting hit
                while (!cancellationToken.IsCancellationRequested)
                {
                    var orderHistoryT = kite.GetOrderHistory(orderId_t);
                    var orderHistoryS = kite.GetOrderHistory(orderId_s);

                    if (!hit)
                    {
                        if (orderHistoryS[orderHistoryS.Count - 1].FilledQuantity > 0)
                        {
                            kite.CancelOrder(orderId_t, "regular");
                            hit = true;
                        }

                        if (orderHistoryT[orderHistoryS.Count - 1].FilledQuantity > 0)
                        {
                            kite.CancelOrder(orderId_s, "regular");
                            hit = true;
                        }
                    }

                    Thread.Sleep(3000);
                }
            }

            // gently ending the order, in app
            var orderHistoryE = kite.GetOrderHistory(orderId_e);
            var orderHistoryT2 = kite.GetOrderHistory(orderId_t);
            var orderHistoryS2 = kite.GetOrderHistory(orderId_s);

            if(orderHistoryE[orderHistoryE.Count - 1].FilledQuantity == 0)
            {
                kite.CancelOrder(orderId_e, "amo");
            }
            if (orderHistoryT2[orderHistoryT2.Count - 1].FilledQuantity == 0)
            {
                kite.CancelOrder(orderId_t, "regular");
            }
            if (orderHistoryS2[orderHistoryS2.Count - 1].FilledQuantity == 0)
            {
                kite.CancelOrder(orderId_s, "regular");
            }

            ticker.UnSubscribe(new[] { order.Instrument.Id });
            ticker.DisableReconnect();
            ticker.Close();
            _tickhub.AddLog($"log: order with id:{order.Id} stopped...");
        }

        // place target
        private void PlaceTarget(TradeOrder order)
        {
            _tickhub.AddLog($"log: placing target... {DateTime.Now}");

            var orderHistoryE = kite.GetOrderHistory(orderId_e);
            target = (order.RxR * zonewidth) + orderHistoryE[orderHistoryE.Count - 1].AveragePrice;
            decimal proximity = (target - orderHistoryE[orderHistoryE.Count - 1].AveragePrice) * (decimal)0.8;

            Dictionary<string, dynamic> orderReponse = kite.PlaceOrder(
                 Exchange: order.Instrument.Exchange,
                 TradingSymbol: order.Instrument.TradingSymbol,
                 TransactionType: Constants.TRANSACTION_TYPE_SELL,
                 Quantity: quantity,
                 Price: target,
                 Product: Constants.PRODUCT_MIS,
                 OrderType: Constants.ORDER_TYPE_LIMIT,
                 Validity: Constants.VALIDITY_DAY,
                 Variety: Constants.VARIETY_REGULAR
             );

            orderReponse.TryGetValue("data", out dynamic value);
            Dictionary<string, dynamic> data = value;
            data.TryGetValue("order_id", out dynamic value1);
            orderId_t = value1;

            _tickhub.AddLog($"log: target: {orderId_t} placed... with target: {target} ... {DateTime.Now}");

            _tickhub.AddLog($"log: checking proximity: {proximity} of order: {orderId_e} ... {DateTime.Now}");
            bool checkingProximity = true;
            // check proximity until order is fully filled. to modify the target order in case tick price is closing in on it.
            while (checkingProximity)
            {
                var orderHistory = kite.GetOrderHistory(orderId_e);
                if (ticks[ticks.Count - 1].High >= proximity)
                {
                    if (orderHistory[orderHistory.Count - 1].FilledQuantity < quantity)
                    {
                        var orderHistoryT = kite.GetOrderHistory(orderId_t);
                        if (orderHistoryT[orderHistoryT.Count - 1].FilledQuantity != orderHistory[orderHistory.Count - 1].Quantity)
                        {
                            kite.ModifyOrder(
                                orderId_t,
                                null,
                                order.Instrument.Exchange,
                                order.TradeSymbol.ToString(),
                                Constants.TRANSACTION_TYPE_SELL,
                                orderHistory[orderHistory.Count - 1].FilledQuantity.ToString(),
                                target,
                                Constants.PRODUCT_MIS,
                                Constants.ORDER_TYPE_LIMIT,
                                Constants.VALIDITY_DAY,
                                null,
                                null,
                                Constants.VARIETY_REGULAR
                            );
                            _tickhub.AddLog($"log: target: {orderId_t} quantity modified {orderHistory[orderHistory.Count - 1].FilledQuantity}... {DateTime.Now}");
                        }
                    }
                }
                if (orderHistory[orderHistory.Count - 1].FilledQuantity == quantity)
                {
                    _tickhub.AddLog($"log: whole entry order filled... {DateTime.Now}");
                    checkingProximity = false;
                }
                Thread.Sleep(500);
            }
        }

        // place stop loss
        private void PlaceStopLoss(TradeOrder order)
        {
            _tickhub.AddLog($"log: placing stop loss... {DateTime.Now}");

            var orderHistory = kite.GetOrderHistory(orderId_e);

            // if entry order average price, is less than the stoploss input, then sleep for a minute for ticks to come in, so we can set the stoploss as the low. 
            if(orderHistory[orderHistory.Count - 1].AveragePrice < order.StopLoss)
            {
                _tickhub.AddLog($"log: average price is less than stop loss... waiting for 1 minute for candle... {DateTime.Now}");
                Thread.Sleep(55000);

                _tickhub.AddLog($"log: finding low... {DateTime.Now}");

                _tickhub.AddLog($"test tick timestamp: {(DateTime)ticks[2].Timestamp}... {DateTime.Now}");
                _tickhub.AddLog($"test tick timestamp: {(DateTime)ticks[ticks.Count - 1].Timestamp}... {DateTime.Now}");

                foreach (var tick in ticks)
                {
                    DateTime timestamp = (DateTime)tick.Timestamp;
                    if (timestamp.Hour == 9 && timestamp.Minute == 15)
                    {
                        if (low == 0 || low > tick.Low)
                        {
                            low = tick.Low;
                        }
                    }
                }
                if(low == 0)
                {
                    low = ticks[ticks.Count - 1].Low;
                }
                _tickhub.AddLog($"log: low found: {low} ... {DateTime.Now}");

                Dictionary<string, dynamic> response = kite.PlaceOrder(
                     Exchange: order.Instrument.Exchange,
                     TradingSymbol: order.Instrument.TradingSymbol,
                     TransactionType: Constants.TRANSACTION_TYPE_SELL,
                     Quantity: quantity,
                     TriggerPrice: low,
                     Product: Constants.PRODUCT_MIS,
                     OrderType: Constants.ORDER_TYPE_SLM,
                     Validity: Constants.VALIDITY_DAY,
                     Variety: Constants.VARIETY_REGULAR
                );

                response.TryGetValue("data", out dynamic value);
                Dictionary<string, dynamic> date = value;
                date.TryGetValue("order_id", out dynamic value1);
                orderId_s = value1;

                _tickhub.AddLog($"log: SLM order: {orderId_s} placed... {DateTime.Now}");
            }

            // else just place the slm order with stoploss input
            else
            {
                Dictionary<string, dynamic> response = kite.PlaceOrder(
                     Exchange: order.Instrument.Exchange,
                     TradingSymbol: order.Instrument.TradingSymbol,
                     TransactionType: Constants.TRANSACTION_TYPE_SELL,
                     Quantity: quantity,
                     TriggerPrice: order.StopLoss,
                     Product: Constants.PRODUCT_MIS,
                     OrderType: Constants.ORDER_TYPE_SLM,
                     Validity: Constants.VALIDITY_DAY,
                     Variety: Constants.VARIETY_REGULAR
                );

                response.TryGetValue("data", out dynamic value);
                Dictionary<string, dynamic> date = value;
                date.TryGetValue("order_id", out dynamic value1);
                orderId_s = value1;

                _tickhub.AddLog($"log: SLM order: {orderId_s} placed... {DateTime.Now}");
            }
        }
        
        private void onTick(KiteConnect.Tick tickData)
        {
            ticks.Add(tickData);
        }
        private void OnOrderUpdate(Order orderData)
        {
            TickHub.orderDataList.Add(orderData);
            _tickhub.AddLog($"log: order update: {orderData.StatusMessage} - at {DateTime.Now}");
        }
        private void OnError(string message)
        {
            _tickhub.AddLog($"log: error message: {message} - at {DateTime.Now}");
        }
        private void OnClose()
        {
            _tickhub.AddLog($"log: ticker connection closed - at {DateTime.Now}");
        }
        private void OnReconnect()
        {
            _tickhub.AddLog($"log: ticker connection reconnected - at {DateTime.Now}");
        }
        private void OnNoReconnect()
        {
            _tickhub.AddLog($"log: ticker connection reconnected - at {DateTime.Now}");
        }
        private void OnConnect()
        {
            _tickhub.AddLog($"log: ticker connection connected - at {DateTime.Now}");
        }
    }
}
