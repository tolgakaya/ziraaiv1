using Entities.Dtos;
using System.Threading.Tasks;

namespace Business.Services.Notification
{
    /// <summary>
    /// Service for sending real-time notifications about plant analysis status
    /// Uses SignalR to push notifications to connected clients
    /// </summary>
    public interface IPlantAnalysisNotificationService
    {
        /// <summary>
        /// Notify user that their plant analysis has completed successfully
        /// </summary>
        /// <param name="userId">ID of the user to notify</param>
        /// <param name="notification">Notification data to send</param>
        Task NotifyAnalysisCompleted(int userId, PlantAnalysisNotificationDto notification);

        /// <summary>
        /// Notify user that their plant analysis has failed
        /// </summary>
        /// <param name="userId">ID of the user to notify</param>
        /// <param name="analysisId">ID of the failed analysis</param>
        /// <param name="errorMessage">Error message describing the failure</param>
        Task NotifyAnalysisFailed(int userId, int analysisId, string errorMessage);

        /// <summary>
        /// Notify user about analysis progress (future enhancement)
        /// </summary>
        /// <param name="userId">ID of the user to notify</param>
        /// <param name="analysisId">ID of the analysis</param>
        /// <param name="progressPercentage">Progress percentage (0-100)</param>
        /// <param name="currentStep">Description of current processing step</param>
        Task NotifyAnalysisProgress(int userId, int analysisId, int progressPercentage, string currentStep);
    }
}