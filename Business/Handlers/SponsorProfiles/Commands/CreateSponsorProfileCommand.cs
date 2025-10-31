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

            public CreateSponsorProfileCommandHandler(
                ISponsorProfileRepository sponsorProfileRepository,
                IUserGroupRepository userGroupRepository,
                IGroupRepository groupRepository,
                IUserRepository userRepository)
            {
                _sponsorProfileRepository = sponsorProfileRepository;
                _userGroupRepository = userGroupRepository;
                _groupRepository = groupRepository;
                _userRepository = userRepository;
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
                    bool needsUpdate = false;

                    // Always update email if provided and different from current
                    if (!string.IsNullOrWhiteSpace(request.ContactEmail) &&
                        user.Email != request.ContactEmail)
                    {
                        // Check if the new email is already in use by another user
                        var emailExists = await _userRepository.GetAsync(u =>
                            u.Email == request.ContactEmail && u.UserId != request.SponsorId);

                        if (emailExists == null)
                        {
                            user.Email = request.ContactEmail;
                            needsUpdate = true;
                        }
                        // If email already exists, keep the current one (don't fail profile creation)
                    }

                    // Always update password if provided (overwrites existing password)
                    if (!string.IsNullOrWhiteSpace(request.Password))
                    {
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
                        _userRepository.Update(user);
                        await _userRepository.SaveChangesAsync();
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