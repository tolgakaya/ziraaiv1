using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.PlantAnalyses.Queries
{
    public class GetPlantAnalysesQuery : IRequest<IDataResult<List<PlantAnalysisResponseDto>>>
    {
        public int? UserId { get; set; }

        public class GetPlantAnalysesQueryHandler : IRequestHandler<GetPlantAnalysesQuery, IDataResult<List<PlantAnalysisResponseDto>>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;

            public GetPlantAnalysesQueryHandler(IPlantAnalysisRepository plantAnalysisRepository)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
            }

            public async Task<IDataResult<List<PlantAnalysisResponseDto>>> Handle(GetPlantAnalysesQuery request, CancellationToken cancellationToken)
            {
                var analyses = request.UserId.HasValue
                    ? await _plantAnalysisRepository.GetListByUserIdAsync(request.UserId.Value)
                    : await _plantAnalysisRepository.GetListAsync(p => p.Status);

                var response = analyses.Select(plantAnalysis => new PlantAnalysisResponseDto
                {
                    Id = plantAnalysis.Id,
                    ImagePath = plantAnalysis.ImagePath,
                    AnalysisDate = plantAnalysis.AnalysisDate,
                    Status = plantAnalysis.AnalysisStatus,
                    
                    // Detailed analysis data
                    DetailedAnalysis = !string.IsNullOrEmpty(plantAnalysis.DetailedAnalysisData)
                        ? JsonConvert.DeserializeObject<DetailedPlantAnalysisDto>(plantAnalysis.DetailedAnalysisData)
                        : new DetailedPlantAnalysisDto(),
                    
                    // Legacy fields for backward compatibility
                    PlantType = plantAnalysis.PlantType,
                    GrowthStage = plantAnalysis.GrowthStage,
                    ElementDeficiencies = !string.IsNullOrEmpty(plantAnalysis.ElementDeficiencies)
                        ? JsonConvert.DeserializeObject<List<ElementDeficiencyDto>>(plantAnalysis.ElementDeficiencies)
                        : new List<ElementDeficiencyDto>(),
                    Diseases = !string.IsNullOrEmpty(plantAnalysis.Diseases)
                        ? JsonConvert.DeserializeObject<List<DiseaseDto>>(plantAnalysis.Diseases)
                        : new List<DiseaseDto>(),
                    Pests = !string.IsNullOrEmpty(plantAnalysis.Pests)
                        ? JsonConvert.DeserializeObject<List<PestDto>>(plantAnalysis.Pests)
                        : new List<PestDto>(),
                    OverallAnalysis = GetAnalysisField(plantAnalysis.AnalysisResult, "OverallAnalysis")
                }).ToList();

                return new SuccessDataResult<List<PlantAnalysisResponseDto>>(response);
            }

            private string GetAnalysisField(string analysisResult, string fieldName)
            {
                if (string.IsNullOrEmpty(analysisResult))
                    return null;

                try
                {
                    var analysisObject = JsonConvert.DeserializeObject<dynamic>(analysisResult);
                    return analysisObject?[fieldName]?.ToString();
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}