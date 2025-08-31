using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Business.Services.Subscription;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Tests.Helpers;
using WebAPI.Controllers;

namespace Tests.WebAPI
{
    [TestFixture]
    public class SubscriptionsControllerTests
    {
        private Mock<ISubscriptionValidationService> _subscriptionValidationService;
        private Mock<ISubscriptionTierRepository> _tierRepository;
        private Mock<IUserSubscriptionRepository> _userSubscriptionRepository;
        private Mock<ISubscriptionUsageLogRepository> _usageLogRepository;
        private SubscriptionsController _controller;

        [SetUp]
        public void Setup()
        {
            _subscriptionValidationService = new Mock<ISubscriptionValidationService>();
            _tierRepository = new Mock<ISubscriptionTierRepository>();
            _userSubscriptionRepository = new Mock<IUserSubscriptionRepository>();
            _usageLogRepository = new Mock<ISubscriptionUsageLogRepository>();

            _controller = new SubscriptionsController(
                _subscriptionValidationService.Object,
                _tierRepository.Object,
                _userSubscriptionRepository.Object,
                _usageLogRepository.Object);
                
            SetupControllerContext();
        }

        #region Get Tiers Tests

        [Test]
        public async Task GetTiers_ReturnsAllActiveTiers()
        {
            // Arrange
            var tiers = SubscriptionDataHelper.GetSubscriptionTierList();
            _tierRepository.Setup(x => x.GetActiveTiersAsync())
                .ReturnsAsync(tiers);

            // Act
            var result = await _controller.GetTiers();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = okResult?.Value as SuccessDataResult<List<SubscriptionTierDto>>;
            
            responseData.Should().NotBeNull();
            responseData!.Success.Should().BeTrue();
            responseData.Data.Should().HaveCount(5);
            responseData.Data.Should().Contain(t => t.TierName == "Trial");
            responseData.Data.Should().Contain(t => t.TierName == "S");
            responseData.Data.Should().Contain(t => t.TierName == "XL");

            _tierRepository.Verify(x => x.GetActiveTiersAsync(), Times.Once);
        }

        [Test]
        public async Task GetTiers_EmptyList_ReturnsEmptySuccess()
        {
            // Arrange
            _tierRepository.Setup(x => x.GetActiveTiersAsync())
                .ReturnsAsync(new List<SubscriptionTier>());

            // Act
            var result = await _controller.GetTiers();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = okResult?.Value as SuccessDataResult<List<SubscriptionTierDto>>;
            
            responseData.Should().NotBeNull();
            responseData!.Data.Should().BeEmpty();
        }

        #endregion

        #region Get My Subscription Tests

        [Test]
        public async Task GetMySubscription_HasActiveSubscription_ReturnsSubscription()
        {
            // Arrange
            var subscription = SubscriptionDataHelper.GetUserSubscription();
            _userSubscriptionRepository.Setup(x => x.GetActiveSubscriptionByUserIdAsync(1))
                .ReturnsAsync(subscription);

            // Act
            var result = await _controller.GetMySubscription();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = okResult?.Value as SuccessDataResult<UserSubscriptionDto>;
            
            responseData.Should().NotBeNull();
            responseData!.Success.Should().BeTrue();
            responseData.Data.Should().NotBeNull();
            responseData.Data.UserId.Should().Be(1);
            responseData.Data.IsActive.Should().BeTrue();
            responseData.Data.TierName.Should().Be("S");

            _userSubscriptionRepository.Verify(x => x.GetActiveSubscriptionByUserIdAsync(1), Times.Once);
        }

        [Test]
        public async Task GetMySubscription_NoActiveSubscription_ReturnsNotFound()
        {
            // Arrange
            _userSubscriptionRepository.Setup(x => x.GetActiveSubscriptionByUserIdAsync(1))
                .ReturnsAsync((UserSubscription)null);

            // Act
            var result = await _controller.GetMySubscription();

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            var errorResult = notFoundResult?.Value as ErrorResult;
            errorResult?.Message.Should().Contain("No active subscription found");
        }

