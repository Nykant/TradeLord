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
        IConfiguration Configuration { get; set; }

        public ApiLoginController(IConfiguration configuration, IKiteService _kiteService)
        {
            Configuration = configuration;
            kiteService = _kiteService;
        }

        [HttpGet]
        public Task<string> Login()
        {
            kiteService.Invalidate();

            var kite = new Kite(Configuration.GetValue<string>("APIKey"), Debug: true);
            kiteService.SetKite(kite);

            return Task.Run(()=>kite.GetLoginURL());
        }

        [HttpGet]
        public Task Logout()
        {
            kiteService.Invalidate();
            return Task.FromResult(0);
        }

        [HttpGet]
        public async Task<string> IsLoggedOn()
        {
            return await Task.Run(() =>
            {
                if (kiteService.GetKite() == null)
                {
                    return "false";
                }
                else
                {
                    return "true";
                }
            });
        }

        //[AllowAnonymous]
        //[HttpGet]
        //public async Task<string> IsUserLoggedIn()
        //{
        //    return signinManager.IsSignedIn(User).ToString();
        //}
    }
}
