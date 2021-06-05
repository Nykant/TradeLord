using KiteConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using TradeMaster6000.Server.Helpers;
using System.Diagnostics;

namespace TradeMaster6000.Server.Tasks
{
    public class OrderInstance
    {
        // private class properties
        private ITradeOrderHelper OrderHelper { get; set; }
        private ITradeLogHelper LogHelper { get; set; }
        private ITickerService TickService { get; set; }
        private ITradeHelper TradeHelper { get; set; }
        private ITimeHelper TimeHelper { get; set; }
        private ITargetHelper TargetHelper { get; set; }
        private ISLMHelper SLMHelper { get; set; }
        private IWatchingTargetHelper WatchingTargetHelper { get; set; }
        private ITickDbHelper TickDbHelper { get; set; }
        private TradeOrder TradeOrder { get; set; }

        private bool finished;

        public OrderInstance(IServiceProvider service)
        {
            OrderHelper = service.GetRequiredService<ITradeOrderHelper>();
            LogHelper = service.GetRequiredService<ITradeLogHelper>();
            TickService = service.GetRequiredService<ITickerService>();
            TradeHelper = service.GetRequiredService<ITradeHelper>();
            TargetHelper = service.GetRequiredService<ITargetHelper>();
            SLMHelper = service.GetRequiredService<ISLMHelper>();
            WatchingTargetHelper = service.GetRequiredService<IWatchingTargetHelper>();
            TimeHelper = service.GetRequiredService<ITimeHelper>();
            TickDbHelper = service.GetRequiredService<ITickDbHelper>();

            finished = false;
        }

        public async Task StartWork(TradeOrder order, CancellationToken token)
        {
            await Initialize(order);

            while(!await TickDbHelper.Exists(order.Instrument.Token))
            {
                await Task.Delay(500);
            }

            TradeOrder.EntryId = await TradeHelper.PlaceEntry(TradeOrder);
            if (TradeOrder.EntryId == null)
            {
                goto Ending;
            }

            var response = await TradeHelper.PlacePreSLM(TradeOrder);
            if (response == "cancelled")
            {
                TradeOrder.PreSLMCancelled = true;
            }
            else
            {
                TradeOrder.SLMId = response;
            }

            TradeOrder temp = new();
            do
            {
                await CheckOrderStatuses(token);

                if (temp != TradeOrder)
                {
                    temp = TradeOrder;
                    await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);
                }

                if (finished)
                {
                    goto Ending;
                }

                if (token.IsCancellationRequested)
                {
                    goto Stopping;
                }

                Thread.Sleep(5000);
            }
            while (!await TimeHelper.IsPreMarketOpen(TradeOrder.Id));

            do
            {
                Parallel.Invoke(async () => await CheckOrderStatuses(token), async () => await CheckIfFilling());

                if (finished)
                {
                    goto Ending;
                }

                if (temp != TradeOrder)
                {
                    temp = TradeOrder;
                    await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);
                }

                Thread.Sleep(1000);
            }
            while (!await TimeHelper.IsMarketOpen(TradeOrder.Id));
            
            if (token.IsCancellationRequested)
            {
                goto Stopping;
            }

            await PlaceTarget(token).ConfigureAwait(false);
            await PlaceStopLoss().ConfigureAwait(false);

            await LogHelper.AddLog(TradeOrder.Id, $"monitoring orders...").ConfigureAwait(false);
            while (true)
            {
                await CheckOrderStatuses(token);

                if (!TradeOrder.PreSLMCancelled)
                {
                    if ((await TickService.GetOrder(TradeOrder.SLMId)).FilledQuantity > 0)
                    {
                        await LogHelper.AddLog(TradeOrder.Id, $"slm hit...").ConfigureAwait(false);
                        await TradeHelper.CancelTarget(TradeOrder).ConfigureAwait(false);
                        goto Ending;
                    }
                }
                else if (TradeOrder.RegularSlmPlaced)
                {
                    if ((await TickService.GetOrder(TradeOrder.SLMId)).FilledQuantity > 0)
                    {
                        await LogHelper.AddLog(TradeOrder.Id, $"slm hit...").ConfigureAwait(false);
                        await TradeHelper.CancelTarget(TradeOrder).ConfigureAwait(false);
                        goto Ending;
                    }
                }

                if (TradeOrder.TargetPlaced)
                {
                    if (!TradeOrder.TargetHit)
                    {
                        if ((await TickService.GetOrder(TradeOrder.TargetId)).FilledQuantity > 0)
                        {
                            TradeOrder.TargetHit = true;
                            await LogHelper.AddLog(TradeOrder.Id, $"target hit...").ConfigureAwait(false);
                            await TradeHelper.CancelStopLoss(TradeOrder).ConfigureAwait(false);
                            await WatchingTarget().ConfigureAwait(false);
                        }
                    }
                }

                if (TimeHelper.IsMarketEnding())
                {
                    finished = true;
                    goto Stopping;
                }

                if (token.IsCancellationRequested)
                {
                    if (!TradeOrder.TargetHit)
                    {
                        finished = true;
                        goto Stopping;
                    }
                }

                if (finished)
                {
                    goto Ending;
                }

                if (temp != TradeOrder)
                {
                    temp = TradeOrder;
                    await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);
                }

