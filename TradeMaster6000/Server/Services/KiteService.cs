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
        private readonly UserManager<ApplicationUser> usermanager;
        readonly ITimeHelper timeHelper;
        private readonly IContextExtension contextExtension;
        public KiteService(IProtectionService protectionService, ITimeHelper timeHelper, UserManager<ApplicationUser> usermanager, IContextExtension contextExtension)
        {
            this.contextExtension = contextExtension;
            this.usermanager = usermanager;
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

        public void InvalidateOne(ApplicationUser user)
        {
            var instance = GetKiteInstance(user.Id);
            var newinstance = instance;
            newinstance.AccessToken = null;
            newinstance.Kite = null;
            KiteInstances.TryUpdate(user.Id, newinstance, instance);
        }

        public void SetAccessToken(string accessToken, ApplicationUser user)
        {
            var instance = GetKiteInstance(user.Id);
            var newinstance = instance;
            newinstance.AccessToken = protectionService.ProtectToken(accessToken);
            var result = KiteInstances.TryUpdate(user.Id, newinstance, instance);
        }
        public string GetAccessToken(string userid)
        {
            var instance = GetKiteInstance(userid);
            if (instance != null)
            {
                return protectionService.UnprotectToken(instance.AccessToken);
            }
            return null;
        }
        public bool IsKiteConnected(ApplicationUser user)
        {
            var instance = GetKiteInstance(user.Id);
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

        public void NewKiteInstance(Kite kite, ApplicationUser user)
        {
            KiteInstances.TryRemove(user.Id, out KiteInstance value);
            KiteInstance kiteInstance = new KiteInstance { Kite = kite };
            KiteInstances.TryAdd(user.Id, kiteInstance);
        }

        public void UpdateKiteInstance(KiteInstance kite, KiteInstance compare, ApplicationUser user)
        {
            KiteInstances.TryUpdate(user.Id, kite, compare);
        }
        public KiteInstance GetKiteInstance(string userid)
        {
            KiteInstances.TryGetValue(userid, out KiteInstance instance);
            return instance;
        }
        public Kite GetKite(ApplicationUser user)
        {
            KiteInstances.TryGetValue(user.Id, out KiteInstance instance);
            return instance.Kite;
        }
    }

    public interface IKiteService
    {
        Kite GetKite(ApplicationUser user);
        void NewKiteInstance(Kite kite, ApplicationUser user);
        void UpdateKiteInstance(KiteInstance kite, KiteInstance compare, ApplicationUser user);
        KiteInstance GetKiteInstance(string userid);
        void SetAccessToken(string accessToken, ApplicationUser user);
        string GetAccessToken(string userid);
        bool IsKiteConnected(ApplicationUser user);
        void InvalidateAll();
        void InvalidateOne(ApplicationUser user);
    }
}
