using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleProxy
{
    class FileUtil
    {
        //public const string FILE_PATH_HEAD = "C:\\work\\";
        //public const string FILE_PATH_HEAD_TEST = "C:\\work\\Test";

        public const string ConfigName = "Config.txt";
        public const string TradeName = "Trade.dat";
        public const string InstrumentName = "Instrument.dat";
        public const string LogName = "Log.txt";
        public const string LogTradeName = "LogTrade.txt";
        public const String TestTag = Program.isTest == true?"Test":"";
    
        private static string buildFilePath(string fileName)
        {
            return System.AppDomain.CurrentDomain.BaseDirectory +"conf\\"+ TestTag+fileName;
        }
            
        public static string getConfigFilePath()
        {
            return buildFilePath(ConfigName);
        }

        public static string getTradeFilePath()
        {
            return buildFilePath(TradeName);
        }
        public static string getInstrumentFilePath()
        {
            return buildFilePath(InstrumentName);
        }

        public static string getLogFilePath()
        {
            return buildFilePath(LogName);
        }
        public static string getLogTradeFilePath()
        {
            return buildFilePath(LogTradeName);
        }
        public static string getInstrumentFilePath(IStrategy strategy)
        {
            string strategyName = strategy.getStrategyName();
            if(Program.isTest)
            {
                return buildFilePath(strategyName + "_"+ TestTag + InstrumentName);
            }
            else
            {
                return buildFilePath(strategyName + "_" + InstrumentName);
            }
        }
    }
}
