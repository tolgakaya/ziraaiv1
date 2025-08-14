using Business.Services.FileStorage;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.PlantAnalyses.Queries
{
    /// <summary>
    /// Query for retrieving farmer's plant analysis history with pagination and filtering
    /// Optimized for mobile app listing with lightweight data transfer
    /// </summary>
    public class GetPlantAnalysesForFarmerQuery : IRequest<IDataResult<PlantAnalysisListResponseDto>>
    {
        public int UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Status { get; set; } // "Completed", "Processing", "Failed", null (all)
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string CropType { get; set; }

        public class GetPlantAnalysesForFarmerQueryHandler : IRequestHandler<GetPlantAnalysesForFarmerQuery, IDataResult<PlantAnalysisListResponseDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly IFileStorageService _fileStorageService;

            public GetPlantAnalysesForFarmerQueryHandler(
                IPlantAnalysisRepository plantAnalysisRepository,
                IFileStorageService fileStorageService)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
                _fileStorageService = fileStorageService;
            }

            public async Task<IDataResult<PlantAnalysisListResponseDto>> Handle(GetPlantAnalysesForFarmerQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    // Get all user analyses first, then apply filters
                    var allUserAnalyses = await _plantAnalysisRepository.GetListByUserIdAsync(request.UserId);
                    
                    // Apply filters
                    var filteredAnalyses = allUserAnalyses.AsQueryable();

                    // Filter by status if specified
                    if (!string.IsNullOrEmpty(request.Status))
                    {
                        filteredAnalyses = filteredAnalyses.Where(p => p.AnalysisStatus == request.Status);
                    }

                    // Filter by date range
                    if (request.FromDate.HasValue)
                    {
                        filteredAnalyses = filteredAnalyses.Where(p => p.CreatedDate >= request.FromDate.Value);
                    }

                    if (request.ToDate.HasValue)
                    {
                        filteredAnalyses = filteredAnalyses.Where(p => p.CreatedDate <= request.ToDate.Value);
                    }

                    // Filter by crop type
                    if (!string.IsNullOrEmpty(request.CropType))
                    {
                        filteredAnalyses = filteredAnalyses.Where(p => 
                            !string.IsNullOrEmpty(p.CropType) && 
                            p.CropType.ToLower().Contains(request.CropType.ToLower()));
                    }

                    // Get total count for pagination
                    var totalCount = filteredAnalyses.Count();

                    // Order by creation date (newest first) and apply pagination
                    var plantAnalyses = filteredAnalyses
                        .OrderByDescending(p => p.CreatedDate)
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToList();

                    var analysisItems = new List<PlantAnalysisListItemDto>();

                    foreach (var analysis in plantAnalyses)
                    {
                        // Convert image path to full URL if it's a relative path
                        string imageUrl = analysis.ImagePath;
                        if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http"))
                        {
                            var baseUrl = _fileStorageService.BaseUrl?.TrimEnd('/');
                            var relativePath = imageUrl.TrimStart('/');
                            imageUrl = $"{baseUrl}/{relativePath}";
                        }

                        var listItem = new PlantAnalysisListItemDto
                        {
                            Id = analysis.Id,
                            AnalysisId = analysis.AnalysisId,
                            ImagePath = imageUrl,
                            ThumbnailUrl = imageUrl, // Same as ImagePath for now, could be optimized later
                            AnalysisDate = analysis.AnalysisDate,
                            CreatedDate = analysis.CreatedDate,
                            Status = analysis.AnalysisStatus,
                            
                            // Basic info
                            CropType = analysis.CropType,
                            Location = analysis.Location,
                            UrgencyLevel = analysis.UrgencyLevel,
                            Notes = analysis.Notes,
                            
                            // Sponsorship info
                            FarmerId = analysis.FarmerId,
                            SponsorId = analysis.SponsorId,
                            SponsorUserId = analysis.SponsorUserId,
                            SponsorshipCodeId = analysis.SponsorshipCodeId,
                            
                            // Summary results (for quick overview)
                            OverallHealthScore = analysis.OverallHealthScore,
                            PrimaryConcern = analysis.PrimaryConcern,
                            Prognosis = analysis.Prognosis,
                            ConfidenceLevel = (int?)analysis.ConfidenceLevel,
                            
                            // Plant identification
                            PlantSpecies = analysis.PlantSpecies,
                            PlantVariety = analysis.PlantVariety,
                            GrowthStage = analysis.GrowthStage,
                            
                            // Processing metadata
                            TotalTokens = analysis.TotalTokens,
                            TotalCostTry = analysis.TotalCostTry,
                            AiModel = analysis.AiModel
                        };

                        analysisItems.Add(listItem);
                    }

                    var response = new PlantAnalysisListResponseDto
                    {
                        Analyses = analysisItems,
                        TotalCount = totalCount,
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                        HasNextPage = request.Page * request.PageSize < totalCount,
                        HasPreviousPage = request.Page > 1
                    };

                    return new SuccessDataResult<PlantAnalysisListResponseDto>(response, 
                        $"Found {analysisItems.Count} plant analyses (page {request.Page} of {response.TotalPages})");
                }
                catch (Exception ex)
                {
                    return new ErrorDataResult<PlantAnalysisListResponseDto>(
                        $"An error occurred while retrieving plant analyses: {ex.Message}");
                }
            }
        }
    }
}