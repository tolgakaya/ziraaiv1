using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.Handlers.Sponsorship.Commands;
using Business.Handlers.Sponsorship.Queries;
using Business.Services.Sponsorship;
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
    public class SponsorshipHandlersTests
    {
        private Mock<ISponsorProfileRepository> _sponsorProfileRepository;
        private Mock<ISponsorshipPurchaseRepository> _sponsorshipPurchaseRepository;
        private Mock<ISponsorshipCodeRepository> _sponsorshipCodeRepository;
        private Mock<ISmartLinkRepository> _smartLinkRepository;
        private Mock<IUserRepository> _userRepository;
        private Mock<ISponsorshipService> _sponsorshipService;

        [SetUp]
        public void Setup()
        {
            _sponsorProfileRepository = new Mock<ISponsorProfileRepository>();
            _sponsorshipPurchaseRepository = new Mock<ISponsorshipPurchaseRepository>();
            _sponsorshipCodeRepository = new Mock<ISponsorshipCodeRepository>();
            _smartLinkRepository = new Mock<ISmartLinkRepository>();
            _userRepository = new Mock<IUserRepository>();
            _sponsorshipService = new Mock<ISponsorshipService>();
        }

        #region Create Sponsorship Code Command Tests

        [Test]
        public async Task CreateSponsorshipCode_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var handler = new CreateSponsorshipCodeCommandHandler(
                _sponsorshipCodeRepository.Object,
                _sponsorProfileRepository.Object);

            var command = new CreateSponsorshipCodeCommand
            {
                SponsorId = 1,
                SubscriptionTierId = 3,
                ValidityDays = 365,
                Code = "TEST001-ABC123"
            };

            var sponsorProfile = SponsorshipDataHelper.GetSponsorProfile();
            var tier = SubscriptionDataHelper.GetSubscriptionTier(3, "L");

            _sponsorProfileRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SponsorProfile, bool>>>()))
                .ReturnsAsync(sponsorProfile);

            _sponsorshipCodeRepository.Setup(x => x.Add(It.IsAny<SponsorshipCode>()));
            _sponsorshipCodeRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("Sponsorship code created successfully");

            _sponsorshipCodeRepository.Verify(x => x.Add(It.Is<SponsorshipCode>(sc => 
                sc.SponsorId == command.SponsorId && 
                sc.SubscriptionTierId == command.SubscriptionTierId)), Times.Once);
            _sponsorshipCodeRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task CreateSponsorshipCode_InvalidSponsor_ReturnsError()
        {
            // Arrange
            var handler = new CreateSponsorshipCodeCommandHandler(
                _sponsorshipCodeRepository.Object,
                _sponsorProfileRepository.Object);

            var command = new CreateSponsorshipCodeCommand
            {
                SponsorId = 999,
                SubscriptionTierId = 3,
                ValidityDays = 365
            };

            _sponsorProfileRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SponsorProfile, bool>>>()))
                .ReturnsAsync((SponsorProfile)null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Sponsor profile not found");

            _sponsorshipCodeRepository.Verify(x => x.Add(It.IsAny<SponsorshipCode>()), Times.Never);
        }

        #endregion

        #region Purchase Bulk Sponsorship Command Tests

        [Test]
        public async Task PurchaseBulkSponsorship_ValidRequest_ReturnsSuccessWithCodes()
        {
            // Arrange
            var handler = new PurchaseBulkSponsorshipCommandHandler(
                _sponsorshipPurchaseRepository.Object,
                _sponsorshipCodeRepository.Object,
                _sponsorProfileRepository.Object);

            var command = SponsorshipDataHelper.GetValidPurchaseCommand();
            var sponsorProfile = SponsorshipDataHelper.GetSponsorProfile();

            _sponsorProfileRepository.Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SponsorProfile, bool>>>()))
                .ReturnsAsync(sponsorProfile);

            _sponsorshipPurchaseRepository.Setup(x => x.Add(It.IsAny<SponsorshipPurchase>()));
            _sponsorshipPurchaseRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

            _sponsorshipCodeRepository.Setup(x => x.GenerateCodesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(SponsorshipDataHelper.GetSponsorshipCodeList());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Amount.Should().Be(command.Amount);

            _sponsorshipPurchaseRepository.Verify(x => x.Add(It.IsAny<SponsorshipPurchase>()), Times.Once);
            _sponsorshipCodeRepository.Verify(x => x.GenerateCodesAsync(
                It.IsAny<int>(), command.SponsorId, command.SubscriptionTierId, 
                command.Quantity, It.IsAny<string>(), command.ValidityDays), Times.Once);
        }

        [Test]
        public async Task PurchaseBulkSponsorship_InvalidQuantity_ReturnsError()
        {
            // Arrange
            var handler = new PurchaseBulkSponsorshipCommandHandler(
                _sponsorshipPurchaseRepository.Object,
                _sponsorshipCodeRepository.Object,
                _sponsorProfileRepository.Object);

            var command = SponsorshipDataHelper.GetValidPurchaseCommand();
            command.Quantity = 0; // Invalid quantity

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Quantity must be greater than 0");

            _sponsorshipPurchaseRepository.Verify(x => x.Add(It.IsAny<SponsorshipPurchase>()), Times.Never);
        }

        #endregion

        #region Redeem Sponsorship Code Command Tests

        [Test]
        public async Task RedeemSponsorshipCode_ValidCode_CreatesSubscription()
        {
            // Arrange
            var handler = new RedeemSponsorshipCodeCommandHandler(
                _sponsorshipCodeRepository.Object,
                _userRepository.Object,
                _sponsorshipService.Object);

            var command = SponsorshipDataHelper.GetValidRedemptionCommand();
            var sponsorshipCode = SponsorshipDataHelper.GetSponsorshipCode();
            var user = new User { Id = command.UserId, Email = command.UserEmail };

            _sponsorshipCodeRepository.Setup(x => x.GetByCodeAsync(command.Code))
                .ReturnsAsync(sponsorshipCode);

            _userRepository.Setup(x => x.GetAsync(command.UserId))
                .ReturnsAsync(user);

            var subscription = SubscriptionDataHelper.GetUserSubscription();
            _sponsorshipService.Setup(x => x.CreateSubscriptionFromSponsorshipAsync(command.UserId, sponsorshipCode))
                .ReturnsAsync(new SuccessDataResult<UserSubscription>(subscription));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();

            _sponsorshipCodeRepository.Verify(x => x.GetByCodeAsync(command.Code), Times.Once);
            _sponsorshipService.Verify(x => x.CreateSubscriptionFromSponsorshipAsync(command.UserId, sponsorshipCode), Times.Once);
        }

        [Test]
        public async Task RedeemSponsorshipCode_InvalidCode_ReturnsError()
        {
            // Arrange
            var handler = new RedeemSponsorshipCodeCommandHandler(
                _sponsorshipCodeRepository.Object,
                _userRepository.Object,
                _sponsorshipService.Object);

            var command = SponsorshipDataHelper.GetValidRedemptionCommand();
            command.Code = "INVALID-CODE";

            _sponsorshipCodeRepository.Setup(x => x.GetByCodeAsync(command.Code))
                .ReturnsAsync((SponsorshipCode)null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Invalid sponsorship code");

            _sponsorshipService.Verify(x => x.CreateSubscriptionFromSponsorshipAsync(It.IsAny<int>(), It.IsAny<SponsorshipCode>()), Times.Never);
        }

        [Test]
        public async Task RedeemSponsorshipCode_ExpiredCode_ReturnsError()
        {
            // Arrange
            var handler = new RedeemSponsorshipCodeCommandHandler(
                _sponsorshipCodeRepository.Object,
                _userRepository.Object,
                _sponsorshipService.Object);

            var command = SponsorshipDataHelper.GetValidRedemptionCommand();
            var expiredCode = SponsorshipDataHelper.GetSponsorshipCode();
            expiredCode.ExpiryDate = DateTime.Now.AddDays(-1); // Expired

            _sponsorshipCodeRepository.Setup(x => x.GetByCodeAsync(command.Code))
                .ReturnsAsync(expiredCode);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("expired");
        }

        [Test]
        public async Task RedeemSponsorshipCode_AlreadyUsedCode_ReturnsError()
        {
            // Arrange
            var handler = new RedeemSponsorshipCodeCommandHandler(
                _sponsorshipCodeRepository.Object,
                _userRepository.Object,
                _sponsorshipService.Object);

            var command = SponsorshipDataHelper.GetValidRedemptionCommand();
            var usedCode = SponsorshipDataHelper.GetSponsorshipCode();
            usedCode.IsUsed = true;
            usedCode.UsedDate = DateTime.Now.AddDays(-5);

            _sponsorshipCodeRepository.Setup(x => x.GetByCodeAsync(command.Code))
                .ReturnsAsync(usedCode);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("already been used");
        }

        #endregion

        #region Get Sponsorship Codes Query Tests

        [Test]
        public async Task GetSponsorshipCodes_ValidSponsor_ReturnsCodeList()
        {
            // Arrange
            var handler = new GetSponsorshipCodesQueryHandler(_sponsorshipCodeRepository.Object);
            var query = new GetSponsorshipCodesQuery
            {
                SponsorId = 1,
                OnlyUnused = false
            };

            var codes = SponsorshipDataHelper.GetSponsorshipCodeList();
            _sponsorshipCodeRepository.Setup(x => x.GetCodesBySponsorIdAsync(query.SponsorId, query.OnlyUnused))
                .ReturnsAsync(codes);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);

            _sponsorshipCodeRepository.Verify(x => x.GetCodesBySponsorIdAsync(query.SponsorId, query.OnlyUnused), Times.Once);
        }

        [Test]
        public async Task GetSponsorshipCodes_OnlyUnused_ReturnsUnusedCodes()
        {
            // Arrange
            var handler = new GetSponsorshipCodesQueryHandler(_sponsorshipCodeRepository.Object);
            var query = new GetSponsorshipCodesQuery
            {
                SponsorId = 1,
                OnlyUnused = true
            };

            var unusedCodes = SponsorshipDataHelper.GetSponsorshipCodeList()
                .Where(c => !c.IsUsed).ToList();

            _sponsorshipCodeRepository.Setup(x => x.GetCodesBySponsorIdAsync(query.SponsorId, query.OnlyUnused))
                .ReturnsAsync(unusedCodes);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2); // 2 unused codes in test data
            result.Data.Should().AllSatisfy(c => c.IsUsed.Should().BeFalse());
        }

        #endregion

        #region Validate Sponsorship Code Query Tests

        [Test]
        public async Task ValidateSponsorshipCode_ValidCode_ReturnsValidation()
        {
            // Arrange
            var handler = new ValidateSponsorshipCodeQueryHandler(_sponsorshipCodeRepository.Object);
            var query = new ValidateSponsorshipCodeQuery
            {
                Code = "SPT001-ABC123"
            };

            var code = SponsorshipDataHelper.GetSponsorshipCode(query.Code);
            _sponsorshipCodeRepository.Setup(x => x.GetByCodeAsync(query.Code))
                .ReturnsAsync(code);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Code.Should().Be(query.Code);
            result.Data.IsUsed.Should().BeFalse();
        }

        [Test]
        public async Task ValidateSponsorshipCode_InvalidCode_ReturnsError()
        {
            // Arrange
            var handler = new ValidateSponsorshipCodeQueryHandler(_sponsorshipCodeRepository.Object);
            var query = new ValidateSponsorshipCodeQuery
            {
                Code = "INVALID-CODE"
            };

            _sponsorshipCodeRepository.Setup(x => x.GetByCodeAsync(query.Code))
                .ReturnsAsync((SponsorshipCode)null);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("not found");
        }

        #endregion

        #region Get Sponsorship Purchases Query Tests

        [Test]
        public async Task GetSponsorshipPurchases_ValidSponsor_ReturnsPurchases()
        {
            // Arrange
            var handler = new GetSponsorshipPurchasesQueryHandler(_sponsorshipPurchaseRepository.Object);
            var query = new GetSponsorshipPurchasesQuery
            {
                SponsorId = 1
            };

            var purchases = new List<SponsorshipPurchase> { SponsorshipDataHelper.GetSponsorshipPurchase() };
            _sponsorshipPurchaseRepository.Setup(x => x.GetPurchasesBySponsorIdAsync(query.SponsorId))
                .ReturnsAsync(purchases);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().Amount.Should().Be(2999.50m);

            _sponsorshipPurchaseRepository.Verify(x => x.GetPurchasesBySponsorIdAsync(query.SponsorId), Times.Once);
        }

        #endregion

        #region Get Sponsorship Statistics Query Tests

        [Test]
        public async Task GetSponsorshipStatistics_ValidSponsor_ReturnsStatistics()
        {
            // Arrange
            var handler = new GetSponsorshipStatisticsQueryHandler(
                _sponsorshipCodeRepository.Object,
                _sponsorshipPurchaseRepository.Object);

            var query = new GetSponsorshipStatisticsQuery
            {
                SponsorId = 1
            };

            var codes = SponsorshipDataHelper.GetSponsorshipCodeList();
            var purchases = new List<SponsorshipPurchase> { SponsorshipDataHelper.GetSponsorshipPurchase() };

            _sponsorshipCodeRepository.Setup(x => x.GetCodesBySponsorIdAsync(query.SponsorId, false))
                .ReturnsAsync(codes);
            _sponsorshipPurchaseRepository.Setup(x => x.GetPurchasesBySponsorIdAsync(query.SponsorId))
                .ReturnsAsync(purchases);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.TotalCodes.Should().Be(3);
            result.Data.UsedCodes.Should().Be(1);
            result.Data.TotalPurchases.Should().Be(1);
        }

        #endregion
    }
}