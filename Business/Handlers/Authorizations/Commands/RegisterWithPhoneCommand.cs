using System;
using System.Threading;
using System.Threading.Tasks;
using Business.Constants;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Authorizations.Commands
{
    /// <summary>
    /// Step 1: Request OTP for phone-based registration
    /// </summary>
    public class RegisterWithPhoneCommand : IRequest<IResult>
    {
        public string MobilePhone { get; set; }
        public string FullName { get; set; }
        public string ReferralCode { get; set; }

        public class RegisterWithPhoneCommandHandler : IRequestHandler<RegisterWithPhoneCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IMobileLoginRepository _mobileLoginRepository;
            private readonly ILogger<RegisterWithPhoneCommandHandler> _logger;

            public RegisterWithPhoneCommandHandler(
                IUserRepository userRepository,
                IMobileLoginRepository mobileLoginRepository,
                ILogger<RegisterWithPhoneCommandHandler> logger)
            {
                _userRepository = userRepository;
                _mobileLoginRepository = mobileLoginRepository;
                _logger = logger;
            }

            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(RegisterWithPhoneCommand request, CancellationToken cancellationToken)
            {
                // Normalize phone number for consistency
                var normalizedPhone = NormalizePhoneNumber(request.MobilePhone);

                _logger.LogInformation("[RegisterWithPhone] Registration OTP requested for phone: {Phone}", normalizedPhone);

                // Check if phone already registered
                var existingUser = await _userRepository.GetAsync(u => u.MobilePhones == normalizedPhone);
                if (existingUser != null)
                {
                    _logger.LogWarning("[RegisterWithPhone] Phone already registered: {Phone}", normalizedPhone);
                    return new ErrorResult("Phone number is already registered");
                }

                // Generate 6-digit OTP
                var random = new Random();
                var code = random.Next(100000, 999999);

                // Save OTP to MobileLogin table
                var mobileLogin = new MobileLogin
                {
                    ExternalUserId = normalizedPhone,  // Use normalized format
                    Provider = AuthenticationProviderType.Phone,
                    Code = code,
                    IsSend = true,
                    SendDate = DateTime.Now,
                    IsUsed = false
                };

                _mobileLoginRepository.Add(mobileLogin);
                await _mobileLoginRepository.SaveChangesAsync();

                _logger.LogInformation("[RegisterWithPhone] OTP generated and saved for phone: {Phone}, Code: {Code}",
                    normalizedPhone, code);

                // TODO: Send SMS with OTP code via SMS service
                // For now, return OTP in response (development only - remove in production!)
                return new SuccessResult($"OTP sent to {normalizedPhone}. Code: {code} (dev mode)");
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
