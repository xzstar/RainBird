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
            DataCollector dataCollector = new DataCollector();
            dataCollector.startService();
        }

    }
}
