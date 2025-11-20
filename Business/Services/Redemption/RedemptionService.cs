using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Business.Constants;
using Business.Services.Analytics;
using Business.Services.Authentication;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using Core.Utilities.Security.Jwt;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;
using UserEntity = Core.Entities.Concrete.User;
using UserGroupEntity = Core.Entities.Concrete.UserGroup;

namespace Business.Services.Redemption
{
    public class RedemptionService : IRedemptionService
    {
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserSubscriptionRepository _subscriptionRepository;
        private readonly ISubscriptionTierRepository _tierRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly ITokenHelper _tokenHelper;
        private readonly IConfiguration _configuration;
        private readonly ISponsorDealerAnalyticsCacheService _analyticsCache;
        private readonly ILogger<RedemptionService> _logger;

        public RedemptionService(
            ISponsorshipCodeRepository codeRepository,
            IUserRepository userRepository,
            IUserSubscriptionRepository subscriptionRepository,
            ISubscriptionTierRepository tierRepository,
            IGroupRepository groupRepository,
            IUserGroupRepository userGroupRepository,
            ITokenHelper tokenHelper,
            IConfiguration configuration,
            ISponsorDealerAnalyticsCacheService analyticsCache,
            ILogger<RedemptionService> logger)
        {
            _codeRepository = codeRepository;
            _userRepository = userRepository;
            _subscriptionRepository = subscriptionRepository;
            _tierRepository = tierRepository;
            _groupRepository = groupRepository;
            _userGroupRepository = userGroupRepository;
            _tokenHelper = tokenHelper;
            _configuration = configuration;
            _analyticsCache = analyticsCache;
            _logger = logger;
        }

        public async Task TrackLinkClickAsync(string code, string ipAddress)
        {
            try
            {
                var sponsorshipCode = await _codeRepository.GetAsync(c => c.Code == code);
                if (sponsorshipCode != null)
                {
                    // Update click tracking information
                    if (!sponsorshipCode.LinkClickDate.HasValue)
                    {
                        sponsorshipCode.LinkClickDate = DateTime.Now;
                    }
                    sponsorshipCode.LinkClickCount++;
                    sponsorshipCode.LastClickIpAddress = ipAddress;

                    _codeRepository.Update(sponsorshipCode);
                    await _codeRepository.SaveChangesAsync();

                    _logger.LogInformation("Link click tracked for code {Code}. Total clicks: {Clicks}", 
                        code, sponsorshipCode.LinkClickCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking link click for code {Code}", code);
            }
        }

        public async Task<IResult> ValidateCodeAsync(string code)
        {
            try
            {
                // Get the sponsorship code with related data
                var sponsorshipCode = await _codeRepository.GetAsync(c => c.Code == code);
                
                if (sponsorshipCode == null)
                {
                    return new ErrorResult("Geçersiz aktivasyon kodu.");
                }

                _logger.LogInformation("Code found: {Code}, SponsorId: {SponsorId}, Used: {Used}, Active: {Active}", 
                    code, sponsorshipCode.SponsorId, sponsorshipCode.IsUsed, sponsorshipCode.IsActive);

                // Check if code is already used
                if (sponsorshipCode.IsUsed)
                {
                    return new ErrorResult("Bu kod daha önce kullanılmış.");
                }

                // Check if code is active
                if (!sponsorshipCode.IsActive)
                {
                    return new ErrorResult("Bu kod devre dışı bırakılmış.");
                }

                // Check expiry date
                if (sponsorshipCode.ExpiryDate < DateTime.Now)
                {
                    return new ErrorResult($"Bu kodun süresi {sponsorshipCode.ExpiryDate:dd.MM.yyyy} tarihinde dolmuş.");
                }

                return new SuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating code {Code}", code);
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " Inner: " + ex.InnerException.Message;
                }
                return new ErrorResult($"Kod doğrulama sırasında bir hata oluştu: {errorMessage}");
            }
        }

        public async Task<IResult> ValidateCodeWithUserAsync(string code, HttpContext httpContext)
        {
            try
            {
                // First, run standard code validation
                var basicValidation = await ValidateCodeAsync(code);
                if (!basicValidation.Success)
                {
                    return basicValidation;
                }

                // Get the sponsorship code with related data
                var sponsorshipCode = await _codeRepository.GetAsync(c => c.Code == code);
                if (sponsorshipCode == null)
                {
                    return new ErrorResult("Geçersiz aktivasyon kodu.");
                }

                // Check if the current user is authenticated and is the sponsor of this code
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int currentUserId))
                    {
                        _logger.LogInformation("Authenticated user {UserId} attempting to redeem code {Code} created by sponsor {SponsorId}", 
                            currentUserId, code, sponsorshipCode.SponsorId);

                        // Check if the current user is the sponsor who created this code
                        if (currentUserId == sponsorshipCode.SponsorId)
                        {
                            _logger.LogWarning("Sponsor {SponsorId} attempting to redeem their own code {Code}", 
                                currentUserId, code);
                            
                            return new ErrorResult("Kendi oluşturduğunuz kodu kullanamazsınız. Bu kod çiftçiler için tasarlanmıştır. Lütfen kodu hedeflenen çiftçiye gönderin.");
                        }
                    }
                }

