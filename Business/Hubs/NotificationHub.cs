using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Business.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time notifications (dealer invitations, etc.)
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
            var userPhone = Context.User?.FindFirst(ClaimTypes.MobilePhone)?.Value;

            _logger.LogInformation("📡 SignalR NotificationHub connected - UserId: {UserId}, Email: {Email}, Phone: {Phone}, ConnectionId: {ConnectionId}",
                userId ?? "null", userEmail ?? "null", userPhone ?? "null", Context.ConnectionId);

            // Add user to groups based on their email and phone
            if (!string.IsNullOrEmpty(userEmail))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"email_{userEmail}");
                _logger.LogInformation("✅ User {UserId} added to group: email_{Email}", userId, userEmail);
            }

            if (!string.IsNullOrEmpty(userPhone))
            {
                // Normalize phone for group name
                var normalizedPhone = NormalizePhone(userPhone);
                await Groups.AddToGroupAsync(Context.ConnectionId, $"phone_{normalizedPhone}");
                _logger.LogInformation("✅ User {UserId} added to group: phone_{Phone}", userId, normalizedPhone);
            }

            // Add to user-specific group (for bulk invitation notifications)
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation("✅ User {UserId} added to group: user_{UserId}", userId, userId);

                // Add to sponsor group if user is sponsor
                if (Context.User.IsInRole("Sponsor"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"sponsor_{userId}");
                    _logger.LogInformation("✅ Sponsor {UserId} added to group: sponsor_{UserId}", userId, userId);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
            var userPhone = Context.User?.FindFirst(ClaimTypes.MobilePhone)?.Value;

            _logger.LogInformation("📡 SignalR NotificationHub disconnected - UserId: {UserId}, Email: {Email}, Phone: {Phone}, ConnectionId: {ConnectionId}, Exception: {Exception}",
                userId ?? "null", userEmail ?? "null", userPhone ?? "null", Context.ConnectionId, exception?.Message ?? "none");

            // Remove from groups
            if (!string.IsNullOrEmpty(userEmail))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"email_{userEmail}");
            }

            if (!string.IsNullOrEmpty(userPhone))
            {
                var normalizedPhone = NormalizePhone(userPhone);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"phone_{normalizedPhone}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Ping method to keep connection alive and test connectivity
        /// </summary>
        public Task Ping()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogDebug("🏓 Ping received from UserId: {UserId}, ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Normalize phone number for group matching
        /// </summary>
        private string NormalizePhone(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return phone;

            return phone
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("+", "");
        }
    }
}
