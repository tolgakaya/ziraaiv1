using System;
using System.Reflection;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Business.Services.PlantAnalysis;
using Business.Services.Configuration;
using Business.Services.FileStorage;
using Business.Services.ImageProcessing;
using Business.Services.MessageQueue;
using Business.Services.Sponsorship;
using Business.Services.Subscription;
using Business.Services.Redemption;
using Business.Services.Notification;
using Business.Services.Admin;
using Business.Services.SponsorRequest;
using Business.Services.MobileIntegration;
using Business.Services.Analytics;
using Business.Services.Payment;
using Business.Services.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Castle.DynamicProxy;
using Core.Utilities.Interceptors;
using Core.Utilities.Security.Jwt;
using Core.CrossCuttingConcerns.Caching;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using MediatR;
using Module = Autofac.Module;

namespace Business.DependencyResolvers
{
    public class AutofacBusinessModule : Module
    {
        private readonly ConfigurationManager _configuration;

        /// <summary>
        /// for Autofac.
        /// </summary>
        public AutofacBusinessModule()
        {
        }

        public AutofacBusinessModule(ConfigurationManager configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder)
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // Register ProjectDbContext with PostgreSQL connection
            builder.Register(c =>
            {
                var config = c.Resolve<IConfiguration>();
                var connectionString = config.GetConnectionString("DArchPgContext");
                
                Console.WriteLine($"[AUTOFAC] ConnectionString from config: {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");
                
                var optionsBuilder = new DbContextOptionsBuilder<ProjectDbContext>();
                optionsBuilder.UseNpgsql(connectionString);
                return new ProjectDbContext(optionsBuilder.Options, config);
            }).As<ProjectDbContext>().InstancePerLifetimeScope();


            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
                .AsClosedTypesOf(typeof(IRequestHandler<,>));

            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
                .AsClosedTypesOf(typeof(IValidator<>));

            builder.RegisterType<PlantAnalysisRepository>().As<IPlantAnalysisRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<ConfigurationRepository>().As<IConfigurationRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<UserRepository>().As<IUserRepository>()
                .InstancePerLifetimeScope();

            // Core repositories
            builder.RegisterType<UserGroupRepository>().As<IUserGroupRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<GroupRepository>().As<IGroupRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<GroupClaimRepository>().As<IGroupClaimRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<UserClaimRepository>().As<IUserClaimRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<OperationClaimRepository>().As<IOperationClaimRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<LanguageRepository>().As<ILanguageRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<TranslateRepository>().As<ITranslateRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<LogRepository>().As<ILogRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<MobileLoginRepository>().As<IMobileLoginRepository>()
                .InstancePerLifetimeScope();

            // Subscription repositories
            builder.RegisterType<SubscriptionTierRepository>().As<ISubscriptionTierRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<UserSubscriptionRepository>().As<IUserSubscriptionRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SubscriptionUsageLogRepository>().As<ISubscriptionUsageLogRepository>()
                .InstancePerLifetimeScope();
            
            // Sponsorship repositories
            builder.RegisterType<SponsorProfileRepository>().As<ISponsorProfileRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SponsorshipCodeRepository>().As<ISponsorshipCodeRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SponsorshipPurchaseRepository>().As<ISponsorshipPurchaseRepository>()
                .InstancePerLifetimeScope();

            // Payment System
            builder.RegisterType<PaymentTransactionRepository>().As<IPaymentTransactionRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<IyzicoPaymentService>().As<IIyzicoPaymentService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<SponsorAnalysisAccessRepository>().As<ISponsorAnalysisAccessRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<AnalysisMessageRepository>().As<IAnalysisMessageRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SmartLinkRepository>().As<ISmartLinkRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<FarmerSponsorBlockRepository>().As<IFarmerSponsorBlockRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<MessagingFeatureRepository>().As<IMessagingFeatureRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<DealerInvitationRepository>().As<IDealerInvitationRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<BulkInvitationJobRepository>().As<IBulkInvitationJobRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<BulkCodeDistributionJobRepository>().As<IBulkCodeDistributionJobRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<BulkSubscriptionAssignmentJobRepository>().As<IBulkSubscriptionAssignmentJobRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<TicketRepository>().As<ITicketRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<TicketMessageRepository>().As<ITicketMessageRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<AppInfoRepository>().As<IAppInfoRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<SmsLogRepository>().As<ISmsLogRepository>()
                .InstancePerLifetimeScope();

