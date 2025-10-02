using System;
using System.Threading;
using System.Threading.Tasks;
using Business.Constants;
using Business.Handlers.Authorizations.ValidationRules;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using System.Text.Json.Serialization;

namespace Business.Handlers.Authorizations.Commands
{
    /// <summary>
    /// Phone-based user registration command.
    /// Users register with phone number only (no email or password required).
    /// Authentication is done via SMS OTP.
    /// </summary>
    public class RegisterUserWithPhoneCommand : IRequest<IResult>
    {
        public string MobilePhone { get; set; }
        public string FullName { get; set; }

        [JsonPropertyName("role")]
        public string UserRole { get; set; } = "Farmer"; // Default to Farmer

        public class RegisterUserWithPhoneCommandHandler : IRequestHandler<RegisterUserWithPhoneCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IGroupRepository _groupRepository;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;
            private readonly IUserSubscriptionRepository _userSubscriptionRepository;

            public RegisterUserWithPhoneCommandHandler(
                IUserRepository userRepository,
                IGroupRepository groupRepository,
                IUserGroupRepository userGroupRepository,
                ISubscriptionTierRepository subscriptionTierRepository,
                IUserSubscriptionRepository userSubscriptionRepository)
            {
                _userRepository = userRepository;
                _groupRepository = groupRepository;
                _userGroupRepository = userGroupRepository;
                _subscriptionTierRepository = subscriptionTierRepository;
                _userSubscriptionRepository = userSubscriptionRepository;
            }

            [ValidationAspect(typeof(RegisterUserWithPhoneValidator), Priority = 1)]
            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(RegisterUserWithPhoneCommand request, CancellationToken cancellationToken)
            {
                Console.WriteLine($"[RegisterUserWithPhone] 🚀 REGISTRATION STARTED - Phone: {request.MobilePhone}, FullName: {request.FullName}");

                // Normalize phone number (remove spaces, dashes, etc.)
                var normalizedPhone = NormalizePhoneNumber(request.MobilePhone);
                Console.WriteLine($"[RegisterUserWithPhone] Normalized phone: {normalizedPhone}");

                // Check if phone already exists
                var existingUser = await _userRepository.GetAsync(u => u.MobilePhones == normalizedPhone);
                if (existingUser != null)
                {
                    Console.WriteLine($"[RegisterUserWithPhone] ❌ Phone number already registered: {normalizedPhone}");
                    return new ErrorResult(Messages.PhoneAlreadyExists);
                }

                Console.WriteLine($"[RegisterUserWithPhone] ✅ Phone number is unique, proceeding with registration...");

                var now = DateTime.Now; // Use local time for PostgreSQL compatibility
                var user = new User
                {
                    CitizenId = 0, // Phone-only users don't have CitizenId
                    Email = null, // Phone-only users don't have email
                    FullName = request.FullName,
                    MobilePhones = normalizedPhone,
                    PasswordHash = null, // Phone-only users use OTP authentication (no password)
                    PasswordSalt = null,
                    Status = true,
                    Address = "Not specified",
                    Notes = "Registered via phone (OTP-based authentication)",
                    AuthenticationProviderType = "Phone",
                    RecordDate = now,
                    UpdateContactDate = now,
                    BirthDate = null,
                    Gender = null
                };

                Console.WriteLine($"[RegisterUserWithPhone] 💾 Adding user to database...");
                try
                {
                    _userRepository.Add(user);
                    await _userRepository.SaveChangesAsync();
                    Console.WriteLine($"[RegisterUserWithPhone] ✅ User saved to database with ID: {user.UserId}");
                }
                catch (Exception saveEx)
                {
                    Console.WriteLine($"[RegisterUserWithPhone] ❌ ERROR SAVING USER: {saveEx.Message}");

                    // Log inner exception details
                    var innerEx = saveEx.InnerException;
                    var level = 1;
                    while (innerEx != null)
                    {
                        Console.WriteLine($"[RegisterUserWithPhone] 🔴 Inner Exception Level {level}: {innerEx.Message}");
                        innerEx = innerEx.InnerException;
                        level++;
                    }

                    Console.WriteLine($"[RegisterUserWithPhone] 📍 Stack Trace: {saveEx.StackTrace}");
                    throw; // Re-throw to maintain original behavior
                }

                // Assign user role
                Console.WriteLine($"[RegisterUserWithPhone] Assigning role: {request.UserRole} to user {user.MobilePhones}");

                var requestedRole = request.UserRole ?? "Farmer";
                var userRoleGroup = await _groupRepository.GetAsync(g => g.GroupName == requestedRole);

                if (userRoleGroup != null)
                {
                    var userGroup = new UserGroup
                    {
                        UserId = user.UserId,
                        GroupId = userRoleGroup.Id
                    };
                    _userGroupRepository.Add(userGroup);
                    await _userGroupRepository.SaveChangesAsync();
                    Console.WriteLine($"[RegisterUserWithPhone] ✅ Role '{requestedRole}' assigned successfully");
                }
                else
                {
                    Console.WriteLine($"[RegisterUserWithPhone] ❌ Role '{requestedRole}' not found, trying Farmer as fallback");
                    // Fallback to Farmer
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
                        Console.WriteLine($"[RegisterUserWithPhone] ✅ Fallback to Farmer role assigned");
                    }
                }

