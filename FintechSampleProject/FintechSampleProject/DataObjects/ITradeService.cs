using FintechSampleProject.Models;
using System.Security.Cryptography.Xml;

namespace FintechSampleProject.DataObjects
{
    public interface ITradeService
    {
        Task<List<Trade>> GetTradesByUserIdAsync(int userId);
        Task<Account> GetUserAccountAsync(int accountId);
        Task<Preferences> UpdatePreferencesAsync(Preferences preferences);
        Task CancelTradeAsync(int tradeId, int userId);
        Task<DailyTradeSummary> GetDailyTradeSummaryAsync(int userId, DateTime date);
        Task<List<Trade>> GetRecentTradesAsync(int userId, int count);
    }
}
