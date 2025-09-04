# WhatsApp Sponsor Request System - Troubleshooting Guide

## Common Issues and Solutions

This troubleshooting guide covers the most common issues encountered with the WhatsApp Sponsor Request System and provides step-by-step solutions for resolution.

## Database Issues

### Issue 1: Migration Conflicts
**Symptoms**:
```
An operation was scaffolded that may result in the loss of data.
Column 'IsSponsoredSubscription' on table 'Users' already exists.
```

**Root Cause**: Existing columns conflict with new migration schema.

**Solution**:
```bash
# Option 1: Manual table creation
dotnet script add_sponsor_request_tables.csx

# Option 2: Migration cleanup
dotnet ef migrations remove --project DataAccess --startup-project WebAPI
dotnet ef migrations add AddSponsorRequestSystemClean --project DataAccess --startup-project WebAPI

# Option 3: Direct SQL execution
psql -h localhost -U postgres -d ziraai_dev -f deploy/migrations/001_sponsor_request_system.sql
```

**Verification**:
```sql
-- Check if tables exist
SELECT table_name FROM information_schema.tables 
WHERE table_name IN ('SponsorRequests', 'SponsorContacts');

-- Verify foreign key constraints
SELECT 
    tc.table_name, 
    kcu.column_name, 
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM information_schema.table_constraints AS tc 
JOIN information_schema.key_column_usage AS kcu 
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu 
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY' 
    AND tc.table_name IN ('SponsorRequests', 'SponsorContacts');
```

### Issue 2: PostgreSQL DateTime Timezone Errors
**Symptoms**:
```
Cannot write DateTime with Kind=UTC to PostgreSQL type 'timestamp without time zone'
```

**Root Cause**: DateTime.UtcNow conflicts with PostgreSQL timestamp columns.

**Solution**:
```csharp
// Global fix in Program.cs (both WebAPI and WorkerService)
System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
System.AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

// Code fix: Replace DateTime.UtcNow with DateTime.Now
// Before:
subscription.CreatedDate = DateTime.UtcNow;

// After:
subscription.CreatedDate = DateTime.Now;
```

**Verification**:
```bash
# Test database save operation
dotnet test Tests/Integration/SponsorRequestDatabaseTests.cs --filter "TestCreateSponsorRequest"
```

### Issue 3: Foreign Key Constraint Violations
**Symptoms**:
```
insert or update on table "SponsorRequests" violates foreign key constraint "FK_SponsorRequests_Sponsor"
```

**Root Cause**: Referenced sponsor user doesn't exist or has wrong ID.

**Solution**:
```csharp
// Enhanced validation in service
public async Task<IDataResult<string>> CreateRequestAsync(int farmerId, string sponsorPhone, string message, int tierId)
{
    // Validate farmer exists and is active
    var farmer = await _userRepository.GetAsync(u => u.UserId == farmerId && u.Status == true);
    if (farmer == null)
    {
        return new ErrorDataResult<string>("Farmer not found or inactive");
    }

    // Validate sponsor exists, is active, and has Sponsor role
    var sponsor = await _userRepository.GetAsync(u => 
        u.MobilePhones == sponsorPhone && 
        u.Status == true && 
        u.UserOperationClaims.Any(oc => oc.OperationClaim.Name == "Sponsor"));
    
    if (sponsor == null)
    {
        return new ErrorDataResult<string>("Sponsor not found, inactive, or doesn't have sponsor privileges");
    }
    
    // Continue with creation...
}
```

**Debugging Commands**:
```sql
-- Check user roles
SELECT u.UserId, u.MobilePhones, u.Status, oc.Name as Role
FROM Users u
LEFT JOIN UserOperationClaims uoc ON u.UserId = uoc.UserId
LEFT JOIN OperationClaims oc ON uoc.OperationClaimId = oc.Id
WHERE u.MobilePhones = '+905551234567';

-- Check subscription tiers
SELECT Id, TierName, DisplayName FROM SubscriptionTiers WHERE Id = 2;
```

## Authentication and Authorization Issues

### Issue 4: JWT Token Validation Failures
**Symptoms**:
```
401 Unauthorized - Unable to get current user from JWT
```

