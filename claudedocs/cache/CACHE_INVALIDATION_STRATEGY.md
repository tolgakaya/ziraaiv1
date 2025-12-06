# ZiraAI Cache Invalidation Strategy

## Executive Summary

Bu doküman, ZiraAI projesinde cache'lenen verilerin ne zaman, nasıl ve hangi komutlar/olaylar sonucu invalidate edileceğini detaylandırmaktadır.

**Invalidation Prensipleri:**
- 🎯 **Event-Driven:** Command handler'lar başarılı işlem sonrası cache invalidate eder
- 🎯 **Granular:** Sadece etkilenen cache key'ler invalidate edilir (cascade pattern)
- 🎯 **Async:** Invalidation işlemi non-blocking (background job)
- 🎯 **Fail-Safe:** Invalidation başarısız olsa bile uygulama çalışmaya devam eder

---

## 🔥 1. Dashboard Cache Invalidation

### 1.1 Dealer Dashboard Cache

**Cache Keys:**
```
dealer:dashboard:{dealerId}
dealer:codes:{dealerId}
dealer:invitations:{dealerId}
```

#### Invalidation Triggers

##### 🔴 **Critical (Immediate Invalidation)**

| Command | Handler File | Invalidate Keys | Reason |
|---------|-------------|-----------------|--------|
| **TransferCodesToDealerCommand** | `TransferCodesToDealerCommandHandler.cs` | `dealer:dashboard:{dealerId}`<br>`dealer:codes:{dealerId}` | Kod sayısı değişti |
| **AcceptDealerInvitationCommand** | `AcceptDealerInvitationCommand.cs` | `dealer:dashboard:{dealerId}`<br>`dealer:invitations:{dealerId}` | Invitation kabul edildi |
| **SendSponsorshipLinkCommand** | `SendSponsorshipLinkCommand.cs` | `dealer:codes:{dealerId}` | Kod dağıtıldı (distributed) |
| **RedeemSponsorshipCodeCommand** | `RedeemSponsorshipCodeCommand.cs` | `dealer:dashboard:{dealerId}`<br>`dealer:codes:{dealerId}` | Kod kullanıldı |

##### 🟡 **Medium (Delayed Invalidation - 1 min)**

| Command | Handler File | Invalidate Keys | Reason |
|---------|-------------|-----------------|--------|
| **CreateDealerInvitationCommand** | `CreateDealerInvitationCommandHandler.cs` | `dealer:invitations:{dealerId}` | Yeni invitation |
| **CancelDealerInvitationCommand** | `CancelDealerInvitationCommand.cs` | `dealer:invitations:{dealerId}` | Invitation iptal edildi |
| **BulkDealerInvitationCommand** | `BulkDealerInvitationCommandHandler.cs` | `dealer:invitations:{dealerId}` | Bulk invitation |

#### Implementation Example

```csharp
// TransferCodesToDealerCommandHandler.cs
public async Task<IResult> Handle(TransferCodesToDealerCommand request, ...)
{
    // ... business logic ...

    await _sponsorshipCodeRepository.SaveChangesAsync();

    // Invalidate dealer cache
    await _cacheInvalidationService.InvalidateDealerDashboardAsync(request.DealerId);

    return new SuccessResult("Codes transferred successfully");
}
```

```csharp
// CacheInvalidationService.cs
public async Task InvalidateDealerDashboardAsync(int dealerId)
{
    var keys = new[]
    {
        $"dealer:dashboard:{dealerId}",
        $"dealer:codes:{dealerId}",
        $"dealer:summary:{dealerId}"
    };

    await _cache.RemoveManyAsync(keys);

    _logger.LogInformation("[CACHE_INVALIDATED] Dealer dashboard - DealerId: {DealerId}, Keys: {KeyCount}",
        dealerId, keys.Length);
}
```

---

### 1.2 Sponsor Dashboard Cache

**Cache Keys:**
```
sponsor:dashboard:{sponsorId}
sponsor:codes:{sponsorId}
sponsor:farmers:{sponsorId}
sponsor:messages:{sponsorId}
sponsor:analytics:{sponsorId}
```

