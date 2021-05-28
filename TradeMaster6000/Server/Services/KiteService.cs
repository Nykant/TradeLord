using KiteConnect;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Helpers;

namespace TradeMaster6000.Server.Services
{
    public class KiteService : IKiteService
    {
        Kite Kite { get; set; } = null;
        string AccessToken { get; set; }
        string RefreshToken { get; set; }
        readonly IProtectionService protectionService;
        readonly ITimeHelper timeHelper;

        public KiteService(IProtectionService protectionService, ITimeHelper timeHelper)
        {
            this.protectionService = protectionService;
            this.timeHelper = timeHelper;
            Task.Run(()=>KiteManager()).ConfigureAwait(false);
        }

        public void Invalidate()
        {
            if(Kite != null)
            {
                try
                {
                    Kite.InvalidateAccessToken(GetAccessToken());
                }
                catch { }
                Kite = null;
            }
        }

        private async Task KiteManager()
        {
            while (true)
            {
                if(await timeHelper.IsRefreshTime())
                {
                    Invalidate();
                }

                await Task.Delay(30000);
            }
        }
        public void SetAccessToken(string accessToken)
        {
            AccessToken = protectionService.ProtectToken(accessToken);
        }
        public string GetAccessToken()
        {
            return protectionService.UnprotectToken(AccessToken);
        }
        public void SetRefreshToken(string refreshToken)
        {
            RefreshToken = protectionService.ProtectToken(refreshToken);
        }
        
        public string GetRefreshToken()
        {
            return protectionService.UnprotectToken(RefreshToken);
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
        void SetKite(Kite kite);
        Kite GetKite();
        void SetAccessToken(string accessToken);
        string GetAccessToken();
        void SetRefreshToken(string refreshToken);
        string GetRefreshToken();
        void Invalidate();
    }
}
