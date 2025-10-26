# Tier Permission Refactoring Proposal

**Problem:** Hard-coded tier permissions scattered across multiple services  
**Solution:** Database-driven, centralized permission system  
**Benefits:** No code changes for permission updates, admin UI support, audit trail

---

## 1. Current Architecture Problems

### Problem 1: Hard-Coded Tier Checks

**Current Code (Scattered):**
```csharp
// AnalysisMessagingService.cs:68
if (purchase.SubscriptionTierId >= 4) // Hard-coded: L and XL

// SmartLinkService.cs:45
if (purchase.SubscriptionTierId == 5) // Hard-coded: XL only

// SponsorVisibilityService.cs
if (tier.TierName == "M" || tier.TierName == "L" || tier.TierName == "XL") // Hard-coded list

// SponsorDataAccessService.cs
var accessPercentage = purchase.SubscriptionTierId switch
{
    2 => 30,  // S tier
    3 => 60,  // M tier
    4 => 100, // L tier
    5 => 100, // XL tier
    _ => 0
}; // Hard-coded percentages
```

**What Happens When L Tier Changes?**
- Find all tier checks manually
- Update each file separately
- Risk of missing some checks
- Requires code deployment
- Potential downtime
- No audit trail

---

### Problem 2: Inconsistent Feature Naming

```csharp
// Service A calls it "Messaging"
if (canMessage) { }

// Service B calls it "SendMessage" 
if (canSendMessage) { }

// Controller calls it "messaging_enabled"
```

No central feature registry!

---

### Problem 3: No Dynamic Permission Updates

**Scenario:** Marketing wants to give L tier users Smart Links for 1 week promotion

**Current System:**
1. Developer changes `SmartLinkService.cs:45`
2. Change `== 5` to `>= 4`
3. Commit, build, test
4. Deploy to production
5. **1 week later:** Revert changes, deploy again

**Needed System:**
1. Admin opens dashboard
2. Enables "Smart Links" for L tier
3. Sets expiry date: 7 days
4. **Automatic revert** after expiry

---

## 2. Proposed Architecture: Database-Driven Permissions

### 2.1. New Entities

#### TierFeature (Junction Table)

```csharp
namespace Entities.Concrete
{
    public class TierFeature : IEntity
    {
        public int Id { get; set; }
        public int SubscriptionTierId { get; set; }
        public int FeatureId { get; set; }
        
        // Configuration
        public bool IsEnabled { get; set; } = true;
        public string ConfigurationJson { get; set; } // Feature-specific config
        
        // Scheduling
        public DateTime? EffectiveDate { get; set; }  // When to activate
        public DateTime? ExpiryDate { get; set; }     // When to deactivate
        
        // Audit
        public DateTime CreatedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedByUserId { get; set; }
        
        // Navigation Properties
        public virtual SubscriptionTier SubscriptionTier { get; set; }
        public virtual Feature Feature { get; set; }
    }
}
```

#### Feature (Feature Registry)

```csharp
namespace Entities.Concrete
{
    public class Feature : IEntity
    {
        public int Id { get; set; }
        public string FeatureKey { get; set; }      // "messaging", "smart_links"
        public string DisplayName { get; set; }     // "Messaging"
        public string Description { get; set; }     // "Send messages to farmers"
        public string Category { get; set; }        // "Communication", "Analytics"
        
        // Feature Metadata
        public string DefaultConfigJson { get; set; } // Default configuration
        public bool RequiresConfiguration { get; set; } = false;
        public string ConfigurationSchema { get; set; } // JSON schema for validation
        
        // Status
        public bool IsActive { get; set; } = true;
        public bool IsDeprecated { get; set; } = false;
        
        // Audit
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        
        // Navigation Properties
        public virtual ICollection<TierFeature> TierFeatures { get; set; }
    }
}
```

---

### 2.2. Feature Seed Data

