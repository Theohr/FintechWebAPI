using FintechSampleProject.Models;
using System.Threading.Channels;

namespace FintechSampleProject.DataObjects.Ado.Net
{
    public class TradeQueueService : ITradeQueueService
    {
        // Could be swapped with RabbitMQ in the future, enabled DI
        private readonly Channel<Trade> _channel;

        public TradeQueueService()
        {
            _channel = Channel.CreateUnbounded<Trade>();
        }

        // High-volume trading qithout overwhelming the database
        // Threading.Channels provides high performance and async0friendly queue to buffer trades for batch processing
        public async Task EnqueueTradeAsync(Trade trade)
        {
            await _channel.Writer.WriteAsync(trade);
        }

        public IAsyncEnumerable<Trade> DequeueTradesAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
