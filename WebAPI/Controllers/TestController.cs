using Business.Services.MessageQueue;
using Business.Services.Configuration;
using Core.Configuration;
using Entities.Constants;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : BaseApiController
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IConfigurationService _configurationService;
        private readonly RabbitMQOptions _rabbitMQOptions;

        public TestController(
            IMessageQueueService messageQueueService, 
            IConfigurationService configurationService,
            IOptions<RabbitMQOptions> rabbitMQOptions)
        {
            _messageQueueService = messageQueueService;
            _configurationService = configurationService;
            _rabbitMQOptions = rabbitMQOptions.Value;
        }

        /// <summary>
        /// Mock N8N response for testing async flow
        /// </summary>
        [HttpPost("mock-n8n-response")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> MockN8NResponse([FromBody] MockN8NRequestDto request)
        {
            try
            {
                // Create mock analysis response
                var mockResponse = new PlantAnalysisAsyncResponseDto
                {
                    AnalysisId = request.AnalysisId ?? $"test_analysis_{DateTime.Now:yyyyMMdd_HHmmss}",
                    Timestamp = DateTime.UtcNow,
                    FarmerId = request.FarmerId ?? "farmer_test_001",
                    SponsorId = request.SponsorId ?? "sponsor_test_xyz",
                    Location = request.Location ?? "Antalya, Turkey",
                    GpsCoordinates = request.GpsCoordinates ?? new GpsCoordinates { Lat = 36.8969m, Lng = 30.7133m },
                    CropType = request.CropType ?? "tomato",
                    FieldId = request.FieldId ?? "field_test_01",
                    UrgencyLevel = request.UrgencyLevel ?? "high",
                    Notes = request.Notes ?? "Test mesajı - mock analysis sonucu",

                    // Mock analysis results
                    PlantIdentification = new PlantIdentification
                    {
                        Species = "Solanum lycopersicum",
                        Variety = "unknown",
                        GrowthStage = "vegetative",
                        Confidence = 95,
                        IdentifyingFeatures = new[] { "compound leaves with serrated edges", "green stems", "typical tomato leaf shape" },
                        VisibleParts = new[] { "leaves", "stems" }
                    },

                    HealthAssessment = new HealthAssessment
                    {
                        VigorScore = 4,
                        LeafColor = "yellowing on leaves, especially older leaves",
                        LeafTexture = "some leaves appear dry and brittle",
                        GrowthPattern = "abnormal - uneven leaf coloration and some leaf curling",
                        StructuralIntegrity = "moderate - stems appear intact but leaves show stress",
                        StressIndicators = new[] { "leaf yellowing", "leaf curling", "possible chlorosis" },
                        DiseaseSymptoms = new[] { "no clear lesions or spots visible", "no wilting or necrosis observed" },
                        Severity = "high"
                    },

                    NutrientStatus = new NutrientStatus
                    {
                        Nitrogen = "deficient",
                        Phosphorus = "normal",
                        Potassium = "normal",
                        Calcium = "normal",
                        Magnesium = "normal",
                        Iron = "normal",
                        PrimaryDeficiency = "nitrogen",
                        SecondaryDeficiencies = new string[] { },
                        Severity = "medium"
                    },

                    PestDisease = new PestDisease
                    {
                        PestsDetected = new string[] { },
                        DiseasesDetected = new string[] { },
                        DamagePattern = "no visible pest or disease damage",
                        AffectedAreaPercentage = 0,
                        SpreadRisk = "none",
                        PrimaryIssue = "none"
                    },

                    EnvironmentalStress = new EnvironmentalStress
                    {
                        WaterStatus = "optimal",
                        TemperatureStress = "none",
                        LightStress = "none",
                        PhysicalDamage = "none",
                        ChemicalDamage = "none",
                        SoilIndicators = "not visible",
                        PrimaryStressor = "nutrient deficiency"
                    },

                    CrossFactorInsights = new List<CrossFactorInsight>
                    {
                        new CrossFactorInsight
                        {
                            Insight = "Nitrogen deficiency is causing leaf yellowing and reduced vigor, which may predispose the plant to further stress if not corrected.",
                            Confidence = 0.9m,
                            AffectedAspects = new[] { "health_assessment", "nutrient_status", "environmental_stress" },
                            ImpactLevel = "high"
                        }
                    },

                    Recommendations = new Recommendations
                    {
                        Immediate = new[]
                        {
                            new Recommendation
                            {
                                Action = "Apply nitrogen-rich fertilizer",
                                Details = "Use a balanced nitrogen fertilizer suitable for tomatoes to correct deficiency",
                                Timeline = "within 24 hours",
                                Priority = "critical"
                            },
                            new Recommendation
                            {
                                Action = "Inspect irrigation practices",
                                Details = "Ensure adequate and uniform watering to support nutrient uptake",
                                Timeline = "within 24 hours",
                                Priority = "high"
                            }
                        },
                        ShortTerm = new[]
                        {
                            new Recommendation
                            {
                                Action = "Monitor leaf color and growth",
                                Details = "Observe for improvement in leaf greenness and plant vigor over 2-7 days",
                                Timeline = "2-7 days",
                                Priority = "high"
                            }
                        },
                        Preventive = new[]
                        {
                            new Recommendation
                            {
                                Action = "Implement regular soil fertility management",
                                Details = "Maintain balanced fertilization schedule based on soil tests",
                                Timeline = "ongoing",
                                Priority = "medium"
                            }
                        },
                        Monitoring = new[]
                        {
                            new MonitoringParameter
                            {
                                Parameter = "Leaf color and vigor",
                                Frequency = "weekly",
                                Threshold = "Persistent yellowing or decline in vigor"
                            }
                        }
                    },

                    Summary = new AnalysisSummary
                    {
                        OverallHealthScore = 4,
                        PrimaryConcern = "Nitrogen deficiency causing leaf yellowing and reduced vigor",
                        SecondaryConcerns = new[] { "Potential for increased stress if deficiency not corrected" },
                        CriticalIssuesCount = 1,
                        ConfidenceLevel = 90,
                        Prognosis = "fair",
                        EstimatedYieldImpact = "moderate"
                    },

                    ImageMetadata = new ImageMetadata
                    {
                        Format = "image/jpeg",
                        SizeBytes = 163772.25m,
                        SizeKb = 159.93m,
                        SizeMb = 0.16m,
                        Base64Length = 218363,
                        UploadTimestamp = DateTime.UtcNow
                    },

                    RabbitMQMetadata = new RabbitMQMetadata
                    {
                        CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString("N")[..12],
                        ResponseQueue = "plant-analysis-results",
                        CallbackUrl = null,
                        Priority = "normal",
                        RetryCount = 0,
                        ReceivedAt = DateTime.UtcNow,
                        MessageId = null,
                        RoutingKey = null
                    },

                    ProcessingMetadata = new ProcessingMetadata
                    {
                        ParseSuccess = true,
                        ProcessingTimestamp = DateTime.UtcNow,
                        AiModel = "gpt-4o-mini",
                        WorkflowVersion = "2.0-rabbitmq",
                        ReceivedAt = DateTime.UtcNow,
                        ProcessingTimeMs = 30900,
                        RetryCount = 0,
                        Priority = "normal"
                    },

                    Success = true,
                    Message = "Plant analysis completed successfully",
                    Error = false,
                    ErrorMessage = null,
                    ErrorType = null
                };

                // Get result queue name from appsettings
                var resultQueueName = _rabbitMQOptions.Queues.PlantAnalysisResult;

                // Publish mock response to result queue
                var published = await _messageQueueService.PublishAsync(
                    resultQueueName, 
                    mockResponse, 
                    mockResponse.RabbitMQMetadata.CorrelationId);

                if (published)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Mock N8N response published to queue successfully",
                        analysis_id = mockResponse.AnalysisId,
                        queue_name = resultQueueName,
                        correlation_id = mockResponse.RabbitMQMetadata.CorrelationId
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to publish mock response to queue"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Error creating mock N8N response: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Check RabbitMQ health
        /// </summary>
        [HttpGet("rabbitmq-health")]
        public async Task<IActionResult> CheckRabbitMQHealth()
        {
            try
            {
                var isHealthy = await _messageQueueService.IsConnectedAsync();
                
                return Ok(new
                {
                    rabbitmq_healthy = isHealthy,
                    timestamp = DateTime.UtcNow,
                    message = isHealthy ? "RabbitMQ connection is healthy" : "RabbitMQ connection failed"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    rabbitmq_healthy = false,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }

    public class MockN8NRequestDto
    {
        public string AnalysisId { get; set; }
        public string FarmerId { get; set; }
        public string SponsorId { get; set; }
        public string Location { get; set; }
        public GpsCoordinates GpsCoordinates { get; set; }
        public string CropType { get; set; }
        public string FieldId { get; set; }
        public string UrgencyLevel { get; set; }
        public string Notes { get; set; }
        public string CorrelationId { get; set; }
    }
}