        [Test]
        public async Task GetMySubscription_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerContext(userId: null);

            // Act
            var result = await _controller.GetMySubscription();

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        #endregion

        #region Get Usage Status Tests

        [Test]
        public async Task GetUsageStatus_ValidUser_ReturnsUsageStatus()
        {
            // Arrange
            var usageStatus = new SubscriptionStatusDto
            {
                HasActiveSubscription = true,
                TierName = "S",
                DailyUsage = 3,
                DailyLimit = 5,
                MonthlyUsage = 25,
                MonthlyLimit = 50,
                CanMakeRequest = true
            };

            _subscriptionValidationService.Setup(x => x.CheckSubscriptionStatusAsync(1))
                .ReturnsAsync(new SuccessDataResult<SubscriptionStatusDto>(usageStatus));

            // Act
            var result = await _controller.GetUsageStatus();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = okResult?.Value as SuccessDataResult<SubscriptionStatusDto>;
            
            responseData.Should().NotBeNull();
            responseData!.Data.Should().NotBeNull();
            responseData.Data.HasActiveSubscription.Should().BeTrue();
            responseData.Data.TierName.Should().Be("S");
            responseData.Data.CanMakeRequest.Should().BeTrue();

            _subscriptionValidationService.Verify(x => x.CheckSubscriptionStatusAsync(1), Times.Once);
        }

        [Test]
        public async Task GetUsageStatus_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerContext(userId: null);

            // Act
            var result = await _controller.GetUsageStatus();

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        #endregion

        #region Subscribe Tests

        [Test]
        public async Task Subscribe_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var subscriptionDto = SubscriptionDataHelper.GetValidCreateSubscriptionDto();
            var tier = SubscriptionDataHelper.GetSubscriptionTier(2, "S");

            _userSubscriptionRepository.Setup(x => x.GetActiveSubscriptionByUserIdAsync(1))
                .ReturnsAsync((UserSubscription)null);
            
