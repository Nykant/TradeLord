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
        private IWebHostEnvironment Env { get; set; }
        public TimeHelper(IWebHostEnvironment env)
        {
            Env = env;
        }

        public DateTime GetWaittime(DateTime current)
        {
            DateTime waittime = OpeningTime();
            if (DateTime.Compare(waittime, current) < 0)
            {
                int hour = current.Hour;
                int minute = current.Minute;
                int second = current.Second;
                if (minute == 59)
                {
                    hour++;
                    minute = 0;
                }
                else
                {
                    minute++;
                }

                if (second > 50)
                {
                    minute++;
                }
                waittime = new DateTime(current.Year, current.Month, current.Day, hour, minute, 00);
            }
            return waittime;
        }

        public async Task<bool> IsPreMarketOpen(int orderId)
        {
            if (Env.IsDevelopment())
            {
                // check time once in a while, to figure out if it is time to wake up and go to work.
                DateTime GST = DateTime.Now;
                DateTime IST = GST.AddHours(3).AddMinutes(30);
                DateTime opening = new (IST.Year, IST.Month, IST.Day, 9, 00, 00);
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
                DateTime opening = new (IST.Year, IST.Month, IST.Day, 9, 00, 00);
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

        public bool IsPreMarketOpen()
        {
            if (Env.IsDevelopment())
            {
                // check time once in a while, to figure out if it is time to wake up and go to work.
                DateTime GST = DateTime.Now;
                DateTime IST = GST.AddHours(3).AddMinutes(30);
                DateTime opening = new(IST.Year, IST.Month, IST.Day, 9, 00, 00);
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
                DateTime opening = new(IST.Year, IST.Month, IST.Day, 9, 00, 00);
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
                if (IST.Hour == 15 && IST.Minute == 00)
                {
                    return true;
                }
                return false;
            }
            else
            {
                DateTime IST = DateTime.Now.AddHours(5).AddMinutes(30);
                if (IST.Hour == 15 && IST.Minute == 00)
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
                if (IST.Hour >= 15 && IST.Minute >= 31)
                {
                    return true;
                }
                return false;
            }
            else
            {
                DateTime IST = DateTime.Now.AddHours(5).AddMinutes(30);
                if (IST.Hour >= 15 && IST.Minute >= 31)
                {
                    return true;
                }
                return false;
            }
        }

        public DateTime CurrentTime()
        {
            if (Env.IsDevelopment())
            {
                return DateTime.Now.AddHours(3).AddMinutes(30);
            }
            else
            {
                return DateTime.Now.AddHours(5).AddMinutes(30);
            }
        }

        public DateTime OpeningTime()
        {
            DateTime now = CurrentTime();
            if(now.Hour >= 16)
            {
                now = now.AddDays(1);
            }
            return new DateTime(now.Year, now.Month, now.Day, 09, 15, 00);
        }

        public TimeSpan GetDuration(DateTime end, DateTime start)
        {
            var time = new TimeSpan(end.Ticks - start.Ticks);
            return time;
        }

        public string GetCurrentVariety()
        {
            if (Env.IsDevelopment())
            {
                string variety = null;

                DateTime GST1 = DateTime.Now;
                DateTime IST1 = GST1.AddHours(3).AddMinutes(30);
                DateTime opening1 = new (IST1.Year, IST1.Month, IST1.Day, 9, 15, 0);
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
                DateTime opening1 = new (IST1.Year, IST1.Month, IST1.Day, 9, 15, 0);
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
                DateTime opening = new (IST.Year, IST.Month, IST.Day, 9, 15, 00);
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
                DateTime opening = new (IST.Year, IST.Month, IST.Day, 9, 15, 00);
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
        public bool IsMarketOpen()
        {
            if (Env.IsDevelopment())
            {
                DateTime GMT = DateTime.Now;
                DateTime IST = GMT.AddHours(3).AddMinutes(30);
                DateTime opening = new(IST.Year, IST.Month, IST.Day, 9, 15, 00);
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
                DateTime opening = new(IST.Year, IST.Month, IST.Day, 9, 15, 00);
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
        DateTime CurrentTime();
        DateTime OpeningTime();
        TimeSpan GetDuration(DateTime end, DateTime start);
        DateTime GetWaittime(DateTime current);

    }
}
