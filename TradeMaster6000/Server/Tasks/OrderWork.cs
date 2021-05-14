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
        private static OrderHub orderHub;
        private readonly IKiteService kiteService;
        private Kite kite;
        private List<Tick> ticks;
        private List<Order> orderUpdates;
        private readonly IConfiguration configuration;
        private string orderId_ent;
        private string orderId_tar;
        private string orderId_slm;
        private decimal triggerPrice = 0;
        private decimal target = 0;
        private decimal zonewidth;
        private int quantity;
        private int orderId;
        private bool isOrderFilling;
        private bool isSL_amoCancelled;
        private bool squareOff;
        private bool regularSLMplaced;
        private bool targetplaced;
        private bool hit;
        private string exitTransactionType;
        private bool isPreMarketOpen;
        private bool isMarketOpen;
        private bool slRejected;
        private string variety;

        public OrderWork(ILogger<OrderWork> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService)
        {
            this.logger = logger;
            this.configuration = configuration;
            _contextAccessor = contextAccessor;
            this.kiteService = kiteService;
            ticks = new List<Tick>();
            orderUpdates = new List<Order>();
            isPreMarketOpen = false;
            isMarketOpen = false;
            targetplaced = false;
            hit = false;
            isOrderFilling = false;
            isSL_amoCancelled = false;
            squareOff = false;
            regularSLMplaced = false;
            slRejected = false;
            variety = null;
        }

        // do the work
        public void StartWork(IHubCallerClients clients, OrderHub orderHub, TradeOrder order, CancellationToken cancellationToken)
        {
            Ticker ticker = Initialize(order, orderHub);

            OrderWork.orderHub.AddLog($"{orderId} log: order starting...");

            order.StopLoss = RoundUp(order.StopLoss, (decimal)0.05);
            order.Entry = RoundUp(order.Entry, (decimal)0.05);

            // calculate zonewidth and quantity
            if (order.TransactionType.ToString() == "BUY")
            {
                zonewidth = order.Entry - order.StopLoss;
                decimal decQuantity = order.Risk / zonewidth;
                quantity = (int)decQuantity;
            }
            else
            {
                zonewidth = order.StopLoss - order.Entry;
                decimal decQuantity = order.Risk / zonewidth;
                quantity = (int)decQuantity;
            }

            // wait untill we get a tick
            bool anyTicks = false;
            while (!anyTicks)
            {
                if (ticks.Count > 0)
                {
                    anyTicks = true;
                }
            }

            variety = GetCurrentVariety();

            EntryPoint:

            try
            {
                PlaceEntry(order);
            }
            catch (KiteException e)
            {
                OrderWork.orderHub.AddLog($"{orderId} kite error {e.Message}");
            }

            if (exitTransactionType == "SELL")
            {
                // if last price is more than stop loss then place slm
                if (ticks[ticks.Count - 1].LastPrice > order.StopLoss)
                {
                    try
                    {
                        PlaceSLM(order);
                    }
                    catch (KiteException e)
                    {
                        OrderWork.orderHub.AddLog($"{orderId} kite error {e.Message}");
                    }
                }
                // else tell app that slm order was cancelled, which means it has to find a new one after 1 min
                else
                {
                    OrderWork.orderHub.AddLog($"{orderId} log: slm order not placed... last price cant be lower than stop loss... it will be placed once filled entry price is known and 1 min analized {DateTime.Now}");
                    isSL_amoCancelled = true;
                }
            }
            else
            {
                // if last price is more than stop loss then place slm
                if (ticks[ticks.Count - 1].LastPrice < order.StopLoss)
                {
                    try
                    {
                        PlaceSLM(order);
                    }
                    catch (KiteException e)
                    {
                        OrderWork.orderHub.AddLog($"{orderId} kite error: {e.Message}");
                    }
                }
                // else tell app that slm order was cancelled, which means it has to find a new one after 1 min
                else
                {
                    OrderWork.orderHub.AddLog($"{orderId} log: slm order not placed... last price cant be lower than stop loss... it will be placed once filled entry price is known and 1 min analized {DateTime.Now}");
                    isSL_amoCancelled = true;
                }
            }

            while(!AnyOrderUpdates(orderId_ent))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    goto Stopping;
                }
            }
            if (!isSL_amoCancelled)
            {
                while (!AnyOrderUpdates(orderId_slm))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        goto Stopping;
                    }
                }
            }

            switch (CheckOrderStatuses())
            {
                case "entrypoint":
                    goto EntryPoint;
                case "continue":
                    break;
            }

            // while pre market is not open
            while (!isPreMarketOpen)
            {
                if (IsPreMarketOpen())
                {
                    isPreMarketOpen = true;
                    break;
                }

                switch (CheckOrderStatuses())
                {
                    case "entrypoint":
                        goto EntryPoint;
                    case "continue":
                        break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    goto Stopping;
                }

                Thread.Sleep(5000);
            }

            // while market is not open
            while (!isMarketOpen)
            {
                if (IsMarketOpen())
                {
                    isMarketOpen = true;
                    break;
                }

                switch (CheckOrderStatuses())
                {
                    case "entrypoint":
                        goto EntryPoint;
                    case "continue":
                        break;
                }

                if (!isOrderFilling)
                {
                    CheckIfFilling(order);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    goto Stopping;
                }
            }

            switch (CheckOrderStatuses())
            {
                case "entrypoint":
                    goto EntryPoint;
                case "continue":
                    break;
            }

            // market is open! lets place 2 orders simultaneously shall we? (find the functions below this one)
            Parallel.Invoke(() => PlaceTarget(order), () => PlaceStopLoss(order));

            // monitoring the orders 
            while (!cancellationToken.IsCancellationRequested)
            {
                if (squareOff)
                {
                    OrderWork.orderHub.AddLog($"{orderId} log: squaring off :'( it is going to be a rough battle, wish me luck guys...");
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

                    kite.CancelOrder(orderId_tar);

                    goto Ending;
                }


                var orderHistoryS = kite.GetOrderHistory(orderId_slm);


                var orderHistoryT = kite.GetOrderHistory(orderId_tar);

                if (!hit)
                {
                    if (orderHistoryS[orderHistoryS.Count - 1].Status == "COMPLETE")
                    {
                        OrderWork.orderHub.AddLog($"{orderId} log: stop loss hit!... filled quantity: {orderHistoryS[orderHistoryS.Count - 1].FilledQuantity} SO FAR --- status: {orderHistoryS[orderHistoryS.Count - 1].Status}...");
                        kite.CancelOrder(orderId_tar);
                        goto Ending;
                    }

                    if (orderHistoryT[orderHistoryT.Count - 1].Status == "COMPLETE")
                    {
                        OrderWork.orderHub.AddLog($"{orderId} log: target hit!... filled quantity: {orderHistoryT[orderHistoryT.Count - 1].FilledQuantity} SO FAR --- status: {orderHistoryT[orderHistoryT.Count - 1].Status}...");
                        if (regularSLMplaced)
                        {
                            kite.CancelOrder(orderId_slm);
                        }
                        else
                        {
                            kite.CancelOrder(orderId_slm);
                        }
                        goto Ending;
                    }
                }

                if (orderHistoryS[orderHistoryS.Count - 1].Status == "REJECTED")
                {
                    OrderWork.orderHub.AddLog($"{orderId} log: stop loss order REJECTED ...");
                    slRejected = true;
                    PlaceStopLoss(order);
                }

                if (orderHistoryT[orderHistoryT.Count - 1].Status == "REJECTED")
                {
                    OrderWork.orderHub.AddLog($"{orderId} log: target order REJECTED ...");
                    PlaceTarget(order);
                }
            }

            // go to when order is stopped
            Stopping:

            // gracefully ending the order, in app
            OrderWork.orderHub.AddLog($"{orderId} log: trying to stop the order...");

            List<Order> OHentry = new List<Order>();
            List<Order> OHstoploss_amo = new List<Order>();
            List<Order> OHtarget = new List<Order>();
            List<Order> OHstoploss_regular = new List<Order>();

            try
            {
                OHentry = kite.GetOrderHistory(orderId_ent);
                if (!isSL_amoCancelled)
                {
                    OHstoploss_amo = kite.GetOrderHistory(orderId_slm);
                }
                if (targetplaced)
                {
                    OHtarget = kite.GetOrderHistory(orderId_tar);
                }
                if (regularSLMplaced)
                {
                    OHstoploss_regular = kite.GetOrderHistory(orderId_slm);
                }
            }
            catch (KiteException e)
            {
                OrderWork.orderHub.AddLog($"{orderId} kite error: {e.Message}...");
            }

            DateTime GMT3 = DateTime.Now;
            DateTime IST3 = GMT3.AddHours(5).AddMinutes(30);
            DateTime opening3 = new DateTime(IST3.Year, IST3.Month, IST3.Day, 9, 0, 0);
            DateTime closing3 = opening3.AddHours(6).AddMinutes(30);

            if(DateTime.Compare(IST3, opening3) < 0)
            {
                variety = "amo";
            }
            if (DateTime.Compare(IST3, opening3) > 0)
            {
                variety = "regular";
            }
            if(DateTime.Compare(IST3, closing3) > 0)
            {
                variety = "amo";
            }

            try
            {
                if (OHentry[OHentry.Count - 1].Status != "COMPLETE" && OHentry[OHentry.Count - 1].Status != "REJECTED")
                {
                    try
                    {
                        kite.CancelOrder(orderId_ent, variety);
                    }
                    catch (KiteException e)
                    {
                        OrderWork.orderHub.AddLog($"{orderId} kite error: {e.Message}...");
                    }
                }

                if (!isSL_amoCancelled)
                {
                    if (OHstoploss_amo[OHstoploss_amo.Count - 1].Status != "COMPLETE" && OHstoploss_amo[OHstoploss_amo.Count - 1].Status != "REJECTED")
                    {
                        try
                        {
                            kite.CancelOrder(orderId_slm, variety);
                        }
                        catch (KiteException e)
                        {
                            OrderWork.orderHub.AddLog($"{orderId} kite error: {e.Message}...");
                        }

                    }
                }

                if (targetplaced)
                {
                    if (OHtarget[OHtarget.Count - 1].Status != "COMPLETE" && OHtarget[OHtarget.Count - 1].Status != "REJECTED")
                    {
                        try
                        {
                            kite.CancelOrder(orderId_tar, "regular");
                        }
                        catch (KiteException e)
                        {
                            OrderWork.orderHub.AddLog($"{orderId} kite error: {e.Message}...");
                        }

                    }
                }

                if (regularSLMplaced)
                {
                    if (OHstoploss_regular[OHstoploss_regular.Count - 1].Status != "COMPLETE" && OHstoploss_regular[OHstoploss_regular.Count - 1].Status != "REJECTED")
                    {
                        try
                        {
                            kite.CancelOrder(orderId_slm, "regular");
                        }
                        catch (KiteException e)
                        {
                            OrderWork.orderHub.AddLog($"{orderId} kite error: {e.Message}...");
                        }

                    }
                }
                OrderWork.orderHub.AddLog($"{orderId} log: orders successfully cancelled...");
            }
            catch (Exception e)
            {
                OrderWork.orderHub.AddLog($"{orderId} log: error cancelling orders: {e.Message}...");
            }

            List<Order> OHentryLOL = kite.GetOrderHistory(orderId_ent);

            if(OHentryLOL[OHentryLOL.Count - 1].FilledQuantity > 0)
            {
                Dictionary<string, dynamic> placeOrderResponse = kite.PlaceOrder(
                     Exchange: order.Instrument.Exchange,
                     TradingSymbol: order.Instrument.TradingSymbol,
                     TransactionType: exitTransactionType,
                     Quantity: OHentryLOL[OHentryLOL.Count - 1].FilledQuantity,
                     Product: Constants.PRODUCT_MIS,
                     OrderType: Constants.ORDER_TYPE_MARKET,
                     Validity: Constants.VALIDITY_DAY,
                     Variety: Constants.VARIETY_REGULAR
                );
            }

            Ending:

            ticker.UnSubscribe(new[] { order.Instrument.Id });
            ticker.DisableReconnect();
            ticker.Close();
            OrderWork.orderHub.AddLog($"{orderId} log: order stopped...");
        }

        // place target
        private void PlaceTarget(TradeOrder order)
        {
            List<Order> orderHistoryE = new List<Order>();
            bool isFilling = false;
            while (!isFilling)
            {
                orderHistoryE = kite.GetOrderHistory(orderId_ent);
                if(orderHistoryE[orderHistoryE.Count - 1].FilledQuantity > 0)
                {
                    isFilling = true;
                }
            }

            decimal proximity = 0;
            if (exitTransactionType == "SELL")
            {
                target = (order.RxR * zonewidth) + orderHistoryE[orderHistoryE.Count - 1].AveragePrice;
                proximity = ((target - orderHistoryE[orderHistoryE.Count - 1].AveragePrice) * (decimal)0.8)
                                            + orderHistoryE[orderHistoryE.Count - 1].AveragePrice;
            }
            else
            {
                target = orderHistoryE[orderHistoryE.Count - 1].AveragePrice - (order.RxR * zonewidth);
                proximity = orderHistoryE[orderHistoryE.Count - 1].AveragePrice 
                    - ((orderHistoryE[orderHistoryE.Count - 1].AveragePrice - target) * (decimal)0.8);
            }

            try
            {
                orderHub.AddLog($"{orderId} log: placing target... {DateTime.Now}");
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
                orderId_tar = value1;

                orderHub.AddLog($"{orderId} log: target: {orderId_tar} placed... with target: {target} ... {DateTime.Now}");
            }
            catch (KiteException e)
            {
                orderHub.AddLog($"{orderId} error: {e.Message}");
            }

            orderHub.AddLog($"{orderId} log: checking proximity: {proximity} of order: {orderId_ent} ... {DateTime.Now}");

            bool checkingProximity = true;

            // check proximity until order is fully filled. to modify the target order in case tick price is closing in on it.
            while (checkingProximity)
            {
                var orderHistory = kite.GetOrderHistory(orderId_ent);
                if (isOrderFilling || orderHistory[orderHistory.Count - 1].FilledQuantity == quantity)
                {
                    if(isOrderFilling == false)
                    {
                        orderHub.AddLog($"{orderId} log: whole entry order filled, stop checking proximity... {DateTime.Now}");
                        isOrderFilling = true;
                    }
                    checkingProximity = false;
                    break;
                }
                if (ticks[ticks.Count - 1].High >= proximity)
                {
                    if (orderHistory[orderHistory.Count - 1].FilledQuantity != quantity)
                    {
                        kite.ModifyOrder(
                            orderId_tar,
                            Quantity: orderHistory[orderHistory.Count - 1].FilledQuantity.ToString()
                        );
                        orderHub.AddLog($"{orderId} log: target: {orderId_tar} - quantity modified {orderHistory[orderHistory.Count - 1].FilledQuantity}... {DateTime.Now}");
                    }
                }
            }
        }

        // place stop loss
        private void PlaceStopLoss(TradeOrder order)
        {
            // if entry order average price, is less than the stoploss input, then sleep for a minute for ticks to come in, so we can set the stoploss as the low. 
            if (isSL_amoCancelled)
            {
                orderHub.AddLog($"{orderId} log: average price is less than stop loss... waiting 1 minute for data...");

                if (!slRejected)
                {
                    DateTime now = DateTime.Now;
                    DateTime min = now.AddSeconds(56.5);
                    bool waiting = true;
                    while (waiting)
                    {
                        now = DateTime.Now;
                        if (DateTime.Compare(now, min) >= 0)
                        {
                            waiting = false;
                        }
                        if (order.TokenSource.IsCancellationRequested)
                        {
                            goto Ending;
                        }
                    }
                }

                if (exitTransactionType == "BUY")
                {
                    triggerPrice = ticks[ticks.Count - 1].High;
                    triggerPrice = triggerPrice * (decimal)1.00015;
                    triggerPrice = RoundUp(triggerPrice, (decimal)0.05);
                }
                else
                {
                    triggerPrice = ticks[ticks.Count - 1].Low;
                    triggerPrice = triggerPrice * (decimal)0.99985;
                    triggerPrice = RoundDown(triggerPrice, (decimal)0.05);
                }

                orderHub.AddLog($"{orderId} log: low found: {triggerPrice} ...");

                bool isBullish = false;
                if (ticks[ticks.Count - 1].Open < ticks[ticks.Count - 1].Close)
                {
                    orderHub.AddLog($"{orderId} log: {order.Instrument.TradingSymbol} is bullish...");
                    isBullish = true;
                }

                if(exitTransactionType == "SELL")
                {
                    if (isBullish)
                    {
                        try
                        {
                            Dictionary<string, dynamic> response = kite.PlaceOrder(
                                 Exchange: order.Instrument.Exchange,
                                 TradingSymbol: order.Instrument.TradingSymbol,
                                 TransactionType: exitTransactionType,
                                 Quantity: quantity,
                                 TriggerPrice: triggerPrice,
                                 Product: Constants.PRODUCT_MIS,
                                 OrderType: Constants.ORDER_TYPE_SLM,
                                 Validity: Constants.VALIDITY_DAY,
                                 Variety: Constants.VARIETY_REGULAR
                            );

                            response.TryGetValue("data", out dynamic value);
                            Dictionary<string, dynamic> date = value;
                            date.TryGetValue("order_id", out dynamic value1);
                            orderId_slm = value1;

                            regularSLMplaced = true;

                            orderHub.AddLog($"{orderId} log: regular SLM order: {orderId_slm} placed... {DateTime.Now}");
                        }
                        catch (KiteException e)
                        {
                            orderHub.AddLog($"{orderId} error: {e.Message}");
                        }
                    }
                    else
                    {
                        squareOff = true;
                    }
                }
                else
                {
                    if (!isBullish)
                    {
                        try
                        {
                            Dictionary<string, dynamic> response = kite.PlaceOrder(
                                 Exchange: order.Instrument.Exchange,
                                 TradingSymbol: order.Instrument.TradingSymbol,
                                 TransactionType: exitTransactionType,
                                 Quantity: quantity,
                                 TriggerPrice: triggerPrice,
                                 Product: Constants.PRODUCT_MIS,
                                 OrderType: Constants.ORDER_TYPE_SLM,
                                 Validity: Constants.VALIDITY_DAY,
                                 Variety: Constants.VARIETY_REGULAR
                            );

                            response.TryGetValue("data", out dynamic value);
                            Dictionary<string, dynamic> date = value;
                            date.TryGetValue("order_id", out dynamic value1);
                            orderId_slm = value1;

                            regularSLMplaced = true;

                            orderHub.AddLog($"{orderId} log: SLM order: {orderId_slm} placed... {DateTime.Now}");
                        }
                        catch (KiteException e)
                        {
                            orderHub.AddLog($"{orderId} error: {e.Message}");
                        }
                    }
                    else
                    {
                        squareOff = true;
                    }
                }
            }
            Ending:;
        }

        private Ticker Initialize(TradeOrder order, OrderHub orderHub)
        {
            orderId = order.Id;
            kite = kiteService.GetKite();

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

            if (OrderWork.orderHub == null)
            {
                OrderWork.orderHub = orderHub;
            }

            if (order.TransactionType.ToString() == "BUY")
            {
                exitTransactionType = "SELL";
            }

            else
            {
                exitTransactionType = "BUY";
            }

            return ticker;
        }

        private void PlaceEntry(TradeOrder order)
        {
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

            // get order id from place order response
            response.TryGetValue("data", out dynamic value);
            Dictionary<string, dynamic> data = value;
            data.TryGetValue("order_id", out dynamic value1);
            orderId_ent = value1;

            orderHub.AddLog($"{orderId} log: entry order placed... {DateTime.Now}");
        }

        private void PlaceSLM(TradeOrder order)
        {
            // place slm order
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
            orderId_slm = value1S;

            orderHub.AddLog($"{orderId} log: SLM order: {orderId_slm} placed... {DateTime.Now}");
        }

        private string CheckOrderStatuses()
        {
            // get order update
            Order orderHistoryQ = GetLatestOrderUpdate(orderId_ent);
            Order orderHistoryA = new Order();
            if (!isSL_amoCancelled)
            {
                orderHistoryA = GetLatestOrderUpdate(orderId_slm);
            }

            // check if entry status is rejected
            if (orderHistoryQ.Status == "REJECTED")
            {
                orderHub.AddLog($"{orderId} log: entry order rejected... {DateTime.Now}");
                if (!isSL_amoCancelled)
                {
                    // if slm is not rejected then cancel it
                    if (orderHistoryA.Status != "REJECTED")
                    {
                        orderHub.AddLog($"{orderId} log: slm order cancelled... {DateTime.Now}");
                        kite.CancelOrder(orderId_slm, GetCurrentVariety());
                    }
                }
                return "entrypoint";
            }
            if (orderHistoryA.Status == "REJECTED")
            {
                orderHub.AddLog($"{orderId} log: entry order rejected... {DateTime.Now}");
                if (!isSL_amoCancelled)
                {
                    // if slm is not rejected then cancel it
                    if (orderHistoryQ.Status != "REJECTED")
                    {
                        orderHub.AddLog($"{orderId} log: slm order cancelled... {DateTime.Now}");
                        kite.CancelOrder(orderId_ent, GetCurrentVariety());
                    }
                }
                return "entrypoint";
            }
            return "continue";
        }

        private void CheckIfFilling(TradeOrder order)
        {
            Order orderHistory = GetLatestOrderUpdate(orderId_ent);
            if (orderHistory.FilledQuantity > 0)
            {
                orderHub.AddLog($"{orderId} log: entry order filled... {DateTime.Now}");

                if (exitTransactionType == "SELL")
                {
                    if (orderHistory.AveragePrice < order.StopLoss)
                    {
                        try
                        {
                            kite.CancelOrder(orderId_slm, "amo");
                            orderHub.AddLog($"{orderId} log: slm amo order cancelled because average price of filled order was less than stop loss...");
                            isSL_amoCancelled = true;
                        }
                        catch (KiteException e)
                        {
                            orderHub.AddLog($"{orderId} kite error: {e.Message}...");
                        }
                    }
                }
                else
                {
                    if (orderHistory.AveragePrice > order.StopLoss)
                    {
                        try
                        {
                            kite.CancelOrder(orderId_slm, "amo");
                            orderHub.AddLog($"{orderId} log: slm amo order cancelled because average price of filled order was more than stop loss...");
                            isSL_amoCancelled = true;
                        }
                        catch (KiteException e)
                        {
                            orderHub.AddLog($"{orderId} kite error: {e.Message}...");
                        }
                    }
                }
                isOrderFilling = true;
            }
        }
        private bool IsMarketOpen()
        {
            DateTime GMT = DateTime.Now;
            DateTime IST = GMT.AddHours(5).AddMinutes(30);
            DateTime opening = new DateTime(IST.Year, IST.Month, IST.Day, 9, 15, 00);
            DateTime closing = opening.AddHours(6).AddMinutes(15);
            if (DateTime.Compare(IST, opening) >= 0)
            {
                if (DateTime.Compare(IST, closing) < 0)
                {
                    orderHub.AddLog($"{orderId} log: market open... market: {IST} server: {DateTime.Now}");
                    return true;
                }
            }
            return false;
        }
        private bool IsPreMarketOpen()
        {
            // check time once in a while, to figure out if it is time to wake up and go to work.
            DateTime GST = DateTime.Now;
            DateTime IST = GST.AddHours(5).AddMinutes(30);
            DateTime opening = new DateTime(IST.Year, IST.Month, IST.Day, 9, 00, 00);
            DateTime closing = opening.AddHours(6).AddMinutes(30);
            // if clock is 9 its time to get up and start the day!
            if (DateTime.Compare(IST, opening) >= 0)
            {
                if (DateTime.Compare(IST, closing) < 0)
                {
                    orderHub.AddLog($"{orderId} log: pre market opening... market: {IST} server: {DateTime.Now}");
                    return true;
                }
            }
            return false;
        }

        private string GetCurrentVariety()
        {
            string variety = null;

            DateTime GST1 = DateTime.Now;
            DateTime IST1 = GST1.AddHours(5).AddMinutes(30);
            DateTime opening1 = new DateTime(IST1.Year, IST1.Month, IST1.Day, 9, 0, 0);
            DateTime closing1 = opening1.AddHours(6).AddMinutes(30);

            if (DateTime.Compare(IST1, opening1) < 0)
            {
                variety = "amo";
            }
            else if (DateTime.Compare(IST1, opening1) >= 0)
            {
                variety = "regular";
            }
            if (DateTime.Compare(IST1, closing1) >= 0)
            {
                variety = "amo";
            }

            return variety;
        }

        private bool AnyOrderUpdates(string id)
        {
            foreach(var update in orderUpdates)
            {
                if(update.OrderId == id)
                {
                    return true;
                }
            }
            return false;
        }

        private Order GetLatestOrderUpdate(string id)
        {
            for(int i = orderUpdates.Count - 1; i > 0; i--)
            {
                if(orderUpdates[i].OrderId == id)
                {
                    return orderUpdates[i];
                }
            }
            throw new Exception($"order update not found with id {id}");
        }

        private decimal RoundUp(decimal value, decimal step)
        {
            var multiplicand = Math.Ceiling(value / step);
            return step * multiplicand;
        }

        private decimal RoundDown(decimal value, decimal step)
        {
            var multiplicand = Math.Floor(value / step);
            return step * multiplicand;
        }

        private void onTick(Tick tickData)
        {
            ticks.Add(tickData);
        }
        private void OnOrderUpdate(Order orderData)
        {
            orderUpdates.Add(orderData);
        }
        private void OnError(string message)
        {
            orderHub.AddLog($"{orderId} ticker error: {message} - at {DateTime.Now}");
        }
        private void OnClose()
        {
            orderHub.AddLog($"{orderId} ticker log: ticker connection closed - at {DateTime.Now}");
        }
        private void OnReconnect()
        {
            orderHub.AddLog($"{orderId} ticker log: ticker connection reconnected - at {DateTime.Now}");
        }
        private void OnNoReconnect()
        {
            orderHub.AddLog($"{orderId} ticker log: ticker connection not reconnected - at {DateTime.Now}");
        }
        private void OnConnect()
        {
            orderHub.AddLog($"{orderId} ticker log: ticker connection connected - at {DateTime.Now}");
        }
    }
}
