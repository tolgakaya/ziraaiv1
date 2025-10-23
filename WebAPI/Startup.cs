using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using Business;
using Business.Helpers;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Extensions;
using Core.Utilities.IoC;
using Core.Utilities.Security.Encyption;
using Core.Utilities.Security.Jwt;
using Core.Utilities.TaskScheduler.Hangfire.Models;
using Hangfire;
using HangfireBasicAuthenticationFilter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using WebAPI.Filters;
using ConfigurationManager = Business.ConfigurationManager;
using Business.Services.DatabaseInitializer;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Business.Services.Notification;
using Business.Hubs;

namespace WebAPI
{
    /// <summary>
    ///
    /// </summary>
    public partial class Startup : BusinessStartup
    {
        /// <summary>
        /// Constructor of <c>Startup</c>
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="hostEnvironment"></param>
        public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
            : base(configuration, hostEnvironment)
        {
            // Railway environment variables are now configured in Program.cs before configuration is built
        }


        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <remarks>
        /// It is common to all configurations and must be called. Aspnet core does not call this method because there are other methods.
        /// </remarks>
        /// <param name="services"></param>

        public override void ConfigureServices(IServiceCollection services)
        {
            // Business katmanında olan dependency tanımlarının bir metot üzerinden buraya implemente edilmesi.
            
            // Configuration Options
            services.Configure<Core.Configuration.RabbitMQOptions>(Configuration.GetSection(Core.Configuration.RabbitMQOptions.SectionName));

            // Add HttpContextAccessor for URL generation
            services.AddHttpContextAccessor();
            
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    // CRITICAL: Use camelCase for property names (mobile team expects camelCase JSON)
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                });

            services.AddApiVersioning(v =>
            {
                v.DefaultApiVersion = new ApiVersion(1, 0);
                v.AssumeDefaultVersionWhenUnspecified = true;
                v.ReportApiVersions = true;
                v.ApiVersionReader = new HeaderApiVersionReader("x-dev-arch-version");
            });

            services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllowOrigin",
                    builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

                // SignalR requires credentials support, so we need a separate policy
                options.AddPolicy(
                    "AllowSignalR",
                    builder => builder
                        .WithOrigins(
                            // Development
                            "http://localhost:3000",  // Web dev
                            "http://localhost:4200",  // Angular dev
                            "http://localhost:5173",  // Vite dev
                            // Staging
                            "https://staging-app.ziraai.com",
                            "https://staging.ziraai.com",
                            // Production
                            "https://app.ziraai.com",
                            "https://ziraai.com"
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });

