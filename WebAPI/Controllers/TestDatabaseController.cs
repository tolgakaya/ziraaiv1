using Microsoft.AspNetCore.Mvc;
using DataAccess.Abstract;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestDatabaseController : ControllerBase
    {
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;
        private readonly ISubscriptionTierRepository _subscriptionTierRepository;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;

        public TestDatabaseController(
            IPlantAnalysisRepository plantAnalysisRepository,
            ISubscriptionTierRepository subscriptionTierRepository,
            IUserSubscriptionRepository userSubscriptionRepository)
        {
            _plantAnalysisRepository = plantAnalysisRepository;
            _subscriptionTierRepository = subscriptionTierRepository;
            _userSubscriptionRepository = userSubscriptionRepository;
        }

        [HttpGet("recent-analyses")]
        public async Task<IActionResult> GetRecentAnalyses()
        {
            var recentAnalyses = await _plantAnalysisRepository.GetListAsync();

            var result = recentAnalyses.Select(p => new {
                p.Id,
                p.UserId,
                p.FarmerId,
                p.CropType,
                p.PlantSpecies,
                p.PrimaryConcern,
                p.OverallHealthScore,
                p.AnalysisId,
                p.CreatedDate,
                p.AnalysisStatus
            }).ToList();

            return Ok(result);
        }

        [HttpGet("subscription-debug")]
        public async Task<IActionResult> DebugSubscriptions()
        {
            var result = new
            {
                // Check if Trial tier exists
                TrialTier = await _subscriptionTierRepository.GetAsync(t => t.TierName == "Trial" && t.IsActive),
                
                // Get all subscription tiers
                AllTiers = await _subscriptionTierRepository.GetListAsync(),
                
                // Get recent user subscriptions
                RecentSubscriptions = await _userSubscriptionRepository.GetListAsync()
            };

            return Ok(result);
        }

        [HttpGet("user-subscription/{userId}")]
        public async Task<IActionResult> CheckUserSubscription(int userId)
        {
            var activeSubscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
            
            if (activeSubscription == null)
            {
                return Ok(new 
                { 
                    UserId = userId,
                    HasActiveSubscription = false,
                    Message = "No active subscription found"
                });
            }

            return Ok(new
            {
                UserId = userId,
                HasActiveSubscription = true,
                SubscriptionId = activeSubscription.Id,
                TierName = activeSubscription.SubscriptionTier?.TierName,
                IsActive = activeSubscription.IsActive,
                StartDate = activeSubscription.StartDate,
                EndDate = activeSubscription.EndDate,
                IsTrialSubscription = activeSubscription.IsTrialSubscription,
                DailyLimit = activeSubscription.SubscriptionTier?.DailyRequestLimit,
                MonthlyLimit = activeSubscription.SubscriptionTier?.MonthlyRequestLimit,
                CurrentDailyUsage = activeSubscription.CurrentDailyUsage,
                CurrentMonthlyUsage = activeSubscription.CurrentMonthlyUsage
            });
        }

        [HttpPost("add-trial-tier")]
        public async Task<IActionResult> AddTrialTier()
        {
            try
            {
                // Check if Trial tier already exists
                var existingTrial = await _subscriptionTierRepository.GetAsync(t => t.TierName == "Trial");
                if (existingTrial != null)
                {
                    return Ok(new { success = true, message = "Trial tier already exists", tier = existingTrial });
                }

                // Add Trial tier
                var trialTier = new Entities.Concrete.SubscriptionTier
                {
                    TierName = "Trial",
                    DisplayName = "Trial", 
                    Description = "30-day trial with limited access",
                    DailyRequestLimit = 1,
                    MonthlyRequestLimit = 30,
                    MonthlyPrice = 0m,
                    YearlyPrice = 0m,
                    Currency = "TRY",
                    PrioritySupport = false,
                    AdvancedAnalytics = false,
                    ApiAccess = false,
                    ResponseTimeHours = 72,
                    AdditionalFeatures = "[\"Basic plant analysis\",\"Email notifications\",\"Trial access\"]",
                    IsActive = true,
                    DisplayOrder = 0,
                    CreatedDate = DateTime.UtcNow
                };

                _subscriptionTierRepository.Add(trialTier);
                await _subscriptionTierRepository.SaveChangesAsync();

                // Add other tiers if they don't exist
                var tiers = new[]
                {
                    new { Name = "S", Display = "Small", Description = "Perfect for small farms and hobbyists", Daily = 5, Monthly = 50, Price = 99.99m, YearPrice = 999.99m, Order = 1, Hours = 48, Features = "[\"Basic plant analysis\",\"Email notifications\",\"Basic reports\"]", Priority = false, Analytics = false, Api = false },
                    new { Name = "M", Display = "Medium", Description = "Ideal for medium-sized farms", Daily = 20, Monthly = 200, Price = 299.99m, YearPrice = 2999.99m, Order = 2, Hours = 24, Features = "[\"Advanced plant analysis\",\"Email & SMS notifications\",\"Detailed reports\",\"Historical data access\",\"Basic API access\"]", Priority = false, Analytics = true, Api = false },
                    new { Name = "L", Display = "Large", Description = "Best for large commercial farms", Daily = 50, Monthly = 500, Price = 599.99m, YearPrice = 5999.99m, Order = 3, Hours = 12, Features = "[\"Premium plant analysis with AI insights\",\"All notification channels\",\"Custom reports\",\"Full historical data\",\"Full API access\",\"Priority support\",\"Export capabilities\"]", Priority = true, Analytics = true, Api = true },
                    new { Name = "XL", Display = "Extra Large", Description = "Enterprise solution for agricultural corporations", Daily = 200, Monthly = 2000, Price = 1499.99m, YearPrice = 14999.99m, Order = 4, Hours = 6, Features = "[\"Enterprise AI analysis with custom models\",\"All features included\",\"Dedicated support team\",\"Custom integrations\",\"White-label options\",\"SLA guarantee\",\"Training sessions\",\"Unlimited data retention\"]", Priority = true, Analytics = true, Api = true }
                };

                foreach (var tier in tiers)
                {
                    var existing = await _subscriptionTierRepository.GetAsync(t => t.TierName == tier.Name);
                    if (existing == null)
                    {
                        var newTier = new Entities.Concrete.SubscriptionTier
                        {
                            TierName = tier.Name,
                            DisplayName = tier.Display,
                            Description = tier.Description,
                            DailyRequestLimit = tier.Daily,
                            MonthlyRequestLimit = tier.Monthly,
                            MonthlyPrice = tier.Price,
                            YearlyPrice = tier.YearPrice,
                            Currency = "TRY",
                            PrioritySupport = tier.Priority,
                            AdvancedAnalytics = tier.Analytics,
                            ApiAccess = tier.Api,
                            ResponseTimeHours = tier.Hours,
                            AdditionalFeatures = tier.Features,
                            IsActive = true,
                            DisplayOrder = tier.Order,
                            CreatedDate = DateTime.UtcNow
                        };

                        _subscriptionTierRepository.Add(newTier);
                        await _subscriptionTierRepository.SaveChangesAsync();
                    }
                }

                // Return all tiers
                var allTiers = await _subscriptionTierRepository.GetListAsync();
                return Ok(new { 
                    success = true, 
                    message = "All subscription tiers have been added successfully", 
                    tiers = allTiers.Select(t => new { 
                        t.Id, t.TierName, t.DisplayName, t.DailyRequestLimit, t.MonthlyRequestLimit, t.MonthlyPrice, t.IsActive 
                    }).OrderBy(t => t.TierName == "Trial" ? 0 : 1).ThenBy(t => t.TierName)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error adding subscription tiers: {ex.Message}" });
            }
        }
    }
}