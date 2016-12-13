using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleProxy
{
    class Log
    {
        public static void log(string LogStr)
        {
            StreamWriter sw = null;
            try
            {
                LogStr = Program.LogTitle + "[" + DateTime.Now.ToLocalTime().ToString() + "]" + LogStr + "\n";
                string logFile = FileUtil.getLogFilePath();
                sw = new StreamWriter(logFile, true);
                //if (Program.isTest == true)
                //{
                //    sw = new StreamWriter("C:\\work\\TestLog.txt", true);
                //}
                //else
                //{
                //    sw = new StreamWriter("C:\\work\\Log.txt", true);
                //}
                sw.WriteLine(LogStr);
            }
            catch
            {
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        public static void log(string LogStr,string instrument)
        {
            StreamWriter sw = null;
            try
            {
                LogStr = Program.LogTitle + "[" + DateTime.Now.ToLocalTime().ToString() + "]" + LogStr + "\n";
                string logFile = FileUtil.getLogFilePath(instrument);
                sw = new StreamWriter(logFile, true);
                //if (Program.isTest == true)
                //{
                //    sw = new StreamWriter("C:\\work\\TestLog.txt", true);
                //}
                //else
                //{
                //    sw = new StreamWriter("C:\\work\\Log.txt", true);
                //}
                sw.WriteLine(LogStr);
            }
            catch
            {
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }
        public static void logTrade(string LogStr)
        {
            StreamWriter sw = null;
            try
            {
                LogStr = /*Program.LogTitle + "[" + DateTime.Now.ToLocalTime().ToString() + "]" +*/ LogStr + "\n";

                string logFile = FileUtil.getLogTradeFilePath();
                sw = new StreamWriter(logFile, true);
                //if (Program.isTest == true)
                //{
                //    sw = new StreamWriter("C:\\work\\TestLogTrade.txt", true);
                //}
                //else
                //{
                //    sw = new StreamWriter("C:\\work\\LogTrade.txt", true);
                //}
                sw.WriteLine(LogStr);
            }
            catch
            {
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }
    }
}
