using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessModels
{
    public class TraderActivity
    {
        public Queue<Trade> RecentTrades { get; set; } = new Queue<Trade>();
        public int TradeCount => RecentTrades.Count;
    }
}
