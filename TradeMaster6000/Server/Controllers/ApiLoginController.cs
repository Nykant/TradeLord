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
using TradeMaster6000.Server.Models;
using TradeMaster6000.Server.Services;

namespace TradeMaster6000.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]/[action]")]
    public class ApiLoginController : ControllerBase
    {
        private readonly IKiteService kiteService;
        private readonly UserManager<ApplicationUser> _userManager;
        IConfiguration Configuration { get; set; }
        private readonly IProtectionService protectionService;


        public ApiLoginController(IConfiguration configuration, IKiteService _kiteService, UserManager<ApplicationUser> _userManager, IProtectionService protectionService)
        {
            this.protectionService = protectionService;
            this._userManager = _userManager;
            Configuration = configuration;
            kiteService = _kiteService;
        }

        [HttpGet]
        public async Task<string> Login()
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null || user.ApiKey == null || user.AppSecret == null)
            {
                return null;
            }

            var kite = new Kite(protectionService.UnprotectApiKey(user.ApiKey), Debug: true);
            kiteService.NewKiteInstance(kite, user);

            return await Task.Run(()=>kite.GetLoginURL());
        }

        [HttpGet]
        public async Task Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                kiteService.InvalidateOne(user);
            }
        }

        [HttpGet]
        public async Task<string> IsLoggedOn()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                if (kiteService.IsKiteConnected(user))
                {
                    return "true";
                }
            }
            return "false";
        }

        //[AllowAnonymous]
        //[HttpGet]
        //public async Task<string> IsUserLoggedIn()
        //{
        //    return signinManager.IsSignedIn(User).ToString();
        //}
    }
}
