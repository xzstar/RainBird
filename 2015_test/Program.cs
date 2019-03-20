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
using System.Globalization;
using StackExchange.Redis;


namespace ConsoleProxy
{
   


    class Program
    {
        private static int _orderId;

        Config config;
        Trade trader;
        Quote quoter;
        HeartBeatService heartBeatService;

        TradeCenter tradeCenter;

        static Object lockFile = new Object();
        private ConcurrentQueue<TradeItem> _tradeQueue = new ConcurrentQueue<TradeItem>();
        //public const bool isTest = true;
        public const bool withoutDB = false;
        //public const bool withoutRedis = true;
        public static string LogTitle;// = isTest?"[测试]":"[正式]";

        private Dictionary<string, InstrumentData> tradeData = new Dictionary<string, InstrumentData>();
        private Dictionary<string, HashSet<string>> _waitingForOp = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, List<UnitData>> unitDataMap = new Dictionary<string, List<UnitData>>();

        private bool isInit = false;

        public Program(Config config)
        {
            this.config = config;
            LogTitle = config.isTest ? "[测试]" : "[正式]";
        }

        public void initTrader()
        {
            if (config.isTest)
            {
                trader = new Trade("ctp_trade_proxy.dll")
                {
                    Server = "tcp://180.168.146.187:10000",
                    Broker = "9999"// "4040",
                };

            }
            else
            {
                trader = new Trade("ctp_trade_proxy.dll")
                {
                    Server = "tcp://180.166.37.129:41205", //国信
                    Broker = "8030"

                    //Server = "tcp://222.73.111.150:41205",//" tcp://101.95.8.178:51205",//中建 
                    //Broker = "9080"// "9999"// "4040",
                };
            }
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


        private void operatorInstrument(int op, string inst, double price,int volumn)
        {
            if (volumn <= 0)
            {
                if (op == TradeCenter.BUY_OPEN || op == TradeCenter.SELL_OPEN)
                {
                    Console.WriteLine(Program.LogTitle + "操作:{0}: {1} volumn == 0", op, inst);
                    Log.log(string.Format(Program.LogTitle + "操作:{0}: {1} volumn == 0", op, inst));
                    return;
                }
                else {
                    Console.WriteLine(Program.LogTitle + "操作:{0}: {1} volumn == 0 change to {2}", op, inst, volumn);
                    Log.log(string.Format(Program.LogTitle + "操作:{0}: {1} volumn == 0 change to {2}", op, inst, volumn));
                    if (volumn == 0)
                        volumn = 1;
                    else
                        volumn = -volumn;
                }
            }

            TradeItem tradeItem = new TradeItem(inst, op, price, volumn);

            lock (_tradeQueue)
            {
                _tradeQueue.Enqueue(tradeItem);//生成一个资源
                Monitor.Pulse(_tradeQueue);//通知在Wait中阻塞的Consumer线程即将执行
            }
        }

        private void subscribeInstruments()
        {
            foreach (string instrument in tradeData.Keys)
            {
                InstrumentData data = tradeData[instrument];
                
                Console.WriteLine(Program.LogTitle + "品种:{0} 交易:{1} 开仓位:{2} 平仓位:{3} 方向:{4}",
                    instrument, data.trade ? "YES" : "NO", data.openvolumn, data.closevolumn, data.holder == 0? "无":(data.holder == 1 ? "买":"卖"));
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

            }
            else if(Utils.isLogInTimeNow() && !trader.IsLogin)
            {
                Console.WriteLine(Program.LogTitle + "isLogInTimeNow");
                int errorCount = 0;
                while (!trader.IsLogin && errorCount < 5)
                {
                    Console.WriteLine(Program.LogTitle + "trade ReqConnect");

                    trader.ReqConnect();
                    Thread.Sleep(30000);
                    if (!quoter.IsLogin)
                        Thread.Sleep(10000);
                    errorCount++;


                    if (!trader.IsLogin)
                    {
                        Console.WriteLine(Program.LogTitle + "trade login failed");
                        Log.log(Program.LogTitle + "trade login failed");
                        //HttpHelper.HttpPostToWechat(trader.Investor + " trade login failed");
                    }
                    else
                    {
                        subscribeInstruments();
                        Console.WriteLine(Program.LogTitle + "trade login");
                        Log.log(Program.LogTitle + "trade login");
                        //HttpHelper.HttpPostToWechat(trader.Investor + " trade login");
                        break;
                    }
                }

            }
            
        }

        private void syncData()
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

            if (tempData != null && tempData.Count != 0)
                tradeData = tempData;
            else if(isInit == false) //第一次启动
            {
                string inst = string.Empty;
                Console.WriteLine(Program.LogTitle + "请输入合约:");
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

            foreach (PositionField data in trader.DicPositionField.Values)
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
                        else if (data.YdPosition > 0)
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

            unitDataMap.Clear();
            DataService dataService = new DataService(!withoutDB);
            dataService.initUnitDataMap(unitDataMap, tradeData.Keys);

            if (trader.IsLogin)
                subscribeInstruments();
        }

        private void init()
        {
            initQuoter();
            initTrader();
            initDelegate();
            trader.Investor = quoter.Investor = config.user;
            trader.Password = quoter.Password = config.password;
            

        }

        private void initDelegate()
        {
            initQuoteDelegate();
            initTradeDelegate();
        }

        private void initQuoteDelegate()
        {
            quoter.OnFrontConnected += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnFrontConnected");
                Log.log("OnFrontConnected");
                //HttpHelper.HttpPostToWechat(program.trader.Investor + " OnFrontConnected");
                if (Utils.isTradingTimeNow() || Utils.isLogInTimeNow())
                    quoter.ReqUserLogin();
            };
            quoter.OnRspUserLogin += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogin:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogin:{0}", e.Value));
            };
            quoter.OnRspUserLogout += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogout:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogout:{0}", e.Value));
            };
            quoter.OnRtnError += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
                Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
            };
            quoter.OnRtnTick += (sender, e) =>
            {
                lock (lockFile)
                {
                    if (isInit == false)
                        return;

                    bool needUpdate = false;

                    InstrumentData currentInstrumentdata;

                    if (tradeData.TryGetValue(e.Tick.InstrumentID, out currentInstrumentdata) == false)
                    {
                        currentInstrumentdata = new InstrumentData();
                        tradeData.Add(e.Tick.InstrumentID, currentInstrumentdata);
                    }

                    List<UnitData> unitDataList;
                    if (unitDataMap.TryGetValue(e.Tick.InstrumentID, out unitDataList) == false)
                        return;

                    DateTime d1 = DateTime.ParseExact(quoter.TradingDay + " " + e.Tick.UpdateTime, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);

                    if (Utils.isValidData(e.Tick.InstrumentID, d1, e.Tick.UpdateTime) == false)
                    {
                        return;
                    }

                    if (Utils.isTradingTime(e.Tick.InstrumentID, d1) == false)
                        return;

                    InstrumentWatcher.updateTime(e.Tick.InstrumentID, d1);
                    //Console.WriteLine(string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2}", e.Tick.InstrumentID,
                    //       e.Tick.UpdateTime, e.Tick.LastPrice));

                    if (Utils.isNewBar(currentInstrumentdata.lastUpdateTime, d1, e.Tick.InstrumentID))
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

                                if (heartBeatService.isAvailable())
                                {
                                    TimeBarInfo timeBarInfo = new TimeBarInfo();
                                    timeBarInfo.user = trader.Investor;
                                    timeBarInfo.instrument = e.Tick.InstrumentID;
                                    timeBarInfo.price = Convert.ToString(lastUnitData.avg_480);
                                    timeBarInfo.time = quoter.TradingDay + " " + e.Tick.UpdateTime;
                                    heartBeatService.publishInfo("timebarinfo", JsonConvert.SerializeObject(timeBarInfo));
                                }

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


                    currentInstrumentdata.lastUpdateTime = d1.ToString();
                    InstrumentData instrumentData;
                    if (tradeData.TryGetValue(e.Tick.InstrumentID, out instrumentData) == false)
                    {
                        Log.log(string.Format(Program.LogTitle + "品种{0} 不在列表中", e.Tick.InstrumentID), e.Tick.InstrumentID);
                        return;
                    }
                    if (instrumentData == null)
                    {
                        Log.log(string.Format(Program.LogTitle + "品种{0} 不在列表中", e.Tick.InstrumentID), e.Tick.InstrumentID);
                        return;
                    }

                    if (instrumentData.trade == false)
                    {
                        return;
                    }

                    if (currentInstrumentdata.curAvg == 0)
                    {
                        return;
                    }

                    int pos = instrumentData.closevolumn;
                    PositionField posField;
                    if (trader.DicPositionField.TryGetValue(e.Tick.InstrumentID, out posField))
                    {
                        pos = posField.TdPosition;
                    }

                    if (e.Tick.LastPrice > currentInstrumentdata.curAvg && currentInstrumentdata.holder == -1)
                    {
                        //close sell
                        if (currentInstrumentdata.isToday)
                        {
                            operatorInstrument(TradeCenter.BUY_CLOSETODAY, e.Tick.InstrumentID, 0, pos);
                        }
                        else
                        {
                            operatorInstrument(TradeCenter.BUY_CLOSE, e.Tick.InstrumentID, 0, pos);
                        }
                        currentInstrumentdata.holder = 0;
                        currentInstrumentdata.isToday = true;
                        currentInstrumentdata.price = e.Tick.LastPrice;
                        needUpdate = true;
                        string info = string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 突破 平均:{3} 平仓:{4}", e.Tick.InstrumentID,
                         e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, pos);
                        Log.log(info, e.Tick.InstrumentID);

                        info = string.Format("user:[{5}] -- {0} :{1} price:{2} break avg:{3} close:{4}", e.Tick.InstrumentID,
                        e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, pos, trader.Investor);
                        //HttpHelper.HttpPostToWechat(info);
                        MailService.Notify(LogTitle + " [Info]", info);


                    }
                    else if (e.Tick.LastPrice < currentInstrumentdata.curAvg && currentInstrumentdata.holder == 1)
                    {
                        //close buy 
                        if (currentInstrumentdata.isToday)
                        {
                            operatorInstrument(TradeCenter.SELL_CLOSETODAY, e.Tick.InstrumentID, 0, pos);

                        }
                        else
                        {
                            operatorInstrument(TradeCenter.SELL_CLOSE, e.Tick.InstrumentID, 0, pos);
                        }
                        currentInstrumentdata.holder = 0;
                        currentInstrumentdata.isToday = true;
                        currentInstrumentdata.price = e.Tick.LastPrice;
                        needUpdate = true;

                        string info = string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 突破 平均:{3} 平仓:{4}", e.Tick.InstrumentID,
                         e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, pos);
                        Log.log(info, e.Tick.InstrumentID);

                        info = string.Format("user:[{5}] -- {0} :{1} price:{2} break avg:{3} close:{4}", e.Tick.InstrumentID,
                         e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, pos, trader.Investor);
                        //HttpHelper.HttpPostToWechat(info);
                        MailService.Notify(LogTitle + " [Info]", info);

                    }


                    if (e.Tick.LastPrice > currentInstrumentdata.curAvg + e.Tick.LastPrice * instrumentData.span)
                    {
                        //Console.WriteLine("品种{0} 时间:{1} 触发新高:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
                        //no trade before
                        if (currentInstrumentdata.holder == 0)
                        {
                            //open buy
                            //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
                            operatorInstrument(TradeCenter.BUY_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice, instrumentData.openvolumn);
                            currentInstrumentdata.holder = 1;
                            currentInstrumentdata.isToday = true;
                            currentInstrumentdata.price = e.Tick.LastPrice;
                            needUpdate = true;
                            string info = string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 突破 平均:{3}+span:{4} 仓位:{5}", e.Tick.InstrumentID,
                         e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, e.Tick.LastPrice * instrumentData.span, instrumentData.openvolumn);
                            Log.log(info, e.Tick.InstrumentID);

                            info = string.Format("user:[{6}] -- {0} :{1} price:{2} break avg:{3}+span:{4} open:{5}", e.Tick.InstrumentID,
                         e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, e.Tick.LastPrice * instrumentData.span, instrumentData.openvolumn, trader.Investor);
                            //HttpHelper.HttpPostToWechat(info);
                            MailService.Notify(LogTitle + " [Info]", info);

                        }

                    }

                    else if (e.Tick.LastPrice < currentInstrumentdata.curAvg - e.Tick.LastPrice * instrumentData.span)
                    {
                        //Console.WriteLine("品种{0} 时间:{1} 触发新低:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);

                        //no trade before
                        if (currentInstrumentdata.holder == 0)
                        {
                            //open sell
                            //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
                            operatorInstrument(TradeCenter.SELL_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice, instrumentData.openvolumn);
                            currentInstrumentdata.holder = -1;
                            currentInstrumentdata.isToday = true;
                            currentInstrumentdata.price = e.Tick.LastPrice;
                            needUpdate = true;
                            string info = string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 突破 平均:{3}-span:{4} 仓位:{5}", e.Tick.InstrumentID,
                        e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, e.Tick.LastPrice * instrumentData.span, instrumentData.openvolumn);
                            Log.log(info, e.Tick.InstrumentID);
                            info = string.Format("user:[{6}] -- {0} :{1} price:{2} break avg:{3}-span:{4} open:{5}", e.Tick.InstrumentID,
                        e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, e.Tick.LastPrice * instrumentData.span, instrumentData.openvolumn, trader.Investor);
                            //HttpHelper.HttpPostToWechat(info);
                            MailService.Notify(LogTitle + " [Info]", info);
                        }

                    }

                    if (needUpdate)
                    {
                        string fileNameSerialize = FileUtil.getTradeFilePath();
                        string jsonString = JsonConvert.SerializeObject(tradeData);

                        File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
                    }
                }
            };

        }

        private void initTradeDelegate()
        {
            trader.OnFrontConnected += (sender, e) =>
            {
                if (Utils.isTradingTimeNow() || Utils.isLogInTimeNow())
                    trader.ReqUserLogin();

            };
            trader.OnRspUserLogin += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogin:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogin:{0}", e.Value));
                MailService.Notify(LogTitle + " [启动]", trader.Investor + " 登录成功");

                if (e.Value == 0)
                    quoter.ReqConnect();
            };
            trader.OnRspUserLogout += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogout:{0}", e.Value);
                Log.log(string.Format("OnRspUserLogout:{0}", e.Value));
            };
            trader.OnRtnCancel += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnCancel:{0}", e.Value.OrderID);
                Log.log(string.Format("OnRtnCancel:{0}", e.Value), e.Value.InstrumentID);
                OrderField orderField = null;
                if (tradeCenter._tradeOrders.TryGetValue(e.Value.OrderID, out orderField))
                {
                    tradeCenter._tradeOrders.Remove(e.Value.OrderID);
                }
                tradeCenter._removingOrders.Remove(e.Value.OrderID);
            };
            trader.OnRtnError += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
                Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
            };
            trader.OnRtnExchangeStatus += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnExchangeStatus:{0}=>{1}", e.Exchange, e.Status);
                Log.log(string.Format("OnRtnExchangeStatus:{0}=>{1}", e.Exchange, e.Status));

                if (e.Status == ExchangeStatusType.Closed || e.Status == ExchangeStatusType.NoTrading)
                {

                }

            };
            trader.OnRtnNotice += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnNotice:{0}", e.Value);
                Log.log(string.Format("OnRtnNotice:{0}", e.Value));
            };
            trader.OnRtnOrder += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnOrder:{0}", e.Value.OrderID);
                string info = string.Format("OnRtnOrder:{0} {1}", e.Value.OrderID, e.Value.LimitPrice);
                Log.log(info, e.Value.InstrumentID);
                _orderId = e.Value.OrderID;
                if (tradeCenter._tradeOrders.ContainsKey(_orderId))
                {
                    Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnOrder:{0} _tradeOrders exists");
                    tradeCenter._tradeOrders[_orderId] = e.Value;
                }
                else
                    tradeCenter._tradeOrders.Add(_orderId, e.Value);

                DateTime d1 = DateTime.Parse(e.Value.InsertTime);
                HashSet<string> waitSecond = null;
                if (_waitingForOp.TryGetValue(e.Value.InstrumentID, out waitSecond))
                {
                    _waitingForOp.Remove(e.Value.InstrumentID);
                }
                else
                {
                    waitSecond = new HashSet<string>();
                    waitSecond.Add(d1.ToString("yyyy-MM-dd-HH-mm"));
                    _waitingForOp.Add(e.Value.InstrumentID, waitSecond);
                }
            };
            trader.OnRtnTrade += (sender, e) =>
            {
                lock (lockFile)
                {
                    Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnTrade:{0}", e.Value.TradeID);
                    Log.log(string.Format("OnRtnTrade:{0} OrderID {1}", e.Value.TradeID, e.Value.OrderID), e.Value.InstrumentID);
                    //成交 需要下委托单
                    string direction = e.Value.Direction == DirectionType.Buy ? "Buy" : "Sell";
                    string offsetType = " Open";
                    if (e.Value.Offset == OffsetType.Close)
                        offsetType = " Close";
                    else if (e.Value.Offset == OffsetType.CloseToday)
                        offsetType = " CloseToday";

                    string info = string.Format("user[{6}] -- {0},{1},{2},{3},{4},{5}", e.Value.InstrumentID, e.Value.TradingDay, e.Value.TradeTime,
                        e.Value.Price, e.Value.Volume, direction + offsetType, trader.Investor);
                    Log.logTrade(info);
                    MailService.Notify(LogTitle + " [成交回报]", info);

                    //HttpHelper.HttpPostToWechat(info);

                    OrderField orderField = null;
                    if (tradeCenter._tradeOrders.TryGetValue(e.Value.OrderID, out orderField))
                    {
                        tradeCenter._tradeOrders.Remove(e.Value.OrderID);
                    }
                    HashSet<string> waitSecond = null;
                    if (_waitingForOp.TryGetValue(e.Value.InstrumentID, out waitSecond))
                    {
                        _waitingForOp.Remove(e.Value.InstrumentID);
                    }
                    trader.ReqQryPosition();

                    //if (withoutRedis == false && program.redisSubscriber != null)
                    if (heartBeatService.isAvailable())
                    {
                        Deal deal = new Deal();
                        deal.user = trader.Investor;
                        deal.instrument = e.Value.InstrumentID;
                        deal.price = Convert.ToString(e.Value.Price);
                        deal.time = e.Value.TradingDay + " " + e.Value.TradeTime;
                        deal.direction = direction + offsetType;
                        deal.holders = Convert.ToString(e.Value.Volume);
                        heartBeatService.publishInfo("deal", JsonConvert.SerializeObject(deal));
                    }
                }
            };
        }


        private void start()
        {
            trader.ReqConnect();
            Thread.Sleep(3000);

            if (!trader.IsLogin && (Utils.isLogInTimeNow() || Utils.isTradingTimeNow()))
            {
                Console.WriteLine("login 失败，交易时段重试");
                //goto R;
            }

            tradeCenter = new TradeCenter(trader, quoter, _tradeQueue);
            tradeCenter.start();

            InstrumentWatcher.Init(trader);
            LoginWatcher.Init(this);
            Console.WriteLine(trader.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\t" + n.Value.InstrumentID));

            syncData();
            isInit = true;
            heartBeatService = new HeartBeatService(trader.Investor, false);
            heartBeatService.startService();
        }

        private void loop()
        {
        //MailService.Notify(LogTitle + " [启动]", trader.Investor + " 启动");

        Inst:

            Console.WriteLine(Program.LogTitle + "q:退出  1-BK  2-SP  3-SK  4-BP  5-撤单");
            Console.WriteLine("a-交易所状态  b-委托  c-成交  d-持仓  e-合约  f-权益 g-换合约 h-平所有仓位 s-立刻保存 t-当前值");
            DirectionType dire = DirectionType.Buy;
            OffsetType offset = OffsetType.Open;
            char c = Console.ReadKey().KeyChar;
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
                    trader.ReqOrderAction(_orderId);
                    break;
                case 'a':
                    Console.WriteLine(trader.DicExcStatus.Aggregate("\r\n交易所状态", (cur, n) => cur + "\r\n" + n.Key + "=>" + n.Value));
                    break;
                case 'b':
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(trader.DicOrderField.Aggregate("\r\n委托", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v)
                        => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'c':
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(trader.DicTradeField.Aggregate("\r\n成交", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'd': //持仓
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine(trader.DicPositionField.Aggregate("\r\n持仓", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    trader.ReqQryPosition();
                    break;
                case 'e':
                    Console.WriteLine(trader.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'f':
                    Console.WriteLine(trader.TradingAccount.GetType().GetFields().Aggregate("\r\n权益\t", (cur, n) => cur + ","
                        + n.GetValue(trader.TradingAccount).ToString()));
                    break;
                case 'g':
                    Console.WriteLine(Program.LogTitle + "请输入合约:");
                    string inst = Console.ReadLine();
                    quoter.ReqSubscribeMarketData(inst);
                    break;
                case 'h':
                    Console.WriteLine("\r\n！！！输入y确认平所有仓位！！！:");
                    char op = Console.ReadKey().KeyChar;
                    if (op == 'y' || op == 'Y')
                    {
                        Console.WriteLine("\r\n" + Program.LogTitle + "！！！正在平所有仓位！！！");
                        closeAllPosition();
                        Console.WriteLine("\r\n" + Program.LogTitle + "！！！已下单平所有可平仓位！！！");

                    }
                    else
                        Console.WriteLine("\r\n" + Program.LogTitle + "放弃平仓");
                    break;
                case 's':
                    syncData();
                    break;
                case 't':
                    foreach (string key in tradeData.Keys)
                    {
                        InstrumentData currentInstrumentdata;
                        if (tradeData.TryGetValue(key, out currentInstrumentdata) == false)
                        {
                            if (currentInstrumentdata == null)
                                continue;
                            Log.log(string.Format("品种:{0} 值:{1}", key, currentInstrumentdata.curAvg));

                        }


                    }
                    break;
                case 'q':
                    if (trader.IsLogin)
                    {
                        quoter.ReqUserLogout();
                        trader.ReqUserLogout();
                    }

                    tradeCenter.stop();
                    tradeCenter = null;
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
        }

        public void startService()
        {
            init();
            start();
            loop();

        }
        //输入：q1ctp /t1ctp /q2xspeed /t2speed
        //private static void Main(string[] args)
        //public static void startService2()
        //{
        //    HttpHelper.isHoliday();
        //    Program program = new Program();
        //    System.Object lockThis = new System.Object();
        ////bool isInit = true;
        //    Console.WriteLine(Program.LogTitle + "CTP接口:\t启动中.....");
        //    program.initTrader();
        //    program.initQuoter();
            
        //    Config config = Config.loadConfig();
        //    if (config == null)
        //    {
        //        Console.WriteLine("请输入帐号:");
        //        program.trader.Investor = Console.ReadLine();
        //        Console.WriteLine("请输入密码:");
        //        program.trader.Password = Console.ReadLine();
        //    }
        //    else
        //    {
        //        program.trader.Investor = program.quoter.Investor = config.user;
        //        program.trader.Password = program.quoter.Password = config.password;
        //    }

        //    program.quoter.OnFrontConnected += (sender, e) =>
        //    {
        //        Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnFrontConnected");
        //        Log.log("OnFrontConnected");
        //        //HttpHelper.HttpPostToWechat(program.trader.Investor + " OnFrontConnected");
        //        if (Utils.isTradingTimeNow() || Utils.isLogInTimeNow())
        //            program.quoter.ReqUserLogin();
        //    };
        //    program.quoter.OnRspUserLogin += (sender, e) =>
        //    {
        //        Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogin:{0}", e.Value);
        //        Log.log(string.Format("OnRspUserLogin:{0}", e.Value));
        //    };
        //    program.quoter.OnRspUserLogout += (sender, e) =>
        //    {
        //        Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogout:{0}", e.Value);
        //        Log.log(string.Format("OnRspUserLogout:{0}", e.Value));
        //    };
        //    program.quoter.OnRtnError += (sender, e) =>
        //    {
        //        Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
        //        Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
        //    };
        //    program.quoter.OnRtnTick += (sender, e) =>
        //    {
        //        lock (lockThis)
        //        {
        //            if (program.isInit == false)
        //                return;

        //            bool needUpdate = false;

        //            InstrumentData currentInstrumentdata;

        //            if (program.tradeData.TryGetValue(e.Tick.InstrumentID, out currentInstrumentdata) == false)
        //            {
        //                currentInstrumentdata = new InstrumentData();
        //                program.tradeData.Add(e.Tick.InstrumentID, currentInstrumentdata);
        //            }

        //            List<UnitData> unitDataList;
        //            if (program.unitDataMap.TryGetValue(e.Tick.InstrumentID, out unitDataList) == false)
        //                return;

        //            DateTime d1 = DateTime.ParseExact(program.quoter.TradingDay + " " + e.Tick.UpdateTime, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);

        //            if (Utils.isValidData(e.Tick.InstrumentID, d1, e.Tick.UpdateTime) == false)
        //            {
        //                return;
        //            }

        //            if (Utils.isTradingTime(e.Tick.InstrumentID, d1) == false)
        //                return;

        //            InstrumentWatcher.updateTime(e.Tick.InstrumentID, d1);
        //            //Console.WriteLine(string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2}", e.Tick.InstrumentID,
        //            //       e.Tick.UpdateTime, e.Tick.LastPrice));

        //            if (Utils.isNewBar(currentInstrumentdata.lastUpdateTime, d1, e.Tick.InstrumentID))
        //            {
        //                int count = unitDataList.Count;
        //                if (count > 0)
        //                {
        //                    UnitData lastUnitData = unitDataList.Last();

        //                    if (count > DataService.TOTALSIZE)
        //                    {
        //                        double total = 0;
        //                        for (int i = 0; i < DataService.TOTALSIZE; i++)
        //                        {
        //                            total += unitDataList.ElementAt(count - i - 1).close;
        //                        }
        //                        lastUnitData.avg_480 = Math.Round(total / DataService.TOTALSIZE, 2);

        //                        if (program.heartBeatService.isAvailable())
        //                        {
        //                            TimeBarInfo timeBarInfo = new TimeBarInfo();
        //                            timeBarInfo.user = program.trader.Investor;
        //                            timeBarInfo.instrument = e.Tick.InstrumentID;
        //                            timeBarInfo.price = Convert.ToString(lastUnitData.avg_480);
        //                            timeBarInfo.time = program.quoter.TradingDay + " " + e.Tick.UpdateTime;
        //                            program.heartBeatService.publishInfo("timebarinfo", JsonConvert.SerializeObject(timeBarInfo));
        //                        }

        //                    }
        //                    currentInstrumentdata.curAvg = lastUnitData.avg_480;
        //                    Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 平均:{3}", e.Tick.InstrumentID,
        //                  e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg), e.Tick.InstrumentID);
        //                    needUpdate = true;
        //                }
        //                UnitData unitData = new UnitData();
        //                unitData.high = unitData.low = unitData.open = unitData.close = e.Tick.LastPrice;
        //                unitData.datetime = d1.ToString();
        //                unitDataList.Add(unitData);

        //                Console.WriteLine(string.Format(Program.LogTitle + "new bar 品种{0} 时间:{1} 当前价格:{2}", e.Tick.InstrumentID,
        //                   e.Tick.UpdateTime, e.Tick.LastPrice));

        //            }
        //            else if (unitDataList.Count > 0)
        //            {
        //                UnitData unitData = unitDataList.Last();

        //                if (e.Tick.LastPrice > unitData.high)
        //                {
        //                    unitData.high = e.Tick.LastPrice;
        //                }

        //                if (e.Tick.LastPrice < unitData.low)
        //                {
        //                    unitData.low = e.Tick.LastPrice;
        //                }
        //                unitData.close = e.Tick.LastPrice;

        //            }


        //            currentInstrumentdata.lastUpdateTime = d1.ToString();
        //            InstrumentData instrumentData;
        //            if (program.tradeData.TryGetValue(e.Tick.InstrumentID, out instrumentData) == false)
        //            {
        //                Log.log(string.Format(Program.LogTitle + "品种{0} 不在列表中", e.Tick.InstrumentID), e.Tick.InstrumentID);
        //                return;
        //            }
        //            if (instrumentData == null)
        //            {
        //                Log.log(string.Format(Program.LogTitle + "品种{0} 不在列表中", e.Tick.InstrumentID), e.Tick.InstrumentID);
        //                return;
        //            }

        //            if (instrumentData.trade == false)
        //            {
        //                return;
        //            }

        //            if (currentInstrumentdata.curAvg == 0)
        //            {
        //                return;
        //            }

        //            int pos = instrumentData.closevolumn;
        //            PositionField posField;
        //            if (program.trader.DicPositionField.TryGetValue(e.Tick.InstrumentID, out posField))
        //            {
        //                pos = posField.TdPosition;
        //            }

        //            if (e.Tick.LastPrice > currentInstrumentdata.curAvg && currentInstrumentdata.holder == -1)
        //            {
        //                //close sell
        //                if (currentInstrumentdata.isToday)
        //                {
        //                    operatorInstrument(TradeCenter.BUY_CLOSETODAY, e.Tick.InstrumentID, 0, pos);
        //                }
        //                else
        //                {
        //                    operatorInstrument(TradeCenter.BUY_CLOSE, e.Tick.InstrumentID, 0, pos);
        //                }
        //                currentInstrumentdata.holder = 0;
        //                currentInstrumentdata.isToday = true;
        //                currentInstrumentdata.price = e.Tick.LastPrice;
        //                needUpdate = true;
        //                string info = string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 突破 平均:{3} 平仓:{4}", e.Tick.InstrumentID,
        //                 e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, pos);
        //                Log.log(info, e.Tick.InstrumentID);

        //                info = string.Format("user:[{5}] -- {0} :{1} price:{2} break avg:{3} close:{4}", e.Tick.InstrumentID,
        //                e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, pos, program.trader.Investor);
        //                //HttpHelper.HttpPostToWechat(info);
        //                MailService.Notify(LogTitle + " [Info]", info);


        //            }
        //            else if (e.Tick.LastPrice < currentInstrumentdata.curAvg && currentInstrumentdata.holder == 1)
        //            {
        //                //close buy 
        //                if (currentInstrumentdata.isToday)
        //                {
        //                    operatorInstrument(TradeCenter.SELL_CLOSETODAY, e.Tick.InstrumentID, 0, pos);
                            
        //                }
        //                else
        //                {
        //                    operatorInstrument(TradeCenter.SELL_CLOSE, e.Tick.InstrumentID, 0, pos);
        //                }
        //                currentInstrumentdata.holder = 0;
        //                currentInstrumentdata.isToday = true;
        //                currentInstrumentdata.price = e.Tick.LastPrice;
        //                needUpdate = true;

        //                string info = string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 突破 平均:{3} 平仓:{4}", e.Tick.InstrumentID,
        //                 e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, pos);
        //                Log.log(info, e.Tick.InstrumentID);

        //                info = string.Format("user:[{5}] -- {0} :{1} price:{2} break avg:{3} close:{4}", e.Tick.InstrumentID,
        //                 e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, pos, program.trader.Investor);
        //                //HttpHelper.HttpPostToWechat(info);
        //                MailService.Notify(LogTitle + " [Info]", info);

        //            }


        //            if (e.Tick.LastPrice > currentInstrumentdata.curAvg + e.Tick.LastPrice * instrumentData.span)
        //            {
        //                //Console.WriteLine("品种{0} 时间:{1} 触发新高:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
        //                //no trade before
        //                if (currentInstrumentdata.holder == 0)
        //                {
        //                    //open buy
        //                    //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
        //                    operatorInstrument(TradeCenter.BUY_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice, instrumentData.openvolumn);
        //                    currentInstrumentdata.holder = 1;
        //                    currentInstrumentdata.isToday = true;
        //                    currentInstrumentdata.price = e.Tick.LastPrice;
        //                    needUpdate = true;
        //                    string info = string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 突破 平均:{3}+span:{4} 仓位:{5}", e.Tick.InstrumentID,
        //                 e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, e.Tick.LastPrice * instrumentData.span, instrumentData.openvolumn);
        //                    Log.log(info, e.Tick.InstrumentID);

        //                    info = string.Format("user:[{6}] -- {0} :{1} price:{2} break avg:{3}+span:{4} open:{5}", e.Tick.InstrumentID,
        //                 e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, e.Tick.LastPrice * instrumentData.span, instrumentData.openvolumn, program.trader.Investor, program.trader.Investor);
        //                    //HttpHelper.HttpPostToWechat(info);
        //                    MailService.Notify(LogTitle + " [Info]", info);

        //                }

        //            }

        //            else if (e.Tick.LastPrice < currentInstrumentdata.curAvg - e.Tick.LastPrice * instrumentData.span)
        //            {
        //                //Console.WriteLine("品种{0} 时间:{1} 触发新低:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);

        //                //no trade before
        //                if (currentInstrumentdata.holder == 0)
        //                {
        //                    //open sell
        //                    //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
        //                    operatorInstrument(TradeCenter.SELL_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice, instrumentData.openvolumn);
        //                    currentInstrumentdata.holder = -1;
        //                    currentInstrumentdata.isToday = true;
        //                    currentInstrumentdata.price = e.Tick.LastPrice;
        //                    needUpdate = true;
        //                    string info = string.Format(Program.LogTitle + "品种{0} 时间:{1} 当前价格:{2} 突破 平均:{3}-span:{4} 仓位:{5}", e.Tick.InstrumentID,
        //                e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, e.Tick.LastPrice * instrumentData.span, instrumentData.openvolumn);
        //                    Log.log(info, e.Tick.InstrumentID);
        //                    info = string.Format("user:[{6}] -- {0} :{1} price:{2} break avg:{3}-span:{4} open:{5}", e.Tick.InstrumentID,
        //                e.Tick.UpdateTime, e.Tick.LastPrice, currentInstrumentdata.curAvg, e.Tick.LastPrice * instrumentData.span, instrumentData.openvolumn, program.trader.Investor);
        //                    //HttpHelper.HttpPostToWechat(info);
        //                    MailService.Notify(LogTitle + " [Info]", info);
        //                }
                        
        //            }

        //            if (needUpdate)
        //            {
        //                string fileNameSerialize = FileUtil.getTradeFilePath();
        //                string jsonString = JsonConvert.SerializeObject(program.tradeData);

        //                File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
        //            }
        //        }
        //    };
        //    program.trader.OnFrontConnected += (sender, e) =>
        //    {
        //        if (Utils.isTradingTimeNow() || Utils.isLogInTimeNow())
        //            program.trader.ReqUserLogin();

        //    };
        //    program.trader.OnRspUserLogin += (sender, e) =>
        //    {
        //        Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogin:{0}", e.Value);
        //        Log.log(string.Format("OnRspUserLogin:{0}", e.Value));
        //        MailService.Notify(LogTitle + " [启动]", program.trader.Investor + " 登录成功");

        //        if (e.Value == 0)
        //            program.quoter.ReqConnect();
        //    };
        //    program.trader.OnRspUserLogout += (sender, e) =>
        //    {
        //        Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRspUserLogout:{0}", e.Value);
        //        Log.log(string.Format("OnRspUserLogout:{0}", e.Value));
        //    };
        //    program.trader.OnRtnCancel += (sender, e) =>
        //    {
        //        Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnCancel:{0}", e.Value.OrderID);
        //        Log.log(string.Format("OnRtnCancel:{0}", e.Value), e.Value.InstrumentID);
        //        OrderField orderField = null;
        //        if (program.tradeCenter._tradeOrders.TryGetValue(e.Value.OrderID, out orderField))
        //        {
        //            program.tradeCenter._tradeOrders.Remove(e.Value.OrderID);
        //        }
        //        program.tradeCenter._removingOrders.Remove(e.Value.OrderID);
        //    };
        //    program.trader.OnRtnError += (sender, e) =>
        //    {
        //        Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
        //        Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
        //    };
        //    program.trader.OnRtnExchangeStatus += (sender, e) =>
        //    {
        //        Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnExchangeStatus:{0}=>{1}", e.Exchange, e.Status);
        //        Log.log(string.Format("OnRtnExchangeStatus:{0}=>{1}", e.Exchange, e.Status));

        //        if (e.Status == ExchangeStatusType.Closed || e.Status == ExchangeStatusType.NoTrading)
        //        {
                  
        //        }

        //    };
        //    program.trader.OnRtnNotice += (sender, e) =>
        //    {
        //        Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnNotice:{0}", e.Value);
        //        Log.log(string.Format("OnRtnNotice:{0}", e.Value));
        //    };
        //    program.trader.OnRtnOrder += (sender, e) =>
        //    {
        //        Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnOrder:{0}", e.Value.OrderID);
        //        string info = string.Format("OnRtnOrder:{0} {1}", e.Value.OrderID, e.Value.LimitPrice);
        //        Log.log(info, e.Value.InstrumentID);
        //        _orderId = e.Value.OrderID;
        //        if (program.tradeCenter._tradeOrders.ContainsKey(_orderId))
        //        {
        //            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnOrder:{0} _tradeOrders exists");
        //            program.tradeCenter._tradeOrders[_orderId] = e.Value;
        //        }
        //        else
        //            program.tradeCenter._tradeOrders.Add(_orderId, e.Value);

        //        DateTime d1 = DateTime.Parse(e.Value.InsertTime);
        //        HashSet<string> waitSecond = null;
        //        if (program._waitingForOp.TryGetValue(e.Value.InstrumentID, out waitSecond))
        //        {
        //            program._waitingForOp.Remove(e.Value.InstrumentID);
        //        }
        //        else
        //        {
        //            waitSecond = new HashSet<string>();
        //            waitSecond.Add(d1.ToString("yyyy-MM-dd-HH-mm"));
        //            program._waitingForOp.Add(e.Value.InstrumentID, waitSecond);
        //        }
        //    };
        //    program.trader.OnRtnTrade += (sender, e) =>
        //    {
        //        lock (lockThis)
        //        {
        //            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnTrade:{0}", e.Value.TradeID);
        //            Log.log(string.Format("OnRtnTrade:{0} OrderID {1}", e.Value.TradeID, e.Value.OrderID), e.Value.InstrumentID);
        //            //成交 需要下委托单
        //            string direction = e.Value.Direction == DirectionType.Buy ? "Buy" : "Sell";
        //            string offsetType = " Open";
        //            if (e.Value.Offset == OffsetType.Close)
        //                offsetType = " Close";
        //            else if (e.Value.Offset == OffsetType.CloseToday)
        //                offsetType = " CloseToday";

        //            string info = string.Format("user[{6}] -- {0},{1},{2},{3},{4},{5}", e.Value.InstrumentID, e.Value.TradingDay, e.Value.TradeTime,
        //                e.Value.Price, e.Value.Volume, direction + offsetType, program.trader.Investor);
        //            Log.logTrade(info);
        //            MailService.Notify(LogTitle + " [成交回报]", info);

        //            //HttpHelper.HttpPostToWechat(info);

        //            OrderField orderField = null;
        //            if (program.tradeCenter._tradeOrders.TryGetValue(e.Value.OrderID, out orderField))
        //            {
        //                program.tradeCenter._tradeOrders.Remove(e.Value.OrderID);
        //            }
        //            HashSet<string> waitSecond = null;
        //            if (program._waitingForOp.TryGetValue(e.Value.InstrumentID, out waitSecond))
        //            {
        //                program._waitingForOp.Remove(e.Value.InstrumentID);
        //            }
        //            program.trader.ReqQryPosition();

        //            //if (withoutRedis == false && program.redisSubscriber != null)
        //            if(program.heartBeatService.isAvailable())
        //            {
        //                Deal deal = new Deal();
        //                deal.user = program.trader.Investor;
        //                deal.instrument = e.Value.InstrumentID;
        //                deal.price = Convert.ToString(e.Value.Price);
        //                deal.time = e.Value.TradingDay + " " + e.Value.TradeTime;
        //                deal.direction = direction + offsetType;
        //                deal.holders = Convert.ToString(e.Value.Volume);
        //                program.heartBeatService.publishInfo("deal", JsonConvert.SerializeObject(deal));
        //            }
        //        }
        //    };

        //    program.trader.ReqConnect();
        //    Thread.Sleep(3000);

        //    if (!program.trader.IsLogin && (Utils.isLogInTimeNow() || Utils.isTradingTimeNow()))
        //    {
        //        Console.WriteLine("login 失败，交易时段重试");
        //        //goto R;
        //    }

        //    program.tradeCenter = new TradeCenter(program.trader, program.quoter, _tradeQueue);
        //    program.tradeCenter.start();

        //    InstrumentWatcher.Init(program.trader);
        //    LoginWatcher.Init(program);
        //    Console.WriteLine(program.trader.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\t" + n.Value.InstrumentID));

        //    program.syncData();
        //    program.isInit = true;
        //    program.heartBeatService = new HeartBeatService(program.trader.Investor,false);
        //    program.heartBeatService.startService();

        //    Inst:
        //    MailService.Notify(LogTitle + " [启动]", program.trader.Investor + " 启动");

        //    Console.WriteLine(Program.LogTitle + "q:退出  1-BK  2-SP  3-SK  4-BP  5-撤单");
        //    Console.WriteLine("a-交易所状态  b-委托  c-成交  d-持仓  e-合约  f-权益 g-换合约 h-平所有仓位 s-立刻保存 t-当前值");
        //    DirectionType dire = DirectionType.Buy;
        //    OffsetType offset = OffsetType.Open;
        //    char c = Console.ReadKey().KeyChar;
        //    switch (c)
        //    {   
        //        case '1':
        //            dire = DirectionType.Buy;
        //            offset = OffsetType.Open;
        //            break;
        //        case '2':
        //            dire = DirectionType.Sell;
        //            offset = OffsetType.CloseToday;
        //            break;
        //        case '3':
        //            dire = DirectionType.Sell;
        //            offset = OffsetType.Open;
        //            break;
        //        case '4':
        //            dire = DirectionType.Buy;
        //            offset = OffsetType.CloseToday;
        //            break;
        //        case '5':
        //            program.trader.ReqOrderAction(_orderId);
        //            break;
        //        case 'a':
        //            Console.WriteLine(program.trader.DicExcStatus.Aggregate("\r\n交易所状态", (cur, n) => cur + "\r\n" + n.Key + "=>" + n.Value));
        //            break;
        //        case 'b':
        //            Console.ForegroundColor = ConsoleColor.Cyan;
        //            Console.WriteLine(program.trader.DicOrderField.Aggregate("\r\n委托", (cur, n) => cur + "\r\n"
        //                + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v)
        //                => f + string.Format("{0,12}", v.GetValue(n.Value)))));
        //            break;
        //        case 'c':
        //            Console.ForegroundColor = ConsoleColor.DarkCyan;
        //            Console.WriteLine(program.trader.DicTradeField.Aggregate("\r\n成交", (cur, n) => cur + "\r\n"
        //                + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
        //            break;
        //        case 'd': //持仓
        //            Console.ForegroundColor = ConsoleColor.DarkGreen;
        //            Console.WriteLine(program.trader.DicPositionField.Aggregate("\r\n持仓", (cur, n) => cur + "\r\n"
        //                + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
        //            program.trader.ReqQryPosition();
        //            break;
        //        case 'e':
        //            Console.WriteLine(program.trader.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\r\n"
        //                + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
        //            break;
        //        case 'f':
        //            Console.WriteLine(program.trader.TradingAccount.GetType().GetFields().Aggregate("\r\n权益\t", (cur, n) => cur + ","
        //                + n.GetValue(program.trader.TradingAccount).ToString()));
        //            break;
        //        case 'g':
        //            Console.WriteLine(Program.LogTitle + "请输入合约:");
        //            string inst = Console.ReadLine();
        //            program.quoter.ReqSubscribeMarketData(inst);
        //            break;
        //        case 'h':
        //            Console.WriteLine("\r\n！！！输入y确认平所有仓位！！！:");
        //            char op = Console.ReadKey().KeyChar;
        //            if (op == 'y'||op=='Y')
        //            { 
        //                Console.WriteLine("\r\n"+Program.LogTitle + "！！！正在平所有仓位！！！");
        //                program.closeAllPosition();
        //                Console.WriteLine("\r\n" + Program.LogTitle + "！！！已下单平所有可平仓位！！！");

        //            }
        //            else
        //                Console.WriteLine("\r\n" + Program.LogTitle + "放弃平仓");
        //            break;
        //        case 's':
        //            program.syncData();
        //            break;
        //        case 't':
        //            foreach (string key in program.tradeData.Keys)
        //            {
        //                InstrumentData currentInstrumentdata;
        //                if (program.tradeData.TryGetValue(key, out currentInstrumentdata) == false)
        //                {
        //                    if (currentInstrumentdata == null)
        //                        continue;
        //                    Log.log(string.Format("品种:{0} 值:{1}", key,currentInstrumentdata.curAvg));

        //                }


        //            }
        //            break;
        //        case 'q':
        //            if(program.trader.IsLogin)
        //            { 
        //                program.quoter.ReqUserLogout();
        //                program.trader.ReqUserLogout();
        //            }
                    
        //            program.tradeCenter.stop();
        //            program.tradeCenter = null;
        //            InstrumentWatcher.flag = false;
        //            Thread.Sleep(2000); //待接口处理后续操作
        //            Environment.Exit(0);
        //            break;
        //    }
        //    if (c >= '1' && c <= '4')
        //    {
        //        Console.WriteLine(Program.LogTitle + "请选择委托类型: 1-限价  2-市价  3-FAK  4-FOK");
        //        OrderType ot = OrderType.Limit;
        //        switch (Console.ReadKey().KeyChar)
        //        {
        //            case '2':
        //                ot = OrderType.Market;
        //                break;
        //            case '3':
        //                ot = OrderType.FAK;
        //                break;
        //            case '4':
        //                ot = OrderType.FOK;
        //                break;
        //        }
               
        //    }
        //    goto Inst;
        //    End:
        //        Console.WriteLine(Program.LogTitle + " end");
        //}
        
    }
}
