using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Models;
using TradeMaster6000.Server.Services;

namespace TradeMaster6000.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TradeController : ControllerBase
    {
        private readonly IKiteService kiteService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProtectionService protectionService;
        public TradeController(IProtectionService protectionService, UserManager<ApplicationUser> _userManager, IKiteService kiteService)
        {
            this.kiteService = kiteService;
            this._userManager = _userManager;
            this.protectionService = protectionService;
        }


    }
}