            var tokenOptions = Configuration.GetSection("TokenOptions").Get<TokenOptions>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidIssuer = tokenOptions.Issuer,
                        ValidAudience = tokenOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey),
                        ClockSkew = TimeSpan.Zero
                    };

                    // Enable SignalR authentication via query string token
                    // SignalR can't send headers during initial connection, so token must be in query string
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            // If the request is for our hub...
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/hubs/plantanalysis")))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "ZiraAI API", 
                    Version = "v1",
                    Description = "ZiraAI Plant Analysis API"
                });
                
                // CRITICAL FIX: Use full type name including namespace to avoid schema conflicts
                c.CustomSchemaIds(type => type.FullName);
                
                // Map IFormFile to prevent Swagger generation errors
                c.MapType<IFormFile>(() => new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                });
                
                // Add operation filter to handle file upload endpoints
                c.OperationFilter<Swagger.FileUploadOperationFilter>();
            });

            services.AddTransient<FileLogger>();
            services.AddTransient<PostgreSqlLogger>();
            services.AddTransient<MsSqlLogger>();
            services.AddScoped<IpControlAttribute>();
            
            services.AddHttpClient();

            // Add Background Services
            // PlantAnalysisResultWorker moved to separate PlantAnalysisWorkerService project

            // Add Database Initializer Service
            services.AddScoped<IDatabaseInitializerService, DatabaseInitializerService>();

            // 🆕 Add SignalR with optional Redis backplane
            var useRedis = Configuration.GetValue<bool>("UseRedis", false);
            var isProduction = Configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Production";
            var signalRBuilder = services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = !isProduction; // Enable detailed errors in dev/staging
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.HandshakeTimeout = TimeSpan.FromSeconds(15);
            });

            // Redis backplane for SignalR horizontal scaling (using existing CacheOptions)
            if (useRedis)
            {
                var cacheConfig = Configuration.GetSection("CacheOptions").Get<Core.CrossCuttingConcerns.Caching.Redis.CacheOptions>();

                if (cacheConfig != null && !string.IsNullOrEmpty(cacheConfig.Host))
                {
                    Console.WriteLine("🔴 Configuring Redis backplane for SignalR using existing CacheOptions");

                    var configOptions = ConfigurationOptions.Parse($"{cacheConfig.Host}:{cacheConfig.Port}");
                    if (!string.IsNullOrEmpty(cacheConfig.Password))
                    {
                        configOptions.Password = cacheConfig.Password;
                    }
                    configOptions.DefaultDatabase = cacheConfig.Database;
                    configOptions.Ssl = cacheConfig.Ssl;
                    configOptions.AbortOnConnectFail = false;

                    // Railway SSL certificate fix
                    if (cacheConfig.Ssl)
                    {
                        configOptions.CertificateValidation += (sender, certificate, chain, errors) => true;
                    }

                    signalRBuilder.AddStackExchangeRedis(configOptions.ToString(), options =>
                    {
                        options.Configuration.ChannelPrefix = RedisChannel.Literal("ZiraAI:SignalR:");
                    });

                    Console.WriteLine($"✅ SignalR Redis backplane configured - Host: {cacheConfig.Host}, SSL: {cacheConfig.Ssl}");
                }
                else
                {
                    Console.WriteLine("⚠️ UseRedis=true but CacheOptions not configured - falling back to in-memory");
                }
            }
            else
            {
                Console.WriteLine("📦 Using in-memory SignalR (single instance only)");
            }

            // Register notification service
            services.AddScoped<IPlantAnalysisNotificationService, PlantAnalysisNotificationService>();

            base.ConfigureServices(services);
        }


        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // VERY IMPORTANT. Since we removed the build from AddDependencyResolvers, let's set the Service provider manually.
            // By the way, we can construct with DI by taking type to avoid calling static methods in aspects.
            ServiceTool.ServiceProvider = app.ApplicationServices;

            // Initialize database with seed data
            InitializeDatabase(app).GetAwaiter().GetResult();


            var configurationManager = app.ApplicationServices.GetService<ConfigurationManager>();
            switch (configurationManager.Mode)
            {
                case ApplicationMode.Development:
                    _ = app.UseDbFakeDataCreator();
                    break;

                case ApplicationMode.Profiling:
                case ApplicationMode.Staging:

                    break;
                case ApplicationMode.Production:
                    break;
            }

            app.UseDeveloperExceptionPage();

            app.ConfigureCustomExceptionMiddleware();

            _ = app.UseDbOperationClaimCreator();
            
            if (!env.IsProduction())
            {
                // Add try-catch to capture Swagger generation errors
                app.Use(async (context, next) =>
                {
                    try
                    {
                        await next();
                    }
                    catch (Exception ex) when (context.Request.Path.StartsWithSegments("/swagger"))
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/json";
                        
                        var errorDetails = new
                        {
                            error = "Swagger generation failed",
                            message = ex.Message,
                            type = ex.GetType().Name,
                            innerMessage = ex.InnerException?.Message,
                            innerType = ex.InnerException?.GetType().Name,
                            stackTrace = ex.StackTrace,
                            // Additional debug info for DI issues
                            fullInnerException = ex.InnerException?.ToString(),
                            allInnerExceptions = GetAllInnerExceptions(ex),
                            requestPath = context.Request.Path.Value,
                            timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                        };
                        
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorDetails));
                    }
                });
                
                app.UseSwagger();

                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("v1/swagger.json", "ZiraAI");
                    c.DocExpansion(DocExpansion.None);
                });
            }
            app.UseCors("AllowOrigin");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            
            // IMPORTANT: OnBehalfOfMiddleware must come AFTER authentication
            // but BEFORE authorization to properly set OBO context
            app.UseMiddleware<WebAPI.Middleware.OnBehalfOfMiddleware>();

            app.UseAuthorization();

            // Make Turkish your default language. It shouldn't change according to the server.
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("tr-TR"),
            });

            var cultureInfo = new CultureInfo("tr-TR");
            cultureInfo.DateTimeFormat.ShortTimePattern = "HH:mm";

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            // Serve .well-known directory for Android Universal Links (assetlinks.json)
            // Docker: /app/.well-known | Local: ContentRootPath/.well-known
            var wellKnownPath = Directory.Exists("/app/.well-known")
                ? "/app/.well-known"
                : Path.Combine(env.ContentRootPath, ".well-known");

            if (!Directory.Exists(wellKnownPath))
            {
                Directory.CreateDirectory(wellKnownPath);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(wellKnownPath),
                RequestPath = "/.well-known",
                ServeUnknownFileTypes = true,
                DefaultContentType = "application/json"
            });

            app.UseStaticFiles();

            // 🆕 Apply CORS for SignalR before endpoints
            app.UseCors("AllowSignalR");

            var taskSchedulerConfig = Configuration.GetSection("TaskSchedulerOptions").Get<TaskSchedulerConfig>();
            
            if (taskSchedulerConfig.Enabled)
            {
                app.UseHangfireDashboard(taskSchedulerConfig.Path, new DashboardOptions
                {
                    DashboardTitle = taskSchedulerConfig.Title,
                    Authorization = new[]
                    {
                        new HangfireCustomBasicAuthenticationFilter
                        {
                            User = taskSchedulerConfig.Username,
                            Pass = taskSchedulerConfig.Password
                        }
                    }
                });
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                // 🆕 Map SignalR hub
                endpoints.MapHub<PlantAnalysisHub>("/hubs/plantanalysis");
            });
        }
        
        /// <summary>
        /// Helper method to extract all inner exceptions for debugging
        /// </summary>
        private static string[] GetAllInnerExceptions(Exception ex)
        {
            var exceptions = new List<string>();
            var current = ex;
            
            while (current != null)
            {
                exceptions.Add($"{current.GetType().Name}: {current.Message}");
                current = current.InnerException;
            }
            
            return exceptions.ToArray();
        }
        
        /// <summary>
        /// Initialize database with seed data
        /// </summary>
        private async Task InitializeDatabase(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var databaseInitializer = services.GetRequiredService<IDatabaseInitializerService>();
                    await databaseInitializer.InitializeAsync();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Startup>>();
                    logger.LogError(ex, "An error occurred while initializing the database.");
                }
            }
        }
        
        /// <summary>
        /// Configure Railway environment variables for .NET Core compatibility
        /// </summary>
    }
}