**Root Cause**: JWT claims not properly configured or token expired.

**Solution**:
```csharp
// Enhanced token extraction in handlers
public async Task<IDataResult<string>> Handle(CreateSponsorRequestCommand request, CancellationToken cancellationToken)
{
    var httpContext = _httpContextAccessor.HttpContext;
    if (httpContext?.User?.Identity?.IsAuthenticated != true)
    {
        return new ErrorDataResult<string>("User not authenticated");
    }

    // Multiple claim name attempts for compatibility
    var userIdClaim = httpContext.User.FindFirst("UserId") 
                      ?? httpContext.User.FindFirst("sub") 
                      ?? httpContext.User.FindFirst("user_id")
                      ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                      
    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int farmerId))
    {
        return new ErrorDataResult<string>("Invalid user token - UserId claim missing or invalid");
    }
    
    // Continue with service call...
}
```

**Debugging Commands**:
```csharp
// Add logging to see all claims
foreach (var claim in httpContext.User.Claims)
{
    Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
}
```

### Issue 5: Role Authorization Failures
**Symptoms**:
```
403 Forbidden - User does not have required role
```

**Root Cause**: User doesn't have required role or role claims not properly set.

**Solution**:
```sql
-- Check user roles in database
SELECT 
    u.UserId,
    u.Email,
    u.MobilePhones,
    oc.Name as RoleName
FROM Users u
JOIN UserOperationClaims uoc ON u.UserId = uoc.UserId
JOIN OperationClaims oc ON uoc.OperationClaimId = oc.Id
WHERE u.UserId = 123;

-- Add Farmer role to user
INSERT INTO UserOperationClaims (UserId, OperationClaimId)
SELECT 123, Id FROM OperationClaims WHERE Name = 'Farmer';

-- Add Sponsor role to user  
INSERT INTO UserOperationClaims (UserId, OperationClaimId)
SELECT 124, Id FROM OperationClaims WHERE Name = 'Sponsor';
```

**JWT Configuration Check**:
```csharp
// Verify JWT configuration in Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Token:Issuer"],
            ValidAudience = builder.Configuration["Token:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Token:SecurityKey"])),
            ClockSkew = TimeSpan.Zero
        };
    });
```

## WhatsApp Integration Issues

### Issue 6: WhatsApp URL Generation Problems
**Symptoms**:
```
WhatsApp doesn't open or message is malformed
```

**Root Cause**: Incorrect URL encoding or phone number format.

**Solution**:
```csharp
public string GenerateWhatsAppMessage(SponsorRequest request)
{
    // Ensure phone number is in correct format
    var sponsorPhone = FormatPhoneNumber(request.SponsorPhone);
    
    var baseUrl = _configuration["SponsorRequest:DeepLinkBaseUrl"];
    var deeplinkUrl = $"{baseUrl}{request.RequestToken}";
    var message = request.RequestMessage ?? _configuration["SponsorRequest:DefaultRequestMessage"];
    
    // Proper URL encoding for WhatsApp
    var fullMessage = $"{message}\n\nOnaylamak için tıklayın: {deeplinkUrl}";
    var encodedMessage = Uri.EscapeDataString(fullMessage);
    
    // WhatsApp URL format validation
    var whatsappUrl = $"https://wa.me/{sponsorPhone}?text={encodedMessage}";
    
    return whatsappUrl;
}

private string FormatPhoneNumber(string phone)
{
    // Remove all non-digit characters except +
    var cleaned = Regex.Replace(phone, @"[^\d+]", "");
    
    // Ensure it starts with +
    if (!cleaned.StartsWith("+"))
    {
        cleaned = "+" + cleaned;
    }
    
    return cleaned;
}
```

**Testing WhatsApp URLs**:
```bash
# Test URL format
curl -X GET "https://api.ziraai.com/api/sponsor-request/123/whatsapp-message" \
  -H "Authorization: Bearer $TOKEN"

# Manual URL test
https://wa.me/+905551234567?text=Test%20message%20with%20deeplink
```

