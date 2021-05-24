using KiteConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Helpers
{
    public class SLMHelper : ISLMHelper
    {
        private ITradeLogHelper LogHelper { get; set; }
        private ITickerService TickService { get; set; }
        private readonly IKiteService kiteService;
        public SLMHelper(IKiteService kiteService, ITradeLogHelper logHelper, ITickerService tickerService)
        {
            this.kiteService = kiteService;
            LogHelper = logHelper;
            TickService = tickerService;
        }

        public decimal GetTriggerPrice(TradeOrder order, Candle candle)
        {
            decimal triggerPrice;
            if (order.ExitTransactionType == "BUY")
            {
                triggerPrice = candle.High;
                triggerPrice *= (decimal)1.00015;
                triggerPrice = MathHelper.RoundUp(triggerPrice, (decimal)0.05);
            }
            else
            {
                triggerPrice = candle.Low;
                triggerPrice *= (decimal)0.99985;
                triggerPrice = MathHelper.RoundDown(triggerPrice, (decimal)0.05);
            }
            return triggerPrice;
        }
        public async Task<string> PlaceOrder(TradeOrder order)
        {
            dynamic id;
            try
            {
                var kite = kiteService.GetKite();
                kite.SetAccessToken(kiteService.GetAccessToken());
                Dictionary<string, dynamic> response = kite.PlaceOrder(
                     Exchange: order.Instrument.Exchange,
                     TradingSymbol: order.Instrument.TradingSymbol,
                     TransactionType: order.ExitTransactionType,
                     Quantity: order.Quantity,
                     TriggerPrice: order.StopLoss,
                     Product: Constants.PRODUCT_MIS,
                     OrderType: Constants.ORDER_TYPE_SLM,
                     Validity: Constants.VALIDITY_DAY,
                     Variety: Constants.VARIETY_REGULAR
                );

                id = response["data"]["order_id"];

                await LogHelper.AddLog(order.Id, $"SLM order placed...").ConfigureAwait(false);

                return id;
            }
            catch (KiteException e)
            {
                await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                return null;
            }
        }
        public async Task SquareOff(TradeOrder order)
        {
            var kite = kiteService.GetKite();
            kite.SetAccessToken(kiteService.GetAccessToken());
            kite.PlaceOrder(
                 Exchange: order.Instrument.Exchange,
                 TradingSymbol: order.Instrument.TradingSymbol,
                 TransactionType: order.ExitTransactionType,
                 Quantity: order.Quantity,
                 Product: Constants.PRODUCT_MIS,
                 OrderType: Constants.ORDER_TYPE_MARKET,
                 Validity: Constants.VALIDITY_DAY,
                 Variety: Constants.VARIETY_REGULAR
             );

            if (order.TargetPlaced)
            {
                kite.CancelOrder(order.TargetId);
            }
            else
            {
                await LogHelper.AddLog(order.Id, $"cant cancel target order cause it is still not placed...").ConfigureAwait(false);
            }

            await LogHelper.AddLog(order.Id, $"squared off...").ConfigureAwait(false);
        }

    }
    public interface ISLMHelper
    {
        decimal GetTriggerPrice(TradeOrder order, Candle candle);
        Task<string> PlaceOrder(TradeOrder order);
        Task SquareOff(TradeOrder order);
    }
}
