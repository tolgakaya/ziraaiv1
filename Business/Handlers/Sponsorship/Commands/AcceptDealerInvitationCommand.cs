using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    public class AcceptDealerInvitationCommand : IRequest<IDataResult<DealerInvitationAcceptResponseDto>>
    {
        public string InvitationToken { get; set; }
        public int CurrentUserId { get; set; } // From JWT
        public string CurrentUserEmail { get; set; } // From JWT (nullable - may be null for phone login)
        public string CurrentUserPhone { get; set; } // From JWT (nullable - may be null for email login)

        public class AcceptDealerInvitationCommandHandler : IRequestHandler<AcceptDealerInvitationCommand, IDataResult<DealerInvitationAcceptResponseDto>>
        {
            private readonly IDealerInvitationRepository _invitationRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly IUserRepository _userRepository;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly IGroupRepository _groupRepository;
            private readonly ILogger<AcceptDealerInvitationCommandHandler> _logger;

            public AcceptDealerInvitationCommandHandler(
                IDealerInvitationRepository invitationRepository,
                ISponsorshipCodeRepository codeRepository,
                IUserRepository userRepository,
                IUserGroupRepository userGroupRepository,
                IGroupRepository groupRepository,
                ILogger<AcceptDealerInvitationCommandHandler> logger)
            {
                _invitationRepository = invitationRepository;
                _codeRepository = codeRepository;
                _userRepository = userRepository;
                _userGroupRepository = userGroupRepository;
                _groupRepository = groupRepository;
                _logger = logger;
            }

            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<DealerInvitationAcceptResponseDto>> Handle(AcceptDealerInvitationCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("🎯 User {UserId} (Email: {Email}, Phone: {Phone}) attempting to accept dealer invitation with token {Token}",
                        request.CurrentUserId, request.CurrentUserEmail ?? "null", request.CurrentUserPhone ?? "null", request.InvitationToken);

                    // 1. Find invitation by token
                    var invitation = await _invitationRepository.GetAsync(i =>
                        i.InvitationToken == request.InvitationToken &&
                        i.Status == "Pending");

                    if (invitation == null)
                    {
                        _logger.LogWarning("❌ Invitation not found or not pending. Token: {Token}", request.InvitationToken);
                        return new ErrorDataResult<DealerInvitationAcceptResponseDto>(
                            "Davetiye bulunamadı veya daha önce kabul edilmiş/reddedilmiş");
                    }

                    // 2. Check expiry
                    if (invitation.ExpiryDate < DateTime.Now)
                    {
                        _logger.LogWarning("❌ Invitation {InvitationId} expired. Expiry: {Expiry}",
                            invitation.Id, invitation.ExpiryDate);

                        invitation.Status = "Expired";
                        await _invitationRepository.SaveChangesAsync();

                        return new ErrorDataResult<DealerInvitationAcceptResponseDto>(
                            "Davetiye süresi dolmuş. Lütfen sponsor ile iletişime geçin");
                    }

                    // 3. Verify email OR phone match (security check)
                    // v2.1: Support both email and phone login methods
                    bool isAuthorized = false;
                    string matchedBy = "";

                    // Check email match (if invitation has email and user logged in with email)
                    if (!string.IsNullOrEmpty(invitation.Email) && !string.IsNullOrEmpty(request.CurrentUserEmail))
                    {
                        if (invitation.Email.Equals(request.CurrentUserEmail, StringComparison.OrdinalIgnoreCase))
                        {
                            isAuthorized = true;
                            matchedBy = "email";
                        }
                    }

                    // Check phone match (if invitation has phone and user logged in with phone)
                    if (!string.IsNullOrEmpty(invitation.Phone) && !string.IsNullOrEmpty(request.CurrentUserPhone))
                    {
                        // Normalize both phones for comparison (remove spaces, dashes, parentheses, plus sign)
                        var invitationPhoneNormalized = NormalizePhoneNumber(invitation.Phone);
                        var userPhoneNormalized = NormalizePhoneNumber(request.CurrentUserPhone);

                        if (invitationPhoneNormalized.Equals(userPhoneNormalized, StringComparison.OrdinalIgnoreCase))
                        {
                            isAuthorized = true;
                            matchedBy = "phone";
                        }
                    }

                    if (!isAuthorized)
                    {
                        _logger.LogWarning("❌ Authorization failed. Invitation Email: {InvEmail}, Phone: {InvPhone} | User Email: {UserEmail}, Phone: {UserPhone}",
                            invitation.Email ?? "null", invitation.Phone ?? "null",
                            request.CurrentUserEmail ?? "null", request.CurrentUserPhone ?? "null");

                        return new ErrorDataResult<DealerInvitationAcceptResponseDto>(
                            "Bu davetiye size ait değil");
                    }

                    _logger.LogInformation("✅ User authorized by {MatchedBy}. Proceeding with acceptance.", matchedBy);

                    // 4. Check if user has Sponsor role, if not assign it
                    var sponsorGroup = await _groupRepository.GetAsync(g => g.GroupName == "Sponsor");
                    if (sponsorGroup == null)
                    {
                        _logger.LogError("❌ Sponsor group not found in database");
                        return new ErrorDataResult<DealerInvitationAcceptResponseDto>(
                            "Sistem hatası: Sponsor rolü bulunamadı");
                    }

                    var hasSponsorRole = await _userGroupRepository.GetAsync(ug =>
                        ug.UserId == request.CurrentUserId &&
                        ug.GroupId == sponsorGroup.Id);

                    if (hasSponsorRole == null)
                    {
                        _logger.LogInformation("➕ Assigning Sponsor role to user {UserId}", request.CurrentUserId);

                        var userGroup = new Core.Entities.Concrete.UserGroup
                        {
                            UserId = request.CurrentUserId,
                            GroupId = sponsorGroup.Id
                        };

                        _userGroupRepository.Add(userGroup);
                        await _userGroupRepository.SaveChangesAsync();

                        _logger.LogInformation("✅ Sponsor role assigned to user {UserId}", request.CurrentUserId);
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ User {UserId} already has Sponsor role", request.CurrentUserId);
                    }

                    // 5. Get reserved codes for this invitation (or fallback to fresh codes)
                    var reservedCodes = await _codeRepository.GetListAsync(c =>
                        c.ReservedForInvitationId == invitation.Id);

                    var codesToTransfer = reservedCodes.ToList();

                    // Fallback: If no reserved codes or insufficient, get fresh codes
                    if (codesToTransfer.Count < invitation.CodeCount)
                    {
                        _logger.LogWarning("⚠️ Reserved codes insufficient or missing. Fetching fresh codes. Reserved: {Reserved}, Needed: {Needed}",
                            codesToTransfer.Count, invitation.CodeCount);

                        var freshCodes = await _codeRepository.GetListAsync(c =>
                            c.SponsorId == invitation.SponsorId &&
                            !c.IsUsed &&
                            c.DealerId == null &&
                            c.ReservedForInvitationId == null &&
                            c.ExpiryDate > DateTime.Now);

                        var additionalNeeded = invitation.CodeCount - codesToTransfer.Count;
                        var freshCodesList = freshCodes
                            .OrderBy(c => c.ExpiryDate)  // Expiring soonest first
                            .ThenBy(c => c.CreatedDate)  // FIFO
                            .Take(additionalNeeded)
                            .ToList();

                        codesToTransfer.AddRange(freshCodesList);
                    }

                    if (codesToTransfer.Count < invitation.CodeCount)
                    {
                        _logger.LogWarning("⚠️ Not enough codes available. Requested: {Requested}, Available: {Available}",
                            invitation.CodeCount, codesToTransfer.Count);

                        return new ErrorDataResult<DealerInvitationAcceptResponseDto>(
                            $"Yetersiz kod. İstenen: {invitation.CodeCount}, Mevcut: {codesToTransfer.Count}");
                    }

                    _logger.LogInformation("📦 Transferring {Count} codes to dealer {DealerId}",
                        codesToTransfer.Count, request.CurrentUserId);

                    foreach (var code in codesToTransfer)
                    {
                        code.DealerId = request.CurrentUserId;
                        code.TransferredAt = DateTime.Now;
                        code.TransferredByUserId = invitation.SponsorId;
                        code.ReservedForInvitationId = null;  // Clear reservation
                        code.ReservedAt = null;
                        _codeRepository.Update(code);
                    }

                    await _codeRepository.SaveChangesAsync();

                    _logger.LogInformation("✅ Transferred {Count} codes successfully", codesToTransfer.Count);

                    // 6. Update invitation status
                    invitation.Status = "Accepted";
                    invitation.AcceptedDate = DateTime.Now;
                    invitation.CreatedDealerId = request.CurrentUserId;

                    await _invitationRepository.SaveChangesAsync();

                    _logger.LogInformation("✅ Dealer invitation {InvitationId} accepted by user {UserId}",
                        invitation.Id, request.CurrentUserId);

                    // 7. Build response
                    var response = new DealerInvitationAcceptResponseDto
                    {
                        InvitationId = invitation.Id,
                        DealerId = request.CurrentUserId,
                        TransferredCodeCount = codesToTransfer.Count,
                        TransferredCodeIds = codesToTransfer.Select(c => c.Id).ToList(),
                        AcceptedAt = invitation.AcceptedDate.Value,
                        Message = $"✅ Tebrikler! {codesToTransfer.Count} adet kod hesabınıza transfer edildi. Artık bu kodları çiftçilere dağıtabilirsiniz."
                    };

                    return new SuccessDataResult<DealerInvitationAcceptResponseDto>(response,
                        "Bayilik daveti başarıyla kabul edildi");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error accepting dealer invitation. Token: {Token}, User: {UserId}",
                        request.InvitationToken, request.CurrentUserId);
                    return new ErrorDataResult<DealerInvitationAcceptResponseDto>(
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

    public class DealerInvitationAcceptResponseDto
    {
        public int InvitationId { get; set; }
        public int DealerId { get; set; }
        public int TransferredCodeCount { get; set; }
        public List<int> TransferredCodeIds { get; set; }
        public DateTime AcceptedAt { get; set; }
        public string Message { get; set; }
    }
}
