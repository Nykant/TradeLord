using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
        // private class variables
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IKiteService kiteService;
        private readonly IConfiguration configuration;
        private readonly ITradeOrderHelper orderHelper;
        private readonly ITradeLogHelper logHelper;
        private readonly OrderHub orderHub;

        private Order Entry { get; set; }
        private Order SLM { get; set; }
        private Order Target { get; set; }
        private Kite Kite { get; set; }
        private Tick Tick { get; set; }

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
        private bool isTargetHit;
        private bool finished;
        private string statusCheck;

        // class constructor
        public OrderWork(IConfiguration configuration, IHttpContextAccessor contextAccessor, IKiteService kiteService, ITradeOrderHelper tradeOrderHelper, ITradeLogHelper tradeLogHelper, OrderHub orderHub)
        {
            // constructor dependency injection
            this.configuration = configuration;
            _contextAccessor = contextAccessor;
            this.kiteService = kiteService;
            orderHelper = tradeOrderHelper;
            logHelper = tradeLogHelper;
            this.orderHub = orderHub;

            //set values when constructor initialized
            isPreMarketOpen = false;
            isMarketOpen = false;
            targetplaced = false;
            hit = false;
            isOrderFilling = false;
            is_pre_slm_cancelled = false;
            squareOff = false;
            regularSLMplaced = false;
            slRejected = false;
            isTargetHit = false;
            statusCheck = null;
        }

        public async Task StartWork(TradeOrder order)
        {
            await logHelper.AddLog(order.Id, $"order starting...").ConfigureAwait(false);
            Ticker ticker = Initialize(order, orderHub);

            order.Status = Status.RUNNING;
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

            order.Quantity = quantity;
            await orderHelper.UpdateTradeOrder(order).ConfigureAwait(false);

            // wait untill we get a tick
            while (Tick.LastPrice == 0)
            {
                await Task.Delay(200);
            }

            variety = await GetCurrentVariety();

            try
            {
                await PlaceEntry(order);
            }
            catch (KiteException e)
            {
                await logHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                goto Ending;
            }

            if (exitTransactionType == "SELL")
            {
                // if last price is more than stop loss then place slm
                if (Tick.LastPrice > order.StopLoss)
                {
                    try
                    {
                        await PlacePreSLM(order);
                    }
                    catch (KiteException e)
                    {
                        await logHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        await CancelEntry();
                        goto Ending;
                    }
                }
                // else tell app that slm order was cancelled, which means it has to find a new one after 1 min
                else
                {
                    await logHelper.AddLog(order.Id, $"slm was cancelled...").ConfigureAwait(false);
                    is_pre_slm_cancelled = true;
                }
            }
            else
            {
                // if last price is more than stop loss then place slm
                if (Tick.LastPrice < order.StopLoss)
                {
                    try
                    {
                        await PlacePreSLM(order);
                    }
                    catch (KiteException e)
                    {
                        await logHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        await CancelEntry();
                        goto Ending;
                    }
                }
                // else tell app that slm order was cancelled, which means it has to find a new one after 1 min
                else
                {
                    await logHelper.AddLog(order.Id, $"slm was cancelled...").ConfigureAwait(false);
                    is_pre_slm_cancelled = true;
                }
            }

            // do while pre market is not open
            do
            {
                await CheckOrderStatuses(order).ConfigureAwait(false);

                if (await IsPreMarketOpen())
                {
                    isPreMarketOpen = true;
                    break;
                }

                if (order.TokenSource.IsCancellationRequested)
                {
                    goto Stopping;
                }

                if (finished)
                {
                    goto Ending;
                }

                await Task.Delay(5000);
            }
            while (!isPreMarketOpen);

            // do while pre market is open
            do
            {
                await CheckOrderStatuses(order).ConfigureAwait(false);

                if (await IsMarketOpen())
                {
                    isMarketOpen = true;
                    break;
                }

                if (!isOrderFilling)
                {
                    await Task.Run(()=>CheckIfFilling(order)).ConfigureAwait(false);
                }

                if (finished)
                {
                    goto Ending;
                }

                await Task.Delay(500);
            }
            while (!isMarketOpen);

            await Task.Run(() =>
            {
                Parallel.Invoke(async () => await PlaceTarget(order), async () => await PlaceStopLoss(order));
            }).ConfigureAwait(false);

            await logHelper.AddLog(order.Id, $"monitoring orders...").ConfigureAwait(false);

            // monitoring the orders
            while (true)
            {
                await CheckOrderStatuses(order).ConfigureAwait(false);

                if (!is_pre_slm_cancelled)
                {
                    if (SLM.FilledQuantity > 0)
                    {
                        await logHelper.AddLog(order.Id, $"slm hit...").ConfigureAwait(false);
                        Kite.CancelOrder(orderId_tar);
                        goto Ending;
                    }
                }
                else if (regularSLMplaced)
                {
                    if (SLM.FilledQuantity > 0)
                    {
                        await logHelper.AddLog(order.Id, $"slm hit...").ConfigureAwait(false);
                        Kite.CancelOrder(orderId_tar);
                        goto Ending;
                    }
                }

                if (targetplaced)
                {
                    if (!isTargetHit)
                    {
                        if (Target.FilledQuantity > 0)
                        {
                            isTargetHit = true;
                            await logHelper.AddLog(order.Id, $"target hit...").ConfigureAwait(false);
                            if (!is_pre_slm_cancelled)
                            {
                                Kite.CancelOrder(orderId_slm);
                            }
                            else if (regularSLMplaced)
                            {
                                Kite.CancelOrder(orderId_slm);
                            }

                            await Task.Run(() => WatchingTarget(order)).ConfigureAwait(false);
                        }
                    }
                }

                if (order.TokenSource.IsCancellationRequested)
                {
                    if (!isTargetHit)
                    {
                        goto Stopping;
                    }
                }

                if (finished)
                {
                    goto Ending;
                }
            }

            // go to when trade order is stopped
            Stopping:

            await CancelEntry();
            await CancelStopLoss();
            await CancelTarget();

            SquareOff(order);

            // go to when trade order is ending
            Ending:

            ticker.UnSubscribe(new[] { order.Instrument.Token });
            ticker.DisableReconnect();
            ticker.Close();
        }

        // place target
        private async Task PlaceTarget(TradeOrder order)
        {
            await logHelper.AddLog(order.Id, $"doing target logic...").ConfigureAwait(false);

            while (!isOrderFilling)
            {
                await Task.Run(() => CheckIfFilling(order));
            }

            decimal proximity = 0;
            if (exitTransactionType == "SELL")
            {
                target = (order.RxR * zonewidth) + Entry.AveragePrice;
                proximity = ((target - Entry.AveragePrice) * (decimal)0.8)
                                            + Entry.AveragePrice;
            }
            else
            {
                target = Entry.AveragePrice - (order.RxR * zonewidth);
                proximity = Entry.AveragePrice 
                    - ((Entry.AveragePrice - target) * (decimal)0.8);
            }

            try
            {
                Dictionary<string, dynamic> orderReponse = Kite.PlaceOrder(
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

                await logHelper.AddLog(order.Id, $"target placed...").ConfigureAwait(false);
            }
            catch (KiteException e)
            {
                await logHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
            }

            bool checkingProximity = true;
            // check proximity until order is fully filled. to modify the target order in case tick price is closing in on it.
            while (checkingProximity)
            {
                if (Entry.FilledQuantity == quantity)
                {
                    await logHelper.AddLog(order.Id, $"entry order filled...").ConfigureAwait(false);
                    break;
                }
                if (Tick.High >= proximity)
                {
                    if (Entry.FilledQuantity != quantity)
                    {
                        Kite.ModifyOrder(
                            orderId_tar,
                            Quantity: Entry.FilledQuantity.ToString()
                        );

                        await logHelper.AddLog(order.Id, $"target modified quantity = {Entry.FilledQuantity}...").ConfigureAwait(false);

                    }
                }
            }
        }

        // place stop loss
        private async Task PlaceStopLoss(TradeOrder order)
        {
            await logHelper.AddLog(order.Id, $"doing stoploss logic...").ConfigureAwait(false);

            // if entry order average price, is less than the stoploss input, then sleep for a minute for ticks to come in, so we can set the stoploss as the low. 
            if (is_pre_slm_cancelled)
            {
                if (!slRejected)
                {
                    await logHelper.AddLog(order.Id, $"average price is less than stop loss, waiting 1 min for data...").ConfigureAwait(false);

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
                        if (isTargetHit)
                        {
                            goto Ending;
                        }
                    }
                }

                if (exitTransactionType == "BUY")
                {
                    triggerPrice = Tick.High;
                    triggerPrice = triggerPrice * (decimal)1.00015;
                    triggerPrice = RoundUp(triggerPrice, (decimal)0.05);
                }
                else
                {
                    triggerPrice = Tick.Low;
                    triggerPrice = triggerPrice * (decimal)0.99985;
                    triggerPrice = RoundDown(triggerPrice, (decimal)0.05);
                }

                bool isBullish = false;
                if (Tick.Open < Tick.Close)
                {
                    await logHelper.AddLog(order.Id, $"{order.Instrument.TradingSymbol} is bullish...").ConfigureAwait(false);
                    isBullish = true;
                }

                if(exitTransactionType == "SELL")
                {
                    if (isBullish)
                    {
                        try
                        {
                            Dictionary<string, dynamic> response = Kite.PlaceOrder(
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

                            await logHelper.AddLog(order.Id, $"SLM order placed...").ConfigureAwait(false);
                        }
                        catch (KiteException e)
                        {
                            await logHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        Dictionary<string, dynamic> placeOrderResponse = Kite.PlaceOrder(
                             Exchange: order.Instrument.Exchange,
                             TradingSymbol: order.Instrument.TradingSymbol,
                             TransactionType: exitTransactionType,
                             Quantity: quantity,
                             Product: Constants.PRODUCT_MIS,
                             OrderType: Constants.ORDER_TYPE_MARKET,
                             Validity: Constants.VALIDITY_DAY,
                             Variety: Constants.VARIETY_REGULAR
                         );

                        Kite.CancelOrder(orderId_tar);

                        await logHelper.AddLog(order.Id, $"squared off...").ConfigureAwait(false);

                        finished = true;
                    }
                }
                else
                {
                    if (!isBullish)
                    {
                        try
                        {
                            Dictionary<string, dynamic> response = Kite.PlaceOrder(
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

                            await logHelper.AddLog(order.Id, $"SLM order placed...").ConfigureAwait(false);
                        }
                        catch (KiteException e)
                        {
                            await logHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        Dictionary<string, dynamic> placeOrderResponse = Kite.PlaceOrder(
                         Exchange: order.Instrument.Exchange,
                         TradingSymbol: order.Instrument.TradingSymbol,
                         TransactionType: exitTransactionType,
                         Quantity: quantity,
                         Product: Constants.PRODUCT_MIS,
                         OrderType: Constants.ORDER_TYPE_MARKET,
                         Validity: Constants.VALIDITY_DAY,
                         Variety: Constants.VARIETY_REGULAR
                        );

                        Kite.CancelOrder(orderId_tar);

                        await logHelper.AddLog(order.Id, $"squared off...").ConfigureAwait(false);

                        finished = true;
                    }
                }
            }
            Ending:;
        }

        private Ticker Initialize(TradeOrder order, OrderHub orderHub)
        {
            orderId = order.Id;
            Kite = kiteService.GetKite();

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
            Dictionary<string, dynamic> response = Kite.PlaceOrder(
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

            await logHelper.AddLog(order.Id, $"entry order placed...").ConfigureAwait(false);
        }

        private async Task PlacePreSLM(TradeOrder order)
        {
            // place slm order
            Dictionary<string, dynamic> responseS = Kite.PlaceOrder(
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

            await logHelper.AddLog(order.Id, $"slm order placed...").ConfigureAwait(false);
        }

        private Task CheckOrderStatuses(TradeOrder order)
        {
            return Task.Run(async () =>
            {
                var variety = await GetCurrentVariety();

                // check if entry status is rejected
                if (Entry.Status == "REJECTED")
                {
                    await logHelper.AddLog(orderId, $"entry order rejected...").ConfigureAwait(false);
                    if (!is_pre_slm_cancelled)
                    {
                        // if slm is not rejected then cancel it
                        if (SLM.Status != "REJECTED")
                        {
                            Kite.CancelOrder(orderId_slm, variety);
                            await logHelper.AddLog(orderId, $"slm order cancelled...").ConfigureAwait(false);
                        }
                    }
                    else if (regularSLMplaced)
                    {
                        // if slm is not rejected then cancel it
                        if (SLM.Status != "REJECTED")
                        {
                            Kite.CancelOrder(orderId_slm, variety);
                            await logHelper.AddLog(orderId, $"slm order cancelled...").ConfigureAwait(false);
                        }
                    }
                    finished = true;
                    goto End;
                }

                if (!is_pre_slm_cancelled)
                {
                    if (SLM.Status == "REJECTED")
                    {
                        await logHelper.AddLog(orderId, $"slm order rejected...").ConfigureAwait(false);

                        if (Entry.Status != "REJECTED")
                        {
                            await logHelper.AddLog(orderId, $"entry order cancelled...").ConfigureAwait(false);
                            Kite.CancelOrder(orderId_ent, variety);
                        }
                        finished = true;
                        goto End;
                    }
                }

                else if (regularSLMplaced)
                {
                    if (SLM.Status == "REJECTED")
                    {
                        await logHelper.AddLog(orderId, $"slm order rejected...").ConfigureAwait(false);
                        slRejected = true;
                        regularSLMplaced = false;
                        await Task.Run(async () => await PlaceStopLoss(order)).ConfigureAwait(false);
                    }
                }

                if (targetplaced)
                {
                    if (Target.Status == "REJECTED")
                    {
                        await logHelper.AddLog(orderId, $"target order rejected...").ConfigureAwait(false);
                        targetplaced = false;
                        await Task.Run(async () => await PlaceTarget(order)).ConfigureAwait(false);
                    }
                }
                End:;
            });
        }

        private async Task WatchingTarget(TradeOrder order)
        {
            while (!finished)
            {
                if (exitTransactionType == "SELL")
                {
                    if (Tick.LastPrice < (Entry.AveragePrice + (0.5m * (target - Entry.AveragePrice))))
                    {
                        await Task.Run(() => Parallel.Invoke(async () => await CancelEntry(), async () => await CancelStopLoss(), async () => await CancelTarget()));

                        await Task.Run(() =>
                        {
                            var squareOffQuantity = Entry.FilledQuantity - Target.FilledQuantity;

                            Kite.PlaceOrder(
                                 Exchange: order.Instrument.Exchange,
                                 TradingSymbol: order.Instrument.TradingSymbol,
                                 TransactionType: exitTransactionType,
                                 Quantity: squareOffQuantity,
                                 Product: Constants.PRODUCT_MIS,
                                 OrderType: Constants.ORDER_TYPE_MARKET,
                                 Validity: Constants.VALIDITY_DAY,
                                 Variety: Constants.VARIETY_REGULAR
                            );

                            finished = true;
                        }).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (Tick.LastPrice > (Entry.AveragePrice - (0.5m * (target - Entry.AveragePrice))))
                    {
                        Parallel.Invoke(async () => await CancelEntry(), async () => await CancelStopLoss(), async () => await CancelTarget());

                        await Task.Run(() =>
                        {
                            var squareOffQuantity = Entry.FilledQuantity - Target.FilledQuantity;

                            Kite.PlaceOrder(
                                 Exchange: order.Instrument.Exchange,
                                 TradingSymbol: order.Instrument.TradingSymbol,
                                 TransactionType: exitTransactionType,
                                 Quantity: squareOffQuantity,
                                 Product: Constants.PRODUCT_MIS,
                                 OrderType: Constants.ORDER_TYPE_MARKET,
                                 Validity: Constants.VALIDITY_DAY,
                                 Variety: Constants.VARIETY_REGULAR
                            );

                            finished = true;
                        }).ConfigureAwait(false);
                    }
                }

                if (Entry.FilledQuantity == Target.FilledQuantity)
                {
                    finished = true;
                    break;
                }
            }
        }
        private async Task CheckIfFilling(TradeOrder order)
        {
            var variety = await GetCurrentVariety();

            if (Entry.FilledQuantity > 0)
            {
                await logHelper.AddLog(orderId, $"entry order filling...").ConfigureAwait(false);

                if (exitTransactionType == "SELL")
                {
                    if (Entry.AveragePrice < order.StopLoss)
                    {
                        try
                        {
                            Kite.CancelOrder(orderId_slm, variety);
                            await logHelper.AddLog(orderId, $"slm order cancelled...").ConfigureAwait(false);
                            is_pre_slm_cancelled = true;
                        }
                        catch (KiteException e)
                        {
                            await logHelper.AddLog(orderId, $"kite error: {e.Message}...").ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    if (Entry.AveragePrice > order.StopLoss)
                    {
                        try
                        {
                            Kite.CancelOrder(orderId_slm, variety);
                            await logHelper.AddLog(orderId, $"slm order cancelled...").ConfigureAwait(false);
                            is_pre_slm_cancelled = true;
                        }
                        catch (KiteException e)
                        {
                            await logHelper.AddLog(orderId, $"kite error: {e.Message}...").ConfigureAwait(false);
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
                    await logHelper.AddLog(orderId, $"market is open...").ConfigureAwait(false);
                    return true;
                }
            }
            return false;
        }

        private async Task CancelEntry()
        {
            variety = await GetCurrentVariety();

            if (Entry.Status != "COMPLETE" && Entry.Status != "REJECTED")
            {
                try
                {
                    Kite.CancelOrder(orderId_ent, variety);
                    await logHelper.AddLog(orderId, $"entry order cancelled...").ConfigureAwait(false);
                }
                catch (KiteException e)
                {
                    await logHelper.AddLog(orderId, $"kite error: {e.Message}...").ConfigureAwait(false);
                }
            }
        }

        private async Task CancelStopLoss()
        {
            variety = await GetCurrentVariety();

            if (!is_pre_slm_cancelled)
            {
                if (SLM.Status != "COMPLETE" && SLM.Status != "REJECTED")
                {
                    try
                    {
                        Kite.CancelOrder(orderId_slm, variety);
                        await logHelper.AddLog(orderId, $"slm order cancelled...").ConfigureAwait(false);
                    }
                    catch (KiteException e)
                    {
                        await logHelper.AddLog(orderId, $"kite error: {e.Message}...").ConfigureAwait(false);
                    }
                }
            }
            else if (regularSLMplaced)
            {
                if (SLM.Status != "COMPLETE" && SLM.Status != "REJECTED")
                {
                    try
                    {
                        Kite.CancelOrder(orderId_slm, variety);
                        await logHelper.AddLog(orderId, $"slm order cancelled...").ConfigureAwait(false);
                    }
                    catch (KiteException e)
                    {
                        await logHelper.AddLog(orderId, $"kite error: {e.Message}...").ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task CancelTarget()
        {
            variety = await GetCurrentVariety();

            if (targetplaced)
            {
                if (Target.Status != "COMPLETE" && Target.Status != "REJECTED")
                {
                    try
                    {
                        Kite.CancelOrder(orderId_tar, variety);
                        await logHelper.AddLog(orderId, $"target order cancelled...").ConfigureAwait(false);
                    }
                    catch (KiteException e)
                    {
                        await logHelper.AddLog(orderId, $"kite error: {e.Message}...").ConfigureAwait(false);
                    }
                }
            }
        }
        
        private void SquareOff(TradeOrder order)
        {
            if(Entry.FilledQuantity > 0)
            {
                Dictionary<string, dynamic> placeOrderResponse = Kite.PlaceOrder(
                     Exchange: order.Instrument.Exchange,
                     TradingSymbol: order.Instrument.TradingSymbol,
                     TransactionType: exitTransactionType,
                     Quantity: Entry.FilledQuantity,
                     Product: Constants.PRODUCT_MIS,
                     OrderType: Constants.ORDER_TYPE_MARKET,
                     Validity: Constants.VALIDITY_DAY,
                     Variety: Constants.VARIETY_REGULAR
                );
            }
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
                    await logHelper.AddLog(orderId, $"pre market is opening...").ConfigureAwait(false);
                    return true;
                }
            }
            return false;
        }

        private async Task<string> GetCurrentVariety()
        {
            return await Task.Run(() =>
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
            });
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
            Tick = tickData;
        }
        private void OnOrderUpdate(Order orderData)
        {
            if(orderData.OrderId == orderId_ent)
            {
                Entry = orderData;
            }
            else if(orderData.OrderId == orderId_slm)
            {
                SLM = orderData;
            }
            else if(orderData.OrderId == orderId_tar)
            {
                Target = orderData;
            }
        }
        private async void OnError(string message)
        {
            await logHelper.AddLog(orderId, $"error: {message}...").ConfigureAwait(false);
        }
        private async void OnClose()
        {
            await logHelper.AddLog(orderId, $"ticker connection closed...").ConfigureAwait(false);
        }
        private async void OnReconnect()
        {
            await logHelper.AddLog(orderId, $"ticker connection reconnected...").ConfigureAwait(false);
        }
        private async void OnNoReconnect()
        {
            await logHelper.AddLog(orderId, $"ticker connection failed to reconnect...").ConfigureAwait(false);
        }
        private async void OnConnect()
        {
            await logHelper.AddLog(orderId, $"ticker connected...").ConfigureAwait(false);
        }
    }
}
