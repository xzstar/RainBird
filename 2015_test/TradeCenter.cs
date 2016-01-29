using Quote2015;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trade2015;
using System.Collections.Concurrent;

namespace ConsoleProxy
{


    public class TradeItem
    {
        public string mInstrument;
        public int mOperator;

        public TradeItem(String instrument,int operate)
        {
            mInstrument = instrument;
            mOperator = operate;
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

        private long _orderId;
        private ConcurrentQueue<TradeItem> _tradeQueue;

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
                Log.log(string.Format("TradeCenter OnRtnTrade:{0}", e.Value.TradeID));
            };

            _trade.OnRtnError += (sender, e) =>
            {
                Console.WriteLine("TradeCenter OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
                Log.log(string.Format("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg));
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
                        tradeOperator(tradeItem.mOperator, tradeItem.mInstrument);
                    }
                    Monitor.Pulse(_tradeQueue);//通知在Wait中阻塞的Producer线程即将执行
                    
                }
            }
        }

        public void stop()
        {
            _release = true;
        }

        private void tradeOperator(int op, string inst)
        {
            //Console.WriteLine("操作start:{0}: {1}", op, inst);
            Log.log(string.Format(Program.LogTitle + "操作start:{0}: {1}", op, inst));
            string operation = string.Empty;


            DirectionType dire = DirectionType.Buy;
            OffsetType offset = OffsetType.Open;
            switch (op)
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
            
            Console.WriteLine(Program.LogTitle + "操作:{0}: {1}", operation, inst);
            Log.log(string.Format(Program.LogTitle + "操作:{0}: {1}", operation, inst));
            OrderType ot = OrderType.Limit;
               
            MarketData tick;

            if (_quote.DicTick.TryGetValue(inst, out tick))
            {
                double price = dire == DirectionType.Buy ? tick.AskPrice : tick.BidPrice;
                _orderId = -1;
                Console.WriteLine(_trade.ReqOrderInsert(inst, dire, offset, price, 1, pType: ot));
                //for (int i = 0; i < 3; ++i)
                //{
                //    Thread.Sleep(200);
                //    if (-1 != _orderId)
                //    {
                //        Console.WriteLine(Program.LogTitle + "委托标识:" + _orderId);
                //        break;
                //    }
                //}
            }
            
        }
    }
}
