using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Business.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time plant analysis notifications
    /// Enables instant notification delivery when async analysis completes
    /// </summary>
    [Authorize] // Requires JWT authentication
    public class PlantAnalysisHub : Hub
    {
        private readonly ILogger<PlantAnalysisHub> _logger;

        public PlantAnalysisHub(ILogger<PlantAnalysisHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// Logs connection for monitoring and debugging
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("userId")?.Value;
            var connectionId = Context.ConnectionId;

            _logger.LogInformation(
                "SignalR Connection Established - UserId: {UserId}, ConnectionId: {ConnectionId}",
                userId,
                connectionId);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// Logs disconnection for monitoring
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst("userId")?.Value;
            var connectionId = Context.ConnectionId;

            if (exception != null)
            {
                _logger.LogWarning(
                    exception,
                    "SignalR Connection Closed with Error - UserId: {UserId}, ConnectionId: {ConnectionId}",
                    userId,
                    connectionId);
            }
            else
            {
                _logger.LogInformation(
                    "SignalR Connection Closed - UserId: {UserId}, ConnectionId: {ConnectionId}",
                    userId,
                    connectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Test method for clients to verify connection health
        /// Returns current server timestamp
        /// </summary>
        public async Task Ping()
        {
            var userId = Context.User?.FindFirst("userId")?.Value;

            _logger.LogDebug(
                "Ping received from UserId: {UserId}, ConnectionId: {ConnectionId}",
                userId,
                Context.ConnectionId);

            await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
        }

        /// <summary>
        /// Allows clients to subscribe to specific analysis updates (future enhancement)
        /// </summary>
        /// <param name="analysisId">The analysis ID to subscribe to</param>
        public async Task SubscribeToAnalysis(int analysisId)
        {
            var userId = Context.User?.FindFirst("userId")?.Value;
            var groupName = $"Analysis_{analysisId}";

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation(
                "User {UserId} subscribed to analysis {AnalysisId}",
                userId,
                analysisId);
        }

        /// <summary>
        /// Unsubscribe from analysis updates
        /// </summary>
        public async Task UnsubscribeFromAnalysis(int analysisId)
        {
            var userId = Context.User?.FindFirst("userId")?.Value;
            var groupName = $"Analysis_{analysisId}";

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation(
                "User {UserId} unsubscribed from analysis {AnalysisId}",
                userId,
                analysisId);
        }
    }
}