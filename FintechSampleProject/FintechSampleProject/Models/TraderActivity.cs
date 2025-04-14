using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FintechSampleProject.Models
{
    public class TraderActivity
    {
        // Track a traders recent trades for suspicious activity detection
        // Use of queue since we can implement a window of 5 minutes allowing easy addition and removal
        [JsonPropertyName("RecentTrades")]
        public Queue<Trade> RecentTrades { get; set; } = new Queue<Trade>();

        [JsonIgnore]
        public int TradeCount => RecentTrades.Count;
    }
}
