using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleProxy
{
    class Config
    {
        public string user;
        public string password;
        public bool isTest = false;
        public bool isDataCollector = false;

        public static Config loadConfig()
        {
            Config config = null;
            try
            {
                Console.WriteLine(FileUtil.getConfigFilePath());
                string text = File.ReadAllText(FileUtil.getConfigFilePath());
                config = JsonConvert.DeserializeObject<Config>(text);
            }
            catch (Exception e)
            {

            }
            return config;
        }
    }
}