### Issue 7: Deeplink Token Validation Failures
**Symptoms**:
```
Invalid or expired request token
```

**Root Cause**: Token generation/validation mismatch or expired tokens.

**Solution**:
```csharp
// Enhanced token validation with detailed logging
public async Task<SponsorRequest> ValidateRequestTokenAsync(string token)
{
    try
    {
        Console.WriteLine($"[TokenValidation] Attempting to validate token: {token?.Substring(0, 10)}...");
        
        // Find request by exact token match
        var request = await _sponsorRequestRepository.GetAsync(sr => sr.RequestToken == token);
        
        if (request == null)
        {
            Console.WriteLine("[TokenValidation] No request found with this token");
            return null;
        }
        
        Console.WriteLine($"[TokenValidation] Found request ID: {request.Id}, Status: {request.Status}");
        
        // Check expiry
        var expiryHours = _configuration.GetValue<int>("SponsorRequest:TokenExpiryHours", 24);
        var expiryTime = request.RequestDate.AddHours(expiryHours);
        
        if (DateTime.Now > expiryTime)
        {
            Console.WriteLine($"[TokenValidation] Token expired. Created: {request.RequestDate}, Expires: {expiryTime}");
            return null;
        }
        
        Console.WriteLine("[TokenValidation] Token validation successful");
        return request;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TokenValidation] Exception: {ex.Message}");
        return null;
    }
}
```

**Token Debugging Commands**:
```sql
-- Check token in database
SELECT Id, RequestToken, RequestDate, Status, 
       RequestDate + INTERVAL '24 hours' as ExpiryTime,
       NOW() as CurrentTime,
       CASE WHEN NOW() > RequestDate + INTERVAL '24 hours' THEN 'EXPIRED' ELSE 'VALID' END as TokenStatus
FROM SponsorRequests 
WHERE RequestToken LIKE 'abc123%';
```

## API Response Issues

### Issue 8: HTTP 500 Internal Server Error
**Symptoms**:
```
500 Internal Server Error on sponsor request endpoints
```

**Root Cause**: Unhandled exceptions in service layer or missing dependencies.

**Debugging Steps**:
```bash
# Check application logs
docker logs ziraai-api --tail 100

# Check specific service logs
grep "SponsorRequestService" /var/log/ziraai/app.log | tail -20

# Test service registration
curl -X GET "http://localhost:5000/health" -v
```

**Common Solutions**:
```csharp
// 1. Fix service registration in AutofacBusinessModule.cs
builder.RegisterType<SponsorRequestService>().As<ISponsorRequestService>().SingleInstance();
builder.RegisterType<SponsorRequestRepository>().As<ISponsorRequestRepository>();

// 2. Add missing HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 3. Verify MediatR registration
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
```

### Issue 9: Invalid Model State Errors
**Symptoms**:
```
400 Bad Request - The SponsorPhone field is required
```

**Root Cause**: Model validation failures or incorrect request format.

**Solution**:
```csharp
// Enhanced model validation
[HttpPost("create")]
public async Task<IActionResult> CreateRequest([FromBody] CreateSponsorRequestDto createSponsorRequestDto)
{
    if (!ModelState.IsValid)
    {
        var errors = ModelState
            .Where(x => x.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );
        
        return BadRequest(new ErrorResult($"Validation failed: {string.Join(", ", errors.SelectMany(e => e.Value))}"));
    }

    // Continue with processing...
}
```

**Request Format Validation**:
```json
// Correct request format
{
  "sponsorPhone": "+905551234567",  // Must include country code
  "requestMessage": "Custom message",
  "requestedTierId": 2               // Valid tier ID (1-4)
}

// Common mistakes to avoid
{
  "sponsorPhone": "05551234567",     // ❌ Missing country code
  "requestMessage": "",              // ❌ Empty message (will use default)
  "requestedTierId": 0               // ❌ Invalid tier ID
}
```

## Performance Issues

### Issue 10: Slow API Response Times
**Symptoms**:
- API responses taking >2 seconds
- Database query timeouts
- High CPU usage