#### Invalidation Triggers

##### 🔴 **Critical (Immediate Invalidation)**

| Command | Handler File | Invalidate Keys | Reason |
|---------|-------------|-----------------|--------|
| **PurchaseBulkSponsorshipCommand** | `PurchaseBulkSponsorshipCommand.cs` | `sponsor:dashboard:{sponsorId}`<br>`sponsor:codes:{sponsorId}`<br>`admin:stats:sponsorship:*` | Yeni purchase |
| **CreateSponsorshipCodeCommand** | `CreateSponsorshipCodeCommand.cs` | `sponsor:codes:{sponsorId}`<br>`admin:stats:sponsorship:*` | Yeni kod oluşturuldu |
| **TransferCodesToDealerCommand** | `TransferCodesToDealerCommandHandler.cs` | `sponsor:dashboard:{sponsorId}`<br>`sponsor:codes:{sponsorId}` | Kod transfer edildi |
| **RedeemSponsorshipCodeCommand** | `RedeemSponsorshipCodeCommand.cs` | `sponsor:dashboard:{sponsorId}`<br>`sponsor:farmers:{sponsorId}`<br>`sponsor:analytics:{sponsorId}` | Kod kullanıldı (yeni farmer) |

##### 🟡 **Medium (Delayed Invalidation)**

| Command | Handler File | Invalidate Keys | Reason |
|---------|-------------|-----------------|--------|
| **SendMessageCommand** | `SendMessageCommand.cs` | `sponsor:messages:{sponsorId}` | Mesaj gönderildi |
| **SendMessageWithAttachmentCommand** | `SendMessageWithAttachmentCommand.cs` | `sponsor:messages:{sponsorId}` | Mesaj gönderildi |
| **CreateSmartLinkCommand** | `CreateSmartLinkCommand.cs` | `sponsor:dashboard:{sponsorId}` | Smart link oluşturuldu |

#### Implementation Example

```csharp
// RedeemSponsorshipCodeCommand.cs
public async Task<IResult> Handle(RedeemSponsorshipCodeCommand request, ...)
{
    // ... redemption logic ...

    await _sponsorshipCodeRepository.SaveChangesAsync();

    // Invalidate sponsor cache (cascade)
    await _cacheInvalidationService.InvalidateSponsorDashboardAsync(code.SponsorId);

    // Also invalidate farmer cache (new sponsor relationship)
    await _cacheInvalidationService.InvalidateFarmerCacheAsync(request.UserId);

    return new SuccessResult("Code redeemed successfully");
}
```

---

### 1.3 Admin Dashboard Cache

**Cache Keys:**
```
admin:stats:users:{startDate}:{endDate}
admin:stats:subscriptions:{startDate}:{endDate}
admin:stats:sponsorship:{startDate}:{endDate}
admin:stats:analytics:*
```

#### Invalidation Triggers

##### 🔴 **Critical (Immediate Invalidation)**

| Command | Handler File | Invalidate Keys | Reason |
|---------|-------------|-----------------|--------|
| **RegisterUserCommand** | `RegisterUserCommand.cs` | `admin:stats:users:*` | Yeni kullanıcı |
| **RegisterUserWithPhoneCommand** | `RegisterUserWithPhoneCommand.cs` | `admin:stats:users:*` | Yeni kullanıcı |
| **AssignSubscriptionCommand** | `AssignSubscriptionCommand.cs` | `admin:stats:subscriptions:*`<br>`admin:stats:users:*` | Subscription atandı |
| **CancelSubscriptionCommand** | `CancelSubscriptionCommand.cs` | `admin:stats:subscriptions:*` | Subscription iptal edildi |
| **PurchaseBulkSponsorshipCommand** | `PurchaseBulkSponsorshipCommand.cs` | `admin:stats:sponsorship:*` | Yeni sponsorship |

##### 🟡 **Medium (Delayed Invalidation - 5 min)**

