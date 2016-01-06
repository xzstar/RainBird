using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleProxy
{


    class TradeItem
    {
        String mInstrument;
        String mDirection;
    }
    class TradeCenter
    {
        public static int BUY_OPEN = 1;
        public static int SELL_CLOSETODAY = 2;
        public static int SELL_OPEN = 3;
        public static int BUY_CLOSETODAY = 4;
        


    }
}