```csharp
namespace Business.Seeds
{
    public static class FeatureSeeds
    {
        public static List<Feature> GetDefaultFeatures()
        {
            return new List<Feature>
            {
                new Feature
                {
                    Id = 1,
                    FeatureKey = "messaging",
                    DisplayName = "Messaging",
                    Description = "Send messages to farmers about their analyses",
                    Category = "Communication",
                    RequiresConfiguration = false
                },
                new Feature
                {
                    Id = 2,
                    FeatureKey = "voice_messages",
                    DisplayName = "Voice Messages",
                    Description = "Send voice messages to farmers",
                    Category = "Communication",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"maxDurationSeconds\": 300, \"maxFileSizeMB\": 10}"
                },
                new Feature
                {
                    Id = 3,
                    FeatureKey = "smart_links",
                    DisplayName = "Smart Links",
                    Description = "Create and manage smart links for product promotion",
                    Category = "Marketing",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"maxLinksPerSponsor\": 50, \"requiresApproval\": false}"
                },
                new Feature
                {
                    Id = 4,
                    FeatureKey = "advanced_analytics",
                    DisplayName = "Advanced Analytics",
                    Description = "Access to advanced analytics dashboards",
                    Category = "Analytics",
                    RequiresConfiguration = false
                },
                new Feature
                {
                    Id = 5,
                    FeatureKey = "api_access",
                    DisplayName = "API Access",
                    Description = "Programmatic access to ZiraAI API",
                    Category = "Integration",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"rateLimit\": 1000, \"rateLimitWindow\": \"hour\"}"
                },
                new Feature
                {
                    Id = 6,
                    FeatureKey = "sponsor_visibility",
                    DisplayName = "Sponsor Visibility",
                    Description = "Show sponsor logo and profile to farmers",
                    Category = "Branding",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"logoVisibility\": true, \"profileVisibility\": true}"
                },
                new Feature
                {
                    Id = 7,
                    FeatureKey = "data_access_percentage",
                    DisplayName = "Farmer Data Access",
                    Description = "Percentage of farmer data accessible to sponsor",
                    Category = "Data",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"accessPercentage\": 100}"
                },
                new Feature
                {
                    Id = 8,
                    FeatureKey = "priority_support",
                    DisplayName = "Priority Support",
                    Description = "Faster response times for support requests",
                    Category = "Support",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"responseTimeHours\": 12}"
                }
            };
        }
        
        public static List<TierFeature> GetDefaultTierFeatures()
        {
            return new List<TierFeature>
            {
                // Trial Tier (ID=1) - No premium features
                
                // S Tier (ID=2)
                new TierFeature 
                { 
                    SubscriptionTierId = 2, 
                    FeatureId = 7, // data_access_percentage
                    IsEnabled = true,
                    ConfigurationJson = "{\"accessPercentage\": 30}"
                },
                
                // M Tier (ID=3)
                new TierFeature 
                { 
                    SubscriptionTierId = 3, 
                    FeatureId = 4, // advanced_analytics
                    IsEnabled = true
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 3, 
                    FeatureId = 6, // sponsor_visibility
                    IsEnabled = true,
                    ConfigurationJson = "{\"logoVisibility\": true, \"profileVisibility\": false}"
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 3, 
                    FeatureId = 7, // data_access_percentage
                    IsEnabled = true,
                    ConfigurationJson = "{\"accessPercentage\": 60}"
                },
                
                // L Tier (ID=4)
                new TierFeature 
                { 
                    SubscriptionTierId = 4, 
                    FeatureId = 1, // messaging
                    IsEnabled = true
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 4, 
                    FeatureId = 4, // advanced_analytics
                    IsEnabled = true
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 4, 
                    FeatureId = 5, // api_access
                    IsEnabled = true,
                    ConfigurationJson = "{\"rateLimit\": 1000, \"rateLimitWindow\": \"hour\"}"
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 4, 
                    FeatureId = 6, // sponsor_visibility
                    IsEnabled = true,
                    ConfigurationJson = "{\"logoVisibility\": true, \"profileVisibility\": true}"
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 4, 
                    FeatureId = 7, // data_access_percentage
                    IsEnabled = true,
                    ConfigurationJson = "{\"accessPercentage\": 100}"
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 4, 
                    FeatureId = 8, // priority_support
                    IsEnabled = true,
                    ConfigurationJson = "{\"responseTimeHours\": 12}"
                },
                
                // XL Tier (ID=5) - All features
                new TierFeature 
                { 
                    SubscriptionTierId = 5, 
                    FeatureId = 1, // messaging
                    IsEnabled = true
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 5, 
                    FeatureId = 2, // voice_messages
                    IsEnabled = true,
                    ConfigurationJson = "{\"maxDurationSeconds\": 300, \"maxFileSizeMB\": 10}"
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 5, 
                    FeatureId = 3, // smart_links
                    IsEnabled = true,
                    ConfigurationJson = "{\"maxLinksPerSponsor\": 50, \"requiresApproval\": false}"
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 5, 
                    FeatureId = 4, // advanced_analytics
                    IsEnabled = true
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 5, 
                    FeatureId = 5, // api_access
                    IsEnabled = true,
                    ConfigurationJson = "{\"rateLimit\": 5000, \"rateLimitWindow\": \"hour\"}"
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 5, 
                    FeatureId = 6, // sponsor_visibility
                    IsEnabled = true,
                    ConfigurationJson = "{\"logoVisibility\": true, \"profileVisibility\": true}"
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 5, 
                    FeatureId = 7, // data_access_percentage
                    IsEnabled = true,
                    ConfigurationJson = "{\"accessPercentage\": 100}"
                },
                new TierFeature 
                { 
                    SubscriptionTierId = 5, 
                    FeatureId = 8, // priority_support
                    IsEnabled = true,
                    ConfigurationJson = "{\"responseTimeHours\": 6}"
                }
            };
        }
    }
}
```