            // Tier Feature Management repositories
            builder.RegisterType<FeatureRepository>().As<IFeatureRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<TierFeatureRepository>().As<ITierFeatureRepository>()
                .InstancePerLifetimeScope();

            // Referral System repositories
            builder.RegisterType<ReferralCodeRepository>().As<IReferralCodeRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<ReferralTrackingRepository>().As<IReferralTrackingRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<ReferralRewardRepository>().As<IReferralRewardRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<ReferralConfigurationRepository>().As<IReferralConfigurationRepository>()
                .InstancePerLifetimeScope();
            
            // Deep Links repositories
            builder.RegisterType<DeepLinkRepository>().As<IDeepLinkRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<PlantAnalysisService>().As<IPlantAnalysisService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<ConfigurationService>().As<IConfigurationService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<ImageProcessingService>().As<IImageProcessingService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<PlantAnalysisAsyncService>().As<IPlantAnalysisAsyncService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<PlantAnalysisMultiImageAsyncService>().As<IPlantAnalysisMultiImageAsyncService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<SimpleRabbitMQService>().As<IMessageQueueService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<RabbitMQConsumerService>().As<IRabbitMQConsumerService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SponsorshipService>().As<ISponsorshipService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<BulkDealerInvitationService>().As<IBulkDealerInvitationService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<BulkCodeDistributionService>().As<IBulkCodeDistributionService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<BulkSubscriptionAssignmentService>().As<IBulkSubscriptionAssignmentService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<SponsorshipTierMappingService>().As<ISponsorshipTierMappingService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RedemptionService>().As<IRedemptionService>()
                .InstancePerLifetimeScope();
            
            // ============================================
            // Messaging Services (SMS & WhatsApp)
            // ============================================

            // Register all SMS service implementations first
            builder.RegisterType<Business.Services.Messaging.Fakes.MockSmsService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<Business.Services.Messaging.TurkcellSmsService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<Business.Services.Messaging.NetgsmSmsService>()
                .InstancePerLifetimeScope();

            // Register Mock WhatsApp Service
            builder.RegisterType<Business.Services.Messaging.Fakes.MockWhatsAppService>()
                .As<Business.Services.Messaging.IWhatsAppService>()
                .InstancePerLifetimeScope();

            // Register Real WhatsApp Service (WhatsApp Business API)
            builder.RegisterType<Business.Services.Messaging.WhatsAppBusinessService>()
                .InstancePerLifetimeScope();

            // Register Messaging Service Factory
            builder.RegisterType<Business.Services.Messaging.Factories.MessagingServiceFactory>()
                .As<Business.Services.Messaging.Factories.IMessagingServiceFactory>()
                .InstancePerLifetimeScope();

            // SMS Service - Configuration-driven registration (like FileStorage)
            // Read SmsService:Provider from configuration (supports environment variables)
            builder.Register<Business.Services.Messaging.ISmsService>(c =>
            {
                var context = c.Resolve<IComponentContext>();
                var config = context.Resolve<IConfiguration>();

                // Read provider from configuration (supports environment variables like SmsService__Provider)
                var provider = config["SmsService:Provider"] ?? "Mock";
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
                var envVarDirect = Environment.GetEnvironmentVariable("SmsService__Provider");

                Console.WriteLine($"[SMS DI] ========================================");
                Console.WriteLine($"[SMS DI] Environment: {environment}");
                Console.WriteLine($"[SMS DI] Direct env var (SmsService__Provider): {envVarDirect ?? "NOT SET"}");
                Console.WriteLine($"[SMS DI] Config value (SmsService:Provider): {provider}");
                Console.WriteLine($"[SMS DI] Selected provider (lowercase): {provider.ToLower()}");
                Console.WriteLine($"[SMS DI] ========================================");

                return provider.ToLower() switch
                {
                    "netgsm" => context.Resolve<Business.Services.Messaging.NetgsmSmsService>(),
                    "turkcell" => context.Resolve<Business.Services.Messaging.TurkcellSmsService>(),
                    "mock" => context.Resolve<Business.Services.Messaging.Fakes.MockSmsService>(),
                    _ => context.Resolve<Business.Services.Messaging.Fakes.MockSmsService>() // Default fallback
                };
            }).InstancePerLifetimeScope();

