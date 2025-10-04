using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.DataAccess;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.Referral
{
    public class ReferralCodeService : IReferralCodeService
    {
        private readonly IReferralCodeRepository _codeRepository;
        private readonly IReferralConfigurationService _configService;
        private readonly ILogger<ReferralCodeService> _logger;

        // Characters without confusing ones (no 0,O,1,I,l)
        private const string AllowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int CodeLength = 6;
        private const int MaxRetries = 10;

        public ReferralCodeService(
            IReferralCodeRepository codeRepository,
            IReferralConfigurationService configService,
            ILogger<ReferralCodeService> logger)
        {
            _codeRepository = codeRepository;
            _configService = configService;
            _logger = logger;
        }

        public async Task<IDataResult<ReferralCode>> GenerateCodeAsync(int userId)
        {
            try
            {
                // Check user's code limit (if configured)
                var maxReferrals = await _configService.GetMaxReferralsPerUserAsync();
                if (maxReferrals > 0)
                {
                    var existingCodes = await _codeRepository.GetByUserIdAsync(userId);
                    if (existingCodes.Count >= maxReferrals)
                    {
                        return new ErrorDataResult<ReferralCode>(
                            $"Maximum referral codes limit reached ({maxReferrals})");
                    }
                }

                // Generate unique code
                var codeString = await GenerateUniqueCodeStringAsync();

                // Get expiry configuration
                var expiryDays = await _configService.GetLinkExpiryDaysAsync();
                var now = DateTime.Now;

                var referralCode = new ReferralCode
                {
                    UserId = userId,
                    Code = codeString,
                    IsActive = true,
                    CreatedAt = now,
                    ExpiresAt = now.AddDays(expiryDays),
                    Status = (int)ReferralCodeStatus.Active
                };

                var addedCode = _codeRepository.Add(referralCode);
                await _codeRepository.SaveChangesAsync();

                _logger.LogInformation("Referral code generated: {Code} for user {UserId}, expires: {ExpiresAt}",
                    codeString, userId, referralCode.ExpiresAt);

                return new SuccessDataResult<ReferralCode>(addedCode, "Referral code generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating referral code for user {UserId}", userId);
                return new ErrorDataResult<ReferralCode>("Failed to generate referral code");
            }
        }

        public async Task<string> GenerateUniqueCodeStringAsync()
        {
            var prefix = await _configService.GetCodePrefixAsync();
            string codeString;
            int attempts = 0;

            do
            {
                if (attempts >= MaxRetries)
                {
                    throw new InvalidOperationException(
                        $"Failed to generate unique code after {MaxRetries} attempts");
                }

                var randomPart = GenerateRandomString(CodeLength);
                codeString = $"{prefix}-{randomPart}";
                attempts++;

            } while (await _codeRepository.GetByCodeAsync(codeString) != null);

            return codeString;
        }

        public async Task<IDataResult<ReferralCode>> GetByCodeAsync(string code)
        {
            try
            {
                var referralCode = await _codeRepository.GetByCodeAsync(code);

                if (referralCode == null)
                    return new ErrorDataResult<ReferralCode>("Referral code not found");

                return new SuccessDataResult<ReferralCode>(referralCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting referral code: {Code}", code);
                return new ErrorDataResult<ReferralCode>("Error retrieving referral code");
            }
        }

        public async Task<IDataResult<ReferralCode>> GetActiveCodeAsync(string code)
        {
            try
            {
                var referralCode = await _codeRepository.GetActiveCodeAsync(code);

                if (referralCode == null)
                    return new ErrorDataResult<ReferralCode>("Referral code not found or expired");

                return new SuccessDataResult<ReferralCode>(referralCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active referral code: {Code}", code);
                return new ErrorDataResult<ReferralCode>("Error retrieving referral code");
            }
        }

        public async Task<IDataResult<List<ReferralCode>>> GetUserCodesAsync(int userId)
        {
            try
            {
                var codes = await _codeRepository.GetByUserIdAsync(userId);
                return new SuccessDataResult<List<ReferralCode>>(codes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user codes for user {UserId}", userId);
                return new ErrorDataResult<List<ReferralCode>>("Error retrieving user codes");
            }
        }

        public async Task<IDataResult<List<ReferralCode>>> GetUserActiveCodesAsync(int userId)
        {
            try
            {
                var codes = await _codeRepository.GetActiveCodesByUserIdAsync(userId);
                return new SuccessDataResult<List<ReferralCode>>(codes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active codes for user {UserId}", userId);
                return new ErrorDataResult<List<ReferralCode>>("Error retrieving active codes");
            }
        }

        public async Task<IResult> ValidateCodeAsync(string code)
        {
            try
            {
                var isValid = await _codeRepository.IsCodeValidAsync(code);

                if (!isValid)
                    return new ErrorResult("Referral code is invalid or expired");

                return new SuccessResult("Referral code is valid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating referral code: {Code}", code);
                return new ErrorResult("Error validating referral code");
            }
        }

        public async Task<IResult> DisableCodeAsync(string code, int userId)
        {
            try
            {
                var referralCode = await _codeRepository.GetByCodeAsync(code);

                if (referralCode == null)
                    return new ErrorResult("Referral code not found");

                // Verify ownership
                if (referralCode.UserId != userId)
                    return new ErrorResult("You can only disable your own referral codes");

                var success = await _codeRepository.DisableCodeAsync(code);

                if (!success)
                    return new ErrorResult("Failed to disable referral code");

                _logger.LogInformation("Referral code disabled: {Code} by user {UserId}", code, userId);

                return new SuccessResult("Referral code disabled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling referral code: {Code}", code);
                return new ErrorResult("Error disabling referral code");
            }
        }

        public async Task<IDataResult<int>> MarkExpiredCodesAsync()
        {
            try
            {
                var count = await _codeRepository.MarkExpiredCodesAsync();

                _logger.LogInformation("Marked {Count} referral codes as expired", count);

                return new SuccessDataResult<int>(count, $"{count} codes marked as expired");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking expired codes");
                return new ErrorDataResult<int>(0, "Error marking expired codes");
            }
        }

        #region Private Helper Methods

        private string GenerateRandomString(int length)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var data = new byte[length];
                rng.GetBytes(data);

                var result = new StringBuilder(length);
                for (int i = 0; i < length; i++)
                {
                    result.Append(AllowedChars[data[i] % AllowedChars.Length]);
                }

                return result.ToString();
            }
        }

        #endregion
    }
}
