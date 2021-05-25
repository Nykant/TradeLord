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
        readonly IConfiguration configuration;
        readonly ITimeHelper timeHelper;
        readonly CancellationTokenSource source;
        public KiteService(IProtectionService protectionService, IConfiguration configuration, ITimeHelper timeHelper)
        {
            this.protectionService = protectionService;
            this.configuration = configuration;
            this.timeHelper = timeHelper;
            source = new CancellationTokenSource();
            Task.Run(() => RefreshAccessTokenAsync(source.Token)).ConfigureAwait(false);
        }

        private async Task RefreshAccessTokenAsync(CancellationToken token)
        {
            while (true)
            {
                if (await timeHelper.IsRefreshTime())
                {
                    var tokenSet = Kite.RenewAccessToken(GetRefreshToken(), configuration.GetValue<string>("AppSecret"));
                    SetAccessToken(tokenSet.AccessToken);
                    SetRefreshToken(tokenSet.RefreshToken);
                    await Task.Delay(72000000, token);
                }
                if (token.IsCancellationRequested)
                {
                    break;
                }
                await Task.Delay(30000, token);
            }
        }

        public void Invalidate()
        {
            source.Cancel();
            Kite.InvalidateAccessToken();
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
