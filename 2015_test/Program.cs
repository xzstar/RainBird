using System;
using System.Linq;
using System.Threading;
using Quote2015;
using Trade2015;
using System.Collections.Generic;

namespace ConsoleProxy
{
   class Data
    {
        double high;
        double low;
    }


    class Program
    {
        static private string _inst;
        private static int _TOTALSIZE = 3;

        private static double _LastPrice = double.NaN;

        private static int _orderId;
        private static int BUY_OPEN = 1;
        private static int SELL_CLOSETODAY = 2;
        private static int SELL_OPEN = 3;
        private static int BUY_CLOSETODAY = 4;

        private static void operatord(Trade t, Quote q, int op, string inst)
        {
            Console.WriteLine("操作start:{0}: {1}", op, inst);
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
                //case '5':
                //    t.ReqOrderAction(_orderId);
                //    break;
                //case 'a':
                //    Console.WriteLine(t.DicExcStatus.Aggregate("\r\n交易所状态", (cur, n) => cur + "\r\n" + n.Key + "=>" + n.Value));
                //    break;
                //case 'b':
                //    Console.ForegroundColor = ConsoleColor.Cyan;
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
            if (op >= 1 && op <= 4)
            {
                Console.WriteLine("操作:{0}: {1}", operation, inst);

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
                            Console.WriteLine("委托标识:" + _orderId);
                            break;
                        }
                    }
                }
            }
        }


        //输入：q1ctp /t1ctp /q2xspeed /t2speed
        private static void Main(string[] args)
        {
            Trade t;
            Quote q;
            string inst = string.Empty;
            System.Object lockThis = new System.Object();


            LinkedList<double> _highList = new LinkedList<double>();
            LinkedList<double> _lowList = new LinkedList<double>();
            double highest = 0;
            double lowest = 1000000;
            int lastMin = -1;
            int holder = 0;
        R:
            Console.WriteLine("选择接口:\t1-CTP  2-xSpeed  3-Femas  4-股指仿真  5-外汇仿真  6-郑商商品期权仿真");
            char c = '1';// Console.ReadKey(true).KeyChar;

            switch (c)
            {
                case '1': //CTP
                    t = new Trade("ctp_trade_proxy.dll")
                    {
                        Server = "tcp://180.168.146.187:10000",//" tcp://101.95.8.178:51205", 
                        Broker = "9999"// "4040",
                    };
                    q = new Quote("ctp_quote_proxy.dll")
                    {
                        Server = "tcp://101.95.8.178:51213",
                        Broker = "4040",
                    };
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
                    t = new Trade("xSpeed_trade_proxy.dll")
                    {
                        Server = "tcp://203.187.171.250:10910",
                        Broker = "0001",
                    };
                    q = new Quote("xSpeed_quote_proxy.dll")
                    {
                        Server = "tcp://203.187.171.250:10915",
                        Broker = "0001",
                    };
                    break;
                case '3': //femas
                    t = new Trade("femas_trade_proxy.dll")
                    {
                        Server = "tcp://116.228.53.149:6666",
                        Broker = "0001",
                    };
                    q = new Quote("femas_quote_proxy.dll")
                    {
                        Server = "tcp://116.228.53.149:6888",
                        Broker = "0001",
                    };
                    break;
                case '4': //CTP
                    t = new Trade("ctp_trade_proxy.dll")
                    {
                        Server = "tcp://124.207.185.88:41205",
                        Broker = "1010",
                    };
                    q = new Quote("ctp_quote_proxy.dll")
                    {
                        Server = "tcp://124.207.185.88:41213",
                        Broker = "1010",
                    };
                    break;
                case '5': //CTP
                    t = new Trade("femas_trade_proxy.dll")
                    {
                        Server = "tcp://117.184.207.111:7036",
                        Broker = "2713",
                    };
                    q = new Quote("femas_quote_proxy.dll")
                    {
                        Server = "tcp://117.184.207.111:7230",
                        Broker = "2713",
                    };
                    break;
                case '6': //CTP
                    t = new Trade("ctp_trade_proxy.dll")
                    {
                        Server = "tcp://106.39.36.72:51205",
                        Broker = "1010",
                    };
                    q = new Quote("ctp_quote_proxy.dll")
                    {
                        Server = "tcp://106.39.36.72:51213",
                        Broker = "1010",
                    };
                    break;
                default:
                    Console.WriteLine("请重新选择");
                    goto R;
            }
            //Console.WriteLine("请输入帐号:");
            t.Investor = q.Investor = "043213"; //Console.ReadLine();
                                                //Console.WriteLine("请输入密码:");
            t.Password = q.Password = "xiezhestar";// Console.ReadLine();

            q.OnFrontConnected += (sender, e) =>
            {
                Console.WriteLine("OnFrontConnected");
                q.ReqUserLogin();
            };
            q.OnRspUserLogin += (sender, e) => Console.WriteLine("OnRspUserLogin:{0}", e.Value);
            q.OnRspUserLogout += (sender, e) => Console.WriteLine("OnRspUserLogout:{0}", e.Value);
            q.OnRtnError += (sender, e) => Console.WriteLine("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
            q.OnRtnTick += (sender, e) =>
            {
                lock (lockThis)
                {
                    Boolean isTriggerLow = false;
                    Boolean isTriggerHigh = false;
                    //Console.WriteLine("OnRtnTick:{0}", e.Tick.LastPrice);
                    //Console.WriteLine("OnRtnTick:{0}=>{1}=>{2}", e.Tick.UpdateTime, e.Tick.AskPrice, e.Tick.InstrumentID);
                    DateTime d1 = DateTime.Parse(e.Tick.UpdateTime);
                    if (lastMin != d1.Minute /*&& d1.Minute%3 == 0*/)
                    {
                        Console.WriteLine("OnRtnTick 目前记录数:{0}", _highList.Count);
                        _highList.AddLast(e.Tick.LastPrice);
                        if (_highList.Count > _TOTALSIZE)
                            _highList.RemoveFirst();

                        _lowList.AddLast(e.Tick.LastPrice);
                        if (_lowList.Count > _TOTALSIZE)
                            _lowList.RemoveFirst();
                        isTriggerHigh = true;
                        isTriggerLow = true;

                        highest = 0;
                        lowest = 1000000;
                        foreach (double data in _highList)
                        {
                            Console.WriteLine(" 最高:{0} 当前k线:{1}", highest, data);

                            if (data > highest)
                                highest = data;

                        }

                        foreach (double data in _lowList)
                        {
                            Console.WriteLine(" 最低:{0},当前k线:{1}", lowest, data);
                            if (data < lowest)
                                lowest = data;
                        }
                        Console.WriteLine("新K线 最高:{0} 最低:{1}", highest, lowest);
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

                    lastMin = d1.Minute;

                    if (isTriggerHigh)
                    {
                        Console.WriteLine("OnRtnTick:{0} 新高 {1}", e.Tick.UpdateTime, e.Tick.LastPrice);

                        // more than _totoalSize , can trade now
                        if (_highList.Count >= _TOTALSIZE && e.Tick.LastPrice > highest)
                        {
                            //no trade before
                            if (holder == 0)
                            {
                                //open buy
                                operatord(t, q, BUY_OPEN, inst);
                                holder = 1;
                            }
                            else if (holder == -1)
                            {
                                //close sell and open buy
                                operatord(t, q, BUY_CLOSETODAY, inst);

                                operatord(t, q, BUY_OPEN, inst);
                                holder = 1;
                            }
                        }


                    }

                    if (isTriggerLow)
                    {
                        Console.WriteLine("OnRtnTick:{0} 新低 {1}", e.Tick.UpdateTime, e.Tick.LastPrice);
                        // more than _totoalSize , can trade now
                        if (_lowList.Count >= _TOTALSIZE && e.Tick.LastPrice < lowest)
                        {
                            //no trade before
                            if (holder == 0)
                            {
                                //open sell
                                operatord(t, q, SELL_OPEN, inst);
                                holder = -1;
                            }
                            else if (holder == 1)
                            {
                                //close buy and open sell
                                operatord(t, q, SELL_CLOSETODAY, inst);

                                operatord(t, q, SELL_OPEN, inst);
                                holder = -1;

                            }
                        }
                    }
                }
            };

            t.OnFrontConnected += (sender, e) => t.ReqUserLogin();
            t.OnRspUserLogin += (sender, e) =>
            {
                Console.WriteLine("OnRspUserLogin:{0}", e.Value);
                if (e.Value == 0)
                    q.ReqConnect();
            };
            t.OnRspUserLogout += (sender, e) => Console.WriteLine("OnRspUserLogout:{0}", e.Value);
            t.OnRtnCancel += (sender, e) => Console.WriteLine("OnRtnCancel:{0}", e.Value.OrderID);
            t.OnRtnError += (sender, e) => Console.WriteLine("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
            t.OnRtnExchangeStatus += (sender, e) => Console.WriteLine("OnRtnExchangeStatus:{0}=>{1}", e.Exchange, e.Status);
            t.OnRtnNotice += (sender, e) => Console.WriteLine("OnRtnNotice:{0}", e.Value);
            t.OnRtnOrder += (sender, e) =>
            {
                Console.WriteLine("OnRtnOrder:{0}", e.Value);
                _orderId = e.Value.OrderID;
            };
            t.OnRtnTrade += (sender, e) => Console.WriteLine("OnRtnTrade:{0}", e.Value.TradeID);

            t.ReqConnect();
            Thread.Sleep(3000);

            if (!t.IsLogin)
                goto R;
            Console.WriteLine(t.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\t" + n.Value.InstrumentID));

            inst = string.Empty;
            Console.WriteLine("请输入合约:");
            inst = Console.ReadLine();
            q.ReqSubscribeMarketData(inst);

        Inst:
            Console.WriteLine("q:退出  1-BK  2-SP  3-SK  4-BP  5-撤单");
            Console.WriteLine("a-交易所状态  b-委托  c-成交  d-持仓  e-合约  f-权益 g-换合约");

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
                    t.ReqOrderAction(_orderId);
                    break;
                case 'a':
                    Console.WriteLine(t.DicExcStatus.Aggregate("\r\n交易所状态", (cur, n) => cur + "\r\n" + n.Key + "=>" + n.Value));
                    break;
                case 'b':
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(t.DicOrderField.Aggregate("\r\n委托", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v)
                        => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'c':
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(t.DicTradeField.Aggregate("\r\n成交", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'd': //持仓
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine(t.DicPositionField.Aggregate("\r\n持仓", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'e':
                    Console.WriteLine(t.DicInstrumentField.Aggregate("\r\n合约", (cur, n) => cur + "\r\n"
                        + n.Value.GetType().GetFields().Aggregate(string.Empty, (f, v) => f + string.Format("{0,12}", v.GetValue(n.Value)))));
                    break;
                case 'f':
                    Console.WriteLine(t.TradingAccount.GetType().GetFields().Aggregate("\r\n权益\t", (cur, n) => cur + ","
                        + n.GetValue(t.TradingAccount).ToString()));
                    break;
                case 'g':
                    Console.WriteLine("请输入合约:");
                    inst = Console.ReadLine();
                    q.ReqSubscribeMarketData(inst);
                    break;
                case 'q':
                    q.ReqUserLogout();
                    t.ReqUserLogout();
                    Thread.Sleep(2000); //待接口处理后续操作
                    Environment.Exit(0);
                    break;
            }
            if (c >= '1' && c <= '4')
            {
                Console.WriteLine("请选择委托类型: 1-限价  2-市价  3-FAK  4-FOK");
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
                            Console.WriteLine("委托标识:" + _orderId);
                            break;
                        }
                    }
                }
            }
            goto Inst;
        }
    }
}
