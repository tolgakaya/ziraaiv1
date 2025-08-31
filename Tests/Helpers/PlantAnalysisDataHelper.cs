using System;
using System.Collections.Generic;
using Entities.Concrete;
using Entities.Dtos;

namespace Tests.Helpers
{
    public static class PlantAnalysisDataHelper
    {
        public static PlantAnalysis GetPlantAnalysis(string analysisId = "analysis_test_123")
        {
            return new PlantAnalysis
            {
                Id = 1,
                AnalysisId = analysisId,
                FarmerId = "F001",
                UserId = 1,
                ImagePath = "uploads/plant-images/test_image.jpg",
                AnalysisDate = DateTime.Now,
                AnalysisStatus = "Completed",
                Status = true,
                CreatedDate = DateTime.Now.AddHours(-1),
                UpdatedDate = DateTime.Now,
                
                // Plant details
                CropType = "tomato",
                Location = "Antalya, Turkey",
                Latitude = 36.8969m,
                Longitude = 30.7133m,
                Altitude = 50,
                FieldId = "Field-001",
                PlantingDate = DateTime.Now.AddDays(-60),
                ExpectedHarvestDate = DateTime.Now.AddDays(30),
                
                // Analysis results
                PlantSpecies = "Solanum lycopersicum",
                PlantVariety = "Cherry Tomato",
                PlantType = "Vegetable",
                OverallHealthScore = 8,
                HealthStatus = "Good",
                Diseases = "[\"Minor leaf spot\"]",
                Pests = "[]",
                ElementDeficiencies = "[\"Nitrogen\"]",
                StressIndicators = "[\"Mild water stress\"]",
                DiseaseSymptoms = "[\"Small brown spots on lower leaves\"]",
                
                // Recommendations
                Recommendations = "[\"Apply nitrogen fertilizer\", \"Monitor for pest development\", \"Ensure adequate watering\"]",
                TreatmentPlan = "Apply balanced NPK fertilizer (10-10-10) at 2kg per hectare",
                PriorityLevel = "Medium",
                
                // Nutrient analysis
                PrimaryDeficiency = "Nitrogen",
                SecondaryDeficiencies = "[\"Potassium\"]",
                NutrientStatus = "{\"nitrogen\": \"Low\", \"phosphorus\": \"Adequate\", \"potassium\": \"Low\"}",
                
                // Environmental data
                WeatherConditions = "Sunny",
                Temperature = 25.5m,
                Humidity = 65.0m,
                SoilType = "Clay loam",
                LastFertilization = DateTime.Now.AddDays(-15),
                LastIrrigation = DateTime.Now.AddDays(-2),
                PreviousTreatments = "[\"Organic fertilizer application\"]",
                
                // Metadata
                ConfidenceScore = 92.5m,
                ProcessingTimeMs = 2500,
                UrgencyLevel = "Normal",
                Notes = "Regular health checkup",
                ContactPhone = "+90 555 123 4567",
                ContactEmail = "farmer@example.com",
                AdditionalInfo = "{\"farmerExperience\": \"5 years\", \"organicFarming\": true}",
                
                // N8N Response tracking
                N8nRequestId = "n8n_req_123",
                N8nResponseData = "{\"status\": \"success\", \"processing_time\": 2500}",
                ImageMetadata = "{\"size_bytes\": 256000, \"format\": \"JPEG\", \"url\": \"https://example.com/image.jpg\"}"
            };
        }

        public static List<PlantAnalysis> GetPlantAnalysisList()
        {
            return new List<PlantAnalysis>
            {
                GetPlantAnalysis("analysis_test_001"),
                GetPlantAnalysis("analysis_test_002") with 
                { 
                    Id = 2, 
                    CropType = "pepper", 
                    AnalysisStatus = "Processing", 
                    OverallHealthScore = null,
                    CreatedDate = DateTime.Now.AddHours(-2)
                },
                GetPlantAnalysis("analysis_test_003") with 
                { 
                    Id = 3, 
                    CropType = "cucumber", 
                    OverallHealthScore = 9,
                    SponsorId = "S001",
                    SponsorUserId = 10,
                    SponsorshipCodeId = 5,
                    CreatedDate = DateTime.Now.AddDays(-1)
                }
            };
        }

        public static PlantAnalysisRequestDto GetValidPlantAnalysisRequest()
        {
            return new PlantAnalysisRequestDto
            {
                Image = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wCEAAkGBw==", // Mock base64
                CropType = "tomato",
                Location = "Antalya, Turkey",
                GpsCoordinates = "36.8969,30.7133",
                Altitude = 50,
                FieldId = "Field-001",
                PlantingDate = DateTime.Now.AddDays(-60),
                ExpectedHarvestDate = DateTime.Now.AddDays(30),
                LastFertilization = DateTime.Now.AddDays(-15),
                LastIrrigation = DateTime.Now.AddDays(-2),
                PreviousTreatments = new List<string> { "Organic fertilizer application" },
                SoilType = "Clay loam",
                Temperature = 25.5m,
                Humidity = 65.0m,
                WeatherConditions = "Sunny",
                UrgencyLevel = "Normal",
                Notes = "Regular health checkup for plant monitoring",
                ContactInfo = "farmer@example.com",
                AdditionalInfo = new { farmerExperience = "5 years", organicFarming = true }
            };
        }

