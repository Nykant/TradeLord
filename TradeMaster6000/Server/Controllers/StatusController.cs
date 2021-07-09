using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IZoneService zoneService;
        private readonly IWebHostEnvironment env;
        public StatusController(ITickerService tickerService, IBackgroundJobClient backgroundJob, IZoneService zoneService, IWebHostEnvironment env)
        {
            this.backgroundJob = backgroundJob;
            this.tickerService = tickerService;
            this.zoneService = zoneService;
            this.env = env;
        }

        [HttpGet]
        public string IsCandlesOn()
        {
            return tickerService.IsCandlesRunning().ToString();
        }

        [HttpGet]
        public string IsFlushing()
        {
            return tickerService.IsFlushing().ToString();
        }

        [HttpGet]
        public string IsTickManagerOn()
        {
            return tickerService.IsTickManagerOn().ToString();

        }

        [HttpGet]
        public string IsCandleManagerOn()
        {
            return tickerService.IsCandleManagerOn().ToString();
        }

        [HttpGet]
        public string IsTickerOn()
        {
            return tickerService.IsTheTickerRunning().ToString();
        }

        [HttpGet]
        public string IsZoneServiceOn()
        {
            return zoneService.IsZoneServiceRunning().ToString();
        }

        [HttpGet]
        public string GetEnvironment()
        {
            return env.EnvironmentName;
        }
    }
}
