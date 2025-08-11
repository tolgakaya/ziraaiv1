using Entities.Dtos;
using System.Threading.Tasks;

namespace Business.Services.PlantAnalysis
{
    public interface IPlantAnalysisAsyncService
    {
        Task<string> QueuePlantAnalysisAsync(PlantAnalysisRequestDto request);
        Task<bool> IsQueueHealthyAsync();
    }
}