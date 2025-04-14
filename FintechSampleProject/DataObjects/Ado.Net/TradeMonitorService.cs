using BusinessModels;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using FintechSampleProject.Server;
namespace DataObjects.Ado.Net
{
    public class TradeMonitorService : ITradeMonitorService
    {
        private readonly string _connectionString;
        private readonly IHubContext<FxHub> _hubContext;
        private readonly Dictionary<string, TraderActivity> _traderActivities = new();
        private readonly TimeSpan _window = TimeSpan.FromMinutes(5);
        private readonly int _tradeThreshold = 50;

        public TradeMonitorService(IConfiguration config, IHubContext<FxHub> hubContext)
        {
            _connectionString = config.GetConnectionString("TradeDatabase");
            _hubContext = hubContext;
        }

        public async Task ProcessTradeAsync(Trade trade)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "INSERT INTO Trades (Id, UserId, Amount, Currency, TradeType, Status, CreationDate) " +
                          "VALUES (@Id, @UserId, @Amount, @Currency, @TradeType, @Status, @CreationDate)";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", trade.Id);
                    command.Parameters.AddWithValue("@UserId", trade.UserId);
                    command.Parameters.AddWithValue("@Amount", trade.Amount);
                    command.Parameters.AddWithValue("@Currency", trade.Currency);
                    command.Parameters.AddWithValue("@TradeType", trade.TradeType);
                    command.Parameters.AddWithValue("@Status", trade.Status);
                    command.Parameters.AddWithValue("@CreationDate", trade.CreationDate);
                    await command.ExecuteNonQueryAsync();
                }
            }

            lock (_traderActivities)
            {
                if (!_traderActivities.TryGetValue(trade.UserId, out var activity))
                {
                    activity = new TraderActivity();
                    _traderActivities[trade.UserId] = activity;
                }

                while (activity.RecentTrades.Count > 0 &&
                       DateTime.UtcNow - activity.RecentTrades.Peek().CreationDate > _window)
                {
                    activity.RecentTrades.Dequeue();
                }

                activity.RecentTrades.Enqueue(trade);

                if (activity.TradeCount > _tradeThreshold)
                {
                    _hubContext.Clients.Group("Admins").SendAsync(
                        "SuspiciousActivityDetected",
                        new { UserId = trade.UserId, TradeCount = activity.TradeCount, Window = "5 minutes" }
                    );
                }
            }
        }
    }
}