| Command | Handler File | Invalidate Keys | Reason |
|---------|-------------|-----------------|--------|
| **CreateUserGroupCommand** | `CreateUserGroupCommand.cs` | `admin:stats:users:*` | Rol değişikliği |
| **UpdateUserGroupByGroupIdCommand** | `UpdateUserGroupByGroupIdCommand.cs` | `admin:stats:users:*` | Bulk rol değişikliği |
| **DeactivateUserCommand** | `DeactivateUserCommand.cs` | `admin:stats:users:*` | Kullanıcı deaktif |
| **ReactivateUserCommand** | `ReactivateUserCommand.cs` | `admin:stats:users:*` | Kullanıcı aktif |

##### 🟢 **Low Priority (Delayed Invalidation - 15 min)**

| Command | Handler File | Invalidate Keys | Reason |
|---------|-------------|-----------------|--------|
| **UpdateUserCommand** | `UpdateUserCommand.cs` | `admin:stats:users:*` | Profil güncellendi |
| **ExtendSubscriptionCommand** | `ExtendSubscriptionCommand.cs` | `admin:stats:subscriptions:*` | Subscription uzatıldı |

#### Implementation Example

```csharp
// RegisterUserCommand.cs
public async Task<IDataResult<User>> Handle(RegisterUserCommand request, ...)
{
    // ... registration logic ...

    await _userRepository.SaveChangesAsync();

    // Invalidate admin user statistics
    await _cacheInvalidationService.InvalidateAdminUserStatsAsync();

    return new SuccessDataResult<User>(user, "User registered successfully");
}
```

```csharp
// CacheInvalidationService.cs
public async Task InvalidateAdminUserStatsAsync()
{
    // Invalidate all date-based user statistics
    await _cache.RemoveByPatternAsync("admin:stats:users:*");

    _logger.LogInformation("[CACHE_INVALIDATED] Admin user statistics - All date ranges");
}
```

---

## 🎯 2. Reference Data Cache Invalidation

### 2.1 Subscription Tiers Cache

**Cache Keys:**
```
tiers:all:active
tiers:detail:{tierId}
tiers:features:{tierId}
```

#### Invalidation Triggers

##### 🔴 **Critical (Immediate Invalidation + Broadcast)**

| Command | Handler File | Invalidate Keys | Reason |
|---------|-------------|-----------------|--------|
| **Admin: CreateSubscriptionTierCommand** | (Admin handler) | `tiers:*` | Yeni tier oluşturuldu |
| **Admin: UpdateSubscriptionTierCommand** | (Admin handler) | `tiers:*` | Tier güncellendi |
| **Admin: UpdateTierPricingCommand** | (Admin handler) | `tiers:*` | Fiyat değişti |
| **UpdateMessagingFeatureCommand** | `UpdateMessagingFeatureCommand.cs` | `tiers:features:{tierId}` | Feature güncellendi |

**Özel Durum:** Tier değişiklikleri tüm instance'lara broadcast edilmeli (Redis Pub/Sub)

#### Implementation Example

```csharp
// Admin: UpdateSubscriptionTierCommand (hypothetical)
public async Task<IResult> Handle(UpdateSubscriptionTierCommand request, ...)
{
    // ... update logic ...

    await _subscriptionTierRepository.SaveChangesAsync();

    // BROADCAST invalidation to all instances
    await _cacheInvalidationService.BroadcastInvalidateAsync("tiers:*");

    // Audit log (critical change)
    await _adminAuditService.LogAsync("TIER_UPDATED", request.TierId, request.AdminUserId);

    return new SuccessResult("Tier updated successfully");
}
```

```csharp
// CacheInvalidationService.cs
public async Task BroadcastInvalidateAsync(string pattern)
{
    // Invalidate local cache
    await _cache.RemoveByPatternAsync(pattern);

    // Publish to Redis for other instances
    await _redisPublisher.PublishAsync("cache:invalidate", new
    {
        Pattern = pattern,
        Timestamp = DateTime.UtcNow,
        Source = _instanceId
    });

    _logger.LogWarning("[CACHE_BROADCAST] Pattern: {Pattern}, Instance: {InstanceId}",
        pattern, _instanceId);
}
```

