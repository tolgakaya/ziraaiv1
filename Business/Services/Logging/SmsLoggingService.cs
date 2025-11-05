using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Services.Logging
{
    /// <summary>
    /// Service for logging SMS content to database
    /// Controlled by configuration flag: SmsLogging:Enabled
    /// </summary>
    public interface ISmsLoggingService
    {
        Task LogDealerInviteAsync(string phone, string message, int sponsorId, int dealerId, int? senderUserId, object additionalData = null);
        Task LogCodeDistributeAsync(string phone, string message, string code, int sponsorId, int? senderUserId, object additionalData = null);
        Task LogReferralAsync(string phone, string message, string referralCode, int userId, int? senderUserId, object additionalData = null);
    }

    public class SmsLoggingService : ISmsLoggingService
    {
        private readonly ISmsLogRepository _smsLogRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsLoggingService> _logger;
        private readonly bool _isEnabled;

        public SmsLoggingService(
            ISmsLogRepository smsLogRepository,
            IConfiguration configuration,
            ILogger<SmsLoggingService> logger)
        {
            _smsLogRepository = smsLogRepository;
            _configuration = configuration;
            _logger = logger;
            
            // Read config flag (default: false for safety)
            _isEnabled = _configuration.GetValue<bool>("SmsLogging:Enabled", false);
        }

        public async Task LogDealerInviteAsync(string phone, string message, int sponsorId, int dealerId, int? senderUserId, object additionalData = null)
        {
            if (!_isEnabled) return;

            try
            {
                var content = new
                {
                    phone,
                    message,
                    sponsorId,
                    dealerId,
                    timestamp = DateTime.Now,
                    additionalData
                };

                await LogAsync("DealerInvite", senderUserId, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log dealer invite SMS");
                // Don't throw - logging failure shouldn't break the main flow
            }
        }

        public async Task LogCodeDistributeAsync(string phone, string message, string code, int sponsorId, int? senderUserId, object additionalData = null)
        {
            if (!_isEnabled) return;

            try
            {
                var content = new
                {
                    phone,
                    message,
                    code,
                    sponsorId,
                    timestamp = DateTime.Now,
                    additionalData
                };

                await LogAsync("CodeDistribute", senderUserId, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log code distribution SMS");
                // Don't throw - logging failure shouldn't break the main flow
            }
        }

        public async Task LogReferralAsync(string phone, string message, string referralCode, int userId, int? senderUserId, object additionalData = null)
        {
            if (!_isEnabled) return;

            try
            {
                var content = new
                {
                    phone,
                    message,
                    referralCode,
                    userId,
                    timestamp = DateTime.Now,
                    additionalData
                };

                await LogAsync("Referral", senderUserId, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log referral SMS");
                // Don't throw - logging failure shouldn't break the main flow
            }
        }

        private async Task LogAsync(string action, int? senderUserId, object content)
        {
            var smsLog = new SmsLog
            {
                Action = action,
                SenderUserId = senderUserId,
                Content = JsonSerializer.Serialize(content, new JsonSerializerOptions
                {
                    WriteIndented = true, // Pretty print for easier reading
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                CreatedDate = DateTime.Now
            };

            _smsLogRepository.Add(smsLog);
            await _smsLogRepository.SaveChangesAsync();

            _logger.LogDebug("SMS logged - Action: {Action}, SenderUserId: {SenderUserId}, Content length: {Length}",
                action, senderUserId, smsLog.Content.Length);
        }
    }
}
