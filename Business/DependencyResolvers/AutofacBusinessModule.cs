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
using Business.Services.Redemption;
using Business.Services.Notification;
using Business.Services.SponsorRequest;
using Business.Services.MobileIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Castle.DynamicProxy;
using Core.Utilities.Interceptors;
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
            
            builder.RegisterType<SponsorAnalysisAccessRepository>().As<ISponsorAnalysisAccessRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<AnalysisMessageRepository>().As<IAnalysisMessageRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SmartLinkRepository>().As<ISmartLinkRepository>()
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
            
            builder.RegisterType<SimpleRabbitMQService>().As<IMessageQueueService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<RabbitMQConsumerService>().As<IRabbitMQConsumerService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SponsorshipService>().As<ISponsorshipService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<RedemptionService>().As<IRedemptionService>()
                .InstancePerLifetimeScope();
            
            // Notification Services
            builder.RegisterType<WhatsAppService>().As<IWhatsAppService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<NotificationService>().As<INotificationService>()
                .InstancePerLifetimeScope();
            
            // Sponsor Request Services
            builder.RegisterType<SponsorRequestService>().As<ISponsorRequestService>()
                .InstancePerLifetimeScope();
            
            // Mobile Integration Services
            builder.RegisterType<AnalysisMessagingService>().As<IAnalysisMessagingService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SmartLinkService>().As<ISmartLinkService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<FarmerProfileVisibilityService>().As<IFarmerProfileVisibilityService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SponsorDataAccessService>().As<ISponsorDataAccessService>()
                .InstancePerLifetimeScope();
            
            // Deep Links services
            builder.RegisterType<Business.Services.MobileIntegration.DeepLinkService>().As<Business.Services.MobileIntegration.IDeepLinkService>()
                .InstancePerLifetimeScope();
            
            
            // Register all storage implementations first
            builder.RegisterType<LocalFileStorageService>().InstancePerLifetimeScope();
            builder.RegisterType<ImgBBStorageService>().InstancePerLifetimeScope();
            builder.RegisterType<FreeImageHostStorageService>().InstancePerLifetimeScope();
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
            builder.RegisterType<Business.Services.Subscription.SubscriptionValidationService>()
                .As<Business.Services.Subscription.ISubscriptionValidationService>()
                .InstancePerLifetimeScope();

            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
                .Where(t => !t.IsAssignableTo<IFileStorageService>()) // Exclude file storage services to prevent override
                .EnableInterfaceInterceptors(new ProxyGenerationOptions()
                {
                    Selector = new AspectInterceptorSelector()
                }).SingleInstance().InstancePerDependency();
        }
    }
}