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
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if(user == null || user.ApiKey == null || user.AppSecret == null)
            {
                return null;
            }
            var apikey = protectionService.UnprotectApiKey(user.ApiKey);
            var kite = new Kite(apikey, Debug: true);
            kiteService.NewKiteInstance(kite, user.UserName);

            return await Task.Run(()=>kite.GetLoginURL());
        }

        [HttpGet]
        public void Logout()
        {
            if (User.Identity.Name != null)
            {
                kiteService.InvalidateOne(User.Identity.Name);
            }
        }

        [HttpGet]
        public string IsLoggedOn()
        {
            if (User.Identity.Name != null)
            {
                if (kiteService.IsKiteConnected(User.Identity.Name))
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