**Diagnostic Commands**:
```bash
# Check API performance
curl -w "@curl-format.txt" -X POST "https://api.ziraai.com/api/sponsor-request/create" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"sponsorPhone":"+905551234567","requestMessage":"Test","requestedTierId":2}'

# Database query performance
EXPLAIN ANALYZE SELECT * FROM "SponsorRequests" 
WHERE "SponsorId" = 123 AND "Status" = 'Pending';
```

**Solutions**:

#### Database Optimization
```sql
-- Add missing indexes
CREATE INDEX IF NOT EXISTS "IX_SponsorRequests_Sponsor_Status" ON "SponsorRequests"("SponsorId", "Status");
CREATE INDEX IF NOT EXISTS "IX_SponsorRequests_Farmer_Status" ON "SponsorRequests"("FarmerId", "Status");
CREATE INDEX IF NOT EXISTS "IX_SponsorRequests_RequestDate" ON "SponsorRequests"("RequestDate" DESC);

-- Update table statistics
ANALYZE "SponsorRequests";
ANALYZE "SponsorContacts";
```

#### Repository Optimization
```csharp
// Optimize repository queries with proper includes
public async Task<List<SponsorRequest>> GetPendingRequestsForSponsorAsync(int sponsorId)
{
    return await Context.SponsorRequests
        .AsNoTracking() // Read-only optimization
        .Include(sr => sr.Farmer)
        .Where(sr => sr.SponsorId == sponsorId && sr.Status == "Pending")
        .OrderByDescending(sr => sr.RequestDate)
        .Take(50) // Limit results
        .ToListAsync();
}
```

#### Service Layer Optimization
```csharp
// Add caching for frequently accessed data
[CacheAspect(duration: 15)] // 15 minutes cache
public async Task<IDataResult<List<SponsorRequest>>> GetPendingRequestsAsync(int sponsorId)
{
    // Implementation with caching
}
```

### Issue 11: Memory Leaks and Resource Issues
**Symptoms**:
- Increasing memory usage over time
- OutOfMemoryException
- Database connection pool exhaustion

**Diagnostic Commands**:
```bash
# Monitor memory usage
dotnet-counters monitor --process-id $(pgrep -f "WebAPI.dll") --counters System.Runtime

# Check database connections
SELECT count(*) as active_connections FROM pg_stat_activity WHERE state = 'active';
```

**Solutions**:
```csharp
// Proper disposal in services
public class SponsorRequestService : ISponsorRequestService, IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Dispose managed resources
        }
        _disposed = true;
    }
}

// Configure connection pool limits
"DArchPgContext": "...;MinPoolSize=5;MaxPoolSize=50;Connection Lifetime=300;"
```

## Business Logic Issues

### Issue 12: Duplicate Request Creation
**Symptoms**:
```
409 Conflict - A pending request already exists for this sponsor
```

**Root Cause**: Race condition in request creation or incorrect duplicate detection.

