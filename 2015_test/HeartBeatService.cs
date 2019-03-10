using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Threading;

namespace ConsoleProxy
{
    class HeartBeatService
    {
        const string REDIS_HOST = "127.0.0.1";
        ConnectionMultiplexer _redis;
        ISubscriber _redisSubscriber;
        bool _useHeartBeatService;
        string _user;
        public HeartBeatService(string user, bool beat)
        {
            _user = user;
            _useHeartBeatService = beat;
            if(beat == true)
            {
                //取连接对象
                _redis = ConnectionMultiplexer.Connect(REDIS_HOST);
                //取得订阅对象
                _redisSubscriber = _redis.GetSubscriber();
            }
            
        }

        public bool isAvailable()
        {
            return _useHeartBeatService && _redisSubscriber != null;
        }

        private long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

        private void startHeartBeatService()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (_useHeartBeatService == false && _redisSubscriber != null)
                    {
                        HeartBeat heartBeat = new HeartBeat();
                        heartBeat.user = _user;
                        heartBeat.timestamp = GetTimeStamp();
                        try
                        {
                            _redisSubscriber.Publish("heartbeat", JsonConvert.SerializeObject(heartBeat));
                            Thread.Sleep(30000);

                        }
                        catch (Exception err)
                        {
                            Log.log(string.Format("heartbeat publish err {0} ", err.Message));
                            return;
                        }

                    }
                }
            });
        }

        public void publishInfo(string tag, string info)
        {
            if (_useHeartBeatService == false && _redisSubscriber != null)
            {
                _redisSubscriber.Publish(tag, info);
            }
        }

        public void startService()
        {
            try
            {
                if(_useHeartBeatService)
                startHeartBeatService();
            }
            catch (Exception e)
            {

            }
        }

    }
}
