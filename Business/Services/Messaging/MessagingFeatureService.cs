using Business.Services.Sponsorship;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Messaging
{
    public class MessagingFeatureService : IMessagingFeatureService
    {
        private readonly IMessagingFeatureRepository _featureRepository;
        private readonly ISponsorDataAccessService _sponsorDataAccessService;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly IMemoryCache _memoryCache;
        private const string CACHE_KEY = "MessagingFeatures_All";
        private readonly TimeSpan CACHE_DURATION = TimeSpan.FromHours(24);

        public MessagingFeatureService(
            IMessagingFeatureRepository featureRepository,
            ISponsorDataAccessService sponsorDataAccessService,
            IUserSubscriptionRepository userSubscriptionRepository,
            IMemoryCache memoryCache)
        {
            _featureRepository = featureRepository;
            _sponsorDataAccessService = sponsorDataAccessService;
            _userSubscriptionRepository = userSubscriptionRepository;
            _memoryCache = memoryCache;
        }

        public async Task<IDataResult<MessagingFeaturesDto>> GetUserFeaturesAsync(int userId)
        {
            // Get user tier
            var userTier = await GetUserTierAsync(userId);

            // Get all features from cache or database
            var features = await GetCachedFeaturesAsync();

            var dto = new MessagingFeaturesDto
            {
                VoiceMessages = MapFeature(features, "VoiceMessages", userTier),
                ImageAttachments = MapFeature(features, "ImageAttachments", userTier),
                VideoAttachments = MapFeature(features, "VideoAttachments", userTier),
                FileAttachments = MapFeature(features, "FileAttachments", userTier),
                MessageEdit = MapFeature(features, "MessageEdit", userTier),
                MessageDelete = MapFeature(features, "MessageDelete", userTier),
                MessageForward = MapFeature(features, "MessageForward", userTier),
                TypingIndicator = MapFeature(features, "TypingIndicator", userTier),
                LinkPreview = MapFeature(features, "LinkPreview", userTier)
            };

            return new SuccessDataResult<MessagingFeaturesDto>(dto, "Features retrieved successfully");
        }

        public async Task<IDataResult<bool>> IsFeatureAvailableAsync(string featureName, int userId)
        {
            var features = await GetCachedFeaturesAsync();
            var feature = features.FirstOrDefault(f => f.FeatureName == featureName);

            if (feature == null)
                return new ErrorDataResult<bool>(false, "Feature not found");

            if (!feature.IsEnabled)
                return new SuccessDataResult<bool>(false, "Feature is disabled");

            var userTier = await GetUserTierAsync(userId);
            var hasAccess = CheckTierAccess(feature.RequiredTier, userTier);

            return new SuccessDataResult<bool>(hasAccess);
        }

        public async Task<IResult> ValidateFeatureAccessAsync(string featureName, int userId, long? fileSize = null, int? duration = null)
        {
            var features = await GetCachedFeaturesAsync();
            var feature = features.FirstOrDefault(f => f.FeatureName == featureName);

            if (feature == null)
                return new ErrorResult($"Feature '{featureName}' not found");

            // 1. Check if feature is enabled globally
            if (!feature.IsEnabled)
                return new ErrorResult($"{feature.DisplayName ?? featureName} feature is currently disabled");

            // 2. Check user tier
            var userTier = await GetUserTierAsync(userId);
            if (!CheckTierAccess(feature.RequiredTier, userTier))
            {
                return new ErrorResult($"{feature.DisplayName ?? featureName} requires {feature.RequiredTier} tier. Your tier: {userTier}");
            }

            // 3. Check file size limit
            if (fileSize.HasValue && feature.MaxFileSize.HasValue)
            {
                if (fileSize.Value > feature.MaxFileSize.Value)
                {
                    var maxSizeMB = feature.MaxFileSize.Value / (1024.0 * 1024.0);
                    return new ErrorResult($"File size exceeds maximum limit of {maxSizeMB:F2} MB");
                }
            }

            // 4. Check duration limit
            if (duration.HasValue && feature.MaxDuration.HasValue)
            {
                if (duration.Value > feature.MaxDuration.Value)
                {
                    return new ErrorResult($"Duration exceeds maximum limit of {feature.MaxDuration.Value} seconds");
                }
            }

            return new SuccessResult("Feature access granted");
        }

        public async Task<IDataResult<MessagingFeature>> GetFeatureAsync(string featureName)
        {
            var features = await GetCachedFeaturesAsync();
            var feature = features.FirstOrDefault(f => f.FeatureName == featureName);

            if (feature == null)
                return new ErrorDataResult<MessagingFeature>($"Feature '{featureName}' not found");

            return new SuccessDataResult<MessagingFeature>(feature);
        }

        public async Task<IDataResult<List<MessagingFeature>>> GetAllFeaturesAsync()
        {
            var features = await GetCachedFeaturesAsync();
            return new SuccessDataResult<List<MessagingFeature>>(features, "Features retrieved successfully");
        }

        public async Task<IResult> UpdateFeatureAsync(int featureId, bool isEnabled, int adminUserId)
        {
            var feature = await _featureRepository.GetAsync(f => f.Id == featureId);
            if (feature == null)
                return new ErrorResult("Feature not found");

            feature.IsEnabled = isEnabled;
            feature.UpdatedDate = DateTime.Now;
            feature.UpdatedByUserId = adminUserId;

            await _featureRepository.UpdateAsync(feature);

            // Clear cache to force refresh
            _memoryCache.Remove(CACHE_KEY);

            return new SuccessResult($"Feature '{feature.FeatureName}' updated successfully");
        }

        #region Private Helper Methods

        private async Task<List<MessagingFeature>> GetCachedFeaturesAsync()
        {
            if (!_memoryCache.TryGetValue(CACHE_KEY, out List<MessagingFeature> features))
            {
                features = await _featureRepository.GetListAsync();
                _memoryCache.Set(CACHE_KEY, features, CACHE_DURATION);
            }

            return features ?? new List<MessagingFeature>();
        }

        private async Task<string> GetUserTierAsync(int userId)
        {
            // Try to get sponsor tier first
            var sponsorTier = await _sponsorDataAccessService.GetSponsorTierAsync(userId);
            if (!string.IsNullOrEmpty(sponsorTier) && sponsorTier != "None")
                return sponsorTier;

            // Fall back to subscription tier
            var subscription = await _userSubscriptionRepository.GetAsync(
                us => us.UserId == userId && us.IsActive && us.EndDate > DateTime.Now);

            return subscription?.TierName ?? "None";
        }

        private MessagingFeatureDto MapFeature(List<MessagingFeature> features, string featureName, string userTier)
        {
            var feature = features.FirstOrDefault(f => f.FeatureName == featureName);

            if (feature == null)
            {
                return new MessagingFeatureDto
                {
                    Enabled = false,
                    Available = false,
                    RequiredTier = "Unknown",
                    UnavailableReason = "Feature not configured"
                };
            }

            var hasAccess = CheckTierAccess(feature.RequiredTier, userTier);

            var dto = new MessagingFeatureDto
            {
                Enabled = feature.IsEnabled,
                Available = feature.IsEnabled && hasAccess,
                RequiredTier = feature.RequiredTier,
                MaxFileSize = feature.MaxFileSize,
                MaxDuration = feature.MaxDuration,
                TimeLimit = feature.TimeLimit
            };

            // Parse allowed MIME types
            if (!string.IsNullOrEmpty(feature.AllowedMimeTypes))
            {
                dto.AllowedTypes = feature.AllowedMimeTypes.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
            }

            // Set unavailable reason
            if (!feature.IsEnabled)
            {
                dto.UnavailableReason = $"{feature.DisplayName ?? featureName} feature is currently disabled";
            }
            else if (!hasAccess)
            {
                dto.UnavailableReason = $"Requires {feature.RequiredTier} tier (your tier: {userTier})";
            }

            return dto;
        }

        private bool CheckTierAccess(string requiredTier, string userTier)
        {
            if (string.IsNullOrEmpty(requiredTier) || requiredTier == "None")
                return true;

            var tierHierarchy = new Dictionary<string, int>
            {
                { "None", 0 },
                { "Trial", 1 },
                { "S", 2 },
                { "M", 3 },
                { "L", 4 },
                { "XL", 5 }
            };

            var requiredLevel = tierHierarchy.GetValueOrDefault(requiredTier, 0);
            var userLevel = tierHierarchy.GetValueOrDefault(userTier, 0);

            return userLevel >= requiredLevel;
        }

        #endregion
    }
}
