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
        private readonly IDataProtector apiKeyProtector;
        private readonly IDataProtector appSecretProtector;
        public ProtectionService(IDataProtectionProvider dataProtectionProvider, IConfiguration configuration)
        {
            tokenProtector = dataProtectionProvider.CreateProtector(configuration["tokenPurpose"]);
            apiKeyProtector = dataProtectionProvider.CreateProtector(configuration["apiKeyPurpose"]);
            appSecretProtector = dataProtectionProvider.CreateProtector(configuration["appSecretPurpose"]);
        }

        public string ProtectToken(string token)
        {
            return tokenProtector.Protect(token);
        }
        public string UnprotectToken(string token)
        {
            return tokenProtector.Unprotect(token);
        }

        public string ProtectApiKey(string apikey)
        {
            return apiKeyProtector.Protect(apikey);
        }
        public string UnprotectApiKey(string apikey)
        {
            return apiKeyProtector.Unprotect(apikey);
        }

        public string ProtectAppSecret(string appsecret)
        {
            return appSecretProtector.Protect(appsecret);
        }
        public string UnprotectAppSecret(string appsecret)
        {
            return appSecretProtector.Unprotect(appsecret);
        }
    }
    public interface IProtectionService
    {
        string UnprotectAppSecret(string appsecret);
        string ProtectAppSecret(string appsecret);
        string UnprotectApiKey(string apikey);
        string ProtectApiKey(string apikey);
        string ProtectToken(string accessToken);
        string UnprotectToken(string accessToken);
    }
}
