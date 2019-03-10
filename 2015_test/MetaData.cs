using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ConsoleProxy
{
    [Serializable]
    public class UnitData
    {
        public ObjectId Id { get; set; }
        public string datetime { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double open { get; set; }
        public double close { get; set; }
        public double avg_480 { get; set; }
    }


    [Serializable]
    public class InstrumentData
    {
        public string lastUpdateTime = null;
        public int holder = 0;
        public bool isToday = true;
        public double price = -1;
        public double curAvg = 0;
        public bool trade;
        public int closevolumn;
        public int openvolumn;
        public double span = 0.02;
        public double stoploss = 0;
        public double targetpos = 0;

    }

    class Deal
    {
        public string user;
        public string instrument;
        public string direction;
        public string holders;
        public string price;
        public string time;

    }

    class TimeBarInfo
    {
        public string user;
        public string instrument;
        public string price;
        public string time;
    }

    class HeartBeat
    {
        public string user;
        public long timestamp;
    }
}
