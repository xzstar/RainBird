using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ConsoleProxy
{
    class DataService
    {
        public static int TOTALSIZE = 60;

        /// <summary>
        /// 数据库连接
        /// </summary>
        private const string connectionString = "mongodb://127.0.0.1:27017";
        /// <summary>
        /// 指定的数据库
        /// </summary>
        private const string dbName = "data_15min";

        static String instrument_15m = "_15m";

        private MongoDatabase _mongoDB;

        bool _isAvailable;

        public DataService(bool init)
        {
            _isAvailable = init;
            if(_isAvailable)
                _mongoDB = MongoDbHepler.GetDatabase(connectionString, dbName);
        }

        public bool isAvailable()
        {
            return _isAvailable;
        }

        public void initUnitDataMap(Dictionary<string, List<UnitData>> unitDataMap, Dictionary<string, InstrumentData>.KeyCollection keys)
        {
            if (_isAvailable)
            {
                foreach (string key in keys)
                {
                    addUnitDataMap(unitDataMap, key);
                }
            }
        }
        private void addUnitDataMap(Dictionary<string, List<UnitData>> unitDataMap,string instrument)
        {
            if (_isAvailable)
            {
                List<UnitData> unitDataList = MongoDbHepler.GetAll<UnitData>(_mongoDB, instrument + instrument_15m);
                unitDataMap.Add(instrument, unitDataList);
                int count = unitDataList.Count;
                if (count > TOTALSIZE)
                {
                    UnitData lastUnitData = unitDataList.Last();
                    if (lastUnitData.avg_480 <= 0)
                    {
                        if (count > TOTALSIZE)
                        {
                            double total = 0;
                            for (int i = 0; i < TOTALSIZE; i++)
                            {
                                total += unitDataList.ElementAt(count - i - 1).close;
                            }
                            lastUnitData.avg_480 = Math.Round(total / TOTALSIZE, 2);
                        }
                    }
                    Console.WriteLine(string.Format(Program.LogTitle + "品种{0} 个数{1} 平均:{2}", instrument, count, lastUnitData.avg_480));
                }
            }
        }

        public void update(string instrument, UnitData data)
        {
            if (_isAvailable)
            {
                IMongoQuery query = Query.EQ("datetime", data.datetime);
                Dictionary<string, BsonValue> dict = new Dictionary<string, BsonValue>();
                dict.Add("open", data.open);
                dict.Add("close", data.close);
                dict.Add("high", data.high);
                dict.Add("low", data.low);
                dict.Add("avg_480", data.avg_480);
                MongoDbHepler.Update(_mongoDB, instrument + instrument_15m, query, dict);
            }

        }

        public void save(string instrument, UnitData data)
        {
            if (_isAvailable)
            {
                MongoDbHepler.Insert<UnitData>(_mongoDB, instrument + instrument_15m, data);
            }
        }
    }
}
