using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public class SponsorshipService : ISponsorshipService
    {
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly ISponsorshipPurchaseRepository _sponsorshipPurchaseRepository;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ISubscriptionTierRepository _subscriptionTierRepository;
        private readonly IUserRepository _userRepository;

        public SponsorshipService(
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            ISponsorshipPurchaseRepository sponsorshipPurchaseRepository,
            IUserSubscriptionRepository userSubscriptionRepository,
            ISubscriptionTierRepository subscriptionTierRepository,
            IUserRepository userRepository)
        {
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _sponsorshipPurchaseRepository = sponsorshipPurchaseRepository;
            _userSubscriptionRepository = userSubscriptionRepository;
            _subscriptionTierRepository = subscriptionTierRepository;
            _userRepository = userRepository;
        }

        public async Task<IDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>> PurchaseBulkSubscriptionsAsync(
            int sponsorId, int tierId, int quantity, decimal amount, string paymentReference)
        {
            try
            {
                // Get sponsor information
                var sponsor = await _userRepository.GetAsync(u => u.UserId == sponsorId);
                if (sponsor == null)
                    return new ErrorDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>("Sponsor not found");

                // Get subscription tier
                var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == tierId);
                if (tier == null)
                    return new ErrorDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>("Subscription tier not found");

                // Validate quantity limits
                if (quantity < tier.MinPurchaseQuantity)
                {
                    return new ErrorDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>(
                        $"Quantity must be at least {tier.MinPurchaseQuantity} for {tier.DisplayName} tier");
                }
                
                if (quantity > tier.MaxPurchaseQuantity)
                {
                    return new ErrorDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>(
                        $"Quantity cannot exceed {tier.MaxPurchaseQuantity} for {tier.DisplayName} tier");
                }

                // Create purchase record
                var purchase = new SponsorshipPurchase
                {
                    SponsorId = sponsorId,
                    SubscriptionTierId = tierId,
                    Quantity = quantity,
                    UnitPrice = tier.MonthlyPrice,
                    TotalAmount = amount,
                    Currency = tier.Currency,
                    PurchaseDate = DateTime.Now,
                    PaymentMethod = "CreditCard",
                    PaymentReference = paymentReference,
                    PaymentStatus = "Completed",
                    PaymentCompletedDate = DateTime.Now,
                    CompanyName = sponsor.FullName,
                    CodePrefix = "AGRI",
                    ValidityDays = 365,
                    Status = "Active",
                    CreatedDate = DateTime.Now,
                    CodesGenerated = 0,
                    CodesUsed = 0
                };

                _sponsorshipPurchaseRepository.Add(purchase);
                await _sponsorshipPurchaseRepository.SaveChangesAsync();

                // Generate codes
                var codes = await _sponsorshipCodeRepository.GenerateCodesAsync(
                    purchase.Id, sponsorId, tierId, quantity, purchase.CodePrefix, purchase.ValidityDays);

                // Update codes generated count
                purchase.CodesGenerated = codes.Count;
                _sponsorshipPurchaseRepository.Update(purchase);
                await _sponsorshipPurchaseRepository.SaveChangesAsync();

                // Create response DTO with codes
                var response = new Entities.Dtos.SponsorshipPurchaseResponseDto
                {
                    Id = purchase.Id,
                    SponsorId = purchase.SponsorId,
                    SubscriptionTierId = purchase.SubscriptionTierId,
                    Quantity = purchase.Quantity,
                    UnitPrice = purchase.UnitPrice,
                    TotalAmount = purchase.TotalAmount,
                    Currency = purchase.Currency,
                    PurchaseDate = purchase.PurchaseDate,
                    PaymentMethod = purchase.PaymentMethod,
                    PaymentReference = purchase.PaymentReference,
                    PaymentStatus = purchase.PaymentStatus,
                    PaymentCompletedDate = purchase.PaymentCompletedDate,
                    CompanyName = purchase.CompanyName,
                    CodesGenerated = purchase.CodesGenerated,
                    CodesUsed = purchase.CodesUsed,
                    CodePrefix = purchase.CodePrefix,
                    ValidityDays = purchase.ValidityDays,
                    Status = purchase.Status,
                    CreatedDate = purchase.CreatedDate,
                    GeneratedCodes = codes.Select(c => new Entities.Dtos.SponsorshipCodeDto
                    {
                        Id = c.Id,
                        Code = c.Code,
                        TierName = tier.TierName,
                        IsUsed = c.IsUsed,
                        IsActive = c.IsActive,
                        ExpiryDate = c.ExpiryDate,
                        UsedDate = c.UsedDate,
                        UsedByUserId = c.UsedByUserId,
                        UsedByUserName = null, // Navigation property removed - fetch separately if needed
                        Notes = c.Notes
                    }).ToList()
                };

                return new SuccessDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>(response, 
                    $"{quantity} sponsorship codes generated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SponsorshipService] ERROR creating sponsorship purchase: {ex.Message}");
                Console.WriteLine($"[SponsorshipService] Stack trace: {ex.StackTrace}");
                
                // Log inner exception details
                var innerEx = ex.InnerException;
                var level = 1;
                while (innerEx != null)
                {
                    Console.WriteLine($"[SponsorshipService] Inner Exception {level}: {innerEx.Message}");
                    if (innerEx.Data?.Count > 0)
                    {
                        Console.WriteLine($"[SponsorshipService] Inner Exception {level} Data:");
                        foreach (var key in innerEx.Data.Keys)
                        {
                            Console.WriteLine($"[SponsorshipService]   {key}: {innerEx.Data[key]}");
                        }
                    }
                    innerEx = innerEx.InnerException;
                    level++;
                }
                
                return new ErrorDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>($"Error creating sponsorship purchase: {ex.Message}");
            }
        }

        public async Task<IDataResult<List<SponsorshipCode>>> GenerateCodesForPurchaseAsync(int purchaseId)
        {
            try
            {
                var purchase = await _sponsorshipPurchaseRepository.GetAsync(p => p.Id == purchaseId);
                if (purchase == null)
                    return new ErrorDataResult<List<SponsorshipCode>>("Purchase not found");

                var codes = await _sponsorshipCodeRepository.GenerateCodesAsync(
                    purchaseId, purchase.SponsorId, purchase.SubscriptionTierId, 
                    purchase.Quantity, purchase.CodePrefix, purchase.ValidityDays);

                return new SuccessDataResult<List<SponsorshipCode>>(codes, 
                    $"{codes.Count} codes generated successfully");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<List<SponsorshipCode>>($"Error generating codes: {ex.Message}");
            }
        }

        public async Task<IDataResult<UserSubscription>> RedeemSponsorshipCodeAsync(string code, int userId)
        {
            try
            {
                // Validate code
                var sponsorshipCode = await _sponsorshipCodeRepository.GetUnusedCodeAsync(code);
                if (sponsorshipCode == null)
                    return new ErrorDataResult<UserSubscription>("Invalid or expired sponsorship code");

                // Check for active sponsored subscription (NO multiple active sponsorships allowed)
                var existingSubscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
                
                bool hasActiveSponsorshipOrPaid = existingSubscription != null && 
                                                   existingSubscription.IsSponsoredSubscription && 
                                                   existingSubscription.QueueStatus == SubscriptionQueueStatus.Active;

                if (hasActiveSponsorshipOrPaid)
                {
                    // Queue the new sponsorship - it will activate when current expires
                    return await QueueSponsorship(code, userId, sponsorshipCode, existingSubscription.Id);
                }

                // Allow immediate activation for Trial users or no active subscription
                return await ActivateSponsorship(code, userId, sponsorshipCode, existingSubscription);
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<UserSubscription>($"Error redeeming sponsorship code: {ex.Message}");
            }
        }

        /// <summary>
        /// Queue a sponsorship for later activation (when current sponsorship expires)
        /// </summary>
        private async Task<IDataResult<UserSubscription>> QueueSponsorship(
            string code, 
            int userId, 
            SponsorshipCode sponsorshipCode, 
            int previousSponsorshipId)
        {
            try
            {
                var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == sponsorshipCode.SubscriptionTierId);
                if (tier == null)
                    return new ErrorDataResult<UserSubscription>("Subscription tier not found");

                var queuedSubscription = new UserSubscription
                {
                    UserId = userId,
                    SubscriptionTierId = sponsorshipCode.SubscriptionTierId,
                    QueueStatus = SubscriptionQueueStatus.Pending,
                    QueuedDate = DateTime.Now,
                    PreviousSponsorshipId = previousSponsorshipId,
                    IsActive = false,  // Not active yet
                    AutoRenew = false,
                    PaymentMethod = "Sponsorship",
                    PaymentReference = code,
                    PaidAmount = 0,
                    Currency = tier.Currency,
                    CurrentDailyUsage = 0,
                    CurrentMonthlyUsage = 0,
                    Status = "Pending",
                    IsTrialSubscription = false,
                    IsSponsoredSubscription = true,
                    SponsorshipCodeId = sponsorshipCode.Id,
                    SponsorId = sponsorshipCode.SponsorId,
                    SponsorshipNotes = $"Queued - Redeemed code: {code}",
                    CreatedDate = DateTime.Now
                };

                _userSubscriptionRepository.Add(queuedSubscription);
                await _userSubscriptionRepository.SaveChangesAsync();

                // Mark code as used
                await _sponsorshipCodeRepository.MarkAsUsedAsync(code, userId, queuedSubscription.Id);

                Console.WriteLine($"[SponsorshipQueue] ✅ Sponsorship queued for user {userId}. Will activate when subscription {previousSponsorshipId} expires.");

                return new SuccessDataResult<UserSubscription>(queuedSubscription, 
                    "Sponsorluk kodunuz sıraya alındı. Mevcut sponsorluk bittiğinde otomatik aktif olacak.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SponsorshipQueue] ❌ Error queueing sponsorship: {ex.Message}");
                return new ErrorDataResult<UserSubscription>($"Error queueing sponsorship: {ex.Message}");
            }
        }

        /// <summary>
        /// Activate a sponsorship immediately (for Trial users or when no active sponsorship exists)
        /// </summary>
        private async Task<IDataResult<UserSubscription>> ActivateSponsorship(
            string code,
            int userId,
            SponsorshipCode sponsorshipCode,
            UserSubscription existingSubscription)
        {
            try
            {
                // Deactivate existing trial/free subscription if present
                if (existingSubscription != null)
                {
                    var existingTier = await _subscriptionTierRepository.GetAsync(t => t.Id == existingSubscription.SubscriptionTierId);
                    bool isTrial = existingTier != null && 
                                  (existingTier.TierName == "Trial" || 
                                   existingTier.MonthlyPrice == 0 || 
                                   existingSubscription.IsTrialSubscription);
                    
                    if (isTrial)
                    {
                        Console.WriteLine($"[SponsorshipRedeem] Deactivating existing {existingTier?.TierName} subscription (ID: {existingSubscription.Id})");
                        existingSubscription.IsActive = false;
                        existingSubscription.QueueStatus = SubscriptionQueueStatus.Expired;
                        existingSubscription.Status = "Upgraded";
                        existingSubscription.EndDate = DateTime.Now;
                        existingSubscription.UpdatedDate = DateTime.Now;
                        _userSubscriptionRepository.Update(existingSubscription);
                        await _userSubscriptionRepository.SaveChangesAsync();
                    }
                }

                // Get tier information
                var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == sponsorshipCode.SubscriptionTierId);
                if (tier == null)
                    return new ErrorDataResult<UserSubscription>("Subscription tier not found");

                // Create active subscription
                var subscription = new UserSubscription
                {
                    UserId = userId,
                    SubscriptionTierId = sponsorshipCode.SubscriptionTierId,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(30), // Default 30 days for sponsored subscriptions
                    QueueStatus = SubscriptionQueueStatus.Active,
                    ActivatedDate = DateTime.Now,
                    IsActive = true,
                    AutoRenew = false,
                    PaymentMethod = "Sponsorship",
                    PaymentReference = code,
                    PaidAmount = 0,
                    Currency = tier.Currency,
                    CurrentDailyUsage = 0,
                    CurrentMonthlyUsage = 0,
                    Status = "Active",
                    IsTrialSubscription = false,
                    IsSponsoredSubscription = true,
                    SponsorshipCodeId = sponsorshipCode.Id,
                    SponsorId = sponsorshipCode.SponsorId,
                    SponsorshipNotes = $"Redeemed code: {code}",
                    CreatedDate = DateTime.Now
                };

                _userSubscriptionRepository.Add(subscription);
                await _userSubscriptionRepository.SaveChangesAsync();

                // Mark code as used
                await _sponsorshipCodeRepository.MarkAsUsedAsync(code, userId, subscription.Id);

                Console.WriteLine($"[SponsorshipRedeem] ✅ Code {code} successfully activated for user {userId}");

                return new SuccessDataResult<UserSubscription>(subscription, 
                    "Sponsorluk aktivasyonu tamamlandı!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SponsorshipRedeem] ❌ Error activating sponsorship: {ex.Message}");
                return new ErrorDataResult<UserSubscription>($"Error activating sponsorship: {ex.Message}");
            }
        }

        public async Task<IDataResult<SponsorshipCode>> ValidateCodeAsync(string code)
        {
            try
            {
                var sponsorshipCode = await _sponsorshipCodeRepository.GetByCodeAsync(code);
                if (sponsorshipCode == null)
                    return new ErrorDataResult<SponsorshipCode>("Code not found");

                if (sponsorshipCode.IsUsed)
                    return new ErrorDataResult<SponsorshipCode>("Code has already been used");

                if (!sponsorshipCode.IsActive)
                    return new ErrorDataResult<SponsorshipCode>("Code has been deactivated");

                if (sponsorshipCode.ExpiryDate < DateTime.Now)
                    return new ErrorDataResult<SponsorshipCode>("Code has expired");

                return new SuccessDataResult<SponsorshipCode>(sponsorshipCode, "Code is valid");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<SponsorshipCode>($"Error validating code: {ex.Message}");
            }
        }

        public async Task<IDataResult<List<SponsorshipCode>>> GetSponsorCodesAsync(int sponsorId)
        {
            try
            {
                var codes = await _sponsorshipCodeRepository.GetBySponsorIdAsync(sponsorId);
                return new SuccessDataResult<List<SponsorshipCode>>(codes);
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<List<SponsorshipCode>>($"Error fetching sponsor codes: {ex.Message}");
            }
        }

        public async Task<IDataResult<List<SponsorshipCode>>> GetUnusedSponsorCodesAsync(int sponsorId)
        {
            try
            {
                var codes = await _sponsorshipCodeRepository.GetUnusedCodesBySponsorAsync(sponsorId);
                return new SuccessDataResult<List<SponsorshipCode>>(codes);
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<List<SponsorshipCode>>($"Error fetching unused codes: {ex.Message}");
            }
        }

        public async Task<IDataResult<List<SponsorshipCode>>> GetUnsentSponsorCodesAsync(int sponsorId)
        {
            try
            {
                var codes = await _sponsorshipCodeRepository.GetUnsentCodesBySponsorAsync(sponsorId);
                return new SuccessDataResult<List<SponsorshipCode>>(codes,
                    $"{codes.Count} unsent codes available for distribution");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<List<SponsorshipCode>>($"Error fetching unsent codes: {ex.Message}");
            }
        }

        public async Task<IDataResult<List<SponsorshipCode>>> GetSentButUnusedSponsorCodesAsync(int sponsorId, int? sentDaysAgo = null)
        {
            try
            {
                var codes = await _sponsorshipCodeRepository.GetSentButUnusedCodesBySponsorAsync(sponsorId, sentDaysAgo);
                var message = sentDaysAgo.HasValue
                    ? $"{codes.Count} codes sent {sentDaysAgo} days ago but still unused"
                    : $"{codes.Count} codes sent but still unused";
                return new SuccessDataResult<List<SponsorshipCode>>(codes, message);
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<List<SponsorshipCode>>($"Error fetching sent but unused codes: {ex.Message}");
            }
        }

        public async Task<IDataResult<List<SponsorshipPurchase>>> GetSponsorPurchasesAsync(int sponsorId)
        {
            try
            {
                var purchases = await _sponsorshipPurchaseRepository.GetBySponsorIdAsync(sponsorId);
                return new SuccessDataResult<List<SponsorshipPurchase>>(purchases);
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<List<SponsorshipPurchase>>($"Error fetching purchases: {ex.Message}");
            }
        }

        public async Task<IDataResult<object>> GetSponsorshipStatisticsAsync(int sponsorId)
        {
            try
            {
                var totalSpent = await _sponsorshipPurchaseRepository.GetTotalSpentBySponsorAsync(sponsorId);
                var totalCodesPurchased = await _sponsorshipPurchaseRepository.GetTotalCodesPurchasedAsync(sponsorId);
                var totalCodesUsed = await _sponsorshipPurchaseRepository.GetTotalCodesUsedAsync(sponsorId);
                var usageByTier = await _sponsorshipPurchaseRepository.GetUsageStatisticsByTierAsync(sponsorId);

                var statistics = new
                {
                    TotalSpent = totalSpent,
                    TotalCodesPurchased = totalCodesPurchased,
                    TotalCodesUsed = totalCodesUsed,
                    UsageRate = totalCodesPurchased > 0 ? (decimal)totalCodesUsed / totalCodesPurchased * 100 : 0,
                    UnusedCodes = totalCodesPurchased - totalCodesUsed,
                    UsageByTier = usageByTier
                };

                return new SuccessDataResult<object>(statistics);
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<object>($"Error fetching statistics: {ex.Message}");
            }
        }

        public async Task<IDataResult<List<object>>> GetSponsoredFarmersAsync(int sponsorId)
        {
            try
            {
                var usedCodes = await _sponsorshipCodeRepository.GetUsedCodesBySponsorAsync(sponsorId);
                
                // Create result list
                var farmers = new List<object>();
                
                foreach (var code in usedCodes)
                {
                    // Fetch user and tier information separately since navigation properties are removed
                    Core.Entities.Concrete.User farmer = null;
                    SubscriptionTier tier = null;
                    
                    if (code.UsedByUserId.HasValue)
                    {
                        farmer = await _userRepository.GetAsync(u => u.UserId == code.UsedByUserId.Value);
                    }
                    
                    tier = await _subscriptionTierRepository.GetAsync(t => t.Id == code.SubscriptionTierId);
                    
                    farmers.Add(new
                    {
                        FarmerId = code.UsedByUserId,
                        FarmerName = farmer?.FullName,
                        FarmerEmail = farmer?.Email,
                        Code = code.Code,
                        SubscriptionTier = tier?.DisplayName,
                        RedeemedDate = code.UsedDate,
                        DistributedTo = code.DistributedTo,
                        Notes = code.Notes
                    });
                }

                return new SuccessDataResult<List<object>>(farmers);
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<List<object>>($"Error fetching sponsored farmers: {ex.Message}");
            }
        }

        public async Task<IResult> DeactivateCodeAsync(string code, int sponsorId)
        {
            try
            {
                var sponsorshipCode = await _sponsorshipCodeRepository.GetByCodeAsync(code);
                if (sponsorshipCode == null)
                    return new ErrorResult("Code not found");

                if (sponsorshipCode.SponsorId != sponsorId)
                    return new ErrorResult("You are not authorized to deactivate this code");

                if (sponsorshipCode.IsUsed)
                    return new ErrorResult("Cannot deactivate a used code");

                sponsorshipCode.IsActive = false;
                _sponsorshipCodeRepository.Update(sponsorshipCode);
                await _sponsorshipCodeRepository.SaveChangesAsync();

                return new SuccessResult("Code deactivated successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResult($"Error deactivating code: {ex.Message}");
            }
        }

        public async Task<IDataResult<bool>> IsCodeValidAsync(string code)
        {
            try
            {
                var isValid = await _sponsorshipCodeRepository.IsCodeValidAsync(code);
                return new SuccessDataResult<bool>(isValid, 
                    isValid ? "Code is valid" : "Code is invalid or has been used");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<bool>($"Error checking code validity: {ex.Message}");
            }
        }
    }
}