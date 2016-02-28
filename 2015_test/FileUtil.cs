using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleProxy
{
    class FileUtil
    {
        public const string FILE_PATH_HEAD = "C:\\work\\";
        public const string FILE_PATH_HEAD_TEST = "C:\\work\\Test";

        public const string ConfigName = "Config.txt";

        public static string getConfigFilePath()
        {
            if (Program.isTest == true)
                return FILE_PATH_HEAD_TEST + ConfigName;
            else
                return FILE_PATH_HEAD + ConfigName;
        }
    }
}
