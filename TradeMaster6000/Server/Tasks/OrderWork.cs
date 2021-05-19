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

        // variables
        private string orderId_ent;
        private string orderId_tar;
        private string orderId_slm;
        private string exitTransactionType;

        private decimal triggerPrice;
        private decimal target;
        private decimal zonewidth;
        private int quantity;
        private int orderId;

        private bool isOrderFilling;
        private bool is_pre_slm_cancelled;
        private bool regularSLMplaced;
        private bool targetplaced;
        private bool isPreMarketOpen;
        private bool isMarketOpen;
        private bool slRejected;
        private bool isTargetHit;
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

            //set values when constructor initialized
            isPreMarketOpen = false;
            isMarketOpen = false;
            targetplaced = false;
            isOrderFilling = false;
            is_pre_slm_cancelled = false;
            regularSLMplaced = false;
            slRejected = false;
            isTargetHit = false;
        }

        public async Task StartWork(TradeOrder order)
        {
            order = await Initialize(order, orderHub);

            // wait untill we get a tick
            while (TickService.LastTick(order.Instrument.Token).LastPrice == 0)
            {
                Thread.Sleep(500);
            }

            orderId_ent = await TradeHelper.PlaceEntry(order);
            if (orderId_ent == null)
            {
                goto Ending;
            }

            orderId_slm = await TradeHelper.PlacePreSLM(order, exitTransactionType, orderId_ent);
            if(orderId_slm == "cancelled")
            {
                is_pre_slm_cancelled = true;
            }

            // do while pre market is not open
            do
            {
                await Task.Run(() => CheckOrderStatuses(order));

                if (await TimeHelper.IsPreMarketOpen(orderId))
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

                Thread.Sleep(5000);
            }
            while (!isPreMarketOpen);

            // do while pre market is open
            do
            {
                await Task.Run(() => CheckOrderStatuses(order));

                if (await TimeHelper.IsMarketOpen(orderId))
                {
                    isMarketOpen = true;
                    break;
                }

                if (!isOrderFilling)
                {
                    await Task.Run(()=>CheckIfFilling(order));
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
                Parallel.Invoke(async () => await PlaceTarget(order), async () => await PlaceStopLoss(order));
            }).ConfigureAwait(false);

            await LogHelper.AddLog(order.Id, $"monitoring orders...").ConfigureAwait(false);

            // monitoring the orders
            while (true)
            {
                await Task.Run(() => CheckOrderStatuses(order));

                if (!is_pre_slm_cancelled)
                {
                    if (TickService.GetOrder(orderId_slm).FilledQuantity > 0)
                    {
                        await LogHelper.AddLog(order.Id, $"slm hit...").ConfigureAwait(false);
                        await Task.Run(()=>TradeHelper.CancelTarget(orderId_tar, targetplaced, orderId)).ConfigureAwait(false);
                        goto Ending;
                    }
                }
                else if (regularSLMplaced)
                {
                    if (TickService.GetOrder(orderId_slm).FilledQuantity > 0)
                    {
                        await LogHelper.AddLog(order.Id, $"slm hit...").ConfigureAwait(false);
                        await Task.Run(() => TradeHelper.CancelTarget(orderId_tar, targetplaced, orderId)).ConfigureAwait(false);
                        goto Ending;
                    }
                }

                if (targetplaced)
                {
                    if (!isTargetHit)
                    {
                        if (TickService.GetOrder(orderId_tar).FilledQuantity > 0)
                        {
                            isTargetHit = true;
                            await LogHelper.AddLog(order.Id, $"target hit...").ConfigureAwait(false);
                            await Task.Run(() => TradeHelper.CancelStopLoss(orderId_slm, is_pre_slm_cancelled, regularSLMplaced, orderId)).ConfigureAwait(false);
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

                await Task.Delay(500);
            }

            // go to when trade order is stopped
            Stopping:

            await TradeHelper.CancelEntry(orderId_ent, orderId);
            await TradeHelper.CancelStopLoss(orderId_slm, is_pre_slm_cancelled, regularSLMplaced, orderId);
            await TradeHelper.CancelTarget(orderId_tar, targetplaced, orderId);
            await TradeHelper.SquareOff(order, orderId_ent, exitTransactionType);

            // go to when trade order is ending
            Ending:;
        }



        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // place target
        private async Task PlaceTarget(TradeOrder order)
        {
            await LogHelper.AddLog(order.Id, $"doing target logic...").ConfigureAwait(false);

            while (!isOrderFilling)
            {
                await Task.Run(() => CheckIfFilling(order));
                await Task.Delay(500);
            }

            decimal proximity = 0;
            var entry = TickService.GetOrder(orderId_ent);
            if (exitTransactionType == "SELL")
            {
                target = (order.RxR * zonewidth) + entry.AveragePrice;
                proximity = ((target - entry.AveragePrice) * (decimal)0.8)
                                            + entry.AveragePrice;
            }
            else
            {
                target = entry.AveragePrice - (order.RxR * zonewidth);
                proximity = entry.AveragePrice 
                    - ((entry.AveragePrice - target) * (decimal)0.8);
            }

            orderId_tar = await Task.Run(()=>TargetHelper.PlaceOrder(order, exitTransactionType, quantity, target));
            if(orderId_tar != null)
            {
                targetplaced = true;
            }

            while (true)
            {
                var entryO = TickService.GetOrder(orderId_ent);
                if (entryO.FilledQuantity == quantity)
                {
                    await LogHelper.AddLog(order.Id, $"entry order filled...").ConfigureAwait(false);
                    break;
                }
                if (TickService.LastTick(order.Instrument.Token).High >= proximity)
                {
                    if (entryO.FilledQuantity != quantity)
                    {
                        await Task.Run(()=>TargetHelper.Update(orderId_tar, entryO, orderId)).ConfigureAwait(false);
                    }
                }
                await Task .Delay(500);
            }
        }

        // place stop loss
        private async Task PlaceStopLoss(TradeOrder order)
        {
            await LogHelper.AddLog(order.Id, $"doing stoploss logic...").ConfigureAwait(false);

            // if entry order average price, is less than the stoploss input, then sleep for a minute for ticks to come in, so we can set the stoploss as the low. 
            if (is_pre_slm_cancelled)
            {
                if (!slRejected)
                {
                    await LogHelper.AddLog(order.Id, $"average price is less than stop loss, waiting 1 min for data...").ConfigureAwait(false);

                    await Task.Delay(55000);
                }

                triggerPrice = SLMHelper.GetTriggerPrice(exitTransactionType, order);

                var tick = TickService.LastTick(order.Instrument.Token);
                bool isBullish = false;
                if (tick.Open < tick.Close)
                {
                    await LogHelper.AddLog(order.Id, $"{order.Instrument.TradingSymbol} is bullish...").ConfigureAwait(false);
                    isBullish = true;
                }

                if(exitTransactionType == "SELL")
                {
                    if (isBullish)
                    {
                        orderId_slm = await SLMHelper.PlaceOrder(order, exitTransactionType, quantity, triggerPrice);
                        if(orderId_slm != null)
                        {
                            regularSLMplaced = true;
                            goto Ending;
                        }
                    }
                    else
                    {
                        await Task.Run(() => SLMHelper.SquareOff(order, exitTransactionType, quantity, orderId_tar)).ConfigureAwait(false);
                        finished = true;
                        goto Ending;
                    }
                }
                else
                {
                    if (!isBullish)
                    {
                        orderId_slm = await SLMHelper.PlaceOrder(order, exitTransactionType, quantity, triggerPrice);
                        if (orderId_slm != null)
                        {
                            regularSLMplaced = true;
                            goto Ending;
                        }
                    }
                    else
                    {
                        await Task.Run(() => SLMHelper.SquareOff(order, exitTransactionType, quantity, orderId_tar)).ConfigureAwait(false);
                        finished = true;
                        goto Ending;
                    }
                }
            }
            Ending:;
        }

        private async Task<TradeOrder> Initialize(TradeOrder order, OrderHub orderHub)
        {
            await LogHelper.AddLog(order.Id, $"order starting...").ConfigureAwait(false);

            orderId = order.Id;

            if (order.TransactionType.ToString() == "BUY")
            {
                exitTransactionType = "SELL";
            }

            else
            {
                exitTransactionType = "BUY";
            }

            order.Status = Status.RUNNING;
            order.StopLoss = MathHelper.RoundUp(order.StopLoss, (decimal)0.05);
            order.Entry = MathHelper.RoundUp(order.Entry, (decimal)0.05);

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
            await OrderHelper.UpdateTradeOrder(order).ConfigureAwait(false);

            return order;
        }



        private async Task CheckOrderStatuses(TradeOrder order)
        {
            var entry = await Task.Run(() => TickService.GetOrder(orderId_ent));

            Order slm = new Order();
            if (!is_pre_slm_cancelled)
            {
                slm = await Task.Run(() => TickService.GetOrder(orderId_slm));
            }
            else if (regularSLMplaced)
            {
                slm = await Task.Run(() => TickService.GetOrder(orderId_slm));
            }

            Order targetO = new Order();
            if (targetplaced)
            {
                targetO = await Task.Run(() => TickService.GetOrder(orderId_tar));
            }

            // check if entry status is rejected
            if (entry.Status == "REJECTED")
            {
                await LogHelper.AddLog(orderId, $"entry order rejected...").ConfigureAwait(false);
                if (!is_pre_slm_cancelled)
                {
                    // if slm is not rejected then cancel it
                    if (slm.Status != "REJECTED")
                    {
                        await TradeHelper.CancelStopLoss(orderId_slm, is_pre_slm_cancelled, regularSLMplaced, orderId).ConfigureAwait(false);
                        await LogHelper.AddLog(orderId, $"slm order cancelled...").ConfigureAwait(false);
                    }
                }
                else if (regularSLMplaced)
                {
                    // if slm is not rejected then cancel it
                    if (slm.Status != "REJECTED")
                    {
                        await TradeHelper.CancelStopLoss(orderId_slm, is_pre_slm_cancelled, regularSLMplaced, orderId).ConfigureAwait(false);
                        await LogHelper.AddLog(orderId, $"slm order cancelled...").ConfigureAwait(false);
                    }
                }

                finished = true;
                goto End;
            }

            if (!is_pre_slm_cancelled)
            {
                if (slm.Status == "REJECTED")
                {
                    await LogHelper.AddLog(orderId, $"slm order rejected...").ConfigureAwait(false);

                    if (entry.Status != "REJECTED")
                    {
                        await TradeHelper.CancelEntry(orderId_ent, orderId).ConfigureAwait(false);
                        await LogHelper.AddLog(orderId, $"entry order cancelled...").ConfigureAwait(false);

                    }
                    finished = true;
                    goto End;
                }
            }

            else if (regularSLMplaced)
            {
                if (slm.Status == "REJECTED")
                {
                    await LogHelper.AddLog(orderId, $"slm order rejected...").ConfigureAwait(false);
                    slRejected = true;
                    regularSLMplaced = false;
                    await Task.Run(async () => await PlaceStopLoss(order)).ConfigureAwait(false);
                }
            }

            if (targetplaced)
            {
                if (targetO.Status == "REJECTED")
                {
                    await LogHelper.AddLog(orderId, $"target order rejected...").ConfigureAwait(false);
                    targetplaced = false;
                    await Task.Run(async () => await PlaceTarget(order)).ConfigureAwait(false);
                }
            }
            End:;
        }

        private async Task WatchingTarget(TradeOrder order)
        {
            while (!finished)
            {
                var tickTask = Task.Run(()=> TickService.LastTick(order.Instrument.Token));
                var entryTask = Task.Run(()=> TickService.GetOrder(orderId_ent));
                var targetTask = Task.Run(()=> TickService.GetOrder(orderId_tar));
                var tick = await tickTask;
                var entry = await entryTask;
                var targetO = await targetTask;
                if (exitTransactionType == "SELL")
                {
                    if (tick.LastPrice < (entry.AveragePrice + (0.5m * (target - entry.AveragePrice))))
                    {
                        await Task.Run(() => Parallel.Invoke(async () => 
                            await TradeHelper.CancelEntry(orderId_ent, orderId),async () => 
                            await TradeHelper.CancelStopLoss(orderId_slm,is_pre_slm_cancelled, regularSLMplaced, orderId), async () => 
                            await TradeHelper.CancelTarget(orderId_tar, targetplaced, orderId)
                        ));

                        await Task.Run(() =>
                        {


                            finished = true;
                        }).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (tick.LastPrice > (entry.AveragePrice - (0.5m * (target - entry.AveragePrice))))
                    {
                        Parallel.Invoke(async () => 
                            await TradeHelper.CancelEntry(orderId_ent, orderId), async () => 
                            await TradeHelper.CancelStopLoss(orderId_slm, is_pre_slm_cancelled, regularSLMplaced, orderId), async () => 
                            await TradeHelper.CancelTarget(orderId_tar, targetplaced, orderId)
                        );

                        await Task.Run(() =>
                        {
                            WatchingTargetHelper.SquareOff(entry, targetO, order, exitTransactionType);
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
        private async Task CheckIfFilling(TradeOrder order)
        {
            var entry = await Task.Run(() => TickService.GetOrder(orderId_ent));

            if (entry.FilledQuantity > 0)
            {
                await LogHelper.AddLog(orderId, $"entry order filling...").ConfigureAwait(false);

                if (!is_pre_slm_cancelled)
                {
                    if (exitTransactionType == "SELL")
                    {
                        if (entry.AveragePrice < order.StopLoss)
                        {
                            try
                            {
                                await TradeHelper.CancelStopLoss(orderId_slm, is_pre_slm_cancelled, regularSLMplaced, orderId).ConfigureAwait(false);
                                await LogHelper.AddLog(orderId, $"slm order cancelled...").ConfigureAwait(false);
                                is_pre_slm_cancelled = true;
                            }
                            catch (KiteException e)
                            {
                                await LogHelper.AddLog(orderId, $"kite error: {e.Message}...").ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        if (entry.AveragePrice > order.StopLoss)
                        {
                            try
                            {
                                await TradeHelper.CancelStopLoss(orderId_slm, is_pre_slm_cancelled, regularSLMplaced, orderId).ConfigureAwait(false);
                                await LogHelper.AddLog(orderId, $"slm order cancelled...").ConfigureAwait(false);
                                is_pre_slm_cancelled = true;
                            }
                            catch (KiteException e)
                            {
                                await LogHelper.AddLog(orderId, $"kite error: {e.Message}...").ConfigureAwait(false);
                            }
                        }
                    }
                }
                isOrderFilling = true;
            }
        }
    }
}
