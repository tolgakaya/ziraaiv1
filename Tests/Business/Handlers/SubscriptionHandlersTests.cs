using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.Services.Subscription;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.Business.Handlers
{
    [TestFixture]
    public class SubscriptionHandlersTests
    {
        private Mock<ISubscriptionTierRepository> _subscriptionTierRepository;
        private Mock<IUserSubscriptionRepository> _userSubscriptionRepository;
        private Mock<ISubscriptionUsageLogRepository> _usageLogRepository;
        private Mock<IUserRepository> _userRepository;
        private Mock<ISubscriptionValidationService> _subscriptionValidationService;

        [SetUp]
        public void Setup()
        {
            _subscriptionTierRepository = new Mock<ISubscriptionTierRepository>();
            _userSubscriptionRepository = new Mock<IUserSubscriptionRepository>();
            _usageLogRepository = new Mock<ISubscriptionUsageLogRepository>();
            _userRepository = new Mock<IUserRepository>();
            _subscriptionValidationService = new Mock<ISubscriptionValidationService>();
        }

        #region Subscription Tier Management Tests

        [Test]
        public async Task GetActiveTiers_ReturnsAllActiveTiers()
        {
            // Arrange
            var tiers = SubscriptionDataHelper.GetSubscriptionTierList();
            _subscriptionTierRepository.Setup(x => x.GetActiveTiersAsync())
                .ReturnsAsync(tiers);

            // Act
            var result = await _subscriptionTierRepository.Object.GetActiveTiersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5);
            result.Should().Contain(t => t.TierName == "Trial");
            result.Should().Contain(t => t.TierName == "S");
            result.Should().Contain(t => t.TierName == "M");
            result.Should().Contain(t => t.TierName == "L");
            result.Should().Contain(t => t.TierName == "XL");
            result.Should().AllSatisfy(t => t.IsActive.Should().BeTrue());

            _subscriptionTierRepository.Verify(x => x.GetActiveTiersAsync(), Times.Once);
        }

        [Test]
        public async Task GetTierById_ExistingTier_ReturnsTier()
        {
            // Arrange
            var tier = SubscriptionDataHelper.GetSubscriptionTier(2, "S");
            _subscriptionTierRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SubscriptionTier, bool>>>()))
                .ReturnsAsync(tier);

            // Act
            var result = await _subscriptionTierRepository.Object.GetAsync(t => t.Id == 2);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(2);
            result.TierName.Should().Be("S");
            result.DisplayName.Should().Be("Small");
            result.DailyRequestLimit.Should().Be(5);
            result.MonthlyRequestLimit.Should().Be(50);
            result.MonthlyPrice.Should().Be(99.99m);
        }

        [Test]
        public async Task GetTierById_NonExistentTier_ReturnsNull()
        {
            // Arrange
            _subscriptionTierRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SubscriptionTier, bool>>>()))
                .ReturnsAsync((SubscriptionTier)null);

            // Act
            var result = await _subscriptionTierRepository.Object.GetAsync(t => t.Id == 999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region User Subscription Management Tests

        [Test]
        public async Task GetActiveSubscriptionByUserId_ExistingSubscription_ReturnsSubscription()
        {
            // Arrange
            var subscription = SubscriptionDataHelper.GetUserSubscription();
            _userSubscriptionRepository.Setup(x => x.GetActiveSubscriptionByUserIdAsync(1))
                .ReturnsAsync(subscription);

            // Act
            var result = await _userSubscriptionRepository.Object.GetActiveSubscriptionByUserIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(1);
            result.IsActive.Should().BeTrue();
            result.Status.Should().Be("Active");
            result.SubscriptionTier.Should().NotBeNull();
            result.SubscriptionTier.TierName.Should().Be("S");
        }

        [Test]
        public async Task GetActiveSubscriptionByUserId_NoActiveSubscription_ReturnsNull()
        {
            // Arrange
            _userSubscriptionRepository.Setup(x => x.GetActiveSubscriptionByUserIdAsync(999))
                .ReturnsAsync((UserSubscription)null);

            // Act
            var result = await _userSubscriptionRepository.Object.GetActiveSubscriptionByUserIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetUserSubscriptionHistory_ValidUser_ReturnsHistory()
        {
            // Arrange
            var subscriptions = SubscriptionDataHelper.GetUserSubscriptionList();
            _userSubscriptionRepository.Setup(x => x.GetUserSubscriptionHistoryAsync(1))
                .ReturnsAsync(subscriptions);

            // Act
            var result = await _userSubscriptionRepository.Object.GetUserSubscriptionHistoryAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4);
            result.Should().Contain(s => s.IsTrialSubscription == true);
            result.Should().Contain(s => s.Status == "Active");
            result.Should().Contain(s => s.Status == "Expired");
        }

        [Test]
        public async Task CreateSubscription_ValidData_CreatesSuccessfully()
        {
            // Arrange
            var subscription = SubscriptionDataHelper.GetUserSubscription();
            _userSubscriptionRepository.Setup(x => x.Add(It.IsAny<UserSubscription>()));
            _userSubscriptionRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            _userSubscriptionRepository.Object.Add(subscription);
            var saveResult = await _userSubscriptionRepository.Object.SaveChangesAsync();

            // Assert
            saveResult.Should().BeTrue();
            _userSubscriptionRepository.Verify(x => x.Add(It.Is<UserSubscription>(s => 
                s.UserId == subscription.UserId && 
                s.SubscriptionTierId == subscription.SubscriptionTierId)), Times.Once);
            _userSubscriptionRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task UpdateSubscription_ValidData_UpdatesSuccessfully()
        {
            // Arrange
            var subscription = SubscriptionDataHelper.GetUserSubscription();
            subscription.CurrentDailyUsage = 4;
            subscription.CurrentMonthlyUsage = 35;

            _userSubscriptionRepository.Setup(x => x.Update(It.IsAny<UserSubscription>()));
            _userSubscriptionRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            _userSubscriptionRepository.Object.Update(subscription);
            var saveResult = await _userSubscriptionRepository.Object.SaveChangesAsync();

            // Assert
            saveResult.Should().BeTrue();
            _userSubscriptionRepository.Verify(x => x.Update(It.Is<UserSubscription>(s => 
                s.CurrentDailyUsage == 4 && 
                s.CurrentMonthlyUsage == 35)), Times.Once);
            _userSubscriptionRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region Subscription Validation Service Tests

        [Test]
        public async Task CheckSubscriptionStatus_ActiveSubscription_ReturnsValidStatus()
        {
            // Arrange
            var subscription = SubscriptionDataHelper.GetUserSubscription();
            var statusDto = new SubscriptionStatusDto
            {
                HasActiveSubscription = true,
                TierName = "S",
                DailyUsage = 3,
                DailyLimit = 5,
                MonthlyUsage = 25,
                MonthlyLimit = 50,
                CanMakeRequest = true,
                NextDailyReset = DateTime.Now.Date.AddDays(1),
                NextMonthlyReset = DateTime.Now.Date.AddDays(DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month) - DateTime.Now.Day + 1)
            };

            _subscriptionValidationService.Setup(x => x.CheckSubscriptionStatusAsync(1))
                .ReturnsAsync(new SuccessDataResult<SubscriptionStatusDto>(statusDto));

            // Act
            var result = await _subscriptionValidationService.Object.CheckSubscriptionStatusAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasActiveSubscription.Should().BeTrue();
            result.Data.TierName.Should().Be("S");
            result.Data.CanMakeRequest.Should().BeTrue();
            result.Data.DailyUsage.Should().Be(3);
            result.Data.MonthlyUsage.Should().Be(25);
        }

        [Test]
        public async Task CheckSubscriptionStatus_NoActiveSubscription_ReturnsInactiveStatus()
        {
            // Arrange
            var statusDto = new SubscriptionStatusDto
            {
                HasActiveSubscription = false,
                CanMakeRequest = false,
                TierName = null,
                DailyUsage = 0,
                DailyLimit = 0,
                MonthlyUsage = 0,
                MonthlyLimit = 0
            };

            _subscriptionValidationService.Setup(x => x.CheckSubscriptionStatusAsync(999))
                .ReturnsAsync(new SuccessDataResult<SubscriptionStatusDto>(statusDto));

            // Act
            var result = await _subscriptionValidationService.Object.CheckSubscriptionStatusAsync(999);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasActiveSubscription.Should().BeFalse();
            result.Data.CanMakeRequest.Should().BeFalse();
        }

        [Test]
        public async Task ValidateAndLogUsage_WithinLimits_ReturnsSuccess()
        {
            // Arrange
            var validation = new SubscriptionValidationDto
            {
                CanProceed = true,
                HasActiveSubscription = true,
                TierName = "S",
                DailyUsageRemaining = 2,
                MonthlyUsageRemaining = 25,
                Message = "Request allowed"
            };

            _subscriptionValidationService.Setup(x => x.ValidateAndLogUsageAsync(1, "/api/plantanalyses/analyze", "POST", 123))
                .ReturnsAsync(new SuccessDataResult<SubscriptionValidationDto>(validation));

            // Act
            var result = await _subscriptionValidationService.Object.ValidateAndLogUsageAsync(1, "/api/plantanalyses/analyze", "POST", 123);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.CanProceed.Should().BeTrue();
            result.Data.HasActiveSubscription.Should().BeTrue();
            result.Data.TierName.Should().Be("S");
        }

        [Test]
        public async Task ValidateAndLogUsage_ExceedsLimits_ReturnsError()
        {
            // Arrange
            var validation = new SubscriptionValidationDto
            {
                CanProceed = false,
                HasActiveSubscription = true,
                TierName = "S",
                DailyUsageRemaining = 0,
                MonthlyUsageRemaining = 25,
                Message = "Daily request limit reached (5 requests). Resets at midnight."
            };

            _subscriptionValidationService.Setup(x => x.ValidateAndLogUsageAsync(1, "/api/plantanalyses/analyze", "POST", 123))
                .ReturnsAsync(new ErrorDataResult<SubscriptionValidationDto>(validation, "Daily limit exceeded"));

            // Act
            var result = await _subscriptionValidationService.Object.ValidateAndLogUsageAsync(1, "/api/plantanalyses/analyze", "POST", 123);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Data.Should().NotBeNull();
            result.Data.CanProceed.Should().BeFalse();
            result.Data.DailyUsageRemaining.Should().Be(0);
            result.Message.Should().Contain("Daily limit exceeded");
        }

        #endregion

        #region Usage Log Management Tests

        [Test]
        public async Task LogUsage_ValidData_CreatesLogEntry()
        {
            // Arrange
            var usageLog = SubscriptionDataHelper.GetUsageLog();
            _usageLogRepository.Setup(x => x.LogUsageAsync(It.IsAny<SubscriptionUsageLog>()))
                .Returns(Task.CompletedTask);

            // Act
            await _usageLogRepository.Object.LogUsageAsync(usageLog);

            // Assert
            _usageLogRepository.Verify(x => x.LogUsageAsync(It.Is<SubscriptionUsageLog>(log => 
                log.UserId == usageLog.UserId && 
                log.UsageType == "PlantAnalysis" &&
                log.RequestEndpoint == "/api/plantanalyses/analyze")), Times.Once);
        }

        [Test]
        public async Task GetUserUsageLogs_ValidDateRange_ReturnsLogs()
        {
            // Arrange
            var logs = SubscriptionDataHelper.GetUsageLogList().Where(l => l.UserId == 1).ToList();
            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;

            _usageLogRepository.Setup(x => x.GetUserUsageLogsAsync(1, startDate, endDate))
                .ReturnsAsync(logs);

            // Act
            var result = await _usageLogRepository.Object.GetUserUsageLogsAsync(1, startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(log => log.UserId.Should().Be(1));
            result.Should().Contain(log => log.IsSuccessful == true);
            result.Should().Contain(log => log.ResponseStatus == "200");
        }

        [Test]
        public async Task GetUsageLogs_AllUsers_ReturnsAllLogs()
        {
            // Arrange
            var allLogs = SubscriptionDataHelper.GetUsageLogList();
            _usageLogRepository.Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SubscriptionUsageLog, bool>>>()))
                .ReturnsAsync(allLogs);

            // Act
            var result = await _usageLogRepository.Object.GetListAsync(l => l.UsageDate >= DateTime.Now.AddDays(-30));

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().Contain(log => log.UserId == 1);
            result.Should().Contain(log => log.UserId == 2);
        }

        #endregion

        #region Trial Subscription Tests

        [Test]
        public async Task CreateTrialSubscription_ValidUser_CreatesTrialSuccessfully()
        {
            // Arrange
            var user = new User { Id = 1, Email = "test@farmer.com", FullName = "Test Farmer" };
            var trialTier = SubscriptionDataHelper.GetSubscriptionTier(1, "Trial");
            var trialSubscription = SubscriptionDataHelper.GetUserSubscription(1, 1);
            trialSubscription.IsTrialSubscription = true;
            trialSubscription.TrialEndDate = DateTime.Now.AddDays(30);
            trialSubscription.PaidAmount = 0m;

            _userRepository.Setup(x => x.GetAsync(1))
                .ReturnsAsync(user);
            _subscriptionTierRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SubscriptionTier, bool>>>()))
                .ReturnsAsync(trialTier);
            _userSubscriptionRepository.Setup(x => x.Add(It.IsAny<UserSubscription>()));
            _userSubscriptionRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            _userSubscriptionRepository.Object.Add(trialSubscription);
            var result = await _userSubscriptionRepository.Object.SaveChangesAsync();

            // Assert
            result.Should().BeTrue();
            _userSubscriptionRepository.Verify(x => x.Add(It.Is<UserSubscription>(s => 
                s.IsTrialSubscription == true && 
                s.PaidAmount == 0m &&
                s.TrialEndDate != null)), Times.Once);
        }

        [Test]
        public async Task CheckTrialExpiry_ExpiredTrial_ReturnsExpired()
        {
            // Arrange
            var expiredTrial = SubscriptionDataHelper.GetUserSubscription(1, 1);
            expiredTrial.IsTrialSubscription = true;
            expiredTrial.TrialEndDate = DateTime.Now.AddDays(-5); // Expired 5 days ago

            var statusDto = new SubscriptionStatusDto
            {
                HasActiveSubscription = false,
                CanMakeRequest = false,
                TierName = "Trial",
                Message = "Trial subscription has expired. Please upgrade to a paid plan."
            };

            _subscriptionValidationService.Setup(x => x.CheckSubscriptionStatusAsync(1))
                .ReturnsAsync(new ErrorDataResult<SubscriptionStatusDto>(statusDto, "Trial expired"));

            // Act
            var result = await _subscriptionValidationService.Object.CheckSubscriptionStatusAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Data.Should().NotBeNull();
            result.Data.HasActiveSubscription.Should().BeFalse();
            result.Data.CanMakeRequest.Should().BeFalse();
            result.Message.Should().Contain("Trial expired");
        }

        #endregion

        #region Subscription Cancellation Tests

        [Test]
        public async Task CancelSubscription_ImmediateCancel_UpdatesStatusCorrectly()
        {
            // Arrange
            var subscription = SubscriptionDataHelper.GetUserSubscription();
            subscription.IsActive = false;
            subscription.Status = "Cancelled";
            subscription.CancellationDate = DateTime.Now;
            subscription.CancellationReason = "Too expensive";

            _userSubscriptionRepository.Setup(x => x.Update(It.IsAny<UserSubscription>()));
            _userSubscriptionRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            _userSubscriptionRepository.Object.Update(subscription);
            var result = await _userSubscriptionRepository.Object.SaveChangesAsync();

            // Assert
            result.Should().BeTrue();
            _userSubscriptionRepository.Verify(x => x.Update(It.Is<UserSubscription>(s => 
                s.IsActive == false && 
                s.Status == "Cancelled" &&
                s.CancellationDate != null &&
                s.CancellationReason == "Too expensive")), Times.Once);
        }

        [Test]
        public async Task CancelSubscription_EndOfPeriodCancel_SetsPendingCancellation()
        {
            // Arrange
            var subscription = SubscriptionDataHelper.GetUserSubscription();
            subscription.AutoRenew = false;
            subscription.Status = "Pending Cancellation";
            subscription.CancellationReason = "Switching to different service";

            _userSubscriptionRepository.Setup(x => x.Update(It.IsAny<UserSubscription>()));
            _userSubscriptionRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            _userSubscriptionRepository.Object.Update(subscription);
            var result = await _userSubscriptionRepository.Object.SaveChangesAsync();

            // Assert
            result.Should().BeTrue();
            _userSubscriptionRepository.Verify(x => x.Update(It.Is<UserSubscription>(s => 
                s.AutoRenew == false && 
                s.Status == "Pending Cancellation" &&
                s.CancellationReason == "Switching to different service")), Times.Once);
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            // Clean up mocks if needed
        }
    }
}