**Solution**:
```csharp
// Enhanced duplicate detection with transaction
public async Task<IDataResult<string>> CreateRequestAsync(int farmerId, string sponsorPhone, string message, int tierId)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Lock for read to prevent race conditions
        var existingRequest = await _sponsorRequestRepository.GetAsync(
            sr => sr.FarmerId == farmerId && 
                  sr.SponsorId == sponsorId && 
                  (sr.Status == "Pending" || sr.Status == "Approved"),
            tracking: true); // Enable tracking for transaction
        
        if (existingRequest != null)
        {
            await transaction.RollbackAsync();
            return new ErrorDataResult<string>("A pending or approved request already exists for this sponsor");
        }

        // Create new request
        var sponsorRequest = new SponsorRequest { /* ... */ };
        _sponsorRequestRepository.Add(sponsorRequest);
        await _sponsorRequestRepository.SaveChangesAsync();
        
        await transaction.CommitAsync();
        
        // Generate result...
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Issue 13: Sponsorship Code Generation Conflicts
**Symptoms**:
```
Duplicate sponsorship code generated
```

**Root Cause**: Race condition in code generation or insufficient randomness.

**Solution**:
```csharp
private async Task<SponsorshipCode> GenerateSponsorshipCodeAsync(int sponsorId, int farmerId, int tierId)
{
    const int maxAttempts = 10;
    var random = new Random();
    
    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
        // More secure random generation
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var randomPart = random.Next(100000, 999999);
        var code = $"SP-{sponsorId}-{timestamp}-{randomPart}";
        
        // Check uniqueness with database lock
        var existingCode = await _sponsorshipCodeRepository.GetAsync(
            sc => sc.Code == code, 
            tracking: false);
        
        if (existingCode == null)
        {
            // Create and save immediately to claim the code
            var sponsorshipCode = new SponsorshipCode
            {
                Code = code,
                SponsorId = sponsorId,
                UsedByUserId = farmerId,
                SubscriptionTierId = tierId,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                IsActive = true,
                IsUsed = false
            };

            _sponsorshipCodeRepository.Add(sponsorshipCode);
            await _sponsorshipCodeRepository.SaveChangesAsync();
            
            return sponsorshipCode;
        }
    }
    
    throw new InvalidOperationException("Unable to generate unique sponsorship code after 10 attempts");
}
```

## Configuration Issues

### Issue 14: Missing Configuration Keys
**Symptoms**:
```
Configuration key 'SponsorRequest:TokenExpiryHours' not found
```

**Root Cause**: Missing or incorrectly named configuration keys.

**Solution**:
```csharp
// Robust configuration reading with defaults
public class SponsorRequestConfiguration
{
    public int TokenExpiryHours { get; set; } = 24;
    public int MaxRequestsPerDay { get; set; } = 10;
    public string DeepLinkBaseUrl { get; set; } = "https://ziraai.com/sponsor-request/";
    public string DefaultRequestMessage { get; set; } = "ZiraAI sponsor request";
    public string RequestTokenSecret { get; set; } = "default-secret-key";
}

// Service registration
builder.Services.Configure<SponsorRequestConfiguration>(
    builder.Configuration.GetSection("SponsorRequest"));

// Service usage
private readonly SponsorRequestConfiguration _config;
public SponsorRequestService(IOptions<SponsorRequestConfiguration> config)
{
    _config = config.Value;
}
```

**Configuration Validation**:
```csharp
// Startup validation
public static void ValidateConfiguration(IConfiguration configuration)
{
    var requiredKeys = new[]
    {
        "SponsorRequest:TokenExpiryHours",
        "SponsorRequest:DeepLinkBaseUrl", 
        "Security:RequestTokenSecret"
    };
    
    var missingKeys = requiredKeys.Where(key => 
        string.IsNullOrEmpty(configuration[key])).ToList();
    
    if (missingKeys.Any())
    {
        throw new InvalidOperationException(
            $"Missing required configuration keys: {string.Join(", ", missingKeys)}");
    }
}
```

### Issue 15: Environment-Specific Configuration Problems
**Symptoms**:
- Development URLs in production
- Wrong database connections
- Invalid secrets

**Solution**:
```bash
# Verify environment variables
echo $ASPNETCORE_ENVIRONMENT
echo $SPONSOR_REQUEST_SECRET

# Check configuration loading
curl -X GET "https://api.ziraai.com/api/configurations?key=SPONSOR_REQUEST_SECRET" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

**Configuration Testing Script**:
```powershell
# deploy/scripts/test_configuration.ps1
param([string]$Environment)

Write-Host "Testing configuration for $Environment environment..." -ForegroundColor Cyan

$configTests = @{
    "Database Connection" = { Test-Database-Connection }
    "JWT Configuration" = { Test-JWT-Config }
    "Sponsor Request Settings" = { Test-SponsorRequest-Config }
    "Security Settings" = { Test-Security-Config }
}

foreach ($test in $configTests.GetEnumerator()) {
    try {
        Write-Host "Testing $($test.Key)..." -ForegroundColor Yellow
        & $test.Value
        Write-Host "✅ $($test.Key) - PASSED" -ForegroundColor Green
    } catch {
        Write-Host "❌ $($test.Key) - FAILED: $($_.Exception.Message)" -ForegroundColor Red
    }
}
```

## Mobile App Integration Issues