---

### 2.3. Centralized Service: TierFeatureService

```csharp
namespace Business.Services.Subscription
{
    public interface ITierFeatureService
    {
        Task<bool> HasFeatureAccessAsync(int userId, string featureKey);
        Task<TierFeatureConfig> GetFeatureConfigAsync(int userId, string featureKey);
        Task<List<FeatureDto>> GetAvailableFeaturesAsync(int userId);
        Task<List<FeatureDto>> GetAllFeaturesAsync();
        Task<bool> EnableFeatureForTierAsync(int tierId, int featureId, string configJson = null);
        Task<bool> DisableFeatureForTierAsync(int tierId, int featureId);
    }
    
    public class TierFeatureService : ITierFeatureService
    {
        private readonly ITierFeatureRepository _tierFeatureRepository;
        private readonly IFeatureRepository _featureRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly ICacheService _cacheService;
        
        public TierFeatureService(
            ITierFeatureRepository tierFeatureRepository,
            IFeatureRepository featureRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            ICacheService cacheService)
        {
            _tierFeatureRepository = tierFeatureRepository;
            _featureRepository = featureRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _cacheService = cacheService;
        }
        
        public async Task<bool> HasFeatureAccessAsync(int userId, string featureKey)
        {
            // Cache key: user:{userId}:feature:{featureKey}
            var cacheKey = $"user:{userId}:feature:{featureKey}";
            var cached = await _cacheService.GetAsync<bool?>(cacheKey);
            
            if (cached.HasValue)
                return cached.Value;
            
            // Get user's tier
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(userId);
            if (profile?.SponsorshipPurchases == null || !profile.SponsorshipPurchases.Any())
                return false;
            
            // Get feature
            var feature = await _featureRepository.GetAsync(f => f.FeatureKey == featureKey && f.IsActive);
            if (feature == null)
                return false;
            
            // Check all purchases (user may have multiple tiers)
            foreach (var purchase in profile.SponsorshipPurchases)
            {
                var tierFeature = await _tierFeatureRepository.GetAsync(tf => 
                    tf.SubscriptionTierId == purchase.SubscriptionTierId &&
                    tf.FeatureId == feature.Id &&
                    tf.IsEnabled);
                
                if (tierFeature != null)
                {
                    // Check if feature is currently active (effective date / expiry date)
                    var now = DateTime.Now;
                    if (tierFeature.EffectiveDate.HasValue && tierFeature.EffectiveDate > now)
                        continue; // Not yet effective
                    
                    if (tierFeature.ExpiryDate.HasValue && tierFeature.ExpiryDate < now)
                        continue; // Already expired
                    
                    // Cache result for 15 minutes
                    await _cacheService.SetAsync(cacheKey, true, TimeSpan.FromMinutes(15));
                    return true;
                }
            }
            
            // Cache negative result for 5 minutes (shorter TTL for negative cache)
            await _cacheService.SetAsync(cacheKey, false, TimeSpan.FromMinutes(5));
            return false;
        }
        
        public async Task<TierFeatureConfig> GetFeatureConfigAsync(int userId, string featureKey)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(userId);
            if (profile?.SponsorshipPurchases == null || !profile.SponsorshipPurchases.Any())
                return null;
            
            var feature = await _featureRepository.GetAsync(f => f.FeatureKey == featureKey);
            if (feature == null)
                return null;
            
            // Get highest tier's configuration
            var highestTierId = profile.SponsorshipPurchases.Max(p => p.SubscriptionTierId);
            var tierFeature = await _tierFeatureRepository.GetAsync(tf => 
                tf.SubscriptionTierId == highestTierId &&
                tf.FeatureId == feature.Id);
            
            if (tierFeature == null)
                return null;
            
            return new TierFeatureConfig
            {
                FeatureKey = featureKey,
                IsEnabled = tierFeature.IsEnabled,
                Configuration = string.IsNullOrEmpty(tierFeature.ConfigurationJson) 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(feature.DefaultConfigJson ?? "{}")
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(tierFeature.ConfigurationJson)
            };
        }
        
        public async Task<List<FeatureDto>> GetAvailableFeaturesAsync(int userId)
        {
            var allFeatures = await _featureRepository.GetListAsync(f => f.IsActive);
            var result = new List<FeatureDto>();
            
            foreach (var feature in allFeatures)
            {
                var hasAccess = await HasFeatureAccessAsync(userId, feature.FeatureKey);
                if (hasAccess)
                {
                    var config = await GetFeatureConfigAsync(userId, feature.FeatureKey);
                    result.Add(new FeatureDto
                    {
                        Id = feature.Id,
                        FeatureKey = feature.FeatureKey,
                        DisplayName = feature.DisplayName,
                        Description = feature.Description,
                        Category = feature.Category,
                        IsAvailable = true,
                        Configuration = config?.Configuration
                    });
                }
            }
            
            return result;
        }
        
        public async Task<bool> EnableFeatureForTierAsync(int tierId, int featureId, string configJson = null)
        {
            var existing = await _tierFeatureRepository.GetAsync(tf => 
                tf.SubscriptionTierId == tierId && tf.FeatureId == featureId);
            
            if (existing != null)
            {
                existing.IsEnabled = true;
                if (!string.IsNullOrEmpty(configJson))
                    existing.ConfigurationJson = configJson;
                existing.ModifiedDate = DateTime.Now;
                
                _tierFeatureRepository.Update(existing);
            }
            else
            {
                var tierFeature = new TierFeature
                {
                    SubscriptionTierId = tierId,
                    FeatureId = featureId,
                    IsEnabled = true,
                    ConfigurationJson = configJson,
                    CreatedDate = DateTime.Now
                };
                
                _tierFeatureRepository.Add(tierFeature);
            }
            
            await _tierFeatureRepository.SaveChangesAsync();
            
            // Invalidate cache
            await _cacheService.RemoveByPatternAsync($"user:*:feature:*");
            
            return true;
        }
        
        public async Task<bool> DisableFeatureForTierAsync(int tierId, int featureId)
        {
            var tierFeature = await _tierFeatureRepository.GetAsync(tf => 
                tf.SubscriptionTierId == tierId && tf.FeatureId == featureId);
            
            if (tierFeature == null)
                return false;
            
            tierFeature.IsEnabled = false;
            tierFeature.ModifiedDate = DateTime.Now;
            
            _tierFeatureRepository.Update(tierFeature);
            await _tierFeatureRepository.SaveChangesAsync();
            
            // Invalidate cache
            await _cacheService.RemoveByPatternAsync($"user:*:feature:*");
            
            return true;
        }
    }
    
    public class TierFeatureConfig
    {
        public string FeatureKey { get; set; }
        public bool IsEnabled { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
    }
}
```

