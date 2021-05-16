using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Server.DataHelpers;
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
        private readonly IKiteService kiteService;
        private readonly IConfiguration configuration;
        private readonly ITradeOrderHelper tradeOrderHelper;
        private static OrderHub orderHub;

        private Kite kite;
        private List<Tick> ticks;
        private List<Order> orderUpdates;

        private string orderId_ent;
        private string orderId_tar;
        private string orderId_slm;
        private decimal triggerPrice;
        private decimal target;
        private decimal zonewidth;
        private int quantity;
        private int orderId;
        private bool isOrderFilling;
        private bool is_pre_slm_cancelled;
        private bool squareOff;
        private bool regularSLMplaced;
        private bool targetplaced;
        private bool hit;
        private string exitTransactionType;
        private bool isPreMarketOpen;
        private bool isMarketOpen;
        private bool slRejected;
        private string variety;

        public OrderWork(ILogger<OrderWork> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService, ITradeOrderHelper tradeOrderHelper)
        {
            this.logger = logger;
            this.configuration = configuration;
            _contextAccessor = contextAccessor;
            this.kiteService = kiteService;
            this.tradeOrderHelper = tradeOrderHelper;
            ticks = new List<Tick>();
            orderUpdates = new List<Order>();
            isPreMarketOpen = false;
            isMarketOpen = false;
            targetplaced = false;
            hit = false;
            isOrderFilling = false;
            is_pre_slm_cancelled = false;
            squareOff = false;
            regularSLMplaced = false;
            slRejected = false;
        }

        // do the work
        public async Task StartWork(IHubCallerClients clients, OrderHub _orderHub, TradeOrder order, CancellationToken cancellationToken)
        {
            Ticker ticker = Initialize(order, _orderHub);

            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = order.Id,
                    Log = $"order starting...",
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);

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
                await PlaceEntry(order);
            }
            catch (KiteException e)
            {
                await tradeDbContext.TradeLogs.AddAsync(
                    new TradeLog
                    {
                        TradeOrderId = order.Id,
                        Log = $"kite error: {e.Message}...",
                        Timestamp = DateTime.Now
                    }
                ).ConfigureAwait(false);
            }

            if (exitTransactionType == "SELL")
            {
                // if last price is more than stop loss then place slm
                if (ticks[ticks.Count - 1].LastPrice > order.StopLoss)
                {
                    try
                    {
                        await PlacePreSLM(order);
                    }
                    catch (KiteException e)
                    {
                        await tradeDbContext.TradeLogs.AddAsync(
                            new TradeLog
                            {
                                TradeOrderId = order.Id,
                                Log = $"kite error: {e.Message}...",
                                Timestamp = DateTime.Now
                            }
                        ).ConfigureAwait(false);
                    }
                }
                // else tell app that slm order was cancelled, which means it has to find a new one after 1 min
                else
                {
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = order.Id,
                            Log = $"pre slm was cancelled...",
                            Timestamp = DateTime.Now
                        }
                    ).ConfigureAwait(false);
                    is_pre_slm_cancelled = true;
                }
            }
            else
            {
                // if last price is more than stop loss then place slm
                if (ticks[ticks.Count - 1].LastPrice < order.StopLoss)
                {
                    try
                    {
                        await PlacePreSLM(order);
                    }
                    catch (KiteException e)
                    {
                        await tradeDbContext.TradeLogs.AddAsync(
                            new TradeLog
                            {
                                TradeOrderId = order.Id,
                                Log = $"kite error: {e.Message}...",
                                Timestamp = DateTime.Now
                            }
                        ).ConfigureAwait(false);
                    }
                }
                // else tell app that slm order was cancelled, which means it has to find a new one after 1 min
                else
                {
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = order.Id,
                            Log = $"pre slm was cancelled...",
                            Timestamp = DateTime.Now
                        }
                    ).ConfigureAwait(false);
                    is_pre_slm_cancelled = true;
                }
            }

            while(!AnyOrderUpdates(orderId_ent))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    goto Stopping;
                }
            }
            if (!is_pre_slm_cancelled)
            {
                while (!AnyOrderUpdates(orderId_slm))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        goto Stopping;
                    }
                }
            }

            // do while pre market is not open
            do
            {
                switch (await CheckOrderStatuses())
                {
                    case "entrypoint":
                        goto EntryPoint;
                    case "continue":
                        break;
                }

                if (await IsPreMarketOpen())
                {
                    isPreMarketOpen = true;
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    goto Stopping;
                }

                Thread.Sleep(5000);
            }
            while (!isPreMarketOpen);

            // while market is not open
            while (!isMarketOpen)
            {
                if (await IsMarketOpen())
                {
                    isMarketOpen = true;
                    break;
                }

                switch (await CheckOrderStatuses())
                {
                    case "entrypoint":
                        goto EntryPoint;
                    case "continue":
                        break;
                }

                if (!isOrderFilling)
                {
                    await CheckIfFilling(order);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    goto Stopping;
                }
            }

            switch (await CheckOrderStatuses())
            {
                case "entrypoint":
                    goto EntryPoint;
                case "continue":
                    break;
            }

            await Task.Run(() =>
            {
                Parallel.Invoke(async () => await PlaceTarget(order), async () => await PlaceStopLoss(order));
            }).ConfigureAwait(false);

            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = order.Id,
                    Log = $"monitoring orders...",
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);
            // monitoring the orders 
            while (!cancellationToken.IsCancellationRequested)
            {
                if (squareOff)
                {

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

                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = order.Id,
                            Log = $"squared off...",
                            Timestamp = DateTime.Now
                        }
                    ).ConfigureAwait(false);

                    goto Ending;
                }

                var orderHistoryS = GetLatestOrderUpdate(orderId_slm);
                var orderHistoryT = GetLatestOrderUpdate(orderId_tar);

                if (!hit)
                {
                    if (orderHistoryS.FilledQuantity > 0)
                    {
                        await tradeDbContext.TradeLogs.AddAsync(
                            new TradeLog
                            {
                                TradeOrderId = order.Id,
                                Log = $"slm hit...",
                                Timestamp = DateTime.Now
                            }
                        ).ConfigureAwait(false);
                        kite.CancelOrder(orderId_tar);
                        goto Ending;
                    }

                    if (orderHistoryT.FilledQuantity > 0)
                    {
                        await tradeDbContext.TradeLogs.AddAsync(
                            new TradeLog
                            {
                                TradeOrderId = order.Id,
                                Log = $"target hit...",
                                Timestamp = DateTime.Now
                            }
                        ).ConfigureAwait(false);
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

                if (orderHistoryS.Status == "REJECTED")
                {
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = order.Id,
                            Log = $"slm rejected...",
                            Timestamp = DateTime.Now
                        }
                    ).ConfigureAwait(false);
                    slRejected = true;
                    await Task.Run(async () =>
                    {
                        await PlaceStopLoss(order);
                    }).ConfigureAwait(false);
                }

                if (orderHistoryT.Status == "REJECTED")
                {
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = order.Id,
                            Log = $"target rejected...",
                            Timestamp = DateTime.Now
                        }
                    ).ConfigureAwait(false);
                    await Task.Run(async () =>
                    {
                        await PlaceTarget(order);
                    }).ConfigureAwait(false);
                }
            }

            // go to when order is stopped
            Stopping:

            // gracefully ending the order, in app
            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = order.Id,
                    Log = $"stopping order...",
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);

            Order OHtarget = new Order();
            Order OHentry = GetLatestOrderUpdate(orderId_ent);
            Order OHstoploss = GetLatestOrderUpdate(orderId_slm);

            if (targetplaced)
            {
                OHtarget = GetLatestOrderUpdate(orderId_tar);
            }

            variety = GetCurrentVariety();

            if (OHentry.Status != "COMPLETE" && OHentry.Status != "REJECTED")
            {
                try
                {
                    kite.CancelOrder(orderId_ent, variety);
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = order.Id,
                            Log = $"entry order cancelled...",
                            Timestamp = DateTime.Now
                        }
                    ).ConfigureAwait(false);
                }
                catch (KiteException e)
                {
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = order.Id,
                            Log = $"kite error: {e.Message}...",
                            Timestamp = DateTime.Now
                        }
                    ).ConfigureAwait(false);
                }
            }


            if (OHstoploss.Status != "COMPLETE" && OHstoploss.Status != "REJECTED")
            {
                try
                {
                    kite.CancelOrder(orderId_slm, variety);
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = order.Id,
                            Log = $"slm order cancelled...",
                            Timestamp = DateTime.Now
                        }
                    ).ConfigureAwait(false);
                }
                catch (KiteException e)
                {
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = order.Id,
                            Log = $"kite error: {e.Message}...",
                            Timestamp = DateTime.Now
                        }
                    ).ConfigureAwait(false);
                }
            }


            if (targetplaced)
            {
                if (OHtarget.Status != "COMPLETE" && OHtarget.Status != "REJECTED")
                {
                    try
                    {
                        kite.CancelOrder(orderId_tar, variety);
                        await tradeDbContext.TradeLogs.AddAsync(
                            new TradeLog
                            {
                                TradeOrderId = order.Id,
                                Log = $"target order cancelled...",
                                Timestamp = DateTime.Now
                            }
                        ).ConfigureAwait(false);
                    }
                    catch (KiteException e)
                    {
                        await tradeDbContext.TradeLogs.AddAsync(
                            new TradeLog
                            {
                                TradeOrderId = order.Id,
                                Log = $"kite error: {e.Message}...",
                                Timestamp = DateTime.Now
                            }
                        ).ConfigureAwait(false);
                    }

                }
            }

            OHentry = GetLatestOrderUpdate(orderId_ent);
            OHstoploss = GetLatestOrderUpdate(orderId_slm);
            if (targetplaced)
            {
                OHtarget = GetLatestOrderUpdate(orderId_tar);
            }

            if (OHentry.FilledQuantity > 0)
            {
                int squareoffamount = OHentry.FilledQuantity;
                if (OHstoploss.FilledQuantity > 0)
                {
                    squareoffamount = OHentry.FilledQuantity - OHstoploss.FilledQuantity;
                }
                if (targetplaced)
                {
                    if (OHtarget.FilledQuantity > 0)
                    {
                        squareoffamount = OHentry.FilledQuantity - OHtarget.FilledQuantity;
                    }
                }

                Dictionary<string, dynamic> placeOrderResponse = kite.PlaceOrder(
                     Exchange: order.Instrument.Exchange,
                     TradingSymbol: order.Instrument.TradingSymbol,
                     TransactionType: exitTransactionType,
                     Quantity: squareoffamount,
                     Product: Constants.PRODUCT_MIS,
                     OrderType: Constants.ORDER_TYPE_MARKET,
                     Validity: Constants.VALIDITY_DAY,
                     Variety: Constants.VARIETY_REGULAR
                );
            }

            Ending:

            ticker.UnSubscribe(new[] { order.Instrument.Token });
            ticker.DisableReconnect();
            ticker.Close();
            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = order.Id,
                    Log = $"order stopped...",
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);
        }

        // place target
        private async Task PlaceTarget(TradeOrder order)
        {
            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = order.Id,
                    Log = $"doing target logic...",
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);

            while (!isOrderFilling)
            {
                CheckIfFilling(order);
            }

            Order orderHistoryE = GetLatestOrderUpdate(orderId_ent);
            decimal proximity = 0;
            if (exitTransactionType == "SELL")
            {
                target = (order.RxR * zonewidth) + orderHistoryE.AveragePrice;
                proximity = ((target - orderHistoryE.AveragePrice) * (decimal)0.8)
                                            + orderHistoryE.AveragePrice;
            }
            else
            {
                target = orderHistoryE.AveragePrice - (order.RxR * zonewidth);
                proximity = orderHistoryE.AveragePrice 
                    - ((orderHistoryE.AveragePrice - target) * (decimal)0.8);
            }

            try
            {
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

                await tradeDbContext.TradeLogs.AddAsync(
                    new TradeLog
                    {
                        TradeOrderId = order.Id,
                        Log = $"target placed...",
                        Timestamp = DateTime.Now
                    }
                ).ConfigureAwait(false);
            }
            catch (KiteException e)
            {
                await tradeDbContext.TradeLogs.AddAsync(
                    new TradeLog { 
                        TradeOrderId = order.Id, 
                        Log = $"kite error: {e.Message}...", 
                        Timestamp = DateTime.Now }
                 ).ConfigureAwait(false);
            }

            bool checkingProximity = true;
            // check proximity until order is fully filled. to modify the target order in case tick price is closing in on it.
            while (checkingProximity)
            {
                var orderHistory = GetLatestOrderUpdate(orderId_ent);
                if (orderHistory.FilledQuantity == quantity)
                {
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = order.Id,
                            Log = $"entry order filled...",
                            Timestamp = DateTime.Now
                        }
                     ).ConfigureAwait(false);
                    break;
                }
                if (ticks[ticks.Count - 1].High >= proximity)
                {
                    if (orderHistory.FilledQuantity != quantity)
                    {
                        kite.ModifyOrder(
                            orderId_tar,
                            Quantity: orderHistory.FilledQuantity.ToString()
                        );

                        await tradeDbContext.TradeLogs.AddAsync(
                            new TradeLog
                            {
                                TradeOrderId = order.Id,
                                Log = $"target modified quantity = {orderHistory.FilledQuantity}...",
                                Timestamp = DateTime.Now
                            }
                         ).ConfigureAwait(false);
                    }
                }
            }
        }

        // place stop loss
        private async Task PlaceStopLoss(TradeOrder order)
        {
            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = order.Id,
                    Log = $"doing stop loss logic...",
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);

            // if entry order average price, is less than the stoploss input, then sleep for a minute for ticks to come in, so we can set the stoploss as the low. 
            if (is_pre_slm_cancelled)
            {
                await tradeDbContext.TradeLogs.AddAsync(
                    new TradeLog
                    {
                        TradeOrderId = order.Id,
                        Log = $"average price is less than stop loss, waiting 1 min for data...",
                        Timestamp = DateTime.Now
                    }
                 ).ConfigureAwait(false);

                if (!slRejected)
                {
                    DateTime now = DateTime.Now;
                    DateTime min = now.AddSeconds(55);
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

                bool isBullish = false;
                if (ticks[ticks.Count - 1].Open < ticks[ticks.Count - 1].Close)
                {
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = order.Id,
                            Log = $"{order.Instrument.TradingSymbol} is bullish...",
                            Timestamp = DateTime.Now
                        }
                     ).ConfigureAwait(false);
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

                            await tradeDbContext.TradeLogs.AddAsync(
                                new TradeLog
                                {
                                    TradeOrderId = order.Id,
                                    Log = $"SLM order placed...",
                                    Timestamp = DateTime.Now
                                }
                             ).ConfigureAwait(false);
                        }
                        catch (KiteException e)
                        {
                            await tradeDbContext.TradeLogs.AddAsync(
                                new TradeLog
                                {
                                    TradeOrderId = order.Id,
                                    Log = $"kite error: {e.Message}...",
                                    Timestamp = DateTime.Now
                                }
                             ).ConfigureAwait(false);
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

                            await tradeDbContext.TradeLogs.AddAsync(
                                new TradeLog
                                {
                                    TradeOrderId = order.Id,
                                    Log = $"SLM order placed...",
                                    Timestamp = DateTime.Now
                                }
                             ).ConfigureAwait(false);
                        }
                        catch (KiteException e)
                        {
                            await tradeDbContext.TradeLogs.AddAsync(
                                new TradeLog
                                {
                                    TradeOrderId = order.Id,
                                    Log = $"kite error: {e.Message}...",
                                    Timestamp = DateTime.Now
                                }
                             ).ConfigureAwait(false);
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
            ticker.Subscribe(Tokens: new UInt32[] { order.Instrument.Token });
            ticker.SetMode(Tokens: new UInt32[] { order.Instrument.Token }, Mode: Constants.MODE_FULL);

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

        private async Task PlaceEntry(TradeOrder order)
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

            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = order.Id,
                    Log = $"entry order placed...",
                    Timestamp = DateTime.Now
                }
             ).ConfigureAwait(false);
        }

        private async Task PlacePreSLM(TradeOrder order)
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

            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = order.Id,
                    Log = $"pre SLM order placed...",
                    Timestamp = DateTime.Now
                }
             ).ConfigureAwait(false);
        }

        private async Task<string> CheckOrderStatuses()
        {
            // get order update
            Order orderHistoryQ = GetLatestOrderUpdate(orderId_ent);
            Order orderHistoryA = new Order();
            if (!is_pre_slm_cancelled)
            {
                orderHistoryA = GetLatestOrderUpdate(orderId_slm);
            }

            // check if entry status is rejected
            if (orderHistoryQ.Status == "REJECTED")
            {
                await tradeDbContext.TradeLogs.AddAsync(
                    new TradeLog
                    {
                        TradeOrderId = orderId,
                        Log = $"entry order rejected...",
                        Timestamp = DateTime.Now
                    }
                 ).ConfigureAwait(false);
                if (!is_pre_slm_cancelled)
                {
                    // if slm is not rejected then cancel it
                    if (orderHistoryA.Status != "REJECTED")
                    {
                        await tradeDbContext.TradeLogs.AddAsync(
                            new TradeLog
                            {
                                TradeOrderId = orderId,
                                Log = $"slm order rejected...",
                                Timestamp = DateTime.Now
                            }
                         ).ConfigureAwait(false);
                        kite.CancelOrder(orderId_slm, GetCurrentVariety());
                    }
                }
                return "entrypoint";
            }
            if (orderHistoryA.Status == "REJECTED")
            {
                await tradeDbContext.TradeLogs.AddAsync(
                    new TradeLog
                    {
                        TradeOrderId = orderId,
                        Log = $"entry order rejected...",
                        Timestamp = DateTime.Now
                    }
                 ).ConfigureAwait(false);
                if (!is_pre_slm_cancelled)
                {
                    // if slm is not rejected then cancel it
                    if (orderHistoryQ.Status != "REJECTED")
                    {
                        await tradeDbContext.TradeLogs.AddAsync(
                            new TradeLog
                            {
                                TradeOrderId = orderId,
                                Log = $"entry order rejected...",
                                Timestamp = DateTime.Now
                            }
                         ).ConfigureAwait(false);
                        kite.CancelOrder(orderId_ent, GetCurrentVariety());
                    }
                }
                return "entrypoint";
            }
            return "continue";
        }

        private async Task CheckIfFilling(TradeOrder order)
        {
            var variety = GetCurrentVariety();
            Order orderHistory = GetLatestOrderUpdate(orderId_ent);
            if (orderHistory.FilledQuantity > 0)
            {
                await tradeDbContext.TradeLogs.AddAsync(
                    new TradeLog
                    {
                        TradeOrderId = orderId,
                        Log = $"entry order filling...",
                        Timestamp = DateTime.Now
                    }
                 ).ConfigureAwait(false);

                if (exitTransactionType == "SELL")
                {
                    if (orderHistory.AveragePrice < order.StopLoss)
                    {
                        try
                        {
                            kite.CancelOrder(orderId_slm, variety);
                            await tradeDbContext.TradeLogs.AddAsync(
                                new TradeLog
                                {
                                    TradeOrderId = orderId,
                                    Log = $"slm order cancelled...",
                                    Timestamp = DateTime.Now
                                }
                             ).ConfigureAwait(false);
                            is_pre_slm_cancelled = true;
                        }
                        catch (KiteException e)
                        {
                            await tradeDbContext.TradeLogs.AddAsync(
                                new TradeLog
                                {
                                    TradeOrderId = orderId,
                                    Log = $"kite error {e.Message}...",
                                    Timestamp = DateTime.Now
                                }
                             ).ConfigureAwait(false);
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
                            await tradeDbContext.TradeLogs.AddAsync(
                                new TradeLog
                                {
                                    TradeOrderId = orderId,
                                    Log = $"slm order cancelled...",
                                    Timestamp = DateTime.Now
                                }
                             ).ConfigureAwait(false);
                            is_pre_slm_cancelled = true;
                        }
                        catch (KiteException e)
                        {
                            await tradeDbContext.TradeLogs.AddAsync(
                                new TradeLog
                                {
                                    TradeOrderId = orderId,
                                    Log = $"kite error: {e.Message}...",
                                    Timestamp = DateTime.Now
                                }
                             ).ConfigureAwait(false);
                        }
                    }
                }
                isOrderFilling = true;
            }
        }
        private async Task<bool> IsMarketOpen()
        {
            DateTime GMT = DateTime.Now;
            DateTime IST = GMT.AddHours(5).AddMinutes(30);
            DateTime opening = new DateTime(IST.Year, IST.Month, IST.Day, 9, 15, 00);
            DateTime closing = opening.AddHours(6).AddMinutes(15);
            if (DateTime.Compare(IST, opening) >= 0)
            {
                if (DateTime.Compare(IST, closing) < 0)
                {
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = orderId,
                            Log = $"market is open...",
                            Timestamp = DateTime.Now
                        }
                     ).ConfigureAwait(false);
                    return true;
                }
            }
            return false;
        }
        private async Task<bool> IsPreMarketOpen()
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
                    await tradeDbContext.TradeLogs.AddAsync(
                        new TradeLog
                        {
                            TradeOrderId = orderId,
                            Log = $"pre market opening...",
                            Timestamp = DateTime.Now
                        }
                     ).ConfigureAwait(false);
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
            DateTime opening1 = new DateTime(IST1.Year, IST1.Month, IST1.Day, 9, 15, 0);
            DateTime closing1 = opening1.AddHours(6).AddMinutes(15);

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

        private static decimal RoundUp(decimal value, decimal step)
        {
            var multiplicand = Math.Ceiling(value / step);
            return step * multiplicand;
        }

        private static decimal RoundDown(decimal value, decimal step)
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
        private async void OnError(string message)
        {
            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = orderId,
                    Log = $"error: {message}...",
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);
        }
        private async void OnClose()
        {
            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = orderId,
                    Log = $"ticker connection closed...",
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);
        }
        private async void OnReconnect()
        {
            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = orderId,
                    Log = $"ticker connection reconnected...",
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);
        }
        private async void OnNoReconnect()
        {
            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = orderId,
                    Log = $"ticker connection failed to reconnect...",
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);
        }
        private async void OnConnect()
        {
            await tradeDbContext.TradeLogs.AddAsync(
                new TradeLog
                {
                    TradeOrderId = orderId,
                    Log = $"ticker connected...",
                    Timestamp = DateTime.Now
                }
            ).ConfigureAwait(false);
        }
    }
}