### Issue 16: Deeplink Handling Problems
**Symptoms**:
- Mobile app doesn't receive deeplinks
- App crashes when processing deeplinks

**Flutter Solution**:
```dart
// pubspec.yaml - Add dependencies
dependencies:
  url_launcher: ^6.2.1
  uni_links: ^0.5.1

// main.dart - Configure deep links
import 'package:uni_links/uni_links.dart';

class _MyAppState extends State<MyApp> {
  StreamSubscription? _linkSubscription;

  @override
  void initState() {
    super.initState();
    _initDeepLinkListener();
  }

  void _initDeepLinkListener() {
    _linkSubscription = linkStream.listen((String link) {
      _handleDeepLink(link);
    }, onError: (err) {
      print('Deep link error: $err');
    });
  }

  void _handleDeepLink(String link) {
    // Parse sponsor request token from URL
    final uri = Uri.parse(link);
    if (uri.path.contains('/sponsor-request/')) {
      final token = uri.pathSegments.last;
      Navigator.pushNamed(context, '/sponsor-request-detail', arguments: token);
    }
  }

  @override
  void dispose() {
    _linkSubscription?.cancel();
    super.dispose();
  }
}
```

**React Native Solution**:
```javascript
// App.js - Configure deep links
import { Linking } from 'react-native';

class App extends Component {
  componentDidMount() {
    // Handle app launch from deep link
    Linking.getInitialURL().then((url) => {
      if (url) this.handleDeepLink(url);
    });

    // Handle deep links while app is running
    Linking.addEventListener('url', (event) => {
      this.handleDeepLink(event.url);
    });
  }

  handleDeepLink(url) {
    const route = url.replace(/.*?:\/\//g, '');
    if (route.includes('sponsor-request/')) {
      const token = route.split('/').pop();
      this.navigateToSponsorRequest(token);
    }
  }
}
```

### Issue 17: WhatsApp Integration Failures
**Symptoms**:
- WhatsApp doesn't open from mobile app
- Message format issues

**Solution**:
```javascript
// Robust WhatsApp integration
export const openWhatsApp = async (whatsappUrl) => {
  try {
    // Validate URL format
    if (!whatsappUrl.startsWith('https://wa.me/')) {
      throw new Error('Invalid WhatsApp URL format');
    }
    
    // Check if WhatsApp is installed
    const canOpen = await Linking.canOpenURL(whatsappUrl);
    if (!canOpen) {
      // Fallback to web WhatsApp
      const webUrl = whatsappUrl.replace('https://wa.me/', 'https://web.whatsapp.com/send?phone=');
      await Linking.openURL(webUrl);
    } else {
      await Linking.openURL(whatsappUrl);
    }
  } catch (error) {
    console.error('Failed to open WhatsApp:', error);
    // Show user-friendly error
    Alert.alert('Error', 'WhatsApp uygulaması açılamadı. Lütfen WhatsApp yüklü olduğundan emin olun.');
  }
};
```

## Monitoring and Logging Issues

### Issue 18: Missing or Incomplete Logs
**Symptoms**:
- No logs in Application Insights
- Missing error details
- Performance metrics not captured

**Solution**:
```csharp
// Enhanced logging configuration
public static class LoggingExtensions
{
    public static IServiceCollection AddSponsorRequestLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.AddApplicationInsights();
            
            // Custom structured logging
            builder.AddSerilog(new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/sponsor-request-.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
                .CreateLogger());
        });
        
        return services;
    }
}

// Service layer logging
[LogAspect(typeof(FileLogger))]
public async Task<IDataResult<string>> CreateRequestAsync(int farmerId, string sponsorPhone, string message, int tierId)
{
    var sw = Stopwatch.StartNew();
    
    try
    {
        _logger.LogInformation("Creating sponsor request: FarmerId={FarmerId}, SponsorPhone={SponsorPhone}, TierId={TierId}", 
            farmerId, sponsorPhone, tierId);
        
        // Implementation...
        
        _logger.LogInformation("Sponsor request created successfully: RequestId={RequestId}, Duration={Duration}ms", 
            sponsorRequest.Id, sw.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create sponsor request: FarmerId={FarmerId}, Duration={Duration}ms", 
            farmerId, sw.ElapsedMilliseconds);
        throw;
    }
    finally
    {
        sw.Stop();
    }
}
```

