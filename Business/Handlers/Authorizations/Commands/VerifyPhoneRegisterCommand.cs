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
                // Normalize phone number for consistency
                var normalizedPhone = NormalizePhoneNumber(request.MobilePhone);

                _logger.LogInformation("[VerifyPhoneRegister] Verifying OTP for phone: {Phone}", normalizedPhone);

                // Check if phone already registered
                var existingUser = await _userRepository.GetAsync(u => u.MobilePhones == normalizedPhone);
                if (existingUser != null)
                {
                    // ENHANCEMENT: Check if this is a duplicate verify call (idempotent behavior)
                    // Scenario: Mobile app calls verify endpoint twice within seconds (button double-tap, auto-retry, etc.)
                    // Solution: If OTP was used within last 10 seconds, treat as duplicate and return success
                    
                    var recentOtp = await _mobileLoginRepository.GetAsync(
                        m => m.ExternalUserId == normalizedPhone &&
                             m.Code == request.Code &&
                             m.Provider == AuthenticationProviderType.Phone &&
                             m.IsUsed &&  // OTP was already used
                             (DateTime.Now - m.SendDate).TotalSeconds <= 10);  // Within last 10 seconds

                    if (recentOtp != null)
                    {
                        // This is a duplicate verify call with same OTP - return success (idempotent)
                        _logger.LogInformation(
                            "[VerifyPhoneRegister] ♻️ Duplicate verify call detected for phone: {Phone}, OTP: {Code}. Returning success token (idempotent behavior).",
                            normalizedPhone, request.Code);

                        // Generate new JWT token for existing user (same as normal registration flow)
                        var existingUserClaims = await _userRepository.GetClaimsAsync(existingUser.UserId);
                        var existingUserGroups = await _userRepository.GetUserGroupsAsync(existingUser.UserId);
                        var duplicateToken = _tokenHelper.CreateToken<DArchToken>(existingUser, existingUserGroups);
                        duplicateToken.Claims = existingUserClaims.Select(x => x.Name).ToList();

                        // Update RefreshToken (user may not have it if registration interrupted)
                        if (string.IsNullOrEmpty(existingUser.RefreshToken) || existingUser.RefreshTokenExpires < DateTime.Now)
                        {
                            existingUser.RefreshToken = duplicateToken.RefreshToken;
                            existingUser.RefreshTokenExpires = duplicateToken.RefreshTokenExpiration;
                            _userRepository.Update(existingUser);
                            await _userRepository.SaveChangesAsync();
                        }

                        return new SuccessDataResult<DArchToken>(duplicateToken, "Registration successful");
                    }

                    // Different scenario - phone truly already registered (not a duplicate verify call)
                    _logger.LogWarning("[VerifyPhoneRegister] Phone already registered: {Phone}", normalizedPhone);
                    return new ErrorDataResult<DArchToken>("Phone number is already registered");
                }

                // Verify OTP
                _logger.LogInformation("[VerifyPhoneRegister] Looking for OTP - Phone: {NormalizedPhone}, Code: {Code}, Provider: Phone",
                    normalizedPhone, request.Code);

                var mobileLogin = await _mobileLoginRepository.GetAsync(
                    m => m.ExternalUserId == normalizedPhone &&
                         m.Code == request.Code &&
                         m.Provider == AuthenticationProviderType.Phone &&
                         !m.IsUsed);

                if (mobileLogin == null)
                {
                    // Debug: Check what records exist for this phone
                    var allRecords = (await _mobileLoginRepository.GetListAsync(
                        m => m.ExternalUserId == normalizedPhone && m.Provider == AuthenticationProviderType.Phone)).ToList();

                    _logger.LogWarning("[VerifyPhoneRegister] Invalid OTP for phone: {Phone}. Found {Count} total records",
                        normalizedPhone, allRecords.Count);

                    foreach (var record in allRecords)
                    {
                        _logger.LogWarning("[VerifyPhoneRegister] Record - Code: {Code}, IsUsed: {IsUsed}, SendDate: {SendDate}",
                            record.Code, record.IsUsed, record.SendDate);
                    }

                    return new ErrorDataResult<DArchToken>("Invalid or expired OTP code");
                }

                // Check OTP expiration (5 minutes)
                if ((DateTime.Now - mobileLogin.SendDate).TotalMinutes > 5)
                {
                    _logger.LogWarning("[VerifyPhoneRegister] Expired OTP for phone: {Phone}", request.MobilePhone);
                    return new ErrorDataResult<DArchToken>("OTP code has expired");
                }

                // Create new user (do NOT mark OTP as used yet - wait until user creation succeeds)
                var now = DateTime.Now;

                // Use FullName if provided, otherwise generate from phone number
                var fullName = !string.IsNullOrWhiteSpace(request.FullName)
                    ? request.FullName
                    : $"User {normalizedPhone.Substring(normalizedPhone.Length - 4)}"; // e.g., "User 8694"

                var user = new User
                {
                    CitizenId = 0,
                    Email = $"{normalizedPhone}@phone.ziraai.com", // Generate email from normalized phone
                    FullName = fullName,  // Use provided or generated FullName
                    MobilePhones = normalizedPhone,  // Store normalized phone
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

                // IMPORTANT: Save RefreshToken to user for refresh token flow
                user.RefreshToken = token.RefreshToken;
                user.RefreshTokenExpires = token.RefreshTokenExpiration;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                // NOW mark OTP as used (only after everything succeeded)
                mobileLogin.IsUsed = true;
                _mobileLoginRepository.Update(mobileLogin);
                await _mobileLoginRepository.SaveChangesAsync();

                _logger.LogInformation("[VerifyPhoneRegister] Registration completed for phone: {Phone}", normalizedPhone);

                return new SuccessDataResult<DArchToken>(token, "Registration successful");
            }

            /// <summary>
            /// Normalize phone number by removing non-digit characters and ensuring Turkish format
            /// </summary>
            private string NormalizePhoneNumber(string phone)
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return phone;

                // Remove all non-digit characters
                var digitsOnly = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", string.Empty);

                // Handle different formats
                // +905321234567 → 05321234567
                if (digitsOnly.StartsWith("90") && digitsOnly.Length == 12)
                {
                    digitsOnly = "0" + digitsOnly.Substring(2);
                }

                // 5321234567 → 05321234567
                if (!digitsOnly.StartsWith("0") && digitsOnly.Length == 10)
                {
                    digitsOnly = "0" + digitsOnly;
                }

                return digitsOnly;
            }
        }
    }
}
