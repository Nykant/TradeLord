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
        private bool isPreMarketOpen;
        private bool isMarketOpen;
        private bool slRejected;
        private bool targetRejected;

        public OrderWork(ILogger<OrderWork> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService)
        {
            this.logger = logger;
            this.configuration = configuration;
            _contextAccessor = contextAccessor;
            this.kiteService = kiteService;
            ticks = new List<Tick>();
            rejected = false;
            isPreMarketOpen = false;
            isMarketOpen = false;
            targetplaced = false;
            hit = false;
            isOrderFilled = false;
            isSL_amoCancelled = false;
            squareOff = false;
            regularSLMplaced = false;
            orderFilling = false;
            slRejected = false;
            targetRejected = false;
    }

        // do the work
        public void StartWork(IHubCallerClients clients, TickHub tickHub, TradeOrder order, CancellationToken cancellationToken)
        {
            if (_tickhub == null)
            {
                _tickhub = tickHub;
            }
            _tickhub.AddLog($"{orderId} log: order starting...");
            if (order.TransactionType.ToString() == "BUY")
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

            EntryPoint:

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

            // wait untill we get a tick
            bool anyTicks = false;
            while(!anyTicks)
            {
                if(ticks.Count > 0)
                {
                    anyTicks = true;
                }
                Thread.Sleep(500);
            }

            if(exitTransactionType == "SELL")
            {
                // if last price is more than stop loss then place slm
                if (ticks[ticks.Count - 1].LastPrice > order.StopLoss)
                {
                    try
                    {
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
                    }
                    catch (KiteException e)
                    {
                        _tickhub.AddLog(e.Message);
                    }
                }
                // else tell app that slm order was cancelled, which means it has to find a new one after 1 min
                else
                {
                    _tickhub.AddLog($"{orderId} log: slm order not placed... last price cant be lower than stop loss... it will be placed once filled entry price is known and 1 min analized {DateTime.Now}");
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
                    }
                    catch (KiteException e)
                    {
                        _tickhub.AddLog(e.Message);
                    }
                }
                // else tell app that slm order was cancelled, which means it has to find a new one after 1 min
                else
                {
                    _tickhub.AddLog($"{orderId} log: slm order not placed... last price cant be lower than stop loss... it will be placed once filled entry price is known and 1 min analized {DateTime.Now}");
                    isSL_amoCancelled = true;
                }
            }

            // get order histories
            var orderHistoryQ = kite.GetOrderHistory(orderId_e);
            List<Order> orderHistoryA = new List<Order>();
            if (!isSL_amoCancelled)
            {
                orderHistoryA = kite.GetOrderHistory(orderId_s_amo);
            }

            // check if entry status is rejected
            if (orderHistoryQ[orderHistoryQ.Count - 1].Status == "REJECTED")
            {
                _tickhub.AddLog($"{orderId} log: entry order rejected... {DateTime.Now}");
                if (!isSL_amoCancelled)
                {
                    // if slm is not rejected then cancel it
                    if (orderHistoryA[orderHistoryA.Count - 1].Status != "REJECTED")
                    {
                        _tickhub.AddLog($"{orderId} log: slm order cancelled... {DateTime.Now}");
                        kite.CancelOrder(orderId_s_amo, variety);
                    }
                }
                goto EntryPoint;
            }
            if (!isSL_amoCancelled)
            {
                // do same check for slm in case entry wasnt rejected
                if (orderHistoryA[orderHistoryA.Count - 1].Status == "REJECTED")
                {
                    _tickhub.AddLog($"{orderId} log: slm order rejected... {DateTime.Now}");
                    if (orderHistoryQ[orderHistoryQ.Count - 1].Status != "REJECTED")
                    {
                        _tickhub.AddLog($"{orderId} log: entry order cancelled... {DateTime.Now}");
                        kite.CancelOrder(orderId_e, variety);
                    }
                    goto EntryPoint;
                }
            }

            // while pre market is not open
            while (!isPreMarketOpen)
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
                        _tickhub.AddLog($"{orderId} log: pre market opening... market: {IST} server: {DateTime.Now}");
                        isPreMarketOpen = true;
                    }
                }

                // get order histories
                var orderHistoryQ2 = kite.GetOrderHistory(orderId_e);
                List<Order> orderHistoryA2 = new List<Order>();
                if (!isSL_amoCancelled)
                {
                    orderHistoryA2 = kite.GetOrderHistory(orderId_s_amo);
                }

                // check if entry status is rejected
                if (orderHistoryQ2[orderHistoryQ2.Count - 1].Status == "REJECTED")
                {
                    _tickhub.AddLog($"{orderId} log: entry order rejected... {DateTime.Now}");
                    if (!isSL_amoCancelled)
                    {
                        // if slm is not rejected then cancel it
                        if (orderHistoryA2[orderHistoryA2.Count - 1].Status != "REJECTED")
                        {
                            _tickhub.AddLog($"{orderId} log: slm order cancelled... {DateTime.Now}");
                            kite.CancelOrder(orderId_s_amo, variety);
                        }
                    }
                    goto EntryPoint;
                }
                if (!isSL_amoCancelled)
                {
                    // do same check for slm in case entry wasnt rejected
                    if (orderHistoryA2[orderHistoryA2.Count - 1].Status == "REJECTED")
                    {
                        _tickhub.AddLog($"{orderId} log: slm order rejected... {DateTime.Now}");
                        if (orderHistoryQ2[orderHistoryQ2.Count - 1].Status != "REJECTED")
                        {
                            _tickhub.AddLog($"{orderId} log: entry order cancelled... {DateTime.Now}");
                            kite.CancelOrder(orderId_e, variety);
                        }
                        goto EntryPoint;
                    }
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
                DateTime GMT = DateTime.Now;
                DateTime IST = GMT.AddHours(5).AddMinutes(30);
                DateTime opening = new DateTime(IST.Year, IST.Month, IST.Day, 9, 15, 00);
                DateTime closing = opening.AddHours(6).AddMinutes(15);
                if (DateTime.Compare(IST, opening) >= 0)
                {
                    if (DateTime.Compare(IST, closing) < 0)
                    {
                        _tickhub.AddLog($"{orderId} log: market open... market: {IST} server: {DateTime.Now}");
                        isMarketOpen = true;
                    }
                }

                var orderHistory = kite.GetOrderHistory(orderId_e);
                List<Order> orderHistoryA2 = new List<Order>();
                if (!isSL_amoCancelled)
                {
                    orderHistoryA2 = kite.GetOrderHistory(orderId_s_amo);
                }

                // check if entry status is rejected
                if (orderHistory[orderHistory.Count - 1].Status == "REJECTED")
                {
                    _tickhub.AddLog($"{orderId} log: entry order rejected... {DateTime.Now}");
                    if (!isSL_amoCancelled)
                    {
                        // if slm is not rejected then cancel it
                        if (orderHistoryA2[orderHistoryA2.Count - 1].Status != "REJECTED")
                        {
                            _tickhub.AddLog($"{orderId} log: slm order cancelled... {DateTime.Now}");
                            kite.CancelOrder(orderId_s_amo, variety);
                        }
                    }
                    goto EntryPoint;
                }

                if (!isSL_amoCancelled)
                {
                    // do same check for slm in case entry wasnt rejected
                    if (orderHistoryA2[orderHistoryA2.Count - 1].Status == "REJECTED")
                    {
                        _tickhub.AddLog($"{orderId} log: slm order rejected... {DateTime.Now}");
                        if (orderHistory[orderHistory.Count - 1].Status != "REJECTED")
                        {
                            _tickhub.AddLog($"{orderId} log: entry order cancelled... {DateTime.Now}");
                            kite.CancelOrder(orderId_e, variety);
                        }
                        goto EntryPoint;
                    }
                }

                if (!isOrderFilled)
                {
                    if (orderHistory[orderHistory.Count - 1].FilledQuantity == quantity)
                    {
                        _tickhub.AddLog($"{orderId} log: entry order filled... {DateTime.Now}");

                        if(exitTransactionType == "SELL")
                        {
                            if (orderHistory[orderHistory.Count - 1].AveragePrice < order.StopLoss)
                            {
                                try
                                {
                                    kite.CancelOrder(orderId_s_amo);
                                    _tickhub.AddLog($"{orderId} log: slm amo order cancelled because average price of filled order was less than stop loss...");
                                    isSL_amoCancelled = true;
                                }
                                catch (KiteException e)
                                {
                                    _tickhub.AddLog($"{orderId} error: {e.Message}...");
                                }
                            }
                        }
                        else
                        {
                            if (orderHistory[orderHistory.Count - 1].AveragePrice > order.StopLoss)
                            {
                                try
                                {
                                    kite.CancelOrder(orderId_s_amo);
                                    _tickhub.AddLog($"{orderId} log: slm amo order cancelled because average price of filled order was less than stop loss...");
                                    isSL_amoCancelled = true;
                                }
                                catch (KiteException e)
                                {
                                    _tickhub.AddLog($"{orderId} error: {e.Message}...");
                                }
                            }
                        }

                        isOrderFilled = true;
                    }
                }
                if (IST.Hour == 9 && IST.Minute == 14)
                {
                    if (!isSL_amoCancelled)
                    {
                        if (orderHistory[orderHistory.Count - 1].AveragePrice < order.StopLoss)
                        {
                            kite.CancelOrder(orderId_s_amo);
                            _tickhub.AddLog($"{orderId} log: slm amo order cancelled because time is 9:14 and entry order still not filled. and average price of currently filled entry order is less than stop loss...");
                            isSL_amoCancelled = true;
                        }
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    goto Stopping;
                }
            }

            // market is open! lets place 2 orders simultaneously shall we? (find the functions below this one)
            Parallel.Invoke(() => PlaceTarget(order), () => PlaceStopLoss(order));

            // monitoring the orders 
            List<Order> orderHistoryS = new List<Order>();
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

                    goto Ending;
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
                    if (orderHistoryS[orderHistoryS.Count - 1].Status == "COMPLETE")
                    {
                        _tickhub.AddLog($"{orderId} log: stop loss hit!... filled quantity: {orderHistoryS[orderHistoryS.Count - 1].FilledQuantity} SO FAR --- status: {orderHistoryS[orderHistoryS.Count - 1].Status}...");
                        kite.CancelOrder(orderId_t);
                        goto Ending;
                    }

                    if (orderHistoryT[orderHistoryT.Count - 1].Status == "COMPLETE")
                    {
                        _tickhub.AddLog($"{orderId} log: target hit!... filled quantity: {orderHistoryT[orderHistoryT.Count - 1].FilledQuantity} SO FAR --- status: {orderHistoryT[orderHistoryT.Count - 1].Status}...");
                        if (regularSLMplaced)
                        {
                            kite.CancelOrder(orderId_s);
                        }
                        else
                        {
                            kite.CancelOrder(orderId_s_amo);
                        }
                        goto Ending;
                    }
                }

                if (orderHistoryS[orderHistoryS.Count - 1].Status == "REJECTED")
                {
                    _tickhub.AddLog($"{orderId} log: stop loss order REJECTED ...");
                    slRejected = true;
                    PlaceStopLoss(order);
                }

                if (orderHistoryT[orderHistoryT.Count - 1].Status == "REJECTED")
                {
                    _tickhub.AddLog($"{orderId} log: target order REJECTED ...");
                    targetRejected = true;
                    PlaceTarget(order);
                }
            }

            // go to when order is stopped
            Stopping:

            // gracefully ending the order, in app
            _tickhub.AddLog($"{orderId} log: trying to stop the order...");

            List<Order> OHentry = new List<Order>();
            List<Order> OHstoploss_amo = new List<Order>();
            List<Order> OHtarget = new List<Order>();
            List<Order> OHstoploss_regular = new List<Order>();

            try
            {
                OHentry = kite.GetOrderHistory(orderId_e);
                if (!isSL_amoCancelled)
                {
                    OHstoploss_amo = kite.GetOrderHistory(orderId_s_amo);
                }
                if (targetplaced)
                {
                    OHtarget = kite.GetOrderHistory(orderId_t);
                }
                if (regularSLMplaced)
                {
                    OHstoploss_regular = kite.GetOrderHistory(orderId_s);
                }
            }
            catch (KiteException e)
            {
                _tickhub.AddLog($"{orderId} kite error: {e.Message}...");
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
                        kite.CancelOrder(orderId_e, variety);
                    }
                    catch (KiteException e)
                    {
                        _tickhub.AddLog($"{orderId} kite error: {e.Message}...");
                    }
                }

                if (!isSL_amoCancelled)
                {
                    if (OHstoploss_amo[OHstoploss_amo.Count - 1].Status != "COMPLETE" && OHstoploss_amo[OHstoploss_amo.Count - 1].Status != "REJECTED")
                    {
                        try
                        {
                            kite.CancelOrder(orderId_s_amo, variety);
                        }
                        catch (KiteException e)
                        {
                            _tickhub.AddLog($"{orderId} kite error: {e.Message}...");
                        }

                    }
                }

                if (targetplaced)
                {
                    if (OHtarget[OHtarget.Count - 1].Status != "COMPLETE" && OHtarget[OHtarget.Count - 1].Status != "REJECTED")
                    {
                        try
                        {
                            kite.CancelOrder(orderId_t, "regular");
                        }
                        catch (KiteException e)
                        {
                            _tickhub.AddLog($"{orderId} kite error: {e.Message}...");
                        }

                    }
                }

                if (regularSLMplaced)
                {
                    if (OHstoploss_regular[OHstoploss_regular.Count - 1].Status != "COMPLETE" && OHstoploss_regular[OHstoploss_regular.Count - 1].Status != "REJECTED")
                    {
                        try
                        {
                            kite.CancelOrder(orderId_s, "regular");
                        }
                        catch (KiteException e)
                        {
                            _tickhub.AddLog($"{orderId} kite error: {e.Message}...");
                        }

                    }
                }
                _tickhub.AddLog($"{orderId} log: orders successfully cancelled...");
            }
            catch (Exception e)
            {
                _tickhub.AddLog($"{orderId} log: error cancelling orders: {e.Message}...");
            }

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
                        _tickhub.AddLog($"{orderId} log: whole entry order filled, stop checking proximity... {DateTime.Now}");
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
            }
        }

        // place stop loss
        private void PlaceStopLoss(TradeOrder order)
        {
            // if entry order average price, is less than the stoploss input, then sleep for a minute for ticks to come in, so we can set the stoploss as the low. 
            if (isSL_amoCancelled)
            {
                _tickhub.AddLog($"{orderId} log: average price is less than stop loss... sleeping for 1 minute for data...");

                if (!slRejected)
                {
                    DateTime now = DateTime.Now;
                    DateTime min = now.AddMinutes(1);
                    bool waiting = true;
                    while (waiting)
                    {
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
                         TransactionType: exitTransactionType,
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
            Ending:;
        }
        
        private void onTick(KiteConnect.Tick tickData)
        {
            ticks.Add(tickData);
        }
        private void OnOrderUpdate(Order orderData)
        {
            _tickhub.AddLog($"{orderId} ticker order update: {orderData.StatusMessage} - at {DateTime.Now}");
            _tickhub.AddOrderUpdate(orderData);
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
