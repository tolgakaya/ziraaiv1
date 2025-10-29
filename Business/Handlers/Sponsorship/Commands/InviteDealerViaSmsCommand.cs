using Business.Services.Messaging;
using Business.Services.Messaging.Factories;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    public class InviteDealerViaSmsCommand : IRequest<IDataResult<DealerInvitationResponseDto>>
    {
        public int SponsorId { get; set; } // Authenticated sponsor ID
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DealerName { get; set; }
        public int PurchaseId { get; set; }
        public int CodeCount { get; set; }

        public class InviteDealerViaSmsCommandHandler : IRequestHandler<InviteDealerViaSmsCommand, IDataResult<DealerInvitationResponseDto>>
        {
            private readonly IDealerInvitationRepository _invitationRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly IMessagingServiceFactory _messagingFactory;
            private readonly IConfiguration _configuration;
            private readonly ILogger<InviteDealerViaSmsCommandHandler> _logger;

            public InviteDealerViaSmsCommandHandler(
                IDealerInvitationRepository invitationRepository,
                ISponsorshipCodeRepository codeRepository,
                ISponsorProfileRepository sponsorProfileRepository,
                IMessagingServiceFactory messagingFactory,
                IConfiguration configuration,
                ILogger<InviteDealerViaSmsCommandHandler> logger)
            {
                _invitationRepository = invitationRepository;
                _codeRepository = codeRepository;
                _sponsorProfileRepository = sponsorProfileRepository;
                _messagingFactory = messagingFactory;
                _configuration = configuration;
                _logger = logger;
            }

            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<DealerInvitationResponseDto>> Handle(InviteDealerViaSmsCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("📨 Sponsor {SponsorId} sending dealer invitation via SMS to {Phone}",
                        request.SponsorId, request.Phone);

                    // 1. Validate available codes
                    var availableCodes = await _codeRepository.GetListAsync(c =>
                        c.SponsorId == request.SponsorId &&
                        c.SponsorshipPurchaseId == request.PurchaseId &&
                        !c.IsUsed &&
                        c.DealerId == null &&
                        c.ExpiryDate > DateTime.Now);

                    var availableCodesList = availableCodes.ToList();
                    if (availableCodesList.Count < request.CodeCount)
                    {
                        _logger.LogWarning("❌ Insufficient codes. Available: {Available}, Requested: {Requested}",
                            availableCodesList.Count, request.CodeCount);
                        return new ErrorDataResult<DealerInvitationResponseDto>(
                            $"Yetersiz kod. Mevcut: {availableCodesList.Count}, İstenen: {request.CodeCount}");
                    }

                    // 2. Get sponsor profile for SMS template
                    var sponsorProfile = await _sponsorProfileRepository.GetAsync(sp => sp.SponsorId == request.SponsorId);
                    var sponsorCompanyName = sponsorProfile?.CompanyName ?? "ZiraAI Sponsor";

                    // 3. Create dealer invitation
                    var invitation = new DealerInvitation
                    {
                        SponsorId = request.SponsorId,
                        Email = request.Email,
                        Phone = FormatPhoneNumber(request.Phone),
                        DealerName = request.DealerName,
                        PurchaseId = request.PurchaseId,
                        CodeCount = request.CodeCount,
                        InvitationType = "Invite",
                        InvitationToken = Guid.NewGuid().ToString("N"), // 32-character hex
                        Status = "Pending",
                        CreatedDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddDays(
                            _configuration.GetValue<int>("DealerInvitation:TokenExpiryDays", 7))
                    };

                    _invitationRepository.Add(invitation);
                    await _invitationRepository.SaveChangesAsync();

                    _logger.LogInformation("✅ Created invitation {InvitationId} with token {Token}",
                        invitation.Id, invitation.InvitationToken);

                    // 4. Generate deep link
                    var baseUrl = _configuration["DealerInvitation:DeepLinkBaseUrl"]
                        ?? _configuration["WebAPI:BaseUrl"]?.TrimEnd('/') + "/dealer-invitation/"
                        ?? throw new InvalidOperationException("DealerInvitation:DeepLinkBaseUrl configuration is missing");

                    var deepLink = $"{baseUrl.TrimEnd('/')}/DEALER-{invitation.InvitationToken}";

                    // 5. Get Play Store link
                    var playStorePackageName = _configuration["MobileApp:PlayStorePackageName"] ?? "com.ziraai.app";
                    var playStoreLink = $"https://play.google.com/store/apps/details?id={playStorePackageName}";

                    // 6. Build SMS message
                    var smsTemplate = _configuration["DealerInvitation:SmsTemplate"];
                    var smsMessage = string.IsNullOrEmpty(smsTemplate)
                        ? BuildDefaultSmsMessage(sponsorCompanyName, invitation.InvitationToken, deepLink, playStoreLink)
                        : smsTemplate
                            .Replace("{sponsorName}", sponsorCompanyName)
                            .Replace("{token}", invitation.InvitationToken)
                            .Replace("{deepLink}", deepLink)
                            .Replace("{playStoreLink}", playStoreLink);

                    // 7. Send SMS
                    var smsService = _messagingFactory.GetSmsService();
                    var sendResult = await smsService.SendSmsAsync(invitation.Phone, smsMessage);

                    if (!sendResult.Success)
                    {
                        _logger.LogWarning("⚠️ SMS send failed for invitation {InvitationId}: {Error}",
                            invitation.Id, sendResult.Message);

                        // Update invitation with failed status but don't fail the whole operation
                        invitation.LinkSentDate = DateTime.Now;
                        invitation.LinkSentVia = "SMS";
                        invitation.LinkDelivered = false;
                        await _invitationRepository.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogInformation("✅ SMS sent successfully for invitation {InvitationId}", invitation.Id);

                        invitation.LinkSentDate = DateTime.Now;
                        invitation.LinkSentVia = "SMS";
                        invitation.LinkDelivered = true;
                        await _invitationRepository.SaveChangesAsync();
                    }

                    // 8. Build response
                    var response = new DealerInvitationResponseDto
                    {
                        InvitationId = invitation.Id,
                        InvitationToken = invitation.InvitationToken,
                        InvitationLink = deepLink,
                        Email = invitation.Email,
                        Phone = invitation.Phone,
                        DealerName = invitation.DealerName,
                        CodeCount = invitation.CodeCount,
                        Status = invitation.Status,
                        InvitationType = invitation.InvitationType,
                        CreatedAt = invitation.CreatedDate,
                        ExpiryDate = invitation.ExpiryDate,
                        SmsSent = sendResult.Success,
                        SmsDeliveryStatus = sendResult.Success ? "Sent" : "Failed"
                    };

                    var successMessage = sendResult.Success
                        ? $"📱 Bayilik daveti {request.Phone} numarasına SMS ile gönderildi"
                        : $"⚠️ Davetiye oluşturuldu ancak SMS gönderilemedi. Linki manuel olarak iletebilirsiniz: {deepLink}";

                    return new SuccessDataResult<DealerInvitationResponseDto>(response, successMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error creating dealer invitation for sponsor {SponsorId}", request.SponsorId);
                    return new ErrorDataResult<DealerInvitationResponseDto>("Bayilik daveti oluşturulurken hata oluştu");
                }
            }

            private string BuildDefaultSmsMessage(string sponsorName, string token, string deepLink, string playStoreLink)
            {
                return $@"🎁 {sponsorName} Bayilik Daveti!

Davet Kodunuz: DEALER-{token}

Hemen katılmak için tıklayın:
{deepLink}

Veya uygulamayı indirin:
{playStoreLink}";
            }

            private string FormatPhoneNumber(string phone)
            {
                // Remove all non-numeric characters
                var cleaned = new string(phone.Where(char.IsDigit).ToArray());

                // Add Turkey country code if not present
                if (!cleaned.StartsWith("90") && cleaned.Length == 10)
                {
                    cleaned = "90" + cleaned;
                }

                // Add + prefix
                if (!cleaned.StartsWith("+"))
                {
                    cleaned = "+" + cleaned;
                }

                return cleaned;
            }
        }
    }
}
