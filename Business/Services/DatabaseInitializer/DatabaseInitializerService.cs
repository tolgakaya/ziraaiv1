using Business.Seeds;
using Core.DataAccess;
using Core.Entities.Concrete;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.DatabaseInitializer
{
    public class DatabaseInitializerService : IDatabaseInitializerService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRepository<OperationClaim> _operationClaimRepository;
        private readonly IRepository<Group> _groupRepository;
        private readonly IRepository<GroupClaim> _groupClaimRepository;
        private readonly IRepository<UserGroup> _userGroupRepository;
        private readonly IRepository<Configuration> _configurationRepository;
        private readonly IRepository<SubscriptionTier> _subscriptionTierRepository;
        private readonly IRepository<UserSubscription> _userSubscriptionRepository;
        private readonly IRepository<SponsorProfile> _sponsorProfileRepository;
        private readonly ILogger<DatabaseInitializerService> _logger;

        public DatabaseInitializerService(
            IUserRepository userRepository,
            IRepository<OperationClaim> operationClaimRepository,
            IRepository<Group> groupRepository,
            IRepository<GroupClaim> groupClaimRepository,
            IRepository<UserGroup> userGroupRepository,
            IRepository<Configuration> configurationRepository,
            IRepository<SubscriptionTier> subscriptionTierRepository,
            IRepository<UserSubscription> userSubscriptionRepository,
            IRepository<SponsorProfile> sponsorProfileRepository,
            ILogger<DatabaseInitializerService> logger)
        {
            _userRepository = userRepository;
            _operationClaimRepository = operationClaimRepository;
            _groupRepository = groupRepository;
            _groupClaimRepository = groupClaimRepository;
            _userGroupRepository = userGroupRepository;
            _configurationRepository = configurationRepository;
            _subscriptionTierRepository = subscriptionTierRepository;
            _userSubscriptionRepository = userSubscriptionRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Starting database initialization...");

                // Check if seed data already exists
                var dataExists = await CheckIfDataExistsAsync();
                if (!dataExists)
                {
                    await SeedDataAsync();
                    _logger.LogInformation("Database initialization completed successfully.");
                }
                else
                {
                    _logger.LogInformation("Database already contains seed data. Skipping initialization.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database initialization");
                throw;
            }
        }

        public async Task<bool> CheckIfDataExistsAsync()
        {
            try
            {
                // Check if we have any operation claims (most critical for authorization)
                var hasOperationClaims = await _operationClaimRepository.GetListAsync();
                if (!hasOperationClaims.Any())
                {
                    _logger.LogInformation("No operation claims found. Database needs seeding.");
                    return false;
                }

                // Check if we have admin user
                var adminUser = await _userRepository.GetAsync(u => u.Email == "admin@ziraai.com");
                if (adminUser == null)
                {
                    _logger.LogInformation("Admin user not found. Database needs seeding.");
                    return false;
                }

                // Check if we have groups
                var hasGroups = await _groupRepository.GetListAsync();
                if (!hasGroups.Any())
                {
                    _logger.LogInformation("No groups found. Database needs seeding.");
                    return false;
                }

                // Check if we have subscription tiers (these might be seeded via migrations)
                var hasTiers = await _subscriptionTierRepository.GetListAsync();
                if (!hasTiers.Any())
                {
                    _logger.LogInformation("No subscription tiers found. Database needs seeding.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if data exists");
                return false;
            }
        }

        public async Task SeedDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting to seed database...");

                // 1. Seed Operation Claims
                await SeedOperationClaimsAsync();

                // 2. Seed Groups
                await SeedGroupsAsync();

                // 3. Seed Group Claims
                await SeedGroupClaimsAsync();

                // 4. Seed Configuration (if not exists from migration)
                await SeedConfigurationAsync();

                // 5. Seed Subscription Tiers (if not exists from migration)
                await SeedSubscriptionTiersAsync();

                // 6. Seed Users
                await SeedUsersAsync();

                // 7. Seed User Groups
                await SeedUserGroupsAsync();

                // 8. Seed User Subscriptions
                await SeedUserSubscriptionsAsync();

                // 9. Seed Sponsor Profile
                await SeedSponsorProfileAsync();

                _logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database seeding");
                throw;
            }
        }

        private async Task SeedOperationClaimsAsync()
        {
            var existingClaims = await _operationClaimRepository.GetListAsync();
            if (!existingClaims.Any())
            {
                _logger.LogInformation("Seeding operation claims...");
                var claims = OperationClaimSeeds.GetDefaultOperationClaims();
                foreach (var claim in claims)
                {
                    _operationClaimRepository.Add(claim);
                }
                _logger.LogInformation($"Seeded {claims.Count} operation claims.");
            }
        }

        private async Task SeedGroupsAsync()
        {
            var existingGroups = await _groupRepository.GetListAsync();
            if (!existingGroups.Any())
            {
                _logger.LogInformation("Seeding groups...");
                var groups = GroupSeeds.GetDefaultGroups();
                foreach (var group in groups)
                {
                    _groupRepository.Add(group);
                }
                _logger.LogInformation($"Seeded {groups.Count} groups.");
            }
        }

        private async Task SeedGroupClaimsAsync()
        {
            var existingGroupClaims = await _groupClaimRepository.GetListAsync();
            if (!existingGroupClaims.Any())
            {
                _logger.LogInformation("Seeding group claims...");
                var groupClaims = GroupSeeds.GetDefaultGroupClaims();
                foreach (var groupClaim in groupClaims)
                {
                    _groupClaimRepository.Add(groupClaim);
                }
                _logger.LogInformation($"Seeded {groupClaims.Count} group claims.");
            }
        }

        private async Task SeedConfigurationAsync()
        {
            var existingConfigs = await _configurationRepository.GetListAsync();
            if (!existingConfigs.Any())
            {
                _logger.LogInformation("Seeding configuration...");
                var configs = ConfigurationSeeds.GetDefaultConfigurations();
                foreach (var config in configs)
                {
                    _configurationRepository.Add(config);
                }
                _logger.LogInformation($"Seeded {configs.Count} configuration entries.");
            }
        }

        private async Task SeedSubscriptionTiersAsync()
        {
            var existingTiers = await _subscriptionTierRepository.GetListAsync();
            if (!existingTiers.Any())
            {
                _logger.LogInformation("Subscription tiers should be seeded via Entity Framework migrations.");
                // Subscription tiers are already seeded in SubscriptionTierEntityConfiguration
                // This is just a check to ensure they exist
            }
        }

        private async Task SeedUsersAsync()
        {
            var adminUser = await _userRepository.GetAsync(u => u.Email == "admin@ziraai.com");
            if (adminUser == null)
            {
                _logger.LogInformation("Seeding users...");
                var users = UserSeeds.GetDefaultUsers();
                foreach (var user in users)
                {
                    _userRepository.Add(user);
                }
                _logger.LogInformation($"Seeded {users.Count} users.");
                _logger.LogWarning("IMPORTANT: Default admin credentials - Email: admin@ziraai.com, Password: Admin@123! - Please change after first login!");
            }
        }

        private async Task SeedUserGroupsAsync()
        {
            var existingUserGroups = await _userGroupRepository.GetListAsync();
            if (!existingUserGroups.Any())
            {
                _logger.LogInformation("Seeding user groups...");
                var userGroups = UserSeeds.GetDefaultUserGroups();
                foreach (var userGroup in userGroups)
                {
                    _userGroupRepository.Add(userGroup);
                }
                _logger.LogInformation($"Seeded {userGroups.Count} user group assignments.");
            }
        }

        private async Task SeedUserSubscriptionsAsync()
        {
            var existingSubscriptions = await _userSubscriptionRepository.GetListAsync();
            if (!existingSubscriptions.Any())
            {
                _logger.LogInformation("Seeding user subscriptions...");
                var subscriptions = UserSeeds.GetDefaultUserSubscriptions();
                foreach (var subscription in subscriptions)
                {
                    _userSubscriptionRepository.Add(subscription);
                }
                _logger.LogInformation($"Seeded {subscriptions.Count} user subscriptions.");
            }
        }

        private async Task SeedSponsorProfileAsync()
        {
            var existingProfile = await _sponsorProfileRepository.GetAsync(p => p.UserId == 3);
            if (existingProfile == null)
            {
                _logger.LogInformation("Seeding sponsor profile...");
                var profile = UserSeeds.GetDefaultSponsorProfile();
                _sponsorProfileRepository.Add(profile);
                _logger.LogInformation("Seeded demo sponsor profile.");
            }
        }
    }
}