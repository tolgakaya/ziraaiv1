using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
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

namespace Business.Handlers.Sponsorship.Commands
{
    public class AcceptFarmerInvitationCommand : IRequest<IDataResult<FarmerInvitationAcceptResponseDto>>
    {
        public string InvitationToken { get; set; }
        public int CurrentUserId { get; set; } // From JWT
        public string CurrentUserPhone { get; set; } // From JWT (for phone matching)

        public class AcceptFarmerInvitationCommandHandler : IRequestHandler<AcceptFarmerInvitationCommand, IDataResult<FarmerInvitationAcceptResponseDto>>
        {
            private readonly IFarmerInvitationRepository _invitationRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ILogger<AcceptFarmerInvitationCommandHandler> _logger;

            public AcceptFarmerInvitationCommandHandler(
                IFarmerInvitationRepository invitationRepository,
                ISponsorshipCodeRepository codeRepository,
                ILogger<AcceptFarmerInvitationCommandHandler> logger)
            {
                _invitationRepository = invitationRepository;
                _codeRepository = codeRepository;
                _logger = logger;
            }

            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<FarmerInvitationAcceptResponseDto>> Handle(AcceptFarmerInvitationCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("🎯 User {UserId} (Phone: {Phone}) attempting to accept farmer invitation with token {Token}",
                        request.CurrentUserId, request.CurrentUserPhone ?? "null", request.InvitationToken);

                    // 1. Find invitation by token (WITH TRACKING for update)
                    var invitation = await _invitationRepository.GetTrackedAsync(i =>
                        i.InvitationToken == request.InvitationToken &&
                        i.Status == "Pending");

                    if (invitation == null)
                    {
                        _logger.LogWarning("❌ Invitation not found or not pending. Token: {Token}", request.InvitationToken);
                        return new ErrorDataResult<FarmerInvitationAcceptResponseDto>(
                            "Davetiye bulunamadı veya daha önce kabul edilmiş/reddedilmiş");
                    }

                    // 2. Check expiry
                    if (invitation.ExpiryDate < DateTime.Now)
                    {
                        _logger.LogWarning("❌ Invitation {InvitationId} expired. Expiry: {Expiry}",
                            invitation.Id, invitation.ExpiryDate);

                        invitation.Status = "Expired";
                        await _invitationRepository.SaveChangesAsync();

                        return new ErrorDataResult<FarmerInvitationAcceptResponseDto>(
                            "Davetiye süresi dolmuş. Lütfen sponsor ile iletişime geçin");
                    }

                    // 3. Verify phone match (security check)
                    // Normalize both phones for comparison (remove spaces, dashes, parentheses, plus sign)
                    var invitationPhoneNormalized = NormalizePhoneNumber(invitation.Phone);
                    var userPhoneNormalized = NormalizePhoneNumber(request.CurrentUserPhone);

                    if (!invitationPhoneNormalized.Equals(userPhoneNormalized, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("❌ Phone mismatch. Invitation Phone: {InvPhone} (normalized: {InvNorm}) | User Phone: {UserPhone} (normalized: {UserNorm})",
                            invitation.Phone, invitationPhoneNormalized,
                            request.CurrentUserPhone, userPhoneNormalized);

                        return new ErrorDataResult<FarmerInvitationAcceptResponseDto>(
                            "Bu davetiye size ait değil");
                    }

                    _logger.LogInformation("✅ Phone verified. Proceeding with acceptance.");

                    // 4. Get reserved codes for this invitation
                    var reservedCodes = await _codeRepository.GetListAsync(c =>
                        c.ReservedForFarmerInvitationId == invitation.Id);

                    var codesToAssign = reservedCodes.ToList();

                    // Fallback: If no reserved codes or insufficient, get fresh codes
                    if (codesToAssign.Count < invitation.CodeCount)
                    {
                        _logger.LogWarning("⚠️ Reserved codes insufficient or missing. Fetching fresh codes. Reserved: {Reserved}, Needed: {Needed}",
                            codesToAssign.Count, invitation.CodeCount);

                        var freshCodes = await _codeRepository.GetListAsync(c =>
                            c.SponsorId == invitation.SponsorId &&
                            !c.IsUsed &&
                            c.DealerId == null &&
                            c.FarmerInvitationId == null &&
                            c.ReservedForInvitationId == null &&
                            c.ReservedForFarmerInvitationId == null &&
                            c.ExpiryDate > DateTime.Now);

                        var additionalNeeded = invitation.CodeCount - codesToAssign.Count;
                        var freshCodesList = freshCodes
                            .OrderBy(c => c.ExpiryDate)  // Expiring soonest first
                            .ThenBy(c => c.CreatedDate)  // FIFO
                            .Take(additionalNeeded)
                            .ToList();

                        codesToAssign.AddRange(freshCodesList);
                    }

                    if (codesToAssign.Count < invitation.CodeCount)
                    {
                        _logger.LogWarning("⚠️ Not enough codes available. Requested: {Requested}, Available: {Available}",
                            invitation.CodeCount, codesToAssign.Count);

                        return new ErrorDataResult<FarmerInvitationAcceptResponseDto>(
                            $"Yetersiz kod. İstenen: {invitation.CodeCount}, Mevcut: {codesToAssign.Count}");
                    }

                    _logger.LogInformation("📦 Assigning {Count} codes to farmer {FarmerId}",
                        codesToAssign.Count, request.CurrentUserId);

                    // 5. Assign codes to farmer and populate statistics fields
                    // CRITICAL: Populate same fields as SendSponsorshipLinkCommand for backward compatibility
                    var now = DateTime.Now;
                    foreach (var code in codesToAssign)
                    {
                        // Link to farmer invitation
                        code.FarmerInvitationId = invitation.Id;

                        // Clear reservation fields
                        code.ReservedForFarmerInvitationId = null;
                        code.ReservedForFarmerAt = null;

                        // CRITICAL: Populate statistics-required fields
                        // These fields are used by GetLinkStatisticsQuery, GetPackageDistributionStatisticsQuery, etc.
                        code.LinkSentDate = invitation.LinkSentDate ?? now;  // When SMS was sent
                        code.DistributionDate = now;  // When code was distributed (assigned) to farmer
                        code.DistributionChannel = "FarmerInvitation";  // Channel identifier
                        code.DistributedTo = request.CurrentUserPhone;  // Farmer's phone

                        _codeRepository.Update(code);
                    }

                    await _codeRepository.SaveChangesAsync();

                    _logger.LogInformation("✅ Assigned {Count} codes successfully", codesToAssign.Count);

                    // 6. Update invitation status
                    invitation.Status = "Accepted";
                    invitation.AcceptedDate = now;
                    invitation.AcceptedByUserId = request.CurrentUserId;

                    await _invitationRepository.SaveChangesAsync();

                    _logger.LogInformation("✅ Farmer invitation {InvitationId} accepted by user {UserId}",
                        invitation.Id, request.CurrentUserId);

                    // 7. Build response with actual sponsorship codes
                    var codeStrings = codesToAssign.Select(c => c.Code).ToList();
                    var codesByTier = codesToAssign
                        .GroupBy(c => c.SubscriptionTierId)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count());

                    var response = new FarmerInvitationAcceptResponseDto
                    {
                        InvitationId = invitation.Id,
                        Status = invitation.Status,
                        AcceptedDate = invitation.AcceptedDate.Value,
                        TotalCodesAssigned = codesToAssign.Count,
                        SponsorshipCodes = codeStrings,
                        CodesByTier = codesByTier,
                        Message = $"✅ Tebrikler! {codesToAssign.Count} adet sponsorluk kodu hesabınıza tanımlandı."
                    };

                    return new SuccessDataResult<FarmerInvitationAcceptResponseDto>(response,
                        "Sponsorluk daveti başarıyla kabul edildi");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error accepting farmer invitation. Token: {Token}, User: {UserId}",
                        request.InvitationToken, request.CurrentUserId);
                    return new ErrorDataResult<FarmerInvitationAcceptResponseDto>(
                        "Davetiye kabul edilirken hata oluştu");
                }
            }

            /// <summary>
            /// Normalize phone number for comparison
            /// Handles Turkish (0) vs international (+90) formats
            /// Examples:
            ///   +90 555 686 6386 → 05556866386
            ///   +905556866386 → 05556866386
            ///   0555-686-6386 → 05556866386
            ///   905556866386 → 05556866386
            /// </summary>
            private string NormalizePhoneNumber(string phone)
            {
                if (string.IsNullOrEmpty(phone))
                    return phone;

                // Remove formatting characters
                var normalized = phone
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("(", "")
                    .Replace(")", "")
                    .Replace("+", "");

                // Convert international format (90xxx) to Turkish format (0xxx)
                if (normalized.StartsWith("90") && normalized.Length == 12)
                {
                    normalized = "0" + normalized.Substring(2);
                }

                return normalized;
            }
        }
    }
}
