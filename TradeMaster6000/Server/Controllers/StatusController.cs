using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Services;

namespace TradeMaster6000.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]/[action]")]
    public class StatusController : ControllerBase
    {
        private readonly ITickerService tickerService;
        private readonly IBackgroundJobClient backgroundJob;
        public StatusController(ITickerService tickerService, IBackgroundJobClient backgroundJob)
        {
            this.backgroundJob = backgroundJob;
            this.tickerService = tickerService;
        }

        [HttpGet]
        public string IsCandlesOn()
        {
            return tickerService.IsCandlesRunning().ToString();
        }

        [HttpGet]
        public string IsFlushing()
        {
            var stri = tickerService.IsFlushing().ToString();
            return stri;
        }

        [HttpGet]
        public string IsTickManagerOn()
        {
            var stri = tickerService.IsCandleManagerOn().ToString();
            return stri;
        }

        [HttpGet]
        public string IsCandleManagerOn()
        {
            var stri = tickerService.IsTickManagerOn().ToString();
            return stri;
        }
    }
}