                return new SuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating code {Code} with user context", code);
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " Inner: " + ex.InnerException.Message;
                }
                return new ErrorResult($"Kod doğrulama sırasında bir hata oluştu: {errorMessage}");
            }
        }

        public async Task<UserEntity> FindUserByCodeAsync(string code)
        {
            try
            {
                var sponsorshipCode = await _codeRepository.GetAsync(c => c.Code == code);
                if (sponsorshipCode == null || string.IsNullOrEmpty(sponsorshipCode.RecipientPhone))
                {
                    return null;
                }

                // Try to find user by phone number
                var user = await _userRepository.GetAsync(u => 
                    u.MobilePhones == sponsorshipCode.RecipientPhone && u.Status);
                
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding user by code {Code}", code);
                return null;
            }
        }

        public async Task<IDataResult<UserEntity>> CreateAccountFromCodeAsync(string code)
        {
            try
            {
                // Get code details including recipient info
                var sponsorshipCode = await _codeRepository.GetAsync(c => c.Code == code);
                if (sponsorshipCode == null)
                {
                    return new ErrorDataResult<UserEntity>("Geçersiz kod");
                }

                // Extract phone from code or use stored recipient phone
                var phone = sponsorshipCode.RecipientPhone;
                var name = sponsorshipCode.RecipientName ?? "Değerli Çiftçi";

                // Check if phone number is provided
                if (string.IsNullOrEmpty(phone))
                {
                    // Generate a temporary phone number for the user
                    phone = $"+90{GenerateRandomPhoneNumber()}";
                    _logger.LogWarning("No phone number in code {Code}, generated temporary: {Phone}", 
                        code, phone);
                }

                // Check if user already exists with this phone
                var existingUser = await _userRepository.GetAsync(u => u.MobilePhones == phone);
                if (existingUser != null)
                {
                    _logger.LogInformation("User already exists with phone {Phone}", phone);
                    return new SuccessDataResult<UserEntity>(existingUser);
                }

                // Generate unique email and password
                var emailBase = phone.Replace("+", "").Replace(" ", "").Replace("-", "");
                var email = $"{emailBase}@ziraai.com";
                
                // Check if email already exists and make it unique if needed
                var emailExists = await _userRepository.GetAsync(u => u.Email == email);
                if (emailExists != null)
                {
                    email = $"{emailBase}_{DateTime.Now.Ticks}@ziraai.com";
                }

                var tempPassword = GenerateSecurePassword();

                // Create password hash
                HashingHelper.CreatePasswordHash(tempPassword, out var passwordSalt, out var passwordHash);

                // Create new farmer account
                var newUser = new UserEntity
                {
                    FullName = name,
                    MobilePhones = phone,
                    Email = email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Status = true,
                    RecordDate = DateTime.Now,
                    UpdateContactDate = DateTime.Now
                };

                _userRepository.Add(newUser);
                await _userRepository.SaveChangesAsync();

                // Assign Farmer role
                var farmerGroup = await _groupRepository.GetAsync(g => g.GroupName == "Farmer");
                if (farmerGroup != null)
                {
                    var userGroup = new UserGroupEntity
                    {
                        UserId = newUser.UserId,
                        GroupId = farmerGroup.Id
                    };
                    _userGroupRepository.Add(userGroup);
                    await _userGroupRepository.SaveChangesAsync();
                }

                _logger.LogInformation("Auto-created account for phone {Phone} via sponsorship code {Code}", 
                    phone, code);

                // Store the temporary password info (in production, send via SMS)
                newUser.Notes = $"Temp password: {tempPassword}"; // This should be sent via SMS in production

                return new SuccessDataResult<UserEntity>(newUser, "Hesap otomatik oluşturuldu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account from code {Code}", code);
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " Inner: " + ex.InnerException.Message;
                }
                return new ErrorDataResult<UserEntity>($"Hesap oluşturulurken hata oluştu: {errorMessage}");
            }
        }

        public async Task<IDataResult<UserSubscription>> ActivateSubscriptionAsync(string code, int userId)
        {
            try
            {
                // Get the sponsorship code with related data
                var sponsorshipCode = await _codeRepository.GetAsync(c => c.Code == code);
                
                if (sponsorshipCode == null)
                {
                    return new ErrorDataResult<UserSubscription>("Geçersiz kod");
                }

                // Double-check the code hasn't been used (race condition prevention)
                if (sponsorshipCode.IsUsed)
                {
                    return new ErrorDataResult<UserSubscription>("Bu kod zaten kullanılmış");
                }

                // Get subscription tier details
                var tier = await _tierRepository.GetAsync(t => t.Id == sponsorshipCode.SubscriptionTierId);
                if (tier == null)
                {
                    return new ErrorDataResult<UserSubscription>("Abonelik paketi bulunamadı");
                }

                // Check if user already has an active subscription
                var existingSubscription = await _subscriptionRepository.GetAsync(s => 
                    s.UserId == userId && s.IsActive && s.EndDate > DateTime.Now);
                
                if (existingSubscription != null)
                {
                    return new ErrorDataResult<UserSubscription>(
                        "Zaten aktif bir aboneliğiniz var. Mevcut aboneliğiniz sona erdikten sonra yeni kod kullanabilirsiniz.");
                }

                // Create new subscription
                var now = DateTime.Now;
                var subscription = new UserSubscription
                {
                    UserId = userId,
                    SubscriptionTierId = sponsorshipCode.SubscriptionTierId,
                    StartDate = now,
                    EndDate = now.AddDays(30), // Standard 30-day subscription from sponsorship
                    IsActive = true,
                    AutoRenew = false,
                    PaymentMethod = "Sponsorship",
                    PaymentReference = $"SPONSOR-{sponsorshipCode.Code}",
                    PaidAmount = 0, // Sponsored, so no payment
                    Currency = "TRY",
                    CurrentDailyUsage = 0,
                    CurrentMonthlyUsage = 0,
                    LastUsageResetDate = now,
                    MonthlyUsageResetDate = now,
                    Status = "Active",
                    IsTrialSubscription = false,
                    CreatedDate = now,
                    CreatedUserId = userId
                };

                _subscriptionRepository.Add(subscription);
                await _subscriptionRepository.SaveChangesAsync();

                // Mark the code as used
                sponsorshipCode.IsUsed = true;
                sponsorshipCode.UsedByUserId = userId;
                sponsorshipCode.UsedDate = now;
                sponsorshipCode.CreatedSubscriptionId = subscription.Id;

                _codeRepository.Update(sponsorshipCode);
                await _codeRepository.SaveChangesAsync();

                // Update analytics cache - code redeemed by farmer
                if (sponsorshipCode.SponsorId > 0 && sponsorshipCode.DealerId.HasValue)
                {
                    await _analyticsCache.OnCodeRedeemedAsync(sponsorshipCode.SponsorId, sponsorshipCode.DealerId.Value);
                }

                // Load tier info for response
                subscription.SubscriptionTier = tier;

                _logger.LogInformation("Subscription activated for user {UserId} with code {Code}", 
                    userId, code);

                return new SuccessDataResult<UserSubscription>(subscription, 
                    $"{tier.DisplayName} aboneliğiniz başarıyla aktive edildi!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating subscription with code {Code} for user {UserId}", 
                    code, userId);
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " Inner: " + ex.InnerException.Message;
                }
                return new ErrorDataResult<UserSubscription>($"Abonelik aktivasyonu sırasında hata oluştu: {errorMessage}");
            }
        }

        public async Task<string> GenerateAutoLoginTokenAsync(int userId)
        {
            try
            {
                // Get user with claims
                var user = await _userRepository.GetAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return null;
                }

                // Get user's groups/claims for token
                var userGroups = await _userGroupRepository.GetListAsync(ug => ug.UserId == userId);
                var groupIds = userGroups.Select(ug => ug.GroupId).ToList();
                
                var operationClaims = await _groupRepository.GetListAsync(g => groupIds.Contains(g.Id));
                var groupNames = operationClaims.Select(g => g.GroupName).ToList();

                // Generate token
                var accessToken = _tokenHelper.CreateToken<Core.Utilities.Security.Jwt.AccessToken>(user, groupNames);
                
                return accessToken.Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating auto-login token for user {UserId}", userId);
                return null;
            }
        }

        public async Task<string> GenerateRedemptionLinkAsync(string code)
        {
            try
            {
                var baseUrl = _configuration["RedemptionSettings:BaseUrl"] ?? "https://localhost:5001";
                var redemptionLink = $"{baseUrl}/redeem/{code}";

                // Update the code with the generated link
                var sponsorshipCode = await _codeRepository.GetAsync(c => c.Code == code);
                if (sponsorshipCode != null && string.IsNullOrEmpty(sponsorshipCode.RedemptionLink))
                {
                    sponsorshipCode.RedemptionLink = redemptionLink;
                    _codeRepository.Update(sponsorshipCode);
                    await _codeRepository.SaveChangesAsync();
                }

                return redemptionLink;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating redemption link for code {Code}", code);
                throw;
            }
        }

        public async Task<IResult> SendSponsorshipLinkAsync(
            string code, 
            string recipientPhone, 
            string recipientName,
            string channel = "SMS")
        {
            try
            {
                // Get sponsorship code details
                var sponsorshipCode = await _codeRepository.GetAsync(c => c.Code == code);
                
                if (sponsorshipCode == null)
                {
                    return new ErrorResult("Kod bulunamadı");
                }

                // Generate redemption link if not exists
                if (string.IsNullOrEmpty(sponsorshipCode.RedemptionLink))
                {
                    sponsorshipCode.RedemptionLink = await GenerateRedemptionLinkAsync(code);
                }

                // Update recipient information
                sponsorshipCode.RecipientPhone = recipientPhone;
                sponsorshipCode.RecipientName = recipientName;
                sponsorshipCode.LinkSentDate = DateTime.Now;
                sponsorshipCode.LinkSentVia = channel;

                // Build message
                var tier = await _tierRepository.GetAsync(t => t.Id == sponsorshipCode.SubscriptionTierId);
                var sponsor = await _userRepository.GetAsync(u => u.UserId == sponsorshipCode.SponsorId);
                var message = BuildSponsorshipMessage(
                    recipientName,
                    sponsor?.FullName ?? "ZiraAI",
                    tier?.DisplayName ?? "Premium",
                    sponsorshipCode.RedemptionLink,
                    sponsorshipCode.ExpiryDate);

                // Send message (in production, integrate with SMS/WhatsApp service)
                bool sentSuccessfully = await SendMessageAsync(recipientPhone, message, channel);
                
                if (sentSuccessfully)
                {
                    sponsorshipCode.LinkDelivered = true;
                    sponsorshipCode.DistributionChannel = channel;
                    sponsorshipCode.DistributionDate = DateTime.Now;
                    sponsorshipCode.DistributedTo = $"{recipientName} ({recipientPhone})";
                }

                _codeRepository.Update(sponsorshipCode);
                await _codeRepository.SaveChangesAsync();

                return sentSuccessfully 
                    ? new SuccessResult($"Link başarıyla {channel} ile gönderildi")
                    : new ErrorResult("Link gönderilemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending sponsorship link for code {Code}", code);
                return new ErrorResult("Link gönderimi sırasında hata oluştu");
            }
        }

        public async Task<IDataResult<BulkSendResult>> SendBulkSponsorshipLinksAsync(BulkSendRequest request)
        {
            var result = new BulkSendResult
            {
                Results = new SendResult[request.Recipients.Length]
            };

            for (int i = 0; i < request.Recipients.Length; i++)
            {
                var recipient = request.Recipients[i];
                var sendResult = new SendResult
                {
                    Code = recipient.Code,
                    Phone = recipient.Phone
                };

                try
                {
                    var sendResponse = await SendSponsorshipLinkAsync(
                        recipient.Code,
                        recipient.Phone,
                        recipient.Name,
                        request.Channel);

                    sendResult.Success = sendResponse.Success;
                    sendResult.ErrorMessage = sendResponse.Success ? null : sendResponse.Message;
                    sendResult.DeliveryStatus = sendResponse.Success ? "Delivered" : "Failed";

                    if (sendResponse.Success)
                    {
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailureCount++;
                    }
                }
                catch (Exception ex)
                {
                    sendResult.Success = false;
                    sendResult.ErrorMessage = ex.Message;
                    sendResult.DeliveryStatus = "Error";
                    result.FailureCount++;
                }

                result.Results[i] = sendResult;
            }

            result.TotalSent = request.Recipients.Length;

            return new SuccessDataResult<BulkSendResult>(result, 
                $"{result.SuccessCount} link başarıyla gönderildi");
        }

        private string GenerateSecurePassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerateRandomPhoneNumber()
        {
            var random = new Random();
            return $"5{random.Next(0, 10)}{random.Next(0, 10)}{random.Next(1000000, 9999999)}";
        }

        private string BuildSponsorshipMessage(
            string recipientName,
            string sponsorName,
            string tierName,
            string redemptionLink,
            DateTime expiryDate)
        {
            return $@"🎁 Merhaba {recipientName}!

{sponsorName} size {tierName} abonelik paketi hediye etti!

📱 Hemen aktivasyon yapın:
{redemptionLink}

⏰ Son kullanım: {expiryDate:dd.MM.yyyy}
🌱 ZiraAI ile tarımınızı dijitalleştirin!

ZiraAI - Akıllı Tarım Çözümleri";
        }

        private async Task<bool> SendMessageAsync(string phone, string message, string channel)
        {
            // MOCK IMPLEMENTATION - No real SMS/WhatsApp integration yet
            _logger.LogInformation("📱 MOCK {Channel} sent to {Phone}", channel, phone);
            _logger.LogInformation("📝 Message content: {Message}", message);
            
            // Simulate network delay
            await Task.Delay(200);
            
            // Mock success response
            _logger.LogInformation("✅ MOCK delivery successful to {Phone} via {Channel}", phone, channel);
            return true; // Always return success in mock mode
        }
    }
}