        public static PlantAnalysisResponseDto GetMockPlantAnalysisResponse()
        {
            return new PlantAnalysisResponseDto
            {
                AnalysisId = "analysis_20241201_123456",
                FarmerId = "F001",
                PlantSpecies = "Solanum lycopersicum",
                PlantVariety = "Cherry Tomato",
                PlantType = "Vegetable",
                OverallHealthScore = 8,
                HealthStatus = "Good",
                Diseases = new List<string> { "Minor leaf spot" },
                Pests = new List<string>(),
                ElementDeficiencies = new List<string> { "Nitrogen" },
                StressIndicators = new List<string> { "Mild water stress" },
                DiseaseSymptoms = new List<string> { "Small brown spots on lower leaves" },
                Recommendations = new List<string> 
                { 
                    "Apply nitrogen fertilizer",
                    "Monitor for pest development",
                    "Ensure adequate watering"
                },
                TreatmentPlan = "Apply balanced NPK fertilizer (10-10-10) at 2kg per hectare",
                PriorityLevel = "Medium",
                PrimaryDeficiency = "Nitrogen",
                SecondaryDeficiencies = new List<string> { "Potassium" },
                NutrientStatus = new Dictionary<string, string>
                {
                    {"nitrogen", "Low"},
                    {"phosphorus", "Adequate"},
                    {"potassium", "Low"},
                    {"calcium", "Adequate"},
                    {"magnesium", "Adequate"}
                },
                ConfidenceScore = 92.5m,
                ProcessingTimeMs = 2500,
                AnalysisDate = DateTime.Now,
                ImagePath = "https://api.example.com/uploads/analysis_image.jpg",
                ImageMetadata = new ImageMetadataDto
                {
                    Url = "https://example.com/processed_image.jpg",
                    SizeBytes = 256000,
                    Format = "JPEG"
                }
            };
        }

        public static PlantAnalysisListResponseDto GetMockListResponse()
        {
            var analyses = new List<PlantAnalysisListItemDto>
            {
                new PlantAnalysisListItemDto
                {
                    Id = 1,
                    ImagePath = "https://api.example.com/uploads/image1.jpg",
                    Status = "Completed",
                    StatusIcon = "✅",
                    CropType = "tomato",
                    FarmerId = "F001",
                    SponsorId = "S001",
                    OverallHealthScore = 8,
                    PrimaryConcern = "Minor nutrient deficiency",
                    FormattedDate = DateTime.Now.AddHours(-1).ToString("dd/MM/yyyy HH:mm"),
                    IsSponsored = true,
                    HasResults = true,
                    HealthScoreText = "8/10"
                },
                new PlantAnalysisListItemDto
                {
                    Id = 2,
                    ImagePath = "https://api.example.com/uploads/image2.jpg",
                    Status = "Processing",
                    StatusIcon = "⏳",
                    CropType = "pepper",
                    FarmerId = "F001",
                    SponsorId = null,
                    OverallHealthScore = null,
                    PrimaryConcern = "Analysis in progress",
                    FormattedDate = DateTime.Now.AddHours(-2).ToString("dd/MM/yyyy HH:mm"),
                    IsSponsored = false,
                    HasResults = false,
                    HealthScoreText = "N/A"
                }
            };

            return new PlantAnalysisListResponseDto
            {
                Analyses = analyses,
                TotalCount = 2,
                Page = 1,
                PageSize = 20,
                TotalPages = 1,
                HasNextPage = false,
                CompletedCount = 1,
                ProcessingCount = 1,
                FailedCount = 0,
                SponsoredCount = 1
            };
        }

        public static DetailedPlantAnalysisDto GetDetailedPlantAnalysisDto()
        {
            return new DetailedPlantAnalysisDto
            {
                Id = 1,
                AnalysisId = "analysis_20241201_123456",
                FarmerId = "F001",
                UserId = 1,
                ImagePath = "https://api.example.com/uploads/analysis_image.jpg",
                AnalysisDate = DateTime.Now,
                AnalysisStatus = "Completed",
                CropType = "tomato",
                Location = "Antalya, Turkey",
                Latitude = 36.8969m,
                Longitude = 30.7133m,
                Altitude = 50,
                FieldId = "Field-001",
                PlantingDate = DateTime.Now.AddDays(-60),
                ExpectedHarvestDate = DateTime.Now.AddDays(30),
                
                // Analysis results
                PlantSpecies = "Solanum lycopersicum",
                PlantVariety = "Cherry Tomato",
                PlantType = "Vegetable",
                OverallHealthScore = 8,
                HealthStatus = "Good",
                Diseases = new List<string> { "Minor leaf spot" },
                Pests = new List<string>(),
                ElementDeficiencies = new List<string> { "Nitrogen" },
                StressIndicators = new List<string> { "Mild water stress" },
                DiseaseSymptoms = new List<string> { "Small brown spots on lower leaves" },
                
                Recommendations = new List<string> 
                { 
                    "Apply nitrogen fertilizer",
                    "Monitor for pest development",
                    "Ensure adequate watering"
                },
                TreatmentPlan = "Apply balanced NPK fertilizer (10-10-10) at 2kg per hectare",
                PriorityLevel = "Medium",
                
                PrimaryDeficiency = "Nitrogen",
                SecondaryDeficiencies = new List<string> { "Potassium" },
                NutrientStatus = new Dictionary<string, string>
                {
                    {"nitrogen", "Low"},
                    {"phosphorus", "Adequate"},
                    {"potassium", "Low"}
                },
                
                ConfidenceScore = 92.5m,
                ProcessingTimeMs = 2500,
                
                // Environmental data
                WeatherConditions = "Sunny",
                Temperature = 25.5m,
                Humidity = 65.0m,
                SoilType = "Clay loam",
                
                // Sponsorship info
                SponsorId = "S001",
                SponsorUserId = 10,
                SponsorshipCodeId = 5,
                
                CreatedDate = DateTime.Now.AddHours(-1),
                UpdatedDate = DateTime.Now
            };
        }
    }
}