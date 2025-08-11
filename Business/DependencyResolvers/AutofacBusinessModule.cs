using System.Reflection;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Business.Services.PlantAnalysis;
using Business.Services.Configuration;
using Business.Services.ImageProcessing;
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
            
            builder.RegisterType<PlantAnalysisService>().As<IPlantAnalysisService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<ConfigurationService>().As<IConfigurationService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<ImageProcessingService>().As<IImageProcessingService>()
                .InstancePerLifetimeScope();

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

            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
                .EnableInterfaceInterceptors(new ProxyGenerationOptions()
                {
                    Selector = new AspectInterceptorSelector()
                }).SingleInstance().InstancePerDependency();
        }
    }
}