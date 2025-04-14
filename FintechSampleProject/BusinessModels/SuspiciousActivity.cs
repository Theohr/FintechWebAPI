using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessModels
{
    public class SuspiciousActivity
    {
        public string UserId { get; set; }
        public int TradeCount { get; set; }
        public string Window { get; set; }
    }
}
