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
                var isThereAnyUser = await _userRepository.GetAsync(u => u.Email == request.Email);

                if (isThereAnyUser != null)
                {
                    return new ErrorResult(Messages.NameAlreadyExist);
                }

                HashingHelper.CreatePasswordHash(request.Password, out var passwordSalt, out var passwordHash);
                var user = new User
                {
                    Email = request.Email,

                    FullName = request.FullName,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Status = true
                };

                _userRepository.Add(user);
                await _userRepository.SaveChangesAsync();

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
                    var trialTier = await _subscriptionTierRepository.GetAsync(t => t.TierName == "Trial" && t.IsActive);
                    if (trialTier != null)
                    {
                        var trialSubscription = new Entities.Concrete.UserSubscription
                        {
                            UserId = user.UserId,
                            SubscriptionTierId = trialTier.Id,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddDays(30), // 30-day trial
                            IsActive = true,
                            AutoRenew = false,
                            PaymentMethod = "Trial",
                            PaidAmount = 0,
                            Currency = "TRY",
                            CurrentDailyUsage = 0,
                            CurrentMonthlyUsage = 0,
                            LastUsageResetDate = DateTime.UtcNow,
                            MonthlyUsageResetDate = DateTime.UtcNow,
                            Status = "Active",
                            IsTrialSubscription = true,
                            TrialEndDate = DateTime.UtcNow.AddDays(30),
                            CreatedDate = DateTime.UtcNow,
                            CreatedUserId = user.UserId
                        };
                        _userSubscriptionRepository.Add(trialSubscription);
                        await _userSubscriptionRepository.SaveChangesAsync();
                    }
                }
                catch (Exception subscriptionEx)
                {
                    // Log subscription creation error but don't fail registration
                    Console.WriteLine($"Failed to create trial subscription for user {user.Email}: {subscriptionEx.Message}");
                    // Registration continues without subscription
                }

                return new SuccessResult(Messages.Added);
            }
        }
    }
}