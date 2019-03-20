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
        private static int _MIN_INTERVAL = 15;

        public static bool isBeforeTradingTime(ExchangeStatusType type, DateTime dt)
        {
            if (dt.Hour == 8 && dt.Minute == 59)
                return true;
            if (dt.Hour == 20 && dt.Minute == 59)
                return true;

            return false;
        }

        public static bool isValidData(string instrument, DateTime d1, string updateTime)
        {
            int hour = DateTime.Now.ToLocalTime().Hour;
            if ((hour == 8 || hour == 9) && d1.Hour == 23)
            {
                Console.WriteLine("onRtnTick:{0},收到昨天交易时间{1} Tick数据{2}，忽略", instrument, d1.ToShortTimeString(), updateTime);
                return false;
            }

            return true;
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

            //if (DateTime.Now.ToLocalTime().DayOfWeek == DayOfWeek.Saturday ||
            //   DateTime.Now.ToLocalTime().DayOfWeek == DayOfWeek.Sunday)
            if(HttpHelper.isHoliday())
                return false;

            if (hour == 8 && min >= 30 && min <= 59)
                return true;

            if (hour == 13 && min >= 0 && min <= 29)
                return true;

            if (hour == 20 && min >= 30 && min <= 59)
                return true;

            return false;
        }

        public static bool isSyncPositionTime()
        {
            int hour = DateTime.Now.ToLocalTime().Hour;
            int min = DateTime.Now.ToLocalTime().Minute;

            //if (DateTime.Now.ToLocalTime().DayOfWeek == DayOfWeek.Saturday ||
            //   DateTime.Now.ToLocalTime().DayOfWeek == DayOfWeek.Sunday)
            //    return false;
            if (HttpHelper.isHoliday())
                return false;

            if (hour == 20 && (min == 58 || min == 59))
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

        public static bool isOpenMin(DateTime dt)
        {
            if ((dt.Hour == 9 && dt.Minute == 0)
                || (dt.Hour == 10 && dt.Minute == 30)
                || (dt.Hour == 13 && dt.Minute == 30)
                || (dt.Hour == 21 && dt.Minute == 0))
                return true;
            else
                return false;
        }

        public static bool isStartMin(DateTime dt, string instrument)
        {
            if ((dt.Hour == 10 && dt.Minute == 15)
                || (dt.Hour == 11 && dt.Minute == 30)
                || (dt.Hour == 15 && dt.Minute == 0))
                return false;
            else if ((instrument.StartsWith("rb") && dt.Hour == 23 && dt.Minute >= 0)
                || (instrument.StartsWith("bu") && dt.Hour == 23 && dt.Minute >= 0)
                || (instrument.StartsWith("ru") && dt.Hour == 23 && dt.Minute >= 0)
                || (instrument.StartsWith("bu") && dt.Hour == 23 && dt.Minute >= 0)
                || (instrument.StartsWith("ag") && dt.Hour == 2 && dt.Minute >= 30)
                || (instrument.StartsWith("al") && dt.Hour == 1 && dt.Minute >= 0)
                || (instrument.StartsWith("i") && dt.Hour == 23 && dt.Minute >= 30)
                || (instrument.StartsWith("j") && dt.Hour == 23 && dt.Minute >= 30)
                || (instrument.StartsWith("jm") && dt.Hour == 23 && dt.Minute >= 30))
                return false;
            else if (dt.Minute % _MIN_INTERVAL == 0)
                return true;
            else
                return false;
        }

        //Todo lastMin 加上日期时间，避免涨跌停无数据，需要判断时差超过15分钟也要新bar
        public static bool isNewBar(string lastUpdateTime, DateTime dt, string instrument)
        {
            if (lastUpdateTime == null || lastUpdateTime == "")
                return true;
            DateTime lastUpdateDT = DateTime.Parse(lastUpdateTime);
            if (lastUpdateTime == null || ((lastUpdateDT.Hour != dt.Hour || lastUpdateDT.Minute != dt.Minute) && isOpenMin(dt)))
                return true;


            TimeSpan span = dt - lastUpdateDT;

            if (((lastUpdateDT.Hour != dt.Hour || lastUpdateDT.Minute != dt.Minute) && isStartMin(dt, instrument)) || span.TotalMinutes > _MIN_INTERVAL)
                return true;
            return false;
        }
    }
}
