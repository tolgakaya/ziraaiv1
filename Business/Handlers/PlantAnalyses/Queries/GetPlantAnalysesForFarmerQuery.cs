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

        // 🆕 NEW: Sorting parameters (from sponsor endpoint)
        public string SortBy { get; set; } = "date"; // date, healthScore, messageCount, unreadCount, lastMessageDate
        public string SortOrder { get; set; } = "desc"; // asc, desc

        // 🆕 NEW: Message status filters (from sponsor endpoint)
        /// <summary>
        /// Filter by message status: all, contacted, notContacted, hasResponse, noResponse, active, idle
        /// </summary>
        public string FilterByMessageStatus { get; set; }

        /// <summary>
        /// Filter to show only analyses with unread messages from sponsor
        /// </summary>
        public bool? HasUnreadMessages { get; set; }

        /// <summary>
        /// Filter to show analyses with at least this many unread messages
        /// </summary>
        public int? UnreadMessagesMin { get; set; }

        public class GetPlantAnalysesForFarmerQueryHandler : IRequestHandler<GetPlantAnalysesForFarmerQuery, IDataResult<PlantAnalysisListResponseDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly IFileStorageService _fileStorageService;
            private readonly IAnalysisMessageRepository _messageRepository;

            public GetPlantAnalysesForFarmerQueryHandler(
                IPlantAnalysisRepository plantAnalysisRepository,
                IFileStorageService fileStorageService,
                IAnalysisMessageRepository messageRepository)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
                _fileStorageService = fileStorageService;
                _messageRepository = messageRepository;
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

                    // 🆕 Fetch messaging status for all analyses (BEFORE applying message filters and sorting)
                    var allAnalysisIds = filteredAnalyses.Select(a => a.Id).ToArray();
                    var messagingStatuses = allAnalysisIds.Length > 0
                        ? await _messageRepository.GetMessagingStatusForAnalysesAsync(request.UserId, allAnalysisIds)
                        : new Dictionary<int, MessagingStatusDto>();

                    // 🆕 NEW: Apply messaging filters (BEFORE pagination)
                    if (!string.IsNullOrEmpty(request.FilterByMessageStatus))
                    {
                        filteredAnalyses = ApplyMessageStatusFilter(
                            filteredAnalyses,
                            messagingStatuses,
                            request.FilterByMessageStatus);
                    }

                    if (request.HasUnreadMessages.HasValue && request.HasUnreadMessages.Value)
                    {
                        filteredAnalyses = filteredAnalyses.Where(a => 
                            messagingStatuses.ContainsKey(a.Id) &&
                            messagingStatuses[a.Id].UnreadCount > 0);
                    }

                    if (request.UnreadMessagesMin.HasValue)
                    {
                        filteredAnalyses = filteredAnalyses.Where(a => 
                            messagingStatuses.ContainsKey(a.Id) &&
                            messagingStatuses[a.Id].UnreadCount >= request.UnreadMessagesMin.Value);
                    }

                    // 🆕 NEW: Apply dynamic sorting (BEFORE pagination)
                    filteredAnalyses = ApplySorting(filteredAnalyses, messagingStatuses, request.SortBy, request.SortOrder);

                    // Get total count AFTER all filters
                    var totalCount = filteredAnalyses.Count();

                    // Apply pagination
                    var plantAnalyses = filteredAnalyses
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToList();

                    var analysisItems = new List<PlantAnalysisListItemDto>();

                    foreach (var analysis in plantAnalyses)
                    {
                        // Get image URL using proper fallback logic (ImageMetadata -> ImageUrl -> ImagePath)
                        string imageUrl = GetImageUrlFromAnalysis(analysis);

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

                        // 🆕 Add messaging status (flat fields for farmer's view)
                        var messagingStatus = messagingStatuses.ContainsKey(analysis.Id)
                            ? messagingStatuses[analysis.Id]
                            : new MessagingStatusDto
                            {
                                HasMessages = false,
                                TotalMessageCount = 0,
                                UnreadCount = 0,
                                ConversationStatus = Entities.Concrete.ConversationStatus.NoContact
                            };

                        // Populate flat messaging fields
                        if (messagingStatus.HasMessages)
                        {
                            listItem.UnreadMessageCount = messagingStatus.UnreadCount;
                            listItem.TotalMessageCount = messagingStatus.TotalMessageCount;
                            listItem.LastMessageDate = messagingStatus.LastMessageDate;
                            listItem.LastMessagePreview = messagingStatus.LastMessagePreview;
                            listItem.LastMessageSenderRole = messagingStatus.LastMessageBy;
                            // For farmers: check if unread messages are FROM sponsor
                            listItem.HasUnreadFromSponsor = messagingStatus.UnreadCount > 0 && messagingStatus.LastMessageBy == "sponsor";
                            listItem.ConversationStatus = messagingStatus.ConversationStatus.ToString();
                        }
                        else
                        {
                            // No messages - set defaults
                            listItem.UnreadMessageCount = 0;
                            listItem.TotalMessageCount = 0;
                            listItem.LastMessageDate = null;
                            listItem.LastMessagePreview = null;
                            listItem.LastMessageSenderRole = null;
                            listItem.HasUnreadFromSponsor = false;
                            listItem.ConversationStatus = "None";
                        }

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

            /// <summary>
            /// Apply dynamic sorting based on sortBy and sortOrder parameters
            /// </summary>
            private IQueryable<Entities.Concrete.PlantAnalysis> ApplySorting(
                IQueryable<Entities.Concrete.PlantAnalysis> analyses,
                Dictionary<int, MessagingStatusDto> messagingStatuses,
                string sortBy,
                string sortOrder)
            {
                var analysesList = analyses.ToList();

                // For message-based sorting, we need to work with the list
                if (sortBy?.ToLower() == "messagecount" || 
                    sortBy?.ToLower() == "unreadcount" || 
                    sortBy?.ToLower() == "lastmessagedate")
                {
                    IEnumerable<Entities.Concrete.PlantAnalysis> sorted = sortBy.ToLower() switch
                    {
                        "messagecount" => sortOrder?.ToLower() == "asc"
                            ? analysesList.OrderBy(a => messagingStatuses.ContainsKey(a.Id) ? messagingStatuses[a.Id].TotalMessageCount : 0)
                            : analysesList.OrderByDescending(a => messagingStatuses.ContainsKey(a.Id) ? messagingStatuses[a.Id].TotalMessageCount : 0),
                        
                        "unreadcount" => sortOrder?.ToLower() == "asc"
                            ? analysesList.OrderBy(a => messagingStatuses.ContainsKey(a.Id) ? messagingStatuses[a.Id].UnreadCount : 0)
                            : analysesList.OrderByDescending(a => messagingStatuses.ContainsKey(a.Id) ? messagingStatuses[a.Id].UnreadCount : 0),
                        
                        "lastmessagedate" => sortOrder?.ToLower() == "asc"
                            ? analysesList.OrderBy(a => messagingStatuses.ContainsKey(a.Id) ? messagingStatuses[a.Id].LastMessageDate ?? DateTime.MinValue : DateTime.MinValue)
                            : analysesList.OrderByDescending(a => messagingStatuses.ContainsKey(a.Id) ? messagingStatuses[a.Id].LastMessageDate ?? DateTime.MinValue : DateTime.MinValue),
                        
                        _ => analysesList.OrderByDescending(a => a.CreatedDate)
                    };

                    return sorted.AsQueryable();
                }

                // For database-based sorting (healthScore, cropType, date)
                return sortBy?.ToLower() switch
                {
                    "healthscore" => sortOrder?.ToLower() == "asc"
                        ? analyses.OrderBy(a => a.OverallHealthScore)
                        : analyses.OrderByDescending(a => a.OverallHealthScore),
                    
                    "croptype" => sortOrder?.ToLower() == "asc"
                        ? analyses.OrderBy(a => a.CropType)
                        : analyses.OrderByDescending(a => a.CropType),
                    
                    _ => sortOrder?.ToLower() == "asc" // default: date
                        ? analyses.OrderBy(a => a.CreatedDate)
                        : analyses.OrderByDescending(a => a.CreatedDate)
                };
            }

            /// <summary>
            /// Apply message status filter to analyses list
            /// </summary>
            private IQueryable<Entities.Concrete.PlantAnalysis> ApplyMessageStatusFilter(
                IQueryable<Entities.Concrete.PlantAnalysis> analyses,
                Dictionary<int, MessagingStatusDto> messagingStatuses,
                string filterValue)
            {
                var analysesList = analyses.ToList();

                var filtered = filterValue?.ToLower() switch
                {
                    "contacted" => analysesList.Where(a =>
                        messagingStatuses.ContainsKey(a.Id) &&
                        messagingStatuses[a.Id].HasMessages),

                    "notcontacted" => analysesList.Where(a =>
                        !messagingStatuses.ContainsKey(a.Id) ||
                        !messagingStatuses[a.Id].HasMessages),

                    "hasresponse" => analysesList.Where(a =>
                        messagingStatuses.ContainsKey(a.Id) &&
                        messagingStatuses[a.Id].HasFarmerResponse),

                    "noresponse" => analysesList.Where(a =>
                        messagingStatuses.ContainsKey(a.Id) &&
                        messagingStatuses[a.Id].HasMessages &&
                        !messagingStatuses[a.Id].HasFarmerResponse),

                    "active" => analysesList.Where(a =>
                        messagingStatuses.ContainsKey(a.Id) &&
                        messagingStatuses[a.Id].ConversationStatus == Entities.Concrete.ConversationStatus.Active),

                    "idle" => analysesList.Where(a =>
                        messagingStatuses.ContainsKey(a.Id) &&
                        messagingStatuses[a.Id].ConversationStatus == Entities.Concrete.ConversationStatus.Idle),

                    _ => analysesList // "all" or invalid value - return unfiltered
                };

                return filtered.AsQueryable();
            }

            private string GetImageUrlFromAnalysis(Entities.Concrete.PlantAnalysis analysis)
            {
                // 1. Try to get URL from ImageMetadata (async analysis)
                if (!string.IsNullOrEmpty(analysis.ImageMetadata))
                {
                    try
                    {
                        var imageMetadata = Newtonsoft.Json.JsonConvert.DeserializeObject<ImageMetadataDto>(analysis.ImageMetadata);
                        if (imageMetadata != null && !string.IsNullOrEmpty(imageMetadata.ImageUrl))
                        {
                            return imageMetadata.ImageUrl;
                        }
                    }
                    catch
                    {
                        // Continue to next fallback
                    }
                }

                // 2. Try analysis.ImageUrl field
                if (!string.IsNullOrEmpty(analysis.ImageUrl))
                {
                    return analysis.ImageUrl;
                }

                // 3. Fallback to ImagePath with base URL conversion
                string imageUrl = analysis.ImagePath;
                if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http"))
                {
                    var baseUrl = _fileStorageService.BaseUrl?.TrimEnd('/');
                    var relativePath = imageUrl.TrimStart('/');
                    imageUrl = $"{baseUrl}/{relativePath}";
                }

                return imageUrl;
            }
        }
    }
}