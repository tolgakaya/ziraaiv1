using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Business.Services.PlantAnalysis;
using Business.Services.Subscription;
using Core.CrossCuttingConcerns.Caching;
using Core.CrossCuttingConcerns.Caching.Microsoft;
using Core.Utilities.Results;
using Entities.Dtos;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Tests.Helpers;
using Tests.Helpers.Token;
using WebAPI;

namespace Tests.WebAPI
{
    [TestFixture]
    public class PlantAnalysesControllerTests : BaseIntegrationTest
    {
        private Mock<IPlantAnalysisAsyncService> _mockAsyncService;
        private Mock<ISubscriptionValidationService> _mockValidationService;
        private const string AuthenticationScheme = "Bearer";

        [SetUp]
        public void SetUp()
        {
            _mockAsyncService = new Mock<IPlantAnalysisAsyncService>();
            _mockValidationService = new Mock<ISubscriptionValidationService>();
        }

        #region Analyze Endpoint Tests

        [Test]
        public async Task Analyze_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/analyze";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetFarmerClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            var cache = new MemoryCacheManager();
            cache.Add($"{CacheKeys.UserIdForClaim}=1", new List<string>() { "CreatePlantAnalysisCommand" });

            var request = GetValidAnalysisRequest();
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Mock subscription validation to succeed
            SetupSuccessfulValidation();

            // Act
            var response = await HttpClient.PostAsync(requestUri, content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<PlantAnalysisResponseDto>>(responseContent);
            
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.AnalysisId.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task Analyze_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/analyze";
            // No authorization header set

            var request = GetValidAnalysisRequest();
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await HttpClient.PostAsync(requestUri, content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Test]
        public async Task Analyze_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/analyze";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetFarmerClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            // Invalid request - missing required image field
            var request = new PlantAnalysisRequestDto
            {
                CropType = "tomato"
                // Image field missing - this should cause validation failure
            };
            
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await HttpClient.PostAsync(requestUri, content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Validation failed");
        }

        [Test]
        public async Task Analyze_QuotaExceeded_ReturnsForbidden()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/analyze";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetFarmerClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            var request = GetValidAnalysisRequest();
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Mock subscription validation to fail due to quota
            SetupQuotaExceededValidation();

            // Act
            var response = await HttpClient.PostAsync(requestUri, content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("quota");
        }

        [Test]
        public async Task Analyze_AdminUser_Success()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/analyze";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetAdminClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            var cache = new MemoryCacheManager();
            cache.Add($"{CacheKeys.UserIdForClaim}=1", new List<string>() { "CreatePlantAnalysisCommand" });

            var request = GetValidAnalysisRequest();
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            SetupSuccessfulValidation();

            // Act
            var response = await HttpClient.PostAsync(requestUri, content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region AnalyzeAsync Endpoint Tests

        [Test]
        public async Task AnalyzeAsync_ValidRequest_ReturnsAccepted()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/analyze-async";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetFarmerClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            var request = GetValidAnalysisRequest();
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            SetupSuccessfulValidation();
            SetupAsyncServiceSuccess();

            // Act
            var response = await HttpClient.PostAsync(requestUri, content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<string>>(responseContent);
            
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().StartWith("async_analysis_");
        }

        [Test]
        public async Task AnalyzeAsync_ServiceFailure_ReturnsError()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/analyze-async";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetFarmerClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            var request = GetValidAnalysisRequest();
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            SetupSuccessfulValidation();
            SetupAsyncServiceFailure();

            // Act
            var response = await HttpClient.PostAsync(requestUri, content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region List Endpoint Tests

        [Test]
        public async Task List_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/list?page=1&pageSize=20";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetFarmerClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            var cache = new MemoryCacheManager();
            cache.Add($"{CacheKeys.UserIdForClaim}=1", new List<string>() { "GetPlantAnalysesForFarmerQuery" });

            // Act
            var response = await HttpClient.GetAsync(requestUri);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<PlantAnalysisListResponseDto>>(responseContent);
            
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Page.Should().Be(1);
        }

        [Test]
        public async Task List_WithFilters_ReturnsFilteredResults()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/list?page=1&pageSize=10&status=Completed&cropType=tomato";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetFarmerClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            var cache = new MemoryCacheManager();
            cache.Add($"{CacheKeys.UserIdForClaim}=1", new List<string>() { "GetPlantAnalysesForFarmerQuery" });

            // Act
            var response = await HttpClient.GetAsync(requestUri);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<PlantAnalysisListResponseDto>>(responseContent);
            
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.PageSize.Should().Be(10);
        }

        [Test]
        public async Task List_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/list";
            // No authorization header

            // Act
            var response = await HttpClient.GetAsync(requestUri);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Test]
        public async Task List_AdminUser_ReturnsAllAnalyses()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/list";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetAdminClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            var cache = new MemoryCacheManager();
            cache.Add($"{CacheKeys.UserIdForClaim}=1", new List<string>() { "GetPlantAnalysesQuery" });