---

### 2.2 Configuration Cache

**Cache Keys:**
```
config:{key}
config:category:{category}
config:all:active
```

#### Invalidation Triggers

##### 🔴 **Critical (Immediate Invalidation + Broadcast)**

| Command | Handler File | Invalidate Keys | Reason |
|---------|-------------|-----------------|--------|
| **CreateConfigurationCommand** | `CreateConfigurationCommand.cs` | `config:all:active`<br>`config:category:{category}` | Yeni config |
| **UpdateConfigurationCommand** | `UpdateConfigurationCommand.cs` | `config:{key}`<br>`config:category:{category}`<br>`config:all:active` | Config güncellendi |

**Mevcut Durum:** ✅ Zaten 15 dakika TTL var, manuel invalidation eklenmeli

#### Implementation Example

```csharp
// UpdateConfigurationCommand.cs
public async Task<IResult> Handle(UpdateConfigurationCommand request, ...)
{
    var config = await _configurationRepository.GetAsync(c => c.Id == request.Id);

    // ... update logic ...

    await _configurationRepository.SaveChangesAsync();

    // Invalidate specific config + category cache
    await _cacheInvalidationService.InvalidateConfigurationAsync(
        config.Key,
        config.Category);

    return new SuccessResult("Configuration updated successfully");
}
```

---

## 🔄 3. Analytics Cache Invalidation

### 3.1 Sponsor Analytics

**Cache Keys:**
```
sponsor:roi:{sponsorId}:{period}
sponsor:segmentation:{sponsorId}
sponsor:temporal:{sponsorId}:{period}
sponsor:engagement:{sponsorId}
sponsor:competitive:{sponsorId}
```

#### Invalidation Triggers

##### 🟡 **Medium (Delayed Invalidation - 5-10 min)**

| Event | Trigger | Invalidate Keys | Reason |
|-------|---------|-----------------|--------|
| **Kod Kullanımı** | `RedeemSponsorshipCodeCommand` | `sponsor:roi:{sponsorId}:*`<br>`sponsor:segmentation:{sponsorId}` | ROI değişti |
| **Mesaj Aktivitesi** | `SendMessageCommand` | `sponsor:engagement:{sponsorId}` | Engagement değişti |
| **Analiz Tamamlanması** | `PlantAnalysisCompleted` (Event) | `sponsor:temporal:{sponsorId}:*` | Yeni veri noktası |

**Strategi:** Batch invalidation (5 dakika buffer) - aynı sponsor için birden fazla invalidation event'i gruplanır

#### Implementation Example

```csharp
// Background Service: Analytics Cache Invalidation Worker
public class AnalyticsCacheInvalidationWorker : BackgroundService
{
    private readonly ConcurrentDictionary<int, DateTime> _pendingInvalidations = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            var toInvalidate = _pendingInvalidations.Where(x =>
                DateTime.Now - x.Value > TimeSpan.FromMinutes(5));

            foreach (var (sponsorId, _) in toInvalidate)
            {
                await _cacheInvalidationService.InvalidateSponsorAnalyticsAsync(sponsorId);
                _pendingInvalidations.TryRemove(sponsorId, out _);
            }
        }
    }

    public void QueueInvalidation(int sponsorId)
    {
        _pendingInvalidations.TryAdd(sponsorId, DateTime.Now);
    }
}
```

---

## 🛠️ 4. CacheInvalidationService Implementation

### 4.1 Service Interface

