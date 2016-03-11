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

namespace ConsoleProxy
{
    [Serializable]
    class InstrumentData
    {
        public LinkedList<double> _highList;
        public LinkedList<double> _lowList;
        public int lastMin = -1;
        public int holder = 0;
        public double highest;
        public double lowest;
        public bool isToday = true;
        public InstrumentData()
        {   
            _highList = new LinkedList<double>();
            _lowList = new LinkedList<double>();
            highest = double.NaN;
            lowest = double.NaN;
        }
    }

    class InstrumentTradeConfig
    {
        public string instrument;
        public bool trade;
        public int volumn;
    }


    class Program
    {
        static private string _inst;
        private static int _TOTALSIZE = 9;
        private static int MINSPAN = 30;
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
        private static ConcurrentQueue<TradeItem> _tradeQueue = new ConcurrentQueue<TradeItem>();
        public const bool isTest = true;
        public static string LogTitle = isTest?"[测试]":"[正式]";

        private List<InstrumentTradeConfig> _instrumentList = new List<InstrumentTradeConfig>();
        private Dictionary<string, InstrumentTradeConfig> _instrumentMap = new Dictionary<string, InstrumentTradeConfig>();
        private Dictionary<int, OrderField> _tradeOrders = new Dictionary<int, OrderField>();
        private HashSet<int> _removingOrders = new HashSet<int>();
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
                    //case '5':
                    //    t.ReqOrderAction(_orderId);
                    //    break;
                    //case 'a':
                    //    Console.WriteLine(t.DicExcStatus.Aggregate("\r\n交易所状态", (cur, n) => cur + "\r\n" + n.Key + "=>" + n.Value));
                    //    break;
                    //case 'b':
                    // 89oiy    Console.ForegroundColor = ConsoleColor.Cyan;
                    //    Console.WriteLine(t.DicOrderField.Aggregate("\r\n委托", (cur, n) => cur + "\r\n"
                    //        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v)
                    //        => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    //    break;
                    //case 'c':
                    //    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    //    Console.WriteLine(t.DicTradeField.Aggregate("\r\n成交", (cur, n) => cur + "\r\n"
                    //        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    //    break;
                    //case 'd': //持仓
                    //    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    //    Console.WriteLine(t.DicPositionField.Aggregate("\r\n持仓", (cur, n) => cur + "\r\n"
                    //        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    //    break;
                    //case 'e':
                    //    Console.WriteLine(t.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\r\n"
                    //        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    //    break;
                    //case 'f':
                    //    Console.WriteLine(t.TradingAccount.GetType().GetFields().Aggregate("\r\n权益\t", (cur, n) => cur + ","
                    //        + n.GetValue(t.TradingAccount).ToString()));
                    //    break;
                    //case 'g':
                    //    Console.WriteLine("请输入合约:");
                    //    inst = Console.ReadLine();
                    //    q.ReqSubscribeMarketData(inst);
                    //    break;
                    //case 'q':
                    //    q.ReqUserLogout();
                    //    t.ReqUserLogout();
                    //    Thread.Sleep(2000); //待接口处理后续操作
                    //    Environment.Exit(0);
                    //    break;
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
            //TradeItem tradeItem = new TradeItem(inst, op, price);

