using System;
using System.Linq;
using System.Threading.Tasks;
using Business.Constants;
using Business.Services.Authentication.Model;
using Business.Services.Messaging;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.Toolkit;
using DataAccess.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Services.Authentication
{
    public abstract class AuthenticationProviderBase : IAuthenticationProvider
    {
        private readonly IMobileLoginRepository _logins;
        private readonly ISmsService _smsService;
        protected readonly ILogger _logger;

        protected AuthenticationProviderBase(IMobileLoginRepository logins, ISmsService smsService, ILogger logger = null)
        {
            _logins = logins;
            _smsService = smsService;
            _logger = logger;
        }

        public virtual async Task<IDataResult<DArchToken>> Verify(VerifyOtpCommand command)
        {
            var externalUserId = command.ExternalUserId;
            var date = DateTime.Now;

            _logger?.LogInformation("[AuthProviderBase:Verify] Searching OTP - ExternalUserId: {ExternalUserId}, Code: {Code}, Provider: {Provider}, CurrentTime: {Time}",
                externalUserId, command.Code, command.Provider, date);

            // Debug: Check all records for this phone
            var allRecords = await _logins.Query()
                .Where(m => m.ExternalUserId == externalUserId && m.Provider == command.Provider)
                .ToListAsync();

            _logger?.LogInformation("[AuthProviderBase:Verify] Found {Count} total OTP records for phone {Phone}",
                allRecords.Count, externalUserId);

            foreach (var record in allRecords)
            {
                var isExpired = record.SendDate.AddMinutes(5) <= date;
                _logger?.LogInformation("[AuthProviderBase:Verify] Record - Code: {Code}, IsUsed: {IsUsed}, SendDate: {SendDate}, Expired: {Expired}",
                    record.Code, record.IsUsed, record.SendDate, isExpired);
            }

            var login = await _logins.GetAsync(m => m.Provider == command.Provider && m.Code == command.Code &&
                                                    m.ExternalUserId == externalUserId &&
                                                    m.SendDate.AddMinutes(5) > date);

            if (login == null)
            {
                _logger?.LogWarning("[AuthProviderBase:Verify] No matching OTP found - returning InvalidCode");
                return new ErrorDataResult<DArchToken>(Messages.InvalidCode);
            }

            _logger?.LogInformation("[AuthProviderBase:Verify] OTP found - proceeding to create token");

            var accessToken = await CreateToken(command);


            if (accessToken.Provider == AuthenticationProviderType.Unknown)
            {
                throw new ArgumentException(Messages.TokenProviderException);
            }

            login.IsUsed = true;
            _logins.Update(login);
            await _logins.SaveChangesAsync();


            return new SuccessDataResult<DArchToken>(accessToken, Messages.SuccessfulLogin);
        }

        public abstract Task<LoginUserResult> Login(LoginUserCommand command);

        public abstract Task<DArchToken> CreateToken(VerifyOtpCommand command);

        protected virtual async Task<LoginUserResult> PrepareOneTimePassword(AuthenticationProviderType providerType, string cellPhone, string externalUserId)
        {
            var currentTime = DateTime.Now;
            var oneTimePassword = await _logins.Query()
                .Where(m => m.Provider == providerType &&
                           m.ExternalUserId == externalUserId &&
                           m.IsUsed == false &&
                           m.SendDate.AddMinutes(5) > currentTime) // Only reuse if not expired
                .Select(m => m.Code)
                .FirstOrDefaultAsync();
            int mobileCode;
            if (oneTimePassword == default)
            {
                mobileCode = RandomPassword.RandomNumberGenerator();
                _logger?.LogInformation("[PrepareOTP] Creating new OTP code {Code} for {Phone}", mobileCode, externalUserId);
                try
                {
                    // Use OTP-specific endpoint for faster delivery (max 3 minutes)
                    var result = await _smsService.SendOtpAsync(cellPhone, mobileCode.ToString());
                    var sendSms = result.Success;

                    _logins.Add(new MobileLogin
                    {
                        Code = mobileCode,
                        IsSend = sendSms,
                        SendDate = DateTime.Now,
                        ExternalUserId = externalUserId,
                        Provider = providerType,
                        IsUsed = false
                    });
                    await _logins.SaveChangesAsync();

                    if (!sendSms)
                    {
                        _logger?.LogWarning("[PrepareOTP] SMS sending failed: {Message}", result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[PrepareOTP] Error sending OTP SMS to {Phone}", cellPhone);
                    return new LoginUserResult
                        { Message = Messages.SmsServiceNotFound, Status = LoginUserResult.LoginStatus.ServiceError };
                }
            }
            else
            {
                mobileCode = oneTimePassword;
                _logger?.LogInformation("[PrepareOTP] Reusing existing valid OTP code {Code} for {Phone}", mobileCode, externalUserId);
            }

            // SECURITY: CRITICAL - Never return OTP code in production/staging
            // ONLY return OTP in local development environment
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var isLocalDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
            
            if (isLocalDevelopment)
            {
                _logger?.LogWarning("[PrepareOTP] ⚠️ DEVELOPMENT MODE: Returning OTP {Code} in response", mobileCode);
                return new LoginUserResult
                    { Message = Messages.SendMobileCode + mobileCode + " (dev mode)", Status = LoginUserResult.LoginStatus.Ok };
            }
            
            // Production/Staging: Generic success message WITHOUT OTP code
            _logger?.LogInformation("[PrepareOTP] ✅ OTP sent to {Phone} successfully (code hidden for security)", externalUserId);
            return new LoginUserResult
                { Message = "OTP sent successfully. Please check your phone.", Status = LoginUserResult.LoginStatus.Ok };
        }
    }
}