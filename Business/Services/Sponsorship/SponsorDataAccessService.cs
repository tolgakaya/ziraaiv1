using DataAccess.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public class SponsorDataAccessService : ISponsorDataAccessService
    {
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly ISponsorAnalysisAccessRepository _sponsorAnalysisAccessRepository;
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;

        public SponsorDataAccessService(
            ISponsorProfileRepository sponsorProfileRepository,
            ISponsorAnalysisAccessRepository sponsorAnalysisAccessRepository,
            IPlantAnalysisRepository plantAnalysisRepository)
        {
            _sponsorProfileRepository = sponsorProfileRepository;
            _sponsorAnalysisAccessRepository = sponsorAnalysisAccessRepository;
            _plantAnalysisRepository = plantAnalysisRepository;
        }

        public async Task<Entities.Concrete.PlantAnalysis> GetFilteredAnalysisDataAsync(int sponsorId, int plantAnalysisId)
        {
            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (sponsorProfile == null || !sponsorProfile.IsActive || !sponsorProfile.IsVerified)
                return null;

            var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == plantAnalysisId);
            if (analysis == null)
                return null;

            // Access kaydını oluştur/güncelle
            await RecordAccessAsync(sponsorId, plantAnalysisId, analysis.UserId ?? 0);

            var accessPercentage = GetAccessPercentageFromLevel(sponsorProfile.DataAccessLevel);
            
            return FilterAnalysisData(analysis, accessPercentage);
        }

        public async Task<int> GetDataAccessPercentageAsync(int sponsorId)
        {
            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (sponsorProfile == null)
                return 0;

            return GetAccessPercentageFromLevel(sponsorProfile.DataAccessLevel);
        }

        public async Task<bool> CanAccessFieldAsync(int sponsorId, string fieldName)
        {
            var accessPercentage = await GetDataAccessPercentageAsync(sponsorId);
            var accessibleFields = GetFieldsByAccessLevel(accessPercentage);
            
            return accessibleFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<List<string>> GetAccessibleFieldsAsync(int sponsorId)
        {
            var accessPercentage = await GetDataAccessPercentageAsync(sponsorId);
            return GetFieldsByAccessLevel(accessPercentage);
        }

        public async Task<List<string>> GetRestrictedFieldsAsync(int sponsorId)
        {
            var accessPercentage = await GetDataAccessPercentageAsync(sponsorId);
            var allFields = GetAllAnalysisFields();
            var accessibleFields = GetFieldsByAccessLevel(accessPercentage);
            
            return allFields.Except(accessibleFields, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public async Task RecordAccessAsync(int sponsorId, int plantAnalysisId, int farmerId)
        {
            var existingAccess = await _sponsorAnalysisAccessRepository.GetBySponsorAndAnalysisAsync(sponsorId, plantAnalysisId);
            
            if (existingAccess == null)
            {
                var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
                var accessPercentage = GetAccessPercentageFromLevel(sponsorProfile?.DataAccessLevel ?? "Basic30");
                
                var newAccess = new Entities.Concrete.SponsorAnalysisAccess
                {
                    SponsorId = sponsorId,
                    PlantAnalysisId = plantAnalysisId,
                    FarmerId = farmerId,
                    AccessLevel = sponsorProfile?.DataAccessLevel ?? "Basic30",
                    AccessPercentage = accessPercentage,
                    FirstViewedDate = DateTime.Now,
                    LastViewedDate = DateTime.Now,
                    ViewCount = 1,
                    AccessedFields = System.Text.Json.JsonSerializer.Serialize(GetFieldsByAccessLevel(accessPercentage)),
                    RestrictedFields = System.Text.Json.JsonSerializer.Serialize(await GetRestrictedFieldsAsync(sponsorId)),
                    CreatedDate = DateTime.Now,
                    CanViewHealthScore = accessPercentage >= 30,
                    CanViewDiseases = accessPercentage >= 60,
                    CanViewPests = accessPercentage >= 60,
                    CanViewNutrients = accessPercentage >= 60,
                    CanViewRecommendations = accessPercentage >= 60,
                    CanViewFarmerContact = accessPercentage >= 100,
                    CanViewLocation = accessPercentage >= 60,
                    CanViewImages = accessPercentage >= 30
                };
                
                _sponsorAnalysisAccessRepository.Add(newAccess);
                await _sponsorAnalysisAccessRepository.SaveChangesAsync();
            }
            else
            {
                await _sponsorAnalysisAccessRepository.UpdateViewCountAsync(existingAccess.Id);
            }
        }

        public async Task<bool> HasAccessToAnalysisAsync(int sponsorId, int plantAnalysisId)
        {
            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (sponsorProfile == null || !sponsorProfile.IsActive || !sponsorProfile.IsVerified)
                return false;

            // Sponsor profile aktif ve verified ise erişim var
            return true;
        }

        public async Task<Entities.Concrete.SponsorAnalysisAccess> GetAccessRecordAsync(int sponsorId, int plantAnalysisId)
        {
            return await _sponsorAnalysisAccessRepository.GetBySponsorAndAnalysisAsync(sponsorId, plantAnalysisId);
        }

        public async Task UpdateAccessPermissionsAsync(int sponsorId, string dataAccessLevel, int accessPercentage)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile != null)
            {
                profile.DataAccessLevel = dataAccessLevel;
                profile.UpdatedDate = DateTime.Now;
                
                _sponsorProfileRepository.Update(profile);
                await _sponsorProfileRepository.SaveChangesAsync();
            }
        }

        private int GetAccessPercentageFromLevel(string dataAccessLevel)
        {
            return dataAccessLevel switch
            {
                "Basic30" => 30,
                "Medium60" => 60,
                "Full100" => 100,
                _ => 0
            };
        }

        private Entities.Concrete.PlantAnalysis FilterAnalysisData(Entities.Concrete.PlantAnalysis analysis, int accessPercentage)
        {
            var filteredAnalysis = new Entities.Concrete.PlantAnalysis
            {
                Id = analysis.Id,
                AnalysisDate = analysis.AnalysisDate,
                AnalysisStatus = analysis.AnalysisStatus,
                CreatedDate = analysis.CreatedDate,
                FarmerId = analysis.FarmerId,
                CropType = analysis.CropType,
                SponsorId = analysis.SponsorId,
                SponsorUserId = analysis.SponsorUserId
            };

            // 30% Access (S ve M paketler)
            if (accessPercentage >= 30)
            {
                filteredAnalysis.OverallHealthScore = analysis.OverallHealthScore;
                filteredAnalysis.PlantSpecies = analysis.PlantSpecies;
                filteredAnalysis.PlantVariety = analysis.PlantVariety;
                filteredAnalysis.GrowthStage = analysis.GrowthStage;
                filteredAnalysis.ImagePath = analysis.ImagePath;
                filteredAnalysis.PlantType = analysis.PlantType; // Legacy
            }

            // 60% Access (L paketi)
            if (accessPercentage >= 60)
            {
                filteredAnalysis.VigorScore = analysis.VigorScore;
                filteredAnalysis.HealthSeverity = analysis.HealthSeverity;
                filteredAnalysis.StressIndicators = analysis.StressIndicators;
                filteredAnalysis.DiseaseSymptoms = analysis.DiseaseSymptoms;
                filteredAnalysis.PrimaryDeficiency = analysis.PrimaryDeficiency;
                filteredAnalysis.NutrientStatus = analysis.NutrientStatus;
                filteredAnalysis.PrimaryConcern = analysis.PrimaryConcern;
                filteredAnalysis.Prognosis = analysis.Prognosis;
                filteredAnalysis.Recommendations = analysis.Recommendations;
                filteredAnalysis.Location = analysis.Location;
                filteredAnalysis.Latitude = analysis.Latitude;
                filteredAnalysis.Longitude = analysis.Longitude;
                filteredAnalysis.WeatherConditions = analysis.WeatherConditions;
                filteredAnalysis.Temperature = analysis.Temperature;
                filteredAnalysis.Humidity = analysis.Humidity;
                filteredAnalysis.SoilType = analysis.SoilType;
                filteredAnalysis.Diseases = analysis.Diseases; // Legacy
                filteredAnalysis.Pests = analysis.Pests; // Legacy
                filteredAnalysis.ElementDeficiencies = analysis.ElementDeficiencies; // Legacy
            }

            // 100% Access (XL paketi)
            if (accessPercentage >= 100)
            {
                // Tüm alanları kopyala
                return analysis;
            }

            return filteredAnalysis;
        }

        private List<string> GetFieldsByAccessLevel(int accessPercentage)
        {
            var fields = new List<string>();
            
            // 30% Access Fields
            if (accessPercentage >= 30)
            {
                fields.AddRange(new[]
                {
                    "OverallHealthScore", "PlantSpecies", "PlantVariety", "GrowthStage",
                    "ImagePath", "PlantType", "CropType", "AnalysisDate"
                });
            }

            // 60% Access Fields
            if (accessPercentage >= 60)
            {
                fields.AddRange(new[]
                {
                    "VigorScore", "HealthSeverity", "StressIndicators", "DiseaseSymptoms",
                    "PrimaryDeficiency", "NutrientStatus", "PrimaryConcern", "Prognosis",
                    "Recommendations", "Location", "Latitude", "Longitude", "WeatherConditions",
                    "Temperature", "Humidity", "SoilType", "Diseases", "Pests", "ElementDeficiencies"
                });
            }

            // 100% Access Fields
            if (accessPercentage >= 100)
            {
                fields.AddRange(new[]
                {
                    "ContactPhone", "ContactEmail", "AdditionalInfo", "FieldId", "PlantingDate",
                    "ExpectedHarvestDate", "LastFertilization", "LastIrrigation", "PreviousTreatments",
                    "UrgencyLevel", "Notes", "DetailedAnalysisData", "CrossFactorInsights",
                    "EstimatedYieldImpact", "ConfidenceLevel", "IdentificationConfidence"
                });
            }

            return fields;
        }

        private List<string> GetAllAnalysisFields()
        {
            return new List<string>
            {
                "OverallHealthScore", "PlantSpecies", "PlantVariety", "GrowthStage",
                "ImagePath", "PlantType", "CropType", "AnalysisDate", "VigorScore",
                "HealthSeverity", "StressIndicators", "DiseaseSymptoms", "PrimaryDeficiency",
                "NutrientStatus", "PrimaryConcern", "Prognosis", "Recommendations",
                "Location", "Latitude", "Longitude", "WeatherConditions", "Temperature",
                "Humidity", "SoilType", "Diseases", "Pests", "ElementDeficiencies",
                "ContactPhone", "ContactEmail", "AdditionalInfo", "FieldId", "PlantingDate",
                "ExpectedHarvestDate", "LastFertilization", "LastIrrigation", "PreviousTreatments",
                "UrgencyLevel", "Notes", "DetailedAnalysisData", "CrossFactorInsights",
                "EstimatedYieldImpact", "ConfidenceLevel", "IdentificationConfidence"
            };
        }
    }
}