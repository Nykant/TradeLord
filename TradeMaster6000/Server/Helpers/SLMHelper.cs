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
        private Kite Kite { get; set; }
        public SLMHelper(IKiteService kiteService, ITradeLogHelper logHelper, ITickerService tickerService)
        {
            Kite = kiteService.GetKite();
            LogHelper = logHelper;
            TickService = tickerService;
        }

        public decimal GetTriggerPrice(string exitTransactionType, TradeOrder order)
        {
            var tick = TickService.LastTick(order.Instrument.Token);
            decimal triggerPrice = 0;
            if (exitTransactionType == "BUY")
            {
                triggerPrice = tick.High;
                triggerPrice = triggerPrice * (decimal)1.00015;
                triggerPrice = MathHelper.RoundUp(triggerPrice, (decimal)0.05);
            }
            else
            {
                triggerPrice = tick.Low;
                triggerPrice = triggerPrice * (decimal)0.99985;
                triggerPrice = MathHelper.RoundDown(triggerPrice, (decimal)0.05);
            }
            return triggerPrice;
        }
        public async Task<string> PlaceOrder(TradeOrder order, string exitTransactionType, int quantity, decimal triggerPrice)
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

                await LogHelper.AddLog(order.Id, $"SLM order placed...").ConfigureAwait(false);

                return value1;
            }
            catch (KiteException e)
            {
                await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                return null;
            }
        }
        public async Task SquareOff(TradeOrder order, string exitTransactionType, int quantity, string orderId_tar)
        {
            Kite.PlaceOrder(
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

            await LogHelper.AddLog(order.Id, $"squared off...").ConfigureAwait(false);
        }

    }
    public interface ISLMHelper
    {
        decimal GetTriggerPrice(string exitTransactionType, TradeOrder order);
        Task<string> PlaceOrder(TradeOrder order, string exitTransactionType, int quantity, decimal triggerPrice);
        Task SquareOff(TradeOrder order, string exitTransactionType, int quantity, string orderId_tar);
    }
}
