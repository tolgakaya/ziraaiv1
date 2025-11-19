using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;

namespace Business.Handlers.AppInfos.Commands
{
    /// <summary>
    /// Update app info - Admin only
    /// Creates new record if none exists, updates existing active record otherwise
    /// </summary>
    public class UpdateAppInfoCommand : IRequest<IResult>
    {
        public int UserId { get; set; }  // From JWT - Admin user ID

        // Company Info
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string AppVersion { get; set; }

        // Address
        public string Address { get; set; }

        // Contact Info
        public string Email { get; set; }
        public string Phone { get; set; }
        public string WebsiteUrl { get; set; }

        // Social Media Links
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string YouTubeUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string LinkedInUrl { get; set; }

        // Legal Pages URLs
        public string TermsOfServiceUrl { get; set; }
        public string PrivacyPolicyUrl { get; set; }
        public string CookiePolicyUrl { get; set; }

        public class UpdateAppInfoCommandHandler : IRequestHandler<UpdateAppInfoCommand, IResult>
        {
            private readonly IAppInfoRepository _appInfoRepository;

            public UpdateAppInfoCommandHandler(IAppInfoRepository appInfoRepository)
            {
                _appInfoRepository = appInfoRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(UpdateAppInfoCommand request, CancellationToken cancellationToken)
            {
                // Get existing active app info
                var existingAppInfo = await _appInfoRepository.GetActiveAppInfoAsync();

                if (existingAppInfo == null)
                {
                    // Create new app info record
                    var newAppInfo = new AppInfo
                    {
                        CompanyName = request.CompanyName?.Trim(),
                        CompanyDescription = request.CompanyDescription?.Trim(),
                        AppVersion = request.AppVersion?.Trim(),
                        Address = request.Address?.Trim(),
                        Email = request.Email?.Trim(),
                        Phone = request.Phone?.Trim(),
                        WebsiteUrl = request.WebsiteUrl?.Trim(),
                        FacebookUrl = request.FacebookUrl?.Trim(),
                        InstagramUrl = request.InstagramUrl?.Trim(),
                        YouTubeUrl = request.YouTubeUrl?.Trim(),
                        TwitterUrl = request.TwitterUrl?.Trim(),
                        LinkedInUrl = request.LinkedInUrl?.Trim(),
                        TermsOfServiceUrl = request.TermsOfServiceUrl?.Trim(),
                        PrivacyPolicyUrl = request.PrivacyPolicyUrl?.Trim(),
                        CookiePolicyUrl = request.CookiePolicyUrl?.Trim(),
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                        UpdatedByUserId = request.UserId
                    };

                    _appInfoRepository.Add(newAppInfo);
                    await _appInfoRepository.SaveChangesAsync();

                    return new SuccessResult("Uygulama bilgileri başarıyla oluşturuldu.");
                }

                // Update existing record
                existingAppInfo.CompanyName = request.CompanyName?.Trim();
                existingAppInfo.CompanyDescription = request.CompanyDescription?.Trim();
                existingAppInfo.AppVersion = request.AppVersion?.Trim();
                existingAppInfo.Address = request.Address?.Trim();
                existingAppInfo.Email = request.Email?.Trim();
                existingAppInfo.Phone = request.Phone?.Trim();
                existingAppInfo.WebsiteUrl = request.WebsiteUrl?.Trim();
                existingAppInfo.FacebookUrl = request.FacebookUrl?.Trim();
                existingAppInfo.InstagramUrl = request.InstagramUrl?.Trim();
                existingAppInfo.YouTubeUrl = request.YouTubeUrl?.Trim();
                existingAppInfo.TwitterUrl = request.TwitterUrl?.Trim();
                existingAppInfo.LinkedInUrl = request.LinkedInUrl?.Trim();
                existingAppInfo.TermsOfServiceUrl = request.TermsOfServiceUrl?.Trim();
                existingAppInfo.PrivacyPolicyUrl = request.PrivacyPolicyUrl?.Trim();
                existingAppInfo.CookiePolicyUrl = request.CookiePolicyUrl?.Trim();
                existingAppInfo.UpdatedDate = DateTime.Now;
                existingAppInfo.UpdatedByUserId = request.UserId;

                _appInfoRepository.Update(existingAppInfo);
                await _appInfoRepository.SaveChangesAsync();

                return new SuccessResult("Uygulama bilgileri başarıyla güncellendi.");
            }
        }
    }
}
