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
    public class LoginUriController : ControllerBase
    {
        private readonly ILogger<RequestUrlController> logger;

        public LoginUriController(ILogger<RequestUrlController> logger, IConfiguration configuration)
        {
            this.logger = logger;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        [HttpGet]
        public LoginUri Get()
        {
            Kite kite = new Kite(Configuration.GetValue<string>("APIKey"), Debug: true);
            logger.Log(LogLevel.Debug, "inside get method");

            string accessToken = HttpContext.Session.Get<string>(Configuration.GetValue<string>("AccessToken"));
            //HttpContext.Session.Get<string>(Configuration.GetValue<string>("PublicTokenPassword"));

            return new LoginUri { Uri = kite.GetLoginURL(), AccessToken = accessToken};
        }
    }
}
