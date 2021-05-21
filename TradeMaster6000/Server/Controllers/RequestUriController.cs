using KiteConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Extensions;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class RequestUriController : ControllerBase
    {
        private readonly ILogger<RequestUriController> logger;
        private readonly IKiteService kiteService;
        private readonly IInstrumentService instrumentService;
        private readonly IProtectionService protectionService;

        public RequestUriController(ILogger<RequestUriController> logger, IConfiguration configuration, IKiteService _kiteService, IInstrumentService instrumentService, IProtectionService protectionService)
        {
            this.logger = logger;
            Configuration = configuration;
            kiteService = _kiteService;
            this.instrumentService = instrumentService;
            this.protectionService = protectionService;
        }

        public IConfiguration Configuration { get; }

        [HttpPost]
        public async Task<IActionResult> Post(RequestUri requestUri)
        {
            if(requestUri.Request_token == null)
            {
                logger.LogInformation("request_token er null");
                return BadRequest();
            }
            if(requestUri.Status == "success")
            {
                try
                {
                    logger.LogInformation("trying to connect kite");
                    Kite kite = kiteService.GetKite();

                    User user = kite.GenerateSession(requestUri.Request_token, Configuration.GetValue<string>("AppSecret"));

                    kite.SetAccessToken(user.AccessToken);
                    kite.SetSessionExpiryHook(() => logger.LogInformation("User need to log in again"));

                    var accessToken = protectionService.ProtectAccessToken(user.AccessToken);
                    HttpContext.Session.Set<string>(Configuration.GetValue<string>("AccessToken"), accessToken);
                    HttpContext.Session.Set<string>(Configuration.GetValue<string>("PublicToken"), user.PublicToken);
                }
                catch (Exception e)
                {
                    logger.LogInformation(e.Message);
                }
            }
            else if(requestUri.Status == null)
            {
                logger.LogInformation("status er null");
            }
            else
            {
                logger.LogInformation("status ik success");
                return BadRequest();
            }

            logger.LogInformation("success");
            return Ok();
        }
    }
}