            //lock (_tradeQueue)
            //{
            //    //Monitor.Wait(_tradeQueue);//暂时放弃调用线程对该资源的锁，让Consumer执行
            //    _tradeQueue.Enqueue(tradeItem);//生成一个资源
            //    Monitor.Pulse(_tradeQueue);//通知在Wait中阻塞的Consumer线程即将执行
            //}
            operatorInstrument(op, inst, price, 1);
        }
        private static void operatorInstrument(int op, string inst, double price,int volumn)
        {
            TradeItem tradeItem = new TradeItem(inst, op, price,volumn);

            lock (_tradeQueue)
            {
                //Monitor.Wait(_tradeQueue);//暂时放弃调用线程对该资源的锁，让Consumer执行
                _tradeQueue.Enqueue(tradeItem);//生成一个资源
                Monitor.Pulse(_tradeQueue);//通知在Wait中阻塞的Consumer线程即将执行
            }
        }
        void subscribeInstruments()
        {
            foreach (InstrumentTradeConfig inst in _instrumentList)
            {
                Console.WriteLine(Program.LogTitle + "品种:{0} 交易:{1} 仓位:{2}",inst.instrument,inst.trade?"YES":"NO",inst.volumn);

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
                    this._removingOrders.Clear();
                    this._tradeOrders.Clear();
                }

                Thread.Sleep(3000);
                Console.WriteLine(Program.LogTitle + "trade logout");
                Log.log(Program.LogTitle + "trade logout");
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
                }

            }
            
        }
        //输入：q1ctp /t1ctp /q2xspeed /t2speed
        private static void Main(string[] args)
        {
            Program program = new Program();
            System.Object lockThis = new System.Object();
            Dictionary<string, InstrumentData> tradeData = new Dictionary<string, InstrumentData>();
            bool isInit = true;
           
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
                    //t = new Trade("ctp_trade_proxy.dll")
                    //{
                    //	Server = "tcp://211.95.40.130:51205", 
                    //	Broker = "1017",
                    //};
                    //q = new Quote("ctp_quote_proxy.dll")
                    //{
                    //	Server = "tcp://211.95.40.130:51213",
                    //	Broker = "1017",
                    //};
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
                    Boolean isTriggerLow = false;
                    Boolean isTriggerHigh = false;
                    //Console.WriteLine("OnRtnTick:{0}", e.Tick.LastPrice);
                    //Console.WriteLine("OnRtnTick:{0}=>{1}=>{2}", e.Tick.UpdateTime, e.Tick.AskPrice, e.Tick.InstrumentID);
                    DateTime d1 = DateTime.Parse(e.Tick.UpdateTime);

                    if (Utils.isTradingTime(e.Tick.InstrumentID,d1) == false)
                        return;

                    InstrumentWatcher.updateTime(e.Tick.InstrumentID, d1);
                    InstrumentData instrumentdata;

                    if (tradeData.TryGetValue(e.Tick.InstrumentID, out instrumentdata) == false)
                    {
                        instrumentdata = new InstrumentData();
                        tradeData.Add(e.Tick.InstrumentID, instrumentdata);
                    }

                    LinkedList<double> _highList = instrumentdata._highList;
                    LinkedList<double> _lowList = instrumentdata._lowList;


                    if (instrumentdata.lastMin == -1 || (instrumentdata.lastMin != d1.Minute && d1.Minute % MINSPAN == 0))
                    {
                        //Console.WriteLine("OnRtnTick 目前记录数:{0} {1}", e.Tick.InstrumentID, _highList.Count);
                        _highList.AddLast(e.Tick.LastPrice);
                        if (_highList.Count > _TOTALSIZE)
                            _highList.RemoveFirst();

                        _lowList.AddLast(e.Tick.LastPrice);
                        if (_lowList.Count > _TOTALSIZE)
                            _lowList.RemoveFirst();
                        isTriggerHigh = true;
                        isTriggerLow = true;

                        instrumentdata.highest = 0;
                        instrumentdata.lowest = 1000000;
                        foreach (double value in _highList)
                        {
                            //Console.WriteLine("品种{0} 最高:{1} 当前k线:{2}", e.Tick.InstrumentID, highest, value);
                            Log.log(string.Format(Program.LogTitle + "品种{0} 最高:{1} 当前k线:{2}", e.Tick.InstrumentID, instrumentdata.highest, value));

                            if (value > instrumentdata.highest)
                                instrumentdata.highest = value;

                        }

                        foreach (double value in _lowList)
                        {
                            //Console.WriteLine("品种{0} 最低:{1} 当前k线:{2}", e.Tick.InstrumentID, lowest, value);
                            Log.log(string.Format("品种{0} 最低:{1} 当前k线:{2}", e.Tick.InstrumentID, instrumentdata.lowest, value));
                            if (value < instrumentdata.lowest)
                                instrumentdata.lowest = value;
                        }
                        //Console.WriteLine("品种{0}新K线 最高:{1} 最低:{2}", e.Tick.InstrumentID, highest, lowest);
                        Log.log(string.Format(Program.LogTitle + "品种{0}新K线 最高:{1} 最低:{2}", e.Tick.InstrumentID, instrumentdata.highest, instrumentdata.lowest));
                    }
                    else
                    {
                        double _lastHigh = _highList.Last();
                        double _lastLow = _lowList.Last();
                        if (e.Tick.LastPrice > _lastHigh)
                        {
                            _highList.RemoveLast();
                            _highList.AddLast(e.Tick.LastPrice);
                            isTriggerHigh = true;
                        }

                        if (e.Tick.LastPrice < _lastLow)
                        {
                            _lowList.RemoveLast();
                            _lowList.AddLast(e.Tick.LastPrice);
                            isTriggerLow = true;
                        }
                    }

                    OrderField openOrder = null;
                    foreach (OrderField order in program._tradeOrders.Values)
                    {
                        if(order.InstrumentID == e.Tick.InstrumentID && order.Offset == OffsetType.Open)
                        {
                            openOrder = order;
                            break;
                        }
                    }
                    if(openOrder !=null && program._removingOrders.Contains(openOrder.OrderID) == false)
                    { 
                        if (instrumentdata.holder == 1 && openOrder.Direction == DirectionType.Buy
                            && instrumentdata.lowest > openOrder.LimitPrice)
                        {
                            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" 
                                + "Cancel buy:{0}, price:{1}, lowest {2}", openOrder.OrderID
                                , openOrder.LimitPrice, instrumentdata.lowest);
                            program._removingOrders.Add(openOrder.OrderID);
                            program.trader.ReqOrderAction(openOrder.OrderID);
                        }
                        else if (instrumentdata.holder == -1 && openOrder.Direction == DirectionType.Sell
                            && instrumentdata.highest < openOrder.LimitPrice)
                        {
                            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]"
                                + "Cancel sell:{0}, price:{1}, highest {2}", openOrder.OrderID
                                , openOrder.LimitPrice, instrumentdata.highest);
                            program._removingOrders.Add(openOrder.OrderID);
                            program.trader.ReqOrderAction(openOrder.OrderID);
                        }
                    }


                    instrumentdata.lastMin = d1.Minute;
                    InstrumentTradeConfig instrumentConfig = program._instrumentMap[e.Tick.InstrumentID];
                    if (instrumentConfig == null)
                    {
                        Log.log(string.Format(Program.LogTitle + "品种{0} 不在列表中", e.Tick.InstrumentID));
                        return;
                    }
                    if (isTriggerHigh)
                    {
                        //Console.WriteLine("品种{0} 时间:{1} 触发新高:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
                        Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 触发新高:{2} 当前最高{3}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice, instrumentdata.highest));
                        // more than _totoalSize , can trade now
                        if (_highList.Count >= _TOTALSIZE && e.Tick.LastPrice > instrumentdata.highest)
                        {
                            //no trade before
                            if (instrumentdata.holder == 0)
                            {
                                //open buy
                                //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
                                if(instrumentConfig.trade)
                                    operatorInstrument(BUY_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice- 2* program.trader.DicInstrumentField[e.Tick.InstrumentID].PriceTick);
                                instrumentdata.holder = 1;
                                instrumentdata.isToday = true;


                            }
                            else if (instrumentdata.holder == -1)
                            {
                                //close sell and open buy
                                if (instrumentdata.isToday)
                                {
                                    //operatord(trader, quoter, BUY_CLOSETODAY, e.Tick.InstrumentID);
                                    if (instrumentConfig.trade)
                                        operatorInstrument(BUY_CLOSETODAY, e.Tick.InstrumentID,0);

                                }
                                else
                                {
                                    //operatord(trader, quoter, BUY_CLOSE, e.Tick.InstrumentID);
                                    if (instrumentConfig.trade)
                                        operatorInstrument(BUY_CLOSE, e.Tick.InstrumentID,0);

                                }
                                //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
                                if (instrumentConfig.trade)
                                    operatorInstrument(BUY_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice - 2* program.trader.DicInstrumentField[e.Tick.InstrumentID].PriceTick);
                                instrumentdata.holder = 1;
                                instrumentdata.isToday = true;
                            }
                        }


                    }

                    if (isTriggerLow)
                    {
                        //Console.WriteLine("品种{0} 时间:{1} 触发新低:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
                        Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 触发新低:{2} 当前最低:{3}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice, instrumentdata.lowest));

                        // more than _totoalSize , can trade now
                        if (_lowList.Count >= _TOTALSIZE && e.Tick.LastPrice < instrumentdata.lowest)
                        {
                            //no trade before
                            if (instrumentdata.holder == 0)
                            {
                                //open sell
                                //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
                                if (instrumentConfig.trade)
                                    operatorInstrument(SELL_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice + 2 * program.trader.DicInstrumentField[e.Tick.InstrumentID].PriceTick);
                                instrumentdata.holder = -1;
                                instrumentdata.isToday = true;
                            }
                            else if (instrumentdata.holder == 1)
                            {
                                //close buy and open sell
                                if (instrumentdata.isToday)
                                {
                                    //operatord(trader, quoter, SELL_CLOSETODAY, e.Tick.InstrumentID);
                                    if (instrumentConfig.trade)
                                        operatorInstrument(SELL_CLOSETODAY, e.Tick.InstrumentID,0);

                                }
                                else
                                {
                                    //operatord(trader, quoter, SELL_CLOSE, e.Tick.InstrumentID);
                                    if (instrumentConfig.trade)
                                        operatorInstrument(SELL_CLOSE, e.Tick.InstrumentID,0);
                                }
                                //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
                                if (instrumentConfig.trade)
                                    operatorInstrument(SELL_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice + 2 * program.trader.DicInstrumentField[e.Tick.InstrumentID].PriceTick);
                                instrumentdata.holder = -1;
                                instrumentdata.isToday = true;
                            }
                        }
                    }

                    if (isTriggerHigh || isTriggerLow) { 
                        
                        string fileNameSerialize = FileUtil.getTradeFilePath();
                        string jsonString = JsonConvert.SerializeObject(tradeData);
                        
                        File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
                  }
                }
            };

            program.trader.OnFrontConnected += (sender, e) =>
            {
                if(Utils.isTradingTimeNow() || Utils.isLogInTimeNow()|| isInit)
                    program.trader.ReqUserLogin();

                if (isInit)
                    isInit = false;
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
            };
            program.trader.OnRtnCancel += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnCancel:{0}", e.Value.OrderID);
                Log.log(string.Format("OnRtnCancel:{0}", e.Value));
                OrderField orderField = null;
                if (program._tradeOrders.TryGetValue(e.Value.OrderID, out orderField))
                {
                    program._tradeOrders.Remove(e.Value.OrderID);
                }
                program._removingOrders.Remove(e.Value.OrderID);
            };
            program.trader.OnRtnError += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
                Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
            };
            program.trader.OnRtnExchangeStatus += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnExchangeStatus:{0}=>{1}", e.Exchange, e.Status);
                Log.log(string.Format("OnRtnExchangeStatus:{0}=>{1}", e.Exchange, e.Status));
            };
            program.trader.OnRtnNotice += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnNotice:{0}", e.Value);
                Log.log(string.Format("OnRtnNotice:{0}", e.Value));
            };
            program.trader.OnRtnOrder += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnOrder:{0}", e.Value.OrderID);
                Log.log(string.Format("OnRtnOrder:{0}", e.Value.OrderID));
                _orderId = e.Value.OrderID;
                if (program._tradeOrders.ContainsKey(_orderId))
                {
                    Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnOrder:{0} _tradeOrders exists");
                    program._tradeOrders[_orderId] = e.Value;
                }
                else
                    program._tradeOrders.Add(_orderId, e.Value);
            };
            program.trader.OnRtnTrade += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnTrade:{0}", e.Value.TradeID);
                Log.log(string.Format("OnRtnTrade:{0} OrderID {1}", e.Value.TradeID, e.Value.OrderID));
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
                if(program._tradeOrders.TryGetValue(e.Value.OrderID,out orderField))
                {
                    program._tradeOrders.Remove(e.Value.OrderID);
                }
            };

            program.trader.ReqConnect();
            Thread.Sleep(3000);
            
            if (!program.trader.IsLogin)
                goto R;

            TradeCenter tradeCenter = new TradeCenter(program.trader, program.quoter, _tradeQueue);
            tradeCenter.start();

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
                tradeData = tempData;


            foreach(PositionField data in program.trader.DicPositionField.Values)
            {
                InstrumentData instrumentData;
                bool found = tradeData.TryGetValue(data.InstrumentID, out instrumentData);
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
                        instrumentConfig.volumn = 1;
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
                instrumentConfig.volumn = 1;
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
            program.subscribeInstruments();

        Inst:
            Console.WriteLine(Program.LogTitle + "q:退出  1-BK  2-SP  3-SK  4-BP  5-撤单");
            Console.WriteLine("a-交易所状态  b-委托  c-成交  d-持仓  e-合约  f-权益 g-换合约 h-平所有仓位");

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
                case 'q':
                    program.quoter.ReqUserLogout();
                    program.trader.ReqUserLogout();
                    tradeCenter.stop();
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
                //MarketData tick;
                //if (quoter.DicTick.TryGetValue(inst, out tick))
                //{
                //    double price = dire == DirectionType.Buy ? tick.AskPrice : tick.BidPrice;
                //    _orderId = -1;
                //    Console.WriteLine(trader.ReqOrderInsert(inst, dire, offset, price, 1, pType: ot));
                //    for (int i = 0; i < 3; ++i)
                //    {
                //        Thread.Sleep(200);
                //        if (-1 != _orderId)
                //        {
                //            Console.WriteLine(Program.LogTitle + "委托标识:" + _orderId);
                //            break;
                //        }
                //    }
                //}
            }
            goto Inst;
        }
    }
}
