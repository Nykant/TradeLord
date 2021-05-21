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
    public class OrderWork
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
        private OrderHub orderHub { get; set; }
        private TradeOrder TradeOrder { get; set; }

        // variables
        private bool isPreMarketOpen;
        private bool isMarketOpen;
        private bool finished;

        // class constructor
        public OrderWork(OrderHub orderHub, IServiceProvider service)
        {
            this.orderHub = orderHub;

            // constructor dependency injection
            OrderHelper = service.GetRequiredService<ITradeOrderHelper>();
            LogHelper = service.GetRequiredService<ITradeLogHelper>();
            TickService = service.GetRequiredService<ITickerService>();
            TradeHelper = service.GetRequiredService<ITradeHelper>();
            TargetHelper = service.GetRequiredService<ITargetHelper>();
            SLMHelper = service.GetRequiredService<ISLMHelper>();
            WatchingTargetHelper = service.GetRequiredService<IWatchingTargetHelper>();
            TimeHelper = service.GetRequiredService<ITimeHelper>();

            //set values when constructor initialized
            isPreMarketOpen = false;
            isMarketOpen = false;
            finished = false;
        }

        public async Task StartWork(TradeOrder order, CancellationToken token)
        {
            await Initialize(order, orderHub);

            // wait untill we get a tick
            while (TickService.LastTick(TradeOrder.Instrument.Token).LastPrice == 0)
            {
                Thread.Sleep(500);
            }

            TradeOrder.EntryId = await TradeHelper.PlaceEntry(TradeOrder);
            if (TradeOrder.EntryId == null)
            {
                goto Ending;
            }

            TradeOrder.SLMId = await TradeHelper.PlacePreSLM(TradeOrder);
            if (TradeOrder.SLMId == "cancelled")
            {
                TradeOrder.PreSLMCancelled = true;
            }

            while (!TickService.AnyOrder(TradeOrder.EntryId))
            {
                if (token.IsCancellationRequested)
                {
                    goto Stopping;
                }
                Thread.Sleep(500);
            }

            if (!TradeOrder.PreSLMCancelled)
            {
                while (!TickService.AnyOrder(TradeOrder.SLMId))
                {
                    if (token.IsCancellationRequested)
                    {
                        goto Stopping;
                    }
                    Thread.Sleep(500);
                }
            }

            // do while pre market is not open
            TradeOrder temp = new TradeOrder();
            do
            {
                await Task.Run(() => CheckOrderStatuses(token)).ConfigureAwait(false);

                if (await TimeHelper.IsPreMarketOpen(TradeOrder.Id))
                {
                    isPreMarketOpen = true;
                    break;
                }

                if (token.IsCancellationRequested)
                {
                    goto Stopping;
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

                Thread.Sleep(5000);
            }
            while (!isPreMarketOpen);

            // do while pre market is open
            do
            {
                await Task.Run(() => CheckOrderStatuses(token)).ConfigureAwait(false);

                if (await TimeHelper.IsMarketOpen(TradeOrder.Id))
                {
                    isMarketOpen = true;
                    break;
                }

                if (!TradeOrder.IsOrderFilling)
                {
                    await Task.Run(() => CheckIfFilling()).ConfigureAwait(false);
                }

                if (temp != TradeOrder)
                {
                    temp = TradeOrder;
                    await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);
                }

                if (finished)
                {
                    goto Ending;
                }

                Thread.Sleep(500);
            }
            while (!isMarketOpen);

            await Task.Run(() =>
            {
                Parallel.Invoke(
                    async () =>
                    {
                        await PlaceTarget(token);
                    },
                    async () =>
                    {
                        await PlaceStopLoss(token);
                    });
            }).ConfigureAwait(false);

            await LogHelper.AddLog(TradeOrder.Id, $"monitoring orders...").ConfigureAwait(false);

            // monitoring the orders
            while (true)
            {
                await Task.Run(() => CheckOrderStatuses(token)).ConfigureAwait(false);

                if (!TradeOrder.PreSLMCancelled)
                {
                    if (TickService.GetOrder(TradeOrder.SLMId).FilledQuantity > 0)
                    {
                        await LogHelper.AddLog(TradeOrder.Id, $"slm hit...").ConfigureAwait(false);
                        await TradeHelper.CancelTarget(TradeOrder).ConfigureAwait(false);
                        goto Ending;
                    }
                }
                else if (TradeOrder.RegularSlmPlaced)
                {
                    if (TickService.GetOrder(TradeOrder.SLMId).FilledQuantity > 0)
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
                        if (TickService.GetOrder(TradeOrder.TargetId).FilledQuantity > 0)
                        {
                            TradeOrder.TargetHit = true;
                            await LogHelper.AddLog(TradeOrder.Id, $"target hit...").ConfigureAwait(false);
                            await Task.Run(() => TradeHelper.CancelStopLoss(TradeOrder)).ConfigureAwait(false);
                            await Task.Run(() => WatchingTarget()).ConfigureAwait(false);
                        }
                    }
                }

                if (temp != TradeOrder)
                {
                    temp = TradeOrder;
                    await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);
                }

                if (token.IsCancellationRequested)
                {
                    if (!TradeOrder.TargetHit)
                    {
                        goto Stopping;
                    }
                }

                if (finished)
                {
                    goto Ending;
                }

                await Task.Delay(500);
            }

            // go to when trade order is stopped
            Stopping:

            await TradeHelper.CancelEntry(TradeOrder);
            await TradeHelper.CancelStopLoss(TradeOrder);
            await TradeHelper.CancelTarget(TradeOrder);
            await TradeHelper.SquareOff(TradeOrder);

            // go to when trade order is ending
            Ending:;

            TradeOrder.Status = Status.DONE;
            await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);
        }



        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // place target
        private async Task PlaceTarget(CancellationToken token)
        {
            await LogHelper.AddLog(TradeOrder.Id, $"doing target logic...").ConfigureAwait(false);

            while (!TradeOrder.IsOrderFilling)
            {
                await Task.Run(() => CheckIfFilling());

                if (token.IsCancellationRequested || finished)
                {
                    goto End;
                }

                await Task.Delay(500);
            }

            decimal proximity = 0;
            var entry = TickService.GetOrder(TradeOrder.EntryId);
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

            TradeOrder.TargetId = await Task.Run(()=>TargetHelper.PlaceOrder(TradeOrder));
            if(TradeOrder.TargetId != null)
            {
                TradeOrder.TargetPlaced = true;
            }

            await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);

            while (true)
            {
                var entryO = TickService.GetOrder(TradeOrder.EntryId);
                if (entryO.FilledQuantity == TradeOrder.Quantity)
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"entry order filled...").ConfigureAwait(false);
                    TradeOrder.QuantityFilled = entryO.FilledQuantity;
                    TradeOrder.Entry = entryO.AveragePrice;
                    break;
                }
                if (TickService.LastTick(TradeOrder.Instrument.Token).High >= proximity)
                {
                    if (entryO.FilledQuantity != TradeOrder.Quantity)
                    {
                        await Task.Run(()=>TargetHelper.Update(TradeOrder, entryO)).ConfigureAwait(false);
                    }
                }
                if (token.IsCancellationRequested || finished)
                {
                    goto End;
                }
                await Task.Delay(500);
            }

            End:;
        }

        // place stop loss
        private async Task PlaceStopLoss(CancellationToken token)
        {
            await LogHelper.AddLog(TradeOrder.Id, $"doing stoploss logic...").ConfigureAwait(false);

            // if entry order average price, is less than the stoploss input, then sleep for a minute for ticks to come in, so we can set the stoploss as the low. 
            if (TradeOrder.PreSLMCancelled)
            {
                if (!TradeOrder.SlRejected)
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"average price is less than stop loss, waiting 1 min for data...").ConfigureAwait(false);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while(stopwatch.Elapsed.TotalSeconds < 55)
                    {
                        if (token.IsCancellationRequested || finished)
                        {
                            goto End;
                        }
                        await Task.Delay(500);
                    }
                    stopwatch.Stop();
                }

                TradeOrder.StopLoss = SLMHelper.GetTriggerPrice(TradeOrder);

                var tick = TickService.LastTick(TradeOrder.Instrument.Token);
                bool isBullish = false;
                if (tick.Open < tick.Close)
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"{TradeOrder.Instrument.TradingSymbol} is bullish...").ConfigureAwait(false);
                    isBullish = true;
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
                        await Task.Run(() => SLMHelper.SquareOff(TradeOrder)).ConfigureAwait(false);
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
                        await Task.Run(() => SLMHelper.SquareOff(TradeOrder)).ConfigureAwait(false);
                        finished = true;
                        goto End;
                    }
                }
            }
            End:;

            await OrderHelper.UpdateTradeOrder(TradeOrder).ConfigureAwait(false);
        }

        private async Task Initialize(TradeOrder order, OrderHub orderHub)
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

            // calculate zonewidth and quantity
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
            var entry = TickService.GetOrder(TradeOrder.EntryId);

            Order slm = new Order();
            if (!TradeOrder.PreSLMCancelled)
            {
                slm = TickService.GetOrder(TradeOrder.SLMId);
            }
            else if (TradeOrder.RegularSlmPlaced)
            {
                slm = TickService.GetOrder(TradeOrder.SLMId);
            }

            Order targetO = new Order();
            if (TradeOrder.TargetPlaced)
            {
                targetO = TickService.GetOrder(TradeOrder.TargetId);
            }

            TradeOrder.EntryStatus = entry.Status;
            // check if entry status is rejected
            if (entry.Status == "REJECTED")
            {
                await LogHelper.AddLog(TradeOrder.Id, $"entry order rejected...").ConfigureAwait(false);
                if (!TradeOrder.PreSLMCancelled)
                {
                    // if slm is not rejected then cancel it
                    if (slm.Status != "REJECTED")
                    {
                        await TradeHelper.CancelStopLoss(TradeOrder).ConfigureAwait(false);
                        await LogHelper.AddLog(TradeOrder.Id, $"slm order cancelled...").ConfigureAwait(false);
                    }
                }
                else if (TradeOrder.RegularSlmPlaced)
                {
                    // if slm is not rejected then cancel it
                    if (slm.Status != "REJECTED")
                    {
                        await TradeHelper.CancelStopLoss(TradeOrder).ConfigureAwait(false);
                        await LogHelper.AddLog(TradeOrder.Id, $"slm order cancelled...").ConfigureAwait(false);
                    }
                }
                finished = true;
                goto End;
            }

            if (!TradeOrder.PreSLMCancelled)
            {
                TradeOrder.SLMStatus = slm.Status;
                if (slm.Status == "REJECTED")
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"slm order rejected...").ConfigureAwait(false);

                    if (entry.Status != "REJECTED")
                    {
                        await TradeHelper.CancelEntry(TradeOrder).ConfigureAwait(false);
                        await LogHelper.AddLog(TradeOrder.Id, $"entry order cancelled...").ConfigureAwait(false);
                    }
                    finished = true;
                    goto End;
                }
            }
            else if (TradeOrder.RegularSlmPlaced)
            {
                TradeOrder.SLMStatus = slm.Status;
                if (slm.Status == "REJECTED")
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"slm order rejected...").ConfigureAwait(false);
                    TradeOrder.SlRejected = true;
                    TradeOrder.RegularSlmPlaced = false;
                    await Task.Run(async () => await PlaceStopLoss(token)).ConfigureAwait(false);
                }
            }

            if (TradeOrder.TargetPlaced)
            {
                TradeOrder.TargetStatus = targetO.Status;
                if (targetO.Status == "REJECTED")
                {
                    await LogHelper.AddLog(TradeOrder.Id, $"target order rejected...").ConfigureAwait(false);
                    TradeOrder.TargetPlaced = false;
                    await Task.Run(async () => await PlaceTarget(token)).ConfigureAwait(false);
                }
            }

            End:;
        }

        private async Task WatchingTarget()
        {
            Tick tick = new Tick();
            Order entry = new Order();
            Order targetO = new Order();
            while (!finished)
            {
                Parallel.Invoke(
                    () => tick = TickService.LastTick(TradeOrder.Instrument.Token), 
                    () => entry = TickService.GetOrder(TradeOrder.EntryId), 
                    () => targetO = TickService.GetOrder(TradeOrder.TargetId)
                );

                if (TradeOrder.ExitTransactionType == "SELL")
                {
                    if (tick.LastPrice < (entry.AveragePrice + (0.5m * (TradeOrder.Target - entry.AveragePrice))))
                    {
                        Parallel.Invoke(
                            async () => await TradeHelper.CancelEntry(TradeOrder),
                            async () => await TradeHelper.CancelStopLoss(TradeOrder),
                            async () => await TradeHelper.CancelTarget(TradeOrder)
                        );

                        await Task.Run(() =>
                        {
                            WatchingTargetHelper.SquareOff(entry, targetO, TradeOrder);
                            finished = true;
                        }).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (tick.LastPrice > (entry.AveragePrice - (0.5m * (TradeOrder.Target - entry.AveragePrice))))
                    {
                        Parallel.Invoke(async () => 
                            await TradeHelper.CancelEntry(TradeOrder), async () => 
                            await TradeHelper.CancelStopLoss(TradeOrder), async () => 
                            await TradeHelper.CancelTarget(TradeOrder)
                        );

                        await Task.Run(() =>
                        {
                            WatchingTargetHelper.SquareOff(entry, targetO, TradeOrder);
                            finished = true;
                        }).ConfigureAwait(false);
                    }
                }

                if (entry.FilledQuantity == targetO.FilledQuantity)
                {
                    finished = true;
                    break;
                }
            }
        }
        private async Task CheckIfFilling()
        {
            var entry = await Task.Run(() => TickService.GetOrder(TradeOrder.EntryId));

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
