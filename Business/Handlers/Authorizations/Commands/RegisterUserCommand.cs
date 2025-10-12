using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Business.Handlers.Authorizations.ValidationRules;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using MediatR;
using System.Text.Json.Serialization;

namespace Business.Handlers.Authorizations.Commands
{
    public class RegisterUserCommand : IRequest<IResult>
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string MobilePhones { get; set; }
        public string ReferralCode { get; set; } // Optional referral code
        
        [JsonPropertyName("role")]
        public string UserRole { get; set; } = "Farmer"; // Default to Farmer, but allow override


        public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IGroupRepository _groupRepository;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;
            private readonly IUserSubscriptionRepository _userSubscriptionRepository;
            private readonly Business.Services.Referral.IReferralTrackingService _referralTrackingService;

            public RegisterUserCommandHandler(
                IUserRepository userRepository, 
                IGroupRepository groupRepository, 
                IUserGroupRepository userGroupRepository,
                ISubscriptionTierRepository subscriptionTierRepository,
                IUserSubscriptionRepository userSubscriptionRepository,
                Business.Services.Referral.IReferralTrackingService referralTrackingService)
            {
                _userRepository = userRepository;
                _groupRepository = groupRepository;
                _userGroupRepository = userGroupRepository;
                _subscriptionTierRepository = subscriptionTierRepository;
                _userSubscriptionRepository = userSubscriptionRepository;
                _referralTrackingService = referralTrackingService;
            }


