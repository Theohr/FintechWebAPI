using FintechSampleProject.Models;

namespace FintechSampleProject.DataObjects.Ado.Net
{
    public interface ITradeMonitorService
    {
        Task ProcessTradeAsync(Trade trade);
    }
}
