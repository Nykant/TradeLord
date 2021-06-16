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
        public bool IsCandlesOn()
        {
            return tickerService.IsCandlesRunning();
        }

        [HttpGet]
        public void StopCandles()
        {
            tickerService.StopCandles();
        }

        [HttpGet]
        public void StopFlushing()
        {
            tickerService.CancelToken();
        }

        [HttpGet]
        public void StartFlushing()
        {
            backgroundJob.Enqueue(() => tickerService.StartFlushing(tickerService.GetToken()));
        }

        [HttpGet]
        public bool IsFlushing()
        {
            return tickerService.IsFlushing();
        }

        [HttpGet]
        public bool IsTickerInUse()
        {
            return tickerService.IsTickerInUse();
        }
    }
}
