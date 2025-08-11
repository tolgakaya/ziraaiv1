using Entities.Dtos;
using System.Threading.Tasks;

namespace Business.Services.PlantAnalysis
{
    public interface IPlantAnalysisService
    {
        Task<PlantAnalysisResponseDto> SendToN8nWebhookAsync(PlantAnalysisRequestDto request);
        Task<string> SaveImageFileAsync(string imageBase64, int analysisId);
    }
}