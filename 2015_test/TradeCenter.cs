using Quote2015;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trade2015;
using System.Collections.Concurrent;
using System.Data;
using System.IO;

namespace ConsoleProxy
{
    public interface ITradeCenter
    {
        void init(string tag);
        void start();
        OrderField getOpeningOrder(string instrumentId);
        bool isRemovingOrder(int orderId);
        bool removeOrder(int orderId);
        bool operateInstrument(OpType op,string instrumentId, double price,int limitMin);
        InstrumentField getInstrumentField(string instrumentId);
    }

    public enum OpType
    {
        BUY_OPEN=1,
        SELL_CLOSETODAY=2,
        SELL_CLOSE = 21,

        SELL_OPEN = 3,
        BUY_CLOSETODAY = 4,
        BUY_CLOSE = 41,
    }
    //public class TradeCenterTestImp : ITradeCenter
    //{
    //    public Dictionary<int, OrderField> _tradeOrders = new Dictionary<int, OrderField>();
    //    //public HashSet<int> _removingOrders = new HashSet<int>();
    //    private Dictionary<string, HashSet<string>> _waitingOrder = new Dictionary<string, HashSet<string>>();
    //    private Dictionary<string, int> _waitingMin = new Dictionary<string, int>();
    //    private int _orderId = 0;
    //    private string _instrumentId;
    //    private int _holding = 0;
    //    private OrderField _holdingOrder = null;
    //    private double _currentLastPrice = 0;
    //    private string _currentTime = null;

    //    public InstrumentField getInstrumentField(string instrumentId)
    //    {
    //        InstrumentField instrumentField = new InstrumentField();

    //        instrumentField.InstrumentID = instrumentId;
    //        instrumentField.PriceTick = 1;
    //        instrumentField.ProductClass = ProductClassType.Futures;

    //        return instrumentField;
    //    }

    //    public OrderField getOpeningOrder(string instrumentId)
    //    {
    //        foreach(OrderField orderField in _tradeOrders.Values)
    //        {
    //            if (orderField.InstrumentID == instrumentId)
    //                return orderField;
    //        }
    //        return null;
    //    }

    //    public OrderField getOpeningOrder(int orderId)
    //    {
    //        foreach (OrderField orderField in _tradeOrders.Values)
    //        {
    //            if (orderField.OrderID == orderId)
    //                return orderField;
    //        }
    //        return null;
    //    }
    //    private bool hasWaitingOrder(string instrumentId)
    //    {
    //        return _waitingOrder.ContainsKey(instrumentId);
    //    }

    //    private void removeWaitingOrder(string instrumentId)
    //    {
    //        _waitingOrder.Remove(instrumentId);
    //    }

    //    public bool isRemovingOrder(int orderId)
    //    {
    //        return false; // _removingOrders.Contains(orderId);
    //    }

    //    public bool operateInstrument(OpType op, string instrumentId, double price,int limitMin)
    //    {
    //        if (op == OpType.BUY_OPEN || op ==OpType.SELL_OPEN)
    //        {
    //            int orderId = _orderId++;
    //            OrderField orderField = new OrderField();
    //            orderField.OrderID = orderId;
    //            orderField.InstrumentID = instrumentId;
    //            orderField.LimitPrice = price;
    //            if(op == OpType.BUY_OPEN)
    //                orderField.Direction = DirectionType.Buy;
    //            else 
    //                orderField.Direction = DirectionType.Sell;

    //            _tradeOrders.Add(orderId, orderField);

    //            if (limitMin > 0)
    //            {
    //                _waitingOrder.Add(instrumentId, new HashSet<string>());
    //                _waitingMin[instrumentId] = limitMin;
    //            }
    //            else
    //                _waitingMin[_instrumentId] = 0;

    //            StrategyManager.getInstance().onOrder(orderField);
    //        }
    //        else
    //        {
    //            if (_holding != 0)
    //            {
    //                TradeArgs tradeArgs = new TradeArgs();
    //                tradeArgs.Value = new TradeField();
    //                if (_holdingOrder.Direction == DirectionType.Buy)
    //                    tradeArgs.Value.Direction = DirectionType.Sell;
    //                else
    //                    tradeArgs.Value.Direction = DirectionType.Buy;

