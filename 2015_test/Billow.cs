using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleProxy
{
    class Billow
    {
        public const bool isTest = false;

        private static void Main(string[] args)
        {
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
                    dataCollector.startService();
                }
                else
                {
                    Program program = new Program(config);
                    program.startService();
                }
            }
                
        }

    }
}