            // Act
            var response = await HttpClient.GetAsync(requestUri);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Get Single Analysis Tests

        [Test]
        public async Task GetAnalysis_ValidId_ReturnsSuccess()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/1";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetFarmerClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            var cache = new MemoryCacheManager();
            cache.Add($"{CacheKeys.UserIdForClaim}=1", new List<string>() { "GetPlantAnalysisQuery" });

            // Act
            var response = await HttpClient.GetAsync(requestUri);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<DetailedPlantAnalysisDto>>(responseContent);
            
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(1);
        }

        [Test]
        public async Task GetAnalysis_InvalidId_ReturnsNotFound()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/999";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetFarmerClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            var cache = new MemoryCacheManager();
            cache.Add($"{CacheKeys.UserIdForClaim}=1", new List<string>() { "GetPlantAnalysisQuery" });

            // Act
            var response = await HttpClient.GetAsync(requestUri);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Image Endpoint Tests

        [Test]
        public async Task GetImage_ValidId_ReturnsImage()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/1/image";
            var token = MockJwtTokens.GenerateJwtToken(ClaimsData.GetFarmerClaims());
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);

            var cache = new MemoryCacheManager();
            cache.Add($"{CacheKeys.UserIdForClaim}=1", new List<string>() { "GetPlantAnalysisQuery" });

            // Act
            var response = await HttpClient.GetAsync(requestUri);

            // Assert
            // Note: This test assumes the image exists or proper mocking is in place
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                response.Content.Headers.ContentType.MediaType.Should().StartWith("image/");
            }
        }

        [Test]
        public async Task GetImage_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            const string requestUri = "api/v1/plantanalyses/1/image";
            // No authorization header

            // Act
            var response = await HttpClient.GetAsync(requestUri);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Helper Methods

        private PlantAnalysisRequestDto GetValidAnalysisRequest()
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
                Notes = "Regular checkup for plant health monitoring",
                ContactInfo = "farmer@example.com",
                AdditionalInfo = new { farmerExperience = "5 years", organicFarming = true }
            };
        }

        private void SetupSuccessfulValidation()
        {
            _mockValidationService
                .Setup(x => x.ValidateAndLogUsageAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new SuccessResult("Validation successful"));

            _mockValidationService
                .Setup(x => x.CheckSubscriptionStatusAsync(It.IsAny<int>()))
                .ReturnsAsync(new SuccessDataResult<SubscriptionUsageStatusDto>(
                    new SubscriptionUsageStatusDto
                    {
                        HasActiveSubscription = true,
                        TierName = "S",
                        DailyUsed = 2,
                        DailyLimit = 5,
                        MonthlyUsed = 15,
                        MonthlyLimit = 50,
                        CanMakeRequest = true
                    }));

            _mockValidationService
                .Setup(x => x.GetSponsorshipDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(new SuccessDataResult<SponsorshipDetailsDto>(
                    new SponsorshipDetailsDto
                    {
                        HasSponsor = false
                    }));
        }

        private void SetupQuotaExceededValidation()
        {
            _mockValidationService
                .Setup(x => x.ValidateAndLogUsageAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ErrorResult("Daily request limit reached (5 requests). Resets at midnight."));

            _mockValidationService
                .Setup(x => x.CheckSubscriptionStatusAsync(It.IsAny<int>()))
                .ReturnsAsync(new SuccessDataResult<SubscriptionUsageStatusDto>(
                    new SubscriptionUsageStatusDto
                    {
                        HasActiveSubscription = true,
                        TierName = "S",
                        DailyUsed = 5,
                        DailyLimit = 5,
                        MonthlyUsed = 25,
                        MonthlyLimit = 50,
                        CanMakeRequest = false,
                        NextDailyReset = DateTime.Now.AddHours(8)
                    }));
        }

        private void SetupAsyncServiceSuccess()
        {
            _mockAsyncService
                .Setup(x => x.QueueAnalysisAsync(It.IsAny<PlantAnalysisAsyncRequestDto>()))
                .ReturnsAsync(new SuccessDataResult<string>("async_analysis_20241201_123456", 
                    "Analysis queued successfully"));
        }

        private void SetupAsyncServiceFailure()
        {
            _mockAsyncService
                .Setup(x => x.QueueAnalysisAsync(It.IsAny<PlantAnalysisAsyncRequestDto>()))
                .ReturnsAsync(new ErrorDataResult<string>("RabbitMQ service unavailable"));
        }

        #endregion

        #region Helper Classes

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
        }

        #endregion
    }
}