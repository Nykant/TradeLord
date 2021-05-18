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
    public class TargetHelper : ITargetHelper
    {
        private ITradeLogHelper LogHelper { get; set; }
        private Kite Kite { get; set; }
        public TargetHelper(IKiteService kiteService, ITradeLogHelper logHelper)
        {
            Kite = kiteService.GetKite();
            LogHelper = logHelper;
        }
        public async Task<string> PlaceOrder(TradeOrder order, string exitTransactionType, int quantity, decimal target)
        {
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

                orderReponse.TryGetValue("data", out dynamic value);
                Dictionary<string, dynamic> data = value;
                data.TryGetValue("order_id", out dynamic value1);

                await LogHelper.AddLog(order.Id, $"target placed...").ConfigureAwait(false);

                return value1;
            }
            catch (KiteException e)
            {
                await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                return null;
            }
        }
        public async Task Update(string orderId_tar, Order entryO, int orderId)
        {
            Kite.ModifyOrder(
                orderId_tar,
                Quantity: entryO.FilledQuantity.ToString()
            );

            await LogHelper.AddLog(orderId, $"target modified quantity = {entryO.FilledQuantity}...").ConfigureAwait(false);
        }
    }
    public interface ITargetHelper
    {
        Task<string> PlaceOrder(TradeOrder order, string exitTransactionType, int quantity, decimal target);
        Task Update(string orderId_tar, Order entryO, int orderId);
    }
}
