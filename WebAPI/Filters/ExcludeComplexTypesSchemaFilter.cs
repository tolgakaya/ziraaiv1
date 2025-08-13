using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace WebAPI.Filters
{
    public class ExcludeComplexTypesSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var typeName = context.Type.Name;
            
            // Skip complex nested DTOs that might cause issues
            if (IsComplexDto(typeName))
            {
                schema.Properties?.Clear();
                schema.Description = $"Complex DTO ({typeName}) - Details excluded for Swagger compatibility";
                schema.Type = "object";
                schema.AdditionalProperties = new OpenApiSchema { Type = "object" };
            }
            
            // Remove any properties that might cause circular references
            if (schema.Properties != null)
            {
                var problematicProps = schema.Properties
                    .Where(p => IsProblematicProperty(p.Key))
                    .Select(p => p.Key)
                    .ToList();
                    
                foreach (var prop in problematicProps)
                {
                    schema.Properties.Remove(prop);
                }
            }
        }
        
        private bool IsComplexDto(string typeName)
        {
            var complexTypes = new[]
            {
                "DetailedPlantAnalysisDto",
                "PlantIdentificationDto", 
                "HealthAssessmentDto",
                "NutrientStatusDto",
                "PestDiseaseDto",
                "EnvironmentalStressDto",
                "RecommendationsDto",
                "CrossFactorInsightDto",
                "SummaryDto",
                "ProcessingMetadataDto",
                "TokenUsageDto",
                "TokenSummaryDto",
                "TokenBreakdownDto",
                "CostBreakdownDto"
            };
            
            return complexTypes.Contains(typeName);
        }
        
        private bool IsProblematicProperty(string propertyName)
        {
            var problematicProps = new[]
            {
                "DetailedAnalysis",
                "PlantIdentification",
                "HealthAssessment", 
                "NutrientStatus",
                "PestDisease",
                "EnvironmentalStress",
                "CrossFactorInsights",
                "Recommendations",
                "Summary",
                "ProcessingMetadata",
                "TokenUsage"
            };
            
            return problematicProps.Contains(propertyName);
        }
    }
}