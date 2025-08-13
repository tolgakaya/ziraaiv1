# ZiraAI - Rol ve Yetki Sistemi Kullanım Kılavuzu

## 📋 İçindekiler
1. [Sistem Mimarisi](#sistem-mimarisi)
2. [Roller ve Sorumlulukları](#roller-ve-sorumlulukları)
3. [Authentication Flow](#authentication-flow)
4. [Authorization Mechanisms](#authorization-mechanisms)
5. [Subscription Integration](#subscription-integration)
6. [Implementation Örnekleri](#implementation-örnekleri)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)

---

## 🏗️ Sistem Mimarisi

### Multi-Layer Authorization Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  API Controller Layer                   │
│           [Authorize], [Authorize(Roles)]              │
├─────────────────────────────────────────────────────────┤
│                Business Handler Layer                   │
│              [SecuredOperation] Aspect                  │
├─────────────────────────────────────────────────────────┤
│                Service Layer Security                   │
│            Subscription Validation Service              │
├─────────────────────────────────────────────────────────┤
│                  Data Access Layer                      │
│              Entity-Based Permissions                   │
└─────────────────────────────────────────────────────────┘
```

### Core Components

#### 1. **Entity Model**
- `User.cs` - Temel kullanıcı bilgileri
- `Group.cs` - Rol grupları
- `OperationClaim.cs` - İşlem yetkileri
- `UserGroup.cs` - Kullanıcı-Grup ilişkisi
- `UserOperationClaim.cs` - Kullanıcı-Yetki ilişkisi
- `SubscriptionTier.cs` - Abonelik seviyeleri
- `UserSubscription.cs` - Kullanıcı abonelikleri

#### 2. **Security Components**
- `SecuredOperation.cs` - Method-level yetkilendirme
- `JwtHelper.cs` - JWT token yönetimi
- `ClaimExtensions.cs` - Claims yardımcı metodlar

---

## 👥 Roller ve Sorumlulukları

### 🌱 **Farmer (Çiftçi)**

#### **Temel Özellikler**
- **Primary Role**: Plant analysis kullanıcısı
- **Database Identifier**: Group = "Farmer" veya Claim = "FarmerAccess"
- **Subscription**: Mandatory (S, M, L, XL tierlarından biri)

#### **Yetkiler**
```csharp
✅ Plant Analysis Create (sync/async)
✅ Own Analysis View & Download
✅ Profile Management
✅ Subscription Management
✅ Usage Status Monitoring
❌ Admin Functions
❌ Other Users' Data
❌ System Configuration
```

#### **API Endpoints Access**
```http
POST /api/plantanalyses/analyze              ✅ (with subscription)
POST /api/plantanalyses/analyze-async        ✅ (with subscription)
GET  /api/plantanalyses/{id}                 ✅ (own analyses only)
GET  /api/plantanalyses/{id}/image           ✅ (own analyses only)
GET  /api/subscriptions/my-subscription      ✅
GET  /api/subscriptions/usage-status         ✅
POST /api/subscriptions/subscribe            ✅
POST /api/subscriptions/cancel               ✅
PUT  /api/users/{id}                         ✅ (own profile only)
```

#### **Subscription Validation**
```csharp
// Her plant analysis isteği öncesi
if (!hasActiveSubscription) 
    return "Subscription required";
if (dailyUsage >= dailyLimit) 
    return "Daily limit exceeded";
if (monthlyUsage >= monthlyLimit) 
    return "Monthly limit exceeded";
```

### 💰 **Sponsor**

#### **Temel Özellikler**
- **Primary Role**: Analysis sponsor ve funding
- **Database Identifier**: Group = "Sponsor"
- **Special Features**: SponsorId tracking, sponsored analyses view

#### **Yetkiler**
```csharp
✅ View Sponsored Analyses
✅ Sponsorship Reports
✅ Profile Management
❌ Create Plant Analysis (unless also Farmer)
❌ Admin Functions
❌ Non-Sponsored Data Access
```

#### **API Endpoints Access**
```http
GET  /api/plantanalyses/sponsored-analyses   ✅ (sponsored only)
GET  /api/subscriptions/sponsored-analyses   ✅
PUT  /api/users/{id}                         ✅ (own profile only)
```

### 👑 **Admin**

#### **Temel Özellikler**
- **Primary Role**: System administrator
- **Database Identifier**: Group = "Admin" or Claims = "AdminAccess"
- **Subscription**: Not required (unlimited access)

#### **Yetkiler**
```csharp
✅ ALL Plant Analyses (View, Manage, Delete)
✅ User Management (CRUD)
✅ Subscription Management (All Users)
✅ System Configuration
✅ Usage Analytics & Reporting  
✅ N8N Integration Management
✅ Database Operations
```

#### **API Endpoints Access**
```http
GET  /api/plantanalyses                      ✅ (all analyses)
GET  /api/plantanalyses/sponsored-analyses   ✅ (all sponsored)
GET  /api/users                              ✅ (all users)
PUT  /api/users/{id}                         ✅ (any user)
DELETE /api/users/{id}                       ✅
GET  /api/subscriptions/usage-logs           ✅
PUT  /api/subscriptions/tiers/{id}           ✅
GET  /api/configurations                     ✅
PUT  /api/configurations/{id}                ✅
```

---

## 🔐 Authentication Flow

### 1. **Registration Process**

```mermaid
sequenceDiagram
    participant C as Client
    participant A as API
    participant D as Database
    
    C->>A: POST /api/v1/auth/register
    A->>A: Validate Data
    A->>A: Hash Password
    A->>D: Create User Entity
    A->>D: Assign Default Role (Farmer)
    A->>C: Registration Success
```

#### **Registration Payload**
```json
{
  "fullName": "John Farmer",
  "email": "farmer@example.com",
  "password": "SecurePassword123!",
  "citizenId": 12345678901,
  "mobilePhones": "+90 555 123 4567",
  "address": "Antalya, Turkey"
}
```

#### **Default Role Assignment**
```csharp
// RegisterUserCommandHandler.cs
var user = new User { /* user data */ };
await _userRepository.Add(user);

// Assign default Farmer role
var farmerGroup = await _groupRepository.GetByName("Farmer");
var userGroup = new UserGroup 
{ 
    UserId = user.UserId, 
    GroupId = farmerGroup.Id 
};
await _userGroupRepository.Add(userGroup);
```

### 2. **Login Process**

```mermaid
sequenceDiagram
    participant C as Client
    participant A as API
    participant DB as Database
    participant Cache as Redis
    
    C->>A: POST /api/v1/auth/login
    A->>DB: Validate Credentials
    A->>DB: Get User Roles & Claims
    A->>Cache: Cache User Claims
    A->>A: Generate JWT Token
    A->>C: Return Token + User Info
```

#### **JWT Token Structure**
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "nameidentifier": "123",
    "email": "farmer@example.com",
    "role": ["Farmer"],
    "claims": ["PlantAnalysisCreate", "PlantAnalysisRead"],
    "subscription": "M",
    "exp": 1692022800,
    "iss": "ZiraAI",
    "aud": "ZiraAI-API"
  }
}
```

### 3. **Token Validation Process**

```csharp
// Startup.cs - JWT Configuration
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
    });
```

---

## 🛡️ Authorization Mechanisms

### 1. **Controller Level Authorization**

#### **Basic Authentication**
```csharp
[HttpPost("analyze")]
[Authorize] // Requires valid JWT token
public async Task<IActionResult> Analyze([FromBody] PlantAnalysisRequestDto request)
{
    // Any authenticated user can access
}
```

#### **Role-Based Authorization**
```csharp
[HttpGet]
[Authorize(Roles = "Admin")] // Only Admin role
public async Task<IActionResult> GetAll()
{
    // Only admins can access
}

[HttpGet("sponsored-analyses")]
[Authorize(Roles = "Admin,Sponsor")] // Multiple roles
public async Task<IActionResult> GetSponsoredAnalyses()
{
    // Admins or Sponsors can access
}
```

### 2. **Method Level Security (Business Layer)**

#### **SecuredOperation Aspect**
```csharp
// Business/BusinessAspects/SecuredOperation.cs
public class SecuredOperation : MethodInterception
{
    protected override void OnBefore(IInvocation invocation)
    {
        var userId = _httpContextAccessor.HttpContext?.User.Claims
            .FirstOrDefault(x => x.Type.EndsWith("nameidentifier"))?.Value;

        if (userId == null)
            throw new SecurityException(Messages.AuthorizationsDenied);

        // Check cached operation claims
        var oprClaims = _cacheManager.Get<IEnumerable<string>>($"{CacheKeys.UserIdForClaim}={userId}");
        var operationName = invocation.TargetType.ReflectedType.Name;
        
        if (!oprClaims.Contains(operationName))
            throw new SecurityException(Messages.AuthorizationsDenied);
    }
}
```

#### **Usage in Business Handlers**
```csharp
[SecuredOperation] // Checks operation claims
public class CreatePlantAnalysisCommandHandler : IRequestHandler<CreatePlantAnalysisCommand, IResult>
{
    public async Task<IResult> Handle(CreatePlantAnalysisCommand request, CancellationToken cancellationToken)
    {
        // Handler implementation
        // User already verified to have "CreatePlantAnalysisCommandHandler" claim
    }
}
```

### 3. **Dynamic Claims Management**

#### **Operation Claims Setup**
```csharp
// Business/Helpers/OperationClaimCreatorMiddleware.cs
public static class OperationClaimCreatorMiddleware
{
    public static async Task<List<OperationClaim>> GetRequiredClaims()
    {
        return new List<OperationClaim>
        {
            new() { Name = "PlantAnalysisCreate", Alias = "plant.analysis.create" },
            new() { Name = "PlantAnalysisRead", Alias = "plant.analysis.read" },
            new() { Name = "UserManagement", Alias = "user.management" },
            new() { Name = "AdminPanel", Alias = "admin.panel" },
            new() { Name = "SubscriptionManagement", Alias = "subscription.management" }
        };
    }
}
```

#### **Claims Cache Management**
```csharp
// Cache key format: "UserIdForClaim=123"
var cacheKey = $"{CacheKeys.UserIdForClaim}={userId}";
var userClaims = await _cacheManager.GetAsync<List<string>>(cacheKey, async () =>
{
    // Load user claims from database
    return await GetUserOperationClaimsAsync(userId);
});
```

---

## 💳 Subscription Integration

### 1. **Subscription Validation Service**

```csharp
public class SubscriptionValidationService
{
    public async Task<SubscriptionValidationResult> ValidateSubscriptionAsync(int userId)
    {
        var subscription = await GetActiveSubscriptionAsync(userId);
        
        if (subscription == null)
            return new SubscriptionValidationResult 
            { 
                IsValid = false, 
                Message = "No active subscription found" 
            };

        if (subscription.CurrentDailyUsage >= subscription.SubscriptionTier.DailyRequestLimit)
            return new SubscriptionValidationResult 
            { 
                IsValid = false, 
                Message = "Daily request limit exceeded" 
            };

        if (subscription.CurrentMonthlyUsage >= subscription.SubscriptionTier.MonthlyRequestLimit)
            return new SubscriptionValidationResult 
            { 
                IsValid = false, 
                Message = "Monthly request limit exceeded" 
            };

        return new SubscriptionValidationResult { IsValid = true };
    }
}
```

### 2. **Pre-Request Subscription Check**

```csharp
[HttpPost("analyze")]
[Authorize] // First check authentication
public async Task<IActionResult> Analyze([FromBody] PlantAnalysisRequestDto request)
{
    var userId = HttpContext.User.GetUserId();
    
    // Subscription validation
    var subscriptionValidation = await _subscriptionService.ValidateSubscriptionAsync(userId);
    if (!subscriptionValidation.IsValid)
    {
        return BadRequest(new ErrorResult(subscriptionValidation.Message)
        {
            SubscriptionStatus = subscriptionValidation.SubscriptionStatus
        });
    }
    
    // Process plant analysis
    var result = await _mediator.Send(new CreatePlantAnalysisCommand(request));
    
    // Increment usage counter after successful processing
    await _subscriptionService.IncrementUsageAsync(userId);
    
    return Ok(result);
}
```

### 3. **Usage Tracking Implementation**

```csharp
public class SubscriptionUsageTracker
{
    public async Task IncrementUsageAsync(int userId, string endpoint, bool isSuccessful)
    {
        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
        if (subscription != null)
        {
            // Update counters
            subscription.CurrentDailyUsage++;
            subscription.CurrentMonthlyUsage++;
            await _subscriptionRepository.UpdateAsync(subscription);
            
            // Log usage for analytics
            await _usageLogRepository.AddAsync(new SubscriptionUsageLog
            {
                UserId = userId,
                UserSubscriptionId = subscription.Id,
                UsageDate = DateTime.UtcNow,
                RequestEndpoint = endpoint,
                IsSuccessful = isSuccessful,
                DailyQuotaUsed = subscription.CurrentDailyUsage,
                DailyQuotaLimit = subscription.SubscriptionTier.DailyRequestLimit,
                MonthlyQuotaUsed = subscription.CurrentMonthlyUsage,
                MonthlyQuotaLimit = subscription.SubscriptionTier.MonthlyRequestLimit
            });
        }
    }
}
```

---

## 💡 Implementation Örnekleri

### 1. **Custom Authorization Attribute**

```csharp
public class SubscriptionRequiredAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var subscriptionService = context.HttpContext.RequestServices
            .GetService<ISubscriptionValidationService>();
        
        var userId = user.GetUserId();
        var validation = subscriptionService.ValidateSubscriptionAsync(userId).Result;
        
        if (!validation.IsValid)
        {
            context.Result = new BadRequestObjectResult(new
            {
                success = false,
                message = validation.Message,
                subscriptionStatus = validation.SubscriptionStatus
            });
        }
    }
}
```

#### **Usage:**
```csharp
[HttpPost("analyze")]
[Authorize]
[SubscriptionRequired] // Custom attribute
public async Task<IActionResult> Analyze([FromBody] PlantAnalysisRequestDto request)
{
    // Subscription already validated
}
```

### 2. **Role-Based Data Filtering**

```csharp
public class PlantAnalysisQueryHandler : IRequestHandler<GetPlantAnalysesQuery, IDataResult<List<PlantAnalysisDto>>>
{
    public async Task<IDataResult<List<PlantAnalysisDto>>> Handle(GetPlantAnalysesQuery request)
    {
        var userId = _httpContextAccessor.HttpContext.User.GetUserId();
        var userRoles = _httpContextAccessor.HttpContext.User.GetRoles();
        
        IQueryable<PlantAnalysis> query = _repository.Query();
        
        // Admin sees everything
        if (!userRoles.Contains("Admin"))
        {
            if (userRoles.Contains("Farmer"))
            {
                // Farmers see only their analyses
                query = query.Where(x => x.UserId == userId);
            }
            else if (userRoles.Contains("Sponsor"))
            {
                // Sponsors see only sponsored analyses
                var sponsorId = _httpContextAccessor.HttpContext.User.GetSponsorId();
                query = query.Where(x => x.SponsorId == sponsorId);
            }
            else
            {
                return new ErrorDataResult<List<PlantAnalysisDto>>("Access denied");
            }
        }
        
        var results = await query.ToListAsync();
        return new SuccessDataResult<List<PlantAnalysisDto>>(results.ToDto());
    }
}
```

### 3. **Multi-Role User Support**

```csharp
public static class UserExtensions
{
    public static List<string> GetRoles(this ClaimsPrincipal user)
    {
        return user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
    }
    
    public static bool HasRole(this ClaimsPrincipal user, string role)
    {
        return user.IsInRole(role);
    }
    
    public static bool HasAnyRole(this ClaimsPrincipal user, params string[] roles)
    {
        return roles.Any(role => user.IsInRole(role));
    }
    
    public static bool CanAccessAnalysis(this ClaimsPrincipal user, PlantAnalysis analysis)
    {
        var userId = user.GetUserId();
        var roles = user.GetRoles();
        
        // Admin can access everything
        if (roles.Contains("Admin"))
            return true;
            
        // Farmers can access their own analyses
        if (roles.Contains("Farmer") && analysis.UserId == userId)
            return true;
            
        // Sponsors can access sponsored analyses
        if (roles.Contains("Sponsor"))
        {
            var sponsorId = user.GetSponsorId();
            return analysis.SponsorId == sponsorId;
        }
        
        return false;
    }
}
```

---

## 📝 Best Practices

### 1. **Security Best Practices**

#### **Password Policies**
```csharp
public static class PasswordPolicy
{
    public static bool IsValid(string password)
    {
        return password.Length >= 8 &&
               password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(c => "!@#$%^&*".Contains(c));
    }
}
```

#### **Secure Token Management**
```csharp
// Token expiration times
public static class TokenSettings
{
    public const int AccessTokenExpiryMinutes = 60;
    public const int RefreshTokenExpiryMinutes = 180;
    public const int PasswordResetTokenExpiryMinutes = 30;
}

// Refresh token rotation
public async Task<AccessToken> RefreshTokenAsync(string refreshToken)
{
    var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
    if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
        throw new SecurityException("Invalid refresh token");
    
    // Generate new token pair
    var newAccessToken = _jwtHelper.CreateToken(user);
    var newRefreshToken = Guid.NewGuid().ToString();
    
    // Update user with new refresh token
    user.RefreshToken = newRefreshToken;
    user.RefreshTokenExpiry = DateTime.UtcNow.AddMinutes(TokenSettings.RefreshTokenExpiryMinutes);
    await _userRepository.UpdateAsync(user);
    
    return newAccessToken;
}
```

### 2. **Performance Optimization**

#### **Claims Caching Strategy**
```csharp
public class OptimizedClaimsService
{
    private const int CACHE_EXPIRY_MINUTES = 15;
    
    public async Task<List<string>> GetUserClaimsAsync(int userId)
    {
        var cacheKey = $"user_claims_{userId}";
        
        return await _cacheManager.GetAsync(cacheKey, async () =>
        {
            var userClaims = await _userClaimRepository.GetByUserIdAsync(userId);
            var groupClaims = await _groupClaimRepository.GetByUserIdAsync(userId);
            
            return userClaims.Concat(groupClaims)
                           .Select(c => c.OperationClaim.Name)
                           .Distinct()
                           .ToList();
        }, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
    }
}
```

#### **Database Query Optimization**
```csharp
public async Task<List<PlantAnalysisDto>> GetUserAnalysesOptimizedAsync(int userId)
{
    return await _context.PlantAnalyses
        .Where(p => p.UserId == userId)
        .Select(p => new PlantAnalysisDto
        {
            Id = p.Id,
            AnalysisId = p.AnalysisId,
            CropType = p.CropType,
            AnalysisDate = p.AnalysisDate,
            Status = p.Status,
            // Only select needed fields
            Summary = new SummaryDto
            {
                OverallHealthScore = p.OverallHealthScore,
                PrimaryConcern = p.PrimaryConcern
            }
        })
        .Take(100) // Limit results
        .ToListAsync();
}
```

### 3. **Error Handling Best Practices**

#### **Consistent Error Responses**
```csharp
public class AuthorizationException : Exception
{
    public AuthorizationError ErrorType { get; }
    public object AdditionalData { get; }
    
    public AuthorizationException(AuthorizationError errorType, string message, object additionalData = null) 
        : base(message)
    {
        ErrorType = errorType;
        AdditionalData = additionalData;
    }
}

public enum AuthorizationError
{
    InvalidToken,
    InsufficientRoles,
    SubscriptionRequired,
    QuotaExceeded,
    ResourceNotFound,
    AccessDenied
}
```

#### **Global Exception Handler**
```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (AuthorizationException ex)
        {
            await HandleAuthorizationExceptionAsync(context, ex);
        }
        catch (SecurityException ex)
        {
            await HandleSecurityExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleAuthorizationExceptionAsync(HttpContext context, AuthorizationException ex)
    {
        context.Response.StatusCode = ex.ErrorType switch
        {
            AuthorizationError.InvalidToken => 401,
            AuthorizationError.InsufficientRoles => 403,
            AuthorizationError.SubscriptionRequired => 402,
            AuthorizationError.QuotaExceeded => 429,
            AuthorizationError.ResourceNotFound => 404,
            _ => 403
        };
        
        var response = new
        {
            success = false,
            message = ex.Message,
            errorType = ex.ErrorType.ToString(),
            additionalData = ex.AdditionalData,
            timestamp = DateTime.UtcNow
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

---

## 🔧 Troubleshooting

### 1. **Common Authentication Issues**

#### **Token Expired**
```
Error: "Token has expired"
Solution: Use refresh token to get new access token
```
```csharp
// Client-side token refresh logic
if (response.status === 401 && response.data?.message?.includes("expired")) {
    const refreshResponse = await refreshToken();
    if (refreshResponse.success) {
        // Retry original request with new token
        return await retryOriginalRequest(newToken);
    }
}
```

#### **Invalid Token Format**
```
Error: "SecurityTokenMalformedException"
Solution: Check Authorization header format
```
```http
❌ Wrong: Authorization: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
✅ Correct: Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 2. **Authorization Issues**

#### **Insufficient Roles**
```
Error: "User does not have required role"
Debug Steps:
1. Check user roles in JWT token payload
2. Verify role assignments in database
3. Check cache expiry for user claims
```

#### **Operation Claims Missing**
```
Error: "Operation claim not found"
Debug Steps:
1. Check if OperationClaimCreatorMiddleware is running
2. Verify operation claims are created in database
3. Check user-claim assignments
4. Clear claims cache and retry
```

### 3. **Subscription Issues**

#### **Quota Exceeded**
```
Error: "Daily/Monthly request limit reached"
Debug Steps:
1. Check subscription tier limits
2. Verify usage counters in database
3. Check if usage reset jobs are running
4. Manually reset counters if needed
```

#### **No Active Subscription**
```
Error: "You need an active subscription"
Debug Steps:
1. Check subscription status and end date
2. Verify payment status
3. Check if subscription expired
4. Assign trial subscription for testing
```

### 4. **Debugging Tools**

#### **JWT Token Decoder**
```csharp
[HttpGet("debug/token")]
[Authorize]
public IActionResult DebugToken()
{
    var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
    var decodedToken = _jwtHelper.DecodeToken(token);
    
    return Ok(new
    {
        userId = User.GetUserId(),
        roles = User.GetRoles(),
        claims = User.Claims.Select(c => new { c.Type, c.Value }),
        tokenExpiry = decodedToken.ValidTo,
        subscription = User.GetSubscriptionTier()
    });
}
```

#### **User Permissions Checker**
```csharp
[HttpGet("debug/permissions")]
[Authorize]
public async Task<IActionResult> CheckPermissions()
{
    var userId = User.GetUserId();
    var userClaims = await _claimsService.GetUserClaimsAsync(userId);
    var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);
    
    return Ok(new
    {
        userId,
        roles = User.GetRoles(),
        operationClaims = userClaims,
        subscription = subscription?.SubscriptionTier?.TierName,
        dailyUsage = subscription?.CurrentDailyUsage,
        dailyLimit = subscription?.SubscriptionTier?.DailyRequestLimit,
        canMakeRequest = subscription != null && 
                        subscription.CurrentDailyUsage < subscription.SubscriptionTier.DailyRequestLimit
    });
}
```

Bu kılavuz ile ZiraAI sistemindeki rol ve yetki yönetimini tam olarak anlayabilir ve sorun yaşadığınızda debug edebilirsiniz.