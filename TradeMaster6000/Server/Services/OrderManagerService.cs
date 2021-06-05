using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Server.Tasks;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Services
{
    public class OrderManagerService : IOrderManagerService
    {
        private readonly IInstrumentHelper instrumentHelper;
        private readonly IKiteService kiteService;
        private readonly IRunningOrderService running;
        private readonly ITradeOrderHelper tradeOrderHelper;
        private readonly ITickerService tickerService;
        private readonly IServiceProvider serviceProvider;
        private readonly ITradeLogHelper tradeLogHelper;
        public OrderManagerService(IRunningOrderService runningOrderService, IKiteService kiteService, IInstrumentHelper instrumentHelper, ITradeOrderHelper tradeOrderHelper, ITickerService tickerService, IServiceProvider serviceProvider, ITradeLogHelper tradeLogHelper)
        {
            this.instrumentHelper = instrumentHelper;
            this.kiteService = kiteService;
            this.running = runningOrderService;
            this.tradeOrderHelper = tradeOrderHelper;
            this.tickerService = tickerService;
            this.serviceProvider = serviceProvider;
            this.tradeLogHelper = tradeLogHelper;
        }

        public async Task StartOrder(TradeOrder order)
        {
            order.TokenSource = new CancellationTokenSource();
            var instruments = await instrumentHelper.GetTradeInstruments();

            await Task.Run(() =>
            {
                foreach (var instrument in instruments)
                {
                    if (instrument.TradingSymbol == order.TradingSymbol)
                    {
                        order.Instrument = instrument;
                        break;
                    }
                }
            });

            var tradeorder = await tradeOrderHelper.AddTradeOrder(order);
            order.Id = tradeorder.Id;
            running.Add(order);

            while (!kiteService.IsKiteConnected())
            {
                if (order.TokenSource.Token.IsCancellationRequested)
                {
                    running.Remove(order.Id);
                    goto Ending;
                }
                Thread.Sleep(2000);
            }

            await tickerService.Start();

            await Task.Factory.StartNew(async () => await RunOrder(order), TaskCreationOptions.LongRunning).ConfigureAwait(false);

            Ending:;
        }

        public async Task AutoOrders(int k)
        {
            if (!kiteService.IsKiteConnected())
            {
                goto Ending;
            }

            var kite = kiteService.GetKite();
            var orders = new List<TradeOrder>();
            var instruments = await instrumentHelper.GetTradeInstruments();
            Random random = new ();

            await Task.Run(async() =>
            {
                for (int i = 0; i < k; i++)
                {
                    TradeOrder order = new ();
                    int rng = random.Next(0, instruments.Count - 1);
                    order.Instrument = instruments[rng];
                    var ltp = kite.GetLTP(new[] { order.Instrument.Token.ToString() })[order.Instrument.Token.ToString()].LastPrice;
                    order = MakeOrder(i, order, ltp);
                    orders.Add(order);
                    await Task.Delay(500);
                }
            });

            await Task.Run(async() =>
            {
                foreach (var order in orders)
                {
                    var tradeorder = await tradeOrderHelper.AddTradeOrder(order);
                    order.Id = tradeorder.Id;
                    running.Add(order);
                }
            });

            await tickerService.Start();

            foreach (var order in orders)
            {
                await Task.Factory.StartNew(async () => await RunOrder(order), TaskCreationOptions.LongRunning).ConfigureAwait(false);
                await Task.Delay(500);
            }

            Ending:;
        }

        private async Task RunOrder(TradeOrder order)
        {
            try
            {
                tickerService.Subscribe(order.Instrument.Token);
                OrderInstance orderWork = new(serviceProvider);
                await orderWork.StartWork(order, order.TokenSource.Token);
                tickerService.UnSubscribe(order.Instrument.Token);
                running.Remove(order.Id);
                if (running.Get().Count == 0)
                {
                    tickerService.Stop();
                }
                await tradeLogHelper.AddLog(order.Id, $"order stopped...").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await tradeLogHelper.AddLog(order.Id, $"some error happened lol: {e.Message}...").ConfigureAwait(false);
                try
                {
                    running.Remove(order.Id);
                }
                catch { }
            }
        }
        private static TradeOrder MakeOrder(int i, TradeOrder order, decimal ltp)
        {
            switch (i)
            {
                case 0:
                    order.Entry = ltp + 1;
                    order.StopLoss = ltp - 3;
                    order.Risk = 4;
                    order.RxR = 2;
                    order.TransactionType = TransactionType.BUY;
                    order.TradingSymbol = order.Instrument.TradingSymbol;
                    order.TokenSource = new CancellationTokenSource();
                    return order;
                case 1:
                    order.Entry = ltp - 1;
                    order.StopLoss = ltp - 4;
                    order.Risk = 3;
                    order.RxR = 2;
                    order.TransactionType = TransactionType.BUY;
                    order.TradingSymbol = order.Instrument.TradingSymbol;
                    order.TokenSource = new CancellationTokenSource();
                    return order;
                case 2:
                    order.Entry = ltp - 6;
                    order.StopLoss = ltp - 2;
                    order.Risk = 4;
                    order.RxR = 2;
                    order.TransactionType = TransactionType.SELL;
                    order.TradingSymbol = order.Instrument.TradingSymbol;
                    order.TokenSource = new CancellationTokenSource();
                    return order;
                case 3:
                    order.Entry = ltp + 6;
                    order.StopLoss = ltp + 2;
                    order.Risk = 4;
                    order.RxR = 2;
                    order.TransactionType = TransactionType.BUY;
                    order.TradingSymbol = order.Instrument.TradingSymbol;
                    order.TokenSource = new CancellationTokenSource();
                    return order;
                case 4:
                    order.Entry = ltp - 1;
                    order.StopLoss = ltp + 3;
                    order.Risk = 4;
                    order.RxR = 2;
                    order.TransactionType = TransactionType.SELL;
                    order.TradingSymbol = order.Instrument.TradingSymbol;
                    order.TokenSource = new CancellationTokenSource();
                    return order;
                default:
                    return default;
            }
        }
    }
    public interface IOrderManagerService
    {
        Task AutoOrders(int k);
        Task StartOrder(TradeOrder order);
    }
}
