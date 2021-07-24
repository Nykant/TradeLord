using KiteConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Extensions;
using TradeMaster6000.Server.Models;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]/[action]")]
    public class RequestUriController : ControllerBase
    {
        private readonly ILogger<RequestUriController> logger;
        private readonly IKiteService kiteService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProtectionService protectionService;

        public RequestUriController(ILogger<RequestUriController> logger, IConfiguration configuration, IKiteService _kiteService, UserManager<ApplicationUser> _userManager, IProtectionService protectionService)
        {
            this._userManager = _userManager;
            this.protectionService = protectionService;
            this.logger = logger;
            Configuration = configuration;
            kiteService = _kiteService;
        }

        public IConfiguration Configuration { get; }

        [HttpPost]
        public async Task<IActionResult> Post(RequestUri requestUri)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return BadRequest();
            }
            if (requestUri.Request_token == null)
            {
                logger.LogInformation("request_token er null");
                return BadRequest();
            }
            if(requestUri.Status == "success")
            {
                try
                {
                    logger.LogInformation("trying to connect kite");
                    KiteInstance instance = kiteService.GetKiteInstance(user.UserName);
                    KiteInstance newinstance = instance;
                    var appsecret = protectionService.UnprotectAppSecret(user.AppSecret);
                    User kiteuser = newinstance.Kite.GenerateSession(requestUri.Request_token, appsecret);
                    newinstance.Kite.SetAccessToken(kiteuser.AccessToken);
                    newinstance.AccessToken = kiteuser.AccessToken;
                    newinstance.Kite.SetSessionExpiryHook(() => kiteService.InvalidateOne(user.UserName));
                    kiteService.UpdateKiteInstance(newinstance, instance, user.UserName);
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
