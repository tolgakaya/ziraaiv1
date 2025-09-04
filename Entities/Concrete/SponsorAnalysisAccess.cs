using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    public class SponsorAnalysisAccess : IEntity
    {
        public int Id { get; set; }
        
        // Access Information
        public int SponsorId { get; set; } // Sponsor user ID
        public int PlantAnalysisId { get; set; } // Analysis being accessed
        public int FarmerId { get; set; } // Farmer who owns the analysis
        
        // Access Level Based on Sponsor Tier
        public string AccessLevel { get; set; } // View30, View60, View100
        public int AccessPercentage { get; set; } // 30, 60, or 100
        
        // Tracking
        public DateTime FirstViewedDate { get; set; }
        public DateTime? LastViewedDate { get; set; }
        public int ViewCount { get; set; }
        public DateTime? DownloadedDate { get; set; }
        public bool HasDownloaded { get; set; }
        
        // Data Access Details
        public string AccessedFields { get; set; } // JSON array of accessed field names
        public string RestrictedFields { get; set; } // JSON array of restricted field names
        public bool CanViewHealthScore { get; set; }
        public bool CanViewDiseases { get; set; }
        public bool CanViewPests { get; set; }
        public bool CanViewNutrients { get; set; }
        public bool CanViewRecommendations { get; set; }
        public bool CanViewFarmerContact { get; set; }
        public bool CanViewLocation { get; set; }
        public bool CanViewImages { get; set; }
        
        // Interaction
        public bool HasContactedFarmer { get; set; }
        public DateTime? ContactDate { get; set; }
        public string ContactMethod { get; set; } // Message, Email, Phone
        public string Notes { get; set; }
        
        // Sponsorship Context
        public int? SponsorshipCodeId { get; set; } // Which code was used for this farmer
        public int? SponsorshipPurchaseId { get; set; } // Which purchase this access is from
        
        // Audit
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public virtual User Sponsor { get; set; }
        
        [JsonIgnore]
        public virtual PlantAnalysis PlantAnalysis { get; set; }
        
        [JsonIgnore]
        public virtual User Farmer { get; set; }
        
        [JsonIgnore]
        public virtual SponsorshipCode SponsorshipCode { get; set; }
        
        [JsonIgnore]
        public virtual SponsorshipPurchase SponsorshipPurchase { get; set; }
    }
}