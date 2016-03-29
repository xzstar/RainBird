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
        void onTrade();

        /// <summary>
		/// Order数据
		/// </summary>
        void onOrder();
    }

    public class StrategyManager
    {
        private static StrategyManager sInstance = new StrategyManager();
        private Dictionary<int, OrderField> mTradeOrders = new Dictionary<int, OrderField>();

        public StrategyManager getInstance()
        {
            return sInstance;
        }

        private Dictionary<string, IStrategy> mStrategyDictionary = new Dictionary<string, IStrategy>();
        public void addStrategy(IStrategy strategy)
        {
            if(strategy!=null)
            {
                removeStrategy(strategy.getStrategyName());

                mStrategyDictionary.Add(strategy.getStrategyName(), strategy);
                strategy.onInit();
                strategy.onStart();
              }
        }

        public void removeStrategy(IStrategy strategy)
        {
            IStrategy curStrategy;
            if (mStrategyDictionary.TryGetValue(strategy.getStrategyName(), out curStrategy))
            {
                curStrategy.onEnd();
                mStrategyDictionary.Remove(strategy.getStrategyName());
            }
        }

        public void removeStrategy(string strategyName)
        {
            IStrategy curStrategy;
            if (mStrategyDictionary.TryGetValue(strategyName, out curStrategy))
            {
                curStrategy.onEnd();
                mStrategyDictionary.Remove(strategyName);
            }
        }

        public void onClose()
        {
            foreach(IStrategy strategy in mStrategyDictionary.Values)
            {
                strategy.onEnd();
            }

            mStrategyDictionary.Clear();
        }

        public void onTick(TickEventArgs e)
        {
            foreach (IStrategy strategy in mStrategyDictionary.Values)
            {
                strategy.onTick(e);
            }
        }

        public void onTrade()
        {
            foreach (IStrategy strategy in mStrategyDictionary.Values)
            {
                strategy.onTrade();
            }
        }

        public void onOrder()
        {
            foreach (IStrategy strategy in mStrategyDictionary.Values)
            {
                strategy.onOrder();
            }
        }
    }


    public abstract class AbstractStrategy : IStrategy
    {
        protected List<string> mTradeInstrumentList = new List<string>();
        string IStrategy.getStrategyName()
        {
            return "AbstractStrategy";
        }

        void IStrategy.onEnd()
        {
            throw new NotImplementedException();
        }

        void IStrategy.onInit()
        {
            throw new NotImplementedException();
        }

        void IStrategy.onOrder()
        {
            throw new NotImplementedException();
        }

        void IStrategy.onStart()
        {
            throw new NotImplementedException();
        }

        void IStrategy.onTick(TickEventArgs e)
        {
            throw new NotImplementedException();
        }

        void IStrategy.onTrade()
        {
            throw new NotImplementedException();
        }


        public void initTradeInstruments()
        {
            //使用二进制序列化对象
            string fileName = FileUtil.getInstrumentFilePath(this);//文件名称与路径
            try
            {
                string text = File.ReadAllText(fileName);
                mTradeInstrumentList = JsonConvert.DeserializeObject<List<string>>(text);
            }
            catch (Exception e)
            {

            }

            
        }
    }


    public abstract class BreakStrategy : IStrategy
    {
        protected List<string> mTradeInstrumentList = new List<string>();
        protected System.Object mLockThis = new System.Object();
        private Dictionary<string, InstrumentData> mTradeData = new Dictionary<string, InstrumentData>();
        private Dictionary<int, OrderField> mTradeOrders;
        private int mTotalSize;
        private int mMinSpan;

        protected int MMinSpan
        {
            get
            {
                return mMinSpan;
            }

            set
            {
                mMinSpan = value;
            }
        }

        protected int MTotalSize
        {
            get
            {
                return mTotalSize;
            }

            set
            {
                mTotalSize = value;
            }
        }

        string IStrategy.getStrategyName()
        {
            return "BreakStrategy";
        }

        void IStrategy.onEnd()
        {
            throw new NotImplementedException();
        }

        void IStrategy.onInit()
        {
            throw new NotImplementedException();
        }

        void IStrategy.onOrder()
        {
            throw new NotImplementedException();
        }

        void IStrategy.onStart()
        {
            throw new NotImplementedException();
        }

        void IStrategy.onTick(TickEventArgs e)
        {
            //lock (mLockThis)
            //{
            //    Boolean isTriggerLow = false;
            //    Boolean isTriggerHigh = false;
            //    //Console.WriteLine("OnRtnTick:{0}", e.Tick.LastPrice);
            //    //Console.WriteLine("OnRtnTick:{0}=>{1}=>{2}", e.Tick.UpdateTime, e.Tick.AskPrice, e.Tick.InstrumentID);
            //    DateTime d1 = DateTime.Parse(e.Tick.UpdateTime);

            //    if (Utils.isTradingTime(e.Tick.InstrumentID, d1) == false)
            //        return;

            //    InstrumentWatcher.updateTime(e.Tick.InstrumentID, d1);
            //    InstrumentData instrumentdata;

            //    if (mTradeData.TryGetValue(e.Tick.InstrumentID, out instrumentdata) == false)
            //    {
            //        instrumentdata = new InstrumentData();
            //        mTradeData.Add(e.Tick.InstrumentID, instrumentdata);
            //    }

            //    LinkedList<double> _highList = instrumentdata._highList;
            //    LinkedList<double> _lowList = instrumentdata._lowList;


            //    if (instrumentdata.lastMin == -1 || (instrumentdata.lastMin != d1.Minute && d1.Minute % mMinSpan == 0))
            //    {
            //        //Console.WriteLine("OnRtnTick 目前记录数:{0} {1}", e.Tick.InstrumentID, _highList.Count);
            //        _highList.AddLast(e.Tick.LastPrice);
            //        if (_highList.Count > mTotalSize)
            //            _highList.RemoveFirst();

            //        _lowList.AddLast(e.Tick.LastPrice);
            //        if (_lowList.Count > mTotalSize)
            //            _lowList.RemoveFirst();
            //        isTriggerHigh = true;
            //        isTriggerLow = true;

            //        instrumentdata.highest = 0;
            //        instrumentdata.lowest = 1000000;
            //        foreach (double value in _highList)
            //        {
            //            //Console.WriteLine("品种{0} 最高:{1} 当前k线:{2}", e.Tick.InstrumentID, highest, value);
            //            Log.log(string.Format(Program.LogTitle + "品种{0} 最高:{1} 当前k线:{2}", e.Tick.InstrumentID, instrumentdata.highest, value));

            //            if (value > instrumentdata.highest)
            //                instrumentdata.highest = value;

            //        }

            //        foreach (double value in _lowList)
            //        {
            //            //Console.WriteLine("品种{0} 最低:{1} 当前k线:{2}", e.Tick.InstrumentID, lowest, value);
            //            Log.log(string.Format("品种{0} 最低:{1} 当前k线:{2}", e.Tick.InstrumentID, instrumentdata.lowest, value));
            //            if (value < instrumentdata.lowest)
            //                instrumentdata.lowest = value;
            //        }
            //        //Console.WriteLine("品种{0}新K线 最高:{1} 最低:{2}", e.Tick.InstrumentID, highest, lowest);
            //        Log.log(string.Format(Program.LogTitle + "品种{0}新K线 最高:{1} 最低:{2}", e.Tick.InstrumentID, instrumentdata.highest, instrumentdata.lowest));
            //    }
            //    else
            //    {
            //        double _lastHigh = _highList.Last();
            //        double _lastLow = _lowList.Last();
            //        if (e.Tick.LastPrice > _lastHigh)
            //        {
            //            _highList.RemoveLast();
            //            _highList.AddLast(e.Tick.LastPrice);
            //            isTriggerHigh = true;
            //        }

            //        if (e.Tick.LastPrice < _lastLow)
            //        {
            //            _lowList.RemoveLast();
            //            _lowList.AddLast(e.Tick.LastPrice);
            //            isTriggerLow = true;
            //        }
            //    }

            //    OrderField openOrder = null;
            //    foreach (OrderField order in program._tradeOrders.Values)
            //    {
            //        if (order.InstrumentID == e.Tick.InstrumentID && order.Offset == OffsetType.Open)
            //        {
            //            openOrder = order;
            //            break;
            //        }
            //    }
            //    if (openOrder != null && program._removingOrders.Contains(openOrder.OrderID) == false)
            //    {
            //        if (instrumentdata.holder == 1 && openOrder.Direction == DirectionType.Buy
            //            && instrumentdata.lowest > openOrder.LimitPrice)
            //        {
            //            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]"
            //                + "Cancel buy:{0}, price:{1}, lowest {2}", openOrder.OrderID
            //                , openOrder.LimitPrice, instrumentdata.lowest);
            //            program._removingOrders.Add(openOrder.OrderID);
            //            program.trader.ReqOrderAction(openOrder.OrderID);
            //        }
            //        else if (instrumentdata.holder == -1 && openOrder.Direction == DirectionType.Sell
            //            && instrumentdata.highest < openOrder.LimitPrice)
            //        {
            //            Console.WriteLine("[" + DateTime.Now.ToLocalTime().ToString() + "]"
            //                + "Cancel sell:{0}, price:{1}, highest {2}", openOrder.OrderID
            //                , openOrder.LimitPrice, instrumentdata.highest);
            //            program._removingOrders.Add(openOrder.OrderID);
            //            program.trader.ReqOrderAction(openOrder.OrderID);
            //        }
            //    }


            //    instrumentdata.lastMin = d1.Minute;
            //    InstrumentTradeConfig instrumentConfig = program._instrumentMap[e.Tick.InstrumentID];
            //    if (instrumentConfig == null)
            //    {
            //        Log.log(string.Format(Program.LogTitle + "品种{0} 不在列表中", e.Tick.InstrumentID));
            //        return;
            //    }
            //    if (isTriggerHigh)
            //    {
            //        //Console.WriteLine("品种{0} 时间:{1} 触发新高:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
            //        Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 触发新高:{2} 当前最高{3}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice, instrumentdata.highest));
            //        // more than _totoalSize , can trade now
            //        if (_highList.Count >= mTotalSize && e.Tick.LastPrice > instrumentdata.highest)
            //        {
            //            //no trade before
            //            if (instrumentdata.holder == 0)
            //            {
            //                //open buy
            //                //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
            //                if (instrumentConfig.trade)
            //                    operatorInstrument(BUY_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice - 2 * program.trader.DicInstrumentField[e.Tick.InstrumentID].PriceTick);
            //                instrumentdata.holder = 1;
            //                instrumentdata.isToday = true;


            //            }
            //            else if (instrumentdata.holder == -1)
            //            {
            //                //close sell and open buy
            //                if (instrumentdata.isToday)
            //                {
            //                    //operatord(trader, quoter, BUY_CLOSETODAY, e.Tick.InstrumentID);
            //                    if (instrumentConfig.trade)
            //                        operatorInstrument(BUY_CLOSETODAY, e.Tick.InstrumentID, 0);

            //                }
            //                else
            //                {
            //                    //operatord(trader, quoter, BUY_CLOSE, e.Tick.InstrumentID);
            //                    if (instrumentConfig.trade)
            //                        operatorInstrument(BUY_CLOSE, e.Tick.InstrumentID, 0);

            //                }
            //                //operatord(trader, quoter, BUY_OPEN, e.Tick.InstrumentID);
            //                if (instrumentConfig.trade)
            //                    operatorInstrument(BUY_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice - 2 * program.trader.DicInstrumentField[e.Tick.InstrumentID].PriceTick);
            //                instrumentdata.holder = 1;
            //                instrumentdata.isToday = true;
            //            }
            //        }


            //    }

            //    if (isTriggerLow)
            //    {
            //        //Console.WriteLine("品种{0} 时间:{1} 触发新低:{2}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice);
            //        Log.log(string.Format(Program.LogTitle + "品种{0} 时间:{1} 触发新低:{2} 当前最低:{3}", e.Tick.InstrumentID, e.Tick.UpdateTime, e.Tick.LastPrice, instrumentdata.lowest));

            //        // more than _totoalSize , can trade now
            //        if (_lowList.Count >= mTotalSize && e.Tick.LastPrice < instrumentdata.lowest)
            //        {
            //            //no trade before
            //            if (instrumentdata.holder == 0)
            //            {
            //                //open sell
            //                //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
            //                if (instrumentConfig.trade)
            //                    operatorInstrument(SELL_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice + 2 * program.trader.DicInstrumentField[e.Tick.InstrumentID].PriceTick);
            //                instrumentdata.holder = -1;
            //                instrumentdata.isToday = true;
            //            }
            //            else if (instrumentdata.holder == 1)
            //            {
            //                //close buy and open sell
            //                if (instrumentdata.isToday)
            //                {
            //                    //operatord(trader, quoter, SELL_CLOSETODAY, e.Tick.InstrumentID);
            //                    if (instrumentConfig.trade)
            //                        operatorInstrument(SELL_CLOSETODAY, e.Tick.InstrumentID, 0);

            //                }
            //                else
            //                {
            //                    //operatord(trader, quoter, SELL_CLOSE, e.Tick.InstrumentID);
            //                    if (instrumentConfig.trade)
            //                        operatorInstrument(SELL_CLOSE, e.Tick.InstrumentID, 0);
            //                }
            //                //operatord(trader, quoter, SELL_OPEN, e.Tick.InstrumentID);
            //                if (instrumentConfig.trade)
            //                    operatorInstrument(SELL_OPEN, e.Tick.InstrumentID, e.Tick.LastPrice + 2 * program.trader.DicInstrumentField[e.Tick.InstrumentID].PriceTick);
            //                instrumentdata.holder = -1;
            //                instrumentdata.isToday = true;
            //            }
            //        }
            //    }

            //    if (isTriggerHigh || isTriggerLow)
            //    {

            //        string fileNameSerialize = FileUtil.getTradeFilePath();
            //        string jsonString = JsonConvert.SerializeObject(mTradeData);

            //        File.WriteAllText(fileNameSerialize, jsonString, Encoding.UTF8);
            //    }
            //}
        }

        void IStrategy.onTrade()
        {
            throw new NotImplementedException();
        }


        public void initTradeInstruments()
        {
            //使用二进制序列化对象
            string fileName = FileUtil.getInstrumentFilePath(this);//文件名称与路径
            try
            {
                string text = File.ReadAllText(fileName);
                mTradeInstrumentList = JsonConvert.DeserializeObject<List<string>>(text);
            }
            catch (Exception e)
            {

            }


        }


    }
}
