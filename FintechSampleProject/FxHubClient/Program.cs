using Microsoft.AspNetCore.SignalR.Client;
using BusinessModels;
class Program
{
    static async Task Main(string[] args)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:5001/fxhub", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult("admin-jwt-token");
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<string, object>("SuspiciousActivityDetected", (method, data) =>
        {
            Console.WriteLine($"Alert: User {data.UserId} made {data.TradeCount} trades in {data.Window}");
        });

        await connection.StartAsync();
        await connection.InvokeAsync("JoinAdminGroup");
        Console.WriteLine("Admin client running. Press any key to exit...");
        Console.ReadKey();
        await connection.StopAsync();
    }
}