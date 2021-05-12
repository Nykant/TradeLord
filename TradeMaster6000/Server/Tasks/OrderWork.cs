﻿using KiteConnect;
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
        private string orderId_s_amo;
        private string orderId_s;
        private decimal low = 0;
        private decimal target = 0;
        private decimal zonewidth;
        private int quantity;
        private bool orderFilling;
        private int orderId;
        private bool isOrderFilled;
        private bool isSL_amoCancelled;
        private bool squareOff;
        private bool regularSLMplaced;
        private string orderId_m;
        private bool targetplaced;
        private bool hit;
        private string exitTransactionType;
        private bool rejected;

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
            if(_tickhub == null)
            {
                _tickhub = tickHub;
            }
            if(order.TransactionType.ToString() == "BUY")
            {
                exitTransactionType = "SELL";
            }
            else
            {
                exitTransactionType = "BUY";
            }

            string variety = null;
            DateTime GST1 = DateTime.Now;
            DateTime IST1 = GST1.AddHours(5).AddMinutes(30);
            DateTime opening1 = new DateTime(IST1.Year, IST1.Month, IST1.Day, 9, 0, 0);
            DateTime closing1 = opening1.AddHours(6).AddMinutes(30);

            if (DateTime.Compare(IST1, opening1) < 0)
            {
                variety = "amo";
            }
            else if(DateTime.Compare(IST1, opening1) >= 0)
            {
                variety = "regular";
            }

            if(DateTime.Compare(IST1, closing1) >= 0)
            {
                variety = "amo";
            }

            orderId = order.Id;
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
            zonewidth = order.Entry - order.StopLoss;
            quantity = (int)order.Risk / (int)zonewidth;

            _tickhub.AddLog($"{orderId} log: order starting...");

            // place entry limit order
            Dictionary<string, dynamic> response = kite.PlaceOrder(
                 Exchange: order.Instrument.Exchange,
                 TradingSymbol: order.Instrument.TradingSymbol,
                 TransactionType: order.TransactionType.ToString(),
                 Quantity: quantity,
                 Price: order.Entry,
                 Product: Constants.PRODUCT_MIS,
                 OrderType: Constants.ORDER_TYPE_LIMIT,
                 Validity: Constants.VALIDITY_DAY,
                 Variety: variety
             );

            _tickhub.AddLog($"{orderId} log: entry order placed... {DateTime.Now}");

            // get order id from place order response
            response.TryGetValue("data", out dynamic value);
            Dictionary<string, dynamic> data = value;
            data.TryGetValue("order_id", out dynamic value1);
            orderId_e = value1;

            // place slm amo order
            Dictionary<string, dynamic> responseS = kite.PlaceOrder(
                 Exchange: order.Instrument.Exchange,
                 TradingSymbol: order.Instrument.TradingSymbol,
                 TransactionType: exitTransactionType,
                 Quantity: quantity,
                 TriggerPrice: order.StopLoss,
                 Product: Constants.PRODUCT_MIS,
                 OrderType: Constants.ORDER_TYPE_SLM,
                 Validity: Constants.VALIDITY_DAY,
                 Variety: variety
            );

            // set id
            responseS.TryGetValue("data", out dynamic valueS);
            Dictionary<string, dynamic> dateS = valueS;
            dateS.TryGetValue("order_id", out dynamic value1S);
            orderId_s_amo = value1S;

            _tickhub.AddLog($"{orderId} log: SLM order: {orderId_s} placed... {DateTime.Now}");

            bool tickerOpen = false;
            orderFilling = false;
            bool isMarketOpen = false;
            isSL_amoCancelled = false;
            squareOff = false;
            hit = false;
            rejected = false;

            var orderHistoryQ = kite.GetOrderHistory(orderId_e);
            var orderHistoryA = kite.GetOrderHistory(orderId_s_amo);
            if (orderHistoryQ[orderHistoryQ.Count - 1].Status == "REJECTED")
            {
                _tickhub.AddLog($"{orderId} log: entry order rejected... {DateTime.Now}");
                if (orderHistoryA[orderHistoryA.Count - 1].Status != "REJECTED")
                {
                    _tickhub.AddLog($"{orderId} log: slm order cancelled... {DateTime.Now}");
                    kite.CancelOrder(orderId_s_amo, variety);
                }
                rejected = true;
                goto Ending;
            }

            if(orderHistoryA[orderHistoryA.Count - 1].Status == "REJECTED")
            {
                _tickhub.AddLog($"{orderId} log: slm order rejected... {DateTime.Now}");
                if (orderHistoryQ[orderHistoryQ.Count - 1].Status != "REJECTED")
                {
                    _tickhub.AddLog($"{orderId} log: entry order cancelled... {DateTime.Now}");
                    kite.CancelOrder(orderId_e, variety);
                }
                rejected = true;
                goto Ending;
            }

            // while order is not stopped in app on orders page.
            while (!cancellationToken.IsCancellationRequested)
            {
                // while ticker is sleeping
                while (!tickerOpen)
                {
                    // check time once in a while, to figure out if it is time to wake up and go to work.
                    DateTime GST = DateTime.Now;
                    DateTime IST = GST.AddHours(5).AddMinutes(30);
                    DateTime opening = new DateTime(IST.Year, IST.Month, IST.Day, 9, 00, 00);
                    DateTime closing = opening.AddHours(6).AddMinutes(45);
                    // if clock is 9 its time to get up and start the day!
                    if (DateTime.Compare(IST, opening) >= 0)
                    {
                        if(DateTime.Compare(IST, closing) < 0)
                        {
                            _tickhub.AddLog($"{orderId} log: pre market opening... market: {IST} server: {DateTime.Now}");
                            tickerOpen = true;
                        }
                    }
                    // if it is less than 7 we can sleep for a while longer :)
                    else if (IST.Hour <= 7 || IST.Hour >= 16)
                    {
                        _tickhub.AddLog($"{orderId} log: market still closed so sleeping for an hour... market: {IST} server: {DateTime.Now}");
                        Thread.Sleep(3600000);
                    }
                    // if it is 8:59 we can snooze for extra suffering...
                    else if (IST.Hour == 8 && IST.Minute <= 59)
                    {
                        _tickhub.AddLog($"{orderId} log: market still closed but soon opening... snoozing for a min... market: {IST} server: {DateTime.Now}");
                        Thread.Sleep(60000);
                    }
                }

                _tickhub.AddLog($"{orderId} log: checking if entry order is filling... {DateTime.Now}");
                // while order is not filling
                while (!orderFilling)
                {
                    // check orders filled quantity to see if it started filling
                    var orderHistory = kite.GetOrderHistory(orderId_e);
                    if (orderHistory[orderHistory.Count - 1].FilledQuantity > 0)
                    {
                        _tickhub.AddLog($"{orderId} log: order filling... {DateTime.Now}");
                        if(orderHistory[orderHistory.Count - 1].FilledQuantity == quantity)
                        {
                            isOrderFilled = true;
                        }
                        else
                        {
                            isOrderFilled = false;
                        }

                        orderFilling = true;
                    }
                    Thread.Sleep(1000);
                }
                
                // while pre market is open, but not yet the real deal
                while (!isMarketOpen)
                {
                    DateTime GMT = DateTime.Now;
                    DateTime IST = GMT.AddHours(5).AddMinutes(30);
                    DateTime opening = new DateTime(IST.Year, IST.Month, IST.Day, 9, 15, 00);
                    DateTime closing = opening.AddHours(6).AddMinutes(45);
                    var orderHistory = kite.GetOrderHistory(orderId_e);
                    if (DateTime.Compare(IST, opening) >= 0)
                    {
                        if(DateTime.Compare(IST, closing) < 0)
                        {
                            _tickhub.AddLog($"{orderId} log: market open... market: {IST} server: {DateTime.Now}");
                            isMarketOpen = true;
                        }
                    }

                    if (!isOrderFilled)
                    {
                        if (orderHistory[orderHistory.Count - 1].FilledQuantity == quantity)
                        {
                            _tickhub.AddLog($"{orderId} log: entry order filled...");

                            if (orderHistory[orderHistory.Count - 1].AveragePrice < order.StopLoss)
                            {
                                try
                                {
                                    kite.CancelOrder(orderId_s_amo);
                                    _tickhub.AddLog($"{orderId} log: slm amo order cancelled...");
                                    isSL_amoCancelled = true;
                                }
                                catch (KiteException e)
                                {
                                    _tickhub.AddLog($"{orderId} error: {e.Message}...");
                                }
                            }
                            isOrderFilled = true;
                        }
                    }
                    else if (isOrderFilled)
                    {
                        if (orderHistory[orderHistory.Count - 1].AveragePrice < order.StopLoss)
                        {
                            try
                            {
                                kite.CancelOrder(orderId_s_amo);
                                _tickhub.AddLog($"{orderId} log: slm amo order cancelled...");
                                isSL_amoCancelled = true;
                            }
                            catch (KiteException e)
                            {
                                _tickhub.AddLog($"{orderId} error: {e.Message}...");
                            }
                        }
                    }

                    if(IST.Hour == 9 && IST.Minute == 14)
                    {
                        if (!isSL_amoCancelled)
                        {
                            if (orderHistory[orderHistory.Count - 1].AveragePrice < order.StopLoss)
                            {
                                kite.CancelOrder(orderId_s_amo);
                                _tickhub.AddLog($"{orderId} log: slm amo order cancelled...");
                                isSL_amoCancelled = true;
                            }
                        }
                    }
                    Thread.Sleep(500);
                }

                // market is open! lets place 2 orders simultaneously shall we? (find the functions below this one)
                Parallel.Invoke(() => PlaceTarget(order), () => PlaceStopLoss(order));

                // monitoring the orders 
                List<Order> orderHistoryS = new List<Order>();
                bool crying = false;
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (squareOff)
                    {
                        _tickhub.AddLog($"{orderId} log: squaring off :'( it is going to be a rough battle, wish me luck guys...");
                        Dictionary<string, dynamic> placeOrderResponse = kite.PlaceOrder(
                             Exchange: order.Instrument.Exchange,
                             TradingSymbol: order.Instrument.TradingSymbol,
                             TransactionType: exitTransactionType,
                             Quantity: quantity,
                             Product: Constants.PRODUCT_MIS,
                             OrderType: Constants.ORDER_TYPE_MARKET,
                             Validity: Constants.VALIDITY_DAY,
                             Variety: Constants.VARIETY_REGULAR
                        );

                        placeOrderResponse.TryGetValue("data", out dynamic valueMarket);
                        Dictionary<string, dynamic> dataMarket = valueMarket;
                        dataMarket.TryGetValue("order_id", out dynamic valueMarket2);
                        orderId_m = valueMarket2;

                        kite.CancelOrder(orderId_t);

                        crying = true;
                    }

                    while (crying)
                    {
                        _tickhub.AddLog($"{orderId} log: crying in the stop order waiting room...");
                        Thread.Sleep(3600000);
                    }

                    if (regularSLMplaced)
                    {
                        orderHistoryS = kite.GetOrderHistory(orderId_s);
                    }
                    else
                    {
                        orderHistoryS = kite.GetOrderHistory(orderId_s_amo);
                    }

                    var orderHistoryT = kite.GetOrderHistory(orderId_t);

                    if (!hit)
                    {
                        if (orderHistoryS[orderHistoryS.Count - 1].FilledQuantity > 0 || orderHistoryS[orderHistoryS.Count - 1].Status == "COMPLETE")
                        {
                            _tickhub.AddLog($"{orderId} log: stop loss hit!... filled quantity: {orderHistoryS[orderHistoryS.Count - 1].FilledQuantity} --- status: {orderHistoryS[orderHistoryS.Count - 1].Status}...");
                            kite.CancelOrder(orderId_t);
                            hit = true;
                        }

                        if (orderHistoryT[orderHistoryT.Count - 1].FilledQuantity > 0 || orderHistoryT[orderHistoryT.Count - 1].Status == "COMPLETE")
                        {
                            _tickhub.AddLog($"{orderId} log: target hit!... filled quantity: {orderHistoryT[orderHistoryT.Count - 1].FilledQuantity} --- status: {orderHistoryT[orderHistoryT.Count - 1].Status}...");
                            if (regularSLMplaced)
                            {
                                kite.CancelOrder(orderId_s);
                            }
                            else
                            {
                                kite.CancelOrder(orderId_s_amo);
                            }
                            hit = true;
                        }
                    }

                    while (hit)
                    {
                        _tickhub.AddLog($"{orderId} log: target or stop loss hit!... stop order waiting room...");
                        Thread.Sleep(3600000);
                    }

                    if(orderHistoryS[orderHistoryS.Count - 1].Status == "REJECTED")
                    {
                        _tickhub.AddLog($"{orderId} log: stop loss order REJECTED ...");
                        squareOff = true;
                    }

                    Thread.Sleep(500);
                }
            }

            // gracefully ending the order, in app
            var OHentry = kite.GetOrderHistory(orderId_e);
            var OHstoploss_amo = kite.GetOrderHistory(orderId_s_amo);
            List<Order> OHtarget = new List<Order>();
            if (targetplaced)
            {
                OHtarget = kite.GetOrderHistory(orderId_t);
            }
            List<Order> OHstoploss_regular = new List<Order>();
            if (regularSLMplaced)
            {
                OHstoploss_regular = kite.GetOrderHistory(orderId_s);
            }

            DateTime GMT3 = DateTime.Now;
            DateTime IST3 = GMT3.AddHours(5).AddMinutes(30);
            DateTime opening3 = new DateTime(IST3.Year, IST3.Month, IST3.Day, 9, 0, 0);
            DateTime closing3 = opening3.AddHours(6).AddMinutes(15);

            if(DateTime.Compare(IST3, opening3) < 0)
            {
                if(DateTime.Compare(IST3, closing3) > 0)
                {
                    variety = "amo";
                }
            }
            else
            {
                variety = "regular";
            }

            try
            {
                if (OHentry[OHentry.Count - 1].Status != "COMPLETE")
                {
                    kite.CancelOrder(orderId_e, variety);
                }

                if (!isSL_amoCancelled)
                {
                    if (OHstoploss_amo[OHstoploss_amo.Count - 1].Status != "COMPLETE")
                    {
                        kite.CancelOrder(orderId_s_amo, variety);
                    }
                }

                if (targetplaced)
                {
                    if (OHtarget[OHtarget.Count - 1].Status != "COMPLETE")
                    {
                        kite.CancelOrder(orderId_t);
                    }
                }
                if (regularSLMplaced)
                {
                    if (OHstoploss_regular[OHstoploss_regular.Count - 1].Status != "COMPLETE")
                    {
                        kite.CancelOrder(orderId_s);
                    }
                }
            }
            catch (Exception e)
            {
                _tickhub.AddLog($"{orderId} log: error cancelling orders: {e.Message}...");
            }

            // go to when rejected
            Ending:

            ticker.UnSubscribe(new[] { order.Instrument.Id });
            ticker.DisableReconnect();
            ticker.Close();
            _tickhub.AddLog($"{orderId} log: order stopped...");
        }

        // place target
        private void PlaceTarget(TradeOrder order)
        {
            _tickhub.AddLog($"{orderId} log: placing target... {DateTime.Now}");

            var orderHistoryE = kite.GetOrderHistory(orderId_e);
            target = (order.RxR * zonewidth) + orderHistoryE[orderHistoryE.Count - 1].AveragePrice;
            decimal proximity = ((target - orderHistoryE[orderHistoryE.Count - 1].AveragePrice) * (decimal)0.8)
                                        + orderHistoryE[orderHistoryE.Count - 1].AveragePrice;

            Dictionary<string, dynamic> orderReponse = kite.PlaceOrder(
                 Exchange: order.Instrument.Exchange,
                 TradingSymbol: order.Instrument.TradingSymbol,
                 TransactionType: exitTransactionType,
                 Quantity: quantity,
                 Price: target,
                 Product: Constants.PRODUCT_MIS,
                 OrderType: Constants.ORDER_TYPE_LIMIT,
                 Validity: Constants.VALIDITY_DAY,
                 Variety: Constants.VARIETY_REGULAR
             );

            targetplaced = true;

            orderReponse.TryGetValue("data", out dynamic value);
            Dictionary<string, dynamic> data = value;
            data.TryGetValue("order_id", out dynamic value1);
            orderId_t = value1;

            _tickhub.AddLog($"{orderId} log: target: {orderId_t} placed... with target: {target} ... {DateTime.Now}");

            _tickhub.AddLog($"{orderId} log: checking proximity: {proximity} of order: {orderId_e} ... {DateTime.Now}");
            bool checkingProximity = true;
            // check proximity until order is fully filled. to modify the target order in case tick price is closing in on it.
            while (checkingProximity)
            {
                var orderHistory = kite.GetOrderHistory(orderId_e);
                if (isOrderFilled || orderHistory[orderHistory.Count - 1].FilledQuantity == quantity)
                {
                    if(isOrderFilled == false)
                    {
                        _tickhub.AddLog($"{orderId} log: whole entry order filled... {DateTime.Now}");
                        isOrderFilled = true;
                    }
                    checkingProximity = false;
                }
                if (ticks[ticks.Count - 1].High >= proximity)
                {
                    if (orderHistory[orderHistory.Count - 1].FilledQuantity != quantity)
                    {
                        kite.ModifyOrder(
                            orderId_t,
                            Quantity: orderHistory[orderHistory.Count - 1].FilledQuantity.ToString()
                        );
                        _tickhub.AddLog($"{orderId} log: target: {orderId_t} - quantity modified {orderHistory[orderHistory.Count - 1].FilledQuantity}... {DateTime.Now}");
                    }
                }
                Thread.Sleep(500);
            }
        }

        // place stop loss
        private void PlaceStopLoss(TradeOrder order)
        {
            _tickhub.AddLog($"{orderId} log:  placing stop loss... {DateTime.Now}");

            // if entry order average price, is less than the stoploss input, then sleep for a minute for ticks to come in, so we can set the stoploss as the low. 
            if (isSL_amoCancelled)
            {
                _tickhub.AddLog($"{orderId} log: average price is less than stop loss... waiting for 1 minute for data...");
                Thread.Sleep(55000);

                bool isBullish = false;

                low = ticks[ticks.Count - 1].Low;

                _tickhub.AddLog($"{orderId} log: low found: {low} ...");

                if (ticks[ticks.Count - 1].Open < ticks[ticks.Count - 1].Close)
                {
                    _tickhub.AddLog($"{orderId} log: {order.Instrument.TradingSymbol} is bullish...");
                    isBullish = true;
                }

                if (isBullish)
                {
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

                    regularSLMplaced = true;

                    _tickhub.AddLog($"{orderId} log: SLM order: {orderId_s} placed... {DateTime.Now}");
                }
                else
                {
                    squareOff = true;
                }
            }
        }
        
        private void onTick(KiteConnect.Tick tickData)
        {
            ticks.Add(tickData);
        }
        private void OnOrderUpdate(Order orderData)
        {
            TickHub.orderDataList.Add(orderData);
            _tickhub.AddLog($"{orderId} ticker order update: {orderData.StatusMessage} - at {DateTime.Now}");
        }
        private void OnError(string message)
        {
            _tickhub.AddLog($"{orderId} ticker error: {message} - at {DateTime.Now}");
        }
        private void OnClose()
        {
            _tickhub.AddLog($"{orderId} ticker log: ticker connection closed - at {DateTime.Now}");
        }
        private void OnReconnect()
        {
            _tickhub.AddLog($"{orderId} ticker log: ticker connection reconnected - at {DateTime.Now}");
        }
        private void OnNoReconnect()
        {
            _tickhub.AddLog($"{orderId} ticker log: ticker connection not reconnected - at {DateTime.Now}");
        }
        private void OnConnect()
        {
            _tickhub.AddLog($"{orderId} ticker log: ticker connection connected - at {DateTime.Now}");
        }
    }
}
