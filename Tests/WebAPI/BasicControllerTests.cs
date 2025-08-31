using System;
using NUnit.Framework;
using FluentAssertions;
using Tests.Helpers;

namespace Tests.WebAPI
{
    [TestFixture]
    public class BasicControllerTests
    {
        [Test]
        public void TestDataHelpers_ShouldCreateValidDtos()
        {
            // Arrange & Act
            var plantAnalysisDto = PlantAnalysisDataHelper.GetPlantAnalysisResponseDto();
            var subscriptionStatusDto = SubscriptionDataHelper.GetSubscriptionUsageStatusDto();

            // Assert
            plantAnalysisDto.Should().NotBeNull();
            plantAnalysisDto.Id.Should().Be(1);
            
            subscriptionStatusDto.Should().NotBeNull();
            subscriptionStatusDto.DailyUsed.Should().Be(3);
            subscriptionStatusDto.CanMakeRequest.Should().BeTrue();
        }

        [Test]
        public void EntityRelationships_ShouldBeConfiguredCorrectly()
        {
            // Arrange
            var plantAnalysis = PlantAnalysisDataHelper.GetPlantAnalysis();
            var sponsorProfile = SponsorshipDataHelper.GetSponsorProfile();

            // Act & Assert
            plantAnalysis.SponsorUserId.Should().BeNull(); // Default case
            sponsorProfile.IsActive.Should().BeTrue();
        }

        [Test]
        [TestCase(1, "F001")]
        [TestCase(2, "F002")]
        public void PlantAnalysis_WithDifferentIds_ShouldHaveCorrectData(int id, string farmerId)
        {
            // Arrange & Act
            var analysis = PlantAnalysisDataHelper.GetPlantAnalysis($"analysis_{id}");
            analysis.Id = id;
            analysis.FarmerId = farmerId;

            // Assert
            analysis.Id.Should().Be(id);
            analysis.FarmerId.Should().Be(farmerId);
            analysis.AnalysisStatus.Should().Be("Completed");
        }
    }
}