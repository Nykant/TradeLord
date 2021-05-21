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
        public async Task<string> PlaceOrder(TradeOrder order)
        {
            dynamic id;
            try
            {
                Dictionary<string, dynamic> response = Kite.PlaceOrder(
                     Exchange: order.Instrument.Exchange,
                     TradingSymbol: order.Instrument.TradingSymbol,
                     TransactionType: order.ExitTransactionType,
                     Quantity: order.Quantity,
                     Price: order.Target,
                     Product: Constants.PRODUCT_MIS,
                     OrderType: Constants.ORDER_TYPE_LIMIT,
                     Validity: Constants.VALIDITY_DAY,
                     Variety: Constants.VARIETY_REGULAR
                 );

                id = response["data"]["order_id"];

                await LogHelper.AddLog(order.Id, $"target placed...").ConfigureAwait(false);

                return id;
            }
            catch (KiteException e)
            {
                await LogHelper.AddLog(order.Id, $"kite error: {e.Message}...").ConfigureAwait(false);
                return null;
            }
        }
        public async Task Update(TradeOrder order, Order entryO)
        {
            Kite.ModifyOrder(
                order.TargetId,
                Quantity: entryO.FilledQuantity.ToString()
            );

            await LogHelper.AddLog(order.Id, $"target modified quantity = {entryO.FilledQuantity}...").ConfigureAwait(false);
        }
    }
    public interface ITargetHelper
    {
        Task<string> PlaceOrder(TradeOrder order);
        Task Update(TradeOrder order, Order entryO);
    }
}
