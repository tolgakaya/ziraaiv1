using Business.Services.Sponsorship;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.SponsorProfiles.Queries
{
    public class GetSponsorProfileQuery : IRequest<IDataResult<SponsorProfileDto>>
    {
        public int SponsorId { get; set; }

        public class GetSponsorProfileQueryHandler : IRequestHandler<GetSponsorProfileQuery, IDataResult<SponsorProfileDto>>
        {
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly ISponsorVisibilityService _sponsorVisibilityService;

            public GetSponsorProfileQueryHandler(
                ISponsorProfileRepository sponsorProfileRepository,
                ISponsorVisibilityService sponsorVisibilityService)
            {
                _sponsorProfileRepository = sponsorProfileRepository;
                _sponsorVisibilityService = sponsorVisibilityService;
            }

            [CacheAspect(10)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsorProfileDto>> Handle(GetSponsorProfileQuery request, CancellationToken cancellationToken)
            {
                var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(request.SponsorId);
                if (sponsorProfile == null)
                    return new ErrorDataResult<SponsorProfileDto>("Sponsor profile not found");

                var profileDto = new SponsorProfileDto
                {
                    Id = sponsorProfile.Id,
                    SponsorId = sponsorProfile.SponsorId,
                    CompanyName = sponsorProfile.CompanyName,
                    CompanyDescription = sponsorProfile.CompanyDescription,
                    SponsorLogoUrl = sponsorProfile.SponsorLogoUrl,
                    WebsiteUrl = sponsorProfile.WebsiteUrl,
                    ContactEmail = sponsorProfile.ContactEmail,
                    ContactPhone = sponsorProfile.ContactPhone,
                    ContactPerson = sponsorProfile.ContactPerson,
                    CompanyType = sponsorProfile.CompanyType,
                    BusinessModel = sponsorProfile.BusinessModel,
                    IsVerifiedCompany = sponsorProfile.IsVerifiedCompany,
                    IsActive = sponsorProfile.IsActive,
                    TotalPurchases = sponsorProfile.TotalPurchases,
                    TotalCodesGenerated = sponsorProfile.TotalCodesGenerated,
                    TotalCodesRedeemed = sponsorProfile.TotalCodesRedeemed,
                    TotalInvestment = sponsorProfile.TotalInvestment,
                    CreatedDate = sponsorProfile.CreatedDate,
                    UpdatedDate = sponsorProfile.UpdatedDate
                };

                return new SuccessDataResult<SponsorProfileDto>(profileDto);
            }
        }
    }
}