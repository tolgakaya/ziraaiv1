using Business.Hubs;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Business.Services.Notification
{
    /// <summary>
    /// Service for sending real-time farmer invitation notifications via SignalR
    /// </summary>
    public class FarmerInvitationNotificationService : IFarmerInvitationNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly ILogger<FarmerInvitationNotificationService> _logger;

        public FarmerInvitationNotificationService(
            IHubContext<NotificationHub> hubContext,
            ISponsorProfileRepository sponsorProfileRepository,
            ILogger<FarmerInvitationNotificationService> logger)
        {
            _hubContext = hubContext;
            _sponsorProfileRepository = sponsorProfileRepository;
            _logger = logger;
        }

        /// <summary>
        /// Send real-time notification when new farmer invitation is created
        /// </summary>
        public async Task NotifyNewInvitationAsync(Entities.Concrete.FarmerInvitation invitation)
        {
            try
            {
                _logger.LogInformation("📣 Sending SignalR notification for farmer invitation {InvitationId}",
                    invitation.Id);

                // Get sponsor info
                var sponsor = await _sponsorProfileRepository.GetAsync(s => s.SponsorId == invitation.SponsorId);

                if (sponsor == null)
                {
                    _logger.LogWarning("⚠️ Sponsor not found for invitation {InvitationId}, SponsorId: {SponsorId}",
                        invitation.Id, invitation.SponsorId);
                }

                var notificationData = new
                {
                    invitationId = invitation.Id,
                    token = invitation.InvitationToken,
                    sponsorName = sponsor?.CompanyName ?? "Unknown Sponsor",
                    codeCount = invitation.CodeCount,
                    packageTier = invitation.PackageTier,
                    expiresAt = invitation.ExpiryDate,
                    remainingDays = (invitation.ExpiryDate - DateTime.Now).Days,
                    status = invitation.Status,
                    farmerPhone = invitation.Phone,
                    createdAt = invitation.CreatedDate
                };

                // Send to phone group (farmer invitations only use phone)
                var tasks = new System.Collections.Generic.List<Task>();

                if (!string.IsNullOrEmpty(invitation.Phone))
                {
                    var normalizedPhone = NormalizePhone(invitation.Phone);
                    var phoneGroup = $"phone_{normalizedPhone}";
                    tasks.Add(_hubContext.Clients.Group(phoneGroup)
                        .SendAsync("NewFarmerInvitation", notificationData));
                    _logger.LogInformation("📱 Sending to phone group: {PhoneGroup}", phoneGroup);
                }
                else
                {
                    _logger.LogWarning("⚠️ No phone number for farmer invitation {InvitationId}", invitation.Id);
                }

                await Task.WhenAll(tasks);

                _logger.LogInformation("✅ SignalR notification sent successfully for invitation {InvitationId}",
                    invitation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Failed to send SignalR notification for invitation {InvitationId}",
                    invitation.Id);
                // Don't throw - notification failure shouldn't break invitation creation
            }
        }

        /// <summary>
        /// Normalize phone number for group matching (same logic as Hub)
        /// IMPORTANT: Must match NotificationHub.NormalizePhone() exactly
        /// </summary>
        private string NormalizePhone(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return phone;

            // Same normalization as NotificationHub - only remove special characters
            return phone
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("+", "");
        }
    }

    /// <summary>
    /// Interface for farmer invitation notification service
    /// </summary>
    public interface IFarmerInvitationNotificationService
    {
        Task NotifyNewInvitationAsync(Entities.Concrete.FarmerInvitation invitation);
    }
}
