using System;
using NUnit.Framework;
using FluentAssertions;
using Tests.Helpers;

namespace Tests.Business.Services
{
    [TestFixture]
    public class BasicServiceTests
    {
        [Test]
        public void PlantAnalysisHelper_CreateMultipleAnalyses_ShouldReturnCorrectCount()
        {
            // Arrange & Act
            var analysesList = PlantAnalysisDataHelper.GetPlantAnalysisList(5);

            // Assert
            analysesList.Should().NotBeNull();
            analysesList.Should().HaveCount(5);
            analysesList[0].Id.Should().Be(1);
            analysesList[4].Id.Should().Be(5);
        }

        [Test]
        public void SponsorshipHelper_CreateMultipleCodes_ShouldReturnCorrectCount()
        {
            // Arrange & Act
            var codesList = SponsorshipDataHelper.GetSponsorshipCodeList(3);

            // Assert
            codesList.Should().NotBeNull();
            codesList.Should().HaveCount(3);
            codesList[0].Id.Should().Be(1);
            codesList[2].Id.Should().Be(3);
        }

        [Test]
        public void SubscriptionHelper_GetUsageLog_ShouldReturnValidData()
        {
            // Arrange & Act
            var usageLog = SubscriptionDataHelper.GetSubscriptionUsageLog();

            // Assert
            usageLog.Should().NotBeNull();
            usageLog.UserId.Should().Be(1);
            usageLog.DailyQuotaUsed.Should().Be(3);
            usageLog.IsSuccessful.Should().BeTrue();
        }

        [Test]
        public void DateTime_Calculations_ShouldWorkCorrectly()
        {
            // Arrange
            var now = DateTime.Now;
            var pastDate = now.AddDays(-10);

            // Act
            var difference = (now - pastDate).Days;

            // Assert
            difference.Should().Be(10);
        }
    }
}