using Core.Attributes;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Entities.Dtos
{
    /// <summary>
    /// Multi-image plant analysis request DTO for async processing.
    /// Supports up to 5 images for comprehensive AI analysis.
    /// </summary>
    public class PlantAnalysisMultiImageRequestDto : IDto
    {
        /// <summary>
        /// Main plant image (REQUIRED).
        /// Base64 encoded image with data URI scheme format.
        /// Supported formats: JPEG, PNG, GIF, WebP, BMP, SVG, TIFF
        /// Example: "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEA..."
        /// </summary>
        [Required(ErrorMessage = "Main image is required")]
        [ValidImage(500.0)]
        public string Image { get; set; }

        /// <summary>
        /// Top view of leaf (OPTIONAL).
        /// Base64 encoded image with data URI scheme format.
        /// </summary>
        [ValidImage(500.0)]
        public string LeafTopImage { get; set; }

        /// <summary>
        /// Bottom view of leaf (OPTIONAL).
        /// Base64 encoded image with data URI scheme format.
        /// </summary>
        [ValidImage(500.0)]
        public string LeafBottomImage { get; set; }

        /// <summary>
        /// Full plant overview image (OPTIONAL).
        /// Base64 encoded image with data URI scheme format.
        /// </summary>
        [ValidImage(500.0)]
        public string PlantOverviewImage { get; set; }

        /// <summary>
        /// Root system image (OPTIONAL).
        /// Base64 encoded image with data URI scheme format.
        /// </summary>
        [ValidImage(500.0)]
        public string RootImage { get; set; }

        // User context - automatically set by server (DO NOT send from client)
        public int? UserId { get; set; }

        // Security fields - automatically set by server based on authenticated user (DO NOT send from client)
        public string FarmerId { get; set; }
        public string SponsorId { get; set; }
        public int? SponsorUserId { get; set; }        // Actual sponsor user ID
        public int? SponsorshipCodeId { get; set; }    // SponsorshipCode table ID

        // Optional field for field identification
        public string FieldId { get; set; }
        public string CropType { get; set; }
        public string Location { get; set; }
        public GpsCoordinates GpsCoordinates { get; set; }
        public int? Altitude { get; set; }
        public DateTime? PlantingDate { get; set; }
        public DateTime? ExpectedHarvestDate { get; set; }
        public DateTime? LastFertilization { get; set; }
        public DateTime? LastIrrigation { get; set; }
        public List<string> PreviousTreatments { get; set; }
        public string SoilType { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public string WeatherConditions { get; set; }
        public string UrgencyLevel { get; set; }
        public string Notes { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public AdditionalInfoData AdditionalInfo { get; set; }
    }
}
