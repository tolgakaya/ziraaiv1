# ZiraAI System Integration Patterns

This document provides comprehensive guidance on integration patterns used in the ZiraAI platform, serving as a reference for developers to understand, maintain, and extend system integrations.

## Table of Contents
1. [N8N AI Pipeline Integration](#1-n8n-ai-pipeline-integration)
2. [RabbitMQ Message Queue Integration](#2-rabbitmq-message-queue-integration)
3. [File Storage Integration](#3-file-storage-integration)
4. [Authentication & Authorization](#4-authentication--authorization)
5. [Database Integration Patterns](#5-database-integration-patterns)
6. [Caching Strategy](#6-caching-strategy)
7. [Background Job Processing](#7-background-job-processing)
8. [External Service Integration](#8-external-service-integration)
9. [Integration Security](#9-integration-security)
10. [Monitoring & Observability](#10-monitoring--observability)

---

## 1. N8N AI Pipeline Integration

### Architecture Pattern: **Webhook + HTTP Client**

The N8N integration uses URL-based image processing to achieve 99.6% token reduction and 99.9% cost savings.

#### Configuration
```json
{
  "N8N": {
    "WebhookUrl": "https://n8n-instance.com/webhook/plant-analysis",
    "UseImageUrl": true,
    "TimeoutSeconds": 300,
    "RetryAttempts": 3,
    "RetryDelaySeconds": 5
  },
  "AIOptimization": {
    "MaxSizeMB": 0.1,
    "Enabled": true,
    "MaxWidth": 800,
    "MaxHeight": 600,
    "Quality": 70
  }
}
```

#### Implementation Pattern
```csharp
public class PlantAnalysisService
{
    public async Task<PlantAnalysisResponseDto> SendToN8nWebhookAsync(PlantAnalysisRequestDto request)
    {
        // 1. Image Optimization (Critical for cost reduction)
        var optimizedImagePath = await OptimizeImageForAI(request.Image);
        
        // 2. Generate Public URL (Token reduction: 400K → 1.5K tokens)
        var imageUrl = await _fileStorageService.UploadAndGetUrlAsync(optimizedImagePath);
        
        // 3. Prepare N8N Request (URL instead of base64)
        var n8nRequest = new
        {
            image_url = imageUrl,  // Key optimization: URL not base64
            farmer_id = request.FarmerId,
            crop_type = request.CropType,
            location = request.Location,
            analysis_type = "comprehensive"
        };
        
        // 4. HTTP Client with Retry Policy
        using var client = _httpClientFactory.CreateClient("N8N");
        var response = await client.PostAsJsonAsync(_n8nWebhookUrl, n8nRequest);
        
        // 5. Process Response and Map to DTO
        return await ProcessN8nResponse(response);
    }
}
```

#### Key Optimizations
- **Token Reduction**: 400,000 → 1,500 tokens (99.6% reduction)
- **Cost Savings**: $12 → $0.01 per image (99.9% reduction)
- **Success Rate**: 20% → 100% (eliminated token limit errors)
- **Performance**: 10x faster processing

#### Error Handling Strategy
```csharp
public async Task<PlantAnalysisResponseDto> SendWithRetryAsync(PlantAnalysisRequestDto request)
{
    var retryPolicy = Policy
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                _logger.LogWarning("N8N request failed. Retry {retryCount} in {delay}s", 
                    retryCount, timespan.TotalSeconds);
            });

    return await retryPolicy.ExecuteAsync(async () => 
    {
        return await SendToN8nWebhookAsync(request);
    });
}
```

---

## 2. RabbitMQ Message Queue Integration

### Architecture Pattern: **Publisher-Consumer with Dead Letter Queue**

RabbitMQ enables asynchronous processing for plant analysis with enterprise-grade reliability.

#### Configuration
```json
{
  "RabbitMQ": {
    "ConnectionString": "amqp://user:pass@localhost:5672/",
    "Queues": {
      "PlantAnalysisRequest": "plant-analysis-requests",
      "PlantAnalysisResult": "plant-analysis-results",
      "Notification": "notifications"
    },
    "RetrySettings": {
      "MaxRetryAttempts": 3,
      "RetryDelayMilliseconds": 1000
    },
    "ConnectionSettings": {
      "RequestedHeartbeat": 60,
      "NetworkRecoveryInterval": 10
    }
  }
}
```

#### Publisher Implementation
```csharp
public class MessageQueueService : IMessageQueueService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public async Task<bool> PublishAsync<T>(string queueName, T message, string correlationId = null)
    {
        try
        {
            // Declare queue with durability
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    { "x-message-ttl", 3600000 }, // 1 hour TTL
                    { "x-dead-letter-exchange", "dlx" }
                });

            // Serialize message
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            // Message properties for persistence
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.CorrelationId = correlationId ?? Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            // Publish with confirmation
            _channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: body);

            _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to queue {Queue}", queueName);
            return false;
        }
    }
}
```

#### Consumer Pattern
```csharp
public class PlantAnalysisConsumerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartConsumingAsync<PlantAnalysisRequestDto>(
            "plant-analysis-requests",
            ProcessPlantAnalysisRequestAsync,
            stoppingToken);
    }

    private async Task ProcessPlantAnalysisRequestAsync(PlantAnalysisRequestDto request, string correlationId)
    {
        try
        {
            // Background processing with Hangfire
            BackgroundJob.Enqueue<IPlantAnalysisJobService>(
                service => service.ProcessAnalysisAsync(request, correlationId));
                
            _logger.LogInformation("Queued plant analysis job for correlation ID: {CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing plant analysis request: {CorrelationId}", correlationId);
            throw; // Re-throw to trigger retry/DLQ
        }
    }
}
```

#### Dead Letter Queue Handling
```csharp
public void ConfigureDeadLetterQueue()
{
    // Dead letter exchange
    _channel.ExchangeDeclare("dlx", ExchangeType.Direct, durable: true);
    
    // Dead letter queue
    _channel.QueueDeclare(
        queue: "dead-letter-queue",
        durable: true,
        exclusive: false,
        autoDelete: false);
        
    _channel.QueueBind("dead-letter-queue", "dlx", "");
}
```

---

## 3. File Storage Integration

### Architecture Pattern: **Multi-Provider Strategy with Fallback**

Supports multiple storage providers with automatic failover and optimization.

#### Provider Configuration
```json
{
  "FileStorage": {
    "Provider": "FreeImageHost",
    "FreeImageHost": {
      "ApiKey": "your-api-key-here"
    },
    "ImgBB": {
      "ApiKey": "your-imgbb-key",
      "ExpirationSeconds": 0
    },
    "Local": {
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "https://api.domain.com"
    },
    "S3": {
      "BucketName": "ziraai-images",
      "Region": "us-east-1",
      "UseCloudFront": true,
      "CloudFrontDomain": "cdn.ziraai.com"
    }
  }
}
```

#### Multi-Provider Implementation
```csharp
public class FileStorageService : IFileStorageService
{
    private readonly Dictionary<string, IStorageProvider> _providers;
    private readonly string _primaryProvider;

    public async Task<string> UploadAndGetUrlAsync(byte[] fileBytes, string fileName = null)
    {
        // Try primary provider first
        try
        {
            var primaryProvider = _providers[_primaryProvider];
            var url = await primaryProvider.UploadAsync(fileBytes, fileName);
            
            _logger.LogInformation("File uploaded successfully to {Provider}: {Url}", 
                _primaryProvider, url);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary provider {Provider} failed, trying fallback", 
                _primaryProvider);
        }

        // Fallback to secondary providers
        foreach (var provider in _providers.Where(p => p.Key != _primaryProvider))
        {
            try
            {
                var url = await provider.Value.UploadAsync(fileBytes, fileName);
                _logger.LogInformation("Fallback successful with {Provider}: {Url}", 
                    provider.Key, url);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed", provider.Key);
            }
        }

        throw new InvalidOperationException("All storage providers failed");
    }
}
```

#### FreeImageHost Implementation
```csharp
public class FreeImageHostProvider : IStorageProvider
{
    public async Task<string> UploadAsync(byte[] fileBytes, string fileName = null)
    {
        using var client = new HttpClient();
        using var form = new MultipartFormDataContent();
        
        // Add API key
        form.Add(new StringContent(_apiKey), "key");
        
        // Add file content
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        form.Add(fileContent, "source", fileName ?? GenerateFileName());

        // Upload request
        var response = await client.PostAsync("https://freeimage.host/api/1/upload", form);
        response.EnsureSuccessStatusCode();

        // Parse response
        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FreeImageHostResponse>(responseJson);
        
        if (!result.Success)
            throw new InvalidOperationException($"Upload failed: {result.Error?.Message}");

        return result.Image.Url;
    }
}
```

---

## 4. Authentication & Authorization

### Architecture Pattern: **JWT Bearer with Role-Based Access Control**

Implements secure authentication with claims-based authorization and token refresh.

#### JWT Configuration
```csharp
public class TokenOptions
{
    public string Audience { get; set; }
    public string Issuer { get; set; }
    public int AccessTokenExpiration { get; set; } = 60; // minutes
    public int RefreshTokenExpiration { get; set; } = 180; // minutes
    public string SecurityKey { get; set; }
}
```

#### Token Generation Service
```csharp
public class AuthenticationService : IAuthenticationService
{
    public async Task<DArchToken> CreateAccessTokenAsync(User user)
    {
        var tokenOptions = _configuration.GetSection("TokenOptions").Get<TokenOptions>();
        var securityKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey);
        var signingCredentials = SigningCredentialsHelper.CreateSigningCredentials(securityKey);

        var jwt = CreateJwtSecurityToken(tokenOptions, user, signingCredentials, await GetUserClaimsAsync(user));
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        var refreshToken = CreateRefreshToken();

        return new DArchToken
        {
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(tokenOptions.AccessTokenExpiration)
        };
    }

    private JwtSecurityToken CreateJwtSecurityToken(TokenOptions tokenOptions, User user, 
        SigningCredentials signingCredentials, IList<OperationClaim> operationClaims)
    {
        var jwt = new JwtSecurityToken(
            issuer: tokenOptions.Issuer,
            audience: tokenOptions.Audience,
            expires: DateTime.UtcNow.AddMinutes(tokenOptions.AccessTokenExpiration),
            notBefore: DateTime.UtcNow,
            claims: SetClaims(user, operationClaims),
            signingCredentials: signingCredentials
        );
        return jwt;
    }

    private IEnumerable<Claim> SetClaims(User user, IList<OperationClaim> operationClaims)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("userId", user.UserId.ToString())
        };
        
        // Add role claims
        foreach (var claim in operationClaims)
        {
            claims.Add(new Claim(ClaimTypes.Role, claim.Name));
        }
        
        return claims;
    }
}
```

#### Role-Based Authorization
```csharp
[ApiController]
[Route("api/[controller]")]
public class PlantAnalysesController : BaseApiController
{
    [HttpPost("analyze")]
    [Authorize(Roles = "Farmer,Admin")]
    public async Task<IActionResult> AnalyzeAsync([FromBody] PlantAnalysisRequestDto request)
    {
        // Get user ID from JWT claims
        var userId = GetUserIdFromClaims();
        request.UserId = userId;
        
        // Process through CQRS
        var result = await Mediator.Send(new CreatePlantAnalysisCommand 
        { 
            // Map request properties
        });
        
        return Ok(result);
    }
    
    [HttpGet("sponsored-analyses")]
    [Authorize(Roles = "Sponsor,Admin")]
    public async Task<IActionResult> GetSponsoredAnalysesAsync()
    {
        var sponsorId = GetUserIdFromClaims();
        var result = await Mediator.Send(new GetSponsoredAnalysesQuery { SponsorId = sponsorId });
        return Ok(result);
    }
}
```

#### SecuredOperation Aspect
```csharp
public class SecuredOperation : MethodInterception
{
    public override void Intercept(IInvocation invocation)
    {
        var httpContext = ServiceTool.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
        
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        // Check operation claims
        var operationName = GetOperationName(invocation);
        if (!httpContext.User.HasClaim(ClaimTypes.Role, operationName))
        {
            throw new UnauthorizedAccessException($"User lacks required claim: {operationName}");
        }

        invocation.Proceed();
    }
}
```

---

## 5. Database Integration Patterns

### Architecture Pattern: **Repository + Unit of Work with CQRS**

Entity Framework Core integration with PostgreSQL timezone handling and performance optimizations.

#### Repository Pattern Implementation
```csharp
public class PlantAnalysisRepository : EfEntityRepositoryBase<PlantAnalysis, ProjectDbContext>, IPlantAnalysisRepository
{
    public PlantAnalysisRepository(ProjectDbContext context) : base(context) { }

    public async Task<List<PlantAnalysis>> GetListByUserIdAsync(int userId)
    {
        return await Context.PlantAnalyses
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PlantAnalysis> GetByIdWithDetailsAsync(int id)
    {
        return await Context.PlantAnalyses
            .Include(p => p.User)
            .Include(p => p.SponsorshipCode)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
```

#### CQRS Command Handler
```csharp
public class CreatePlantAnalysisCommandHandler : IRequestHandler<CreatePlantAnalysisCommand, IDataResult<PlantAnalysisResponseDto>>
{
    private readonly IPlantAnalysisRepository _plantAnalysisRepository;
    private readonly IPlantAnalysisService _plantAnalysisService;

    public async Task<IDataResult<PlantAnalysisResponseDto>> Handle(CreatePlantAnalysisCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Create entity
            var plantAnalysis = new PlantAnalysis
            {
                UserId = request.UserId,
                FarmerId = request.FarmerId,
                CropType = request.CropType,
                Location = request.Location,
                Status = "Processing",
                CreatedDate = DateTime.Now, // PostgreSQL compatible
                UpdatedDate = DateTime.Now
            };

            // 2. Save to get ID
            var addedAnalysis = await _plantAnalysisRepository.AddAsync(plantAnalysis);
            await _plantAnalysisRepository.SaveChangesAsync();

            // 3. Process image and call N8N
            var analysisResult = await _plantAnalysisService.SendToN8nWebhookAsync(request);
            
            // 4. Update with results
            addedAnalysis.N8nResponse = JsonSerializer.Serialize(analysisResult);
            addedAnalysis.Status = "Completed";
            addedAnalysis.UpdatedDate = DateTime.Now;
            
            await _plantAnalysisRepository.UpdateAsync(addedAnalysis);
            await _plantAnalysisRepository.SaveChangesAsync();

            return new SuccessDataResult<PlantAnalysisResponseDto>(analysisResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plant analysis");
            return new ErrorDataResult<PlantAnalysisResponseDto>("Analysis failed");
        }
    }
}
```

#### PostgreSQL Timezone Handling
```csharp
// Program.cs - Global timezone compatibility
System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
System.AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

// Entity Configuration
public class PlantAnalysisEntityConfiguration : IEntityTypeConfiguration<PlantAnalysis>
{
    public void Configure(EntityTypeBuilder<PlantAnalysis> builder)
    {
        builder.ToTable("PlantAnalyses");
        
        builder.Property(p => p.CreatedDate)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.Property(p => p.UpdatedDate)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
```

#### Connection Pool Configuration
```json
{
  "ConnectionStrings": {
    "DArchPgContext": "Host=localhost;Database=ziraai;Username=user;Password=pass;Pooling=true;MinPoolSize=10;MaxPoolSize=100;ConnectionIdleLifetime=300"
  }
}
```

---

## 6. Caching Strategy

### Architecture Pattern: **Multi-Level Cache with Cache-Aside**

Redis distributed cache with in-memory fallback and intelligent invalidation.

#### Cache Configuration
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Redis distributed cache
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = configuration.GetConnectionString("Redis");
        options.InstanceName = "ZiraAI";
    });

    // Memory cache fallback
    services.AddMemoryCache();
    
    // Custom cache service
    services.AddSingleton<ICacheManager, CacheManager>();
}
```

#### Cache Service Implementation
```csharp
public class CacheManager : ICacheManager
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;

    public async Task<T> GetAsync<T>(string key)
    {
        try
        {
            // Try Redis first
            var redisValue = await _distributedCache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(redisValue))
            {
                return JsonSerializer.Deserialize<T>(redisValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache failed, falling back to memory cache");
        }

        // Fallback to memory cache
        return _memoryCache.Get<T>(key);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions();
        
        if (expiry.HasValue)
        {
            options.SetAbsoluteExpiration(expiry.Value);
        }

        try
        {
            // Set in Redis
            await _distributedCache.SetStringAsync(key, serializedValue, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set Redis cache");
        }

        // Always set in memory cache as backup
        _memoryCache.Set(key, value, expiry ?? TimeSpan.FromMinutes(15));
    }
}
```

#### Cache Aspect Implementation
```csharp
public class CacheAspect : MethodInterception
{
    private readonly int _duration;
    private readonly ICacheManager _cacheManager;

    public CacheAspect(int duration = 60)
    {
        _duration = duration;
        _cacheManager = ServiceTool.ServiceProvider.GetService<ICacheManager>();
    }

    public override void Intercept(IInvocation invocation)
    {
        var methodName = $"{invocation.Method.ReflectedType.FullName}.{invocation.Method.Name}";
        var arguments = invocation.Arguments.ToList();
        var key = $"{methodName}({string.Join(",", arguments.Select(x => x?.ToString() ?? "<Null>"))})";

        var cachedResult = _cacheManager.Get(key);
        if (cachedResult != null)
        {
            invocation.ReturnValue = cachedResult;
            return;
        }

        invocation.Proceed();

        if (invocation.ReturnValue != null)
        {
            _cacheManager.Set(key, invocation.ReturnValue, TimeSpan.FromMinutes(_duration));
        }
    }
}
```

#### Configuration Service Caching
```csharp
public class ConfigurationService : IConfigurationService
{
    public async Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue)
    {
        var cacheKey = $"config:{key}";
        
        // Try cache first
        var cachedValue = await _cacheManager.GetAsync<string>(cacheKey);
        if (cachedValue != null)
        {
            return decimal.TryParse(cachedValue, out var cached) ? cached : defaultValue;
        }

        // Get from database
        var config = await _configurationRepository.GetAsync(c => c.Key == key && c.IsActive);
        var value = config != null && decimal.TryParse(config.Value, out var parsed) ? parsed : defaultValue;

        // Cache for 15 minutes
        await _cacheManager.SetAsync(cacheKey, value.ToString(), TimeSpan.FromMinutes(15));
        
        return value;
    }
}
```

---

## 7. Background Job Processing

### Architecture Pattern: **Hangfire with PostgreSQL Storage**

Enterprise background job processing with automatic retries and monitoring.

#### Hangfire Configuration
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseNpgsqlStorage(connectionString, new NpgsqlStorageOptions
        {
            QueuePollInterval = TimeSpan.FromSeconds(15),
            JobExpirationCheckInterval = TimeSpan.FromHours(1),
            CountersAggregateInterval = TimeSpan.FromMinutes(5),
            PrepareSchemaIfNecessary = true,
            DashboardJobListLimit = 50000,
            TransactionSynchronisationTimeout = TimeSpan.FromMinutes(5)
        }));

    services.AddHangfireServer(options =>
    {
        options.WorkerCount = Environment.ProcessorCount * 2;
        options.Queues = new[] { "critical", "default", "background" };
    });
}
```

#### Job Service Implementation
```csharp
public class PlantAnalysisJobService : IPlantAnalysisJobService
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    [Queue("default")]
    public async Task ProcessPlantAnalysisResultAsync(PlantAnalysisAsyncResponseDto result, string correlationId)
    {
        try
        {
            _logger.LogInformation("Processing plant analysis result for correlation ID: {CorrelationId}", correlationId);

            // Find existing analysis record
            var analysis = await _plantAnalysisRepository.GetAsync(p => p.CorrelationId == correlationId);
            if (analysis == null)
            {
                throw new InvalidOperationException($"Analysis not found for correlation ID: {correlationId}");
            }

            // Update with N8N results
            analysis.N8nResponse = JsonSerializer.Serialize(result);
            analysis.PlantType = result.PlantType;
            analysis.OverallHealth = result.OverallHealth;
            analysis.OverallHealthScore = result.OverallHealthScore;
            analysis.Diseases = JsonSerializer.Serialize(result.Diseases);
            analysis.Pests = JsonSerializer.Serialize(result.Pests);
            analysis.Recommendations = result.Recommendations;
            analysis.Status = "Completed";
            analysis.UpdatedDate = DateTime.Now;

            await _plantAnalysisRepository.UpdateAsync(analysis);
            await _plantAnalysisRepository.SaveChangesAsync();

            // Queue notification job
            BackgroundJob.Enqueue<INotificationService>(
                service => service.SendAnalysisCompleteNotificationAsync(analysis.UserId, analysis.Id));

            _logger.LogInformation("Successfully processed analysis for correlation ID: {CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing plant analysis result for correlation ID: {CorrelationId}", correlationId);
            throw; // Re-throw to trigger retry
        }
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 10, 30 })]
    [Queue("background")]
    public async Task ResetDailyUsageCountersAsync()
    {
        var subscriptions = await _userSubscriptionRepository.GetListAsync(s => s.IsActive);
        
        foreach (var subscription in subscriptions)
        {
            subscription.CurrentDailyUsage = 0;
            subscription.UpdatedDate = DateTime.Now;
        }

        await _userSubscriptionRepository.SaveChangesAsync();
        _logger.LogInformation("Reset daily usage counters for {Count} subscriptions", subscriptions.Count);
    }
}
```

#### Recurring Job Setup
```csharp
public class JobSchedulingService
{
    public void ConfigureRecurringJobs()
    {
        // Daily quota reset at midnight UTC
        RecurringJob.AddOrUpdate<IPlantAnalysisJobService>(
            "reset-daily-usage",
            service => service.ResetDailyUsageCountersAsync(),
            "0 0 * * *", // Cron: Daily at midnight
            TimeZoneInfo.Utc);

        // Monthly quota reset on 1st of month
        RecurringJob.AddOrUpdate<IPlantAnalysisJobService>(
            "reset-monthly-usage",
            service => service.ResetMonthlyUsageCountersAsync(),
            "0 0 1 * *", // Cron: 1st day of month at midnight
            TimeZoneInfo.Utc);

        // Cleanup old analysis records
        RecurringJob.AddOrUpdate<IPlantAnalysisJobService>(
            "cleanup-old-analyses",
            service => service.CleanupOldAnalysesAsync(),
            "0 2 * * 0", // Cron: Weekly Sunday at 2 AM
            TimeZoneInfo.Utc);
    }
}
```

#### Job Dashboard Security
```csharp
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Require authentication
        if (!httpContext.User.Identity.IsAuthenticated)
            return false;

        // Require Admin role
        return httpContext.User.IsInRole("Admin");
    }
}
```

---

## 8. External Service Integration

### Architecture Pattern: **Circuit Breaker with Retry Policy**

Resilient integration with external services using circuit breaker and retry patterns.

#### WhatsApp Business API Integration
```csharp
public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly ICircuitBreaker _circuitBreaker;

    public async Task<bool> SendTemplateMessageAsync(string phoneNumber, string templateName, Dictionary<string, string> parameters)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var request = new
            {
                messaging_product = "whatsapp",
                to = phoneNumber,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = "tr" },
                    components = new[]
                    {
                        new
                        {
                            type = "body",
                            parameters = parameters.Select(p => new { type = "text", text = p.Value }).ToArray()
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("messages", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new WhatsAppApiException($"WhatsApp API error: {error}");
            }

            return true;
        });
    }
}
```

#### Circuit Breaker Implementation
```csharp
public class CircuitBreakerService : ICircuitBreaker
{
    private readonly ICircuitBreakerPolicy _policy;

    public CircuitBreakerService()
    {
        _policy = Policy
            .Handle<HttpRequestException>()
            .Or<WhatsAppApiException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, timespan) =>
                {
                    _logger.LogWarning("Circuit breaker opened for {Duration}s due to: {Error}",
                        timespan.TotalSeconds, exception.Message);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker closed - service recovered");
                });
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        return await _policy.ExecuteAsync(operation);
    }
}
```

#### Rate Limiting Implementation
```csharp
public class RateLimitedHttpClient
{
    private readonly SemaphoreSlim _semaphore;
    private readonly TimeSpan _rateLimitPeriod;
    private DateTime _lastRequest = DateTime.MinValue;

    public RateLimitedHttpClient(int maxConcurrentRequests = 5, TimeSpan? rateLimitPeriod = null)
    {
        _semaphore = new SemaphoreSlim(maxConcurrentRequests);
        _rateLimitPeriod = rateLimitPeriod ?? TimeSpan.FromSeconds(1);
    }

    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
    {
        await _semaphore.WaitAsync();
        
        try
        {
            // Enforce rate limiting
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequest;
            if (timeSinceLastRequest < _rateLimitPeriod)
            {
                var delay = _rateLimitPeriod - timeSinceLastRequest;
                await Task.Delay(delay);
            }

            _lastRequest = DateTime.UtcNow;
            return await _httpClient.PostAsync(url, content);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

---

## 9. Integration Security

### Security Patterns and Best Practices

#### API Key Management
```csharp
public class SecureApiKeyManager
{
    private readonly IConfiguration _configuration;
    private readonly IDataProtectionProvider _dataProtection;

    public string GetDecryptedApiKey(string keyName)
    {
        var encryptedKey = _configuration[$"ApiKeys:{keyName}"];
        if (string.IsNullOrEmpty(encryptedKey))
            throw new InvalidOperationException($"API key {keyName} not configured");

        var protector = _dataProtection.CreateProtector("ApiKeys");
        return protector.Unprotect(encryptedKey);
    }
}
```

#### Request Validation
```csharp
public class ApiRequestValidator
{
    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = $"sha256={Convert.ToHexString(computedHash).ToLowerInvariant()}";
        
        return signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);
    }
}
```

#### SSL/TLS Configuration
```csharp
public void ConfigureHttpClients(IServiceCollection services)
{
    services.AddHttpClient("N8N", client =>
    {
        client.BaseAddress = new Uri(_configuration["N8N:WebhookUrl"]);
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.Add("User-Agent", "ZiraAI/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            // Only allow valid certificates in production
            return _environment.IsDevelopment() || errors == SslPolicyErrors.None;
        }
    });
}
```

---

## 10. Monitoring & Observability

### Monitoring Integration Patterns

#### Application Insights Integration
```csharp
public class TelemetryService
{
    private readonly TelemetryClient _telemetryClient;

    public void TrackIntegrationMetrics(string integrationName, string operation, TimeSpan duration, bool success)
    {
        var telemetry = new EventTelemetry("IntegrationOperation");
        telemetry.Properties["Integration"] = integrationName;
        telemetry.Properties["Operation"] = operation;
        telemetry.Properties["Success"] = success.ToString();
        telemetry.Metrics["Duration"] = duration.TotalMilliseconds;
        
        _telemetryClient.TrackEvent(telemetry);
    }

    public void TrackN8nAnalysis(int userId, string correlationId, TimeSpan processingTime, decimal cost)
    {
        var telemetry = new EventTelemetry("PlantAnalysis");
        telemetry.Properties["UserId"] = userId.ToString();
        telemetry.Properties["CorrelationId"] = correlationId;
        telemetry.Metrics["ProcessingTimeMs"] = processingTime.TotalMilliseconds;
        telemetry.Metrics["CostUSD"] = (double)cost;
        
        _telemetryClient.TrackEvent(telemetry);
    }
}
```

#### Health Check Integration
```csharp
public void ConfigureHealthChecks(IServiceCollection services)
{
    services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "postgresql")
        .AddRedis(redisConnectionString, name: "redis")
        .AddRabbitMQ(rabbitMqConnectionString, name: "rabbitmq")
        .AddUrlGroup(new Uri(n8nWebhookUrl), name: "n8n-webhook")
        .AddHangfire(options => options.MinimumAvailableServers = 1, name: "hangfire");
}

public class ExternalServiceHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy($"Service responded with {response.StatusCode}");
            }
            
            return HealthCheckResult.Unhealthy($"Service responded with {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Service unreachable: {ex.Message}");
        }
    }
}
```

#### Custom Metrics Collection
```csharp
public class IntegrationMetricsCollector
{
    private readonly IMetricsLogger _metrics;

