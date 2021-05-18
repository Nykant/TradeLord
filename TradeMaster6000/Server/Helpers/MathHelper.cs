using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TradeMaster6000.Server.Helpers
{
    public static class MathHelper
    {
        public static decimal RoundUp(decimal value, decimal step)
        {
            var multiplicand = Math.Ceiling(value / step);
            return step * multiplicand;
        }

        public static decimal RoundDown(decimal value, decimal step)
        {
            var multiplicand = Math.Floor(value / step);
            return step * multiplicand;
        }
    }
}
