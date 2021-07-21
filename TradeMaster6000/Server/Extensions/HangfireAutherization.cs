using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TradeMaster6000.Server.Models;
using Microsoft.AspNetCore.Authorization;

namespace TradeMaster6000.Server.Extensions
{
    public class HangfireAutherization : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext dashboardContext)
        {
            var value = dashboardContext.GetHttpContext().User.Identity.IsAuthenticated;
            //var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext.User);
            //if(user.)
            return true;
        }
    }
}
