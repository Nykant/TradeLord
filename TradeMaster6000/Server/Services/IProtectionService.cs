using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TradeMaster6000.Server.Services
{
    public class ProtectionService : IProtectionService
    {
        private readonly IDataProtector tokenProtector;
        public ProtectionService(IDataProtectionProvider dataProtectionProvider, IConfiguration configuration)
        {
            tokenProtector = dataProtectionProvider.CreateProtector(configuration["Purpose"]);
        }

        public string ProtectToken(string token)
        {
            return tokenProtector.Protect(token);
        }
        public string UnprotectToken(string token)
        {
            return tokenProtector.Unprotect(token);
        }
    }
    public interface IProtectionService
    {
        string ProtectToken(string accessToken);
        string UnprotectToken(string accessToken);
    }
}