                // Create trial subscription for new users
                try
                {
                    Console.WriteLine($"[RegisterUserWithPhone] Attempting to create trial subscription for user {user.MobilePhones} (ID: {user.UserId})");

                    if (user.UserId <= 0)
                    {
                        Console.WriteLine($"[RegisterUserWithPhone] ❌ Invalid UserId: {user.UserId}");
                        throw new Exception($"User ID is invalid: {user.UserId}");
                    }

                    var trialTier = await _subscriptionTierRepository.GetAsync(t => t.TierName == "Trial" && t.IsActive);
                    Console.WriteLine($"[RegisterUserWithPhone] Trial tier search result: {(trialTier != null ? $"Found ID {trialTier.Id}" : "NOT FOUND")}");

                    if (trialTier != null)
                    {
                        var subscriptionStartDate = DateTime.Now;
                        var trialEnd = subscriptionStartDate.AddDays(30);

                        var trialSubscription = new Entities.Concrete.UserSubscription
                        {
                            UserId = user.UserId,
                            SubscriptionTierId = trialTier.Id,
                            StartDate = subscriptionStartDate,
                            EndDate = trialEnd,
                            IsActive = true,
                            AutoRenew = false,
                            PaymentMethod = "Trial",
                            PaidAmount = 0,
                            Currency = "TRY",
                            CurrentDailyUsage = 0,
                            CurrentMonthlyUsage = 0,
                            LastUsageResetDate = subscriptionStartDate,
                            MonthlyUsageResetDate = subscriptionStartDate,
                            Status = "Active",
                            IsTrialSubscription = true,
                            TrialEndDate = trialEnd,
                            CreatedDate = subscriptionStartDate,
                            CreatedUserId = user.UserId
                        };

                        _userSubscriptionRepository.Add(trialSubscription);
                        await _userSubscriptionRepository.SaveChangesAsync();

                        Console.WriteLine($"[RegisterUserWithPhone] ✅ Trial subscription created successfully for user {user.MobilePhones}");
                    }
                    else
                    {
                        Console.WriteLine($"[RegisterUserWithPhone] ❌ Trial tier not found! Cannot create trial subscription");
                    }
                }
                catch (Exception subscriptionEx)
                {
                    Console.WriteLine($"[RegisterUserWithPhone] ❌ EXCEPTION: Failed to create trial subscription");
                    Console.WriteLine($"[RegisterUserWithPhone] Exception message: {subscriptionEx.Message}");
                    // Registration continues without subscription
                }

                Console.WriteLine($"[RegisterUserWithPhone] 🎉 REGISTRATION COMPLETED SUCCESSFULLY for {request.MobilePhone} (ID: {user.UserId})");
                return new SuccessResult(Messages.UserRegisteredSuccessfully);
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