---

### 2.4. Refactored Service Methods

#### Before (Hard-Coded):

```csharp
// AnalysisMessagingService.cs
public async Task<bool> CanSendMessageAsync(int sponsorId)
{
    var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
    
    if (profile?.SponsorshipPurchases != null && profile.SponsorshipPurchases.Any())
    {
        foreach (var purchase in profile.SponsorshipPurchases)
        {
            if (purchase.SubscriptionTierId >= 4) // ❌ Hard-coded
            {
                return true;
            }
        }
    }
    
    return false;
}
```

#### After (Database-Driven):

```csharp
// AnalysisMessagingService.cs
public async Task<bool> CanSendMessageAsync(int sponsorId)
{
    // ✅ Database-driven, centralized check
    return await _tierFeatureService.HasFeatureAccessAsync(sponsorId, "messaging");
}
```

#### Smart Link Before:

```csharp
// SmartLinkService.cs
public async Task<bool> CanCreateSmartLinksAsync(int sponsorId)
{
    var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
    
    if (profile?.SponsorshipPurchases != null)
    {
        foreach (var purchase in profile.SponsorshipPurchases)
        {
            if (purchase.SubscriptionTierId == 5) // ❌ Hard-coded XL tier
            {
                return true;
            }
        }
    }
    
    return false;
}

public async Task<int> GetMaxSmartLinksAsync(int sponsorId)
{
    var canCreate = await CanCreateSmartLinksAsync(sponsorId);
    if (!canCreate)
        return 0;
    
    return 50; // ❌ Hard-coded quota
}
```

