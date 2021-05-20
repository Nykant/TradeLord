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
        public KiteService()
        {

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
    }
}
