using System;

namespace Entities.Dtos
{
    public class MockN8NRequestDto
    {
        public string AnalysisId { get; set; }
        public string ImageUrl { get; set; } // New: URL support
        public string Image { get; set; } // Legacy: base64 support
        public int? UserId { get; set; }
        public string FarmerId { get; set; }
        public string SponsorId { get; set; }
        public string Location { get; set; }
        public GpsCoordinates GpsCoordinates { get; set; }
        public string CropType { get; set; }
        public string FieldId { get; set; }
        public string UrgencyLevel { get; set; }
        public string Notes { get; set; }
        
        // Additional fields
        public int? Altitude { get; set; }
        public DateTime? PlantingDate { get; set; }
        public DateTime? ExpectedHarvestDate { get; set; }
        public DateTime? LastFertilization { get; set; }
        public DateTime? LastIrrigation { get; set; }
        public string[] PreviousTreatments { get; set; }
        public string WeatherConditions { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public string SoilType { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public AdditionalInfoData AdditionalInfo { get; set; }
    }
}