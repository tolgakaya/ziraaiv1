using System.Linq;
using System.Threading.Tasks;
using Business.Adapters.SmsService;
using Business.Constants;
using Business.Services.Authentication.Model;
using Core.Entities.Concrete;
using Core.Utilities.Security.Jwt;
using Core.CrossCuttingConcerns.Caching;
using DataAccess.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Services.Authentication
{
    /// <summary>
    /// Phone-based authentication provider using SMS OTP
    /// </summary>
    public class PhoneAuthenticationProvider : AuthenticationProviderBase, IAuthenticationProvider
    {
        private readonly IUserRepository _users;
        private readonly ITokenHelper _tokenHelper;
        private readonly ILogger<PhoneAuthenticationProvider> _logger;
        private readonly ICacheManager _cacheManager;

        public PhoneAuthenticationProvider(
            AuthenticationProviderType providerType,
            IUserRepository users,
            IMobileLoginRepository mobileLogins,
            ITokenHelper tokenHelper,
            ISmsService smsService,
            ILogger<PhoneAuthenticationProvider> logger,
            ICacheManager cacheManager)
            : base(mobileLogins, smsService, logger)
        {
            _users = users;
            ProviderType = providerType;
            _tokenHelper = tokenHelper;
            _logger = logger;
            _cacheManager = cacheManager;
        }

        public AuthenticationProviderType ProviderType { get; }

        public override async Task<Core.Utilities.Results.IDataResult<DArchToken>> Verify(Model.VerifyOtpCommand command)
        {
            _logger.LogInformation("[PhoneAuth:Verify] Starting OTP verification - Input phone: {Phone}, Code: {Code}",
                command.ExternalUserId, command.Code);

            // Normalize phone number for consistent lookup
            var originalPhone = command.ExternalUserId;
            command.ExternalUserId = NormalizePhoneNumber(command.ExternalUserId);

            _logger.LogInformation("[PhoneAuth:Verify] Phone normalized - Original: {Original}, Normalized: {Normalized}",
                originalPhone, command.ExternalUserId);

            var result = await base.Verify(command);

            _logger.LogInformation("[PhoneAuth:Verify] Verification result - Success: {Success}, Message: {Message}",
                result.Success, result.Message);

            return result;
        }

        public override async Task<LoginUserResult> Login(LoginUserCommand command)
        {
            _logger.LogInformation("[PhoneAuth:Login] Starting phone login - Input phone: {Phone}", command.MobilePhone);

            // Normalize phone number
            var normalizedPhone = NormalizePhoneNumber(command.MobilePhone);
            _logger.LogInformation("[PhoneAuth:Login] Phone normalized - Original: {Original}, Normalized: {Normalized}",
                command.MobilePhone, normalizedPhone);

            // Find user by phone
            var user = await _users.Query()
                .Where(u => u.MobilePhones == normalizedPhone)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("[PhoneAuth:Login] User not found for phone: {Phone}", normalizedPhone);
                return new LoginUserResult
                {
                    Message = Messages.UserNotFound,
                    Status = LoginUserResult.LoginStatus.UserNotFound
                };
            }

            _logger.LogInformation("[PhoneAuth:Login] User found - UserId: {UserId}, Phone in DB: {DbPhone}",
                user.UserId, user.MobilePhones);

            // Generate and send OTP
            // IMPORTANT: Use normalized phone for both SMS and ExternalUserId to ensure consistency
            var result = await PrepareOneTimePassword(
                AuthenticationProviderType.Phone,
                normalizedPhone,  // SMS will be sent to normalized format
                normalizedPhone); // ExternalUserId stored as normalized format for verify lookup

            _logger.LogInformation("[PhoneAuth:Login] OTP preparation result - Status: {Status}, Message: {Message}",
                result.Status, result.Message);

            return result;
        }

        public override async Task<DArchToken> CreateToken(VerifyOtpCommand command)
        {
            // Find user by phone (ExternalUserId is phone number for phone-based auth)
            var normalizedPhone = NormalizePhoneNumber(command.ExternalUserId);
            var user = await _users.GetAsync(u => u.MobilePhones == normalizedPhone);

            if (user == null)
            {
                throw new System.Exception(Messages.UserNotFound);
            }
            
            // SECURITY: Check if user is deactivated by admin
            if (!user.IsActive)
            {
                throw new System.Exception(Messages.UserDeactivated);
            }

            user.AuthenticationProviderType = ProviderType.ToString();

            var claims = await _users.GetClaimsAsync(user.UserId);
            var userGroups = await _users.GetUserGroupsAsync(user.UserId);
            
            // DEBUG: Log claims for troubleshooting
            _logger.LogInformation($"[PhoneAuth:Claims] User {user.UserId} has {claims.Count} claims: {string.Join(", ", claims.Take(10).Select(x => x.Name))}");
            
            var accessToken = _tokenHelper.CreateToken<DArchToken>(user, userGroups);
            accessToken.Provider = ProviderType;

            // Add user claims to cache for authorization checks
            var claimNames = claims.Select(x => x.Name).ToList();
            _cacheManager.Add($"{CacheKeys.UserIdForClaim}={user.UserId}", claimNames);
            
            _logger.LogInformation($"[PhoneAuth:Cache] Added {claimNames.Count} claims to cache for user {user.UserId}");

            return accessToken;
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
