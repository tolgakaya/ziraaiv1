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
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.SponsorProfiles.Commands
{
    public class UpdateSponsorProfileCommand : IRequest<IResult>
    {
        public int SponsorId { get; set; }
        
        // All fields optional for partial updates
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string SponsorLogoUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string ContactPerson { get; set; }
        public string CompanyType { get; set; }
        public string BusinessModel { get; set; }
        
        // Social Media Links
        public string LinkedInUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        
        // Business Information
        public string TaxNumber { get; set; }
        public string TradeRegistryNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        
        // Special: Password update (optional)
        public string Password { get; set; }

        public class UpdateSponsorProfileCommandHandler : IRequestHandler<UpdateSponsorProfileCommand, IResult>
        {
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly IUserRepository _userRepository;
            private readonly ILogger<UpdateSponsorProfileCommandHandler> _logger;

            public UpdateSponsorProfileCommandHandler(
                ISponsorProfileRepository sponsorProfileRepository,
                IUserRepository userRepository,
                ILogger<UpdateSponsorProfileCommandHandler> logger)
            {
                _sponsorProfileRepository = sponsorProfileRepository;
                _userRepository = userRepository;
                _logger = logger;
            }

            [ValidationAspect(typeof(UpdateSponsorProfileValidator), Priority = 1)]
            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(UpdateSponsorProfileCommand request, CancellationToken cancellationToken)
            {
                // Get existing profile
                var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(request.SponsorId);
                if (sponsorProfile == null)
                    return new ErrorResult(Messages.SponsorProfileNotFound);

                // Update only non-null fields (partial update support)
                if (!string.IsNullOrWhiteSpace(request.CompanyName))
                    sponsorProfile.CompanyName = request.CompanyName;
                
                if (!string.IsNullOrWhiteSpace(request.CompanyDescription))
                    sponsorProfile.CompanyDescription = request.CompanyDescription;
                
                if (!string.IsNullOrWhiteSpace(request.SponsorLogoUrl))
                    sponsorProfile.SponsorLogoUrl = request.SponsorLogoUrl;
                
                if (!string.IsNullOrWhiteSpace(request.WebsiteUrl))
                    sponsorProfile.WebsiteUrl = request.WebsiteUrl;
                
                if (!string.IsNullOrWhiteSpace(request.ContactEmail))
                    sponsorProfile.ContactEmail = request.ContactEmail;
                
                if (!string.IsNullOrWhiteSpace(request.ContactPhone))
                    sponsorProfile.ContactPhone = request.ContactPhone;
                
                if (!string.IsNullOrWhiteSpace(request.ContactPerson))
                    sponsorProfile.ContactPerson = request.ContactPerson;
                
                if (!string.IsNullOrWhiteSpace(request.CompanyType))
                    sponsorProfile.CompanyType = request.CompanyType;
                
                if (!string.IsNullOrWhiteSpace(request.BusinessModel))
                    sponsorProfile.BusinessModel = request.BusinessModel;
                
                // Social Media Links
                if (!string.IsNullOrWhiteSpace(request.LinkedInUrl))
                    sponsorProfile.LinkedInUrl = request.LinkedInUrl;
                
                if (!string.IsNullOrWhiteSpace(request.TwitterUrl))
                    sponsorProfile.TwitterUrl = request.TwitterUrl;
                
                if (!string.IsNullOrWhiteSpace(request.FacebookUrl))
                    sponsorProfile.FacebookUrl = request.FacebookUrl;
                
                if (!string.IsNullOrWhiteSpace(request.InstagramUrl))
                    sponsorProfile.InstagramUrl = request.InstagramUrl;
                
                // Business Information
                if (!string.IsNullOrWhiteSpace(request.TaxNumber))
                    sponsorProfile.TaxNumber = request.TaxNumber;
                
                if (!string.IsNullOrWhiteSpace(request.TradeRegistryNumber))
                    sponsorProfile.TradeRegistryNumber = request.TradeRegistryNumber;
                
                if (!string.IsNullOrWhiteSpace(request.Address))
                    sponsorProfile.Address = request.Address;
                
                if (!string.IsNullOrWhiteSpace(request.City))
                    sponsorProfile.City = request.City;
                
                if (!string.IsNullOrWhiteSpace(request.Country))
                    sponsorProfile.Country = request.Country;
                
                if (!string.IsNullOrWhiteSpace(request.PostalCode))
                    sponsorProfile.PostalCode = request.PostalCode;
                
                // Set update metadata
                sponsorProfile.UpdatedDate = DateTime.Now;
                sponsorProfile.UpdatedByUserId = request.SponsorId;

                _sponsorProfileRepository.Update(sponsorProfile);
                await _sponsorProfileRepository.SaveChangesAsync();

                // Update user's email and password if provided
                var user = await _userRepository.GetAsync(u => u.UserId == request.SponsorId);
                if (user != null)
                {
                    _logger.LogInformation("📧 [UpdateSponsorProfile] User found - UserId: {UserId}, CurrentEmail: {CurrentEmail}",
                        user.UserId, user.Email);

                    bool needsUpdate = false;

                    // Update email if provided and different (case-insensitive comparison)
                    if (!string.IsNullOrWhiteSpace(request.ContactEmail))
                    {
                        var normalizedNewEmail = request.ContactEmail.Trim().ToLowerInvariant();
                        var normalizedCurrentEmail = user.Email?.Trim().ToLowerInvariant() ?? "";

                        _logger.LogInformation("📧 [UpdateSponsorProfile] Email comparison - New: {NewEmail}, Current: {CurrentEmail}",
                            normalizedNewEmail, normalizedCurrentEmail);

                        if (normalizedNewEmail != normalizedCurrentEmail)
                        {
                            // Check if the new email is already in use by another user
                            var emailExists = await _userRepository.GetAsync(u =>
                                u.Email.ToLower() == normalizedNewEmail && u.UserId != request.SponsorId);

                            if (emailExists == null)
                            {
                                user.Email = request.ContactEmail.Trim();
                                needsUpdate = true;
                                _logger.LogInformation("✅ [UpdateSponsorProfile] Email will be updated to: {NewEmail}", user.Email);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ [UpdateSponsorProfile] Email {Email} already exists for another user", normalizedNewEmail);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("ℹ️ [UpdateSponsorProfile] Email unchanged (same as current)");
                        }
                    }

                    // Update password if provided
                    if (!string.IsNullOrWhiteSpace(request.Password))
                    {
                        _logger.LogInformation("🔐 [UpdateSponsorProfile] Password will be updated");

                        Core.Utilities.Security.Hashing.HashingHelper.CreatePasswordHash(
                            request.Password,
                            out byte[] passwordSalt,
                            out byte[] passwordHash);

                        user.PasswordHash = passwordHash;
                        user.PasswordSalt = passwordSalt;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        _logger.LogInformation("💾 [UpdateSponsorProfile] Updating user - Email: {Email}, HasPassword: {HasPassword}",
                            user.Email, user.PasswordHash != null && user.PasswordHash.Length > 0);

                        _userRepository.Update(user);
                        await _userRepository.SaveChangesAsync();

                        _logger.LogInformation("✅ [UpdateSponsorProfile] User updated successfully");
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ [UpdateSponsorProfile] No user updates needed");
                    }
                }

                return new SuccessResult(Messages.SponsorProfileUpdated);
            }
        }
    }
}