#### Smart Link After:

```csharp
// SmartLinkService.cs
public async Task<bool> CanCreateSmartLinksAsync(int sponsorId)
{
    // ✅ Database-driven check
    return await _tierFeatureService.HasFeatureAccessAsync(sponsorId, "smart_links");
}

public async Task<int> GetMaxSmartLinksAsync(int sponsorId)
{
    var config = await _tierFeatureService.GetFeatureConfigAsync(sponsorId, "smart_links");
    if (config == null || !config.IsEnabled)
        return 0;
    
    // ✅ Configuration from database
    if (config.Configuration.TryGetValue("maxLinksPerSponsor", out var maxLinks))
    {
        return Convert.ToInt32(maxLinks);
    }
    
    return 50; // Fallback default
}
```

---

## 3. Migration Strategy

### Step 1: Database Changes

```csharp
// Migration: AddTierFeatureSystem
public partial class AddTierFeatureSystem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create Features table
        migrationBuilder.CreateTable(
            name: "Features",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FeatureKey = table.Column<string>(maxLength: 100, nullable: false),
                DisplayName = table.Column<string>(maxLength: 200, nullable: false),
                Description = table.Column<string>(maxLength: 1000, nullable: true),
                Category = table.Column<string>(maxLength: 50, nullable: true),
                DefaultConfigJson = table.Column<string>(nullable: true),
                RequiresConfiguration = table.Column<bool>(nullable: false),
                ConfigurationSchema = table.Column<string>(nullable: true),
                IsActive = table.Column<bool>(nullable: false),
                IsDeprecated = table.Column<bool>(nullable: false),
                CreatedDate = table.Column<DateTime>(nullable: false),
                ModifiedDate = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Features", x => x.Id);
            });
        
        // Create TierFeatures junction table
        migrationBuilder.CreateTable(
            name: "TierFeatures",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                SubscriptionTierId = table.Column<int>(nullable: false),
                FeatureId = table.Column<int>(nullable: false),
                IsEnabled = table.Column<bool>(nullable: false),
                ConfigurationJson = table.Column<string>(nullable: true),
                EffectiveDate = table.Column<DateTime>(nullable: true),
                ExpiryDate = table.Column<DateTime>(nullable: true),
                CreatedDate = table.Column<DateTime>(nullable: false),
                CreatedByUserId = table.Column<int>(nullable: false),
                ModifiedDate = table.Column<DateTime>(nullable: true),
                ModifiedByUserId = table.Column<int>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TierFeatures", x => x.Id);
                table.ForeignKey(
                    name: "FK_TierFeatures_SubscriptionTiers_SubscriptionTierId",
                    column: x => x.SubscriptionTierId,
                    principalTable: "SubscriptionTiers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TierFeatures_Features_FeatureId",
                    column: x => x.FeatureId,
                    principalTable: "Features",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        
        // Create unique index
        migrationBuilder.CreateIndex(
            name: "IX_TierFeatures_SubscriptionTierId_FeatureId",
            table: "TierFeatures",
            columns: new[] { "SubscriptionTierId", "FeatureId" },
            unique: true);
        
        migrationBuilder.CreateIndex(
            name: "IX_Features_FeatureKey",
            table: "Features",
            column: "FeatureKey",
            unique: true);
    }
}
```

