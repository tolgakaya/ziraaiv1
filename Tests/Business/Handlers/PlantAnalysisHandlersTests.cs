using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Business.Handlers.PlantAnalyses.Commands;
using Business.Handlers.PlantAnalyses.Queries;
using Business.Services.Configuration;
using Business.Services.FileStorage;
using Business.Services.ImageProcessing;
using Business.Services.PlantAnalysis;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.Business.Handlers
{
    [TestFixture]
    public class PlantAnalysisHandlersTests
    {
        private Mock<IPlantAnalysisRepository> _plantAnalysisRepository;
        private Mock<IPlantAnalysisService> _plantAnalysisService;
        private Mock<IImageProcessingService> _imageProcessingService;
        private Mock<IFileStorageService> _fileStorageService;
        private Mock<IConfigurationService> _configurationService;
        private Mock<IConfiguration> _configuration;
        private Mock<ILogger<CreatePlantAnalysisCommand.CreatePlantAnalysisCommandHandler>> _createLogger;
        private Mock<ILogger<GetPlantAnalysisQuery.GetPlantAnalysisQueryHandler>> _getLogger;
        private Mock<ILogger<GetPlantAnalysesQuery.GetPlantAnalysesQueryHandler>> _listLogger;
        private Mock<ILogger<GetPlantAnalysesForFarmerQuery.GetPlantAnalysesForFarmerQueryHandler>> _farmerLogger;

        private CreatePlantAnalysisCommand.CreatePlantAnalysisCommandHandler _createHandler;
        private GetPlantAnalysisQuery.GetPlantAnalysisQueryHandler _getHandler;
        private GetPlantAnalysesQuery.GetPlantAnalysesQueryHandler _listHandler;
        private GetPlantAnalysesForFarmerQuery.GetPlantAnalysesForFarmerQueryHandler _farmerHandler;

        [SetUp]
        public void Setup()
        {
            _plantAnalysisRepository = new Mock<IPlantAnalysisRepository>();
            _plantAnalysisService = new Mock<IPlantAnalysisService>();
            _imageProcessingService = new Mock<IImageProcessingService>();
            _fileStorageService = new Mock<IFileStorageService>();
            _configurationService = new Mock<IConfigurationService>();
            _configuration = new Mock<IConfiguration>();
            _createLogger = new Mock<ILogger<CreatePlantAnalysisCommand.CreatePlantAnalysisCommandHandler>>();
            _getLogger = new Mock<ILogger<GetPlantAnalysisQuery.GetPlantAnalysisQueryHandler>>();
            _listLogger = new Mock<ILogger<GetPlantAnalysesQuery.GetPlantAnalysesQueryHandler>>();
            _farmerLogger = new Mock<ILogger<GetPlantAnalysesForFarmerQuery.GetPlantAnalysesForFarmerQueryHandler>>();

            _createHandler = new CreatePlantAnalysisCommand.CreatePlantAnalysisCommandHandler(
                _plantAnalysisRepository.Object,
                _plantAnalysisService.Object,
                _imageProcessingService.Object,
                _fileStorageService.Object,
                _configurationService.Object,
                _configuration.Object,
                _createLogger.Object);

            _getHandler = new GetPlantAnalysisQuery.GetPlantAnalysisQueryHandler(
                _plantAnalysisRepository.Object,
                _getLogger.Object);

            _listHandler = new GetPlantAnalysesQuery.GetPlantAnalysesQueryHandler(
                _plantAnalysisRepository.Object,
                _listLogger.Object);

            _farmerHandler = new GetPlantAnalysesForFarmerQuery.GetPlantAnalysesForFarmerQueryHandler(
                _plantAnalysisRepository.Object,
                _farmerLogger.Object);
        }

        #region CreatePlantAnalysisCommand Tests

        [Test]
        public async Task CreatePlantAnalysis_Success()
        {
            // Arrange
            var command = GetValidCreateCommand();
            var processedImageResult = new DataResult<string>("processed-image-path.jpg", true, "Image processed successfully");
            var analysisResult = new DataResult<PlantAnalysisResponseDto>(GetMockAnalysisResponse(), true, "Analysis completed");

            _imageProcessingService.Setup(x => x.ProcessImageForAnalysisAsync(It.IsAny<string>()))
                .ReturnsAsync(processedImageResult);

            _plantAnalysisService.Setup(x => x.AnalyzeAsync(It.IsAny<PlantAnalysisRequestDto>()))
                .ReturnsAsync(analysisResult);

            _plantAnalysisRepository.Setup(x => x.AddAsync(It.IsAny<PlantAnalysis>()))
                .ReturnsAsync(GetMockPlantAnalysis());

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.AnalysisId.Should().NotBeNullOrEmpty();
            
            _imageProcessingService.Verify(x => x.ProcessImageForAnalysisAsync(It.IsAny<string>()), Times.Once);
            _plantAnalysisService.Verify(x => x.AnalyzeAsync(It.IsAny<PlantAnalysisRequestDto>()), Times.Once);
            _plantAnalysisRepository.Verify(x => x.AddAsync(It.IsAny<PlantAnalysis>()), Times.Once);
        }

        [Test]
        public async Task CreatePlantAnalysis_ImageProcessingFails_ReturnsError()
        {
            // Arrange
            var command = GetValidCreateCommand();
            var processedImageResult = new ErrorDataResult<string>("Image processing failed");

            _imageProcessingService.Setup(x => x.ProcessImageForAnalysisAsync(It.IsAny<string>()))
                .ReturnsAsync(processedImageResult);

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Image processing failed");
            
            _plantAnalysisService.Verify(x => x.AnalyzeAsync(It.IsAny<PlantAnalysisRequestDto>()), Times.Never);
            _plantAnalysisRepository.Verify(x => x.AddAsync(It.IsAny<PlantAnalysis>()), Times.Never);
        }

        [Test]
        public async Task CreatePlantAnalysis_AnalysisServiceFails_ReturnsError()
        {
            // Arrange
            var command = GetValidCreateCommand();
            var processedImageResult = new DataResult<string>("processed-image-path.jpg", true, "Image processed successfully");
            var analysisResult = new ErrorDataResult<PlantAnalysisResponseDto>("N8N service unavailable");

            _imageProcessingService.Setup(x => x.ProcessImageForAnalysisAsync(It.IsAny<string>()))
                .ReturnsAsync(processedImageResult);

            _plantAnalysisService.Setup(x => x.AnalyzeAsync(It.IsAny<PlantAnalysisRequestDto>()))
                .ReturnsAsync(analysisResult);

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("N8N service unavailable");
            
            _plantAnalysisRepository.Verify(x => x.AddAsync(It.IsAny<PlantAnalysis>()), Times.Never);
        }

        [Test]
        public async Task CreatePlantAnalysis_WithSponsorshipDetails_Success()
        {
            // Arrange
            var command = GetValidCreateCommand();
            command.SponsorId = "S001";
            command.SponsorUserId = 10;
            command.SponsorshipCodeId = 5;

            var processedImageResult = new DataResult<string>("processed-image-path.jpg", true, "Image processed successfully");
            var analysisResult = new DataResult<PlantAnalysisResponseDto>(GetMockAnalysisResponse(), true, "Analysis completed");

            _imageProcessingService.Setup(x => x.ProcessImageForAnalysisAsync(It.IsAny<string>()))
                .ReturnsAsync(processedImageResult);

            _plantAnalysisService.Setup(x => x.AnalyzeAsync(It.IsAny<PlantAnalysisRequestDto>()))
                .ReturnsAsync(analysisResult);

            _plantAnalysisRepository.Setup(x => x.AddAsync(It.IsAny<PlantAnalysis>()))
                .ReturnsAsync(GetMockPlantAnalysis());

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.SponsorId.Should().Be("S001");
            
            _plantAnalysisRepository.Verify(x => x.AddAsync(It.Is<PlantAnalysis>(p => 
                p.SponsorId == "S001" && 
                p.SponsorUserId == 10 && 
                p.SponsorshipCodeId == 5)), Times.Once);
        }

        #endregion

        #region GetPlantAnalysisQuery Tests

        [Test]
        public async Task GetPlantAnalysis_Success()
        {
            // Arrange
            var query = new GetPlantAnalysisQuery { Id = 1 };
            var mockAnalysis = GetMockPlantAnalysis();

            _plantAnalysisRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<PlantAnalysis, bool>>>()))
                .ReturnsAsync(mockAnalysis);

            // Act
            var result = await _getHandler.Handle(query, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(1);
            result.Data.CropType.Should().Be("tomato");
            result.Data.AnalysisStatus.Should().Be("Completed");
        }

        [Test]
        public async Task GetPlantAnalysis_NotFound_ReturnsError()
        {
            // Arrange
            var query = new GetPlantAnalysisQuery { Id = 999 };

            _plantAnalysisRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<PlantAnalysis, bool>>>()))
                .ReturnsAsync((PlantAnalysis)null);

            // Act
            var result = await _getHandler.Handle(query, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Plant analysis not found");
        }

        #endregion

        #region GetPlantAnalysesQuery Tests

        [Test]
        public async Task GetPlantAnalyses_Success()
        {
            // Arrange
            var query = new GetPlantAnalysesQuery();
            var mockAnalyses = GetMockPlantAnalysisList();

            _plantAnalysisRepository.Setup(x => x.GetListAsync())
                .ReturnsAsync(mockAnalyses);

            // Act
            var result = await _listHandler.Handle(query, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Count.Should().Be(3);
            result.Data.First().CropType.Should().Be("tomato");
        }

        [Test]
        public async Task GetPlantAnalyses_EmptyList_ReturnsEmptyResult()
        {
            // Arrange
            var query = new GetPlantAnalysesQuery();
            var emptyList = new List<PlantAnalysis>();

            _plantAnalysisRepository.Setup(x => x.GetListAsync())
                .ReturnsAsync(emptyList);

            // Act
            var result = await _listHandler.Handle(query, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Count.Should().Be(0);
        }

        #endregion

        #region GetPlantAnalysesForFarmerQuery Tests

        [Test]
        public async Task GetPlantAnalysesForFarmer_Success()
        {
            // Arrange
            var query = new GetPlantAnalysesForFarmerQuery 
            { 
                UserId = 1,
                Page = 1,
                PageSize = 20
            };
            var mockAnalyses = GetMockPlantAnalysisList();
            var mockResponse = new PlantAnalysisListResponseDto
            {
                Analyses = mockAnalyses.Select(MapToListItemDto).ToList(),
                TotalCount = 3,
                Page = 1,
                TotalPages = 1,
                HasNextPage = false,
                CompletedCount = 2,
                SponsoredCount = 1
            };

            _plantAnalysisRepository.Setup(x => x.GetListByUserIdAsync(It.IsAny<int>()))
                .ReturnsAsync(mockAnalyses);

            // Act
            var result = await _farmerHandler.Handle(query, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.TotalCount.Should().Be(3);
            result.Data.CompletedCount.Should().BeGreaterThan(0);
            result.Data.Analyses.Should().NotBeEmpty();
            result.Data.Analyses.First().StatusIcon.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task GetPlantAnalysesForFarmer_WithFiltering_Success()
        {
            // Arrange
            var query = new GetPlantAnalysesForFarmerQuery 
            { 
                UserId = 1,
                Status = "Completed",
                CropType = "tomato",
                Page = 1,
                PageSize = 20
            };
            var mockAnalyses = GetMockPlantAnalysisList()
                .Where(a => a.AnalysisStatus == "Completed" && a.CropType == "tomato")
                .ToList();

            _plantAnalysisRepository.Setup(x => x.GetListByUserIdAsync(It.IsAny<int>()))
                .ReturnsAsync(mockAnalyses);

            // Act
            var result = await _farmerHandler.Handle(query, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Analyses.All(a => a.Status == "Completed").Should().BeTrue();
            result.Data.Analyses.All(a => a.CropType == "tomato").Should().BeTrue();
        }

        [Test]
        public async Task GetPlantAnalysesForFarmer_InvalidUserId_ReturnsError()
        {
            // Arrange
            var query = new GetPlantAnalysesForFarmerQuery { UserId = 0 };

            // Act
            var result = await _farmerHandler.Handle(query, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid user ID");
        }

        #endregion

        #region Helper Methods

        private CreatePlantAnalysisCommand GetValidCreateCommand()
        {
            return new CreatePlantAnalysisCommand
            {
                Image = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wCEAAkGBw==", // Mock base64
                UserId = 1,
                FarmerId = "F001",
                CropType = "tomato",
                Location = "Antalya, Turkey",
                GpsCoordinates = "36.8969,30.7133",
                Altitude = 50,
                PlantingDate = DateTime.Now.AddDays(-60),
                ExpectedHarvestDate = DateTime.Now.AddDays(30),
                SoilType = "Clay",
                Temperature = 25.5m,
                Humidity = 65.0m,
                WeatherConditions = "Sunny",
                UrgencyLevel = "Normal",
                Notes = "Regular growth checkup",
                ContactInfo = "farmer@example.com"
            };
        }

        private PlantAnalysisResponseDto GetMockAnalysisResponse()
        {
            return new PlantAnalysisResponseDto
            {
                AnalysisId = "analysis_20241201_123456",
                FarmerId = "F001",
                PlantSpecies = "Solanum lycopersicum",
                PlantVariety = "Cherry Tomato",
                OverallHealthScore = 8,
                PlantType = "Vegetable",
                HealthStatus = "Good",
                Diseases = new List<string> { "Minor leaf spot" },
                Pests = new List<string>(),
                ElementDeficiencies = new List<string> { "Nitrogen" },
                Recommendations = new List<string> 
                { 
                    "Apply nitrogen fertilizer",
                    "Monitor for pest development",
                    "Ensure adequate watering"
                },
                TreatmentPlan = "Apply balanced NPK fertilizer (10-10-10) at 2kg per hectare",
                ConfidenceScore = 92.5m,
                AnalysisDate = DateTime.Now,
                ImagePath = "https://api.example.com/uploads/analysis_image.jpg",
                ProcessingTimeMs = 2500
            };
        }

        private PlantAnalysis GetMockPlantAnalysis()
        {
            return new PlantAnalysis
            {
                Id = 1,
                AnalysisId = "analysis_20241201_123456",
                FarmerId = "F001",
                UserId = 1,
                ImagePath = "uploads/plant-images/analysis_20241201_123456.jpg",
                AnalysisDate = DateTime.Now,
                AnalysisStatus = "Completed",
                Status = true,
                CreatedDate = DateTime.Now,
                CropType = "tomato",
                Location = "Antalya, Turkey",
                PlantSpecies = "Solanum lycopersicum",
                PlantVariety = "Cherry Tomato",
                OverallHealthScore = 8,
                PlantType = "Vegetable",
                HealthStatus = "Good",
                Diseases = "[\"Minor leaf spot\"]",
                Pests = "[]",
                ElementDeficiencies = "[\"Nitrogen\"]",
                Recommendations = "[\"Apply nitrogen fertilizer\", \"Monitor for pest development\"]",
                TreatmentPlan = "Apply balanced NPK fertilizer",
                ConfidenceScore = 92.5m,
                ProcessingTimeMs = 2500
            };
        }

        private List<PlantAnalysis> GetMockPlantAnalysisList()
        {
            return new List<PlantAnalysis>
            {
                new PlantAnalysis
                {
                    Id = 1,
                    AnalysisId = "analysis_20241201_123456",
                    FarmerId = "F001",
                    UserId = 1,
                    CropType = "tomato",
                    AnalysisStatus = "Completed",
                    OverallHealthScore = 8,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Status = true,
                    SponsorId = "S001"
                },
                new PlantAnalysis
                {
                    Id = 2,
                    AnalysisId = "analysis_20241202_234567",
                    FarmerId = "F001",
                    UserId = 1,
                    CropType = "pepper",
                    AnalysisStatus = "Processing",
                    OverallHealthScore = null,
                    CreatedDate = DateTime.Now.AddHours(-2),
                    Status = true
                },
                new PlantAnalysis
                {
                    Id = 3,
                    AnalysisId = "analysis_20241203_345678",
                    FarmerId = "F001",
                    UserId = 1,
                    CropType = "cucumber",
                    AnalysisStatus = "Completed",
                    OverallHealthScore = 9,
                    CreatedDate = DateTime.Now.AddDays(-3),
                    Status = true
                }
            };
        }

        private PlantAnalysisListItemDto MapToListItemDto(PlantAnalysis analysis)
        {
            return new PlantAnalysisListItemDto
            {
                Id = analysis.Id,
                ImagePath = $"https://api.example.com/{analysis.ImagePath}",
                Status = analysis.AnalysisStatus,
                StatusIcon = analysis.AnalysisStatus switch
                {
                    "Completed" => "✅",
                    "Processing" => "⏳",
                    "Failed" => "❌",
                    _ => "⏳"
                },
                CropType = analysis.CropType,
                FarmerId = analysis.FarmerId,
                SponsorId = analysis.SponsorId,
                OverallHealthScore = analysis.OverallHealthScore,
                PrimaryConcern = analysis.HealthStatus ?? "Unknown",
                FormattedDate = analysis.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                IsSponsored = !string.IsNullOrEmpty(analysis.SponsorId),
                HasResults = analysis.AnalysisStatus == "Completed",
                HealthScoreText = analysis.OverallHealthScore?.ToString("0") + "/10" ?? "N/A"
            };
        }

        #endregion
    }
}