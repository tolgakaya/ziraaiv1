#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.Constants;
using Business.Services.Authentication;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.Security.Jwt;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Authorizations.Commands
{
    /// <summary>
    /// Step 2: Verify OTP and complete phone-based registration
    /// </summary>
    public class VerifyPhoneRegisterCommand : IRequest<IDataResult<DArchToken>>
    {
        public string MobilePhone { get; set; } = string.Empty;
        public int Code { get; set; }
        public string? FullName { get; set; }
        public string? ReferralCode { get; set; }

        public class VerifyPhoneRegisterCommandHandler : IRequestHandler<VerifyPhoneRegisterCommand, IDataResult<DArchToken>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IMobileLoginRepository _mobileLoginRepository;
            private readonly IGroupRepository _groupRepository;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;
            private readonly IUserSubscriptionRepository _userSubscriptionRepository;
            private readonly Business.Services.Referral.IReferralTrackingService _referralTrackingService;
            private readonly ITokenHelper _tokenHelper;
            private readonly ILogger<VerifyPhoneRegisterCommandHandler> _logger;

            public VerifyPhoneRegisterCommandHandler(
                IUserRepository userRepository,
                IMobileLoginRepository mobileLoginRepository,
                IGroupRepository groupRepository,
                IUserGroupRepository userGroupRepository,
                ISubscriptionTierRepository subscriptionTierRepository,
                IUserSubscriptionRepository userSubscriptionRepository,
                Business.Services.Referral.IReferralTrackingService referralTrackingService,
                ITokenHelper tokenHelper,
                ILogger<VerifyPhoneRegisterCommandHandler> logger)
            {
                _userRepository = userRepository;
                _mobileLoginRepository = mobileLoginRepository;
                _groupRepository = groupRepository;
                _userGroupRepository = userGroupRepository;
                _subscriptionTierRepository = subscriptionTierRepository;
                _userSubscriptionRepository = userSubscriptionRepository;
                _referralTrackingService = referralTrackingService;
                _tokenHelper = tokenHelper;
                _logger = logger;
            }

            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<DArchToken>> Handle(VerifyPhoneRegisterCommand request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("[VerifyPhoneRegister] Verifying OTP for phone: {Phone}", request.MobilePhone);

                // Check if phone already registered
                var existingUser = await _userRepository.GetAsync(u => u.MobilePhones == request.MobilePhone);
                if (existingUser != null)
                {
                    _logger.LogWarning("[VerifyPhoneRegister] Phone already registered: {Phone}", request.MobilePhone);
                    return new ErrorDataResult<DArchToken>("Phone number is already registered");
                }

                // Verify OTP
                var mobileLogin = await _mobileLoginRepository.GetAsync(
                    m => m.ExternalUserId == request.MobilePhone &&
                         m.Code == request.Code &&
                         m.Provider == AuthenticationProviderType.Phone &&
                         !m.IsUsed);

                if (mobileLogin == null)
                {
                    _logger.LogWarning("[VerifyPhoneRegister] Invalid OTP for phone: {Phone}", request.MobilePhone);
                    return new ErrorDataResult<DArchToken>("Invalid or expired OTP code");
                }

                // Check OTP expiration (5 minutes)
                if ((DateTime.Now - mobileLogin.SendDate).TotalMinutes > 5)
                {
                    _logger.LogWarning("[VerifyPhoneRegister] Expired OTP for phone: {Phone}", request.MobilePhone);
                    return new ErrorDataResult<DArchToken>("OTP code has expired");
                }

                // Mark OTP as used
                mobileLogin.IsUsed = true;
                _mobileLoginRepository.Update(mobileLogin);
                await _mobileLoginRepository.SaveChangesAsync();

                // Create new user
                var now = DateTime.Now;
                var user = new User
                {
                    CitizenId = 0,
                    Email = $"{request.MobilePhone}@phone.ziraai.com", // Generate email from phone
                    FullName = request.FullName,
                    MobilePhones = request.MobilePhone,
                    PasswordHash = new byte[0], // No password for phone auth
                    PasswordSalt = new byte[0],
                    Status = true,
                    Address = "Not specified",
                    Notes = "Registered via phone",
                    AuthenticationProviderType = "Phone",
                    RecordDate = now,
                    UpdateContactDate = now,
                    BirthDate = null,
                    Gender = null,
                    RegistrationReferralCode = request.ReferralCode
                };

                _userRepository.Add(user);
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation("[VerifyPhoneRegister] User created with ID: {UserId}", user.UserId);

                // Assign to Farmer group (default)
                var farmerGroup = await _groupRepository.GetAsync(g => g.GroupName == "Farmer");
                if (farmerGroup != null)
                {
                    var userGroup = new UserGroup
                    {
                        UserId = user.UserId,
                        GroupId = farmerGroup.Id
                    };
                    _userGroupRepository.Add(userGroup);
                    await _userGroupRepository.SaveChangesAsync();
                    _logger.LogInformation("[VerifyPhoneRegister] User assigned to Farmer group");
                }

                // Create trial subscription
                var trialTier = await _subscriptionTierRepository.GetAsync(t => t.TierName == "Trial" && t.IsActive);
                if (trialTier != null)
                {
                    var subscription = new UserSubscription
                    {
                        UserId = user.UserId,
                        SubscriptionTierId = trialTier.Id,
                        StartDate = now,
                        EndDate = now.AddDays(30), // Trial period: 30 days
                        IsActive = true,
                        AutoRenew = false,
                        PaymentMethod = "Trial",
                        PaidAmount = 0,
                        Currency = "TRY",
                        CurrentDailyUsage = 0,
                        CurrentMonthlyUsage = 0,
                        LastUsageResetDate = now,
                        MonthlyUsageResetDate = now,
                        Status = "Active",
                        IsTrialSubscription = true,
                        TrialEndDate = now.AddDays(30),
                        CreatedDate = now,
                        CreatedUserId = user.UserId
                    };
                    _userSubscriptionRepository.Add(subscription);
                    await _userSubscriptionRepository.SaveChangesAsync();
                    _logger.LogInformation("[VerifyPhoneRegister] Trial subscription created");
                }

                // Link referral if provided
                if (!string.IsNullOrWhiteSpace(request.ReferralCode))
                {
                    try
                    {
                        var referralResult = await _referralTrackingService.LinkRegistrationAsync(user.UserId, request.ReferralCode);
                        if (referralResult.Success)
                        {
                            _logger.LogInformation("[VerifyPhoneRegister] Referral linked: {Code}", request.ReferralCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[VerifyPhoneRegister] Referral linking failed");
                    }
                }

                // Generate JWT token
                var claims = await _userRepository.GetClaimsAsync(user.UserId);
                var userGroups = await _userRepository.GetUserGroupsAsync(user.UserId);

                var token = _tokenHelper.CreateToken<DArchToken>(user, userGroups);
                token.Claims = claims.Select(x => x.Name).ToList();

                _logger.LogInformation("[VerifyPhoneRegister] Registration completed for phone: {Phone}", request.MobilePhone);

                return new SuccessDataResult<DArchToken>(token, "Registration successful");
            }
        }
    }
}
