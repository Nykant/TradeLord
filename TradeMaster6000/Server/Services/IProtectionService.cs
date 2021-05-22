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
        private readonly IDataProtector accessTokeProt;
        public ProtectionService(IDataProtectionProvider dataProtectionProvider, IConfiguration configuration)
        {
            accessTokeProt = dataProtectionProvider.CreateProtector(configuration["AccessPurpose"]);
        }

        public string ProtectAccessToken(string accessToken)
        {
            return accessTokeProt.Protect(accessToken);
        }
        public string UnprotectAccessToken(string accessToken)
        {
            return accessTokeProt.Unprotect(accessToken);
        }
    }
    public interface IProtectionService
    {
        string ProtectAccessToken(string accessToken);
        string UnprotectAccessToken(string accessToken);
    }
}
