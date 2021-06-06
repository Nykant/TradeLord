using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
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
        private IWebHostEnvironment Env { get; set; }
        public TimeHelper(ITradeLogHelper tradeLogHelper, IWebHostEnvironment env)
        {
            LogHelper = tradeLogHelper;
            Env = env;
        }

        public async Task<bool> IsPreMarketOpen(int orderId)
        {
            if (Env.IsDevelopment())
            {
                // check time once in a while, to figure out if it is time to wake up and go to work.
                DateTime GST = DateTime.Now;
                DateTime IST = GST.AddHours(3).AddMinutes(30);
                DateTime opening = new (IST.Year, IST.Month, IST.Day, 22, 00, 00);
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
            else
            {
                // check time once in a while, to figure out if it is time to wake up and go to work.
                DateTime GST = DateTime.Now;
                DateTime IST = GST.AddHours(5).AddMinutes(30);
                DateTime opening = new (IST.Year, IST.Month, IST.Day, 22, 00, 00);
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
        }

        public bool IsPreMarketOpen()
        {
            if (Env.IsDevelopment())
            {
                // check time once in a while, to figure out if it is time to wake up and go to work.
                DateTime GST = DateTime.Now;
                DateTime IST = GST.AddHours(3).AddMinutes(30);
                DateTime opening = new(IST.Year, IST.Month, IST.Day, 22, 00, 00);
                DateTime closing = opening.AddHours(6).AddMinutes(30);
                // if clock is 9 its time to get up and start the day!
                if (DateTime.Compare(IST, opening) >= 0)
                {
                    if (DateTime.Compare(IST, closing) < 0)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                // check time once in a while, to figure out if it is time to wake up and go to work.
                DateTime GST = DateTime.Now;
                DateTime IST = GST.AddHours(5).AddMinutes(30);
                DateTime opening = new(IST.Year, IST.Month, IST.Day, 22, 00, 00);
                DateTime closing = opening.AddHours(6).AddMinutes(30);
                // if clock is 9 its time to get up and start the day!
                if (DateTime.Compare(IST, opening) >= 0)
                {
                    if (DateTime.Compare(IST, closing) < 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsMarketEnding()
        {
            if (Env.IsDevelopment())
            {
                DateTime IST = DateTime.Now.AddHours(3).AddMinutes(30);
                if (IST.Hour == 00 && IST.Minute == 00)
                {
                    return true;
                }
                return false;
            }
            else
            {
                DateTime IST = DateTime.Now.AddHours(5).AddMinutes(30);
                if (IST.Hour == 00 && IST.Minute == 00)
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsMarketEnded()
        {
            if (Env.IsDevelopment())
            {
                DateTime IST = DateTime.Now.AddHours(3).AddMinutes(30);
                if (IST.Hour == 00 && IST.Minute == 35)
                {
                    return true;
                }
                return false;
            }
            else
            {
                DateTime IST = DateTime.Now.AddHours(5).AddMinutes(30);
                if (IST.Hour == 00 && IST.Minute == 35)
                {
                    return true;
                }
                return false;
            }
        }

        public string GetCurrentVariety()
        {
            if (Env.IsDevelopment())
            {
                string variety = null;

                DateTime GST1 = DateTime.Now;
                DateTime IST1 = GST1.AddHours(3).AddMinutes(30);
                DateTime opening1 = new (IST1.Year, IST1.Month, IST1.Day, 22, 15, 0);
                DateTime closing1 = opening1.AddHours(6).AddMinutes(15);

                if (DateTime.Compare(IST1, opening1) < 0)
                {
                    variety = "amo";
                }
                else if (DateTime.Compare(IST1, opening1) >= 0)
                {
                    variety = "regular";
                }
                if (DateTime.Compare(IST1, closing1) >= 0)
                {
                    variety = "amo";
                }

                return variety;
            }
            else
            {
                string variety = null;

                DateTime GST1 = DateTime.Now;
                DateTime IST1 = GST1.AddHours(5).AddMinutes(30);
                DateTime opening1 = new (IST1.Year, IST1.Month, IST1.Day, 22, 15, 0);
                DateTime closing1 = opening1.AddHours(6).AddMinutes(15);

                if (DateTime.Compare(IST1, opening1) < 0)
                {
                    variety = "amo";
                }
                else if (DateTime.Compare(IST1, opening1) >= 0)
                {
                    variety = "regular";
                }
                if (DateTime.Compare(IST1, closing1) >= 0)
                {
                    variety = "amo";
                }

                return variety;
            }
            

        }

        public async Task<bool> IsMarketOpen(int orderId)
        {
            if (Env.IsDevelopment())
            {
                DateTime GMT = DateTime.Now;
                DateTime IST = GMT.AddHours(3).AddMinutes(30);
                DateTime opening = new (IST.Year, IST.Month, IST.Day, 22, 15, 00);
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
            else
            {
                DateTime GMT = DateTime.Now;
                DateTime IST = GMT.AddHours(5).AddMinutes(30);
                DateTime opening = new (IST.Year, IST.Month, IST.Day, 22, 15, 00);
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
        public bool IsMarketOpen()
        {
            if (Env.IsDevelopment())
            {
                DateTime GMT = DateTime.Now;
                DateTime IST = GMT.AddHours(3).AddMinutes(30);
                DateTime opening = new(IST.Year, IST.Month, IST.Day, 22, 15, 00);
                DateTime closing = opening.AddHours(6).AddMinutes(15);
                if (DateTime.Compare(IST, opening) >= 0)
                {
                    if (DateTime.Compare(IST, closing) < 0)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                DateTime GMT = DateTime.Now;
                DateTime IST = GMT.AddHours(5).AddMinutes(30);
                DateTime opening = new(IST.Year, IST.Month, IST.Day, 22, 15, 00);
                DateTime closing = opening.AddHours(6).AddMinutes(15);
                if (DateTime.Compare(IST, opening) >= 0)
                {
                    if (DateTime.Compare(IST, closing) < 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsRefreshTime()
        {

            if (Env.IsDevelopment())
            {
                DateTime IST = DateTime.Now.AddHours(3).AddMinutes(30);
                if (IST.Hour == 5 && IST.Minute == 29)
                {
                    return true;
                }
                return false;
            }
            else
            {
                DateTime IST = DateTime.Now.AddHours(5).AddMinutes(30);
                if (IST.Hour == 5 && IST.Minute == 29)
                {
                    return true;
                }
                return false;
            }

        }
    }
    public interface ITimeHelper
    {
        Task<bool> IsPreMarketOpen(int orderId);
        Task<bool> IsMarketOpen(int orderId);
        bool IsPreMarketOpen();
        bool IsMarketOpen();
        string GetCurrentVariety();
        bool IsMarketEnding();
        bool IsMarketEnded();
        bool IsRefreshTime();

    }
}
