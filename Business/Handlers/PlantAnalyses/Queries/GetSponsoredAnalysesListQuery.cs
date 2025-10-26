using Business.Services.Sponsorship;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
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
    /// Get paginated list of analyses for sponsor with tier-based filtering
    /// </summary>
    public class GetSponsoredAnalysesListQuery : IRequest<IDataResult<SponsoredAnalysesListResponseDto>>
    {
        public int SponsorId { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Sorting
        public string SortBy { get; set; } = "date"; // date, healthScore, cropType
        public string SortOrder { get; set; } = "desc"; // asc, desc

        // Filters
        public string FilterByTier { get; set; } // S, M, L, XL
        public string FilterByCropType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// Filter by dealer ID to show only analyses distributed by specific dealer
        /// Used for dealer view (their own analyses) or main sponsor view (monitoring dealer)
        /// </summary>
        public int? DealerId { get; set; }


        // NEW: Message Status Filters
        /// <summary>
        /// Filter by message status: all, contacted, notContacted, hasResponse, noResponse, active, idle
        /// </summary>
        public string FilterByMessageStatus { get; set; }

        /// <summary>
        /// Filter to show only analyses with unread messages (from any sender)
        /// &lt;/summary&gt;
        public bool? HasUnreadMessages { get; set; }

        /// &lt;summary&gt;
        /// Filter to show only analyses with unread messages FOR current user (sent TO them)
        /// For farmers: unread messages FROM sponsor
        /// For sponsors: unread messages FROM farmer
        /// &lt;/summary&gt;
        public bool? HasUnreadForCurrentUser { get; set; }

        /// &lt;summary&gt;
        /// Filter to show analyses with at least this many unread messages
        /// &lt;/summary&gt;
        public int? UnreadMessagesMin { get; set; }

        public class GetSponsoredAnalysesListQueryHandler : IRequestHandler<GetSponsoredAnalysesListQuery, IDataResult<SponsoredAnalysesListResponseDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly ISponsorDataAccessService _dataAccessService;
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly IUserRepository _userRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;
            private readonly IAnalysisMessageRepository _messageRepository; // NEW

            public GetSponsoredAnalysesListQueryHandler(
                IPlantAnalysisRepository plantAnalysisRepository,
                ISponsorDataAccessService dataAccessService,
                ISponsorProfileRepository sponsorProfileRepository,
                IUserRepository userRepository,
                ISubscriptionTierRepository subscriptionTierRepository,
                IAnalysisMessageRepository messageRepository) // NEW
            {
                _plantAnalysisRepository = plantAnalysisRepository;
                _dataAccessService = dataAccessService;
                _sponsorProfileRepository = sponsorProfileRepository;
                _userRepository = userRepository;
                _subscriptionTierRepository = subscriptionTierRepository;
                _messageRepository = messageRepository; // NEW
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsoredAnalysesListResponseDto>> Handle(GetSponsoredAnalysesListQuery request, CancellationToken cancellationToken)
            {
                // Validate sponsor profile
                var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(request.SponsorId);
                if (sponsorProfile == null || !sponsorProfile.IsActive)
                {
                    return new ErrorDataResult<SponsoredAnalysesListResponseDto>("Sponsor profile not found or inactive");
                }

                // Get sponsor's access percentage
                var accessPercentage = await _dataAccessService.GetDataAccessPercentageAsync(request.SponsorId);

                // Build query: Get all analyses where user is involved as sponsor OR dealer
                // - As Sponsor: SponsorUserId = userId (codes distributed directly by sponsor)
                // - As Dealer: DealerId = userId (codes distributed by dealer on behalf of sponsor)
                // - Both roles: Show analyses from both capacities
                var query = _plantAnalysisRepository.GetListAsync(a =>
                    (a.SponsorUserId == request.SponsorId || a.DealerId == request.SponsorId) &&
                    a.AnalysisStatus != null
                );

                var allAnalyses = await query;
                var analysesQuery = allAnalyses.AsQueryable();
                
                // Optional: Filter by specific DealerId if provided (for admin/sponsor monitoring specific dealer)
                if (request.DealerId.HasValue && request.DealerId.Value != request.SponsorId)
                {
                    // Admin/Sponsor wants to see a specific dealer's analyses
                    analysesQuery = analysesQuery.Where(a => a.DealerId == request.DealerId.Value);
                }

                // Apply filters
                if (!string.IsNullOrEmpty(request.FilterByCropType))
                {
                    analysesQuery = analysesQuery.Where(a =>
                        a.CropType != null &&
                        a.CropType.Contains(request.FilterByCropType, StringComparison.OrdinalIgnoreCase));
                }

                if (request.StartDate.HasValue)
                {
                    analysesQuery = analysesQuery.Where(a => a.AnalysisDate >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    analysesQuery = analysesQuery.Where(a => a.AnalysisDate <= request.EndDate.Value);
                }

                // Apply sorting
                analysesQuery = request.SortBy?.ToLower() switch
                {
                    "healthscore" => request.SortOrder?.ToLower() == "asc"
                        ? analysesQuery.OrderBy(a => a.OverallHealthScore)
                        : analysesQuery.OrderByDescending(a => a.OverallHealthScore),
                    "croptype" => request.SortOrder?.ToLower() == "asc"
                        ? analysesQuery.OrderBy(a => a.CropType)
                        : analysesQuery.OrderByDescending(a => a.CropType),
                    _ => request.SortOrder?.ToLower() == "asc"
                        ? analysesQuery.OrderBy(a => a.AnalysisDate)
                        : analysesQuery.OrderByDescending(a => a.AnalysisDate)
                };

                var filteredAnalyses = analysesQuery.ToList();

                // NEW: Fetch messaging status for all analyses (BEFORE pagination)
                var analysisIds = filteredAnalyses.Select(a => a.Id).ToArray();
                var messagingStatuses = await _messageRepository.GetMessagingStatusForAnalysesAsync(
                    request.SponsorId,
                    analysisIds);

                // NEW: Apply messaging filters
                if (!string.IsNullOrEmpty(request.FilterByMessageStatus))
                {
                    filteredAnalyses = ApplyMessageStatusFilter(
                        filteredAnalyses,
                        messagingStatuses,
                        request.FilterByMessageStatus).ToList();
                }

                if (request.HasUnreadMessages.HasValue && request.HasUnreadMessages.Value)
                {
                    filteredAnalyses = filteredAnalyses
                        .Where(a => messagingStatuses.ContainsKey(a.Id) &&
                                   messagingStatuses[a.Id].UnreadCount > 0)
                        .ToList();
                }

                // 🆕 NEW: Filter for unread messages FOR current user (from farmer to sponsor)
                if (request.HasUnreadForCurrentUser.HasValue && request.HasUnreadForCurrentUser.Value)
                {
                    filteredAnalyses = filteredAnalyses
                        .Where(a => messagingStatuses.ContainsKey(a.Id) &&
                                   messagingStatuses[a.Id].UnreadCount > 0 &&
                                   messagingStatuses[a.Id].LastMessageBy == "farmer") // Sponsor receives FROM farmer
                        .ToList();
                }

                if (request.UnreadMessagesMin.HasValue)
                {
                    filteredAnalyses = filteredAnalyses
                        .Where(a => messagingStatuses.ContainsKey(a.Id) &&
                                   messagingStatuses[a.Id].UnreadCount >= request.UnreadMessagesMin.Value)
                        .ToList();
                }

                // Update total count after messaging filters
                var totalCount = filteredAnalyses.Count;

                // Calculate pagination
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
                var skip = (request.Page - 1) * request.PageSize;
                var pagedAnalyses = filteredAnalyses.Skip(skip).Take(request.PageSize).ToList();

                // Map to DTOs with messaging status
                var items = pagedAnalyses.Select(analysis =>
                {
                    var dto = MapToSummaryDto(
                        analysis,
                        accessPercentage,
                        sponsorProfile);

                    // Add messaging status (both nested and flat for backward compatibility)
                    var messagingStatus = messagingStatuses.ContainsKey(analysis.Id)
                        ? messagingStatuses[analysis.Id]
                        : new MessagingStatusDto
                        {
                            HasMessages = false,
                            TotalMessageCount = 0,
                            UnreadCount = 0,
                            ConversationStatus = ConversationStatus.NoContact
                        };

                    // Nested format (DEPRECATED but kept for backward compatibility)
                    dto.MessagingStatus = messagingStatus;

                    // 🆕 Flat fields (Mobile team preference)
                    if (messagingStatus.HasMessages)
                    {
                        dto.UnreadMessageCount = messagingStatus.UnreadCount;
                        dto.TotalMessageCount = messagingStatus.TotalMessageCount;
                        dto.LastMessageDate = messagingStatus.LastMessageDate;
                        dto.LastMessagePreview = messagingStatus.LastMessagePreview;
                        dto.LastMessageSenderRole = messagingStatus.LastMessageBy;
                        dto.HasUnreadFromFarmer = messagingStatus.UnreadCount > 0 && messagingStatus.LastMessageBy == "farmer";
                        dto.ConversationStatus = messagingStatus.ConversationStatus.ToString();
                    }
                    else
                    {
                        // No messages - all fields null (mobile team requirement)
                        dto.UnreadMessageCount = 0;
                        dto.TotalMessageCount = 0;
                        dto.LastMessageDate = null;
                        dto.LastMessagePreview = null;
                        dto.LastMessageSenderRole = null;
                        dto.HasUnreadFromFarmer = false;
                        dto.ConversationStatus = "None";
                    }

                    return dto;
                }).ToArray();

                // Calculate summary statistics
                var summary = new SponsoredAnalysesListSummaryDto
                {
                    TotalAnalyses = totalCount,
                    AverageHealthScore = filteredAnalyses.Any()
                        ? (decimal)filteredAnalyses.Average(a => a.OverallHealthScore)
                        : 0,
                    TopCropTypes = filteredAnalyses
                        .Where(a => !string.IsNullOrEmpty(a.CropType))
                        .GroupBy(a => a.CropType)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => g.Key)
                        .ToArray(),
                    AnalysesThisMonth = filteredAnalyses
                        .Count(a => a.AnalysisDate.Month == DateTime.Now.Month &&
                                    a.AnalysisDate.Year == DateTime.Now.Year),

                    // Messaging statistics
                    ContactedAnalyses = messagingStatuses.Count(kvp => kvp.Value.HasMessages),
                    NotContactedAnalyses = totalCount - messagingStatuses.Count(kvp => kvp.Value.HasMessages),
                    ActiveConversations = messagingStatuses.Count(kvp =>
                        kvp.Value.ConversationStatus == ConversationStatus.Active),
                    PendingResponses = messagingStatuses.Count(kvp =>
                        kvp.Value.ConversationStatus == ConversationStatus.Pending),
                    TotalUnreadMessages = messagingStatuses.Sum(kvp => kvp.Value.UnreadCount),
                    // 🆕 Mobile team requirement: count of analyses with unread messages
                    AnalysesWithUnread = messagingStatuses.Count(kvp => kvp.Value.UnreadCount > 0)
                };

                var response = new SponsoredAnalysesListResponseDto
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1,
                    Summary = summary
                };

                return new SuccessDataResult<SponsoredAnalysesListResponseDto>(
                    response,
                    $"Retrieved {items.Length} analyses (page {request.Page} of {totalPages})"
                );
            }

            private SponsoredAnalysisSummaryDto MapToSummaryDto(
                Entities.Concrete.PlantAnalysis analysis,
                int accessPercentage,
                Entities.Concrete.SponsorProfile sponsorProfile)
            {
                var dto = new SponsoredAnalysisSummaryDto
                {
                    // Core fields (always available)
                    AnalysisId = analysis.Id,
                    AnalysisDate = analysis.AnalysisDate,
                    AnalysisStatus = analysis.AnalysisStatus,
                    CropType = analysis.CropType,

                    // Tier info
                    TierName = GetTierName(accessPercentage),
                    AccessPercentage = accessPercentage,
                    CanMessage = accessPercentage >= 30, // M, L, XL
                    CanViewLogo = true, // All tiers on result screen

                    // Sponsor info
                    SponsorInfo = new SponsorDisplayInfoDto
                    {
                        SponsorId = sponsorProfile.SponsorId,
                        CompanyName = sponsorProfile.CompanyName,
                        LogoUrl = sponsorProfile.SponsorLogoUrl,
                        WebsiteUrl = sponsorProfile.WebsiteUrl
                    }
                };

                // 30% Access Fields (S & M tiers)
                if (accessPercentage >= 30)
                {
                    dto.OverallHealthScore = analysis.OverallHealthScore;
                    dto.PlantSpecies = analysis.PlantSpecies;
                    dto.PlantVariety = analysis.PlantVariety;
                    dto.GrowthStage = analysis.GrowthStage;
                    // Use ImageUrl (original) or ImagePath (thumbnail) - prefer original for better quality
                    dto.ImageUrl = !string.IsNullOrEmpty(analysis.ImageUrl)
                        ? analysis.ImageUrl
                        : analysis.ImagePath;
                }

                // 60% Access Fields (L tier)
                if (accessPercentage >= 60)
                {
                    dto.VigorScore = analysis.VigorScore;
                    dto.HealthSeverity = analysis.HealthSeverity;
                    dto.PrimaryConcern = analysis.PrimaryConcern;
                    dto.Location = analysis.Location;
                    // Recommendations removed from list view - too large for list display
                    // Use GET /api/v1/sponsorship/analyses/{id} for full details including recommendations
                }

                // 100% Access Fields (XL tier)
                if (accessPercentage >= 100)
                {
                    // Fetch farmer info from User entity if available
                    if (analysis.UserId.HasValue)
                    {
                        var farmer = _userRepository.Get(u => u.UserId == analysis.UserId.Value);
                        if (farmer != null)
                        {
                            dto.FarmerName = farmer.FullName;
                            dto.FarmerPhone = farmer.MobilePhones ?? analysis.ContactPhone;
                            dto.FarmerEmail = farmer.Email ?? analysis.ContactEmail;
                        }
                    }

                    // Fallback to analysis contact info if User not found
                    if (string.IsNullOrEmpty(dto.FarmerName))
                    {
                        dto.FarmerPhone = analysis.ContactPhone;
                        dto.FarmerEmail = analysis.ContactEmail;
                    }
                }

                return dto;
            }


            /// <summary>
            /// Apply message status filter to analyses list
            /// </summary>
            private IEnumerable<Entities.Concrete.PlantAnalysis> ApplyMessageStatusFilter(
                IEnumerable<Entities.Concrete.PlantAnalysis> analyses,
                Dictionary<int, MessagingStatusDto> messagingStatuses,
                string filterValue)
            {
                return filterValue?.ToLower() switch
                {
                    "contacted" => analyses.Where(a =>
                        messagingStatuses.ContainsKey(a.Id) &&
                        messagingStatuses[a.Id].HasMessages),

                    "notcontacted" => analyses.Where(a =>
                        !messagingStatuses.ContainsKey(a.Id) ||
                        !messagingStatuses[a.Id].HasMessages),

                    "hasresponse" => analyses.Where(a =>
                        messagingStatuses.ContainsKey(a.Id) &&
                        messagingStatuses[a.Id].HasFarmerResponse),

                    "noresponse" => analyses.Where(a =>
                        messagingStatuses.ContainsKey(a.Id) &&
                        messagingStatuses[a.Id].HasMessages &&
                        !messagingStatuses[a.Id].HasFarmerResponse),

                    "active" => analyses.Where(a =>
                        messagingStatuses.ContainsKey(a.Id) &&
                        messagingStatuses[a.Id].ConversationStatus == ConversationStatus.Active),

                    "idle" => analyses.Where(a =>
                        messagingStatuses.ContainsKey(a.Id) &&
                        messagingStatuses[a.Id].ConversationStatus == ConversationStatus.Idle),

                    _ => analyses // "all" or invalid value - return unfiltered
                };
            }

            private string GetTierName(int accessPercentage)
            {
                return accessPercentage switch
                {
                    30 => "S/M",
                    60 => "L",
                    100 => "XL",
                    _ => "Unknown"
                };
            }
        }
    }
}
