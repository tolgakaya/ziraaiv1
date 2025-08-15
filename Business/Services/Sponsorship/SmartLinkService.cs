using Business.Services.Sponsorship;
using DataAccess.Abstract;
using Entities.Concrete;
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

        public SmartLinkService(
            ISmartLinkRepository smartLinkRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            IUserRepository userRepository)
        {
            _smartLinkRepository = smartLinkRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _userRepository = userRepository;
        }

        public async Task<bool> CanCreateSmartLinksAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile == null || !profile.IsActive || !profile.IsVerified)
                return false;

            // Sadece XL paketi smart link oluşturabilir
            return profile.HasSmartLinking;
        }

        public async Task<int> GetMaxSmartLinksAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile == null || !profile.HasSmartLinking)
                return 0;

            // XL paket için varsayılan 50 smart link hakkı
            return 50;
        }

        public async Task<int> GetActiveSmartLinksCountAsync(int sponsorId)
        {
            var links = await _smartLinkRepository.GetBySponsorIdAsync(sponsorId);
            return links.Count(l => l.IsActive);
        }

        public async Task<SmartLink> CreateSmartLinkAsync(SmartLink smartLink)
        {
            if (!await CanCreateSmartLinksAsync(smartLink.SponsorId))
                return null;

            var maxLinks = await GetMaxSmartLinksAsync(smartLink.SponsorId);
            var activeCount = await GetActiveSmartLinksCountAsync(smartLink.SponsorId);
            
            if (activeCount >= maxLinks)
                return null; // Quota exceeded

            smartLink.CreatedDate = DateTime.Now;
            smartLink.IsActive = true;
            smartLink.IsApproved = false; // Requires approval
            smartLink.Priority = 50; // Default priority
            smartLink.DisplayCount = 0;
            smartLink.ClickCount = 0;
            smartLink.UniqueClickCount = 0;
            
            var user = await _userRepository.GetAsync(u => u.UserId == smartLink.SponsorId);
            smartLink.SponsorName = user?.FullName;

            await _smartLinkRepository.AddAsync(smartLink);
            return smartLink;
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

        public async Task<List<SmartLink>> GetSponsorLinksAsync(int sponsorId)
        {
            return await _smartLinkRepository.GetBySponsorIdAsync(sponsorId);
        }

        public async Task<SmartLink> UpdateSmartLinkAsync(SmartLink smartLink)
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

            await _smartLinkRepository.UpdateAsync(existing);
            return existing;
        }

        public async Task<bool> DeleteSmartLinkAsync(int linkId, int sponsorId)
        {
            var link = await _smartLinkRepository.GetAsync(l => l.Id == linkId && l.SponsorId == sponsorId);
            if (link == null)
                return false;

            await _smartLinkRepository.DeleteAsync(link);
            return true;
        }

        public async Task IncrementClickAsync(int linkId)
        {
            await _smartLinkRepository.IncrementClickCountAsync(linkId);
            await UpdateSmartLinkPerformanceAsync(linkId);
        }

        public async Task<List<SmartLink>> GetTopPerformingLinksAsync(int sponsorId, int count = 10)
        {
            return await _smartLinkRepository.GetTopPerformingLinksAsync(sponsorId, count);
        }

        public async Task<decimal> CalculateRelevanceScoreAsync(SmartLink link, PlantAnalysis analysis)
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
                score = link.Priority ?? 0;
            }

            return score;
        }

        public async Task<List<SmartLink>> GetPromotionalLinksAsync()
        {
            return await _smartLinkRepository.GetPromotionalLinksAsync();
        }

        public async Task<bool> ApproveSmartLinkAsync(int linkId, int approvedByUserId)
        {
            await _smartLinkRepository.ApproveSmartLinkAsync(linkId, approvedByUserId);
            return true;
        }

        public async Task<List<SmartLink>> GetPendingApprovalLinksAsync()
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

        public async Task<List<SmartLink>> GetAIOptimizedLinksAsync(PlantAnalysis analysis)
        {
            var matchingLinks = await GetMatchingLinksAsync(analysis);
            
            // AI optimization: Prioritize based on recent performance
            var optimizedLinks = new List<SmartLink>();
            
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

        private List<string> ExtractKeywordsFromAnalysis(PlantAnalysis analysis)
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

        private string ExtractDiseaseFromAnalysis(PlantAnalysis analysis)
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

        private string ExtractPestFromAnalysis(PlantAnalysis analysis)
        {
            if (!string.IsNullOrEmpty(analysis.Pests))
                return analysis.Pests.Split(',')[0].Trim();
            
            return null;
        }
    }
}