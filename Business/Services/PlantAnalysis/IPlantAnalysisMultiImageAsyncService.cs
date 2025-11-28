using Entities.Dtos;
using System.Threading.Tasks;

namespace Business.Services.PlantAnalysis
{
    /// <summary>
    /// Service interface for multi-image plant analysis async operations.
    /// Supports up to 5 images for comprehensive AI analysis.
    /// </summary>
    public interface IPlantAnalysisMultiImageAsyncService
    {
        /// <summary>
        /// Queue multi-image plant analysis request for async processing.
        /// Processes, uploads, and queues all provided images.
        /// </summary>
        /// <param name="request">Multi-image analysis request with up to 5 images</param>
        /// <returns>Analysis ID for tracking</returns>
        Task<string> QueuePlantAnalysisAsync(PlantAnalysisMultiImageRequestDto request);

        /// <summary>
        /// Check if RabbitMQ queue is healthy and accepting messages.
        /// </summary>
        /// <returns>True if queue is healthy, false otherwise</returns>
        Task<bool> IsQueueHealthyAsync();
    }
}
