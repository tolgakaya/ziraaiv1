using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Messaging
{
    /// <summary>
    /// Service for managing messaging feature flags and user access validation
    /// </summary>
    public interface IMessagingFeatureService
    {
        /// <summary>
        /// Get all messaging features configuration for a specific user
        /// Returns feature availability based on user tier and admin toggles
        /// </summary>
        Task<IDataResult<MessagingFeaturesDto>> GetUserFeaturesAsync(int userId);

        /// <summary>
        /// Check if a specific feature is available for a user
        /// </summary>
        Task<IDataResult<bool>> IsFeatureAvailableAsync(string featureName, int userId);

        /// <summary>
        /// Validate if user can use a feature (combines enabled check + tier check + limits)
        /// </summary>
        Task<IResult> ValidateFeatureAccessAsync(string featureName, int userId, long? fileSize = null, int? duration = null);

        /// <summary>
        /// Get a specific feature configuration
        /// </summary>
        Task<IDataResult<MessagingFeature>> GetFeatureAsync(string featureName);

        /// <summary>
        /// Get all features (admin panel)
        /// </summary>
        Task<IDataResult<List<MessagingFeature>>> GetAllFeaturesAsync();

        /// <summary>
        /// Update feature toggle (admin only)
        /// </summary>
        Task<IResult> UpdateFeatureAsync(int featureId, bool isEnabled, int adminUserId);
    }
}
