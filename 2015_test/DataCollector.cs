using Newtonsoft.Json;
using Quote2015;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleProxy
{
    class DataCollectorWatcher
    {
        static DataCollector _p;
        static System.Threading.Timer timer;
        static Object lockObj = new object();
        static long lastTime = 0;
        static void Excute(object obj)
        {
            Thread.CurrentThread.IsBackground = true;
            lock (lockObj)
            {
                long curr = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
                string info = String.Format("cur {0}, lastTime {1}", curr, lastTime);
                Console.WriteLine(info);
                //至少要间隔2分钟
                if (curr - lastTime > 120)
                {
                    _p.checkStatus();
                    lastTime = curr;
                }
                else
                {
                    
                    Log.log(info);
                }
                
            }
        }
        
        public static void Init(DataCollector dc)
        {
            _p = dc;
            timer = new System.Threading.Timer(Excute, null, 60 * 1000, 10 * 60 * 1000);
            //timer = new System.Threading.Timer(Excute, null, 10 * 1000, 10 * 1000);

        }
    }
    class DataCollector
    {
        static private string _inst;
        private static double _LastPrice = double.NaN;

        Config config;
        Quote quoter;
        HeartBeatService heartBeatService;
        DataService dataService;

        static Object lockFile = new Object();
        //public const bool isTest = false;
        public const bool withoutDB = false;
        //public const bool withoutRedis = true;
        //public static string LogTitle;//= isTest ? "[测试_数据]" : "[正式_数据]";

        private Dictionary<string, InstrumentData> tradeData = new Dictionary<string, InstrumentData>();
        private Dictionary<string, List<UnitData>> unitDataMap = new Dictionary<string, List<UnitData>>();
        private Dictionary<string, string> lastUpdateTimeMap = new Dictionary<string, string>();

        private bool isInit = false;
        
        public DataCollector(Config config)
        {
            this.config = config;
            Log.LogTitle = config.isTest ? "[测试_数据]" : "[正式_数据]";
        }
        public void initQuoter()
        {
            if (config.isTest)
            {
                quoter = new Quote("ctp_quote_proxy.dll")
                {
                    Server = "tcp://180.168.146.187:10010",
                    Broker = "9999",
                };

            }
            else
            {
                quoter = new Quote("ctp_quote_proxy.dll")
                {
                    Server = "tcp://180.166.37.129:41213",//国信
                    Broker = "8030",
                    //Server = "tcp://222.73.111.150:41213",//"tcp://101.95.8.178:51213",//中建 
                    //Broker = "9080",
                };
            }
        }
        
        void subscribeInstruments()
        {
            foreach (string instrument in tradeData.Keys)
            {
                InstrumentData data = tradeData[instrument];

                Console.WriteLine(Log.LogTitle + "品种:{0} 交易:{1} 开仓位:{2} 平仓位:{3} 方向:{4}",
                    instrument, data.trade ? "YES" : "NO", data.openvolumn, data.closevolumn, data.holder == 0 ? "无" : (data.holder == 1 ? "买" : "卖"));
                quoter.ReqSubscribeMarketData(instrument);
            }
        }

        public void checkStatus()
        {
            string info = String.Format("checkStatus -- isLogoutTimeNow:{0} isLogInTimeNow:{1}  IsLogin:{2}", Utils.isLogoutTimeNow(),
                Utils.isLogInTimeNow(), quoter.IsLogin);
            Log.log(info);
            Console.WriteLine(Log.LogTitle+info);

            if (Utils.isLogoutTimeNow() && quoter.IsLogin)
            {
                unSubscribeInstruments();
                Console.WriteLine(Log.LogTitle + "isLogoutTimeNow");
                quoter.ReqUserLogout();

                if (Utils.isOverDayNow())
                {
                    Console.WriteLine(Log.LogTitle + "isOverDayNow");
                    
                }

                Thread.Sleep(3000);
                Console.WriteLine(Log.LogTitle + "trade logout");
                Log.log("trade logout");

            }
            if (Utils.isLogInTimeNow() && !quoter.IsLogin)
            {
                Console.WriteLine(Log.LogTitle + "isLogInTimeNow");
                int errorCount = 0;
                while (!quoter.IsLogin && errorCount < 5)
                {
                    Console.WriteLine(Log.LogTitle + "trade ReqConnect");

                    quoter.ReqConnect();
                    Thread.Sleep(30000);
                    errorCount++;


                    if (!quoter.IsLogin)
                    {
                        Console.WriteLine(Log.LogTitle + "trade login failed");
                        Log.log("trade login failed");
                        //HttpHelper.HttpPostToWechat(trader.Investor + " trade login failed");
                    }
                    else
                    {
                        syncData();
                        //subscribeInstruments();
                        Console.WriteLine(Log.LogTitle + "trade login");
                        Log.log("trade login");
                        //HttpHelper.HttpPostToWechat(trader.Investor + " trade login");
                        break;
                    }
                }

            }

        }

        private void unSubscribeInstruments()
        {
            foreach (string instrument in tradeData.Keys)
            {
                InstrumentData data = tradeData[instrument];

                quoter.ReqUnSubscribeMarketData(instrument);
            }
        }

        private void syncData()
        {
            lock (lockFile)
            {
                string fileName = FileUtil.getTradeFilePath();
                Dictionary<string, InstrumentData> tempData = null;
                try
                {
                    string text = File.ReadAllText(fileName);
                    tempData = JsonConvert.DeserializeObject<Dictionary<string, InstrumentData>>(text);
                }
                catch (Exception e)
                {

                }
                if (quoter.IsLogin)
                    unSubscribeInstruments();

                if (tempData != null && tempData.Count != 0)
                    tradeData = tempData;
                else if (isInit == false) //第一次启动
                {
                    string inst = string.Empty;
                    Console.WriteLine(Log.LogTitle + "请输入合约:");
                    inst = Console.ReadLine();
                    //program.quoter.ReqSubscribeMarketData(inst);
                    InstrumentData instrumentData = new InstrumentData();
                    instrumentData.holder = 0;
                    instrumentData.isToday = false;
                    instrumentData.lastUpdateTime = "";
                    instrumentData.price = 0;
                    instrumentData.span = 0.02;
                    instrumentData.trade = true;
                    instrumentData.openvolumn = 1;
                    instrumentData.closevolumn = 1;
                    instrumentData.curAvg = 0;
                    tradeData = new Dictionary<string, InstrumentData>();
                    tradeData.Add(inst, instrumentData);

                }
                unitDataMap.Clear();
                dataService = new DataService(!withoutDB);
                dataService.initUnitDataMap(unitDataMap, tradeData.Keys);

                if (quoter.IsLogin)
                    subscribeInstruments();
            }
        }

        public void init()
        {
            initQuoter();
            Config config = Config.loadConfig();
            if (config == null)
            {
                Console.WriteLine("请输入帐号:");
                quoter.Investor = Console.ReadLine();
                Console.WriteLine("请输入密码:");
                quoter.Password = Console.ReadLine();
            }
            else
            {
                quoter.Investor = config.user;
                quoter.Password = config.password;
            }
            initDelegate();
        }

        public void initDelegate()
        {
            quoter.OnFrontConnected += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnFrontConnected");
                Log.log("OnFrontConnected");
                MailService.Notify(Log.LogTitle + " [启动]", quoter.Investor + " 前置主机连接成功");

                //HttpHelper.HttpPostToWechat(program.trader.Investor + " OnFrontConnected");
                if (Utils.isTradingTimeNow() || Utils.isLogInTimeNow())
                    quoter.ReqUserLogin();
            };
            quoter.OnRspUserLogin += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogin:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogin:{0}", e.Value));
                MailService.Notify(Log.LogTitle + " [启动]", quoter.Investor + " 登录成功");

            };

            quoter.OnRspUserLogout += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogout:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogout:{0}", e.Value));
                MailService.Notify(Log.LogTitle + " [退出]", quoter.Investor + " 退出登录");

            };

            quoter.OnRtnError += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
                Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
                MailService.Notify(Log.LogTitle + " [错误]", quoter.Investor + " "+e.ErrorMsg);
            };

            quoter.OnRtnTick += (sender, e) =>
            {
                List<UnitData> unitDataList;
                if (unitDataMap.TryGetValue(e.Tick.InstrumentID, out unitDataList) == false)
                    return;


                //DateTime d1 = DateTime.Parse(program.quoter.TradingDay+" "+e.Tick.UpdateTime);
                DateTime d1 = DateTime.ParseExact(quoter.TradingDay + " " + e.Tick.UpdateTime, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
                if (Utils.isValidData(e.Tick.InstrumentID, d1, e.Tick.UpdateTime) == false)
                {
                    return;
                }

                if (Utils.isTradingTime(e.Tick.InstrumentID, d1) == false)
                    return;


                string lastUpdateTime;
                if (lastUpdateTimeMap.TryGetValue(e.Tick.InstrumentID, out lastUpdateTime) == false)
                {
                    lastUpdateTime = null;
                }

                if (lastUpdateTime != null && Utils.isNewBar(lastUpdateTime, d1, e.Tick.InstrumentID))
                {
                    int count = unitDataList.Count;
                    if (count > 0)
                    {
                        UnitData lastUnitData = unitDataList.Last();

                        if (count > DataService.TOTALSIZE)
                        {
                            double total = 0;
                            for (int i = 0; i < DataService.TOTALSIZE; i++)
                            {
                                total += unitDataList.ElementAt(count - i - 1).close;
                            }
                            lastUnitData.avg_480 = Math.Round(total / DataService.TOTALSIZE, 2);
                        }
                        dataService.update(e.Tick.InstrumentID, lastUnitData);
                    }
                    UnitData unitData = new UnitData();
                    unitData.high = unitData.low = unitData.open = unitData.close = e.Tick.LastPrice;
                    unitData.datetime = d1.ToString();
                    unitDataList.Add(unitData);

                    string info = string.Format(Log.LogTitle + "new bar 品种{0} 时间:{1} 当前价格:{2}", e.Tick.InstrumentID,
                       e.Tick.UpdateTime, e.Tick.LastPrice);
                    Console.WriteLine(info);
                    MailService.Notify(Log.LogTitle + " [info]",info);
                    dataService.save(e.Tick.InstrumentID, unitData);

                }
                else if (unitDataList.Count > 0)
                {
                    UnitData unitData = unitDataList.Last();

                    if (e.Tick.LastPrice > unitData.high)
                    {
                        unitData.high = e.Tick.LastPrice;
                    }

                    if (e.Tick.LastPrice < unitData.low)
                    {
                        unitData.low = e.Tick.LastPrice;
                    }
                    unitData.close = e.Tick.LastPrice;

                }
                lastUpdateTimeMap[e.Tick.InstrumentID] = d1.ToString();

            };
        }

        private void saveAll()
        {
            foreach (string key in unitDataMap.Keys)
            {
                List<UnitData> unitDataList;
                if (unitDataMap.TryGetValue(key, out unitDataList))
                {
                    if (unitDataList == null)
                        continue;
                    Log.log(string.Format("Quit:saving {0} ", key));

                    if (unitDataList.Count > 0)
                    {
                        UnitData lastUnitData = unitDataList.Last();
                        dataService.update(key, lastUnitData);
                    }

                }
            }
        }

        public void startService()
        {
            Console.WriteLine(Log.LogTitle + "CTP接口:\t启动中.....");
            init();
            quoter.ReqConnect();
            Thread.Sleep(3000);

            if (!quoter.IsLogin && (Utils.isLogInTimeNow() || Utils.isTradingTimeNow()))
            {
                Console.WriteLine("login 失败，交易时段重试");
                //goto R;
            }

            DataCollectorWatcher.Init(this);

            syncData();
            isInit = true;
            heartBeatService = new HeartBeatService(quoter.Investor, false);
            heartBeatService.startService();
            MailService.Notify(Log.LogTitle + " [启动]", quoter.Investor + " 启动");

        Inst:

            Console.WriteLine(Log.LogTitle + "q:退出 s:立刻保存");
            char c = Console.ReadKey().KeyChar;
            switch (c)
            {
               
                case 's':
                    syncData();
                    break;
                
                case 'q':
                    if (quoter.IsLogin)
                    {
                        quoter.ReqUserLogout();
                    }

                    InstrumentWatcher.flag = false;
                    Thread.Sleep(5000); //待接口处理后续操作
                    Environment.Exit(0);
                    break;
            }
            
            goto Inst;
 
        }
    }
}
