using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TradeMaster6000.Shared
{
    public class TickerLog
    {
        public string Log { get; set; }
        public DateTime Timestamp { get; set; }
        public LogType LogType { get; set; }

    }
    public enum LogType
    {
        Order,
        Error,
        Connect,
        Reconnect,
        NoReconnect,
        Close
    }
}
