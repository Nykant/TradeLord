using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Helpers;
using TradeMaster6000.Server.Models;
using TradeMaster6000.Server.Services;

namespace TradeMaster6000.Server.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    [ApiController]
    public class TradingController : ControllerBase
    {
        private readonly IKiteService kiteService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IProtectionService protectionService;
        private readonly IOrderManagerService orderManagerService;
        private readonly ITimeHelper timeHelper;
        private readonly ITradeabilityService tradeabilityService;
        private readonly ITickerService tickerService;
        private readonly IZoneService zoneService;
        private readonly IBackgroundJobClient backgroundJob;
        public TradingController(IBackgroundJobClient backgroundJob, IZoneService zoneService, ITickerService tickerService, ITradeabilityService tradeabilityService, IProtectionService protectionService, UserManager<ApplicationUser> _userManager, IKiteService kiteService, IOrderManagerService orderManagerService, ITimeHelper timeHelper)
        {
            this.tradeabilityService = tradeabilityService;
            this.tickerService = tickerService;
            this.zoneService = zoneService;
            this.backgroundJob = backgroundJob;
            this.timeHelper = timeHelper;
            this.kiteService = kiteService;
            this.userManager = _userManager;
            this.protectionService = protectionService;
            this.orderManagerService = orderManagerService;
        }

        [HttpGet]
        public void EnterTrades()
        {
            orderManagerService.EnterTradeQueue(User.Identity.Name);
        }

        [HttpGet]
        public async Task StartTrader()
        {
            DateTime current = timeHelper.CurrentTime();
            DateTime opening = timeHelper.OpeningTime();
            TimeSpan duration = timeHelper.GetDuration(opening, current);

            var user = await userManager.FindByNameAsync(User.Identity.Name);
            backgroundJob.Schedule(() => RunTrader(user), duration);
        }

        [HttpGet]
        public void StopTrader()
        {
            tradeabilityService.Stop();
            tickerService.StopCandles();
            zoneService.CancelToken();
        }

        // -------------------------------------------------------------------------------

        public async Task RunTrader(ApplicationUser user)
        {
            if (!tickerService.IsCandlesRunning() && !zoneService.IsZoneServiceRunning())
            {
                await tickerService.RunCandles(user);
                await zoneService.StartZoneService();
            }
        }
    }
}
