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
using Microsoft.Extensions.Configuration;
using Castle.DynamicProxy;
using Core.Utilities.Interceptors;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
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


            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
                .AsClosedTypesOf(typeof(IRequestHandler<,>));

            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
                .AsClosedTypesOf(typeof(IValidator<>));

            builder.RegisterType<PlantAnalysisRepository>().As<IPlantAnalysisRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<ConfigurationRepository>().As<IConfigurationRepository>()
                .InstancePerLifetimeScope();
            
            // Subscription repositories
            builder.RegisterType<SubscriptionTierRepository>().As<ISubscriptionTierRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<UserSubscriptionRepository>().As<IUserSubscriptionRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SubscriptionUsageLogRepository>().As<ISubscriptionUsageLogRepository>()
                .InstancePerLifetimeScope();
            
            // Sponsorship repositories
            builder.RegisterType<SponsorshipCodeRepository>().As<ISponsorshipCodeRepository>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<SponsorshipPurchaseRepository>().As<ISponsorshipPurchaseRepository>()
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
            
            
            // Register all storage implementations first
            builder.RegisterType<LocalFileStorageService>().InstancePerLifetimeScope();
            builder.RegisterType<ImgBBStorageService>().InstancePerLifetimeScope();
            builder.RegisterType<FreeImageHostStorageService>().InstancePerLifetimeScope();
            // builder.RegisterType<S3FileStorageService>().InstancePerLifetimeScope(); // Requires AWS SDK
            
            // File Storage Services - Simple environment-based registration
            // Read configuration at registration time to avoid DI issues
            if (_configuration != null)
            {
                var configManager = _configuration;
                Console.WriteLine($"[FileStorage] AutofacBusinessModule Mode: {configManager.Mode}");
                
                // For now, register based on environment mode
                // In Development/Staging: Use FreeImageHost, in Production: Use S3 or Local
                switch (configManager.Mode)
                {
                    case ApplicationMode.Development:
                    case ApplicationMode.Staging:
                        Console.WriteLine("[FileStorage] Registering FreeImageHostStorageService for Development/Staging");
                        builder.Register<IFileStorageService>(c => c.Resolve<FreeImageHostStorageService>()).InstancePerLifetimeScope();
                        break;
                    case ApplicationMode.Production:
                        Console.WriteLine("[FileStorage] Registering LocalFileStorageService for Production");
                        builder.Register<IFileStorageService>(c => c.Resolve<LocalFileStorageService>()).InstancePerLifetimeScope();
                        break;
                    default:
                        Console.WriteLine("[FileStorage] Registering LocalFileStorageService as default");
                        builder.Register<IFileStorageService>(c => c.Resolve<LocalFileStorageService>()).InstancePerLifetimeScope();
                        break;
                }
            }
            else
            {
                Console.WriteLine("[FileStorage] No ConfigurationManager available, using LocalFileStorageService");
                builder.Register<IFileStorageService>(c => c.Resolve<LocalFileStorageService>()).InstancePerLifetimeScope();
            }

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