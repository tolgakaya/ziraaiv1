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
                HealthSeverity = "Good",
                Diseases = "[\"Minor leaf spot\"]",
                Pests = "[]",
                ElementDeficiencies = "[\"Nitrogen\"]",
                StressIndicators = "[\"Mild water stress\"]",
                DiseaseSymptoms = "[\"Small brown spots on lower leaves\"]",
                
                // Recommendations
                Recommendations = "{\"treatments\":[\"Apply nitrogen fertilizer\", \"Monitor for pest development\", \"Ensure adequate watering\"]}",
                PrimaryConcern = "Nitrogen deficiency",
                UrgencyLevel = "Medium",
                PrimaryDeficiency = "Nitrogen",
                NutrientStatus = "{\"nitrogen\":\"low\",\"phosphorus\":\"adequate\",\"potassium\":\"high\"}",
                
                // Processing metadata
                ConfidenceLevel = 92.5m,
                AiModel = "gpt-4-vision-preview",
                TotalTokens = 1250,
                TotalCostUsd = 0.05m,
                TotalCostTry = 1.5m,
                ImageSizeKb = 1024,
                
                // Technical data
                N8nWebhookResponse = "{\"analysis\":\"complete\",\"status\":\"success\"}",
                WeatherConditions = "Sunny, 25°C",
                Temperature = 25.5m,
                Humidity = 65.0m,
                SoilType = "Clay loam",
                
                // Sponsorship fields
                SponsorUserId = null,
                SponsorshipCodeId = null
            };
        }

        public static PlantAnalysis GetPlantAnalysisWithSponsorship()
        {
            var analysis = GetPlantAnalysis();
            analysis.SponsorUserId = 10;
            analysis.SponsorshipCodeId = 5;
            return analysis;
        }


        public static PlantAnalysisResponseDto GetPlantAnalysisResponseDto()
        {
            return new PlantAnalysisResponseDto
            {
                Id = 1,
                AnalysisId = "analysis_test_123",
                FarmerId = "F001",
                ImagePath = "uploads/plant-images/test_image.jpg",
                AnalysisDate = DateTime.Now,
                AnalysisStatus = "Completed",
                PlantType = "Vegetable"
            };
        }



        public static List<PlantAnalysis> GetPlantAnalysisList(int count = 3)
        {
            var list = new List<PlantAnalysis>();
            for (int i = 1; i <= count; i++)
            {
                var analysis = GetPlantAnalysis($"analysis_{i}");
                analysis.Id = i;
                list.Add(analysis);
            }
            return list;
        }

    }
}