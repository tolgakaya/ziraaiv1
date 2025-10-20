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

        #region Messaging Features (Phase 3)

        /// <summary>
        /// Notify that user started typing in a conversation
        /// </summary>
        public async Task StartTyping(int conversationUserId, int plantAnalysisId)
        {
            var userId = Context.User?.FindFirst("userId")?.Value;
            
            _logger.LogDebug(
                "User {UserId} started typing to {RecipientId} in analysis {AnalysisId}",
                userId,
                conversationUserId,
                plantAnalysisId);

            // Send typing notification to the recipient
            await Clients.User(conversationUserId.ToString())
                .SendAsync("UserTyping", new
                {
                    UserId = userId,
                    PlantAnalysisId = plantAnalysisId,
                    IsTyping = true,
                    Timestamp = DateTime.UtcNow
                });
        }

        /// <summary>
        /// Notify that user stopped typing in a conversation
        /// </summary>
        public async Task StopTyping(int conversationUserId, int plantAnalysisId)
        {
            var userId = Context.User?.FindFirst("userId")?.Value;
            
            _logger.LogDebug(
                "User {UserId} stopped typing to {RecipientId} in analysis {AnalysisId}",
                userId,
                conversationUserId,
                plantAnalysisId);

            // Send stop typing notification to the recipient
            await Clients.User(conversationUserId.ToString())
                .SendAsync("UserTyping", new
                {
                    UserId = userId,
                    PlantAnalysisId = plantAnalysisId,
                    IsTyping = false,
                    Timestamp = DateTime.UtcNow
                });
        }

        /// <summary>
        /// Notify that a new message was sent (real-time delivery)
        /// </summary>
        public async Task NotifyNewMessage(int recipientUserId, int messageId, int plantAnalysisId)
        {
            var userId = Context.User?.FindFirst("userId")?.Value;

            _logger.LogInformation(
                "New message {MessageId} notification from {SenderId} to {RecipientId}",
                messageId,
                userId,
                recipientUserId);

            await Clients.User(recipientUserId.ToString())
                .SendAsync("NewMessage", new
                {
                    MessageId = messageId,
                    SenderId = userId,
                    PlantAnalysisId = plantAnalysisId,
                    Timestamp = DateTime.UtcNow
                });
        }

        /// <summary>
        /// Notify that a message was read (read receipts)
        /// </summary>
        public async Task NotifyMessageRead(int senderUserId, int messageId)
        {
            var userId = Context.User?.FindFirst("userId")?.Value;

            _logger.LogDebug(
                "Message {MessageId} marked as read by {ReaderId}, notifying sender {SenderId}",
                messageId,
                userId,
                senderUserId);

            await Clients.User(senderUserId.ToString())
                .SendAsync("MessageRead", new
                {
                    MessageId = messageId,
                    ReadByUserId = userId,
                    ReadAt = DateTime.UtcNow
                });
        }

        #endregion

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