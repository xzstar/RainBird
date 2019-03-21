using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trade2015;

namespace ConsoleProxy
{
    class InstrumentWatcher
    {
        static Trade sTrade;
        static System.Threading.Timer timer;
        static Dictionary<string, DateTime> lastUpdateTime = new Dictionary<string, DateTime>();
        public static bool flag = true;
        static Object mylock = new Object();

        public static string getInstumentName(string key)
        {
            char[] charArray = key.ToCharArray();
            for (int i = 0; i < key.Length; i++)
            {
                char ch = charArray[i];
                if (ch >= '0' && ch <= '9')
                    return key.Substring(0, i);
            }
            return key;
        }

        static void Excute(object obj)
        {
            Thread.CurrentThread.IsBackground = true;

            lock (mylock)
            {
                if (!flag)
                {
                    return;
                }


                foreach (string key in lastUpdateTime.Keys)
                {
                    DateTime last = lastUpdateTime[key];
                    TimeSpan ts = DateTime.Now - last;
                    string sub = getInstumentName(key);

                    if (sTrade.DicExcStatus.ContainsKey(sub) == true && sTrade.DicExcStatus[sub] == ExchangeStatusType.Trading)
                    {
                        if (ts.TotalSeconds > 180)
                        {
                            Console.WriteLine("InstrumentWatcher Excute:{0} 最近一次获得数据时间为{1}，距离现在{2}秒", key, last.ToString(), ts.TotalSeconds);
                            Log.log(string.Format("InstrumentWatcher Excute:{0} 最近一次获得数据时间为{1}，距离现在{2}秒", key, last.ToString(), ts.TotalSeconds));
                        }
                        else
                        {
                            Log.log(string.Format("InstrumentWatcher Excute:{0} 最近一次获得数据时间为{1}，距离现在{2}秒", key, last.ToString(), ts.TotalSeconds));
                        }
                    }

                }

            }

        }

        public static void updateTime(string instrument, DateTime dt)
        {
            lock (mylock)
            {
                lastUpdateTime[instrument] = dt;
            }
        }

        public static void Init(Trade trade)
        {
            sTrade = trade;
            timer = new System.Threading.Timer(Excute, null, 0, 60000);
        }
    }



    class LoginWatcher
    {
        static Program _p;
        static System.Threading.Timer timer;
        static System.Threading.Timer oneMinTimer;
        static Object lockObj = new Object();
        static Object lockOneMinObj = new Object();
        static long lastTime = 0;
        static void Excute(object obj)
        {
            Thread.CurrentThread.IsBackground = true;
            lock (lockObj)
            {
                long curr = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
                //至少要间隔2分钟
                if (curr - lastTime > 120)
                    _p.checkStatus();
                else
                {
                    string info = String.Format("cur {0}, lastTime {1}", curr, lastTime);
                    Log.log(DataCollector.LogTitle + info);
                }
                lastTime = curr;
            }
        }
        static void ExcuteOneMin(object obj)
        {
            Thread.CurrentThread.IsBackground = true;
            lock (lockOneMinObj)
            {
                long curr = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
                //至少要间隔45s
                if (curr - lastTime > 45)
                    _p.checkStatusOneMin();
                else
                {
                    string info = String.Format("cur {0}, lastTime {1}", curr, lastTime);
                    Log.log(DataCollector.LogTitle + info);
                }
                lastTime = curr;
            }
            
        }

        public static void Init(Program program)
        {
            _p = program;
            timer = new System.Threading.Timer(Excute, null, 60 * 1000, 10 * 60 * 1000);
            oneMinTimer = new System.Threading.Timer(ExcuteOneMin, null, 60 * 1000, 1 * 60 * 1000);
        }
    }
}