    //                tradeArgs.Value.InstrumentID = _instrumentId;
    //                tradeArgs.Value.Price = _currentLastPrice;
    //                tradeArgs.Value.TradeTime = _currentTime;
    //                tradeArgs.Value.OrderID = _holdingOrder.OrderID;
    //                tradeArgs.Value.Offset = OffsetType.Close;
    //                StrategyManager.getInstance().onTrade(tradeArgs);
    //                _holding = 0;
    //                _holdingOrder = null;
    //            }
    //        }
    //        return true;
    //    }

    //    public bool removeOrder(int orderId)
    //    {
    //        OrderField order = getOpeningOrder(orderId);
    //        //_removingOrders.Remove(orderId);
    //        _tradeOrders.Remove(orderId);
    //        //StrategyManager.getInstance().onOrderCancel(order);
    //        return true;
    //    }

    //    public void init(string instrumentId)
    //    {
    //        _instrumentId = instrumentId;
    //        BreakStrategy breakStrategy = new BreakStrategy(this);
    //        breakStrategy.mMinSpan = 30;
    //        breakStrategy.mTotalSize = 9;
    //        StrategyManager.getInstance().addStrategy(breakStrategy);

    //    }

    //    private Decimal ChangeDataToD(string strData)
    //    {
    //        Decimal dData = 0.0M;
    //        if (strData.Contains("E"))
    //        {
    //            dData = Convert.ToDecimal(Decimal.Parse(strData.ToString(), System.Globalization.NumberStyles.Float));
    //        }
    //        return dData;
    //    }

    //    public void start()
    //    {
    //        FileStream fs = new FileStream(FileUtil.getTestDataFilePath(_instrumentId+"_Tick.csv"), System.IO.FileMode.Open, System.IO.FileAccess.Read);

    //        //StreamReader sr = new StreamReader(fs, Encoding.UTF8);
    //        StreamReader sr = new StreamReader(fs, Encoding.UTF8);
    //        //string fileContent = sr.ReadToEnd();
    //        //encoding = sr.CurrentEncoding;
    //        //记录每次读取的一行记录
    //        string strLine = "";
    //        //记录每行记录中的各字段内容
    //        string[] aryLine = null;
    //        //标示列数
    //        int columnCount = 0;
    //        //标示是否是读取的第一行
    //        bool IsFirst = true;
    //        //逐行读取CSV中的数据
    //        int count = 0;
    //        while ((strLine = sr.ReadLine()) != null)
    //        {
                
    //            //strLine = Common.ConvertStringUTF8(strLine, encoding);
    //            //strLine = Common.ConvertStringUTF8(strLine);

    //            {
    //                aryLine = strLine.Split(',');
    //                TickEventArgs tick = new TickEventArgs();
    //                tick.Tick = new MarketData();
    //                bool result = double.TryParse(aryLine[8], out tick.Tick.LastPrice);
    //                if (result == false || tick.Tick.LastPrice<=0)
    //                    continue;
    //                _currentLastPrice = tick.Tick.LastPrice;

    //                tick.Tick.InstrumentID = _instrumentId;
    //                string date = aryLine[0].Substring(0, 4) + "-" + aryLine[0].Substring(4, 2) + "-" + aryLine[0].Substring(6);
    //                string time;
    //                string temparyLine1 = aryLine[1];
    //                string temparyLine2 = aryLine[1];
    //                if (temparyLine1.IndexOf('E')>=0)
    //                {
    //                    Decimal dec = ChangeDataToD(temparyLine1);
    //                    temparyLine2 = dec.ToString();
    //                }

    //                temparyLine2 += "00000000";
                   
    //                time = temparyLine2.Substring(2, 2) + ":" + temparyLine2.Substring(4, 2) + ":" + temparyLine2.Substring(6,2);
                       

    //                tick.Tick.UpdateTime = date + " " + time;
    //                _currentTime = tick.Tick.UpdateTime;

