using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FintechSampleProject.Server
{
    public class FxHub : Hub
    {
        public const string TradeStatusUpdated = "TradeStatusUpdated";

        public async Task JoinAdminGroup()
        {
            // Optionally check if the user has an "Admin" role
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }
            else
            {
                throw new HubException("User is not authorized to join the Admins group.");
            }
        }

        public override async Task OnConnectedAsync()
        {
            // Automatically associates the connection with the authenticated user
            string userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }
    }

}
