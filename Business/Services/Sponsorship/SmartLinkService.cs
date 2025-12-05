using Business.Services.Subscription;
using DataAccess.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public class SmartLinkService : ISmartLinkService
    {
        private readonly ISmartLinkRepository _smartLinkRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITierFeatureService _tierFeatureService;

        public SmartLinkService(
            ISmartLinkRepository smartLinkRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            IUserRepository userRepository,
            ITierFeatureService tierFeatureService)
        {
            _smartLinkRepository = smartLinkRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _userRepository = userRepository;
            _tierFeatureService = tierFeatureService;
        }

        public async Task<bool> CanCreateSmartLinksAsync(int sponsorId)
        {
            try
            {
                var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
                if (profile == null)
                {
                    Console.WriteLine($"[SmartLinkService] No sponsor profile found for sponsorId: {sponsorId}");
                    return false;
                }
                
                if (!profile.IsActive)
                {
                    Console.WriteLine($"[SmartLinkService] Sponsor profile is inactive for sponsorId: {sponsorId}");
                    return false;
                }

                // Check if sponsor has smart_links feature in any of their active purchases
                if (profile.SponsorshipPurchases != null && profile.SponsorshipPurchases.Any())
                {
                    foreach (var purchase in profile.SponsorshipPurchases)
                    {
                        // Use TierFeatureService to check if tier has smart_links feature
                        var hasSmartLinks = await _tierFeatureService.HasFeatureAccessAsync(purchase.SubscriptionTierId, "smart_links");
                        if (hasSmartLinks)
                        {
                            Console.WriteLine($"[SmartLinkService] Sponsor {sponsorId} has smart_links feature, can create smart links");
                            return true;
                        }
                    }
                    Console.WriteLine($"[SmartLinkService] Sponsor {sponsorId} does not have smart_links feature");
                }
                else
                {
                    Console.WriteLine($"[SmartLinkService] No purchases found for sponsorId: {sponsorId}");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartLinkService] Error checking smart link permissions: {ex.Message}");
                return false;
            }
        }

        public async Task<int> GetMaxSmartLinksAsync(int sponsorId)
        {
            var canCreate = await CanCreateSmartLinksAsync(sponsorId);
            if (!canCreate)
                return 0;

            // XL paket için varsayılan 50 smart link hakkı
            return 50;
        }

        public async Task<int> GetActiveSmartLinksCountAsync(int sponsorId)
        {
            var links = await _smartLinkRepository.GetBySponsorIdAsync(sponsorId);
            return links.Count(l => l.IsActive);
        }

        public async Task<Entities.Concrete.SmartLink> CreateSmartLinkAsync(Entities.Concrete.SmartLink smartLink)
        {
            try
            {
                if (!await CanCreateSmartLinksAsync(smartLink.SponsorId))
                {
                    Console.WriteLine($"[SmartLinkService] Sponsor {smartLink.SponsorId} cannot create smart links");
                    return null;
                }

                var maxLinks = await GetMaxSmartLinksAsync(smartLink.SponsorId);
                var activeCount = await GetActiveSmartLinksCountAsync(smartLink.SponsorId);
                
                if (activeCount >= maxLinks)
                {
                    Console.WriteLine($"[SmartLinkService] Quota exceeded for sponsor {smartLink.SponsorId}: {activeCount}/{maxLinks}");
                    return null; // Quota exceeded
                }

                smartLink.CreatedDate = DateTime.Now;
                smartLink.IsActive = true;
                smartLink.IsApproved = false; // Requires approval
                smartLink.Priority = smartLink.Priority > 0 ? smartLink.Priority : 50; // Use provided or default priority
                smartLink.DisplayCount = 0;
                smartLink.ClickCount = 0;
                smartLink.UniqueClickCount = 0;
                
                var user = await _userRepository.GetAsync(u => u.UserId == smartLink.SponsorId);
                smartLink.SponsorName = user?.FullName ?? "Unknown Sponsor";

                _smartLinkRepository.Add(smartLink);
                await _smartLinkRepository.SaveChangesAsync();
                
                Console.WriteLine($"[SmartLinkService] Smart link created successfully for sponsor {smartLink.SponsorId}");
                return smartLink;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartLinkService] Error creating smart link: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Entities.Concrete.SmartLink>> GetMatchingLinksAsync(Entities.Concrete.PlantAnalysis analysis)
        {
            // Analiz verilerine göre anahtar kelimeler çıkar
            var keywords = ExtractKeywordsFromAnalysis(analysis);
            
            var matchingLinks = await _smartLinkRepository.GetMatchingLinksAsync(
                keywords.ToArray(), 
                analysis.CropType,
                ExtractDiseaseFromAnalysis(analysis),
                ExtractPestFromAnalysis(analysis)
            );

            // Relevance score'a göre sırala
            foreach (var link in matchingLinks)
            {
                link.RelevanceScore = await CalculateRelevanceScoreAsync(link, analysis);
            }

            return matchingLinks.OrderByDescending(l => l.RelevanceScore)
                              .ThenByDescending(l => l.Priority)
                              .Take(5) // En fazla 5 link göster
                              .ToList();
        }

        public async Task<List<Entities.Concrete.SmartLink>> GetSponsorLinksAsync(int sponsorId)
        {
            try
            {
                var links = await _smartLinkRepository.GetBySponsorIdAsync(sponsorId);
                Console.WriteLine($"[SmartLinkService] Retrieved {links?.Count ?? 0} smart links for sponsor {sponsorId}");
                return links ?? new List<Entities.Concrete.SmartLink>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartLinkService] Error getting sponsor links: {ex.Message}");
                return new List<Entities.Concrete.SmartLink>();
            }
        }

        public async Task<Entities.Concrete.SmartLink> UpdateSmartLinkAsync(Entities.Concrete.SmartLink smartLink)
        {
            var existing = await _smartLinkRepository.GetAsync(l => l.Id == smartLink.Id && l.SponsorId == smartLink.SponsorId);
            if (existing == null)
                return null;

            existing.LinkText = smartLink.LinkText;
            existing.LinkDescription = smartLink.LinkDescription;
            existing.Keywords = smartLink.Keywords;
            existing.TargetCropTypes = smartLink.TargetCropTypes;
            existing.TargetDiseases = smartLink.TargetDiseases;
            existing.TargetPests = smartLink.TargetPests;
            existing.Priority = smartLink.Priority;
            existing.IsActive = smartLink.IsActive;
            existing.UpdatedDate = DateTime.Now;

            _smartLinkRepository.Update(existing);
            await _smartLinkRepository.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteSmartLinkAsync(int linkId, int sponsorId)
        {
            var link = await _smartLinkRepository.GetTrackedAsync(l => l.Id == linkId && l.SponsorId == sponsorId);
            if (link == null)
                return false;

            _smartLinkRepository.Delete(link);
            await _smartLinkRepository.SaveChangesAsync();
            return true;
        }

        public async Task IncrementClickAsync(int linkId)
        {
            await _smartLinkRepository.IncrementClickCountAsync(linkId);
            await UpdateSmartLinkPerformanceAsync(linkId);
        }

        public async Task<List<Entities.Concrete.SmartLink>> GetTopPerformingLinksAsync(int sponsorId, int count = 10)
        {
            return await _smartLinkRepository.GetTopPerformingLinksAsync(sponsorId, count);
        }

        public async Task<decimal> CalculateRelevanceScoreAsync(Entities.Concrete.SmartLink link, Entities.Concrete.PlantAnalysis analysis)
        {
            decimal score = 0;
            
            try
            {
                // Keyword matching (40 points max)
                var linkKeywords = JsonSerializer.Deserialize<string[]>(link.Keywords ?? "[]");
                var analysisKeywords = ExtractKeywordsFromAnalysis(analysis);
                
                if (linkKeywords != null && analysisKeywords.Any())
                {
                    var matchingKeywords = linkKeywords.Intersect(analysisKeywords, StringComparer.OrdinalIgnoreCase).Count();
                    score += Math.Min(matchingKeywords * 10, 40);
                }

                // Crop type matching (25 points max)
                var targetCropTypes = JsonSerializer.Deserialize<string[]>(link.TargetCropTypes ?? "[]");
                if (targetCropTypes != null && !string.IsNullOrEmpty(analysis.CropType) &&
                    targetCropTypes.Any(ct => ct.Equals(analysis.CropType, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 25;
                }

                // Disease matching (20 points max)
                var targetDiseases = JsonSerializer.Deserialize<string[]>(link.TargetDiseases ?? "[]");
                var analysisDisease = ExtractDiseaseFromAnalysis(analysis);
                if (targetDiseases != null && !string.IsNullOrEmpty(analysisDisease) &&
                    targetDiseases.Any(d => d.Equals(analysisDisease, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 20;
                }

                // Pest matching (15 points max)
                var targetPests = JsonSerializer.Deserialize<string[]>(link.TargetPests ?? "[]");
                var analysisPest = ExtractPestFromAnalysis(analysis);
                if (targetPests != null && !string.IsNullOrEmpty(analysisPest) &&
                    targetPests.Any(p => p.Equals(analysisPest, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 15;
                }
            }
            catch (JsonException)
            {
                // JSON parsing error, use base priority
                score = link.Priority;
            }

            return score;
        }

        public async Task<List<Entities.Concrete.SmartLink>> GetPromotionalLinksAsync()
        {
            return await _smartLinkRepository.GetPromotionalLinksAsync();
        }

        public async Task<bool> ApproveSmartLinkAsync(int linkId, int approvedByUserId)
        {
            await _smartLinkRepository.ApproveSmartLinkAsync(linkId, approvedByUserId);
            return true;
        }

        public async Task<List<Entities.Concrete.SmartLink>> GetPendingApprovalLinksAsync()
        {
            return await _smartLinkRepository.GetPendingApprovalAsync();
        }

        public async Task UpdateSmartLinkPerformanceAsync(int linkId)
        {
            var link = await _smartLinkRepository.GetAsync(l => l.Id == linkId);
            if (link != null && link.DisplayCount > 0)
            {
                var ctr = (decimal)link.ClickCount / link.DisplayCount * 100;
                await _smartLinkRepository.UpdateClickThroughRateAsync(linkId, ctr);
            }
        }

        public async Task<List<Entities.Concrete.SmartLink>> GetAIOptimizedLinksAsync(Entities.Concrete.PlantAnalysis analysis)
        {
            var matchingLinks = await GetMatchingLinksAsync(analysis);
            
            // AI optimization: Prioritize based on recent performance
            var optimizedLinks = new List<Entities.Concrete.SmartLink>();
            
            foreach (var link in matchingLinks)
            {
                // AI score considers both relevance and performance
                var aiScore = (link.RelevanceScore ?? 0) * 0.7m + (link.ClickThroughRate * 0.3m);
                link.RelevanceScore = aiScore;
                
                if (aiScore > 10) // Minimum threshold
                {
                    optimizedLinks.Add(link);
                }
            }
            
            return optimizedLinks.OrderByDescending(l => l.RelevanceScore).Take(3).ToList();
        }

        private List<string> ExtractKeywordsFromAnalysis(Entities.Concrete.PlantAnalysis analysis)
        {
            var keywords = new List<string>();
            
            if (!string.IsNullOrEmpty(analysis.CropType))
                keywords.Add(analysis.CropType);
            
            if (!string.IsNullOrEmpty(analysis.PrimaryConcern))
                keywords.AddRange(analysis.PrimaryConcern.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            
            if (!string.IsNullOrEmpty(analysis.PrimaryDeficiency))
                keywords.Add(analysis.PrimaryDeficiency);
            
            if (!string.IsNullOrEmpty(analysis.HealthSeverity))
                keywords.Add(analysis.HealthSeverity);
            
            // Extract from diseases and pests (legacy fields)
            if (!string.IsNullOrEmpty(analysis.Diseases))
                keywords.AddRange(analysis.Diseases.Split(',', StringSplitOptions.RemoveEmptyEntries));
            
            if (!string.IsNullOrEmpty(analysis.Pests))
                keywords.AddRange(analysis.Pests.Split(',', StringSplitOptions.RemoveEmptyEntries));

            return keywords.Select(k => k.Trim().ToLower()).Distinct().ToList();
        }

        private string ExtractDiseaseFromAnalysis(Entities.Concrete.PlantAnalysis analysis)
        {
            if (!string.IsNullOrEmpty(analysis.Diseases))
                return analysis.Diseases.Split(',')[0].Trim();
            
            // Try to extract from DiseaseSymptoms JSON
            try
            {
                if (!string.IsNullOrEmpty(analysis.DiseaseSymptoms))
                {
                    var symptoms = JsonSerializer.Deserialize<string[]>(analysis.DiseaseSymptoms);
                    return symptoms?.FirstOrDefault();
                }
            }
            catch (JsonException) { }
            
            return null;
        }

        private string ExtractPestFromAnalysis(Entities.Concrete.PlantAnalysis analysis)
        {
            if (!string.IsNullOrEmpty(analysis.Pests))
                return analysis.Pests.Split(',')[0].Trim();
            
            return null;
        }
    }
}