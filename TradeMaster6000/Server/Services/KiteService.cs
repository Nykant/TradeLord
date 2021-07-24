using KiteConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeMaster6000.Server.Extensions;
using TradeMaster6000.Server.Helpers;
using TradeMaster6000.Server.Models;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Services
{
    public class KiteService : IKiteService
    {
        readonly IProtectionService protectionService;
        private static readonly ConcurrentDictionary<string, KiteInstance> KiteInstances = new ConcurrentDictionary<string, KiteInstance>();
        readonly ITimeHelper timeHelper;
        private readonly IContextExtension contextExtension;
        public KiteService(IProtectionService protectionService, ITimeHelper timeHelper, IContextExtension contextExtension)
        {
            this.contextExtension = contextExtension;
            this.protectionService = protectionService;
            this.timeHelper = timeHelper;
        }

        public void InvalidateAll()
        {
            var keypairs = KiteInstances.ToList();
            foreach(var keypair in keypairs)
            {
                var newinstance = keypair.Value;
                newinstance.AccessToken = null;
                newinstance.Kite = null;
                KiteInstances.TryUpdate(keypair.Key, newinstance, keypair.Value);
            }
        }

        public void InvalidateOne(string username)
        {
            var instance = GetKiteInstance(username);
            var newinstance = instance;
            newinstance.AccessToken = null;
            newinstance.Kite = null;
            KiteInstances.TryUpdate(username, newinstance, instance);
        }

        public void SetAccessToken(string accessToken, string username)
        {
            var instance = GetKiteInstance(username);
            var newinstance = instance;
            newinstance.AccessToken = protectionService.ProtectToken(accessToken);
            var result = KiteInstances.TryUpdate(username, newinstance, instance);
        }

        public string GetAccessToken(string username)
        {
            var instance = GetKiteInstance(username);
            if (instance != null)
            {
                return protectionService.UnprotectToken(instance.AccessToken);
            }
            return null;
        }

        public bool IsKiteConnected(string username)
        {
            var instance = GetKiteInstance(username);
            if (instance != null)
            {
                if (instance.AccessToken != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        public void NewKiteInstance(Kite kite, string username)
        {
            if (KiteInstances.ContainsKey(username))
            {
                KiteInstances.TryRemove(username, out _);
            }
            KiteInstance kiteInstance = new KiteInstance { Kite = kite };
            KiteInstances.TryAdd(username, kiteInstance);
        }

        public void UpdateKiteInstance(KiteInstance kite, KiteInstance compare, string username)
        {
            KiteInstances.TryUpdate(username, kite, compare);
        }

        public KiteInstance GetKiteInstance(string username)
        {
            KiteInstances.TryGetValue(username, out KiteInstance instance);
            return instance;
        }

        public Kite GetKite(string username)
        {
            KiteInstances.TryGetValue(username, out KiteInstance instance);
            return instance.Kite;
        }
    }

    public interface IKiteService
    {
        Kite GetKite(string username);
        void NewKiteInstance(Kite kite, string username);
        void UpdateKiteInstance(KiteInstance kite, KiteInstance compare, string username);
        KiteInstance GetKiteInstance(string username);
        void SetAccessToken(string accessToken, string username);
        string GetAccessToken(string username);
        bool IsKiteConnected(string username);
        void InvalidateAll();
        void InvalidateOne(string username);
    }
}