### Issue 19: Health Check Failures
**Symptoms**:
```
Health check failed: Service unavailable
```

**Root Cause**: Database connectivity issues or service dependency failures.

**Solution**:
```csharp
// Comprehensive health check
public class SponsorRequestHealthCheck : IHealthCheck
{
    private readonly ProjectDbContext _context;
    private readonly ISponsorRequestService _service;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connectivity
            await _context.Database.CanConnectAsync(cancellationToken);
            
            // Test service functionality
            var testToken = _service.GenerateRequestToken("+905551234567", "+905557654321", 1);
            if (string.IsNullOrEmpty(testToken))
            {
                return HealthCheckResult.Degraded("Token generation not working");
            }
            
            // Test configuration
            var config = _context.Database.GetConnectionString();
            if (string.IsNullOrEmpty(config))
            {
                return HealthCheckResult.Unhealthy("Database configuration missing");
            }
            
            return HealthCheckResult.Healthy("All systems operational");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}");
        }
    }
}
```

## Testing and Validation Issues

### Issue 20: Integration Test Failures
**Symptoms**:
- Tests fail in CI/CD but pass locally
- Authentication issues in tests
- Database state conflicts

**Solution**:
```csharp
// Robust integration test base
[TestClass]
public class SponsorRequestIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private string _farmerToken;
    private string _sponsorToken;

    [TestInitialize]
    public async Task Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Use test database
                builder.ConfigureServices(services =>
                {
                    // Remove production database
                    services.RemoveAll(typeof(DbContextOptions<ProjectDbContext>));
                    
                    // Add test database
                    services.AddDbContext<ProjectDbContext>(options =>
                        options.UseInMemoryDatabase("TestDatabase"));
                });
                
                builder.UseEnvironment("Testing");
            });

        _client = _factory.CreateClient();
        
        // Setup test data and authentication
        await SeedTestData();
        _farmerToken = await GenerateTestToken("Farmer");
        _sponsorToken = await GenerateTestToken("Sponsor");
    }

    [TestMethod]
    public async Task CreateRequest_WithValidData_ReturnsWhatsAppUrl()
    {
        // Arrange
        var request = new CreateSponsorRequestDto
        {
            SponsorPhone = "+905551234567",
            RequestMessage = "Test sponsor request",
            RequestedTierId = 2
        };

        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _farmerToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/sponsor-request/create", request);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<IDataResult<string>>();
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Data.StartsWith("https://wa.me/"));
    }
}
```

## Emergency Response Procedures

### Issue 21: System Down - Critical Failure
**Immediate Response**:
```bash
# 1. Check service status
curl -f "https://api.ziraai.com/health" || echo "Service DOWN"

# 2. Check database connectivity
psql -h ziraai-prod-db.postgres.database.azure.com -U ziraai -d ziraai_prod -c "SELECT 1;"

# 3. Check recent logs
az webapp log download --resource-group rg-ziraai-prod --name ziraai-prod-api

# 4. Rollback if necessary
az webapp deployment slot swap \
  --resource-group rg-ziraai-prod \
  --name ziraai-prod-api \
  --slot production \
  --target-slot staging
```

**Recovery Checklist**:
- [ ] Identify root cause from logs
- [ ] Check database integrity
- [ ] Verify configuration settings
- [ ] Test critical endpoints
- [ ] Monitor error rates
- [ ] Notify stakeholders

### Issue 22: Data Corruption Recovery
**Symptoms**:
- Inconsistent sponsor request states
- Missing sponsorship codes
- Foreign key violations