```csharp
public interface ICacheInvalidationService
{
    // Dashboard Invalidations
    Task InvalidateDealerDashboardAsync(int dealerId);
    Task InvalidateSponsorDashboardAsync(int sponsorId);
    Task InvalidateAdminUserStatsAsync();
    Task InvalidateAdminSubscriptionStatsAsync();
    Task InvalidateAdminSponsorshipStatsAsync();

    // Reference Data Invalidations
    Task InvalidateTiersAsync();
    Task InvalidateConfigurationAsync(string key, string category);
    Task InvalidateMessagingFeaturesAsync(int tierId);

    // Analytics Invalidations (Batch)
    Task QueueSponsorAnalyticsInvalidationAsync(int sponsorId);
    Task InvalidateSponsorAnalyticsAsync(int sponsorId);

    // Pattern-based Invalidations
    Task InvalidateByPatternAsync(string pattern);
    Task BroadcastInvalidateAsync(string pattern);

    // User-specific Invalidations
    Task InvalidateFarmerCacheAsync(int farmerId);

    // Manual Admin Invalidations
    Task InvalidateAllAsync();
    Task InvalidateCategoryAsync(string category);
}
```

---

### 4.2 Service Implementation

```csharp
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IDistributedCache _cache;
    private readonly IRedisPublisher _redisPublisher;
    private readonly ILogger<CacheInvalidationService> _logger;
    private readonly string _instanceId;

    public CacheInvalidationService(
        IDistributedCache cache,
        IRedisPublisher redisPublisher,
        ILogger<CacheInvalidationService> logger)
    {
        _cache = cache;
        _redisPublisher = redisPublisher;
        _logger = logger;
        _instanceId = Environment.MachineName;
    }

    public async Task InvalidateDealerDashboardAsync(int dealerId)
    {
        var keys = new[]
        {
            $"dealer:dashboard:{dealerId}",
            $"dealer:codes:{dealerId}",
            $"dealer:invitations:{dealerId}",
            $"dealer:summary:{dealerId}"
        };

        await RemoveManyAsync(keys);

        _logger.LogInformation(
            "[CACHE_INVALIDATED] Dealer dashboard - DealerId: {DealerId}, Keys: {KeyCount}",
            dealerId, keys.Length);
    }

    public async Task InvalidateSponsorDashboardAsync(int sponsorId)
    {
        var keys = new[]
        {
            $"sponsor:dashboard:{sponsorId}",
            $"sponsor:codes:{sponsorId}",
            $"sponsor:farmers:{sponsorId}",
            $"sponsor:messages:{sponsorId}",
            $"sponsor:summary:{sponsorId}"
        };

        await RemoveManyAsync(keys);

        _logger.LogInformation(
            "[CACHE_INVALIDATED] Sponsor dashboard - SponsorId: {SponsorId}, Keys: {KeyCount}",
            sponsorId, keys.Length);
    }

    public async Task InvalidateAdminUserStatsAsync()
    {
        await InvalidateByPatternAsync("admin:stats:users:*");

        _logger.LogInformation("[CACHE_INVALIDATED] Admin user statistics - All date ranges");
    }

    public async Task InvalidateAdminSubscriptionStatsAsync()
    {
        await InvalidateByPatternAsync("admin:stats:subscriptions:*");

        _logger.LogInformation("[CACHE_INVALIDATED] Admin subscription statistics");
    }

    public async Task InvalidateTiersAsync()
    {
        // Critical: Broadcast to all instances
        await BroadcastInvalidateAsync("tiers:*");
    }

    public async Task InvalidateConfigurationAsync(string key, string category)
    {
        var keys = new[]
        {
            $"config:{key}",
            $"config:category:{category}",
            "config:all:active"
        };

        // Broadcast to all instances
        await BroadcastInvalidateManyAsync(keys);
    }

    public async Task BroadcastInvalidateAsync(string pattern)
    {
        // Local invalidation
        await InvalidateByPatternAsync(pattern);

        // Publish to other instances
        await _redisPublisher.PublishAsync("cache:invalidate:pattern", new
        {
            Pattern = pattern,
            Timestamp = DateTime.UtcNow,
            Source = _instanceId
        });

        _logger.LogWarning(
            "[CACHE_BROADCAST] Pattern: {Pattern}, Instance: {InstanceId}",
            pattern, _instanceId);
    }

    private async Task BroadcastInvalidateManyAsync(string[] keys)
    {
        // Local invalidation
        await RemoveManyAsync(keys);

        // Publish to other instances
        await _redisPublisher.PublishAsync("cache:invalidate:keys", new
        {
            Keys = keys,
            Timestamp = DateTime.UtcNow,
            Source = _instanceId
        });

        _logger.LogWarning(
            "[CACHE_BROADCAST] Keys: {KeyCount}, Instance: {InstanceId}",
            keys.Length, _instanceId);
    }

    private async Task RemoveManyAsync(string[] keys)
    {
        var tasks = keys.Select(key => _cache.RemoveAsync(key));
        await Task.WhenAll(tasks);
    }

    public async Task InvalidateByPatternAsync(string pattern)
    {
        // Redis pattern matching implementation
        var keys = await _cache.GetKeysByPatternAsync(pattern);
        await RemoveManyAsync(keys.ToArray());

        _logger.LogInformation(
            "[CACHE_INVALIDATED] Pattern: {Pattern}, Matched: {KeyCount}",
            pattern, keys.Count);
    }
}
```

