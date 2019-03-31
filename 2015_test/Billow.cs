using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleProxy
{
    class Billow
    {
        public const bool isTest = false;
        public static string AssemblySHA1
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "Build SHA1:" + "";
                }
                return "Build SHA1:" + ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public static string AssemblyBranch
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
                if (attributes.Length == 0)
                {
                    return "Build Branch:" + "";
                }
                return "Build Branch:" + ((AssemblyConfigurationAttribute)attributes[0]).Configuration;
            }
        }

        public static string AssemblyBuildDate
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTrademarkAttribute), false);
                if (attributes.Length == 0)
                {
                    return "Build Date:" + "";
                }
                return "Build Date:" + ((AssemblyTrademarkAttribute)attributes[0]).Trademark;
            }
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("##########");
            Console.WriteLine(AssemblyBranch);
            Console.WriteLine(AssemblySHA1);
            Console.WriteLine(AssemblyBuildDate);
            Console.WriteLine("##########");

            HttpHelper.isHoliday();
            Config config = Config.loadConfig();
            if (config == null)
            {
                Console.WriteLine("配置文件不存在");
                Console.ReadKey();
            }
            else
            {
                if (config.totalSize > 0)
                    DataService.TOTALSIZE = config.totalSize;
                
                Console.WriteLine("### Total Size is {0} ###", DataService.TOTALSIZE);

                if (config.isDataCollector)
                {
                    Console.WriteLine("DataCollector");
                    DataCollector dataCollector = new DataCollector(config);
                    //Log.LogTitle = DataCollector.LogTitle;
                    dataCollector.startService();
                }
                else
                {
                    Program program = new Program(config);
                    //Log.LogTitle = Program.LogTitle;
                    program.startService();
                }
            }
                
        }

    }
}
