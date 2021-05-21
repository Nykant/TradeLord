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
    public class WatchingTargetHelper : IWatchingTargetHelper
    {
        private ITradeLogHelper LogHelper { get; set; }
        private ITickerService TickService { get; set; }
        private Kite Kite { get; set; }
        public WatchingTargetHelper(IKiteService kiteService, ITradeLogHelper logHelper, ITickerService tickerService)
        {
            Kite = kiteService.GetKite();
            LogHelper = logHelper;
            TickService = tickerService;
        }

        public void SquareOff(Order entry, Order targetO, TradeOrder order)
        {
            var squareOffQuantity = entry.FilledQuantity - targetO.FilledQuantity;

            Kite.PlaceOrder(
                 Exchange: order.Instrument.Exchange,
                 TradingSymbol: order.Instrument.TradingSymbol,
                 TransactionType: order.ExitTransactionType,
                 Quantity: squareOffQuantity,
                 Product: Constants.PRODUCT_MIS,
                 OrderType: Constants.ORDER_TYPE_MARKET,
                 Validity: Constants.VALIDITY_DAY,
                 Variety: Constants.VARIETY_REGULAR
            );
        }

    }
    public interface IWatchingTargetHelper
    {
        void SquareOff(Order entry, Order targetO, TradeOrder order);
    }
}