            // Register legacy services for backward compatibility
            builder.RegisterType<Business.Fakes.SmsService.MockSmsService>()
                .As<Business.Adapters.SmsService.ISmsService>()
                .InstancePerLifetimeScope();

            // Notification Services
            builder.RegisterType<NotificationService>().As<INotificationService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<DealerInvitationNotificationService>().As<IDealerInvitationNotificationService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<BulkInvitationNotificationService>().As<IBulkInvitationNotificationService>()
                .InstancePerLifetimeScope();

            // SMS Logging Service
            builder.RegisterType<Business.Services.Logging.SmsLoggingService>()
                .As<Business.Services.Logging.ISmsLoggingService>()
                .InstancePerLifetimeScope();

            // Sponsor Request Services
            builder.RegisterType<SponsorRequestService>().As<ISponsorRequestService>()
                .InstancePerLifetimeScope();
            
            // Mobile Integration Services
            builder.RegisterType<AnalysisMessagingService>().As<IAnalysisMessagingService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<MessageRateLimitService>().As<IMessageRateLimitService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<Business.Services.Messaging.MessagingFeatureService>()
                .As<Business.Services.Messaging.IMessagingFeatureService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<Business.Services.User.AvatarService>()
                .As<Business.Services.User.IAvatarService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<Business.Services.Sponsor.SponsorLogoService>()
                .As<Business.Services.Sponsor.ISponsorLogoService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<Business.Services.Messaging.AttachmentValidationService>()
                .As<Business.Services.Messaging.IAttachmentValidationService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<SmartLinkService>().As<ISmartLinkService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<FarmerProfileVisibilityService>().As<IFarmerProfileVisibilityService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SponsorDataAccessService>().As<ISponsorDataAccessService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<TierFeatureService>().As<ITierFeatureService>()
                .InstancePerLifetimeScope();
            
            // Deep Links services
            builder.RegisterType<Business.Services.MobileIntegration.DeepLinkService>().As<Business.Services.MobileIntegration.IDeepLinkService>()
                .InstancePerLifetimeScope();
            
            // Referral System Services
            builder.RegisterType<Business.Services.Referral.ReferralConfigurationService>()
                .As<Business.Services.Referral.IReferralConfigurationService>()
                .InstancePerLifetimeScope();
            
            // Dealer Invitation Services
            builder.RegisterType<Business.Services.DealerInvitation.DealerInvitationConfigurationService>()
                .As<Business.Services.DealerInvitation.IDealerInvitationConfigurationService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<Business.Services.Referral.ReferralCodeService>()
                .As<Business.Services.Referral.IReferralCodeService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<Business.Services.Referral.ReferralTrackingService>()
                .As<Business.Services.Referral.IReferralTrackingService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<Business.Services.Referral.ReferralRewardService>()
                .As<Business.Services.Referral.IReferralRewardService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<Business.Services.Referral.ReferralLinkService>()
                .As<Business.Services.Referral.IReferralLinkService>()
                .InstancePerLifetimeScope();

