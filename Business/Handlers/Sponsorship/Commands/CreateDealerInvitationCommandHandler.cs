using Core.Entities.Concrete;
using Business.BusinessAspects;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using Entities.Concrete;
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
    /// <summary>
    /// Handler for creating dealer invitations (Invite or AutoCreate types)
    /// Authorization: Sponsor role only
    /// </summary>
    public class CreateDealerInvitationCommandHandler : IRequestHandler<CreateDealerInvitationCommand, IDataResult<DealerInvitationResponseDto>>
    {
        private readonly IDealerInvitationRepository _dealerInvitationRepository;
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ISubscriptionTierRepository _tierRepository;
        private readonly ILogger<CreateDealerInvitationCommandHandler> _logger;

        public CreateDealerInvitationCommandHandler(
            IDealerInvitationRepository dealerInvitationRepository,
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IUserRepository userRepository,
            IGroupRepository groupRepository,
            IUserGroupRepository userGroupRepository,
            ISubscriptionTierRepository tierRepository,
            ILogger<CreateDealerInvitationCommandHandler> logger)
        {
            _dealerInvitationRepository = dealerInvitationRepository;
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _userRepository = userRepository;
            _groupRepository = groupRepository;
            _userGroupRepository = userGroupRepository;
            _tierRepository = tierRepository;
            _logger = logger;
        }

        [SecuredOperation(Priority = 1)]

        public async Task<IDataResult<DealerInvitationResponseDto>> Handle(CreateDealerInvitationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("📨 Sponsor {SponsorId} creating dealer invitation (Type: {Type}, Tier: {Tier})",
                    request.SponsorId, request.InvitationType, request.PackageTier ?? "Any");

                // 1. Validate request
                if (request.InvitationType == "Invite" && string.IsNullOrWhiteSpace(request.Email))
                {
                    _logger.LogWarning("❌ Email required for Invite type");
                    return new ErrorDataResult<DealerInvitationResponseDto>("Email is required for Invite type.");
                }

                // 2. Validate tier if specified
                if (!string.IsNullOrEmpty(request.PackageTier))
                {
                    var validTiers = new[] { "S", "M", "L", "XL" };
                    if (!validTiers.Contains(request.PackageTier.ToUpper()))
                    {
                        _logger.LogWarning("❌ Invalid tier: {Tier}", request.PackageTier);
                        return new ErrorDataResult<DealerInvitationResponseDto>(
                            "Geçersiz paket tier. Geçerli değerler: S, M, L, XL");
                    }
                }

                // 3. Get available codes using intelligent selection
                var codesToReserve = await GetCodesToTransferAsync(
                    request.SponsorId,
                    request.CodeCount,
                    request.PackageTier,
                    request.PurchaseId);

                if (codesToReserve.Count < request.CodeCount)
                {
                    var filterMessage = BuildFilterMessage(request.PackageTier, request.PurchaseId);
                    
                    _logger.LogWarning("❌ Insufficient codes{FilterMsg}. Available: {Available}, Requested: {Requested}",
                        filterMessage, codesToReserve.Count, request.CodeCount);
                    
                    return new ErrorDataResult<DealerInvitationResponseDto>(
                        $"Yetersiz kod{filterMessage}. Mevcut: {codesToReserve.Count}, İstenen: {request.CodeCount}");
                }

                // 4. Create invitation token
                var invitationToken = Guid.NewGuid().ToString("N");

                // 5. Create dealer invitation entity
                var invitation = new DealerInvitation
                {
                    SponsorId = request.SponsorId,
                    Email = request.Email,
                    Phone = request.Phone,
                    DealerName = request.DealerName,
                    Status = "Pending",
                    InvitationType = request.InvitationType,
                    InvitationToken = invitationToken,
                    PurchaseId = request.PurchaseId,  // Store for backward compatibility
                    PackageTier = request.PackageTier?.ToUpper(),  // Store tier filter
                    CodeCount = request.CodeCount,
                    CreatedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(7) // 7 days expiry
                };

                // 6. Handle AutoCreate type - create dealer account immediately
                if (request.InvitationType == "AutoCreate")
                {
                    var autoPassword = GenerateRandomPassword();
                    
                    // Create dealer user account
                    var newDealer = new User
                    {
                        Email = request.Email,
                        FullName = request.DealerName,
                        Status = true
                    };

                    // Hash password
                    HashingHelper.CreatePasswordHash(autoPassword, out byte[] passwordHash, out byte[] passwordSalt);
                    newDealer.PasswordHash = passwordHash;
                    newDealer.PasswordSalt = passwordSalt;

                    // Save new dealer
                    var createdDealer = _userRepository.Add(newDealer);
                    await _userRepository.SaveChangesAsync();

                    _logger.LogInformation("✅ Created dealer account {UserId} for {Email}", 
                        createdDealer.UserId, request.Email);

                    // Assign Sponsor role to new dealer (using UserGroup entity)
                    var sponsorGroup = await _groupRepository.GetAsync(g => g.GroupName == "Sponsor");
                    if (sponsorGroup != null)
                    {
                        var userGroup = new UserGroup
                        {
                            UserId = createdDealer.UserId,
                            GroupId = sponsorGroup.Id
                        };
                        _userGroupRepository.Add(userGroup);
                        await _userGroupRepository.SaveChangesAsync();
                    }

                    // Update invitation with created dealer info
                    invitation.CreatedDealerId = createdDealer.UserId;
                    invitation.AutoCreatedPassword = autoPassword; // Store plain password for one-time retrieval
                    invitation.Status = "Accepted"; // Auto-accepted
                    invitation.AcceptedDate = DateTime.Now;

                    // Save invitation first to get ID
                    _dealerInvitationRepository.Add(invitation);
                    await _dealerInvitationRepository.SaveChangesAsync();

                    // Transfer codes immediately (without reservation, direct transfer)
                    await TransferCodesToDealer(codesToReserve, request.SponsorId, createdDealer.UserId);
                    
                    _logger.LogInformation("✅ Auto-transferred {Count} codes to dealer {DealerId}",
                        codesToReserve.Count, createdDealer.UserId);
                }
                else
                {
                    // For "Invite" type: Save invitation first, then reserve codes
                    _dealerInvitationRepository.Add(invitation);
                    await _dealerInvitationRepository.SaveChangesAsync();

                    // Reserve codes for this invitation
                    foreach (var code in codesToReserve)
                    {
                        code.ReservedForInvitationId = invitation.Id;
                        code.ReservedAt = DateTime.Now;
                        _sponsorshipCodeRepository.Update(code);
                    }
                    await _sponsorshipCodeRepository.SaveChangesAsync();

                    _logger.LogInformation("✅ Reserved {Count} codes for invitation {InvitationId}",
                        codesToReserve.Count, invitation.Id);
                }

                // 7. Build response
                var response = new DealerInvitationResponseDto
                {
                    InvitationId = invitation.Id,
                    InvitationToken = invitationToken,
                    InvitationLink = request.InvitationType == "Invite" 
                        ? $"https://ziraai.com/dealer-invitation?token={invitationToken}" 
                        : null,
                    Email = request.Email,
                    Phone = request.Phone,
                    DealerName = request.DealerName,
                    CodeCount = request.CodeCount,
                    Status = invitation.Status,
                    InvitationType = request.InvitationType,
                    AutoCreatedPassword = invitation.AutoCreatedPassword,
                    CreatedDealerId = invitation.CreatedDealerId,
                    CreatedAt = invitation.CreatedDate
                };

                var message = request.InvitationType == "AutoCreate"
                    ? $"Bayi hesabı oluşturuldu. Login: {request.Email}, Şifre: {invitation.AutoCreatedPassword}"
                    : $"Davetiye {request.Email} adresine gönderildi";

                return new SuccessDataResult<DealerInvitationResponseDto>(response, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating dealer invitation for sponsor {SponsorId}", request.SponsorId);
                return new ErrorDataResult<DealerInvitationResponseDto>("Bayilik daveti oluşturulurken hata oluştu");
            }
        }

        /// <summary>
        /// Intelligent code selection algorithm.
        /// Priority: 1) PurchaseId filter (if specified) 2) Tier filter (if specified) 3) Expiry date (FIFO) 4) Creation date (oldest first)
        /// Supports multi-purchase automatic selection.
        /// </summary>
        private async Task<List<SponsorshipCode>> GetCodesToTransferAsync(
            int sponsorId,
            int codeCount,
            string packageTier,
            int? purchaseId)
        {
            // Start with base query - available codes for sponsor
            var availableCodes = await _sponsorshipCodeRepository.GetListAsync(c =>
                c.SponsorId == sponsorId &&
                !c.IsUsed &&
                c.DealerId == null &&  // Not already transferred
                c.ReservedForInvitationId == null &&  // Not reserved
                c.ExpiryDate > DateTime.Now);  // Not expired

            var codesList = availableCodes.ToList();

            // Apply purchase filter if specified (backward compatibility)
            if (purchaseId.HasValue)
            {
                codesList = codesList
                    .Where(c => c.SponsorshipPurchaseId == purchaseId.Value)
                    .ToList();
            }

            // Apply tier filter if specified
            if (!string.IsNullOrEmpty(packageTier))
            {
                // Get tier ID for the specified tier string (S, M, L, XL)
                var tier = await _tierRepository.GetAsync(t => t.TierName == packageTier.ToUpper());
                
                if (tier != null)
                {
                    // Filter codes by tier
                    codesList = codesList
                        .Where(c => c.SubscriptionTierId == tier.Id)
                        .ToList();
                }
            }

            // Intelligent ordering:
            // 1. Codes expiring soonest first (prevent waste)
            // 2. Oldest codes first (FIFO for same expiry date)
            var selectedCodes = codesList
                .OrderBy(c => c.ExpiryDate)
                .ThenBy(c => c.CreatedDate)
                .Take(codeCount)
                .ToList();

            return selectedCodes;
        }

        private async Task TransferCodesToDealer(List<SponsorshipCode> codes, int sponsorId, int dealerId)
        {
            var transferTime = DateTime.Now;
            foreach (var code in codes)
            {
                code.DealerId = dealerId;
                code.TransferredAt = transferTime;
                code.TransferredByUserId = sponsorId;
                code.ReservedForInvitationId = null;  // Clear any reservation
                code.ReservedAt = null;
                _sponsorshipCodeRepository.Update(code);
            }
            await _sponsorshipCodeRepository.SaveChangesAsync();
        }

        private string BuildFilterMessage(string packageTier, int? purchaseId)
        {
            var filters = new List<string>();
            
            if (!string.IsNullOrEmpty(packageTier))
                filters.Add($"{packageTier} tier");
            
            if (purchaseId.HasValue)
                filters.Add($"PurchaseId: {purchaseId.Value}");

            return filters.Any() ? $" ({string.Join(", ", filters)})" : "";
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
