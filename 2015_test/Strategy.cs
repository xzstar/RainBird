using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Trade2015;
using Quote2015;

namespace ConsoleProxy
{
    public interface IStrategy
    {
        /// <summary>
        /// 策略名
        /// </summary>
        string getStrategyName();
        /// <summary>
		/// 初始化数据
		/// </summary>
        void onInit();

        /// <summary>
		/// 策略开始
		/// </summary>
        void onStart();

        /// <summary>
		/// 策略结束
		/// </summary>
        void onEnd();

        /// <summary>
		/// Tick数据
		/// </summary>
        void onTick(TickEventArgs e);

        /// <summary>
		/// Trade数据
		/// </summary>
        void onTrade(TradeArgs tradeArgs);

        /// <summary>
		/// Order数据
		/// </summary>
        void onOrder(OrderField order);


        void onOrderCancel(OrderField order);
    }

//    public class StrategyManager
//    {
//        private static StrategyManager sInstance = new StrategyManager();
//        private Dictionary<int, OrderField> mTradeOrders = new Dictionary<int, OrderField>();

//        public static StrategyManager getInstance()
//        {
//            return sInstance;
//        }

//        private Dictionary<string, IStrategy> mStrategyDictionary = new Dictionary<string, IStrategy>();
//        public void addStrategy(IStrategy strategy)
//        {
//            if (strategy != null)
//            {
//                removeStrategy(strategy.getStrategyName());

//                mStrategyDictionary.Add(strategy.getStrategyName(), strategy);
//                strategy.onInit();
//                strategy.onStart();
//            }
//        }

//        public void removeStrategy(IStrategy strategy)
//        {
//            IStrategy curStrategy;
//            if (mStrategyDictionary.TryGetValue(strategy.getStrategyName(), out curStrategy))
//            {
//                curStrategy.onEnd();
//                mStrategyDictionary.Remove(strategy.getStrategyName());
//            }
//        }

//        public void removeStrategy(string strategyName)
//        {
//            IStrategy curStrategy;
//            if (mStrategyDictionary.TryGetValue(strategyName, out curStrategy))
//            {
//                curStrategy.onEnd();
//                mStrategyDictionary.Remove(strategyName);
//            }
//        }

//        public void onClose()
//        {
//            foreach (IStrategy strategy in mStrategyDictionary.Values)
//            {
//                strategy.onEnd();
//            }

//            mStrategyDictionary.Clear();
//        }

//        public void onTick(TickEventArgs e)
//        {
//            foreach (IStrategy strategy in mStrategyDictionary.Values)
//            {
//                strategy.onTick(e);
//            }
//        }

//        public void onTrade(TradeArgs tradeArgs)
//        {
//            foreach (IStrategy strategy in mStrategyDictionary.Values)
//            {
//                strategy.onTrade(tradeArgs);
//            }
//        }

//        public void onOrder(OrderField order)
//        {
//            foreach (IStrategy strategy in mStrategyDictionary.Values)
//            {
//                strategy.onOrder(order);
//            }
//        }

//        public void onOrderCancel(OrderField order)
//        {
//            foreach (IStrategy strategy in mStrategyDictionary.Values)
//            {
//                strategy.onOrderCancel(order);
//            }
//        }
//    }


//    public abstract class AbstractStrategy : IStrategy
//    {
//        protected List<InstrumentTradeConfig> mTradeInstrumentList;
//        protected Dictionary<string, InstrumentTradeConfig> mTradeInstumentMap;
//        protected ITradeCenter mTradeCenter;



//        public void setTradeCenter(ITradeCenter tradeCenter)
//        {
//            mTradeCenter = tradeCenter;
//        }

//        public string getStrategyName()
//        {
//            return "AbstractStrategy";
//        }

//        public void onEnd()
//        {
//            //throw new NotImplementedException();
//        }

//        public void onInit()
//        {
//            //throw new NotImplementedException();
//        }

//        public void onOrder(OrderField order)
//        {
//            //throw new NotImplementedException();
//        }

//        public void onStart()
//        {
//           // throw new NotImplementedException();
//        }

//        public void onTick(TickEventArgs e)
//        {
//            //throw new NotImplementedException();
//        }

//        public void onTrade(TradeArgs e)
//        {
//            //throw new NotImplementedException();
//        }


//        public void initTradeInstruments()
//        {
//            //使用二进制序列化对象
//            string fileName = FileUtil.getInstrumentFilePath();//文件名称与路径
//            mTradeInstumentMap = new Dictionary<string, InstrumentTradeConfig>();
//            try
//            {
//                string text = File.ReadAllText(fileName);
//                mTradeInstrumentList = JsonConvert.DeserializeObject<List<InstrumentTradeConfig>>(text);

//                foreach(InstrumentTradeConfig instrumentConfig in mTradeInstrumentList)
//                {
//                    mTradeInstumentMap.Add(instrumentConfig.instrument, instrumentConfig);
//                }
//            }
//            catch (Exception e)
//            {
//                mTradeInstrumentList = new List<InstrumentTradeConfig>();

//            }


//        }

//        public void onOrderCancel(OrderField order)
//        {
//            //throw new NotImplementedException();
//        }
//    }


//    public class BreakStrategy : IStrategy
//    {
//        protected List<InstrumentTradeConfig> mTradeInstrumentList;
//        protected Dictionary<string, InstrumentTradeConfig> mTradeInstumentMap;
//        protected Dictionary<string, HashSet<string>> mWaitingInstrumentMap = new Dictionary<string, HashSet<string>>();
//        protected ITradeCenter mTradeCenter;
//        private const int BACK_SPAN = 5;
//        private const int STOP_OPERATION = 10;  //10min
//        private const int STOP_LOSS = 5;

//        public void setTradeCenter(ITradeCenter tradeCenter)
//        {
//            mTradeCenter = tradeCenter;
//        }
//        protected Object mLockThis = new Object();
//        private Dictionary<string, InstrumentData> mTradeData = new Dictionary<string, InstrumentData>();
//        private Dictionary<int, OrderField> mTradeOrders;
//        public int mTotalSize;
//        public int mMinSpan;

//        public BreakStrategy(ITradeCenter tradeCenter)
//        {
//               setTradeCenter(tradeCenter);
//        }

//        //protected int MMinSpan
//        //{
//        //    get
//        //    {
//        //        return mMinSpan;
//        //    }

//        //    set
//        //    {
//        //        mMinSpan = value;
//        //    }
//        //}

//        //protected int MTotalSize
//        //{
//        //    get
//        //    {
//        //        return mTotalSize;
//        //    }

//        //    set
//        //    {
//        //        mTotalSize = value;
//        //    }
//        //}

//        public string getStrategyName()
//        {
//            return "BreakStrategy";
//        }

//        public void onEnd()
//        {
//            //throw new NotImplementedException();
//        }

//        public void onInit()
//        {
//            initTradeInstruments();
//            //throw new NotImplementedException();
//        }

//        public void onOrder(OrderField order)
//        {
//            //throw new NotImplementedException();
//            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnOrder:{0}", order.OrderID);
//            Log.log(string.Format("OnRtnOrder:{0} {1}", order.OrderID, order.LimitPrice), order.InstrumentID);
//        }

//        public void onStart()
//        {
//            //throw new NotImplementedException();
//        }

//        public void onTick(TickEventArgs e)
//        {
//            //lock (mLockThis)
//            //{
//            //    Boolean isTriggerLow = false;
//            //    Boolean isTriggerHigh = false;
//            //    bool isStopLoss = false;
//            //    //Console.WriteLine("OnRtnTick:{0}", e.Tick.LastPrice);
//            //    //Console.WriteLine("OnRtnTick:{0}=>{1}=>{2}", e.Tick.UpdateTime, e.Tick.AskPrice, e.Tick.InstrumentID);
//            //    DateTime d1 = DateTime.Parse(e.Tick.UpdateTime);

//            //    if (Utils.isTradingTime(e.Tick.InstrumentID, d1) == false)
//            //        return;

//            //    InstrumentWatcher.updateTime(e.Tick.InstrumentID, d1);
//            //    InstrumentData instrumentdata;

//            //    if (mTradeData.TryGetValue(e.Tick.InstrumentID, out instrumentdata) == false)
//            //    {
//            //        instrumentdata = new InstrumentData();
//            //        mTradeData.Add(e.Tick.InstrumentID, instrumentdata);
//            //    }

//            //    LinkedList<double> _highList = instrumentdata._highList;
//            //    LinkedList<double> _lowList = instrumentdata._lowList;

//            //    double priceTick = mTradeCenter.getInstrumentField(e.Tick.InstrumentID).PriceTick;

//            //    if (instrumentdata.lastMin == -1 || (instrumentdata.lastMin != d1.Minute && d1.Minute % mMinSpan == 0))
//            //    {
//            //        //Console.WriteLine("OnRtnTick 目前记录数:{0} {1}", e.Tick.InstrumentID, _highList.Count);
//            //        _highList.AddLast(e.Tick.LastPrice);
//            //        if (_highList.Count > mTotalSize)
//            //            _highList.RemoveFirst();

//            //        _lowList.AddLast(e.Tick.LastPrice);
//            //        if (_lowList.Count > mTotalSize)
//            //            _lowList.RemoveFirst();
//            //        isTriggerHigh = true;
//            //        isTriggerLow = true;

//            //        instrumentdata.highest = 0;
//            //        instrumentdata.lowest = 1000000;
//            //        foreach (double value in _highList)
//            //        {
//            //            //Console.WriteLine("品种{0} 最高:{1} 当前k线:{2}", e.Tick.InstrumentID, highest, value);
//            //            Log.log(string.Format(Program.LogTitle + "品种{0} 最高:{1} 当前k线:{2}", e.Tick.InstrumentID, instrumentdata.highest, value), e.Tick.InstrumentID);

//            //            if (value > instrumentdata.highest)
//            //                instrumentdata.highest = value;

//            //        }

//            //        foreach (double value in _lowList)
//            //        {
//            //            //Console.WriteLine("品种{0} 最低:{1} 当前k线:{2}", e.Tick.InstrumentID, lowest, value);
//            //            Log.log(string.Format("品种{0} 最低:{1} 当前k线:{2}", e.Tick.InstrumentID, instrumentdata.lowest, value), e.Tick.InstrumentID);
//            //            if (value < instrumentdata.lowest)
//            //                instrumentdata.lowest = value;
//            //        }
//            //        //Console.WriteLine("品种{0}新K线 最高:{1} 最低:{2}", e.Tick.InstrumentID, highest, lowest);
//            //        Log.log(string.Format(Program.LogTitle + "品种{0}新K线 最高:{1} 最低:{2}", e.Tick.InstrumentID, instrumentdata.highest, instrumentdata.lowest), e.Tick.InstrumentID);
//            //    }
//            //    else
//            //    {
//            //        double _lastHigh = _highList.Last();
//            //        double _lastLow = _lowList.Last();
//            //        if (e.Tick.LastPrice > _lastHigh)
//            //        {
//            //            _highList.RemoveLast();
//            //            _highList.AddLast(e.Tick.LastPrice);
//            //            isTriggerHigh = true;
//            //        }

//            //        if (e.Tick.LastPrice < _lastLow)
//            //        {
//            //            _lowList.RemoveLast();
//            //            _lowList.AddLast(e.Tick.LastPrice);
//            //            isTriggerLow = true;
//            //        }
//            //    }

//            //    OrderField openOrder = mTradeCenter.getOpeningOrder(e.Tick.InstrumentID);

//            //    if (openOrder != null && mTradeCenter.isRemovingOrder(openOrder.OrderID) == false)
//            //    {
//            //        if (instrumentdata.holder == 1 && openOrder.Direction == DirectionType.Buy
//            //            && instrumentdata.lowest > openOrder.LimitPrice)
//            //        {
//            //            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]"
//            //                + "Cancel buy:{0}, price:{1}, lowest {2}", openOrder.OrderID
//            //                , openOrder.LimitPrice, instrumentdata.lowest);
//            //            mTradeCenter.removeOrder(openOrder.OrderID);

//            //        }
//            //        else if (instrumentdata.holder == -1 && openOrder.Direction == DirectionType.Sell
//            //            && instrumentdata.highest < openOrder.LimitPrice)
//            //        {
//            //            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]"
//            //                + "Cancel sell:{0}, price:{1}, highest {2}", openOrder.OrderID
//            //                , openOrder.LimitPrice, instrumentdata.highest);
//            //            mTradeCenter.removeOrder(openOrder.OrderID);
//            //        }
//            //    }
//            //    if (openOrder != null && mTradeCenter.isRemovingOrder(openOrder.OrderID) == false)
//            //    {
//            //        bool isCancel = false;
//            //        HashSet<string> waitSecond = null;
//            //        if (mWaitingInstrumentMap.TryGetValue(openOrder.InstrumentID, out waitSecond))
//            //        {
//            //            waitSecond.Add(d1.ToString("yyyy-MM-dd-HH-mm"));
//            //            if (waitSecond != null && waitSecond.Count() > STOP_OPERATION)
//            //            {
//            //                Log.log(string.Format("品种{0} 下单超时放弃", e.Tick.InstrumentID), e.Tick.InstrumentID);
//            //                isCancel = true;
//            //            }
//            //        }
//            //        else if (instrumentdata.holder == 1 && openOrder.Direction == DirectionType.Buy
//            //            && instrumentdata.lowest > openOrder.LimitPrice)
//            //        {
//            //            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]"
//            //                + "Cancel buy:{0}, price:{1}, lowest {2}", openOrder.OrderID
//            //                , openOrder.LimitPrice, instrumentdata.lowest);
//            //            Log.log(string.Format("Cancel buy:{0}, price:{1}, lowest {2}", openOrder.OrderID
//            //                , openOrder.LimitPrice, instrumentdata.lowest), e.Tick.InstrumentID);
//            //            isCancel = true;
//            //        }
//            //        else if (instrumentdata.holder == -1 && openOrder.Direction == DirectionType.Sell
//            //            && instrumentdata.highest < openOrder.LimitPrice)
//            //        {
//            //            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]"
//            //                + "Cancel sell:{0}, price:{1}, highest {2}", openOrder.OrderID
//            //                , openOrder.LimitPrice, instrumentdata.highest);
//            //            Log.log(string.Format("Cancel sell:{0}, price:{1}, highest {2}", openOrder.OrderID
//            //                , openOrder.LimitPrice, instrumentdata.highest), e.Tick.InstrumentID);
//            //            isCancel = true;
//            //        }

//            //        if (isCancel)
//            //        {
//            //            instrumentdata.holder = 0;
//            //            instrumentdata.price = 0;
//            //            mTradeCenter.removeOrder(openOrder.OrderID);
//            //            mWaitingInstrumentMap.Remove(e.Tick.InstrumentID);
//            //            //program.tradeCenter._removingOrders.Add(openOrder.OrderID);
//            //            //program.trader.ReqOrderAction(openOrder.OrderID);
//            //            //program._waitingForOp.Remove(openOrder.InstrumentID);
//            //            openOrder = null;
//            //        }
//            //    }


//            //    instrumentdata.lastMin = d1.Minute;
//            //    InstrumentTradeConfig instrumentConfig = mTradeInstumentMap[e.Tick.InstrumentID];
//            //    if (instrumentConfig == null)
//            //    {
//            //        Log.log(string.Format(Program.LogTitle + "品种{0} 不在列表中", e.Tick.InstrumentID), e.Tick.InstrumentID);
//            //        return;
//            //    }
//            //    if (isTriggerHigh)
//            //    {
//            //        //Console.WriteLine("品种{0} 时间:{1} 触发新高:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
//            //        Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 触发新高:{2} 当前最高{3}  holder:{4}", e.Tick.InstrumentID,
//            //            e.Tick.UpdateTime, e.Tick.LastPrice, instrumentdata.highest, instrumentdata.holder), e.Tick.InstrumentID);
//            //        // more than _totoalSize , can trade now
//            //        if (_highList.Count >= mTotalSize && e.Tick.LastPrice > instrumentdata.highest)
//            //        {
//            //            //no trade before
//            //            if (instrumentdata.holder == 0 && openOrder == null)
//            //            {
//            //                //open buy
//            //                //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
//            //                if (instrumentConfig.trade)
//            //                {
//            //                    Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} Buy:{2}", e.Tick.InstrumentID,
//            //                        e.Tick.UpdateTime, e.Tick.LastPrice - BACK_SPAN * priceTick), e.Tick.InstrumentID);
//            //                    mTradeCenter.operateInstrument(OpType.BUY_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice - BACK_SPAN * priceTick, STOP_OPERATION);
//            //                }
//            //                //instrumentdata.holder = 1;
//            //                //instrumentdata.isToday = true;
//            //                //instrumentdata.price = e.Tick.LastPrice - BACK_SPAN * priceTick;


//            //            }
//            //            else if (instrumentdata.holder == -1)
//            //            {
//            //                if (instrumentConfig.trade)
//            //                {
//            //                    Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} Buy:{2}", e.Tick.InstrumentID,
//            //                        e.Tick.UpdateTime, e.Tick.LastPrice - BACK_SPAN * priceTick), e.Tick.InstrumentID);
//            //                    mTradeCenter.operateInstrument(OpType.BUY_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice - BACK_SPAN * priceTick, STOP_OPERATION);

//            //                }

//            //                //close sell and open buy
//            //                if (instrumentdata.isToday)
//            //                {
//            //                    //operatord(trader, quoter, BUY_CLOSETODAY, e.Tick.InstrumentID);
//            //                    if (instrumentConfig.trade)
//            //                        mTradeCenter.operateInstrument(OpType.BUY_CLOSETODAY, e.Tick.InstrumentID, 0, -1);

//            //                }
//            //                else
//            //                {
//            //                    //operatord(trader, quoter, BUY_CLOSE, e.Tick.InstrumentID);
//            //                    if (instrumentConfig.trade)
//            //                        mTradeCenter.operateInstrument(OpType.BUY_CLOSE, e.Tick.InstrumentID, 0, -1);

//            //                }
//            //                //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
//            //                //instrumentdata.holder = 1;
//            //                //instrumentdata.isToday = true;
//            //                //instrumentdata.price = e.Tick.LastPrice - BACK_SPAN * priceTick;
//            //                instrumentdata.holder = 0;
//            //                instrumentdata.price = 0;
//            //            }
//            //        }


//            //    }

//            //    else if (isTriggerLow)
//            //    {
//            //        //Console.WriteLine("品种{0} 时间:{1} 触发新低:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
//            //        Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 触发新低:{2} 当前最低:{3} holder:{4}", e.Tick.InstrumentID,
//            //            e.Tick.UpdateTime, e.Tick.LastPrice, instrumentdata.lowest, instrumentdata.holder), e.Tick.InstrumentID);

//            //        // more than _totoalSize , can trade now
//            //        if (_lowList.Count >= mTotalSize && e.Tick.LastPrice < instrumentdata.lowest)
//            //        {
//            //            //no trade before
//            //            if (instrumentdata.holder == 0 && openOrder == null)
//            //            {
//            //                //open sell
//            //                //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
//            //                if (instrumentConfig.trade)
//            //                {
//            //                    Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} Sell:{2}", e.Tick.InstrumentID,
//            //                        e.Tick.UpdateTime, e.Tick.LastPrice + BACK_SPAN * priceTick), e.Tick.InstrumentID);
//            //                    mTradeCenter.operateInstrument(OpType.SELL_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice + BACK_SPAN * priceTick, STOP_OPERATION);
//            //                }
//            //                //instrumentdata.holder = -1;
//            //                //instrumentdata.isToday = true;
//            //                //instrumentdata.price = e.Tick.LastPrice + BACK_SPAN * priceTick;

//            //            }
//            //            else if (instrumentdata.holder == 1)
//            //            {
//            //                if (instrumentConfig.trade)
//            //                {
//            //                    Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} Sell:{2}", e.Tick.InstrumentID,
//            //                        e.Tick.UpdateTime, e.Tick.LastPrice + BACK_SPAN * priceTick), e.Tick.InstrumentID);
//            //                    mTradeCenter.operateInstrument(OpType.SELL_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice + BACK_SPAN * priceTick, STOP_OPERATION);
//            //                }
//            //                //close buy and open sell
//            //                if (instrumentdata.isToday)
//            //                {
//            //                    //operatord(trader, quoter, SELL_CLOSETODAY, e.Tick.InstrumentID);
//            //                    if (instrumentConfig.trade)
//            //                        mTradeCenter.operateInstrument(OpType.SELL_CLOSETODAY, e.Tick.InstrumentID, 0, -1);

//            //                }
//            //                else
//            //                {
//            //                    //operatord(trader, quoter, SELL_CLOSE, e.Tick.InstrumentID);
//            //                    if (instrumentConfig.trade)
//            //                        mTradeCenter.operateInstrument(OpType.SELL_CLOSE, e.Tick.InstrumentID, 0, -1);
//            //                }
//            //                //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
//            //                //instrumentdata.holder = -1;
//            //                //instrumentdata.isToday = true;
//            //                //instrumentdata.price = e.Tick.LastPrice + BACK_SPAN * priceTick;
//            //                instrumentdata.holder = 0;
//            //                instrumentdata.price = 0;
//            //            }
//            //        }
//            //    }


//            //    //没有在挂单
//            //    else if (openOrder == null)
//            //    {
//            //        if (instrumentdata.holder == 1 && instrumentdata.price - e.Tick.LastPrice > STOP_LOSS)
//            //        {
//            //            Log.log(string.Format(Program.LogTitle + "品种{0} 多头止损 {1}  {2} ", e.Tick.InstrumentID, instrumentdata.price, e.Tick.LastPrice), e.Tick.InstrumentID);

//            //            //close buy 
//            //            if (instrumentdata.isToday)
//            //            {
//            //                if (instrumentConfig.trade)
//            //                    mTradeCenter.operateInstrument(OpType.SELL_CLOSETODAY, e.Tick.InstrumentID, 0, -1);

//            //            }
//            //            else
//            //            {
//            //                if (instrumentConfig.trade)
//            //                    mTradeCenter.operateInstrument(OpType.SELL_CLOSE, e.Tick.InstrumentID, 0, -1);
//            //            }
//            //            instrumentdata.holder = 0;
//            //            instrumentdata.price = 0;
//            //            isStopLoss = true;
//            //        }
//            //        else if (instrumentdata.holder == -1 && e.Tick.LastPrice - instrumentdata.price > STOP_LOSS)
//            //        {
//            //            Log.log(string.Format(Program.LogTitle + "品种{0} 空头止损 {1}  {2} ", e.Tick.InstrumentID, instrumentdata.price, e.Tick.LastPrice), e.Tick.InstrumentID);
//            //            //close sell 
//            //            if (instrumentdata.isToday)
//            //            {
//            //                if (instrumentConfig.trade)
//            //                    mTradeCenter.operateInstrument(OpType.BUY_CLOSETODAY, e.Tick.InstrumentID, 0, -1);

//            //            }
//            //            else
//            //            {
//            //                if (instrumentConfig.trade)
//            //                    mTradeCenter.operateInstrument(OpType.BUY_CLOSE, e.Tick.InstrumentID, 0, -1);

//            //            }
//            //            instrumentdata.holder = 0;
//            //            instrumentdata.price = 0;
//            //            isStopLoss = true;
//            //        }

//            //    }

//            //    if (isTriggerHigh || isTriggerLow || isStopLoss)
//            //    {

//            //        string fileNameSerialize = FileUtil.getTradeFilePath();
//            //        string jsonString = JsonConvert.SerializeObject(mTradeData);

//            //        File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
//            //    }
//            //}

//        }

//        public void onTrade(TradeArgs e)
//        {
//            ////throw new NotImplementedException();
//            //Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]" + "OnRtnTrade:{0}", e.Value.TradeID);
//            //Log.log(string.Format("OnRtnTrade:{0} OrderID {1}", e.Value.TradeID, e.Value.OrderID), e.Value.InstrumentID);
//            ////成交 需要下委托单
//            //string direction = e.Value.Direction == DirectionType.Buy ? "买" : "卖";
//            //InstrumentData instrumentdata;
//            //if (mTradeData.TryGetValue(e.Value.InstrumentID, out instrumentdata) == false)
//            //{
//            //    return;
//            //}


//            //if (e.Value.Direction == DirectionType.Buy)
//            //{
//            //    instrumentdata.holder = 1;
//            //    instrumentdata.price = e.Value.Price;
//            //}
//            //else
//            //{
//            //    instrumentdata.holder = -1;
//            //    instrumentdata.price = e.Value.Price;
//            //}

//            //string offsetType = "开";
//            //if (e.Value.Offset == OffsetType.Close)
//            //    offsetType = "平";
//            //else if (e.Value.Offset == OffsetType.CloseToday)
//            //    offsetType = "平今";
//            //Log.logTrade(string.Format("{0},{1},{2},{3},{4},{5}", e.Value.InstrumentID, e.Value.TradingDay, e.Value.TradeTime,
//            //    e.Value.Price, e.Value.Volume, direction + offsetType));
//        }


//        public void initTradeInstruments()
//        {
//            //使用二进制序列化对象
//            string fileName = FileUtil.getInstrumentFilePath();//文件名称与路径
//            mTradeInstumentMap = new Dictionary<string, InstrumentTradeConfig>();
//            try
//            {
//                string text = File.ReadAllText(fileName);
//                mTradeInstrumentList = JsonConvert.DeserializeObject<List<InstrumentTradeConfig>>(text);

//                foreach (InstrumentTradeConfig instrumentConfig in mTradeInstrumentList)
//                {
//                    mTradeInstumentMap.Add(instrumentConfig.instrument, instrumentConfig);
//                }
//            }
//            catch (Exception e)
//            {
//                mTradeInstrumentList = new List<InstrumentTradeConfig>();

//            }


//        }

//        public void onOrderCancel(OrderField order)
//        {
//            Log.log(string.Format("OnRtnCancel:{0} 时间:{1}",order.OrderID,order.InsertTime), order.InstrumentID);
//            InstrumentData instrumentdata;
//            if (mTradeData.TryGetValue(order.InstrumentID, out instrumentdata) == false)
//            {
//                return;
//            }

//        }
//    }
}
