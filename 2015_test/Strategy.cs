using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Trade2015;

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
        void onTick();

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

        public void onTick()
        {
            foreach (IStrategy strategy in mStrategyDictionary.Values)
            {
                strategy.onTick();
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

        void IStrategy.onTick()
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
}