**Recovery Procedure**:
```sql
-- 1. Backup current state
pg_dump -h prod-db -U ziraai ziraai_prod > emergency_backup_$(date +%Y%m%d_%H%M%S).sql

-- 2. Identify corrupted data
SELECT sr.Id, sr.Status, sr.GeneratedSponsorshipCode, sc.Code as ActualCode
FROM SponsorRequests sr
LEFT JOIN SponsorshipCodes sc ON sr.GeneratedSponsorshipCode = sc.Code
WHERE sr.Status = 'Approved' AND sc.Code IS NULL;

-- 3. Fix missing sponsorship codes
UPDATE SponsorRequests 
SET Status = 'Pending', 
    ApprovalDate = NULL, 
    GeneratedSponsorshipCode = NULL
WHERE Status = 'Approved' 
  AND GeneratedSponsorshipCode NOT IN (SELECT Code FROM SponsorshipCodes);

-- 4. Verify integrity
SELECT COUNT(*) FROM SponsorRequests WHERE Status = 'Approved' AND GeneratedSponsorshipCode IS NULL;
```

## Performance Monitoring

### Monitoring Dashboard Queries
```sql
-- Request volume by hour
SELECT 
    DATE_TRUNC('hour', "RequestDate") as hour,
    COUNT(*) as request_count,
    COUNT(CASE WHEN "Status" = 'Approved' THEN 1 END) as approved_count
FROM "SponsorRequests" 
WHERE "RequestDate" >= NOW() - INTERVAL '24 hours'
GROUP BY DATE_TRUNC('hour', "RequestDate")
ORDER BY hour;

-- Top sponsors by request volume
SELECT 
    s."FullName" as sponsor_name,
    COUNT(sr."Id") as total_requests,
    COUNT(CASE WHEN sr."Status" = 'Approved' THEN 1 END) as approved_requests,
    ROUND(AVG(EXTRACT(EPOCH FROM (sr."ApprovalDate" - sr."RequestDate"))/60)) as avg_response_minutes
FROM "SponsorRequests" sr
JOIN "Users" s ON sr."SponsorId" = s."UserId"
WHERE sr."RequestDate" >= NOW() - INTERVAL '30 days'
GROUP BY s."UserId", s."FullName"
ORDER BY total_requests DESC
LIMIT 10;
```

### Alerting Rules
```yaml
# Azure Monitor Alert Rules
- name: "High Error Rate"
  condition: "requests/failed > 10 over 5 minutes"
  action: "Send email to devops team"

- name: "Slow Response Time" 
  condition: "requests/duration > 2000ms for 95th percentile over 10 minutes"
  action: "Send Slack notification"

- name: "Database Connection Failures"
  condition: "dependencies/failed > 5 over 1 minute"
  action: "Page on-call engineer"
```

## Support Scripts

### Debug Information Script
Create `deploy/scripts/debug_sponsor_request.ps1`:

```powershell
param(
    [int]$RequestId,
    [string]$Token,
    [string]$BaseUrl = "https://api.ziraai.com"
)

Write-Host "🔍 Debugging Sponsor Request System" -ForegroundColor Cyan

if ($RequestId) {
    Write-Host "Debugging Request ID: $RequestId" -ForegroundColor Yellow
    
    # Get request details from database
    $query = @"
    SELECT sr.*, 
           f."FullName" as farmer_name, 
           s."FullName" as sponsor_name,
           st."DisplayName" as tier_name
    FROM "SponsorRequests" sr
    LEFT JOIN "Users" f ON sr."FarmerId" = f."UserId"  
    LEFT JOIN "Users" s ON sr."SponsorId" = s."UserId"
    LEFT JOIN "SubscriptionTiers" st ON sr."ApprovedSubscriptionTierId" = st."Id"
    WHERE sr."Id" = $RequestId;
"@
    
    Write-Host "Query executed: $query" -ForegroundColor Gray
}

if ($Token) {
    Write-Host "Testing Token: $($Token.Substring(0, 10))..." -ForegroundColor Yellow
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/sponsor-request/process/$Token" -Method GET
        Write-Host "✅ Token validation successful" -ForegroundColor Green
        Write-Host "Request Status: $($response.data.status)" -ForegroundColor White
    } catch {
        Write-Host "❌ Token validation failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "🎉 Debug completed!" -ForegroundColor Green
```

This comprehensive troubleshooting guide covers all major issues and provides practical solutions for maintaining and debugging the WhatsApp Sponsor Request System in production.