            _tierRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SubscriptionTier, bool>>>()))
                .ReturnsAsync(tier);

            _userSubscriptionRepository.Setup(x => x.Add(It.IsAny<UserSubscription>()));
            _userSubscriptionRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Subscribe(subscriptionDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var successResult = okResult?.Value as SuccessResult;
            successResult?.Message.Should().Contain("Successfully subscribed to Small plan");

            _userSubscriptionRepository.Verify(x => x.Add(It.Is<UserSubscription>(s => 
                s.UserId == 1 && 
                s.SubscriptionTierId == subscriptionDto.SubscriptionTierId)), Times.Once);
            _userSubscriptionRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task Subscribe_ExistingActiveSubscription_ReturnsBadRequest()
        {
            // Arrange
            var subscriptionDto = SubscriptionDataHelper.GetValidCreateSubscriptionDto();
            var existingSubscription = SubscriptionDataHelper.GetUserSubscription();

            _userSubscriptionRepository.Setup(x => x.GetActiveSubscriptionByUserIdAsync(1))
                .ReturnsAsync(existingSubscription);

            // Act
            var result = await _controller.Subscribe(subscriptionDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var errorResult = badRequestResult?.Value as ErrorResult;
            errorResult?.Message.Should().Contain("You already have an active subscription");

            _userSubscriptionRepository.Verify(x => x.Add(It.IsAny<UserSubscription>()), Times.Never);
        }

        [Test]
        public async Task Subscribe_InvalidTier_ReturnsBadRequest()
        {
            // Arrange
            var subscriptionDto = SubscriptionDataHelper.GetValidCreateSubscriptionDto();
            subscriptionDto.SubscriptionTierId = 999; // Invalid tier

            _userSubscriptionRepository.Setup(x => x.GetActiveSubscriptionByUserIdAsync(1))
                .ReturnsAsync((UserSubscription)null);
            
            _tierRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SubscriptionTier, bool>>>()))
                .ReturnsAsync((SubscriptionTier)null);

            // Act
            var result = await _controller.Subscribe(subscriptionDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var errorResult = badRequestResult?.Value as ErrorResult;
            errorResult?.Message.Should().Contain("Invalid subscription tier");
        }

        [Test]
        public async Task Subscribe_TrialSubscription_CreatesTrialCorrectly()
        {
            // Arrange
            var trialDto = SubscriptionDataHelper.GetValidTrialSubscriptionDto();
            var trialTier = SubscriptionDataHelper.GetSubscriptionTier(1, "Trial");

            _userSubscriptionRepository.Setup(x => x.GetActiveSubscriptionByUserIdAsync(1))
                .ReturnsAsync((UserSubscription)null);
            
            _tierRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SubscriptionTier, bool>>>()))
                .ReturnsAsync(trialTier);

            // Act
            var result = await _controller.Subscribe(trialDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            _userSubscriptionRepository.Verify(x => x.Add(It.Is<UserSubscription>(s => 
                s.IsTrialSubscription == true && 
                s.PaidAmount == 0m &&
                s.TrialEndDate != null)), Times.Once);
        }

        #endregion

        #region Cancel Subscription Tests

        [Test]
        public async Task CancelSubscription_ImmediateCancellation_CancelsImmediately()
        {
            // Arrange
            var cancelDto = SubscriptionDataHelper.GetValidCancelSubscriptionDto();
            cancelDto.ImmediateCancellation = true;
            
            var subscription = SubscriptionDataHelper.GetUserSubscription();

            _userSubscriptionRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserSubscription, bool>>>()))
                .ReturnsAsync(subscription);

            _userSubscriptionRepository.Setup(x => x.Update(It.IsAny<UserSubscription>()));
            _userSubscriptionRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _controller.CancelSubscription(cancelDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var successResult = okResult?.Value as SuccessResult;
            successResult?.Message.Should().Contain("cancelled immediately");

            _userSubscriptionRepository.Verify(x => x.Update(It.Is<UserSubscription>(s => 
                s.IsActive == false && 
                s.Status == "Cancelled" &&
                s.CancellationDate != null)), Times.Once);
        }

        [Test]
        public async Task CancelSubscription_EndOfPeriodCancellation_SetsForEndOfPeriod()
        {
            // Arrange
            var cancelDto = SubscriptionDataHelper.GetValidCancelSubscriptionDto();
            cancelDto.ImmediateCancellation = false;
            
            var subscription = SubscriptionDataHelper.GetUserSubscription();

            _userSubscriptionRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserSubscription, bool>>>()))
                .ReturnsAsync(subscription);

            _userSubscriptionRepository.Setup(x => x.Update(It.IsAny<UserSubscription>()));
            _userSubscriptionRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _controller.CancelSubscription(cancelDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var successResult = okResult?.Value as SuccessResult;
            successResult?.Message.Should().Contain("end of the current period");

            _userSubscriptionRepository.Verify(x => x.Update(It.Is<UserSubscription>(s => 
                s.AutoRenew == false && 
                s.Status == "Pending Cancellation")), Times.Once);
        }

        [Test]
        public async Task CancelSubscription_SubscriptionNotFound_ReturnsNotFound()
        {
            // Arrange
            var cancelDto = SubscriptionDataHelper.GetValidCancelSubscriptionDto();
            
            _userSubscriptionRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserSubscription, bool>>>()))
                .ReturnsAsync((UserSubscription)null);

            // Act
            var result = await _controller.CancelSubscription(cancelDto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region Get Subscription History Tests

        [Test]
        public async Task GetSubscriptionHistory_ValidUser_ReturnsHistory()
        {
            // Arrange
            var subscriptions = SubscriptionDataHelper.GetUserSubscriptionList();
            
            _userSubscriptionRepository.Setup(x => x.GetUserSubscriptionHistoryAsync(1))
                .ReturnsAsync(subscriptions);

            // Act
            var result = await _controller.GetSubscriptionHistory();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = okResult?.Value as SuccessDataResult<List<UserSubscriptionDto>>;
            
            responseData.Should().NotBeNull();
            responseData!.Data.Should().HaveCount(4);
            responseData.Data.Should().Contain(s => s.IsTrialSubscription == true);
            responseData.Data.Should().Contain(s => s.Status == "Expired");

            _userSubscriptionRepository.Verify(x => x.GetUserSubscriptionHistoryAsync(1), Times.Once);
        }

        [Test]
        public async Task GetSubscriptionHistory_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerContext(userId: null);

            // Act
            var result = await _controller.GetSubscriptionHistory();

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        #endregion

        #region Get Usage Logs Tests (Admin Only)

        [Test]
        public async Task GetUsageLogs_AdminUser_ReturnsAllLogs()
        {
            // Arrange
            SetupControllerContext(role: "Admin");
            var logs = SubscriptionDataHelper.GetUsageLogList();
            
            _usageLogRepository.Setup(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SubscriptionUsageLog, bool>>>()))
                .ReturnsAsync(logs);

            // Act
            var result = await _controller.GetUsageLogs(null, null, null);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = okResult?.Value as SuccessDataResult<List<SubscriptionUsageLog>>;
            
            responseData.Should().NotBeNull();
            responseData!.Data.Should().HaveCount(3);

            _usageLogRepository.Verify(x => x.GetListAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SubscriptionUsageLog, bool>>>()), Times.Once);
        }

        [Test]
        public async Task GetUsageLogs_SpecificUser_ReturnsUserLogs()
        {
            // Arrange
            SetupControllerContext(role: "Admin");
            var userLogs = SubscriptionDataHelper.GetUsageLogList().Where(l => l.UserId == 1).ToList();
            
            _usageLogRepository.Setup(x => x.GetUserUsageLogsAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(userLogs);

            // Act
            var result = await _controller.GetUsageLogs(1, DateTime.Now.AddDays(-30), DateTime.Now);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = okResult?.Value as SuccessDataResult<List<SubscriptionUsageLog>>;
            
            responseData.Should().NotBeNull();
            responseData!.Data.Should().HaveCount(2);
            responseData.Data.Should().AllSatisfy(log => log.UserId.Should().Be(1));

            _usageLogRepository.Verify(x => x.GetUserUsageLogsAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        }

        #endregion

        #region Update Tier Tests (Admin Only)

        [Test]
        public async Task UpdateTier_AdminUser_UpdatesSuccessfully()
        {
            // Arrange
            SetupControllerContext(role: "Admin");
            var updateDto = SubscriptionDataHelper.GetValidUpdateTierDto();
            var existingTier = SubscriptionDataHelper.GetSubscriptionTier(2, "S");

            _tierRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SubscriptionTier, bool>>>()))
                .ReturnsAsync(existingTier);

            _tierRepository.Setup(x => x.Update(It.IsAny<SubscriptionTier>()));
            _tierRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateTier(2, updateDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var successResult = okResult?.Value as SuccessResult;
            successResult?.Message.Should().Contain("updated successfully");

            _tierRepository.Verify(x => x.Update(It.Is<SubscriptionTier>(t => 
                t.DisplayName == updateDto.DisplayName && 
                t.DailyRequestLimit == updateDto.DailyRequestLimit)), Times.Once);
        }

        [Test]
        public async Task UpdateTier_TierNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupControllerContext(role: "Admin");
            var updateDto = SubscriptionDataHelper.GetValidUpdateTierDto();

            _tierRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SubscriptionTier, bool>>>()))
                .ReturnsAsync((SubscriptionTier)null);

            // Act
            var result = await _controller.UpdateTier(999, updateDto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region Helper Methods

        private void SetupControllerContext(int? userId = 1, string role = "Farmer", string email = "test@farmer.com")
        {
            var claims = new List<Claim>();
            
            if (userId.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
                claims.Add(new Claim(ClaimTypes.Email, email));
                claims.Add(new Claim(ClaimTypes.Name, "Test User"));
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }
    }
}