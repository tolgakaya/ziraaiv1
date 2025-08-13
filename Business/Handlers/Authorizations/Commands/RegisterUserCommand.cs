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

namespace Business.Handlers.Authorizations.Commands
{
    public class RegisterUserCommand : IRequest<IResult>
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }


        public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IGroupRepository _groupRepository;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;
            private readonly IUserSubscriptionRepository _userSubscriptionRepository;

            public RegisterUserCommandHandler(
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


            [ValidationAspect(typeof(RegisterUserValidator), Priority = 1)]
            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
            {
                Console.WriteLine($"[RegisterUser] 🚀 REGISTER STARTED - Email: {request.Email}, FullName: {request.FullName}");
                
                var isThereAnyUser = await _userRepository.GetAsync(u => u.Email == request.Email);

                if (isThereAnyUser != null)
                {
                    Console.WriteLine($"[RegisterUser] ❌ User already exists: {request.Email}");
                    return new ErrorResult(Messages.NameAlreadyExist);
                }

                Console.WriteLine($"[RegisterUser] ✅ User email is unique, proceeding with registration...");

                HashingHelper.CreatePasswordHash(request.Password, out var passwordSalt, out var passwordHash);
                var user = new User
                {
                    Email = request.Email,

                    FullName = request.FullName,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Status = true
                };

                Console.WriteLine($"[RegisterUser] 💾 Adding user to database...");
                _userRepository.Add(user);
                await _userRepository.SaveChangesAsync();
                Console.WriteLine($"[RegisterUser] ✅ User saved to database with ID: {user.UserId}");

                // Automatically assign Farmer role to new users
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
                        var now = DateTime.Now;
                        var trialEnd = now.AddDays(30);
                        
                        Console.WriteLine($"[RegisterUser] Creating subscription with dates: Start={now:yyyy-MM-dd HH:mm:ss}, End={trialEnd:yyyy-MM-dd HH:mm:ss}");
                        
                        var trialSubscription = new Entities.Concrete.UserSubscription
                        {
                            UserId = user.UserId,
                            SubscriptionTierId = trialTier.Id,
                            StartDate = now,
                            EndDate = trialEnd,
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
                            TrialEndDate = trialEnd,
                            CreatedDate = now,
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

                Console.WriteLine($"[RegisterUser] 🎉 REGISTRATION COMPLETED SUCCESSFULLY for {request.Email} (ID: {user.UserId})");
                return new SuccessResult(Messages.Added);
            }
        }
    }
}