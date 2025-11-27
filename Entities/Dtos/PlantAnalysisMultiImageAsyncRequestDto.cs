using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Multi-image plant analysis async request DTO for RabbitMQ queue messages.
    /// Contains image URLs (not base64) to reduce message size and token usage.
    /// </summary>
    public class PlantAnalysisMultiImageAsyncRequestDto
    {
        /// <summary>
        /// Main plant image URL (REQUIRED).
        /// Uploaded to storage service, used by AI analysis.
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Top view of leaf image URL (OPTIONAL).
        /// </summary>
        public string LeafTopUrl { get; set; }

        /// <summary>
        /// Bottom view of leaf image URL (OPTIONAL).
        /// </summary>
        public string LeafBottomUrl { get; set; }

        /// <summary>
        /// Full plant overview image URL (OPTIONAL).
        /// </summary>
        public string PlantOverviewUrl { get; set; }

        /// <summary>
        /// Root system image URL (OPTIONAL).
        /// </summary>
        public string RootUrl { get; set; }

        // User context - preserved from request
        public int? UserId { get; set; }

        // Security fields - preserved from request
        public string FarmerId { get; set; }
        public string SponsorId { get; set; }
        public int? SponsorUserId { get; set; }
        public int? SponsorshipCodeId { get; set; }

        // Optional field identification
        public string FieldId { get; set; }
        public string CropType { get; set; }
        public string Location { get; set; }
        public GpsCoordinates GpsCoordinates { get; set; }
        public int? Altitude { get; set; }
        
        // Temporal data
        public DateTime? PlantingDate { get; set; }
        public DateTime? ExpectedHarvestDate { get; set; }
        public DateTime? LastFertilization { get; set; }
        public DateTime? LastIrrigation { get; set; }
        
        // Treatment and environment
        public List<string> PreviousTreatments { get; set; }
        public string SoilType { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public string WeatherConditions { get; set; }
        
        // Additional context
        public string UrgencyLevel { get; set; }
        public string Notes { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public AdditionalInfoData AdditionalInfo { get; set; }

        // Queue management fields
        public string ResponseQueue { get; set; } = "plant-analysis-results";
        public string CorrelationId { get; set; }
        public string AnalysisId { get; set; }
    }
}
