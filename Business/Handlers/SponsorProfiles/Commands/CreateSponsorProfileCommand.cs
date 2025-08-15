using Business.Constants;
using Business.Handlers.SponsorProfiles.ValidationRules;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.SponsorProfiles.Commands
{
    public class CreateSponsorProfileCommand : IRequest<IResult>
    {
        public int SponsorId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string SponsorLogoUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string ContactPerson { get; set; }
        public int CurrentSubscriptionTierId { get; set; }

        public class CreateSponsorProfileCommandHandler : IRequestHandler<CreateSponsorProfileCommand, IResult>
        {
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;

            public CreateSponsorProfileCommandHandler(
                ISponsorProfileRepository sponsorProfileRepository,
                ISubscriptionTierRepository subscriptionTierRepository)
            {
                _sponsorProfileRepository = sponsorProfileRepository;
                _subscriptionTierRepository = subscriptionTierRepository;
            }

            [ValidationAspect(typeof(CreateSponsorProfileValidator), Priority = 1)]
            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(CreateSponsorProfileCommand request, CancellationToken cancellationToken)
            {
                // Check if sponsor profile already exists
                var existingProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(request.SponsorId);
                if (existingProfile != null)
                    return new ErrorResult(Messages.SponsorProfileAlreadyExists);

                // Get subscription tier to set capabilities
                var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == request.CurrentSubscriptionTierId);
                if (tier == null)
                    return new ErrorResult(Messages.SubscriptionTierNotFound);

                var sponsorProfile = new SponsorProfile
                {
                    SponsorId = request.SponsorId,
                    CompanyName = request.CompanyName,
                    CompanyDescription = request.CompanyDescription,
                    SponsorLogoUrl = request.SponsorLogoUrl,
                    WebsiteUrl = request.WebsiteUrl,
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    ContactPerson = request.ContactPerson,
                    CurrentSubscriptionTierId = request.CurrentSubscriptionTierId,
                    VisibilityLevel = GetVisibilityLevel(tier.TierName),
                    DataAccessLevel = GetDataAccessLevel(tier.TierName),
                    HasMessaging = GetMessagingCapability(tier.TierName),
                    HasSmartLinking = GetSmartLinkingCapability(tier.TierName),
                    IsVerified = false,
                    IsActive = true,
                    TotalSponsored = 0,
                    ActiveSponsored = 0,
                    TotalInvestment = 0,
                    CreatedDate = DateTime.Now
                };

                await _sponsorProfileRepository.AddAsync(sponsorProfile);
                return new SuccessResult(Messages.SponsorProfileCreated);
            }

            private string GetVisibilityLevel(string tierName)
            {
                return tierName switch
                {
                    "S" => "ResultOnly",
                    "M" => "StartAndResult",
                    "L" => "AllScreens",
                    "XL" => "AllScreens",
                    _ => "ResultOnly"
                };
            }

            private string GetDataAccessLevel(string tierName)
            {
                return tierName switch
                {
                    "S" => "Basic30",
                    "M" => "Basic30",
                    "L" => "Medium60",
                    "XL" => "Full100",
                    _ => "Basic30"
                };
            }

            private bool GetMessagingCapability(string tierName)
            {
                return tierName == "L" || tierName == "XL";
            }

            private bool GetSmartLinkingCapability(string tierName)
            {
                return tierName == "XL";
            }
        }
    }
}