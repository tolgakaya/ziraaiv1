using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Business.Handlers.Sponsorship.Commands;
using Business.Handlers.Sponsorship.Queries;
using Business.Handlers.Sponsorships.Queries;
using Business.Handlers.SponsorProfiles.Commands;
using Business.Handlers.SponsorProfiles.Queries;
using Business.Handlers.AnalysisMessages.Commands;
using Business.Handlers.SmartLinks.Commands;
using Business.Handlers.SmartLinks.Queries;
using Core.Utilities.Results;
using Entities.Dtos;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Tests.Helpers;
using WebAPI.Controllers;

namespace Tests.WebAPI
{
    [TestFixture]
    public class SponsorshipControllerTests
    {
        private Mock<IMediator> _mediator;
        private Mock<ILogger<SponsorshipController>> _logger;
        private SponsorshipController _controller;

        [SetUp]
        public void Setup()
        {
            _mediator = new Mock<IMediator>();
            _logger = new Mock<ILogger<SponsorshipController>>();

            _controller = new SponsorshipController(_logger.Object);
            
            // Set up mediator using reflection (BaseApiController has protected Mediator property)
            var mediatorProperty = typeof(BaseApiController).GetProperty("Mediator");
            mediatorProperty?.SetValue(_controller, _mediator.Object);
            
            SetupControllerContext();
        }

        #region Create Sponsor Profile Tests

