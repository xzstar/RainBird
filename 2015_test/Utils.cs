using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trade2015;

namespace ConsoleProxy
{
    public class Utils
    {
        public static bool isBeforeTradingTime(ExchangeStatusType type, DateTime dt)
        {
            if (dt.Hour == 8 && dt.Minute == 59)
                return true;
            if (dt.Hour == 20 && dt.Minute == 59)
                return true;
            
            return false;
        }

        public static bool isTradingTime(string instrument, DateTime d1)
        {
            if ((d1.Hour > 3 && d1.Hour < 9) || (d1.Hour > 11 && d1.Hour < 13) || (d1.Hour > 15 && d1.Hour < 21))
            {
                Console.WriteLine("onRtnTick:{0},收到非交易时间{1} Tick数据，忽略", instrument, d1.ToShortTimeString());
                return false;
            }
            return true;
        }

        public static bool isTradingTimeNow()
        {
            int hour = DateTime.Now.ToLocalTime().Hour;
            int min = DateTime.Now.ToLocalTime().Minute;

            if (hour == 0 || hour == 1 || (hour == 2 && min <= 30)
                || hour == 9 || hour == 10 || (hour == 11 && min <= 30)
                || (hour == 13 && min >= 30) || hour == 14 || (hour == 15 && min == 0)
                || (hour >= 21 && hour <= 23))
                return true;

            return false;
        }
        public static bool isLogoutTimeNow()
        {
            int hour = DateTime.Now.ToLocalTime().Hour;
            int min = DateTime.Now.ToLocalTime().Minute;

            if (hour == 2 && min > 30 && min <= 59)
                return true;

            if (hour == 11 && min > 30 && min <= 59)
                return true;

            if (hour == 15 && min > 0 && min <= 30)
                return true;

            return false;
        }

        public static bool isLogInTimeNow()
        {
            int hour = DateTime.Now.ToLocalTime().Hour;
            int min = DateTime.Now.ToLocalTime().Minute;

            if (hour == 8 && min >=30 && min <= 59)
                return true;

            if (hour == 13 && min >=0 && min <= 29)
                return true;

            if (hour == 20 && min >=30 && min <= 59)
                return true;

            return false;
        }

        public static bool isOverDayNow()
        {
            int hour = DateTime.Now.ToLocalTime().Hour;
            int min = DateTime.Now.ToLocalTime().Minute;

            if (hour == 15 && min > 0 && min <= 30)
                return true;

            return false;
        }
    }
}
