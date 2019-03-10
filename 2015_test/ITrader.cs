using Quote2015;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trade2015;

namespace ConsoleProxy
{
    /// <summary>
    /// 远程交易平台接口 用于抽象不同的交易平台
    /// 如CTP、测试系统、虚拟币等交易平台
    /// </summary>
    interface ITrader
    {
        /// <summary>
        /// 远程交易平台名
        /// </summary>
        string getTraderName();

        /// <summary>
		/// 初始化交易平台
		/// </summary>
        void onInit();

        /// <summary>
		/// 交易平台连接成功
		/// </summary>
        void onStart();


        /// <summary>
        /// 交易平台重启
        /// </summary>
        void onRestart();


        /// <summary>
		/// 交易平台连接结束
		/// </summary>
        void onEnd();

        /// <summary>
		/// 交易平台传来的Tick数据
		/// </summary>
        void onTick(TickEventArgs e);

        /// <summary>
		/// 交易Trade数据
		/// </summary>
        void onTrade(TradeArgs tradeArgs);

        /// <summary>
		/// Order数据
		/// </summary>
        void onOrder(OrderField order);

        /// <summary>
        /// Order数据
        /// </summary>
        void onOrderCancel(OrderField order);
    }
}
