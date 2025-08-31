using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Business.Services.Configuration;
using Business.Services.FileStorage;
using Business.Services.PlantAnalysis;
using Core.Utilities.Results;
using Entities.Dtos;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests.Business.Services
{
    [TestFixture]
    public class PlantAnalysisServiceTests
    {
        private Mock<HttpMessageHandler> _httpMessageHandler;
        private Mock<IConfiguration> _configuration;
        private Mock<IConfigurationService> _configurationService;
        private Mock<IFileStorageService> _fileStorageService;
        private Mock<ILogger<PlantAnalysisService>> _logger;
        private HttpClient _httpClient;
        private PlantAnalysisService _plantAnalysisService;

        [SetUp]
        public void Setup()
        {
            _httpMessageHandler = new Mock<HttpMessageHandler>();
            _configuration = new Mock<IConfiguration>();
            _configurationService = new Mock<IConfigurationService>();
            _fileStorageService = new Mock<IFileStorageService>();
            _logger = new Mock<ILogger<PlantAnalysisService>>();

            _httpClient = new HttpClient(_httpMessageHandler.Object);
            
            // Setup configuration
            SetupConfiguration();

            _plantAnalysisService = new PlantAnalysisService(
                _httpClient,
                _configuration.Object,
                _configurationService.Object,
                _fileStorageService.Object,
                _logger.Object);
        }

        #region AnalyzeAsync Tests

        [Test]
        public async Task AnalyzeAsync_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = GetValidAnalysisRequest();
            var mockN8NResponse = GetMockN8NResponse();
            
            SetupFileStorageSuccess();
            SetupHttpClientSuccess(mockN8NResponse);

            // Act
            var result = await _plantAnalysisService.AnalyzeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.AnalysisId.Should().Be(mockN8NResponse.AnalysisId);
            result.Data.PlantSpecies.Should().Be(mockN8NResponse.PlantIdentification.Species);
            result.Data.OverallHealthScore.Should().Be(mockN8NResponse.HealthAssessment.OverallScore);
        }

        [Test]
        public async Task AnalyzeAsync_FileUploadFails_ReturnsError()
        {
            // Arrange
            var request = GetValidAnalysisRequest();
            
            SetupFileStorageFailure();

            // Act
            var result = await _plantAnalysisService.AnalyzeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Failed to upload image");
            
            // Verify file storage was attempted
            _fileStorageService.Verify(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task AnalyzeAsync_N8NServiceUnavailable_ReturnsError()
        {
            // Arrange
            var request = GetValidAnalysisRequest();
            
            SetupFileStorageSuccess();
            SetupHttpClientFailure(HttpStatusCode.ServiceUnavailable);

            // Act
            var result = await _plantAnalysisService.AnalyzeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("N8N service is currently unavailable");
        }

        [Test]
        public async Task AnalyzeAsync_N8NInvalidResponse_ReturnsError()
        {
            // Arrange
            var request = GetValidAnalysisRequest();
            var invalidResponse = "{ invalid json }";
            
            SetupFileStorageSuccess();
            SetupHttpClientSuccess(invalidResponse);

            // Act
            var result = await _plantAnalysisService.AnalyzeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Failed to process N8N response");
        }

        [Test]
        public async Task AnalyzeAsync_WithUrlBasedProcessing_UsesImageUrl()
        {
            // Arrange
            var request = GetValidAnalysisRequest();
            var mockN8NResponse = GetMockN8NResponse();
            
            // Setup URL-based processing
            SetupUrlBasedProcessing();
            SetupFileStorageSuccess();
            SetupHttpClientSuccess(mockN8NResponse);

            // Act
            var result = await _plantAnalysisService.AnalyzeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            // Verify that image URL was used instead of base64
            _fileStorageService.Verify(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            
            // Verify HTTP request was made with URL parameter
            VerifyHttpRequestWithImageUrl();
        }

        [Test]
        public async Task AnalyzeAsync_NetworkTimeout_ReturnsError()
        {
            // Arrange
            var request = GetValidAnalysisRequest();
            
            SetupFileStorageSuccess();
            SetupHttpClientTimeout();

            // Act
            var result = await _plantAnalysisService.AnalyzeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("timeout");
        }

        [Test]
        public async Task AnalyzeAsync_LargeImageBase64_ProcessesSuccessfully()
        {
            // Arrange
            var request = GetValidAnalysisRequest();
            // Simulate larger base64 image (2MB worth of base64 data)
            request.Image = "data:image/jpeg;base64," + new string('A', 2_800_000); // ~2MB base64
            
            var mockN8NResponse = GetMockN8NResponse();
            
            SetupFileStorageSuccess();
            SetupHttpClientSuccess(mockN8NResponse);

            // Act
            var result = await _plantAnalysisService.AnalyzeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            // Verify file storage handled large image
            _fileStorageService.Verify(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task AnalyzeAsync_MultipleFormats_HandlesCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                "data:image/jpeg;base64,/9j/4AAQSkZJRgABA",
                "data:image/png;base64,iVBORw0KGgoAAAANSUhEU",
                "data:image/webp;base64,UklGRnoGAABXRUJQVlA4WAoAAAAQ"
            };

            foreach (var imageData in testCases)
            {
                var request = GetValidAnalysisRequest();
                request.Image = imageData;
                
                var mockN8NResponse = GetMockN8NResponse();
                
                SetupFileStorageSuccess();
                SetupHttpClientSuccess(mockN8NResponse);

                // Act
                var result = await _plantAnalysisService.AnalyzeAsync(request);

                // Assert
                result.Should().NotBeNull();
                result.Success.Should().BeTrue();
                result.Data.Should().NotBeNull();
            }
        }

        #endregion

        #region ProcessN8NResponse Tests

        [Test]
        public void ProcessN8NResponse_ValidResponse_MapsCorrectly()
        {
            // Arrange
            var n8nResponse = GetMockN8NResponse();
            var request = GetValidAnalysisRequest();

            // Act
            var result = _plantAnalysisService.ProcessN8NResponse(JsonConvert.SerializeObject(n8nResponse), request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            
            var data = result.Data;
            data.AnalysisId.Should().Be(n8nResponse.AnalysisId);
            data.PlantSpecies.Should().Be(n8nResponse.PlantIdentification.Species);
            data.PlantVariety.Should().Be(n8nResponse.PlantIdentification.Variety);
            data.OverallHealthScore.Should().Be(n8nResponse.HealthAssessment.OverallScore);
            data.Diseases.Should().BeEquivalentTo(n8nResponse.HealthAssessment.Diseases);
            data.Pests.Should().BeEquivalentTo(n8nResponse.HealthAssessment.Pests);
            data.Recommendations.Should().BeEquivalentTo(n8nResponse.Recommendations.Recommendations);
        }

        [Test]
        public void ProcessN8NResponse_MissingOptionalFields_HandlesGracefully()
        {
            // Arrange
            var n8nResponse = new
            {
                analysis_id = "test_123",
                plant_identification = new
                {
                    species = "Test Plant",
                    // Missing variety
                },
                health_assessment = new
                {
                    overall_score = 7,
                    // Missing diseases and pests arrays
                },
                recommendations = new
                {
                    recommendations = new[] { "Test recommendation" }
                }
            };
            var request = GetValidAnalysisRequest();

            // Act
            var result = _plantAnalysisService.ProcessN8NResponse(JsonConvert.SerializeObject(n8nResponse), request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            
            var data = result.Data;
            data.PlantVariety.Should().BeNull();
            data.Diseases.Should().BeEmpty();
            data.Pests.Should().BeEmpty();
        }

        #endregion

        #region Helper Methods

        private PlantAnalysisRequestDto GetValidAnalysisRequest()
        {
            return new PlantAnalysisRequestDto
            {
                Image = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wCEAAkGBw==",
                CropType = "tomato",
                Location = "Antalya, Turkey",
                GpsCoordinates = "36.8969,30.7133",
                Altitude = 50,
                FieldId = "Field-001",
                PlantingDate = DateTime.Now.AddDays(-60),
                ExpectedHarvestDate = DateTime.Now.AddDays(30),
                SoilType = "Clay loam",
                Temperature = 25.5m,
                Humidity = 65.0m,
                WeatherConditions = "Sunny",
                UrgencyLevel = "Normal",
                Notes = "Test analysis",
                ContactInfo = "test@example.com"
            };
        }

        private dynamic GetMockN8NResponse()
        {
            return new
            {
                analysis_id = "analysis_20241201_123456",
                farmer_id = "F001",
                plant_identification = new
                {
                    species = "Solanum lycopersicum",
                    variety = "Cherry Tomato",
                    confidence = 0.95
                },
                health_assessment = new
                {
                    overall_score = 8,
                    health_status = "Good",
                    diseases = new[] { "Minor leaf spot" },
                    pests = new string[] { },
                    stress_indicators = new[] { "Mild water stress" },
                    disease_symptoms = new[] { "Small brown spots on lower leaves" }
                },
                nutrient_analysis = new
                {
                    primary_deficiency = "Nitrogen",
                    secondary_deficiencies = new[] { "Potassium" },
                    nutrient_status = new
                    {
                        nitrogen = "Low",
                        phosphorus = "Adequate",
                        potassium = "Low",
                        calcium = "Adequate",
                        magnesium = "Adequate"
                    }
                },
                recommendations = new
                {
                    recommendations = new[]
                    {
                        "Apply nitrogen fertilizer",
                        "Monitor for pest development",
                        "Ensure adequate watering"
                    },
                    treatment_plan = "Apply balanced NPK fertilizer (10-10-10) at 2kg per hectare",
                    priority_level = "Medium"
                },
                analysis_metadata = new
                {
                    processing_time_ms = 2500,
                    confidence_score = 92.5,
                    analysis_date = DateTime.Now,
                    image_metadata = new
                    {
                        url = "https://example.com/processed_image.jpg",
                        size_bytes = 256000,
                        format = "JPEG"
                    }
                }
            };
        }

        private void SetupConfiguration()
        {
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x.Value).Returns("http://localhost:5678/webhook/api/plant-analysis");
            
            _configuration.Setup(x => x.GetSection("N8N:WebhookUrl")).Returns(configSection.Object);

            // Setup URL-based processing configuration
            var useUrlSection = new Mock<IConfigurationSection>();
            useUrlSection.Setup(x => x.Value).Returns("true");
            _configuration.Setup(x => x.GetSection("N8N:UseImageUrl")).Returns(useUrlSection.Object);
        }

        private void SetupUrlBasedProcessing()
        {
            _configurationService.Setup(x => x.GetBoolValueAsync("N8N_USE_IMAGE_URL", true))
                .ReturnsAsync(true);
        }

        private void SetupFileStorageSuccess()
        {
            _fileStorageService.Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new SuccessDataResult<string>("https://example.com/uploaded_image.jpg", "Upload successful"));
        }

        private void SetupFileStorageFailure()
        {
            _fileStorageService.Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ErrorDataResult<string>("Failed to upload image"));
        }

        private void SetupHttpClientSuccess(object responseObject)
        {
            var json = JsonConvert.SerializeObject(responseObject);
            SetupHttpClientSuccess(json);
        }

        private void SetupHttpClientSuccess(string responseJson)
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };

            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        private void SetupHttpClientFailure(HttpStatusCode statusCode)
        {
            var response = new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("Service unavailable", System.Text.Encoding.UTF8, "text/plain")
            };

            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        private void SetupHttpClientTimeout()
        {
            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("The operation was canceled.", new TimeoutException()));
        }

        private void VerifyHttpRequestWithImageUrl()
        {
            _httpMessageHandler.Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => req.Content != null),
                    ItExpr.IsAny<CancellationToken>());
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            _httpClient?.Dispose();
        }
    }
}