    public void RecordN8nRequestMetrics(TimeSpan responseTime, int tokenCount, decimal cost)
    {
        _metrics.Counter("n8n_requests_total").Increment();
        _metrics.Histogram("n8n_response_time_ms").Observe(responseTime.TotalMilliseconds);
        _metrics.Counter("n8n_tokens_used_total").Increment(tokenCount);
        _metrics.Counter("n8n_cost_usd_total").Increment((double)cost);
    }

    public void RecordSubscriptionMetrics(string tierName, string operation)
    {
        _metrics.Counter("subscription_operations_total")
            .WithTag("tier", tierName)
            .WithTag("operation", operation)
            .Increment();
    }
}
```

---

## Integration Testing Strategy

### Integration Test Patterns

```csharp
[TestClass]
public class N8nIntegrationTests
{
    [TestMethod]
    public async Task SendToN8nWebhookAsync_WithValidImage_ReturnsAnalysis()
    {
        // Arrange
        var testImageBytes = File.ReadAllBytes("test-plant.jpg");
        var request = new PlantAnalysisRequestDto
        {
            Image = Convert.ToBase64String(testImageBytes),
            CropType = "tomato",
            FarmerId = "TEST001"
        };

        // Act
        var result = await _plantAnalysisService.SendToN8nWebhookAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.PlantType);
        Assert.IsTrue(result.OverallHealthScore >= 0 && result.OverallHealthScore <= 10);
    }
}

