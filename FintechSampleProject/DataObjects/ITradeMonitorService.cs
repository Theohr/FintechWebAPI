using BusinessModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public interface ITradeMonitorService
    {
        Task ProcessTradeAsync(Trade trade);
    }
}