### Step 2: Seed Initial Data

Add to `DatabaseInitializerService.cs`:

```csharp
// Seed features
var features = FeatureSeeds.GetDefaultFeatures();
foreach (var feature in features)
{
    if (!_featureRepository.GetListAsync().Result.Any(f => f.FeatureKey == feature.FeatureKey))
    {
        _featureRepository.Add(feature);
    }
}
await _featureRepository.SaveChangesAsync();

// Seed tier-feature mappings
var tierFeatures = FeatureSeeds.GetDefaultTierFeatures();
foreach (var tierFeature in tierFeatures)
{
    if (!_tierFeatureRepository.GetListAsync().Result.Any(tf => 
        tf.SubscriptionTierId == tierFeature.SubscriptionTierId && 
        tf.FeatureId == tierFeature.FeatureId))
    {
        _tierFeatureRepository.Add(tierFeature);
    }
}
await _tierFeatureRepository.SaveChangesAsync();
```

### Step 3: Gradual Service Refactoring

**Phase 1:** Add `ITierFeatureService` dependency to existing services (constructor injection)

**Phase 2:** Replace hard-coded checks with service calls (one feature at a time)

**Phase 3:** Remove old hard-coded logic after verification

**Phase 4:** Add admin UI for feature management

---

## 4. Admin UI Endpoints

```csharp
// GET /api/admin/tier-features
[HttpGet("tier-features")]
[SecuredOperation("Admin")]
public async Task<IActionResult> GetAllTierFeatures()
{
    var result = await _mediator.Send(new GetAllTierFeaturesQuery());
    return Ok(result);
}

// POST /api/admin/tier-features
[HttpPost("tier-features")]
[SecuredOperation("Admin")]
public async Task<IActionResult> EnableFeatureForTier([FromBody] EnableTierFeatureCommand command)
{
    var result = await _mediator.Send(command);
    return Ok(result);
}

// DELETE /api/admin/tier-features/{tierId}/{featureId}
[HttpDelete("tier-features/{tierId}/{featureId}")]
[SecuredOperation("Admin")]
public async Task<IActionResult> DisableFeatureForTier(int tierId, int featureId)
{
    var result = await _mediator.Send(new DisableTierFeatureCommand { TierId = tierId, FeatureId = featureId });
    return Ok(result);
}

// PUT /api/admin/tier-features/{tierId}/{featureId}/schedule
[HttpPut("tier-features/{tierId}/{featureId}/schedule")]
[SecuredOperation("Admin")]
public async Task<IActionResult> ScheduleFeature(int tierId, int featureId, [FromBody] ScheduleFeatureCommand command)
{
    command.TierId = tierId;
    command.FeatureId = featureId;
    var result = await _mediator.Send(command);
    return Ok(result);
}
```