---

### 4.3 Redis Pub/Sub Subscriber

```csharp
public class CacheInvalidationSubscriber : BackgroundService
{
    private readonly IRedisSubscriber _redisSubscriber;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheInvalidationSubscriber> _logger;
    private readonly string _instanceId;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _instanceId = Environment.MachineName;

        // Subscribe to pattern invalidation
        await _redisSubscriber.SubscribeAsync("cache:invalidate:pattern", async message =>
        {
            var data = JsonSerializer.Deserialize<CacheInvalidationMessage>(message);

            // Ignore own broadcasts
            if (data.Source == _instanceId) return;

            _logger.LogInformation(
                "[CACHE_REMOTE_INVALIDATE] Pattern: {Pattern}, From: {Source}",
                data.Pattern, data.Source);

            await _cache.RemoveByPatternAsync(data.Pattern);
        });

        // Subscribe to key invalidation
        await _redisSubscriber.SubscribeAsync("cache:invalidate:keys", async message =>
        {
            var data = JsonSerializer.Deserialize<CacheInvalidationKeysMessage>(message);

            if (data.Source == _instanceId) return;

            _logger.LogInformation(
                "[CACHE_REMOTE_INVALIDATE] Keys: {KeyCount}, From: {Source}",
                data.Keys.Length, data.Source);

            var tasks = data.Keys.Select(key => _cache.RemoveAsync(key));
            await Task.WhenAll(tasks);
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

public class CacheInvalidationMessage
{
    public string Pattern { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; }
}

public class CacheInvalidationKeysMessage
{
    public string[] Keys { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; }
}
```

---

## 📊 5. Invalidation Monitoring

### 5.1 Metrics

```csharp
public class CacheInvalidationMetrics
{
    public long TotalInvalidations { get; set; }
    public long PatternInvalidations { get; set; }
    public long KeyInvalidations { get; set; }
    public long BroadcastInvalidations { get; set; }
    public Dictionary<string, int> InvalidationsByType { get; set; }
    public TimeSpan AverageInvalidationTime { get; set; }
}
```

### 5.2 Logging Examples

```csharp
_logger.LogInformation(
    "[CACHE_INVALIDATED] Type: {Type}, Target: {Target}, Keys: {KeyCount}, Duration: {Duration}ms",
    "DealerDashboard", dealerId, 4, stopwatch.ElapsedMilliseconds);

_logger.LogWarning(
    "[CACHE_BROADCAST] Pattern: {Pattern}, Instance: {InstanceId}, Recipients: all",
    "tiers:*", _instanceId);

_logger.LogError(
    "[CACHE_INVALIDATION_FAILED] Key: {Key}, Error: {Error}",
    cacheKey, ex.Message);
```

---

## 🚀 6. Invalidation Testing

### 6.1 Unit Tests