    //                OrderField order = getOpeningOrder(_instrumentId);
    //                if (order != null) {
    //                   if ((order.Direction == DirectionType.Buy && 
    //                        order.LimitPrice >= tick.Tick.LastPrice) 
    //                        || (order.Direction == DirectionType.Sell &&
    //                            order.LimitPrice <=tick.Tick.LastPrice))
    //                    {
    //                        TradeArgs tradeArgs = new TradeArgs();
    //                        tradeArgs.Value = new TradeField();
    //                        tradeArgs.Value.Direction = order.Direction;
    //                        tradeArgs.Value.InstrumentID = _instrumentId;
    //                        tradeArgs.Value.Price = tick.Tick.LastPrice;
    //                        tradeArgs.Value.TradeTime = tick.Tick.UpdateTime;
    //                        tradeArgs.Value.OrderID = order.OrderID;
    //                        tradeArgs.Value.Offset = order.Offset;
    //                        StrategyManager.getInstance().onTrade(tradeArgs);
    //                        _holding = 1;
    //                        _holdingOrder = order;
    //                        removeOrder(order.OrderID);
    //                        removeWaitingOrder(_instrumentId);
    //                    }
    //                }
    //                StrategyManager.getInstance().onTick(tick);
    //                if (hasWaitingOrder(_instrumentId))
    //                {

    //                    if(_waitingOrder.ContainsKey(_instrumentId))
    //                    {
    //                        HashSet<string> tickTime = _waitingOrder[_instrumentId];
    //                        tickTime.Add(tick.Tick.UpdateTime);
    //                        if(tickTime.Count == 1 && order !=null)
    //                        {
    //                            order.InsertTime = tick.Tick.UpdateTime;
    //                        }

    //                        if(tickTime.Count > _waitingMin[_instrumentId]*60)
    //                        {
    //                            removeOrder(order.OrderID);
    //                            removeWaitingOrder(_instrumentId);
    //                            order.InsertTime = tick.Tick.UpdateTime;
    //                            StrategyManager.getInstance().onOrderCancel(order);

    //                        }
    //                    }
                        
    //                }

    //            }
    //        }
            

    //        sr.Close();
    //        fs.Close();
    //    }
    //}

    public class TradeItem
    {
        public string mInstrument;
        public int mOperator;
        public double mPrice;
        public int mVolumn;

        public TradeItem(String instrument,int operate)
        {
            mInstrument = instrument;
            mOperator = operate;
            mPrice = 0;
            mVolumn = 1;
        }
        public TradeItem(String instrument, int operate,double price)
        {
            mInstrument = instrument;
            mOperator = operate;
            mPrice = price;
            mVolumn = 1;
        }
        public TradeItem(String instrument, int operate, double price,int volumn)
        {
            mInstrument = instrument;
            mOperator = operate;
            mPrice = price;
            mVolumn = volumn;
        }
    }
    class TradeCenter
    {
        public const int BUY_OPEN = 1;
        public const int SELL_CLOSETODAY = 2;
        public const int SELL_CLOSE = 21;

        public const int SELL_OPEN = 3;
        public const int BUY_CLOSETODAY = 4;
        public const int BUY_CLOSE = 41;

        //private long _orderId;
        private ConcurrentQueue<TradeItem> _tradeQueue;
        public Dictionary<int, OrderField> _tradeOrders = new Dictionary<int, OrderField>();
        public HashSet<int> _removingOrders = new HashSet<int>();

        private Trade _trade;
        private Quote _quote;
        private volatile bool _release;


