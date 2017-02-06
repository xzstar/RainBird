using System;
using System.Linq;
using System.Threading;
using Quote2015;
using Trade2015;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;

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

    }

    public class InstrumentTradeConfig
    {
        public string instrument;
        public bool trade;
        public int closevolumn;
        public int openvolumn;
        public int span;
    }


    class Program
    {
        static private string _inst;
        private static int _TOTALSIZE = 480 ;
        private static int _MIN_INTERVAL = 15;
        private static double _LastPrice = double.NaN;

        private static int _orderId;
        private static int BUY_OPEN = 1;
        private static int SELL_CLOSETODAY = 2;
        private static int SELL_CLOSE = 21;
        private static int SELL_OPEN = 3;
        private static int BUY_CLOSETODAY = 4;
        private static int BUY_CLOSE = 41;

        Trade trader;
        Quote quoter;
        TradeCenter tradeCenter;
        static Object lockFile = new Object();
        private static ConcurrentQueue<TradeItem> _tradeQueue = new ConcurrentQueue<TradeItem>();
        public const bool isTest = false;
        public static string LogTitle = isTest?"[测试]":"[正式]";

        private List<InstrumentTradeConfig> _instrumentList = new List<InstrumentTradeConfig>();
        private Dictionary<string, InstrumentTradeConfig> _instrumentMap = new Dictionary<string, InstrumentTradeConfig>();
        private Dictionary<string, InstrumentData> tradeData = new Dictionary<string, InstrumentData>();
        private Dictionary<string, HashSet<string>> _waitingForOp = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, List<UnitData>> unitDataMap = new Dictionary<string, List<UnitData>>();

        /// <summary>
        /// 数据库连接
        /// </summary>
        private const string connectionString = "mongodb://127.0.0.1:27017";
        /// <summary>
        /// 指定的数据库
        /// </summary>
        private const string dbName = "data_15min";
        
        static String instrument_15m = "_15m";

        private MongoDatabase mongoDB;

        private bool isInit = false;

        private static void operatord(Trade t, Quote q, int op, string inst)
        {
            //Console.WriteLine("操作start:{0}: {1}", op, inst);
            Log.log(string.Format(Program.LogTitle+"操作开始:{0}: {1}", op, inst));
            string operation = string.Empty;
 

            DirectionType dire = DirectionType.Buy;
            OffsetType offset = OffsetType.Open;
            switch (op)
            {
                case 1:
                    dire = DirectionType.Buy;
                    offset = OffsetType.Open;
                    operation = "买开";
                    break;
                case 2:
                    dire = DirectionType.Sell;
                    offset = OffsetType.CloseToday;
                    operation = "买平";
                    break;
                case 21:
                    dire = DirectionType.Sell;
                    offset = OffsetType.Close;
                    operation = "买平昨";
                    break;
                case 3:
                    dire = DirectionType.Sell;
                    offset = OffsetType.Open;
                    operation = "卖开";
                    break;
                case 4:
                    dire = DirectionType.Buy;
                    offset = OffsetType.CloseToday;
                    operation = "卖平";
                    break;
                case 41:
                    dire = DirectionType.Buy;
                    offset = OffsetType.Close;
                    operation = "卖平昨";
                    break;    
            }
            //if (op >= 1 && op <= 4)
            {
                Console.WriteLine(Program.LogTitle + "操作:{0}: {1}", operation, inst);
                Log.log(string.Format(Program.LogTitle + "操作:{0}: {1}", operation, inst));
                //Console.WriteLine("请选择委托类型: 1-限价  2-市价  3-FAK  4-FOK");
                OrderType ot = OrderType.Limit;
                /*switch (Console.ReadKey().KeyChar)
                {
                    case '2':
                        ot = OrderType.Market;
                        break;
                    case '3':
                        ot = OrderType.FAK;
                        break;
                    case '4':
                        ot = OrderType.FOK;
                        break;
                }*/
                MarketData tick;
                if (q.DicTick.TryGetValue(inst, out tick))
                {
                    double price = dire == DirectionType.Buy ? tick.AskPrice : tick.BidPrice;
                    _orderId = -1;
                    Console.WriteLine(t.ReqOrderInsert(inst, dire, offset, price, 1, pType: ot));
                    for (int i = 0; i < 3; ++i)
                    {
                        Thread.Sleep(200);
                        if (-1 != _orderId)
                        {
                            Console.WriteLine(Program.LogTitle + "委托标识:" + _orderId);
                            break;
                        }
                    }
                }
            }
        }

        private static void operatorInstrument(int op, string inst,double price)
        {
            operatorInstrument(op, inst, price, 1);
        }
        private static void operatorInstrument(int op, string inst, double price,int volumn)
        {
            if (volumn == 0)
                volumn = 1;
            TradeItem tradeItem = new TradeItem(inst, op, price,volumn);

            lock (_tradeQueue)
            {
                _tradeQueue.Enqueue(tradeItem);//生成一个资源
                Monitor.Pulse(_tradeQueue);//通知在Wait中阻塞的Consumer线程即将执行
            }
        }
        void subscribeInstruments()
        {
            foreach (InstrumentTradeConfig inst in _instrumentList)
            {
                Console.WriteLine(Program.LogTitle + "品种:{0} 交易:{1} 开仓位:{2} 平仓位:{3}",inst.instrument,inst.trade?"YES":"NO",inst.openvolumn,inst.closevolumn);

                string instrument = inst.instrument;
                quoter.ReqSubscribeMarketData(instrument);
            }
        }

        private void closeAllPosition()
        {
            foreach(PositionField posField in trader.DicPositionField.Values)
            {
                Console.WriteLine("\r\n"+Program.LogTitle + "品种:{0} 仓位:昨{1} 今{2}  方向{3}",
                    posField.InstrumentID, posField.YdPosition,posField.TdPosition, posField.Direction);

                if(trader.DicExcStatus.ContainsKey(InstrumentWatcher.getInstumentName(posField.InstrumentID)))
                {
                    if(trader.DicExcStatus[InstrumentWatcher.getInstumentName(posField.InstrumentID)] != ExchangeStatusType.Trading)
                    {
                        Console.WriteLine("\r\n" + Program.LogTitle + "品种:{0} 不在交易时段",posField.InstrumentID);
                        continue;
                    }
                }
                else
                {
                    Console.WriteLine("\r\n" + Program.LogTitle + "品种:{0} 不在交易列表", posField.InstrumentID);
                    continue;
                }

                if(posField.Direction == DirectionType.Buy)
                { 
                    if(posField.YdPosition>0)
                    {
                        operatorInstrument(TradeCenter.SELL_CLOSE, posField.InstrumentID, 0, posField.YdPosition);
                    }

                    if(posField.TdPosition > 0)
                    {
                        operatorInstrument(TradeCenter.SELL_CLOSETODAY, posField.InstrumentID,0, posField.TdPosition);
                    }
                }
                else
                {
                    if (posField.YdPosition > 0)
                    {
                        operatorInstrument(TradeCenter.BUY_CLOSE, posField.InstrumentID, 0, posField.YdPosition);
                    }

                    if (posField.TdPosition > 0)
                    {
                        operatorInstrument(TradeCenter.BUY_CLOSETODAY, posField.InstrumentID, 0, posField.TdPosition);
                    }
                }

            }
        }
        public void checkStatusOneMin()
        {
            if(Utils.isSyncPositionTime())
            {
                Console.WriteLine(Program.LogTitle + "更新持仓");
                Console.WriteLine(trader.DicPositionField.Aggregate("\r\n持仓", (cur, n) => cur + "\r\n"
                       + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                trader.ReqQryPosition();

                foreach(InstrumentData data in tradeData.Values)
                {
                    data.isToday = false;
                }
            }
        }
        
        public void checkStatus()
        {
            Console.WriteLine(Program.LogTitle + "checkStatus");
            if (Utils.isLogoutTimeNow() && trader.IsLogin)
            {
                Console.WriteLine(Program.LogTitle + "isLogoutTimeNow");
                quoter.ReqUserLogout();
                trader.ReqUserLogout();

                if(Utils.isOverDayNow())
                {
                    Console.WriteLine(Program.LogTitle + "isOverDayNow");
                    this.tradeCenter._removingOrders.Clear();
                    this.tradeCenter._tradeOrders.Clear();
                }

                Thread.Sleep(3000);
                Console.WriteLine(Program.LogTitle + "trade logout");
                Log.log(Program.LogTitle + "trade logout");

                //foreach (string key in unitDataMap.Keys)
                //{
                //    LinkedList<UnitData> unitDataList;
                //    if (unitDataMap.TryGetValue(key, out unitDataList))
                //    {
                //        if (unitDataList == null)
                //            continue;
                //        Log.log(string.Format("Quit:saving {0} ", key));

                //        Task.Factory.StartNew(() =>
                //        {
                //            lock(lockFile)
                //            {
                //                lock(lockFile)
                //                {
                //                    string fileNameSerialize = FileUtil.getUnitDataPath(key);
                //                    string jsonString = JsonConvert.SerializeObject(unitDataList);
                //                    File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
                //                }
                //            }
                //        });
                //    }
                //}

            }
            else if(Utils.isLogInTimeNow() && !trader.IsLogin)
            {
                Console.WriteLine(Program.LogTitle + "isLogInTimeNow");
                int errorCount = 0;
                while (!trader.IsLogin && errorCount < 100)
                {
                    trader.ReqConnect();
                    Thread.Sleep(3000);
                    errorCount++;
                }

                if(!trader.IsLogin)
                {
                    Console.WriteLine(Program.LogTitle + "trade login failed");
                    Log.log(Program.LogTitle + "trade login failed");
                }
                else
                {
                    subscribeInstruments();
                    Console.WriteLine(Program.LogTitle + "trade login");
                    Log.log(Program.LogTitle + "trade login");
                    //trader.ReqQryPosition();
                }

            }
            
        }


        private bool isOpenMin(DateTime dt)
        {
            if ((dt.Hour == 9 && dt.Minute == 0)
                || (dt.Hour == 10 && dt.Minute == 30)
                || (dt.Hour == 13 && dt.Minute == 30)
                || (dt.Hour == 21 && dt.Minute == 0))
                return true;
            else
                return false;
        }

        private bool isStartMin(DateTime dt, string instrument)
        {
            if ((dt.Hour == 10 && dt.Minute == 15)
                || (dt.Hour == 11 && dt.Minute == 30)
                || (dt.Hour == 15 && dt.Minute == 0))
                return false;
            else if ((instrument.StartsWith("rb") && dt.Hour == 23 && dt.Minute >= 0)
                || (instrument.StartsWith("bu") && dt.Hour == 23 && dt.Minute >= 0)
                || (instrument.StartsWith("ag") && dt.Hour == 2 && dt.Minute >= 30)
                || (instrument.StartsWith("al") && dt.Hour == 1 && dt.Minute >= 0))
                return false;
            else if (dt.Minute % _MIN_INTERVAL == 0)
                return true;
            else
                return false;
        }

        //Todo lastMin 加上日期时间，避免涨跌停无数据，需要判断时差超过15分钟也要新bar
        private bool isNewBar(string lastUpdateTime, DateTime dt, string instrument)
        {
            if (lastUpdateTime == null || lastUpdateTime == "")
                return true;
            DateTime lastUpdateDT = DateTime.Parse(lastUpdateTime);
            if (lastUpdateTime == null || ((lastUpdateDT.Hour != dt.Hour || lastUpdateDT.Minute != dt.Minute) && isOpenMin(dt)))
                return true;


            TimeSpan span = dt - lastUpdateDT;

            if (((lastUpdateDT.Hour != dt.Hour || lastUpdateDT.Minute != dt.Minute) && isStartMin(dt, instrument)) || span.TotalMinutes > _MIN_INTERVAL)
                return true;
            return false;
        }

        private void initUnitDataMap(string instrument)
        {
            List<UnitData> unitDataList = MongoDbHepler.GetAll<UnitData>(mongoDB, instrument + instrument_15m);
            unitDataMap.Add(instrument, unitDataList);
            int count = unitDataList.Count;
            if (count > _TOTALSIZE)
            {
                UnitData lastUnitData = unitDataList.Last();
                if (lastUnitData.avg_480 <= 0)
                {
                    if (count > _TOTALSIZE)
                    {
                        double total = 0;
                        for (int i = 0; i < _TOTALSIZE; i++)
                        {
                            total += unitDataList.ElementAt(count - i -1).close;
                        }
                        lastUnitData.avg_480 = Math.Round(total / _TOTALSIZE, 2);
                    }
                }
                Console.WriteLine(string.Format(Program.LogTitle + "品种{0} 个数{1} 平均:{2}", instrument, count, lastUnitData.avg_480));
            }
        }

        //输入：q1ctp /t1ctp /q2xspeed /t2speed
        private static void Main(string[] args)
        {
            Program program = new Program();
            System.Object lockThis = new System.Object();
            //bool isInit = true;
           
        R:
            Console.WriteLine(Program.LogTitle + "选择接口:\t1-CTP  2-xSpeed  3-Femas  4-股指仿真  5-外汇仿真  6-郑商商品期权仿真");
            char c = '1';

            switch (c)
            {
                case '1': //CTP
                    if (isTest)
                    {
                        program.trader = new Trade("ctp_trade_proxy.dll")
                        {
                            Server = "tcp://180.168.146.187:10000", 
                            Broker = "9999"// "4040",
                        };
                        program.quoter = new Quote("ctp_quote_proxy.dll")
                        {
                            Server = "tcp://180.168.146.187:10010",
                            Broker = "9999",
                        };

                    }
                    else {
                        program.trader = new Trade("ctp_trade_proxy.dll")
                        {
                            Server = "tcp://180.166.37.129:41205", //国信
                            Broker = "8030"

                            //Server = "tcp://222.73.111.150:41205",//" tcp://101.95.8.178:51205",//中建 
                            //Broker = "9080"// "9999"// "4040",
                        };
                        program.quoter = new Quote("ctp_quote_proxy.dll")
                        {
                            Server = "tcp://180.166.37.129:41213",//国信
                            Broker = "8030",
                            //Server = "tcp://222.73.111.150:41213",//"tcp://101.95.8.178:51213",//中建 
                            //Broker = "9080",
                        };
                    }
                    break;
                case '2': //xSpeed
                    program.trader = new Trade("xSpeed_trade_proxy.dll")
                    {
                        Server = "tcp://203.187.171.250:10910",
                        Broker = "0001",
                    };
                    program.quoter = new Quote("xSpeed_quote_proxy.dll")
                    {
                        Server = "tcp://203.187.171.250:10915",
                        Broker = "0001",
                    };
                    break;
                case '3': //femas
                    program.trader = new Trade("femas_trade_proxy.dll")
                    {
                        Server = "tcp://116.228.53.149:6666",
                        Broker = "0001",
                    };
                    program.quoter = new Quote("femas_quote_proxy.dll")
                    {
                        Server = "tcp://116.228.53.149:6888",
                        Broker = "0001",
                    };
                    break;
                case '4': //CTP
                    program.trader = new Trade("ctp_trade_proxy.dll")
                    {
                        Server = "tcp://124.207.185.88:41205",
                        Broker = "1010",
                    };
                    program.quoter = new Quote("ctp_quote_proxy.dll")
                    {
                        Server = "tcp://124.207.185.88:41213",
                        Broker = "1010",
                    };
                    break;
                case '5': //CTP
                    program.trader = new Trade("femas_trade_proxy.dll")
                    {
                        Server = "tcp://117.184.207.111:7036",
                        Broker = "2713",
                    };
                    program.quoter = new Quote("femas_quote_proxy.dll")
                    {
                        Server = "tcp://117.184.207.111:7230",
                        Broker = "2713",
                    };
                    break;
                case '6': //CTP
                    program.trader = new Trade("ctp_trade_proxy.dll")
                    {
                        Server = "tcp://106.39.36.72:51205",
                        Broker = "1010",
                    };
                    program.quoter = new Quote("ctp_quote_proxy.dll")
                    {
                        Server = "tcp://106.39.36.72:51213",
                        Broker = "1010",
                    };
                    break;
                default:
                    Console.WriteLine(Program.LogTitle + "请重新选择");
                    goto R;
            }

            //if (isTest)
            //{
            //    ITradeCenter tradeCenter = new TradeCenterTestImp();

            //    //tradeCenter.init("m1609");
            //    tradeCenter.init("rb1610");
            //    tradeCenter.start();
            //    goto End;
            //}

            Config config = Config.loadConfig();
            if(config == null)
            {
                Console.WriteLine("请输入帐号:");
                program.trader.Investor = Console.ReadLine();
                Console.WriteLine("请输入密码:");
                program.trader.Password = Console.ReadLine();
            }
            else
            {
                program.trader.Investor = program.quoter.Investor = config.user;
                program.trader.Password = program.quoter.Password = config.password;
            }

            program.quoter.OnFrontConnected += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" +"OnFrontConnected");
                Log.log("OnFrontConnected");
                if(Utils.isTradingTimeNow() || Utils.isLogInTimeNow())
                    program.quoter.ReqUserLogin();
            };
            program.quoter.OnRspUserLogin += (sender, e) => 
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogin:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogin:{0}", e.Value));
            };
            program.quoter.OnRspUserLogout += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogout:{0}", e.Value);
                 Log.log(string.Format("OnRspUserLogout:{0}", e.Value));
            };
            program.quoter.OnRtnError += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
                Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
            };
            program.quoter.OnRtnTick += (sender, e) =>
            {
                lock (lockThis)
                {
                    if (program.isInit == false)
                        return;

                    bool needUpdate = false;
                    
                    InstrumentData currentInstrumentdata;

                    if (program.tradeData.TryGetValue(e.Tick.InstrumentID, out currentInstrumentdata) == false)
                    {
                        currentInstrumentdata = new InstrumentData();
                        program.tradeData.Add(e.Tick.InstrumentID, currentInstrumentdata);
                    }

                    List<UnitData> unitDataList;
                    if (program.unitDataMap.TryGetValue(e.Tick.InstrumentID, out unitDataList) == false)
                        return;

                    DateTime d1 = DateTime.ParseExact(program.quoter.TradingDay + " " + e.Tick.UpdateTime, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);

                    if (Utils.isValidData(e.Tick.InstrumentID, d1, e.Tick.UpdateTime) == false)
                    {
                        return;
                    }

                    if (Utils.isTradingTime(e.Tick.InstrumentID, d1) == false)
                        return;

                    InstrumentWatcher.updateTime(e.Tick.InstrumentID, d1);
                    //Console.WriteLine(string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2}", e.Tick.InstrumentID,
                    //       e.Tick.UpdateTime, e.Tick.LastPrice));

                    if (program.isNewBar(currentInstrumentdata.lastUpdateTime,d1,e.Tick.InstrumentID))
                    {
                        int count = unitDataList.Count;
                        if (count > 0)
                        {
                            UnitData lastUnitData = unitDataList.Last();

                            if (count > _TOTALSIZE)
                            {
                                double total = 0;
                                for (int i = 0; i < _TOTALSIZE; i++)
                                {
                                    total += unitDataList.ElementAt(count - i -1).close;
                                }
                                lastUnitData.avg_480 = Math.Round(total / _TOTALSIZE, 2);
                            }
                            currentInstrumentdata.curAvg = lastUnitData.avg_480;
                            Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 平均:{3}", e.Tick.InstrumentID,
                          e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg), e.Tick.InstrumentID);
                            needUpdate = true;
                        }
                        UnitData unitData = new UnitData();
                        unitData.high = unitData.low = unitData.open = unitData.close = e.Tick.LastPrice;
                        unitData.datetime = d1.ToString();
                        unitDataList.Add(unitData);

                        Console.WriteLine(string.Format(Program.LogTitle + "new bar 品种{0} 时间:{1} 当前价格:{2}", e.Tick.InstrumentID,
                           e.Tick.UpdateTime, e.Tick.LastPrice));


                        //UnitData unitData = new UnitData();
                        //unitData.high = unitData.low = unitData.open = unitData.close = e.Tick.LastPrice;
                        //unitData.datetime = d1.ToString();
                        //unitDataList.AddLast(unitData);

                        //Console.WriteLine(string.Format(Program.LogTitle + "new bar 品种{0} 时间:{1} 当前价格:{2}", e.Tick.InstrumentID,
                        //   e.Tick.UpdateTime, e.Tick.LastPrice));

                        //Task.Factory.StartNew(() =>
                        //{
                        //    lock (lockFile)
                        //    {
                        //        string fileNameSerialize = FileUtil.getUnitDataPath(e.Tick.InstrumentID);
                        //        string jsonString = JsonConvert.SerializeObject(unitDataList);
                        //        File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
                        //    }
                        //});
                        

                        //if (unitDataList.Count > _TOTALSIZE)
                        //{
                        //    UnitData[] unitDataArray = unitDataList.ToArray();
                        //    double allColse = 0;
                        //    for (int i = 0; i < _TOTALSIZE; i++)
                        //    {
                        //        allColse += unitDataArray[unitDataList.Count-1-i].close;
                        //    }
                        //    currentInstrumentdata.curAvg = allColse / _TOTALSIZE;

                        //    Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 平均:{3}", e.Tick.InstrumentID,
                        //   e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg), e.Tick.InstrumentID);
                        //    needUpdate = true;
                        //}
                        
                    }
                    else if(unitDataList.Count>0)
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
                    

                    currentInstrumentdata.lastUpdateTime = d1.ToString();
                    InstrumentTradeConfig instrumentConfig;
                    if (program._instrumentMap.TryGetValue(e.Tick.InstrumentID, out instrumentConfig) == false)
                    {
                        Log.log(string.Format(Program.LogTitle + "品种{0} 不在列表中", e.Tick.InstrumentID), e.Tick.InstrumentID);
                        return;
                    }
                    if (instrumentConfig == null)
                    {
                        Log.log(string.Format(Program.LogTitle + "品种{0} 不在列表中", e.Tick.InstrumentID), e.Tick.InstrumentID);
                        return;
                    }
                    if (currentInstrumentdata.curAvg != 0 && e.Tick.LastPrice > currentInstrumentdata.curAvg + instrumentConfig.span)
                    {
                        //Console.WriteLine("品种{0} 时间:{1} 触发新高:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
                       
                        //no trade before
                        if (currentInstrumentdata.holder == 0)
                        {
                            //open buy
                            //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
                            if (instrumentConfig.trade)
                                operatorInstrument(BUY_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice, instrumentConfig.openvolumn);
                            currentInstrumentdata.holder = 1;
                            currentInstrumentdata.isToday = true;
                            currentInstrumentdata.price = e.Tick.LastPrice;
                            needUpdate = true;

                        }
                        else if (currentInstrumentdata.holder == -1)
                        {
                            if (instrumentConfig.trade)
                                operatorInstrument(BUY_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice, instrumentConfig.openvolumn);

                            //close sell and open buy
                            if (currentInstrumentdata.isToday)
                            {
                                //operatord(trader, quoter, BUY_CLOSETODAY, e.Tick.InstrumentID);
                                if (instrumentConfig.trade)
                                    operatorInstrument(BUY_CLOSETODAY, e.Tick.InstrumentID, 0,instrumentConfig.closevolumn);

                            }
                            else
                            {
                                //operatord(trader, quoter, BUY_CLOSE, e.Tick.InstrumentID);
                                if (instrumentConfig.trade)
                                    operatorInstrument(BUY_CLOSE, e.Tick.InstrumentID, 0, instrumentConfig.closevolumn);

                            }
                            //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
                            currentInstrumentdata.holder = 1;
                            currentInstrumentdata.isToday = true;
                            currentInstrumentdata.price = e.Tick.LastPrice;
                            needUpdate = true;
                        }

                        if(needUpdate)
                            Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 突破 平均:{3}+span:{4} 仓位:{5}", e.Tick.InstrumentID,
                          e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, instrumentConfig.span, instrumentConfig.openvolumn), e.Tick.InstrumentID);

                    }

                    else if (currentInstrumentdata.curAvg != 0 && e.Tick.LastPrice < currentInstrumentdata.curAvg - instrumentConfig.span)
                    {
                        //Console.WriteLine("品种{0} 时间:{1} 触发新低:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
                       
                        //no trade before
                        if (currentInstrumentdata.holder == 0)
                        {
                            //open sell
                            //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
                            if (instrumentConfig.trade)
                                operatorInstrument(SELL_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice,instrumentConfig.openvolumn);
                            currentInstrumentdata.holder = -1;
                            currentInstrumentdata.isToday = true;
                            currentInstrumentdata.price = e.Tick.LastPrice;
                            needUpdate = true;

                        }
                        else if (currentInstrumentdata.holder == 1)
                        {
                            if (instrumentConfig.trade)
                                operatorInstrument(SELL_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice,instrumentConfig.openvolumn);

                            //close buy and open sell
                            if (currentInstrumentdata.isToday)
                            {
                                //operatord(trader, quoter, SELL_CLOSETODAY, e.Tick.InstrumentID);
                                if (instrumentConfig.trade)
                                    operatorInstrument(SELL_CLOSETODAY, e.Tick.InstrumentID, 0,instrumentConfig.closevolumn);

                            }
                            else
                            {
                                //operatord(trader, quoter, SELL_CLOSE, e.Tick.InstrumentID);
                                if (instrumentConfig.trade)
                                    operatorInstrument(SELL_CLOSE, e.Tick.InstrumentID, 0,instrumentConfig.closevolumn);
                            }
                            //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
                            currentInstrumentdata.holder = -1;
                            currentInstrumentdata.isToday = true;
                            currentInstrumentdata.price = e.Tick.LastPrice;
                            needUpdate = true;
                        }
                        
                        if(needUpdate)
                            Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 突破 平均:{3}-span:{4} 仓位:{5}", e.Tick.InstrumentID,
                         e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, instrumentConfig.span, instrumentConfig.openvolumn), e.Tick.InstrumentID);

                    }

                    if (needUpdate)
                    {
                        string fileNameSerialize = FileUtil.getTradeFilePath();
                        string jsonString = JsonConvert.SerializeObject(program.tradeData);

                        File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
                    }
                }
            };
            program.trader.OnFrontConnected += (sender, e) =>
            {
                if(Utils.isTradingTimeNow() || Utils.isLogInTimeNow())
                    program.trader.ReqUserLogin();

            };
            program.trader.OnRspUserLogin += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogin:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogin:{0}", e.Value));
                if (e.Value == 0)
                    program.quoter.ReqConnect();
            };
            program.trader.OnRspUserLogout += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogout:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogout:{0}", e.Value));
                //foreach (string key in program.unitDataMap.Keys)
                //{
                //    List<UnitData> unitDataList;
                //    if (program.unitDataMap.TryGetValue(key, out unitDataList))
                //    {
                //        if (unitDataList == null)
                //            continue;
                //        Log.log(string.Format("Quit:saving {0}", key));

                //        //Task.Factory.StartNew(() =>
                //        //{
                //        //    lock(lockFile)
                //        //    {
                //        //        string fileNameSerialize = FileUtil.getUnitDataPath(key);
                //        //        string jsonString = JsonConvert.SerializeObject(unitDataList);
                //        //        File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
                //        //    }
                //        //});
                       
                //    }
                //}
            };
            program.trader.OnRtnCancel += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnCancel:{0}", e.Value.OrderID);
                Log.log(string.Format("OnRtnCancel:{0}", e.Value), e.Value.InstrumentID);
                OrderField orderField = null;
                if (program.tradeCenter._tradeOrders.TryGetValue(e.Value.OrderID, out orderField))
                {
                    program.tradeCenter._tradeOrders.Remove(e.Value.OrderID);
                }
                program.tradeCenter._removingOrders.Remove(e.Value.OrderID);
            };
            program.trader.OnRtnError += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
                Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
            };
            program.trader.OnRtnExchangeStatus += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnExchangeStatus:{0}=>{1}", e.Exchange, e.Status);
                Log.log(string.Format("OnRtnExchangeStatus:{0}=>{1}", e.Exchange , e.Status));

                if(e.Status == ExchangeStatusType.Closed || e.Status == ExchangeStatusType.NoTrading)
                {
                    //foreach (string key in program.unitDataMap.Keys)
                    //{
                    //    if (key.StartsWith(e.Exchange) == false)
                    //        continue;
                    //    LinkedList<UnitData> unitDataList;
                    //    if (program.unitDataMap.TryGetValue(key, out unitDataList))
                    //    {
                    //        if (unitDataList == null)
                    //            continue;
                    //        lock (lockFile)
                    //        {
                    //            Log.log(string.Format("saving {0} ", key));
                    //            string fileNameSerialize = FileUtil.getUnitDataPath(key);
                    //            string jsonString = JsonConvert.SerializeObject(unitDataList);
                    //            File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
                    //        }
                    //    }
                    //}
                }

            };
            program.trader.OnRtnNotice += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnNotice:{0}", e.Value);
                Log.log(string.Format("OnRtnNotice:{0}", e.Value));
            };
            program.trader.OnRtnOrder += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnOrder:{0}", e.Value.OrderID);
                Log.log(string.Format("OnRtnOrder:{0} {1}", e.Value.OrderID, e.Value.LimitPrice),e.Value.InstrumentID);
                _orderId = e.Value.OrderID;
                if (program.tradeCenter._tradeOrders.ContainsKey(_orderId))
                {
                    Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnOrder:{0} _tradeOrders exists");
                    program.tradeCenter._tradeOrders[_orderId] = e.Value;
                }
                else
                    program.tradeCenter._tradeOrders.Add(_orderId, e.Value);

                DateTime d1 = DateTime.Parse(e.Value.InsertTime);
                HashSet<string> waitSecond = null;
                if (program._waitingForOp.TryGetValue(e.Value.InstrumentID, out waitSecond))
                {
                    program._waitingForOp.Remove(e.Value.InstrumentID);
                }
                else
                {
                    waitSecond = new HashSet<string>();
                    waitSecond.Add(d1.ToString("yyyy-MM-dd-HH-mm"));
                    program._waitingForOp.Add(e.Value.InstrumentID, waitSecond);
                }
            };
            program.trader.OnRtnTrade += (sender, e) =>
            {
                lock (lockThis)
                { 
                    Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnTrade:{0}", e.Value.TradeID);
                    Log.log(string.Format("OnRtnTrade:{0} OrderID {1}", e.Value.TradeID, e.Value.OrderID), e.Value.InstrumentID);
                    //成交 需要下委托单
                    string direction = e.Value.Direction == DirectionType.Buy ? "买" : "卖";
                    string offsetType = "开";
                    if (e.Value.Offset == OffsetType.Close)
                        offsetType = "平";
                    else if (e.Value.Offset == OffsetType.CloseToday)
                        offsetType = "平今";
                    Log.logTrade(string.Format("{0},{1},{2},{3},{4},{5}", e.Value.InstrumentID, e.Value.TradingDay, e.Value.TradeTime,
                        e.Value.Price, e.Value.Volume, direction+offsetType));
                    OrderField orderField = null;
                    if(program.tradeCenter._tradeOrders.TryGetValue(e.Value.OrderID,out orderField))
                    {
                        program.tradeCenter._tradeOrders.Remove(e.Value.OrderID);
                    }
                    HashSet<string> waitSecond = null;
                    if (program._waitingForOp.TryGetValue(e.Value.InstrumentID, out waitSecond))
                    {
                        program._waitingForOp.Remove(e.Value.InstrumentID);
                    }
                    program.trader.ReqQryPosition();
                }
            };

            program.trader.ReqConnect();
            Thread.Sleep(3000);
            
            if (!program.trader.IsLogin && (Utils.isLogInTimeNow() || Utils.isTradingTimeNow()))
                goto R;

            program.tradeCenter = new TradeCenter(program.trader, program.quoter, _tradeQueue);
            program.tradeCenter.start();

            InstrumentWatcher.Init(program.trader);
            LoginWatcher.Init(program);
            Console.WriteLine(program.trader.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\t" + n.Value.InstrumentID));

            //使用二进制序列化对象
            //string fileName = @"C:\work\Trade.dat";//文件名称与路径
            //if(isTest)
            //    fileName = @"C:\work\TestTrade.dat";//文件名称与路径
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

            if (tempData != null)
                program.tradeData = tempData;

            foreach(PositionField data in program.trader.DicPositionField.Values)
            {
                InstrumentData instrumentData;
                bool found = program.tradeData.TryGetValue(data.InstrumentID, out instrumentData);
                if (found)
                {
                    if (data.Position > 0)
                    {
                        if (data.Direction == DirectionType.Buy)
                            instrumentData.holder = 1;
                        else
                            instrumentData.holder = -1;

                        if (data.TdPosition > 0)
                        {
                           instrumentData.isToday = true;
                        }
                        else if(data.YdPosition > 0)
                        {
                            instrumentData.isToday = false;
                        }
                        
                    }
                    else
                    {
                        instrumentData.holder = 0;
                    }
                }
            }
            
            fileName = FileUtil.getInstrumentFilePath();
            List<InstrumentTradeConfig> instrumentList = null;
            try
            {
                string text = File.ReadAllText(fileName);
                instrumentList = JsonConvert.DeserializeObject<List<InstrumentTradeConfig>>(text);
            }
            catch (Exception e)
            {
                try
                {
                    List<string> oldinstrumentList = null;
                    string text = File.ReadAllText(fileName);
                    oldinstrumentList = JsonConvert.DeserializeObject<List<string>>(text);
                    instrumentList = new List<InstrumentTradeConfig>();
                    foreach (string inst in oldinstrumentList)
                    {
                        InstrumentTradeConfig instrumentConfig = new InstrumentTradeConfig();
                        instrumentConfig.instrument = inst;
                        instrumentConfig.trade = true;
                        instrumentConfig.openvolumn = 1;
                        instrumentConfig.closevolumn = 1;
                        instrumentList.Add(instrumentConfig);
                        
                    }
                    string jsonString = JsonConvert.SerializeObject(instrumentList);
                    File.WriteAllText(fileName, jsonString, Encoding.UTF8);
                }
                catch (Exception e2)
                {
                }
            }

            if (instrumentList == null || instrumentList.Count == 0)
            {
                string inst = string.Empty;
                Console.WriteLine(Program.LogTitle + "请输入合约:");
                inst = Console.ReadLine();
                //program.quoter.ReqSubscribeMarketData(inst);
                InstrumentTradeConfig instrumentConfig = new InstrumentTradeConfig();
                instrumentConfig.instrument = inst;
                instrumentConfig.trade = true;
                instrumentConfig.openvolumn = 1;
                instrumentConfig.closevolumn = 1;
                program._instrumentList.Clear();
                program._instrumentList.Add(instrumentConfig);
                program._instrumentMap.Add(inst, instrumentConfig);
            }
            else
            {
                program._instrumentList.Clear();
                program._instrumentList.AddRange(instrumentList);
                foreach(InstrumentTradeConfig instrumentConfig in program._instrumentList)
                {
                    program._instrumentMap.Add(instrumentConfig.instrument, instrumentConfig);
                }
                
            }

            program.mongoDB = MongoDbHepler.GetDatabase(connectionString, dbName);

            foreach (string key in program._instrumentMap.Keys)
            {
                program.initUnitDataMap(key);
                //string unitFileName = FileUtil.getUnitDataPath(key);
                //LinkedList<UnitData> unitData = new LinkedList<UnitData>();
                //if (File.Exists(unitFileName))
                //{
                //    string text = File.ReadAllText(unitFileName);
                //    unitData = JsonConvert.DeserializeObject<LinkedList<UnitData>>(text);
                //}

                //program.unitDataMap.Add(key, unitData);
                //if (unitData.Count > _TOTALSIZE)
                //{
                //    UnitData[] unitDataArray = unitData.ToArray();
                //    double allColse = 0;
                //    for (int i = 0; i < _TOTALSIZE; i++)
                //    {
                //        allColse += unitDataArray[unitData.Count - 1 - i].close;
                //    }
                //    Console.WriteLine(string.Format(Program.LogTitle + "品种{0} 平均:{1}", key, allColse / _TOTALSIZE));
                //    Log.log(string.Format(Program.LogTitle + "品种{0} 平均:{1}", key, allColse / _TOTALSIZE), key);
                //}


            }
            if (program.trader.IsLogin)
                program.subscribeInstruments();
            program.isInit = true;

        Inst:
            Console.WriteLine(Program.LogTitle + "q:退出  1-BK  2-SP  3-SK  4-BP  5-撤单");
            Console.WriteLine("a-交易所状态  b-委托  c-成交  d-持仓  e-合约  f-权益 g-换合约 h-平所有仓位 s-立刻保存 t-当前值");

            DirectionType dire = DirectionType.Buy;
            OffsetType offset = OffsetType.Open;
            c = Console.ReadKey().KeyChar;
            switch (c)
            {   
                case '1':
                    dire = DirectionType.Buy;
                    offset = OffsetType.Open;
                    break;
                case '2':
                    dire = DirectionType.Sell;
                    offset = OffsetType.CloseToday;
                    break;
                case '3':
                    dire = DirectionType.Sell;
                    offset = OffsetType.Open;
                    break;
                case '4':
                    dire = DirectionType.Buy;
                    offset = OffsetType.CloseToday;
                    break;
                case '5':
                    program.trader.ReqOrderAction(_orderId);
                    break;
                case 'a':
                    Console.WriteLine(program.trader.DicExcStatus.Aggregate("\r\n交易所状态", (cur, n) => cur + "\r\n" + n.Key + "=>" + n.Value));
                    break;
                case 'b':
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(program.trader.DicOrderField.Aggregate("\r\n委托", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v)
                        => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'c':
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(program.trader.DicTradeField.Aggregate("\r\n成交", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'd': //持仓
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine(program.trader.DicPositionField.Aggregate("\r\n持仓", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    program.trader.ReqQryPosition();
                    break;
                case 'e':
                    Console.WriteLine(program.trader.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'f':
                    Console.WriteLine(program.trader.TradingAccount.GetType().GetFields().Aggregate("\r\n权益\t", (cur, n) => cur + ","
                        + n.GetValue(program.trader.TradingAccount).ToString()));
                    break;
                case 'g':
                    Console.WriteLine(Program.LogTitle + "请输入合约:");
                    string inst = Console.ReadLine();
                    program.quoter.ReqSubscribeMarketData(inst);
                    break;
                case 'h':
                    Console.WriteLine("\r\n！！！输入y确认平所有仓位！！！:");
                    char op = Console.ReadKey().KeyChar;
                    if (op == 'y'||op=='Y')
                    { 
                        Console.WriteLine("\r\n"+Program.LogTitle + "！！！正在平所有仓位！！！");
                        program.closeAllPosition();
                        Console.WriteLine("\r\n" + Program.LogTitle + "！！！已下单平所有可平仓位！！！");

                    }
                    else
                        Console.WriteLine("\r\n" + Program.LogTitle + "放弃平仓");
                    break;
                case 's':
                    foreach (string key in program.unitDataMap.Keys)
                    {
                        List<UnitData> unitDataList;
                        if (program.unitDataMap.TryGetValue(key, out unitDataList))
                        {
                            if (unitDataList == null)
                                continue;
                            Log.log(string.Format("Quit:saving {0} ", key));

                            //Task.Factory.StartNew(() =>
                            //{
                            //    lock(lockFile)
                            //    {
                            //        string fileNameSerialize = FileUtil.getUnitDataPath(key);
                            //        string jsonString = JsonConvert.SerializeObject(unitDataList);
                            //        File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
                            //    }
                            //});
                        }
                    }
                    break;
                case 't':
                    foreach (string key in program.tradeData.Keys)
                    {
                        InstrumentData currentInstrumentdata;
                        if (program.tradeData.TryGetValue(key, out currentInstrumentdata) == false)
                        {
                            if (currentInstrumentdata == null)
                                continue;
                            Log.log(string.Format("品种:{0} 值:{1}", key,currentInstrumentdata.curAvg));

                        }


                    }
                    break;
                case 'q':
                    if(program.trader.IsLogin)
                    { 
                        program.quoter.ReqUserLogout();
                        program.trader.ReqUserLogout();
                    }
                    //foreach (string key in program.unitDataMap.Keys)
                    //{
                    //    LinkedList<UnitData> unitDataList;
                    //    if (program.unitDataMap.TryGetValue(key, out unitDataList))
                    //    {
                    //        if (unitDataList == null)
                    //            continue;
                    //        Log.log(string.Format("Quit:saving {0} ", key));
                            
                    //        Task.Factory.StartNew(() =>
                    //        {
                    //            lock(lockFile)
                    //            {
                    //                string fileNameSerialize = FileUtil.getUnitDataPath(key);
                    //                string jsonString = JsonConvert.SerializeObject(unitDataList);
                    //                File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
                    //            }
                    //        });
                    //    }
                    //}
                    program.tradeCenter.stop();
                    program.tradeCenter = null;
                    InstrumentWatcher.flag = false;
                    Thread.Sleep(2000); //待接口处理后续操作
                    Environment.Exit(0);
                    break;
            }
            if (c >= '1' && c <= '4')
            {
                Console.WriteLine(Program.LogTitle + "请选择委托类型: 1-限价  2-市价  3-FAK  4-FOK");
                OrderType ot = OrderType.Limit;
                switch (Console.ReadKey().KeyChar)
                {
                    case '2':
                        ot = OrderType.Market;
                        break;
                    case '3':
                        ot = OrderType.FAK;
                        break;
                    case '4':
                        ot = OrderType.FOK;
                        break;
                }
               
            }
            goto Inst;
            End:
                Console.WriteLine(Program.LogTitle + " end");
        }
        
    }
}
