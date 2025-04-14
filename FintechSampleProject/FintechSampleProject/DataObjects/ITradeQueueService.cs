using FintechSampleProject.Models;

namespace FintechSampleProject.DataObjects
{
    public interface ITradeQueueService
    {
        Task EnqueueTradeAsync(Trade trade);
        IAsyncEnumerable<Trade> DequeueTradesAsync(CancellationToken cancellationToken);
    }
}
