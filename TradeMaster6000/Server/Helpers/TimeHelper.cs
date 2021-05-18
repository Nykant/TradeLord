using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.DataHelpers;

namespace TradeMaster6000.Server.Helpers
{
    public class TimeHelper : ITimeHelper
    {
        private ITradeLogHelper LogHelper { get; set; }
        public TimeHelper(ITradeLogHelper tradeLogHelper)
        {
            LogHelper = tradeLogHelper;
        }

        public async Task<bool> IsPreMarketOpen(int orderId)
        {
            // check time once in a while, to figure out if it is time to wake up and go to work.
            DateTime GST = DateTime.Now;
            DateTime IST = GST.AddHours(5).AddMinutes(30);
            DateTime opening = new DateTime(IST.Year, IST.Month, IST.Day, 9, 00, 00);
            DateTime closing = opening.AddHours(6).AddMinutes(30);
            // if clock is 9 its time to get up and start the day!
            if (DateTime.Compare(IST, opening) >= 0)
            {
                if (DateTime.Compare(IST, closing) < 0)
                {
                    await LogHelper.AddLog(orderId, $"pre market is opening...").ConfigureAwait(false);
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> IsMarketOpen(int orderId)
        {
            DateTime GMT = DateTime.Now;
            DateTime IST = GMT.AddHours(5).AddMinutes(30);
            DateTime opening = new DateTime(IST.Year, IST.Month, IST.Day, 9, 15, 00);
            DateTime closing = opening.AddHours(6).AddMinutes(15);
            if (DateTime.Compare(IST, opening) >= 0)
            {
                if (DateTime.Compare(IST, closing) < 0)
                {
                    await LogHelper.AddLog(orderId, $"market is open...").ConfigureAwait(false);
                    return true;
                }
            }
            return false;
        }
    }
    public interface ITimeHelper
    {
        Task<bool> IsPreMarketOpen(int orderId);
        Task<bool> IsMarketOpen(int orderId);
    }
}
