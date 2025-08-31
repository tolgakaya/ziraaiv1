using System;
using NUnit.Framework;
using FluentAssertions;
using Tests.Helpers;

namespace Tests.Business.Handlers
{
    [TestFixture]
    public class BasicHandlerTests
    {
        [Test]
        public void PlantAnalysisDataHelper_GetPlantAnalysis_ShouldReturnValidEntity()
        {
            // Arrange & Act
            var plantAnalysis = PlantAnalysisDataHelper.GetPlantAnalysis();

            // Assert
            plantAnalysis.Should().NotBeNull();
            plantAnalysis.Id.Should().Be(1);
            plantAnalysis.AnalysisId.Should().Be("analysis_test_123");
            plantAnalysis.FarmerId.Should().Be("F001");
            plantAnalysis.CropType.Should().Be("tomato");
        }

        [Test]
        public void SponsorshipDataHelper_GetSponsorProfile_ShouldReturnValidEntity()
        {
            // Arrange & Act
            var sponsorProfile = SponsorshipDataHelper.GetSponsorProfile();

            // Assert
            sponsorProfile.Should().NotBeNull();
            sponsorProfile.Id.Should().Be(1);
            sponsorProfile.SponsorId.Should().Be(10);
            sponsorProfile.CompanyName.Should().Be("AgroTech Solutions");
        }

        [Test]
        public void SubscriptionDataHelper_GetSubscriptionTier_ShouldReturnValidEntity()
        {
            // Arrange & Act
            var tier = SubscriptionDataHelper.GetSubscriptionTier("S");

            // Assert
            tier.Should().NotBeNull();
            tier.Id.Should().Be(1);
            tier.TierName.Should().Be("S");
            tier.DisplayName.Should().Be("S Plan");
        }

        [Test]
        [TestCase("S", 50)]
        [TestCase("M", 100)]
        [TestCase("L", 200)]
        [TestCase("XL", 500)]
        public void SubscriptionTier_ShouldHaveCorrectLimits(string tierName, int expectedDailyLimit)
        {
            // Arrange & Act
            var tier = SubscriptionDataHelper.GetSubscriptionTier(tierName);

            // Assert
            tier.DailyRequestLimit.Should().Be(expectedDailyLimit);
        }
    }
}