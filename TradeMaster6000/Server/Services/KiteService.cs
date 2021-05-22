using KiteConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TradeMaster6000.Server.Services
{
    public class KiteService : IKiteService
    {
        private static Kite Kite { get; set; }
        private static string AccessToken { get; set; }
        private readonly IProtectionService protectionService;
        public KiteService(IProtectionService protectionService)
        {
            this.protectionService = protectionService;
        }
        public void SetAccessToken(string accessToken)
        {
            AccessToken = protectionService.ProtectAccessToken(accessToken);
        }
        public string GetAccessToken()
        {
            return protectionService.UnprotectAccessToken(AccessToken);
        }

        public void SetKite(Kite kite)
        {
            Kite = kite;
        }
        public Kite GetKite()
        {
            return Kite;
        }

    }

    public interface IKiteService
    {
        public void SetKite(Kite kite);
        public Kite GetKite();
        void SetAccessToken(string accessToken);
        string GetAccessToken();
    }
}
