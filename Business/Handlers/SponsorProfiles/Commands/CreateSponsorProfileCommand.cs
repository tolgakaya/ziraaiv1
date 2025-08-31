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
        public string CompanyType { get; set; }
        public string BusinessModel { get; set; }

        public class CreateSponsorProfileCommandHandler : IRequestHandler<CreateSponsorProfileCommand, IResult>
        {
            private readonly ISponsorProfileRepository _sponsorProfileRepository;

            public CreateSponsorProfileCommandHandler(ISponsorProfileRepository sponsorProfileRepository)
            {
                _sponsorProfileRepository = sponsorProfileRepository;
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
                    CompanyType = request.CompanyType ?? "Agriculture",
                    BusinessModel = request.BusinessModel ?? "B2B",
                    IsVerifiedCompany = false,
                    IsActive = true,
                    TotalPurchases = 0,
                    TotalCodesGenerated = 0,
                    TotalCodesRedeemed = 0,
                    TotalInvestment = 0,
                    CreatedDate = DateTime.Now
                };

                _sponsorProfileRepository.Add(sponsorProfile);
                await _sponsorProfileRepository.SaveChangesAsync();
                return new SuccessResult(Messages.SponsorProfileCreated);
            }

        }
    }
}