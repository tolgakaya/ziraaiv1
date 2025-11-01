using Business.Constants;
using Business.Handlers.SponsorProfiles.ValidationRules;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Core.Entities.Concrete;
using MediatR;
using Microsoft.Extensions.Logging;
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
        public string Password { get; set; } // Optional: For phone-registered users to enable email+password login

        public class CreateSponsorProfileCommandHandler : IRequestHandler<CreateSponsorProfileCommand, IResult>
        {
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly IGroupRepository _groupRepository;
            private readonly IUserRepository _userRepository;
            private readonly ILogger<CreateSponsorProfileCommandHandler> _logger;

            public CreateSponsorProfileCommandHandler(
                ISponsorProfileRepository sponsorProfileRepository,
                IUserGroupRepository userGroupRepository,
                IGroupRepository groupRepository,
                IUserRepository userRepository,
                ILogger<CreateSponsorProfileCommandHandler> logger)
            {
                _sponsorProfileRepository = sponsorProfileRepository;
                _userGroupRepository = userGroupRepository;
                _groupRepository = groupRepository;
                _userRepository = userRepository;
                _logger = logger;
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

                // Update user's email and password (if provided) to enable business email login
                // This allows sponsors to login with their business credentials
                var user = await _userRepository.GetAsync(u => u.UserId == request.SponsorId);
                if (user != null)
                {
                    _logger.LogInformation("📧 [CreateSponsorProfile] User found - UserId: {UserId}, CurrentEmail: {CurrentEmail}",
                        user.UserId, user.Email);

                    bool needsUpdate = false;

                    // Always update email if provided and different from current (case-insensitive comparison)
                    if (!string.IsNullOrWhiteSpace(request.ContactEmail))
                    {
                        var normalizedNewEmail = request.ContactEmail.Trim().ToLowerInvariant();
                        var normalizedCurrentEmail = user.Email?.Trim().ToLowerInvariant() ?? "";

                        _logger.LogInformation("📧 [CreateSponsorProfile] Email comparison - New: {NewEmail}, Current: {CurrentEmail}",
                            normalizedNewEmail, normalizedCurrentEmail);

                        if (normalizedNewEmail != normalizedCurrentEmail)
                        {
                            // Check if the new email is already in use by another user (case-insensitive)
                            var emailExists = await _userRepository.GetAsync(u =>
                                u.Email.ToLower() == normalizedNewEmail && u.UserId != request.SponsorId);

                            if (emailExists == null)
                            {
                                user.Email = request.ContactEmail.Trim(); // Use original case from request
                                needsUpdate = true;
                                _logger.LogInformation("✅ [CreateSponsorProfile] Email will be updated to: {NewEmail}", user.Email);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ [CreateSponsorProfile] Email {Email} already exists for another user", normalizedNewEmail);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("ℹ️ [CreateSponsorProfile] Email unchanged (same as current)");
                        }
                    }

                    // Always update password if provided (overwrites existing password)
                    if (!string.IsNullOrWhiteSpace(request.Password))
                    {
                        _logger.LogInformation("🔐 [CreateSponsorProfile] Password will be updated");

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
                        _logger.LogInformation("💾 [CreateSponsorProfile] Updating user - Email: {Email}, HasPassword: {HasPassword}",
                            user.Email, user.PasswordHash != null && user.PasswordHash.Length > 0);

                        _userRepository.Update(user);
                        await _userRepository.SaveChangesAsync();

                        _logger.LogInformation("✅ [CreateSponsorProfile] User updated successfully");
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ [CreateSponsorProfile] No user updates needed");
                    }
                }

                // Assign Sponsor role to user (in addition to existing Farmer role)
                var sponsorGroup = await _groupRepository.GetAsync(g => g.GroupName == "Sponsor");
                if (sponsorGroup != null)
                {
                    // Check if user already has Sponsor role (idempotent operation)
                    var existingUserGroup = await _userGroupRepository.GetAsync(
                        ug => ug.UserId == request.SponsorId && ug.GroupId == sponsorGroup.Id);
                    
                    if (existingUserGroup == null)
                    {
                        var userGroup = new UserGroup
                        {
                            UserId = request.SponsorId,
                            GroupId = sponsorGroup.Id
                        };
                        _userGroupRepository.Add(userGroup);
                        await _userGroupRepository.SaveChangesAsync();
                    }
                }

                return new SuccessResult(Messages.SponsorProfileCreated);
            }

        }
    }
}