```csharp
[Fact]
public async Task RedeemCode_Should_InvalidateSponsorAndDealerCache()
{
    // Arrange
    var dealerId = 123;
    var sponsorId = 456;
    var mockCache = new Mock<IDistributedCache>();
    var service = new CacheInvalidationService(mockCache.Object, ...);

    // Act
    await service.InvalidateDealerDashboardAsync(dealerId);
    await service.InvalidateSponsorDashboardAsync(sponsorId);

    // Assert
    mockCache.Verify(c => c.RemoveAsync($"dealer:dashboard:{dealerId}", default), Times.Once);
    mockCache.Verify(c => c.RemoveAsync($"sponsor:dashboard:{sponsorId}", default), Times.Once);
}
```

### 6.2 Integration Tests

```csharp
[Fact]
public async Task Configuration_Update_Should_BroadcastInvalidation()
{
    // Arrange
    var redisSubscriber = new TestRedisSubscriber();
    var service = new CacheInvalidationService(..., redisSubscriber);

    // Act
    await service.InvalidateConfigurationAsync("TEST_KEY", "TestCategory");

    // Assert
    Assert.Contains("cache:invalidate:keys", redisSubscriber.PublishedMessages.Keys);
    var message = redisSubscriber.PublishedMessages["cache:invalidate:keys"];
    Assert.Contains("config:TEST_KEY", message.Keys);
}
```

---

## 📈 7. Invalidation Frequency Analysis

### Expected Invalidation Rates (Production)

| Cache Type | Invalidation/Hour | Peak Time | Strategy |
|-----------|-------------------|-----------|----------|
| **Dealer Dashboard** | ~50-100 | Business hours (9-18) | Immediate |
| **Sponsor Dashboard** | ~20-50 | Business hours | Immediate |
| **Admin Statistics** | ~5-10 | Anytime | Delayed (5 min) |
| **Tiers/Config** | ~1-2/day | Maintenance window | Broadcast |
| **Analytics** | ~100-200 | Evening (18-22) | Batch (10 min) |

### Optimization Opportunities

1. **Batch Invalidations**: Group multiple invalidations for same entity
2. **Delayed Invalidations**: Admin stats can tolerate 5-10 min delay
3. **Smart TTL**: Longer TTL during low-activity periods
4. **Predictive Invalidation**: Invalidate before scheduled tasks

---

## 💡 8. Best Practices

### 8.1 Command Handler Pattern

```csharp
public class TransferCodesToDealerCommandHandler
{
    public async Task<IResult> Handle(...)
    {
        try
        {
            // 1. Execute business logic
            await ExecuteBusinessLogic();

            // 2. Save changes
            await _repository.SaveChangesAsync();

            // 3. Invalidate cache (non-blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _cacheInvalidation.InvalidateDealerDashboardAsync(dealerId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cache invalidation failed (non-critical)");
                }
            });

            return new SuccessResult();
        }
        catch (Exception ex)
        {
            // DON'T invalidate cache on failure
            return new ErrorResult(ex.Message);
        }
    }
}
```

### 8.2 Error Handling

```csharp
// ALWAYS graceful degradation
try
{
    await _cache.RemoveAsync(key);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Cache invalidation failed for key: {Key}", key);
    // Continue execution - cache will expire via TTL
}
```

### 8.3 Audit Trail

```csharp
// Log critical invalidations
if (pattern == "tiers:*" || pattern.StartsWith("admin:"))
{
    await _adminAuditService.LogAsync(
        "CACHE_INVALIDATED",
        $"Pattern: {pattern}",
        userId,
        metadata: new { Pattern = pattern, Reason = reason });
}
```

---

## 📞 Özet

**Toplam Invalidation Points:** 50+ commands
**Critical Invalidations:** 15 (broadcast gerekli)
**Delayed Invalidations:** 25 (batch işlem)
**Automatic Invalidations:** 10 (background service)

**Key Takeaways:**
- ✅ Event-driven invalidation (command success sonrası)
- ✅ Granular cache keys (sadece etkilenen veriler)
- ✅ Non-blocking invalidation (async background)
- ✅ Broadcast for critical data (tiers, config)
- ✅ Batch processing for analytics
- ✅ Graceful degradation on errors

**Hazırlayan:** Claude AI Analysis
**Tarih:** 2025-12-05
**Versiyon:** 1.0
**Durum:** Ready for Implementation

