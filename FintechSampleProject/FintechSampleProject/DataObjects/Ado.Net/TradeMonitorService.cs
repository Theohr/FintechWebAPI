using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using FintechSampleProject.Server;
using FintechSampleProject.DataObjects.Ado.Net;
using FintechSampleProject.Models;
using StackExchange.Redis;
using System.Text.Json;


namespace FintechSampleProject.DataObjects
{
    public class TradeMonitorService : ITradeMonitorService
    {
        private readonly string _connectionString;
        private readonly IHubContext<FxHub> _hubContext;
        private readonly Dictionary<string, TraderActivity> _traderActivities = new();
        private readonly TimeSpan _window = TimeSpan.FromMinutes(5);
        private readonly int _tradeThreshold = 50;
        private readonly IDatabase _redisDb;

        public TradeMonitorService(IConfiguration config, IHubContext<FxHub> hubContext, IConnectionMultiplexer redis)
        {
            _connectionString = config.GetConnectionString("TradeDatabase");
            _hubContext = hubContext;
            _redisDb = redis.GetDatabase();
        }

        public async Task ProcessTradeAsync(Trade trade)
        {
            // Save the trade to the database
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "INSERT INTO Trades (UserId, Amount, Currency, TradeType, Status, CreationDate) " +
                          "OUTPUT INSERTED.Id " +
                          "VALUES (@UserId, @Amount, @Currency, @TradeType, @Status, @CreationDate)";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserId", trade.UserId);
                    command.Parameters.AddWithValue("@Amount", trade.Amount);
                    command.Parameters.AddWithValue("@Currency", trade.Currency);
                    command.Parameters.AddWithValue("@TradeType", trade.TradeType);
                    command.Parameters.AddWithValue("@Status", trade.Status);
                    command.Parameters.AddWithValue("@CreationDate", trade.CreationDate);
                    trade.Id = (int)await command.ExecuteScalarAsync();
                }
            }

            // Use Redis to manage trader activities
            string redisKey = $"TraderActivity:{trade.UserId}";
            TraderActivity activity;

            // Read the current activity from Redis
            var redisValue = await _redisDb.StringGetAsync(redisKey);
            if (redisValue.IsNullOrEmpty)
            {
                activity = new TraderActivity();
            }
            else
            {
                activity = JsonSerializer.Deserialize<TraderActivity>(redisValue);
            }

            // Update the activity (same logic as before, but now using Redis)
            while (activity.RecentTrades.Count > 0 &&
                   DateTime.UtcNow - activity.RecentTrades.Peek().CreationDate > _window)
            {
                activity.RecentTrades.Dequeue();
            }

            activity.RecentTrades.Enqueue(trade);

            if (activity.TradeCount > _tradeThreshold)
            {
                await _hubContext.Clients.Group("Admins").SendAsync(
                    "SuspiciousActivityDetected",
                    new SuspiciousActivity
                    {
                        UserId = trade.UserId.ToString(),
                        TradeCount = activity.TradeCount,
                        Window = "5 minutes"
                    }
                );
            }

            // Save the updated activity back to Redis
            await _redisDb.StringSetAsync(redisKey, JsonSerializer.Serialize(activity));
        }

        //public async Task ProcessTradeAsync(Trade trade)
        //{
        //    using (var connection = new SqlConnection(_connectionString))
        //    {
        //        await connection.OpenAsync();
        //        var sql = "INSERT INTO FxTrades (UserId, Amount, Currency, TradeType, Status, CreationDate, AccountId) " +
        //                  "VALUES (@UserId, @Amount, @Currency, @TradeType, @Status, @CreationDate, @AccountId)";
        //        using (var command = new SqlCommand(sql, connection))
        //        {
        //            command.Parameters.AddWithValue("@UserId", trade.UserId);
        //            command.Parameters.AddWithValue("@Amount", trade.Amount);
        //            command.Parameters.AddWithValue("@Currency", trade.Currency);
        //            command.Parameters.AddWithValue("@TradeType", trade.TradeType);
        //            command.Parameters.AddWithValue("@Status", trade.Status);
        //            command.Parameters.AddWithValue("@CreationDate", trade.CreationDate);
        //            command.Parameters.AddWithValue("@AccountId", trade.AccountId);
        //            await command.ExecuteNonQueryAsync();
        //        }
        //    }

        //    // thread-safety since Dictionary is not thread safe and multiple requests might process trades concurrently
        //    lock (_traderActivities)
        //    {
        //        if (!_traderActivities.TryGetValue(trade.UserId.ToString(), out var activity))
        //        {
        //            activity = new TraderActivity();
        //            _traderActivities[trade.UserId.ToString()] = activity;
        //        }

        //        while (activity.RecentTrades.Count > 0 &&
        //               DateTime.UtcNow - activity.RecentTrades.Peek().CreationDate > _window)
        //        {
        //            activity.RecentTrades.Dequeue();
        //        }

        //        activity.RecentTrades.Enqueue(trade);

        //        if (activity.TradeCount > _tradeThreshold)
        //        {
        //            _hubContext.Clients.Group("Admins").SendAsync(
        //                "SuspiciousActivityDetected",
        //                new { UserId = trade.UserId, TradeCount = activity.TradeCount, Window = "5 minutes" }
        //            );
        //        }
        //    }
        //}
    }
}
