﻿using KiteConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Helpers
{
    public class TradeHelper : ITradeHelper
    {
        private ITradeLogHelper LogHelper { get; set; }
        private ITickerService TickService { get; set; }
        private ITimeHelper TimeHelper { get; set; }
        private readonly IKiteService kiteService;
        public TradeHelper(ITradeLogHelper tradeLogHelper, ITickerService tickService, IKiteService kiteService, ITimeHelper timeHelper)
        {
            LogHelper = tradeLogHelper;
            TickService = tickService;
            TimeHelper = timeHelper;
            this.kiteService = kiteService;
        }

        public async Task CancelEntry(TradeOrder order)
        {
            bool cancelled = false;
            var variety = await Task.Run(() => TimeHelper.GetCurrentVariety());
            Order entry = new ();

            Stopwatch stopwatch = new ();
            stopwatch.Start();
            while (!cancelled) 
            {
                entry = await Task.Run(() => TickService.GetOrder(order.EntryId));
                if (entry.Status == "COMPLETE" || entry.Status == "REJECTED")
                {
                    cancelled = true;
                    break;
                }
                else
                {
                    try
                    {
                        var kite = kiteService.GetKite();
                        kite.CancelOrder(order.EntryId, variety);
                        await LogHelper.AddLog(order.Id, $"entry order cancelled...").ConfigureAwait(false);
                        cancelled = true;
                        break;
                    }
                    catch (KiteException e)
                    {
                        await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                    }
                }

                if (stopwatch.Elapsed.TotalMinutes < 5)
                {
                    stopwatch.Stop();
                    cancelled = true;
                    break;
                }
                await Task.Delay(1000);
            }
        }

        public async Task CancelStopLoss(TradeOrder order)
        {
            if (!order.PreSLMCancelled)
            {
                var variety = await Task.Run(() => TimeHelper.GetCurrentVariety());
                bool cancelled = false;
                Order slm = new ();

                Stopwatch stopwatch = new ();
                stopwatch.Start();
                while (!cancelled)
                {
                    slm = await Task.Run(() => TickService.GetOrder(order.SLMId));
                    if(slm.Status == "COMPLETE" || slm.Status == "REJECTED")
                    {
                        cancelled = true;
                        break;
                    }
                    else
                    {
                        try
                        {
                            var kite = kiteService.GetKite();
                            kite.CancelOrder(order.SLMId, variety);
                            await LogHelper.AddLog(order.Id, $"slm order cancelled...").ConfigureAwait(false);
                            cancelled = true;
                            break;
                        }
                        catch (KiteException e)
                        {
                            await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        }
                    }

                    if (stopwatch.Elapsed.TotalMinutes < 5)
                    {
                        stopwatch.Stop();
                        cancelled = true;
                        break;
                    }
                    await Task.Delay(1000);
                }
            }
            else if (order.RegularSlmPlaced)
            {
                var variety = await Task.Run(() => TimeHelper.GetCurrentVariety());
                bool cancelled = false;
                Order slm = new ();

                Stopwatch stopwatch = new ();
                stopwatch.Start();
                while (!cancelled)
                {
                    slm = await Task.Run(() => TickService.GetOrder(order.SLMId));
                    if (slm.Status == "COMPLETE" || slm.Status == "REJECTED")
                    {
                        cancelled = true;
                        break;
                    }
                    else
                    {
                        try
                        {
                            var kite = kiteService.GetKite();
                            kite.CancelOrder(order.SLMId, variety);
                            await LogHelper.AddLog(order.Id, $"slm order cancelled...").ConfigureAwait(false);
                            cancelled = true;
                            break;
                        }
                        catch (KiteException e)
                        {
                            await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        }
                    }
                    if (stopwatch.Elapsed.TotalMinutes > 5)
                    {
                        stopwatch.Stop();
                        cancelled = true;
                        break;
                    }
                    await Task.Delay(1000);
                }
            }
        }

        public async Task CancelTarget(TradeOrder order)
        {
            if (order.TargetPlaced)
            {
                Order targetO = new ();
                var variety = await Task.Run(() => TimeHelper.GetCurrentVariety());
                bool cancelled = false;

                Stopwatch stopwatch = new ();
                stopwatch.Start();
                while (!cancelled)
                {
                    targetO = await Task.Run(() => TickService.GetOrder(order.TargetId));
                    if (targetO.Status == "COMPLETE" || targetO.Status == "REJECTED")
                    {
                        cancelled = true;
                        break;
                    }
                    else
                    {
                        try
                        {
                            var kite = kiteService.GetKite();
                            kite.CancelOrder(order.TargetId, variety);
                            await LogHelper.AddLog(order.Id, $"target order cancelled...").ConfigureAwait(false);
                            cancelled = true;
                            break;
                        }
                        catch (KiteException e)
                        {
                            await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        }
                    }
                    if (stopwatch.Elapsed.TotalMinutes > 5)
                    {
                        stopwatch.Stop();
                        cancelled = true;
                        break;
                    }
                }
            }
        }

        public async Task SquareOff(TradeOrder order)
        {
            var entry = await Task.Run(() => TickService.GetOrder(order.EntryId));
            if (entry.FilledQuantity > 0)
            {
                var kite = kiteService.GetKite();
                kite.PlaceOrder(
                     Exchange: order.Instrument.Exchange,
                     TradingSymbol: order.Instrument.TradingSymbol,
                     TransactionType: order.ExitTransactionType,
                     Quantity: entry.FilledQuantity,
                     Product: Constants.PRODUCT_MIS,
                     OrderType: Constants.ORDER_TYPE_MARKET,
                     Validity: Constants.VALIDITY_DAY,
                     Variety: Constants.VARIETY_REGULAR
                );

                await LogHelper.AddLog(order.Id, $"squared off...").ConfigureAwait(false);
            }
        }

        public async Task<string> PlaceEntry(TradeOrder order)
        {
            dynamic id;
            try
            {
                var variety = await Task.Run(() => TimeHelper.GetCurrentVariety());

                var kite = kiteService.GetKite();
                // place entry limit order
                Dictionary<string, dynamic> response = kite.PlaceOrder(
                     Exchange: order.Instrument.Exchange,
                     TradingSymbol: order.Instrument.TradingSymbol,
                     TransactionType: order.TransactionType.ToString(),
                     Quantity: order.Quantity,
                     Price: order.Entry,
                     Product: Constants.PRODUCT_MIS,
                     OrderType: Constants.ORDER_TYPE_LIMIT,
                     Validity: Constants.VALIDITY_DAY,
                     Variety: variety
                 );

                id = response["data"]["order_id"];

                await LogHelper.AddLog(order.Id, $"entry order placed...")
                    .ConfigureAwait(false);
            }
            catch (KiteException e)
            {
                await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...")
                    .ConfigureAwait(false);
                return null;
            }
            return id;
        }

        public async Task<string> PlacePreSLM(TradeOrder order)
        {
            var lastPrice = TickService.LastTick(order.Instrument.Token).LastPrice;
            if (order.ExitTransactionType == "SELL")
            {
                // if last price is more than stop loss then place slm
                if (lastPrice > order.StopLoss)
                {
                    try
                    {
                        var variety = await Task.Run(() => TimeHelper.GetCurrentVariety());
                        // place slm order
                        var kite = kiteService.GetKite();
                        Dictionary<string, dynamic> response = kite.PlaceOrder(
                             Exchange: order.Instrument.Exchange,
                             TradingSymbol: order.Instrument.TradingSymbol,
                             TransactionType: order.ExitTransactionType,
                             Quantity: order.Quantity,
                             TriggerPrice: order.StopLoss,
                             Product: Constants.PRODUCT_MIS,
                             OrderType: Constants.ORDER_TYPE_SLM,
                             Validity: Constants.VALIDITY_DAY,
                             Variety: variety
                        );

                        // set id
                        dynamic id = response["data"]["order_id"];

                        await LogHelper.AddLog(order.Id, $"slm order placed...").ConfigureAwait(false);

                        return (string)id;
                    }
                    catch (KiteException e)
                    {
                        await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        await Task.Run(()=>CancelEntry(order)).ConfigureAwait(false);
                        return "cancelled";
                    }
                }
                // else tell app that slm order was cancelled, which means it has to find a new one after 1 min
                else
                {
                    await LogHelper.AddLog(order.Id, $"slm was cancelled...").ConfigureAwait(false);
                    return "cancelled";
                }

            }
            else
            {
                // if last price is more than stop loss then place slm
                if (lastPrice < order.StopLoss)
                {
                    try
                    {
                        var variety = await Task.Run(() => TimeHelper.GetCurrentVariety());
                        // place slm order
                        var kite = kiteService.GetKite();
                        Dictionary<string, dynamic> response = kite.PlaceOrder(
                             Exchange: order.Instrument.Exchange,
                             TradingSymbol: order.Instrument.TradingSymbol,
                             TransactionType: order.ExitTransactionType,
                             Quantity: order.Quantity,
                             TriggerPrice: order.StopLoss,
                             Product: Constants.PRODUCT_MIS,
                             OrderType: Constants.ORDER_TYPE_SLM,
                             Validity: Constants.VALIDITY_DAY,
                             Variety: variety
                        );

                        // set id
                        dynamic id = response["data"]["order_id"];

                        await LogHelper.AddLog(order.Id, $"slm order placed...").ConfigureAwait(false);

                        return (string)id;
                    }
                    catch (KiteException e)
                    {
                        await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        await Task.Run(()=>CancelEntry(order)).ConfigureAwait(false);
                        return "cancelled";
                    }
                }
                // else tell app that slm order was cancelled, which means it has to find a new one after 1 min
                else
                {
                    await LogHelper.AddLog(order.Id, $"slm was cancelled...").ConfigureAwait(false);
                    return "cancelled";
                }
            }
        }
    }
    public interface ITradeHelper
    {
        Task<string> PlacePreSLM(TradeOrder order);
        Task<string> PlaceEntry(TradeOrder order);
        Task SquareOff(TradeOrder order);
        Task CancelTarget(TradeOrder order);
        Task CancelStopLoss(TradeOrder order);
        Task CancelEntry(TradeOrder order);
    }
}