[TestClass]
public class FileStorageIntegrationTests
{
    [TestMethod]
    public async Task UploadAndGetUrlAsync_WithFallback_ReturnsValidUrl()
    {
        // Arrange
        var testImageBytes = new byte[] { 1, 2, 3, 4 };
        var mockPrimaryProvider = new Mock<IStorageProvider>();
        mockPrimaryProvider.Setup(p => p.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Primary provider failed"));

        // Act
        var result = await _fileStorageService.UploadAndGetUrlAsync(testImageBytes);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(Uri.TryCreate(result, UriKind.Absolute, out _));
    }
}
```

---

## Configuration Management

### Environment-Specific Integration Settings

#### Development Configuration
```json
{
  "N8N": {
    "WebhookUrl": "http://localhost:5678/webhook/plant-analysis",
    "UseImageUrl": true,
    "TimeoutSeconds": 60
  },
  "RabbitMQ": {
    "ConnectionString": "amqp://dev:devpass@localhost:5672/",
    "RetrySettings": {
      "MaxRetryAttempts": 2,
      "RetryDelayMilliseconds": 500
    }
  },
  "FileStorage": {
    "Provider": "Local",
    "Local": {
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "https://localhost:5001"
    }
  }
}
```

#### Production Configuration
```json
{
  "N8N": {
    "WebhookUrl": "https://n8n.production.com/webhook/plant-analysis",
    "UseImageUrl": true,
    "TimeoutSeconds": 300
  },
  "RabbitMQ": {
    "ConnectionString": "amqps://prod:prodpass@rabbitmq.production.com:5671/prod-vhost",
    "RetrySettings": {
      "MaxRetryAttempts": 5,
      "RetryDelayMilliseconds": 2000
    }
  },
  "FileStorage": {
    "Provider": "S3",
    "S3": {
      "BucketName": "ziraai-production-images",
      "Region": "eu-west-1",
      "UseCloudFront": true,
      "CloudFrontDomain": "cdn.ziraai.com"
    }
  }
}
```

---

## Performance Optimization Guidelines

### Integration Performance Best Practices

1. **Connection Pooling**: Use HTTP client factories and database connection pooling
2. **Async Operations**: Always use async/await for I/O operations
3. **Caching**: Cache frequently accessed configuration and reference data
4. **Bulk Operations**: Batch database operations where possible
5. **Resource Limits**: Implement rate limiting and circuit breakers
6. **Monitoring**: Track performance metrics and set up alerts

### Key Performance Metrics to Monitor

- **N8N Integration**: Response time, token usage, cost per request
- **RabbitMQ**: Queue depth, message processing rate, dead letter queue size
- **Database**: Connection pool usage, query response time, deadlocks
- **Cache**: Hit ratio, eviction rate, memory usage
- **File Storage**: Upload success rate, response time by provider

---

*Generated: December 2024 | Version: 1.0 | Last Updated: Integration patterns based on production codebase analysis*