                Thread.Sleep(500);
            }

            Stopping:

            await TradeHelper.CancelEntry(TradeOrder);
            await TradeHelper.CancelStopLoss(TradeOrder);
            await TradeHelper.CancelTarget(TradeOrder);
            await TradeHelper.SquareOff(TradeOrder);

            Ending:;

            TradeOrder.Status = Status.DONE;
            await OrderHelper.UpdateTradeOrder(TradeOrder);
        }



        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------



        private async Task PlaceTarget(CancellationToken token)
        {
            await LogHelper.AddLog(TradeOrder.Id, $"doing target logic...").ConfigureAwait(false);

            while (!TradeOrder.IsOrderFilling)
            {
                await CheckIfFilling();

                if (token.IsCancellationRequested || finished)
                {
                    goto End;
                }

                await Task.Delay(500);
            }

            decimal proximity;
            var entry = await TickService.GetOrder(TradeOrder.EntryId);
            if (TradeOrder.ExitTransactionType == "SELL")
            {
                TradeOrder.Target = (TradeOrder.RxR * TradeOrder.ZoneWidth) + entry.AveragePrice;
                proximity = ((TradeOrder.Target - entry.AveragePrice) * (decimal)0.8)
                                            + entry.AveragePrice;
            }
            else
            {
                TradeOrder.Target = entry.AveragePrice - (TradeOrder.RxR * TradeOrder.ZoneWidth);
                proximity = entry.AveragePrice 
                    - ((entry.AveragePrice - TradeOrder.Target) * (decimal)0.8);
            }

            if (finished)
            {
                goto End;
            }

            TradeOrder.TargetId = await TargetHelper.PlaceOrder(TradeOrder);
            if(TradeOrder.TargetId != null)
            {
                TradeOrder.TargetPlaced = true;
            }

            await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);

            while (true)
            {
                var entryO = await TickService.GetOrder(TradeOrder.EntryId);
                if (entryO.FilledQuantity == TradeOrder.Quantity)
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"entry order filled...").ConfigureAwait(false);
                    TradeOrder.QuantityFilled = entryO.FilledQuantity;
                    TradeOrder.Entry = entryO.AveragePrice;
                    goto End;
                }
                if ((await TickDbHelper.GetLast(TradeOrder.Instrument.Token)).LTP >= proximity)
                {
                    if (entryO.FilledQuantity != TradeOrder.Quantity)
                    {
                        await TargetHelper.Update(TradeOrder, entryO).ConfigureAwait(false);
                    }
                }
                if (token.IsCancellationRequested || finished)
                {
                    goto End;
                }

                await Task.Delay(500);
            }

            End:;

            await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);
        }

        // place stop loss
        private async Task PlaceStopLoss()
        {
            await LogHelper.AddLog(TradeOrder.Id, $"doing stoploss logic...").ConfigureAwait(false);

            // if entry order average price, is less than the stoploss input, then sleep for a minute for ticks to come in, so we can set the stoploss as the low. 
            if (TradeOrder.PreSLMCancelled)
            {
                await LogHelper.AddLog(TradeOrder.Id, $"average price is less than stop loss, waiting 1 min for data...").ConfigureAwait(false);

                var tick = await TickDbHelper.GetLast(TradeOrder.Instrument.Token);
                var candle = new Candle()
                {
                    Open = tick.LTP,
                    High = tick.LTP,
                    Low = tick.LTP
                };

                Stopwatch stopwatch = new();
                stopwatch.Start();
                while (stopwatch.Elapsed.TotalSeconds < 60)
                {
                    tick = await TickDbHelper.GetLast(TradeOrder.Instrument.Token);
                    if (candle.High < tick.LTP)
                    {
                        candle.High = tick.LTP;
                    }
                    if (candle.Low > tick.LTP)
                    {
                        candle.Low = tick.LTP;
                    }
                    await Task.Delay(200);
                }
                candle.Close = tick.LTP;
                stopwatch.Stop();

                TradeOrder.StopLoss = SLMHelper.GetTriggerPrice(TradeOrder, candle);

                bool isBullish = false;
                if (candle.Open < candle.Close)
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"{TradeOrder.Instrument.TradingSymbol} candle is bullish...").ConfigureAwait(false);
                    isBullish = true;
                }
                else
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"{TradeOrder.Instrument.TradingSymbol} candle is bearish...").ConfigureAwait(false);
                }

                if(TradeOrder.ExitTransactionType == "SELL")
                {
                    if (isBullish)
                    {
                        TradeOrder.SLMId = await SLMHelper.PlaceOrder(TradeOrder);
                        if(TradeOrder.SLMId != null)
                        {
                            TradeOrder.RegularSlmPlaced = true;
                            goto End;
                        }
                    }
                    else
                    {
                        await SLMHelper.SquareOff(TradeOrder);
                        finished = true;
                        goto End;
                    }
                }
                else
                {
                    if (!isBullish)
                    {
                        TradeOrder.SLMId = await SLMHelper.PlaceOrder(TradeOrder);
                        if (TradeOrder.SLMId != null)
                        {
                            TradeOrder.RegularSlmPlaced = true;
                            goto End;
                        }
                    }
                    else
                    {
                        await SLMHelper.SquareOff(TradeOrder);
                        finished = true;
                        goto End;
                    }
                }

                End:;

                await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);
            }
        }

        private async Task Initialize(TradeOrder order)
        {
            await LogHelper.AddLog(order.Id, $"order starting...").ConfigureAwait(false);

            TradeOrder = order;

            if (TradeOrder.TransactionType.ToString() == "BUY")
            {
                TradeOrder.ExitTransactionType = "SELL";
            }
            else
            {
                TradeOrder.ExitTransactionType = "BUY";
            }

            TradeOrder.Status = Status.RUNNING;
            TradeOrder.StopLoss = MathHelper.RoundUp(TradeOrder.StopLoss, (decimal)0.05);
            TradeOrder.Entry = MathHelper.RoundUp(TradeOrder.Entry, (decimal)0.05);

            if (TradeOrder.TransactionType.ToString() == "BUY")
            {
                TradeOrder.ZoneWidth = TradeOrder.Entry - TradeOrder.StopLoss;
                decimal decQuantity = TradeOrder.Risk / TradeOrder.ZoneWidth;
                TradeOrder.Quantity = (int)decQuantity;
            }
            else
            {
                TradeOrder.ZoneWidth = TradeOrder.StopLoss - TradeOrder.Entry;
                decimal decQuantity = TradeOrder.Risk / TradeOrder.ZoneWidth;
                TradeOrder.Quantity = (int)decQuantity;
            }

            await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);
        }

        private async Task CheckOrderStatuses(CancellationToken token)
        {
            OrderUpdate entry = await TickService.GetOrder(TradeOrder.EntryId);
            OrderUpdate slm = new();
            if (!TradeOrder.PreSLMCancelled)
            {
                slm = await TickService.GetOrder(TradeOrder.SLMId);
                TradeOrder.SLMStatus = slm.Status;
            }
            else if (TradeOrder.RegularSlmPlaced)
            {
                slm = await TickService.GetOrder(TradeOrder.SLMId);
                TradeOrder.SLMStatus = slm.Status;
            }
            OrderUpdate target = new();
            if (TradeOrder.TargetPlaced)
            {
                target = await TickService.GetOrder(TradeOrder.TargetId);
                TradeOrder.TargetStatus = target.Status;
            }

            // check if entry status is rejected
            if (entry.Status == "REJECTED")
            {
                await LogHelper.AddLog(TradeOrder.Id, $"entry order rejected...").ConfigureAwait(false);
                if (!TradeOrder.PreSLMCancelled)
                {
                    // if slm is not rejected then cancel it
                    if (slm.Status != "REJECTED")
                    {
                        Parallel.Invoke(
                            async () => await TradeHelper.CancelStopLoss(TradeOrder),
                            async () => await TradeHelper.CancelTarget(TradeOrder));
                    }
                }
                else if (TradeOrder.RegularSlmPlaced)
                {
                    // if slm is not rejected then cancel it
                    if (slm.Status != "REJECTED")
                    {
                        Parallel.Invoke(
                            async () => await TradeHelper.CancelStopLoss(TradeOrder),
                            async () => await TradeHelper.CancelTarget(TradeOrder));
                    }
                }
                finished = true;
                goto End;
            }

            if (!TradeOrder.PreSLMCancelled)
            {
                if (slm.Status == "REJECTED")
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"slm order rejected...").ConfigureAwait(false);
                    TradeOrder.PreSLMCancelled = true;
                    await PlaceStopLoss().ConfigureAwait(false);
                }
            }
            else if (TradeOrder.RegularSlmPlaced)
            {
                if (slm.Status == "REJECTED")
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"slm order rejected...").ConfigureAwait(false);
                    TradeOrder.RegularSlmPlaced = false;
                    await PlaceStopLoss().ConfigureAwait(false);
                }
            }

            if (TradeOrder.TargetPlaced)
            {
                if (target.Status == "REJECTED")
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"target order rejected...").ConfigureAwait(false);
                    TradeOrder.TargetPlaced = false;
                    await PlaceTarget(token).ConfigureAwait(false);
                }
            }

            End:;
        }

        private async Task WatchingTarget()
        {
            MyTick tick = new ();
            OrderUpdate entry = new ();
            OrderUpdate target = new ();
            while (!finished)
            {
                Parallel.Invoke(
                    async() => tick = await TickDbHelper.GetLast(TradeOrder.Instrument.Token),
                    async() => entry = await TickService.GetOrder(TradeOrder.EntryId),
                    async() => target = await TickService.GetOrder(TradeOrder.TargetId)
                );

                if (TradeOrder.ExitTransactionType == "SELL")
                {
                    if (tick.LTP < (entry.AveragePrice + (0.5m * (TradeOrder.Target - entry.AveragePrice))))
                    {
                        await TradeHelper.CancelEntry(TradeOrder);
                        await TradeHelper.CancelStopLoss(TradeOrder);
                        await TradeHelper.CancelTarget(TradeOrder);
                        await WatchingTargetHelper.SquareOff(entry, target, TradeOrder);

                        await LogHelper.AddLog(TradeOrder.Id, $"squared off...").ConfigureAwait(false);
                        finished = true;
                    }
                }
                else
                {
                    if (tick.LTP > (entry.AveragePrice - (0.5m * (TradeOrder.Target - entry.AveragePrice))))
                    {
                        await TradeHelper.CancelEntry(TradeOrder);
                        await TradeHelper.CancelStopLoss(TradeOrder);
                        await TradeHelper.CancelTarget(TradeOrder);
                        await WatchingTargetHelper.SquareOff(entry, target, TradeOrder);

                        await LogHelper.AddLog(TradeOrder.Id, $"squared off...").ConfigureAwait(false);
                        finished = true;
                    }
                }

                if (entry.FilledQuantity == target.FilledQuantity)
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"target filled...").ConfigureAwait(false);
                    finished = true;
                    break;
                }

                await Task.Delay(500);
            }
        }
        private async Task CheckIfFilling()
        {
            if (!TradeOrder.IsOrderFilling)
            {
                var entry = await TickService .GetOrder(TradeOrder.EntryId);
                if (entry.FilledQuantity > 0)
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"entry order filling...").ConfigureAwait(false);
                    TradeOrder.QuantityFilled = entry.FilledQuantity;
                    TradeOrder.Entry = entry.AveragePrice;
                    if (!TradeOrder.PreSLMCancelled)
                    {
                        if (TradeOrder.ExitTransactionType == "SELL")
                        {
                            if (entry.AveragePrice < TradeOrder.StopLoss)
                            {
                                try
                                {
                                    await TradeHelper.CancelStopLoss(TradeOrder).ConfigureAwait(false);
                                    await LogHelper.AddLog(TradeOrder.Id, $"slm order cancelled...").ConfigureAwait(false);
                                    TradeOrder.PreSLMCancelled = true;
                                }
                                catch (KiteException e)
                                {
                                    await LogHelper.AddLog(TradeOrder.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                                }
                            }
                        }
                        else
                        {
                            if (entry.AveragePrice > TradeOrder.StopLoss)
                            {
                                try
                                {
                                    await TradeHelper.CancelStopLoss(TradeOrder).ConfigureAwait(false);
                                    await LogHelper.AddLog(TradeOrder.Id, $"slm order cancelled...").ConfigureAwait(false);
                                    TradeOrder.PreSLMCancelled = true;
                                }
                                catch (KiteException e)
                                {
                                    await LogHelper.AddLog(TradeOrder.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    TradeOrder.IsOrderFilling = true;
                }
            }
        }
    }
}
