using System.Linq;
using System.Threading.Tasks;
using Business.Adapters.SmsService;
using Business.Constants;
using Business.Services.Authentication.Model;
using Core.Entities.Concrete;
using Core.Utilities.Security.Jwt;
using DataAccess.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Business.Services.Authentication
{
    /// <summary>
    /// Phone-based authentication provider using SMS OTP
    /// </summary>
    public class PhoneAuthenticationProvider : AuthenticationProviderBase, IAuthenticationProvider
    {
        private readonly IUserRepository _users;
        private readonly ITokenHelper _tokenHelper;

        public PhoneAuthenticationProvider(
            AuthenticationProviderType providerType,
            IUserRepository users,
            IMobileLoginRepository mobileLogins,
            ITokenHelper tokenHelper,
            ISmsService smsService)
            : base(mobileLogins, smsService)
        {
            _users = users;
            ProviderType = providerType;
            _tokenHelper = tokenHelper;
        }

        public AuthenticationProviderType ProviderType { get; }

        public override async Task<Core.Utilities.Results.IDataResult<DArchToken>> Verify(Model.VerifyOtpCommand command)
        {
            // Normalize phone number for consistent lookup
            command.ExternalUserId = NormalizePhoneNumber(command.ExternalUserId);
            return await base.Verify(command);
        }

        public override async Task<LoginUserResult> Login(LoginUserCommand command)
        {
            // Normalize phone number
            var normalizedPhone = NormalizePhoneNumber(command.MobilePhone);

            // Find user by phone
            var user = await _users.Query()
                .Where(u => u.MobilePhones == normalizedPhone)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return new LoginUserResult
                {
                    Message = Messages.UserNotFound,
                    Status = LoginUserResult.LoginStatus.UserNotFound
                };
            }

            // Generate and send OTP
            return await PrepareOneTimePassword(
                AuthenticationProviderType.Phone,
                user.MobilePhones,
                user.MobilePhones); // ExternalUserId is phone for phone-based auth
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

            user.AuthenticationProviderType = ProviderType.ToString();

            var claims = await _users.GetClaimsAsync(user.UserId);
            var userGroups = await _users.GetUserGroupsAsync(user.UserId);
            var accessToken = _tokenHelper.CreateToken<DArchToken>(user, userGroups);
            accessToken.Provider = ProviderType;

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
