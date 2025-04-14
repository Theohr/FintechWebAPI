using FintechSampleProject.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace FintechSampleProject.Client
{
    public class FxClient : IAsyncDisposable
    {
        private readonly HubConnection _connection;

        public FxClient(string adminToken)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/adminhub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(adminToken);
                })
                .WithAutomaticReconnect()
                .Build();

            // Handler for PreferencesUpdated
            _connection.On<string, object>("PreferencesUpdated", (method, data) =>
            {
                dynamic payload = data;
                Console.WriteLine($"TraderClient: Preferences updated - DefaultCurrency: {payload.DefaultCurrency}, DefaultTradeSize: {payload.DefaultTradeSize}");
            });

            // Handler for TradeCreated
            _connection.On<string, object>("TradeCreated", (method, data) =>
            {
                dynamic payload = data;
                Console.WriteLine($"TraderClient: Trade created - TradeId: {payload.TradeId}, Amount: {payload.Amount}, Currency: {payload.Currency}, TradeType: {payload.TradeType}");
            });

            // Handler for TradeCanceled
            _connection.On<string, object>("TradeCanceled", (method, data) =>
            {
                dynamic payload = data;
                Console.WriteLine($"TraderClient: Trade canceled - TradeId: {payload.TradeId}");
            });

            //_connection.Closed += async (error) =>
            //{
            //    Console.WriteLine($"TraderClient: Connection closed. Error: {error?.Message ?? "No error"}");
            //    await Task.CompletedTask;
            //};
        }

        public async Task StartAsync()
        {
            Console.WriteLine("Client starting...");
            await _connection.StartAsync();
            //await _connection.InvokeAsync("JoinAdminGroup");
            Console.WriteLine("Client running...");
        }

        public Action<string, object> PreferencesUpdated { get; set; }
        public Action<string, object> TradeCreated { get; set; }
        public Action<string, object> TradeCanceled { get; set; }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                Console.WriteLine("Client: Disposed.");
            }
        }
    }
}
