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
        public const string UnitDataName = "15m.json";
        public const string TickDataName = "TickData.txt";

        public const String TestTag = Billow.isTest == true?"Test":"";
    
        private static string buildFilePath(string fileName)
        {
            return System.AppDomain.CurrentDomain.BaseDirectory +"conf\\"+ TestTag+fileName;
        }

        private static string buildDataFilePath(string fileName)
        {
            return System.AppDomain.CurrentDomain.BaseDirectory + "test\\" + fileName;
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

        public static string getLogFilePath(String instrument)
        {
            return buildFilePath("_" + instrument +"_"+LogName);
        }

        public static string getUnitDataPath(string instrument)
        {
            return buildFilePath(instrument + "_" + UnitDataName);
        }

        public static string getTickDataPath(string instrument)
        {
            return buildFilePath("_" + instrument + "_" + TickDataName);
        }

        public static string getLogTradeFilePath()
        {
            return buildFilePath(LogTradeName);
        }
        public static string getTestDataFilePath(string instrument)
        {
            return buildDataFilePath(instrument);
        }
        //public static string getInstrumentFilePath(IStrategy strategy)
        //{
        //    string strategyName = strategy.getStrategyName();
        //    if(Program.isTest)
        //    {
        //        return buildFilePath(strategyName + "_"+ TestTag + InstrumentName);
        //    }
        //    else
        //    {
        //        return buildFilePath(strategyName + "_" + InstrumentName);
        //    }
        //}
    }
}
