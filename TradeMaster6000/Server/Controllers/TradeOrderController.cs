using KiteConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class TradeOrderController : ControllerBase
    {
        private readonly ILogger<RequestUrlController> logger;

        public TradeOrderController(ILogger<RequestUrlController> logger, IConfiguration configuration)
        {
            this.logger = logger;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        [HttpPost]
        public async Task<IActionResult> Post(TradeOrder tradeOrder)
        {
            Kite kite = new Kite(Configuration.GetValue<string>("APIKey"), Debug: true);

            Dictionary<string, dynamic> response = kite.PlaceOrder(
                Exchange: Constants.EXCHANGE_NSE,
                TradingSymbol: "ASIANPAINTS",
                TransactionType: Constants.TRANSACTION_TYPE_BUY,
                Quantity: 1,
                Price: 64.0000m,
                OrderType: Constants.ORDER_TYPE_SL,
                Product: Constants.PRODUCT_MIS,
                StoplossValue: 63.0000m,
                TriggerPrice: 65.0000m,
                TrailingStoploss: 64.0000m
            );

            return LocalRedirect("/orders");
        }
    }
}
