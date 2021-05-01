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
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class RequestUrlController : ControllerBase
    {
        private readonly ILogger<RequestUrlController> logger;

        public RequestUrlController(ILogger<RequestUrlController> logger, IConfiguration configuration)
        {
            this.logger = logger;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        [HttpGet]
        public RequestUrl Get()
        {
            return new RequestUrl();
        }

        [HttpPost]
        public async Task<IActionResult> Post(RequestUrl requestUrl)
        {
            Kite kite = new Kite(Configuration.GetValue<string>("APIKey"), Debug: true);
            User user = kite.GenerateSession(requestUrl.Url, Configuration.GetValue<string>("AppSecret"));

            kite.SetSessionExpiryHook(() => Console.WriteLine("Need to login again"));

            HttpContext.Session.Set<string>(Configuration.GetValue<string>("AccessToken"), user.AccessToken);
            HttpContext.Session.Set<string>(Configuration.GetValue<string>("PublicToken"), user.PublicToken);

            return LocalRedirect("/orders");
        }
    }
}