            // Authentication Providers
            builder.Register(c => new Business.Services.Authentication.PhoneAuthenticationProvider(
                Core.Entities.Concrete.AuthenticationProviderType.Phone,
                c.Resolve<IUserRepository>(),
                c.Resolve<IMobileLoginRepository>(),
                c.Resolve<ITokenHelper>(),
                c.Resolve<Business.Services.Messaging.ISmsService>(),
                c.Resolve<ILogger<Business.Services.Authentication.PhoneAuthenticationProvider>>(),
                c.Resolve<ICacheManager>()
            )).InstancePerLifetimeScope();
            
            
            // Register all storage implementations first
            builder.RegisterType<LocalFileStorageService>().InstancePerLifetimeScope();
            builder.RegisterType<ImgBBStorageService>().InstancePerLifetimeScope();
            builder.RegisterType<FreeImageHostStorageService>().InstancePerLifetimeScope();
            builder.RegisterType<CloudflareR2StorageService>().InstancePerLifetimeScope();
            // builder.RegisterType<S3FileStorageService>().InstancePerLifetimeScope(); // Requires AWS SDK

            // File Storage Services - Configuration-driven registration
            // Read FileStorage:Provider from configuration (supports environment variables)
            builder.Register<IFileStorageService>(c =>
            {
                var context = c.Resolve<IComponentContext>();
                var config = context.Resolve<IConfiguration>();

                // Read provider from configuration (supports environment variables like FileStorage__Provider)
                var provider = config["FileStorage:Provider"] ?? "Local";

                Console.WriteLine($"[FileStorage DI] Selected provider: {provider}");

                return provider switch
                {
                    "CloudflareR2" => context.Resolve<CloudflareR2StorageService>(),
                    "FreeImageHost" => context.Resolve<FreeImageHostStorageService>(),
                    "ImgBB" => context.Resolve<ImgBBStorageService>(),
                    "Local" => context.Resolve<LocalFileStorageService>(),
                    // "S3" => context.Resolve<S3FileStorageService>(), // Uncomment when S3 is implemented
                    _ => context.Resolve<LocalFileStorageService>() // Default fallback
                };
            }).InstancePerLifetimeScope();

            switch (_configuration.Mode)
            {
                case ApplicationMode.Development:
                    builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
                        .Where(t => t.FullName.StartsWith("Business.Fakes"))
                        ;
                    break;
                case ApplicationMode.Profiling:

                    builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
                        .Where(t => t.FullName.StartsWith("Business.Fakes.SmsService"));
                    break;
                case ApplicationMode.Staging:

                    builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
                        .Where(t => t.FullName.StartsWith("Business.Fakes.SmsService"));
                    break;
                case ApplicationMode.Production:

                    builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
                        .Where(t => t.FullName.StartsWith("Business.Adapters"))
                        ;
                    break;
                default:
                    break;
            }

            // Subscription System Services

            // Cache Services
            builder.RegisterType<CacheInvalidationService>()
                .As<ICacheInvalidationService>()
                .SingleInstance(); // Singleton for cache invalidation coordination

            builder.RegisterType<Business.Services.Sponsorship.DealerDashboardCacheService>()
                .As<Business.Services.Sponsorship.IDealerDashboardCacheService>()
                .InstancePerLifetimeScope(); // Scoped for dealer dashboard caching

            // Analytics Services
            builder.RegisterType<Business.Services.Analytics.SponsorDealerAnalyticsCacheService>()
                .As<Business.Services.Analytics.ISponsorDealerAnalyticsCacheService>()
                .InstancePerLifetimeScope();

            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
                .Where(t => !t.IsAssignableTo<IFileStorageService>() // Exclude file storage services to prevent override
                         && !t.IsAssignableTo<Business.Services.Messaging.ISmsService>() // Exclude SMS services to use configuration-driven registration
                         && !t.IsAssignableTo<IPlantAnalysisAsyncService>() // Exclude plant analysis async service to preserve explicit registration with feature flags
                         && !t.IsAssignableTo<IPlantAnalysisMultiImageAsyncService>()) // Exclude multi-image async service to preserve explicit registration
                .EnableInterfaceInterceptors(new ProxyGenerationOptions()
                {
                    Selector = new AspectInterceptorSelector()
                }).SingleInstance().InstancePerDependency();
        }
    }
}