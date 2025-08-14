using System;

namespace Entities.Dtos
{
    public class PlantAnalysisAsyncRequestDto
    {
        public string Image { get; set; }
        public string ImageUrl { get; set; } // New: URL option to avoid token limits
        public int? UserId { get; set; } // Asenkron işlemde UserId korunması için
        public string FarmerId { get; set; }
        public string SponsorId { get; set; }
        public int? SponsorUserId { get; set; }        // Actual sponsor user ID
        public int? SponsorshipCodeId { get; set; }    // SponsorshipCode table ID
        public string Location { get; set; }
        public GpsCoordinates GpsCoordinates { get; set; }
        public string CropType { get; set; }
        public string FieldId { get; set; }
        public string UrgencyLevel { get; set; }
        public string Notes { get; set; }
        public string ResponseQueue { get; set; } = "plant-analysis-results";
        public string CorrelationId { get; set; }
        public string AnalysisId { get; set; }
        
        // Additional optional fields
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