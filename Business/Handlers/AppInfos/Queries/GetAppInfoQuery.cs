using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.AppInfos.Queries
{
    /// <summary>
    /// Get app info for viewing - Farmer or Sponsor
    /// </summary>
    public class GetAppInfoQuery : IRequest<IDataResult<AppInfoDto>>
    {
        public class GetAppInfoQueryHandler : IRequestHandler<GetAppInfoQuery, IDataResult<AppInfoDto>>
        {
            private readonly IAppInfoRepository _appInfoRepository;

            public GetAppInfoQueryHandler(IAppInfoRepository appInfoRepository)
            {
                _appInfoRepository = appInfoRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<AppInfoDto>> Handle(GetAppInfoQuery request, CancellationToken cancellationToken)
            {
                var appInfo = await _appInfoRepository.GetActiveAppInfoAsync();

                if (appInfo == null)
                {
                    return new ErrorDataResult<AppInfoDto>("Uygulama bilgileri bulunamadı.");
                }

                var appInfoDto = new AppInfoDto
                {
                    CompanyName = appInfo.CompanyName,
                    CompanyDescription = appInfo.CompanyDescription,
                    AppVersion = appInfo.AppVersion,
                    Address = appInfo.Address,
                    Email = appInfo.Email,
                    Phone = appInfo.Phone,
                    WebsiteUrl = appInfo.WebsiteUrl,
                    FacebookUrl = appInfo.FacebookUrl,
                    InstagramUrl = appInfo.InstagramUrl,
                    YouTubeUrl = appInfo.YouTubeUrl,
                    TwitterUrl = appInfo.TwitterUrl,
                    LinkedInUrl = appInfo.LinkedInUrl,
                    TermsOfServiceUrl = appInfo.TermsOfServiceUrl,
                    PrivacyPolicyUrl = appInfo.PrivacyPolicyUrl,
                    CookiePolicyUrl = appInfo.CookiePolicyUrl,
                    UpdatedDate = appInfo.UpdatedDate
                };

                return new SuccessDataResult<AppInfoDto>(appInfoDto);
            }
        }
    }
}