---

## 5. Benefits Summary

### Before (Hard-Coded):
- ❌ Code deployment required for permission changes
- ❌ Scattered tier checks across multiple files
- ❌ Magic numbers everywhere (`>= 4`, `== 5`)
- ❌ No audit trail
- ❌ No scheduling/expiry support
- ❌ No admin UI support
- ❌ Inconsistent feature naming

### After (Database-Driven):
- ✅ **Zero-downtime permission updates** (database change only)
- ✅ **Centralized feature registry** (single source of truth)
- ✅ **Admin UI support** (non-technical users can manage)
- ✅ **Audit trail** (who changed what, when)
- ✅ **Feature scheduling** (promotional periods, A/B testing)
- ✅ **Configuration per tier** (XL gets 50 links, custom gets 100)
- ✅ **Caching support** (15-minute cache for performance)
- ✅ **Consistent API** (`HasFeatureAccessAsync`, `GetFeatureConfigAsync`)

---

## 6. Example Use Cases

### Use Case 1: Black Friday Promotion

**Goal:** Give L tier users Smart Links for 7 days

**Steps:**
1. Admin panel → Select "Smart Links" feature
2. Enable for "L Tier"
3. Set effective date: 2024-11-24 00:00
4. Set expiry date: 2024-12-01 23:59
5. Save

**Result:**
- Automatically activates on Black Friday
- Automatically deactivates after 7 days
- No code deployment
- Full audit trail

---

### Use Case 2: A/B Testing

**Goal:** Test if voice messages increase engagement

**Steps:**
1. Enable "Voice Messages" for M tier (50% of users)
2. Set expiry: 30 days
3. Monitor analytics
4. If successful → Keep enabled
5. If not → Automatically reverts after 30 days

---

### Use Case 3: Custom Enterprise Plan

**Goal:** Create custom tier for enterprise client

**Steps:**
1. Create new SubscriptionTier: "Enterprise" (ID=6)
2. Enable all features for Enterprise tier
3. Custom configuration:
   - Smart Links: 500 (instead of 50)
   - API Rate Limit: 50,000/hour (instead of 5,000)
   - Response Time: 2 hours (instead of 6)

**Implementation:**
```json
{
  "maxLinksPerSponsor": 500,
  "requiresApproval": false
}
```

No code changes needed!

---

## 7. Implementation Timeline

**Week 1:**
- Create entities (Feature, TierFeature)
- Create migration
- Seed initial data
- Deploy to staging

**Week 2:**
- Implement TierFeatureService
- Add caching layer
- Unit tests

**Week 3:**
- Refactor AnalysisMessagingService (messaging feature)
- Refactor SmartLinkService (smart links feature)
- Integration tests

**Week 4:**
- Refactor remaining services
- Admin UI endpoints
- End-to-end testing

**Week 5:**
- Admin UI frontend
- Documentation
- Production deployment

---

## 8. Backwards Compatibility

During migration, both systems can coexist:

```csharp
public async Task<bool> CanSendMessageAsync(int sponsorId)
{
    // New system (database-driven)
    var hasFeature = await _tierFeatureService.HasFeatureAccessAsync(sponsorId, "messaging");
    
    if (hasFeature)
        return true;
    
    // Fallback to old system (hard-coded) - TEMPORARY
    var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
    if (profile?.SponsorshipPurchases != null)
    {
        foreach (var purchase in profile.SponsorshipPurchases)
        {
            if (purchase.SubscriptionTierId >= 4)
                return true;
        }
    }
    
    return false;
}
```

After verification, remove fallback code.

---

**Status:** ✅ Ready for Implementation  
**Estimated Effort:** 5 weeks (1 developer)  
**Risk:** Low (incremental migration, backwards compatible)  
**Priority:** High (significant technical debt reduction)
