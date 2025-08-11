using Entities.Dtos;

namespace PlantAnalysisWorkerService.Jobs
{
    public interface IPlantAnalysisJobService
    {
        Task ProcessPlantAnalysisResultAsync(PlantAnalysisAsyncResponseDto result, string correlationId);
        Task SendNotificationAsync(PlantAnalysisAsyncResponseDto result);
    }
}