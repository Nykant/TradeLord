using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TradeMaster6000.Server.Extensions
{
    public class ContextExtension : IContextExtension
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        public ContextExtension(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public HttpContext GetContext()
        {
            return httpContextAccessor.HttpContext;
        }
    }
    public interface IContextExtension
    {
        HttpContext GetContext();
    }
}
