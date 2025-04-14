using FintechSampleProject.DataObjects;
using FintechSampleProject.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FintechSampleProject.Services
{
    public class TradeProcessingService : BackgroundService
    {
        // background service to process trades from queue in batches, saving with SqlBulkCOpy
        private readonly ITradeQueueService _queueService;
        private readonly string _connectionString;

        public TradeProcessingService(ITradeQueueService queueService, IConfiguration config)
        {
            _queueService = queueService;
            _connectionString = config.GetConnectionString("TradeDatabase");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var batch = new List<Trade>();
            const int batchSize = 100;
            var batchTimeout = TimeSpan.FromSeconds(5);

            await foreach (var trade in _queueService.DequeueTradesAsync(stoppingToken))
            {
                batch.Add(trade);

                if (batch.Count >= batchSize || DateTime.UtcNow - batch.First().CreationDate > batchTimeout)
                {
                    await ProcessBatchAsync(batch);
                    batch.Clear();
                }
            }
        }

        private async Task ProcessBatchAsync(List<Trade> trades)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "Trades";
                    var dataTable = new DataTable();
                    dataTable.Columns.Add("Id", typeof(int));
                    dataTable.Columns.Add("UserId", typeof(string));
                    dataTable.Columns.Add("Amount", typeof(decimal));
                    dataTable.Columns.Add("Currency", typeof(string));
                    dataTable.Columns.Add("TradeType", typeof(string));
                    dataTable.Columns.Add("Status", typeof(string));
                    dataTable.Columns.Add("CreationDate", typeof(DateTime));

                    foreach (var trade in trades)
                    {
                        dataTable.Rows.Add(trade.Id, trade.UserId, trade.Amount, trade.Currency,
                            trade.TradeType, trade.Status, trade.CreationDate);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable);
                }
            }
        }
    }
}
