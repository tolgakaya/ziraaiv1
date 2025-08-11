using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.PlantAnalyses.Queries
{
    public class GetPlantAnalysisQuery : IRequest<IDataResult<PlantAnalysisResponseDto>>
    {
        public int Id { get; set; }

        public class GetPlantAnalysisQueryHandler : IRequestHandler<GetPlantAnalysisQuery, IDataResult<PlantAnalysisResponseDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;

            public GetPlantAnalysisQueryHandler(IPlantAnalysisRepository plantAnalysisRepository)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
            }

            public async Task<IDataResult<PlantAnalysisResponseDto>> Handle(GetPlantAnalysisQuery request, CancellationToken cancellationToken)
            {
                var plantAnalysis = await _plantAnalysisRepository.GetAsync(p => p.Id == request.Id && p.Status);
                
                if (plantAnalysis == null)
                    return new ErrorDataResult<PlantAnalysisResponseDto>("Analysis not found");

                var response = new PlantAnalysisResponseDto
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
                        : new List<PestDto>()
                };

                if (!string.IsNullOrEmpty(plantAnalysis.AnalysisResult))
                {
                    var analysisResult = JsonConvert.DeserializeObject<dynamic>(plantAnalysis.AnalysisResult);
                    response.OverallAnalysis = analysisResult?.OverallAnalysis;
                    response.Recommendations = analysisResult?.Recommendations;
                }

                return new SuccessDataResult<PlantAnalysisResponseDto>(response);
            }
        }
    }
}