        public TradeCenter(Trade t, Quote q, ConcurrentQueue<TradeItem> queue)
        {
            _trade = t;
            _quote = q;
            _tradeQueue = queue;
            _release = false;

            _trade.OnRtnTrade += (sender, e) =>
            {
                Console.WriteLine("TradeCenter OnRtnTrade:{0}", e.Value.TradeID);
                Log.log(string.Format("TradeCenter OnRtnTrade:{0}", e.Value.TradeID),e.Value.InstrumentID);
            };

            _trade.OnRtnError += (sender, e) =>
            {
                Console.WriteLine("TradeCenter OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
                Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
            };

            _trade.OnRtnCancel += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnCancel:{0}", e.Value.OrderID);
                Log.log(string.Format("OnRtnCancel:{0}", e.Value), e.Value.InstrumentID);
                OrderField orderField = null;
                if (_tradeOrders.TryGetValue(e.Value.OrderID, out orderField))
                {
                    _tradeOrders.Remove(e.Value.OrderID);
                }
                _removingOrders.Remove(e.Value.OrderID);
            };

            _trade.OnRtnOrder += (sender, e) =>
            {
                Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnOrder:{0}", e.Value.OrderID);
                Log.log(string.Format("OnRtnOrder:{0}", e.Value.OrderID), e.Value.InstrumentID);
                int _orderId = e.Value.OrderID;
                if (_tradeOrders.ContainsKey(_orderId))
                {
                    Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnOrder:{0} _tradeOrders exists");
                    _tradeOrders[_orderId] = e.Value;
                }
                else
                    _tradeOrders.Add(_orderId, e.Value);
            };

            _trade.OnRtnTrade += (sender, e) =>
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
                    e.Value.Price, e.Value.Volume, direction + offsetType));
                OrderField orderField = null;
                if (_tradeOrders.TryGetValue(e.Value.OrderID, out orderField))
                {
                    _tradeOrders.Remove(e.Value.OrderID);
                }
                _trade.ReqQryPosition();
            };
        }

        public void start()
        {
            Thread threadTest1 = new Thread(() =>
            {
                startImp();
            });
            threadTest1.Start();
        }

        private void startImp()
        {
            lock (_tradeQueue)
            {
                //Monitor.Pulse(_tradeQueue);//通知在Wait中阻塞的Producer线程即将执行
                while (true)
                {
                    bool result = Monitor.Wait(_tradeQueue, 1000);
                    
                    if (_release)
                        break;

                    TradeItem tradeItem;
                    bool getItem = _tradeQueue.TryDequeue(out tradeItem);//取出一个资源
                    if (getItem != false)
                    {
                        tradeOperator(tradeItem);
                    }
                    Monitor.Pulse(_tradeQueue);//通知在Wait中阻塞的Producer线程即将执行
                    
                }
            }
        }

        public void stop()
        {
            _release = true;
        }

        private void tradeOperator(TradeItem tradeItem)
        {
            
            //Console.WriteLine("操作start:{0}: {1}", op, inst);
            Log.log(string.Format(Log.LogTitle + "操作start:{0}: {1}", tradeItem.mOperator, tradeItem.mInstrument), tradeItem.mInstrument);
            string operation = string.Empty;


            DirectionType dire = DirectionType.Buy;
            OffsetType offset = OffsetType.Open;
            switch (tradeItem.mOperator)
            {
                case BUY_OPEN:
                    dire = DirectionType.Buy;
                    offset = OffsetType.Open;
                    operation = "买开";
                    break;
                case SELL_CLOSETODAY:
                    dire = DirectionType.Sell;
                    offset = OffsetType.CloseToday;
                    operation = "买平";
                    break;
                case SELL_CLOSE:
                    dire = DirectionType.Sell;
                    offset = OffsetType.Close;
                    operation = "买平昨";
                    break;
                case SELL_OPEN:
                    dire = DirectionType.Sell;
                    offset = OffsetType.Open;
                    operation = "卖开";
                    break;
                case BUY_CLOSETODAY:
                    dire = DirectionType.Buy;
                    offset = OffsetType.CloseToday;
                    operation = "卖平";
                    break;
                case BUY_CLOSE:
                    dire = DirectionType.Buy;
                    offset = OffsetType.Close;
                    operation = "卖平昨";
                    break;
                    
            }
            
            Console.WriteLine(Log.LogTitle + "操作:{0}: {1}", operation, tradeItem.mInstrument);
            Log.log(string.Format("操作:{0}: {1}", operation, tradeItem.mInstrument), tradeItem.mInstrument);
            OrderType ot = OrderType.Limit;
               
            MarketData tick;
            double price = tradeItem.mPrice;

            if(price<=0)
            {
                if (_quote.DicTick.TryGetValue(tradeItem.mInstrument, out tick))
                    price = dire == DirectionType.Buy ? tick.AskPrice : tick.BidPrice;
            }

            if(price>=0)
                Console.WriteLine(_trade.ReqOrderInsert(tradeItem.mInstrument, dire, offset, price, tradeItem.mVolumn, pType: ot));

            //if (_quote.DicTick.TryGetValue(tradeItem.mInstrument, out tick))
            //{
            //    double price = dire == DirectionType.Buy ? tick.AskPrice : tick.BidPrice;
            //    _orderId = -1;
            //    Console.WriteLine(_trade.ReqOrderInsert(tradeItem.mInstrument, dire, offset, price, 1, pType: ot));
                
            //}
            
        }
    }
}
