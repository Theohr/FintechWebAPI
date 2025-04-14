using FintechSampleProject.DataObjects;
using FintechSampleProject.DataObjects.Ado.Net;
using FintechSampleProject.Models;
using FintechSampleProject.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FintechSampleProject.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/trades")]
    public class TradesController : ControllerBase
    {
        private readonly ITradeService _tradeService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly ITradeMonitorService _monitorService;
        private readonly ITradeQueueService _queueService;
        private readonly IHubContext<FxHub> _tradesHubContext;

        public TradesController(
            ITradeService tradeService,
            IExchangeRateService exchangeRateService,
            ITradeMonitorService monitorService,
            ITradeQueueService queueService,
            IHubContext<FxHub> tradesHubContext)
        {
            _tradeService = tradeService;
            _exchangeRateService = exchangeRateService;
            _monitorService = monitorService;
            _queueService = queueService;
            _tradesHubContext = tradesHubContext;
        }

        [HttpGet("total/{userId}/{accountId}")]
        public async Task<IActionResult> GetTotalByUser([FromRoute] int userId, [FromRoute] int accountId)
        {
            try
            {
                var account = await _tradeService.GetUserAccountAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("Account not found or does not belong to user.");
                }

                var trades = await _tradeService.GetTradesByUserIdAsync(userId);
                var userBalance = new UserBalance
                {
                    Balance = account.Balance,
                    TotalProfitLoss = 0m
                };

                foreach (var trade in trades)
                {
                    if (trade.Status != "Executed") continue;

                    decimal amountUsd = trade.Currency == "USD"
                        ? trade.Amount
                        : trade.Amount * await _exchangeRateService.GetExchangeRateAsync(trade.Currency, "USD");

                    if (trade.TradeType == "Sell")
                    {
                        userBalance.TotalProfitLoss -= amountUsd;
                    }
                    else if (trade.TradeType == "Buy")
                    {
                        userBalance.TotalProfitLoss += amountUsd;
                    }
                }

                return Ok(userBalance);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the balance.");
            }
        }

        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] Preferences preferences)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token.");
                }
                var updatedPreferences = await _tradeService.UpdatePreferencesAsync(preferences);

                await _tradesHubContext.Clients.All.SendAsync(
                    "PreferencesUpdated",
                    new { updatedPreferences.DefaultCurrency, updatedPreferences.DefaultTradeSize }
                );

                return Ok(updatedPreferences);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to update preferences: {ex.Message}");
            }
        }

        [HttpPost("ingest")]
        public async Task<IActionResult> IngestTrade([FromBody] Trade trade)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            trade.CreationDate = DateTime.UtcNow;
            trade.Id = new Random().Next(1, 1000000); 
            await _queueService.EnqueueTradeAsync(trade);
            return Accepted();
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitTrade([FromBody] Trade trade)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Extract userId from the JWT token
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            try
            {
                trade.CreationDate = DateTime.UtcNow;
                await _monitorService.ProcessTradeAsync(trade);

                // Send alert to the trader
                await _tradesHubContext.Clients.User(userId).SendAsync(
                    "TradeCreated",
                    new { TradeId = trade.Id, trade.Amount, trade.Currency, trade.TradeType }
                );

                // Send alert to admins
                await _tradesHubContext.Clients.Group("Admins").SendAsync(
                    "TradeCreated",
                    new { UserId = userId, TradeId = trade.Id, trade.Amount, trade.Currency, trade.TradeType }
                );

                return Accepted(new { TradeId = trade.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to create trade: {ex.Message}");
            }
        }

        [HttpPut("cancel/{tradeId}")]
        public async Task<IActionResult> CancelTrade([FromRoute] int tradeId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            try
            {
                await _tradeService.CancelTradeAsync(tradeId, Convert.ToInt32(userId));

                // Send alert to the trader
                await _tradesHubContext.Clients.User(userId).SendAsync(
                    "TradeCanceled",
                    tradeId
                );

                // Send alert to admins
                await _tradesHubContext.Clients.Group("Admins").SendAsync(
                    "TradeCanceled",
                    "UserId = " + userId + ", TradeId = " + tradeId
                );

                return Ok(new { Message = $"Trade {tradeId} canceled successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to cancel trade: {ex.Message}");
            }
        }

        // Added: Get daily trade summary
        [HttpGet("summary/{userId}")]
        public async Task<IActionResult> GetDailyTradeSummary([FromRoute] int userId, [FromQuery] DateTime date)
        {
            var authenticatedUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userId.ToString())
            {
                return Unauthorized("User is not authorized to access this data.");
            }

            try
            {
                var summary = await _tradeService.GetDailyTradeSummaryAsync(userId, date);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve daily trade summary: {ex.Message}");
            }
        }

        // Added: Get recent trades
        [HttpGet("recent/{userId}")]
        public async Task<IActionResult> GetRecentTrades([FromRoute] int userId, [FromQuery] int count = 10)
        {
            var authenticatedUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userId.ToString())
            {
                return Unauthorized("User is not authorized to access this data.");
            }

            try
            {
                var trades = await _tradeService.GetRecentTradesAsync(userId, count);
                return Ok(trades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve recent trades: {ex.Message}");
            }
        }

        [HttpGet("generate-token")]
        [AllowAnonymous] // Allow unauthenticated access to this endpoint
        public IActionResult GenerateToken(string userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key-here-1234567890ab"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Trader")
        };

            var token = new JwtSecurityToken(
                issuer: "TradeMonitorIssuer",
                audience: "TradeMonitorAudience",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
    }
}
