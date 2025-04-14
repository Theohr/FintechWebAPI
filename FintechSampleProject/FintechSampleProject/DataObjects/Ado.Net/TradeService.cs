using FintechSampleProject.Models;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography.Xml;

namespace FintechSampleProject.DataObjects.Ado.Net
{
    public class TradeService : ITradeService
    {
        private readonly string _connectionString;

        public TradeService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("TradeDatabase");
        }

        public async Task<List<Trade>> GetTradesByUserIdAsync(int userId)
        {
            var trades = new List<Trade>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT Id, UserId, Amount, Currency, TradeType, Status, CreationDate, AccountId FROM FxTrades WHERE UserId = @UserId";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            trades.Add(new Trade
                            {
                                Id = reader.GetInt32(0),
                                UserId = Convert.ToInt32(reader.GetString(1)),
                                Amount = reader.GetDecimal(2),
                                Currency = reader.GetString(3),
                                TradeType = reader.GetString(4),
                                Status = reader.GetString(5),
                                CreationDate = reader.GetDateTime(6),
                                AccountId = reader.GetInt32(7)
                            });
                        }
                    }
                }
            }
            return trades;
        }

        public async Task<Account> GetUserAccountAsync(int accountId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT Id, UserId, Balance FROM Accounts WHERE Id = @AccountId";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@AccountId", accountId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Account
                            {
                                Id = reader.GetInt32(0),
                                UserId = Convert.ToInt32(reader.GetString(1)),
                                Balance = reader.GetDecimal(2)
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task<Preferences> UpdatePreferencesAsync(Preferences preferences)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                UPDATE Preferences 
                SET DefaultCurrency = @DefaultCurrency, DefaultSize = @DefaultSize
                WHERE Id = @Id AND AccountId = @AccountId";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", preferences.Id);
                    command.Parameters.AddWithValue("@AccountId", preferences.AccountId);
                    command.Parameters.AddWithValue("@DefaultCurrency", preferences.DefaultCurrency);
                    command.Parameters.AddWithValue("@DefaultSize", preferences.DefaultTradeSize);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new Exception("Preferences not found or account mismatch.");
                    }
                }
            }
            return preferences;
        }

        public async Task CancelTradeAsync(int tradeId, int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                UPDATE FxTrades 
                SET Status = 'Cancelled'
                WHERE Id = @TradeId AND UserId = @UserId AND Status = 'Pending'";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@TradeId", tradeId);
                    command.Parameters.AddWithValue("@UserId", userId);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new Exception("Trade not found, not owned by user, or not in a cancellable state.");
                    }
                }
            }
        }

        public async Task<DailyTradeSummary> GetDailyTradeSummaryAsync(int userId, DateTime date)
        {
            var summary = new DailyTradeSummary { Date = date.Date };
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                SELECT 
                    COUNT(*) as TotalTrades,
                    SUM(CASE WHEN TradeType = 'Buy' THEN 1 ELSE 0 END) as BuyTrades,
                    SUM(CASE WHEN TradeType = 'Sell' THEN 1 ELSE 0 END) as SellTrades,
                    SUM(Amount) as TotalAmount
                FROM FxTrades
                WHERE UserId = @UserId 
                AND CAST(CreationDate AS DATE) = @Date";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Date", date.Date);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            summary.TotalTrades = reader.GetInt32(0);
                            summary.BuyTrades = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            summary.SellTrades = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            summary.TotalAmount = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3);
                        }
                    }
                }
            }
            return summary;
        }

        public async Task<List<Trade>> GetRecentTradesAsync(int userId, int count)
        {
            var trades = new List<Trade>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                SELECT TOP (@Count) Id, UserId, Amount, Currency, TradeType, Status, CreationDate, AccountId
                FROM FxTrades 
                WHERE UserId = @UserId 
                ORDER BY CreationDate DESC";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Count", count);
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            trades.Add(new Trade
                            {
                                Id = reader.GetInt32(0),
                                UserId = Convert.ToInt32(reader.GetString(1)),
                                Amount = reader.GetDecimal(2),
                                Currency = reader.GetString(3),
                                TradeType = reader.GetString(4),
                                Status = reader.GetString(5),
                                CreationDate = reader.GetDateTime(6),
                                AccountId = reader.GetInt32(7)
                            });
                        }
                    }
                }
            }
            return trades;
        }
    }
}
