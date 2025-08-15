using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public interface ISponsorDataAccessService
    {
        Task<Entities.Concrete.PlantAnalysis> GetFilteredAnalysisDataAsync(int sponsorId, int plantAnalysisId);
        Task<int> GetDataAccessPercentageAsync(int sponsorId);
        Task<bool> CanAccessFieldAsync(int sponsorId, string fieldName);
        Task<List<string>> GetAccessibleFieldsAsync(int sponsorId);
        Task<List<string>> GetRestrictedFieldsAsync(int sponsorId);
        Task RecordAccessAsync(int sponsorId, int plantAnalysisId, int farmerId);
        Task<bool> HasAccessToAnalysisAsync(int sponsorId, int plantAnalysisId);
        Task<Entities.Concrete.SponsorAnalysisAccess> GetAccessRecordAsync(int sponsorId, int plantAnalysisId);
        Task UpdateAccessPermissionsAsync(int sponsorId, string dataAccessLevel, int accessPercentage);
    }
}