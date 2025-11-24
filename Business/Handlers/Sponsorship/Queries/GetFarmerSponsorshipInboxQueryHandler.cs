using Business.Handlers.Sponsorship.Queries;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Handler for getting farmer's sponsorship inbox
    /// Pattern: Similar to GetDealerInvitationsQueryHandler
    /// No SecuredOperation - public endpoint using phone as identifier
    /// </summary>
    public class GetFarmerSponsorshipInboxQueryHandler
        : IRequestHandler<GetFarmerSponsorshipInboxQuery, IDataResult<List<FarmerSponsorshipInboxDto>>>
    {
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly ISubscriptionTierRepository _tierRepository;
        private readonly ILogger<GetFarmerSponsorshipInboxQueryHandler> _logger;

        public GetFarmerSponsorshipInboxQueryHandler(
            ISponsorshipCodeRepository codeRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            ISubscriptionTierRepository tierRepository,
            ILogger<GetFarmerSponsorshipInboxQueryHandler> logger)
        {
            _codeRepository = codeRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _tierRepository = tierRepository;
            _logger = logger;
        }

        public async Task<IDataResult<List<FarmerSponsorshipInboxDto>>> Handle(
            GetFarmerSponsorshipInboxQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("📥 [INBOX] Fetching sponsorship inbox for phone: {Phone}", request.Phone);

                // Step 1: Normalize phone number (same logic as SendSponsorshipLinkCommand)
                var normalizedPhone = FormatPhoneNumber(request.Phone);
                _logger.LogInformation("📞 [INBOX] Normalized phone: {NormalizedPhone}", normalizedPhone);

                // Step 2: Query codes sent to this phone with filters
                var codesEnumerable = await _codeRepository.GetListAsync(c =>
                    c.RecipientPhone == normalizedPhone &&
                    c.LinkDelivered == true &&
                    (request.IncludeUsed || !c.IsUsed) &&
                    (request.IncludeExpired || c.ExpiryDate > DateTime.Now));

                // Sort by sent date (newest first) and convert to list
                var codes = codesEnumerable.OrderByDescending(c => c.LinkSentDate).ToList();

                _logger.LogInformation("🔍 [INBOX] Applied filters - IncludeUsed: {IncludeUsed}, IncludeExpired: {IncludeExpired}",
                    request.IncludeUsed, request.IncludeExpired);

                _logger.LogInformation("📋 [INBOX] Found {Count} codes for phone {Phone}",
                    codes.Count, normalizedPhone);

                if (codes.Count == 0)
                {
                    return new SuccessDataResult<List<FarmerSponsorshipInboxDto>>(
                        new List<FarmerSponsorshipInboxDto>(),
                        "Henüz sponsorluk kodu gönderilmemiş");
                }

                // Step 5: Get sponsor names (batch query for performance)
                var sponsorIds = codes.Select(c => c.SponsorId).Distinct().ToList();
                var sponsorsEnumerable = await _sponsorProfileRepository.GetListAsync(s =>
                    sponsorIds.Contains(s.SponsorId));
                var sponsors = sponsorsEnumerable.ToList();

                _logger.LogInformation("👥 [INBOX] Loaded {Count} sponsor profiles", sponsors.Count);

                // Step 6: Get tier names (batch query for performance)
                var tierIds = codes.Select(c => c.SubscriptionTierId).Distinct().ToList();
                var tiersEnumerable = await _tierRepository.GetListAsync(t =>
                    tierIds.Contains(t.Id));
                var tiers = tiersEnumerable.ToList();

                _logger.LogInformation("🎯 [INBOX] Loaded {Count} subscription tiers", tiers.Count);

                // Step 7: Map to DTOs
                var result = codes.Select(code => new FarmerSponsorshipInboxDto
                {
                    Code = code.Code,
                    SponsorName = sponsors
                        .FirstOrDefault(s => s.SponsorId == code.SponsorId)
                        ?.CompanyName ?? "Unknown Sponsor",
                    TierName = tiers
                        .FirstOrDefault(t => t.Id == code.SubscriptionTierId)
                        ?.TierName ?? "Unknown",
                    SentDate = code.LinkSentDate ?? code.CreatedDate,
                    SentVia = code.LinkSentVia ?? "SMS",
                    IsUsed = code.IsUsed,
                    UsedDate = code.UsedDate,
                    ExpiryDate = code.ExpiryDate,
                    RedemptionLink = code.RedemptionLink,
                    RecipientName = code.RecipientName
                }).ToList();

                _logger.LogInformation("✅ [INBOX] Successfully mapped {Count} codes to DTOs", result.Count);

                return new SuccessDataResult<List<FarmerSponsorshipInboxDto>>(
                    result,
                    $"{result.Count} sponsorluk kodu bulundu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [INBOX] Error fetching sponsorship inbox for phone: {Phone}",
                    request.Phone);
                return new ErrorDataResult<List<FarmerSponsorshipInboxDto>>(
                    "Sponsorluk kutusu yüklenirken hata oluştu");
            }
        }

        /// <summary>
        /// Format phone number to normalized format (+905551234567)
        /// IMPORTANT: Must match SendSponsorshipLinkCommand.FormatPhoneNumber() logic
        /// </summary>
        private string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            // Remove all non-numeric characters
            var cleaned = new string(phone.Where(char.IsDigit).ToArray());

            // Add Turkey country code if not present
            if (!cleaned.StartsWith("90") && cleaned.Length == 10)
            {
                cleaned = "90" + cleaned;
            }

            // Add + prefix
            if (!cleaned.StartsWith("+"))
            {
                cleaned = "+" + cleaned;
            }

            return cleaned;
        }
    }
}