            [ValidationAspect(typeof(RegisterUserValidator), Priority = 1)]
            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
            {
                Console.WriteLine($"[RegisterUser] 🚀 REGISTER STARTED - Email: {request.Email}, FullName: {request.FullName}");

                // Check if email already exists
                var isThereAnyUser = await _userRepository.GetAsync(u => u.Email == request.Email);

                if (isThereAnyUser != null)
                {
                    Console.WriteLine($"[RegisterUser] ❌ User already exists: {request.Email}");
                    return new ErrorResult(Messages.EmailAlreadyExists);
                }

                // Check if phone number already exists (if provided)
                if (!string.IsNullOrWhiteSpace(request.MobilePhones))
                {
                    var isThereAnyUserWithPhone = await _userRepository.GetAsync(u => u.MobilePhones == request.MobilePhones);
                    if (isThereAnyUserWithPhone != null)
                    {
                        Console.WriteLine($"[RegisterUser] ❌ Phone number already exists: {request.MobilePhones}");
                        return new ErrorResult("Phone number is already registered");
                    }
                }

                Console.WriteLine($"[RegisterUser] ✅ User email is unique, proceeding with registration...");

                HashingHelper.CreatePasswordHash(request.Password, out var passwordSalt, out var passwordHash);
                var now = DateTime.Now; // Use local time for PostgreSQL compatibility
                var user = new User
                {
                    CitizenId = 0, // Default for non-citizen users
                    Email = request.Email,
                    FullName = request.FullName,
                    MobilePhones = request.MobilePhones ?? string.Empty,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Status = true,
                    Address = "Not specified",
                    Notes = "Registered via API",
                    AuthenticationProviderType = "Person",
                    RecordDate = now,
                    UpdateContactDate = now,
                    BirthDate = null, // Explicitly set as null
                    Gender = null, // Explicitly set as null
                    RegistrationReferralCode = request.ReferralCode // Store referral code if provided
                };

                Console.WriteLine($"[RegisterUser] 💾 Adding user to database...");
                try
                {
                    _userRepository.Add(user);
                    await _userRepository.SaveChangesAsync();
                    Console.WriteLine($"[RegisterUser] ✅ User saved to database with ID: {user.UserId}");
                }
                catch (Exception saveEx)
                {
                    Console.WriteLine($"[RegisterUser] ❌ ERROR SAVING USER: {saveEx.Message}");
                    
                    // Log inner exception details
                    var innerEx = saveEx.InnerException;
                    var level = 1;
                    while (innerEx != null)
                    {
                        Console.WriteLine($"[RegisterUser] 🔴 Inner Exception Level {level}: {innerEx.Message}");
                        if (innerEx.Data?.Count > 0)
                        {
                            Console.WriteLine($"[RegisterUser] 📋 Inner Exception {level} Data:");
                            foreach (var key in innerEx.Data.Keys)
                            {
                                Console.WriteLine($"[RegisterUser]   - {key}: {innerEx.Data[key]}");
                            }
                        }
                        
                        // PostgreSQL specific error details
                        if (innerEx.GetType().Name.Contains("Postgres"))
                        {
                            Console.WriteLine($"[RegisterUser] 🐘 PostgreSQL Error Details:");
                            foreach (var prop in innerEx.GetType().GetProperties())
                            {
                                try
                                {
                                    var value = prop.GetValue(innerEx);
                                    if (value != null && !string.IsNullOrEmpty(value.ToString()))
                                    {
                                        Console.WriteLine($"[RegisterUser]   - {prop.Name}: {value}");
                                    }
                                }
                                catch { }
                            }
                        }
                        
                        innerEx = innerEx.InnerException;
                        level++;
                    }
                    
                    Console.WriteLine($"[RegisterUser] 📍 Stack Trace: {saveEx.StackTrace}");
                    throw; // Re-throw to maintain original behavior
                }

                // Always assign Farmer role on registration (regardless of user input)
                // Users can become Sponsors later by creating a sponsor profile
                Console.WriteLine($"[RegisterUser] Assigning Farmer role to user {user.Email}");
                
                var requestedRole = "Farmer"; // Always force Farmer role on registration
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
                    Console.WriteLine($"[RegisterUser] ✅ Role '{requestedRole}' assigned successfully");
                }
                else
                {
                    Console.WriteLine($"[RegisterUser] ❌ Role '{requestedRole}' not found, trying Farmer as fallback");
                    // Fallback to Farmer if requested role doesn't exist
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
                        Console.WriteLine($"[RegisterUser] ✅ Fallback to Farmer role assigned");
                    }
                }

                // Create trial subscription for new users
                try
                {
                    Console.WriteLine($"[RegisterUser] Attempting to create trial subscription for user {user.Email} (ID: {user.UserId})");
                    
                    // Check if UserId is valid
                    if (user.UserId <= 0)
                    {
                        Console.WriteLine($"[RegisterUser] ❌ Invalid UserId: {user.UserId}. User may not have been saved properly.");
                        throw new Exception($"User ID is invalid: {user.UserId}");
                    }
                    
                    Console.WriteLine($"[RegisterUser] ✅ User ID verified: {user.UserId}");
                    
                    var trialTier = await _subscriptionTierRepository.GetAsync(t => t.TierName == "Trial" && t.IsActive);
                    Console.WriteLine($"[RegisterUser] Trial tier search result: {(trialTier != null ? $"Found ID {trialTier.Id}" : "NOT FOUND")}");
                    
                    if (trialTier != null)
                    {
                        Console.WriteLine($"[RegisterUser] Creating trial subscription with tier ID {trialTier.Id}...");
                        // Use DateTime.Now instead of DateTime.UtcNow to avoid timezone issues with PostgreSQL
                        var subscriptionStartDate = DateTime.Now;
                        var trialEnd = subscriptionStartDate.AddDays(30);
                        
                        Console.WriteLine($"[RegisterUser] Creating subscription with dates: Start={subscriptionStartDate:yyyy-MM-dd HH:mm:ss}, End={trialEnd:yyyy-MM-dd HH:mm:ss}");
                        
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
                        
                        Console.WriteLine($"[RegisterUser] Adding trial subscription to repository...");
                        _userSubscriptionRepository.Add(trialSubscription);
                        
                        Console.WriteLine($"[RegisterUser] Saving trial subscription to database...");
                        await _userSubscriptionRepository.SaveChangesAsync();
                        
                        Console.WriteLine($"[RegisterUser] ✅ Trial subscription created successfully for user {user.Email}");
                    }
                    else
                    {
                        Console.WriteLine($"[RegisterUser] ❌ Trial tier not found! Cannot create trial subscription for user {user.Email}");
                    }
                }
                catch (Exception subscriptionEx)
                {
                    // Log subscription creation error but don't fail registration
                    Console.WriteLine($"[RegisterUser] ❌ EXCEPTION: Failed to create trial subscription for user {user.Email}");
                    Console.WriteLine($"[RegisterUser] Exception message: {subscriptionEx.Message}");
                    
                    // Log inner exception details
                    var innerEx = subscriptionEx.InnerException;
                    var level = 1;
                    while (innerEx != null)
                    {
                        Console.WriteLine($"[RegisterUser] Inner Exception {level}: {innerEx.Message}");
                        if (innerEx.Data?.Count > 0)
                        {
                            Console.WriteLine($"[RegisterUser] Inner Exception {level} Data:");
                            foreach (var key in innerEx.Data.Keys)
                            {
                                Console.WriteLine($"[RegisterUser]   {key}: {innerEx.Data[key]}");
                            }
                        }
                        innerEx = innerEx.InnerException;
                        level++;
                    }
                    
                    Console.WriteLine($"[RegisterUser] Stack trace: {subscriptionEx.StackTrace}");
                    // Registration continues without subscription
                }

                // Process referral tracking if referral code was provided
                if (!string.IsNullOrWhiteSpace(request.ReferralCode))
                {
                    try
                    {
                        Console.WriteLine($"[RegisterUser] Processing referral code: {request.ReferralCode} for user {user.Email}");
                        
                        var referralResult = await _referralTrackingService.LinkRegistrationAsync(
                            user.UserId,
                            request.ReferralCode);
                        
                        if (referralResult.Success)
                        {
                            Console.WriteLine($"[RegisterUser] ✅ Referral registration processed successfully");
                        }
                        else
                        {
                            Console.WriteLine($"[RegisterUser] ⚠️ Referral processing failed: {referralResult.Message}");
                        }
                    }
                    catch (Exception refEx)
                    {
                        // Log but don't fail registration
                        Console.WriteLine($"[RegisterUser] ❌ Exception during referral processing: {refEx.Message}");
                    }
                }

                Console.WriteLine($"[RegisterUser] 🎉 REGISTRATION COMPLETED SUCCESSFULLY for {request.Email} (ID: {user.UserId})");
                return new SuccessResult(Messages.Added);
            }
        }
    }
}