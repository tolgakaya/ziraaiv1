using Business.BusinessAspects;
using Core.Utilities.Results;
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
    /// Handler for transferring sponsorship codes from main sponsor to dealer
    /// Authorization: Sponsor role only
    /// </summary>
    public class TransferCodesToDealerCommandHandler : IRequestHandler<TransferCodesToDealerCommand, IDataResult<DealerCodeTransferResponseDto>>
    {
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISubscriptionTierRepository _tierRepository;
        private readonly ILogger<TransferCodesToDealerCommandHandler> _logger;

        public TransferCodesToDealerCommandHandler(
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IUserRepository userRepository,
            ISubscriptionTierRepository tierRepository,
            ILogger<TransferCodesToDealerCommandHandler> logger)
        {
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _userRepository = userRepository;
            _tierRepository = tierRepository;
            _logger = logger;
        }

        [SecuredOperation(Priority = 1)]

        public async Task<IDataResult<DealerCodeTransferResponseDto>> Handle(TransferCodesToDealerCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("📦 Sponsor {SponsorId} transferring codes to dealer {DealerId} (Tier: {Tier}, PurchaseId: {PurchaseId})",
                    request.UserId, request.DealerId, request.PackageTier ?? "Any", request.PurchaseId?.ToString() ?? "Any");

                // 1. Validate dealer exists and is a sponsor
                var dealer = await _userRepository.GetAsync(u => u.UserId == request.DealerId);
                if (dealer == null)
                {
                    _logger.LogWarning("❌ Dealer user {DealerId} not found", request.DealerId);
                    return new ErrorDataResult<DealerCodeTransferResponseDto>("Dealer user not found.");
                }

                // Check if dealer has Sponsor role
                var dealerGroups = await _userRepository.GetUserGroupsAsync(request.DealerId);
                if (!dealerGroups.Any(g => g == "Sponsor"))
                {
                    _logger.LogWarning("❌ User {DealerId} does not have Sponsor role", request.DealerId);
                    return new ErrorDataResult<DealerCodeTransferResponseDto>("Dealer must have Sponsor role to receive codes.");
                }

                // 2. Validate tier if specified
                if (!string.IsNullOrEmpty(request.PackageTier))
                {
                    var validTiers = new[] { "S", "M", "L", "XL" };
                    if (!validTiers.Contains(request.PackageTier.ToUpper()))
                    {
                        _logger.LogWarning("❌ Invalid tier: {Tier}", request.PackageTier);
                        return new ErrorDataResult<DealerCodeTransferResponseDto>(
                            "Geçersiz paket tier. Geçerli değerler: S, M, L, XL");
                    }
                }

                // 3. Get available codes using intelligent selection
                var codesToTransfer = await GetCodesToTransferAsync(
                    request.UserId,
                    request.CodeCount,
                    request.PackageTier,
                    request.PurchaseId);

                if (codesToTransfer.Count < request.CodeCount)
                {
                    var filterMessage = BuildFilterMessage(request.PackageTier, request.PurchaseId);
                    
                    _logger.LogWarning("❌ Insufficient codes{FilterMsg}. Available: {Available}, Requested: {Requested}",
                        filterMessage, codesToTransfer.Count, request.CodeCount);
                    
                    return new ErrorDataResult<DealerCodeTransferResponseDto>(
                        $"Yetersiz kod{filterMessage}. Mevcut: {codesToTransfer.Count}, İstenen: {request.CodeCount}");
                }

                // 4. Transfer codes to dealer
                var transferredCodeIds = new List<int>();
                var transferTime = DateTime.Now;

                foreach (var code in codesToTransfer)
                {
                    code.DealerId = request.DealerId;
                    code.TransferredAt = transferTime;
                    code.TransferredByUserId = request.UserId;
                    
                    _sponsorshipCodeRepository.Update(code);
                    transferredCodeIds.Add(code.Id);
                }

                await _sponsorshipCodeRepository.SaveChangesAsync();

                _logger.LogInformation("✅ Transferred {Count} codes to dealer {DealerId}",
                    transferredCodeIds.Count, request.DealerId);

                // 5. Return response
                var response = new DealerCodeTransferResponseDto
                {
                    TransferredCodeIds = transferredCodeIds,
                    TransferredCount = transferredCodeIds.Count,
                    DealerId = request.DealerId,
                    DealerName = dealer.FullName ?? "",
                    TransferredAt = transferTime
                };

                return new SuccessDataResult<DealerCodeTransferResponseDto>(
                    response, 
                    $"Bayiye başarıyla {response.TransferredCount} kod aktarıldı.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error transferring codes for sponsor {SponsorId}", request.UserId);
                return new ErrorDataResult<DealerCodeTransferResponseDto>("Kod aktarımı sırasında hata oluştu");
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

        private string BuildFilterMessage(string packageTier, int? purchaseId)
        {
            var filters = new List<string>();
            
            if (!string.IsNullOrEmpty(packageTier))
                filters.Add($"{packageTier} tier");
            
            if (purchaseId.HasValue)
                filters.Add($"PurchaseId: {purchaseId.Value}");

            return filters.Any() ? $" ({string.Join(", ", filters)})" : "";
        }
    }
}
