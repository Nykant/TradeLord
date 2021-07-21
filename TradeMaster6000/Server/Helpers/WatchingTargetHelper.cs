using KiteConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Server.Models;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Helpers
{
    public class WatchingTargetHelper : IWatchingTargetHelper
    {
        private readonly IKiteService kiteService;
        private readonly ITradeLogHelper tradeLogHelper;
        public WatchingTargetHelper(IKiteService kiteService, ITradeLogHelper tradeLogHelper)
        {
            this.kiteService = kiteService;
            this.tradeLogHelper = tradeLogHelper;
        }

        public async Task SquareOff(OrderUpdate entry, OrderUpdate targetO, TradeOrder order, ApplicationUser user)
        {
            var squareOffQuantity = entry.FilledQuantity - targetO.FilledQuantity;
            var kite = kiteService.GetKite(user);
            kite.PlaceOrder(
                 Exchange: order.Instrument.Exchange,
                 TradingSymbol: order.Instrument.TradingSymbol,
                 TransactionType: order.ExitTransactionType,
                 Quantity: squareOffQuantity,
                 Product: Constants.PRODUCT_MIS,
                 OrderType: Constants.ORDER_TYPE_MARKET,
                 Validity: Constants.VALIDITY_DAY,
                 Variety: Constants.VARIETY_REGULAR
            );

            await tradeLogHelper.AddLog(order.Id, $"squared off...").ConfigureAwait(false);
        }

    }
    public interface IWatchingTargetHelper
    {
        Task SquareOff(OrderUpdate entry, OrderUpdate targetO, TradeOrder order, ApplicationUser user);
    }
}
