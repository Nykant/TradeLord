﻿using KiteConnect;
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
        string AccessToken { get; set; } = null;
        readonly IProtectionService protectionService;
        readonly ITimeHelper timeHelper;

        public KiteService(IProtectionService protectionService, ITimeHelper timeHelper)
        {
            this.protectionService = protectionService;
            this.timeHelper = timeHelper;
            Task.Run(async()=> await KiteManager()).ConfigureAwait(false);
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
                AccessToken = null;
                Kite = null;
            }
        }

        private async Task KiteManager()
        {
            while (true)
            {
                if(timeHelper.IsRefreshTime())
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
        public bool IsKiteConnected()
        {
            if(AccessToken != null)
            {
                return true;
            }
            else
            {
                return false;
            }
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
        bool IsKiteConnected();
        void Invalidate();
    }
}