        [Test]
        public async Task CreateSponsorProfile_ValidRequest_ReturnsOk()
        {
            // Arrange
            var dto = SponsorshipDataHelper.GetValidCreateSponsorProfileDto();
            var expectedResult = new SuccessDataResult<SponsorProfile>(
                SponsorshipDataHelper.GetSponsorProfile(), "Profile created successfully");

            _mediator.Setup(x => x.Send(It.IsAny<CreateSponsorProfileCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateSponsorProfile(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.Value.Should().BeEquivalentTo(expectedResult);

            _mediator.Verify(x => x.Send(It.Is<CreateSponsorProfileCommand>(cmd => 
                cmd.SponsorId == 1 && 
                cmd.CompanyName == dto.CompanyName), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateSponsorProfile_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerContext(userId: null);
            var dto = SponsorshipDataHelper.GetValidCreateSponsorProfileDto();

            // Act
            var result = await _controller.CreateSponsorProfile(dto);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
            _mediator.Verify(x => x.Send(It.IsAny<CreateSponsorProfileCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateSponsorProfile_HandlerReturnsError_ReturnsBadRequest()
        {
            // Arrange
            var dto = SponsorshipDataHelper.GetValidCreateSponsorProfileDto();
            var errorResult = new ErrorResult("Profile creation failed");

            _mediator.Setup(x => x.Send(It.IsAny<CreateSponsorProfileCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(errorResult);

            // Act
            var result = await _controller.CreateSponsorProfile(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task CreateSponsorProfile_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var dto = SponsorshipDataHelper.GetValidCreateSponsorProfileDto();

            _mediator.Setup(x => x.Send(It.IsAny<CreateSponsorProfileCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateSponsorProfile(dto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult?.StatusCode.Should().Be(500);
        }

        #endregion

        #region Purchase Package Tests

        [Test]
        public async Task PurchasePackage_ValidRequest_ReturnsOk()
        {
            // Arrange
            var command = SponsorshipDataHelper.GetValidPurchaseCommand();
            var purchase = SponsorshipDataHelper.GetSponsorshipPurchase();
            var expectedResult = new SuccessDataResult<SponsorshipPurchaseResponseDto>(
                new SponsorshipPurchaseResponseDto { Id = purchase.Id, Amount = purchase.Amount },
                "Package purchased successfully");

            _mediator.Setup(x => x.Send(It.IsAny<PurchaseBulkSponsorshipCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.PurchasePackage(command);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.Value.Should().BeEquivalentTo(expectedResult);

            _mediator.Verify(x => x.Send(It.Is<PurchaseBulkSponsorshipCommand>(cmd => 
                cmd.SponsorId == 1 && 
                cmd.Quantity == command.Quantity), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PurchasePackage_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerContext(userId: null);
            var command = SponsorshipDataHelper.GetValidPurchaseCommand();

            // Act
            var result = await _controller.PurchasePackage(command);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        #endregion

        #region Redeem Sponsorship Code Tests

        [Test]
        public async Task RedeemSponsorshipCode_ValidRequest_ReturnsOk()
        {
            // Arrange
            SetupControllerContext(role: "Farmer");
            var command = SponsorshipDataHelper.GetValidRedemptionCommand();
            var subscription = SubscriptionDataHelper.GetUserSubscription();
            var expectedResult = new SuccessDataResult<UserSubscriptionDto>(
                new UserSubscriptionDto { Id = subscription.Id }, "Code redeemed successfully");

            _mediator.Setup(x => x.Send(It.IsAny<RedeemSponsorshipCodeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.RedeemSponsorshipCode(command);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mediator.Verify(x => x.Send(It.Is<RedeemSponsorshipCodeCommand>(cmd => 
                cmd.UserId == 1 && 
                cmd.Code == command.Code), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task RedeemSponsorshipCode_InvalidCode_ReturnsBadRequest()
        {
            // Arrange
            SetupControllerContext(role: "Farmer");
            var command = SponsorshipDataHelper.GetValidRedemptionCommand();
            var errorResult = new ErrorResult("Invalid or expired sponsorship code");

            _mediator.Setup(x => x.Send(It.IsAny<RedeemSponsorshipCodeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(errorResult);

            // Act
            var result = await _controller.RedeemSponsorshipCode(command);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region Get Sponsorship Codes Tests

        [Test]
        public async Task GetSponsorshipCodes_ValidRequest_ReturnsOk()
        {
            // Arrange
            var codes = SponsorshipDataHelper.GetSponsorshipCodeList();
            var expectedResult = new SuccessDataResult<List<SponsorshipCodeDto>>(
                codes.Select(c => new SponsorshipCodeDto { Code = c.Code, IsUsed = c.IsUsed }).ToList());

            _mediator.Setup(x => x.Send(It.IsAny<GetSponsorshipCodesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetSponsorshipCodes(onlyUnused: true);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mediator.Verify(x => x.Send(It.Is<GetSponsorshipCodesQuery>(q => 
                q.SponsorId == 1 && q.OnlyUnused == true), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetSponsorshipCodes_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerContext(userId: null);

            // Act
            var result = await _controller.GetSponsorshipCodes();

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        #endregion

        #region Validate Sponsorship Code Tests

        [Test]
        public async Task ValidateSponsorshipCode_ValidCode_ReturnsOkWithValidData()
        {
            // Arrange
            var code = "SPT001-ABC123";
            var sponsorshipCode = SponsorshipDataHelper.GetSponsorshipCode(code);
            var expectedResult = new SuccessDataResult<SponsorshipCodeDto>(
                new SponsorshipCodeDto { Code = code, IsUsed = false });

            _mediator.Setup(x => x.Send(It.IsAny<ValidateSponsorshipCodeQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.ValidateSponsorshipCode(code);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = okResult?.Value as dynamic;
            responseData?.Success.Should().Be(true);
            responseData?.Data.IsValid.Should().Be(true);
        }

        [Test]
        public async Task ValidateSponsorshipCode_InvalidCode_ReturnsOkWithInvalidData()
        {
            // Arrange
            var code = "INVALID-CODE";
            var errorResult = new ErrorResult("Code not found or expired");

            _mediator.Setup(x => x.Send(It.IsAny<ValidateSponsorshipCodeQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(errorResult);

            // Act
            var result = await _controller.ValidateSponsorshipCode(code);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseData = okResult?.Value as dynamic;
            responseData?.Success.Should().Be(false);
            responseData?.Data.IsValid.Should().Be(false);
        }

        #endregion

        #region Smart Links Tests

        [Test]
        public async Task CreateSmartLink_ValidRequest_ReturnsOk()
        {
            // Arrange
            var command = SponsorshipDataHelper.GetValidCreateSmartLinkCommand();
            var smartLink = SponsorshipDataHelper.GetSmartLink();
            var expectedResult = new SuccessDataResult<SmartLinkDto>(
                new SmartLinkDto { Id = smartLink.Id, LinkUrl = smartLink.LinkUrl },
                "Smart link created successfully");

            _mediator.Setup(x => x.Send(It.IsAny<CreateSmartLinkCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateSmartLink(command);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mediator.Verify(x => x.Send(It.Is<CreateSmartLinkCommand>(cmd => 
                cmd.SponsorId == 1 && 
                cmd.LinkUrl == command.LinkUrl), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateSmartLink_InsufficientTier_ReturnsBadRequest()
        {
            // Arrange
            var command = SponsorshipDataHelper.GetValidCreateSmartLinkCommand();
            var errorResult = new ErrorResult("Smart links are only available for XL tier sponsors");

            _mediator.Setup(x => x.Send(It.IsAny<CreateSmartLinkCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(errorResult);

            // Act
            var result = await _controller.CreateSmartLink(command);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task GetSmartLinks_ValidRequest_ReturnsOk()
        {
            // Arrange
            var smartLinks = new List<SmartLinkDto> 
            { 
                new SmartLinkDto { Id = 1, LinkUrl = "https://test.com" }
            };
            var expectedResult = new SuccessDataResult<List<SmartLinkDto>>(smartLinks);

            _mediator.Setup(x => x.Send(It.IsAny<GetSponsorSmartLinksQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetSmartLinks();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region Send Message Tests

        [Test]
        public async Task SendMessage_ValidRequest_ReturnsOk()
        {
            // Arrange
            var command = SponsorshipDataHelper.GetValidSendMessageCommand();
            var expectedResult = new SuccessResult("Message sent successfully");

            _mediator.Setup(x => x.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SendMessage(command);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mediator.Verify(x => x.Send(It.Is<SendMessageCommand>(cmd => 
                cmd.FromUserId == 1 && 
                cmd.FarmerId == command.FarmerId), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SendMessage_InsufficientTier_ReturnsBadRequest()
        {
            // Arrange
            var command = SponsorshipDataHelper.GetValidSendMessageCommand();
            var errorResult = new ErrorResult("Messaging is not allowed for your subscription tier");

            _mediator.Setup(x => x.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(errorResult);

            // Act
            var result = await _controller.SendMessage(command);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region Logo Permissions Tests

        [Test]
        public async Task GetLogoPermissionsForAnalysis_ValidRequest_ReturnsOk()
        {
            // Arrange
            var plantAnalysisId = 123;
            var screen = "results";
            var expectedResult = new SuccessDataResult<LogoPermissionDto>(
                new LogoPermissionDto { CanDisplayLogo = true, TierName = "XL" });

            _mediator.Setup(x => x.Send(It.IsAny<GetLogoPermissionsForAnalysisQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetLogoPermissionsForAnalysis(plantAnalysisId, screen);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mediator.Verify(x => x.Send(It.Is<GetLogoPermissionsForAnalysisQuery>(q => 
                q.PlantAnalysisId == plantAnalysisId && 
                q.Screen == screen), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetDisplayInfoForAnalysis_ValidRequest_ReturnsOk()
        {
            // Arrange
            var plantAnalysisId = 123;
            var screen = "result";
            var expectedResult = new SuccessDataResult<SponsorDisplayInfoDto>(
                new SponsorDisplayInfoDto { CanDisplay = true, CompanyName = "Test Company" });

            _mediator.Setup(x => x.Send(It.IsAny<GetSponsorDisplayInfoForAnalysisQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetDisplayInfoForAnalysis(plantAnalysisId, screen);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region Send Sponsorship Link Tests

        [Test]
        public async Task SendSponsorshipLink_ValidRequest_ReturnsOk()
        {
            // Arrange
            var command = SponsorshipDataHelper.GetValidSendLinkCommand();
            var expectedResult = new SuccessDataResult<BulkSendResultDto>(
                new BulkSendResultDto { SuccessCount = 1, FailureCount = 0 },
                "Links sent successfully");

            _mediator.Setup(x => x.Send(It.IsAny<SendSponsorshipLinkCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SendSponsorshipLink(command);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mediator.Verify(x => x.Send(It.Is<SendSponsorshipLinkCommand>(cmd => 
                cmd.SponsorId == 1 && 
                cmd.Recipients.Count == command.Recipients.Count), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SendSponsorshipLink_SendingFails_ReturnsBadRequest()
        {
            // Arrange
            var command = SponsorshipDataHelper.GetValidSendLinkCommand();
            var expectedResult = new SuccessDataResult<BulkSendResultDto>(
                new BulkSendResultDto { SuccessCount = 0, FailureCount = 1 },
                "Some links failed to send");
            expectedResult.Success = false;

            _mediator.Setup(x => x.Send(It.IsAny<SendSponsorshipLinkCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SendSponsorshipLink(command);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task SendSponsorshipLink_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var command = SponsorshipDataHelper.GetValidSendLinkCommand();

            _mediator.Setup(x => x.Send(It.IsAny<SendSponsorshipLinkCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("SMS service unavailable"));

            // Act
            var result = await _controller.SendSponsorshipLink(command);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult?.StatusCode.Should().Be(500);
        }

        #endregion

        #region Helper Methods

        private void SetupControllerContext(int? userId = 1, string role = "Sponsor", string email = "test@sponsor.com")
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