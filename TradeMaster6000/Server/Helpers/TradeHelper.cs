using KiteConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Server.Models;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Helpers
{
    public class TradeHelper : ITradeHelper
    {
        private ITradeLogHelper LogHelper { get; set; }
        private ITickerService TickService { get; set; }
        private ITimeHelper TimeHelper { get; set; }
        private ITickDbHelper TickDbHelper { get; set; }
        private readonly IKiteService kiteService;
        public TradeHelper(ITradeLogHelper tradeLogHelper, ITickerService tickService, IKiteService kiteService, ITimeHelper timeHelper, ITickDbHelper tickDbHelper)
        {
            LogHelper = tradeLogHelper;
            TickService = tickService;
            TimeHelper = timeHelper;
            this.kiteService = kiteService;
            TickDbHelper = tickDbHelper;
        }

        public async Task CancelEntry(TradeOrder order)
        {
            var variety = TimeHelper.GetCurrentVariety();
            OrderUpdate entry;

            Stopwatch stopwatch = new ();
            stopwatch.Start();
            while (true) 
            {
                entry = await TickService.GetOrder(order.EntryId, order.Username);
                if (entry.Status == "COMPLETE" || entry.Status == "REJECTED")
                {
                    break;
                }
                else
                {
                    try
                    {
                        var kite = kiteService.GetKite(order.Username);
                        kite.CancelOrder(order.EntryId, variety);
                        await LogHelper.AddLog(order.Id, $"entry order cancelled...").ConfigureAwait(false);
                        break;
                    }
                    catch (KiteException e)
                    {
                        await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                    }
                }

                if (stopwatch.Elapsed.Seconds < 30)
                {
                    stopwatch.Stop();
                    break;
                }
                await Task.Delay(1000);
            }
        }

        public async Task CancelStopLoss(TradeOrder order)
        {
            if (!order.PreSLMCancelled)
            {
                var variety = TimeHelper.GetCurrentVariety();
                OrderUpdate slm;

                Stopwatch stopwatch = new ();
                stopwatch.Start();
                while (true)
                {
                    slm = await TickService.GetOrder(order.SLMId, order.Username);
                    if(slm.Status == "COMPLETE" || slm.Status == "REJECTED")
                    {
                        break;
                    }
                    else
                    {
                        try
                        {
                            var kite = kiteService.GetKite(order.Username);
                            kite.CancelOrder(order.SLMId, variety);
                            await LogHelper.AddLog(order.Id, $"slm order cancelled...").ConfigureAwait(false);
                            break;
                        }
                        catch (KiteException e)
                        {
                            await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        }
                    }

                    if (stopwatch.Elapsed.Seconds < 30)
                    {
                        stopwatch.Stop();
                        break;
                    }
                    await Task.Delay(1000);
                }
            }
            else if (order.RegularSlmPlaced)
            {
                var variety = TimeHelper.GetCurrentVariety();
                OrderUpdate slm;

                Stopwatch stopwatch = new ();
                stopwatch.Start();
                while (true)
                {
                    slm = await TickService.GetOrder(order.SLMId, order.Username);
                    if (slm.Status == "COMPLETE" || slm.Status == "REJECTED")
                    {
                        break;
                    }
                    else
                    {
                        try
                        {
                            var kite = kiteService.GetKite(order.Username);
                            kite.CancelOrder(order.SLMId, variety);
                            await LogHelper.AddLog(order.Id, $"slm order cancelled...").ConfigureAwait(false);
                            break;
                        }
                        catch (KiteException e)
                        {
                            await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        }
                    }
                    if (stopwatch.Elapsed.Seconds < 30)
                    {
                        stopwatch.Stop();
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
                OrderUpdate targetO;
                var variety = TimeHelper.GetCurrentVariety();

                Stopwatch stopwatch = new ();
                stopwatch.Start();
                while (true)
                {
                    targetO = await TickService .GetOrder(order.TargetId, order.Username);
                    if (targetO.Status == "COMPLETE" || targetO.Status == "REJECTED")
                    {
                        break;
                    }
                    else
                    {
                        try
                        {
                            var kite = kiteService.GetKite(order.Username);
                            kite.CancelOrder(order.TargetId, variety);
                            await LogHelper.AddLog(order.Id, $"target order cancelled...").ConfigureAwait(false);
                            break;
                        }
                        catch (KiteException e)
                        {
                            await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                        }
                    }
                    if (stopwatch.Elapsed.Seconds < 30)
                    {
                        stopwatch.Stop();
                        break;
                    }

                    await Task.Delay(1000);
                }
            }
        }

        public async Task SquareOff(TradeOrder order)
        {
            var entry = await TickService.GetOrder(order.EntryId, order.Username);
            if (entry.FilledQuantity > 0)
            {
                var kite = kiteService.GetKite(order.Username);
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
                var variety = TimeHelper.GetCurrentVariety();

                var kite = kiteService.GetKite(order.Username);
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
            var tick = await TickDbHelper.GetLast(order.Instrument.Token);
            var ltp = tick.LTP;
            if (order.ExitTransactionType == "SELL")
            {
                // if last price is more than stop loss then place slm
                if (ltp > order.StopLoss)
                {
                    try
                    {
                        var variety = TimeHelper.GetCurrentVariety();
                        // place slm order
                        var kite = kiteService.GetKite(order.Username);
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
                        await Task.Run(async()=> await CancelEntry(order)).ConfigureAwait(false);
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
                if (ltp < order.StopLoss)
                {
                    try
                    {
                        var variety = TimeHelper.GetCurrentVariety();
                        // place slm order
                        var kite = kiteService.GetKite(order.Username);
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
                        await Task.Run(async()=> await CancelEntry(order)).ConfigureAwait(false);
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
