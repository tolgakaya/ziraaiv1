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
    /// Get app info with metadata - Admin only
    /// </summary>
    public class GetAppInfoAsAdminQuery : IRequest<IDataResult<AdminAppInfoDto>>
    {
        public class GetAppInfoAsAdminQueryHandler : IRequestHandler<GetAppInfoAsAdminQuery, IDataResult<AdminAppInfoDto>>
        {
            private readonly IAppInfoRepository _appInfoRepository;
            private readonly IUserRepository _userRepository;

            public GetAppInfoAsAdminQueryHandler(
                IAppInfoRepository appInfoRepository,
                IUserRepository userRepository)
            {
                _appInfoRepository = appInfoRepository;
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<AdminAppInfoDto>> Handle(GetAppInfoAsAdminQuery request, CancellationToken cancellationToken)
            {
                var appInfo = await _appInfoRepository.GetActiveAppInfoAsync();

                if (appInfo == null)
                {
                    return new ErrorDataResult<AdminAppInfoDto>("Uygulama bilgileri bulunamadı. Lütfen önce bilgileri oluşturun.");
                }

                // Get updated by user name
                string updatedByUserName = null;
                if (appInfo.UpdatedByUserId.HasValue)
                {
                    var user = await _userRepository.GetAsync(u => u.UserId == appInfo.UpdatedByUserId.Value);
                    updatedByUserName = user?.FullName;
                }

                var adminAppInfoDto = new AdminAppInfoDto
                {
                    Id = appInfo.Id,
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
                    IsActive = appInfo.IsActive,
                    CreatedDate = appInfo.CreatedDate,
                    UpdatedDate = appInfo.UpdatedDate,
                    UpdatedByUserId = appInfo.UpdatedByUserId,
                    UpdatedByUserName = updatedByUserName
                };

                return new SuccessDataResult<AdminAppInfoDto>(adminAppInfoDto);
            }
        }
    }
}
