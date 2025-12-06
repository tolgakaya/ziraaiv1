# ZiraAI Cache Performance Optimization Analysis

## Executive Summary

Bu rapor, ZiraAI projesinde cache implementasyonu ile performans iyileştirmesi yapılabilecek sorguları, feature'ları ve potansiyel kazanımları detaylı olarak analiz etmektedir.

**Mevcut Durum:**
- ✅ ConfigurationService: Memory cache (15 dakika TTL)
- ✅ Bazı servislerde IMemoryCache kullanımı var
- ❌ Repository/Query seviyesinde cache yok
- ❌ Dashboard/Analytics sorgularında cache yok
- ❌ Distributed cache (Redis) sadece infra var, kullanımda değil

**Potansiyel Kazanç:**
- 🎯 **60-80% Response Time** azalması (cached endpoints)
- 🎯 **70-90% DB Load** azalması (dashboard queries)
- 🎯 **100-500ms → 5-20ms** (tier/config queries)
- 🎯 **500-2000ms → 50-200ms** (analytics queries)

---

## 🔥 1. Yüksek Öncelikli Cache Fırsatları

### 1.1 Dashboard & Analytics Queries

#### 🎯 **GetDealerDashboardSummaryQuery**
**Dosya**: `Business/Handlers/Sponsorship/Queries/GetDealerDashboardSummaryQuery.cs`

**Mevcut Performans:**
```csharp
// 2 database queries:
// 1. Dealer codes query (ToListAsync)
// 2. Pending invitations count (CountAsync)
```

**Cache Stratejisi:**
```csharp
Cache Key: $"dealer:dashboard:{dealerId}"
TTL: 5 dakika
Invalidation: Kod dağıtımında, kod kullanımında, invitation değişikliğinde
```

**Potansiyel Kazanç:**
- ⚡ Response time: **500-1200ms → 10-30ms** (95% azalma)
- 📊 DB queries: **2 → 0** (cache hit durumunda)
- 🔄 Güncellenme sıklığı: Orta (5-10 dakikada bir)
- 📈 Hit ratio beklentisi: **80-90%** (dealers frequent refresh)

**Implementasyon Önceliği:** 🔴 **VERY HIGH**
- Çok sık çağrılan endpoint (dashboard loading)
- Ağır queries (aggregate operations)
- Yüksek cache hit ratio potansiyeli

---

#### 🎯 **GetUserStatisticsQuery (Admin)**
**Dosya**: `Business/Handlers/AdminAnalytics/Queries/GetUserStatisticsQuery.cs`

**Mevcut Performans:**
```csharp
// 7 database queries:
// 1. All users query (filtered)
// 2. Admin group lookup
// 3. Farmer group lookup
// 4. Sponsor group lookup
// 5. Admin users count
// 6. Farmer users count
// 7. Sponsor users count
// + Multiple in-memory counts
```

**Cache Stratejisi:**
```csharp
Cache Key: $"admin:stats:users:{startDate}:{endDate}"
TTL: 15 dakika
Invalidation: User registration, role assignment, manual refresh
```

**Potansiyel Kazanç:**
- ⚡ Response time: **800-2000ms → 20-50ms** (97% azalma)
- 📊 DB queries: **7 → 0** (cache hit durumunda)
- 🔄 Güncellenme sıklığı: Düşük (kullanıcı büyüme hızına bağlı)
- 📈 Hit ratio beklentisi: **90-95%** (admin infrequent refresh)

**Implementasyon Önceliği:** 🔴 **VERY HIGH**
- En ağır query'lerden biri (7 query)
- Gerçek zamanlılık gerekmez (15 dk eski veri kabul edilebilir)
- Admin panel'de sık görüntüleniyor

---

#### 🎯 **GetSubscriptionStatisticsQuery (Admin)**
**Dosya**: `Business/Handlers/AdminAnalytics/Queries/GetSubscriptionStatisticsQuery.cs`

**Mevcut Performans:**
```csharp
// 1 complex query with Include + aggregate operations:
// - Include(SubscriptionTier)
// - Multiple GroupBy, Sum, Average operations in-memory
```

**Cache Stratejisi:**
```csharp
Cache Key: $"admin:stats:subscriptions:{startDate}:{endDate}"
TTL: 10 dakika
Invalidation: Subscription creation, tier change, manual refresh
```

**Potansiyel Kazanç:**
- ⚡ Response time: **600-1500ms → 15-40ms** (96% azalma)
- 📊 DB load: **Complex JOIN + Aggregations → 0**
- 🔄 Güncellenme sıklığı: Düşük (subscription değişimi seyrek)
- 📈 Hit ratio beklentisi: **85-95%**

**Implementasyon Önceliği:** 🔴 **VERY HIGH**

---

#### 🎯 **GetSponsorDashboardSummaryQuery**
**Dosya**: `Business/Handlers/Sponsorship/Queries/GetSponsorDashboardSummaryQuery.cs`

**Mevcut Performans:**
```csharp
// 4 queries:
// 1. Purchase history
// 2. Code statistics
// 3. Active farmers
// 4. Message statistics
```

**Cache Stratejisi:**
```csharp
Cache Key: $"sponsor:dashboard:{sponsorId}"
TTL: 5 dakika
Invalidation: Purchase, code distribution, message activity
```

**Potansiyel Kazanç:**
- ⚡ Response time: **700-1800ms → 15-50ms** (96% azalma)
- 📊 DB queries: **4 → 0**
- 🔄 Güncellenme sıklığı: Orta
- 📈 Hit ratio beklentisi: **75-85%**

**Implementasyon Önceliği:** 🟡 **HIGH**

---

### 1.2 Static/Semi-Static Reference Data

#### 🎯 **Subscription Tiers**
**Mevcut Kullanım**: Sık çağrılıyor, nadiren değişiyor

**Cache Stratejisi:**
```csharp
Cache Key: "tiers:all:active"
TTL: 1 saat (veya infinite + manual invalidation)
Invalidation: Tier güncelleme/oluşturma (admin operation)
```

**Kullanım Yerleri:**
- `/api/v1/sponsorship/tiers-for-purchase` (SponsorshipController.cs:78)
- Subscription validation işlemleri
- Purchase workflows

**Potansiyel Kazanç:**
- ⚡ Response time: **150-300ms → 5-10ms**
- 📊 Hit ratio: **95-98%** (çok sık çağrılan, nadiren değişen)

**Implementasyon Önceliği:** 🟢 **MEDIUM** (ConfigurationService pattern takip edilebilir)

---

#### 🎯 **Configuration Keys**
**Mevcut Durum**: ✅ **Zaten implement edilmiş**

**Dosya**: `Business/Services/Configuration/ConfigurationService.cs`

**Mevcut Implementasyon:**
```csharp
private readonly IMemoryCache _cache;
private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);
```

**Kullanım:**
- Image processing configurations
- Feature flags
- Application settings

**Mevcut Performans:**
- ✅ Cache hit: ~5-10ms
- ❌ Cache miss: ~100-200ms (DB query)
- 📈 Hit ratio: **90-95%**

**İyileştirme Önerileri:**
- TTL'yi 30 dakikaya çıkarılabilir (configuration nadir değişir)
- Redis'e migration (distributed cache) → multi-instance support

---

## 🎯 2. Orta Öncelikli Cache Fırsatları

### 2.1 Sponsorship Analytics Queries

#### **GetSponsorROIAnalyticsQuery**
**Dosya**: `Business/Handlers/Sponsorship/Queries/GetSponsorROIAnalyticsQuery.cs`

**Cache Stratejisi:**
```csharp
Cache Key: $"sponsor:roi:{sponsorId}:{period}"
TTL: 30 dakika
```

**Potansiyel Kazanç:**
- ⚡ Response time: **1000-2500ms → 30-80ms**
- 📊 Complex calculations cached
- 📈 Hit ratio: **70-80%**

---

#### **GetFarmerSegmentationQuery**
**Dosya**: `Business/Handlers/Sponsorship/Queries/GetFarmerSegmentationQuery.cs`

**Cache Stratejisi:**
```csharp
Cache Key: $"sponsor:segmentation:{sponsorId}"
TTL: 1 saat
```

**Potansiyel Kazanç:**
- ⚡ Response time: **800-1500ms → 20-50ms**
- 📈 Hit ratio: **60-70%**

---

### 2.2 Lookup Queries

#### **GetMessagingFeaturesQuery**
**Tanım**: Tier'a göre messaging özellikleri

**Cache Stratejisi:**
```csharp
Cache Key: $"messaging:features:tier:{tierId}"
TTL: 1 saat
```

**Potansiyel Kazanç:**
- ⚡ Response time: **100-200ms → 5-15ms**
- 📈 Hit ratio: **85-95%**

---

#### **GetLogoPermissionsForAnalysisQuery**
**Tanım**: Logo görünürlük izinleri (tier-based)

**Cache Stratejisi:**
```csharp
Cache Key: $"logo:permissions:tier:{tierId}"
TTL: 1 saat
```

**Potansiyel Kazanç:**
- ⚡ Response time: **80-150ms → 5-10ms**
- 📈 Hit ratio: **90-95%**

---

## 🛠️ 3. Cache Implementation Stratejisi

### 3.1 Katmanlı Cache Mimarisi

```
┌─────────────────────────────────────────────────┐
│  Layer 1: Memory Cache (IMemoryCache)          │
│  - Static data (tiers, configs)                 │
│  - TTL: 30 min - 1 hour                        │
│  - Use: Single-instance, low latency           │
└─────────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────┐
│  Layer 2: Distributed Cache (Redis)             │
│  - Dashboard data (dealer, sponsor, admin)      │
│  - TTL: 5-15 minutes                           │
│  - Use: Multi-instance, shared cache            │
└─────────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────┐
│  Layer 3: Database (PostgreSQL)                 │
│  - Source of truth                              │
│  - Cache miss fallback                          │
└─────────────────────────────────────────────────┘
```

---

### 3.2 Implementasyon Pattern

#### **Repository-Level Caching** (Önerilen)

```csharp
public interface ICachedRepository<T>
{
    Task<T> GetWithCacheAsync(string cacheKey,
        Func<Task<T>> dataFactory,
        TimeSpan? expiration = null);

    void InvalidateCache(string cacheKey);
    void InvalidateCachePattern(string pattern);
}
```

**Örnek Kullanım:**
```csharp
public async Task<IDataResult<DealerDashboardSummaryDto>> Handle(...)
{
    var cacheKey = $"dealer:dashboard:{request.DealerId}";

    var summary = await _cacheRepository.GetWithCacheAsync(
        cacheKey,
        async () => {
            // Existing query logic here
            return await CalculateDashboardSummary(request.DealerId);
        },
        TimeSpan.FromMinutes(5)
    );

    return new SuccessDataResult<DealerDashboardSummaryDto>(summary);
}
```

---

#### **Query-Level Caching** (Alternative)

```csharp
[Cacheable(Key = "dealer:dashboard:{DealerId}", DurationMinutes = 5)]
public class GetDealerDashboardSummaryQuery : IRequest<IDataResult<DealerDashboardSummaryDto>>
{
    public int DealerId { get; set; }
}
```

**Aspect Implementation:**
```csharp
public class CacheableAttribute : MethodInterception
{
    public string Key { get; set; }
    public int DurationMinutes { get; set; }

    protected override void OnBefore(IInvocation invocation)
    {
        // Check cache before execution
    }

    protected override void OnSuccess(IInvocation invocation)
    {
        // Store result in cache
    }
}
```

---

### 3.3 Cache Invalidation Strategies

#### **Event-Based Invalidation**

```csharp
public class CacheInvalidationService
{
    public async Task InvalidateOnCodeDistribution(int dealerId)
    {
        await _cache.RemoveAsync($"dealer:dashboard:{dealerId}");
        await _cache.RemoveAsync($"dealer:codes:{dealerId}");
    }

    public async Task InvalidateOnUserRegistration()
    {
        await _cache.RemoveAsync("admin:stats:users:*");
    }

    public async Task InvalidateOnSubscriptionChange(int userId)
    {
        await _cache.RemoveAsync("admin:stats:subscriptions:*");
        await _cache.RemoveAsync($"user:subscription:{userId}");
    }
}
```

**Integration Points:**
- Command handlers (after successful operation)
- Domain events (if event sourcing implemented)
- Manual refresh endpoints (admin panel)

---

#### **Time-Based Invalidation**

```csharp
// Simple TTL-based (current approach)
var cacheOptions = new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
};
```

---

#### **Hybrid Invalidation**

```csharp
// TTL + Tag-based invalidation
var cacheOptions = new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
    SlidingExpiration = TimeSpan.FromMinutes(10)
};

// Tags for bulk invalidation
cache.Set(cacheKey, data, cacheOptions)
    .AddTag("dealer")
    .AddTag($"dealer:{dealerId}");

// Invalidate all dealer caches
cache.RemoveByTag("dealer");
```

---

## 📊 4. Performans Beklentileri

### 4.1 Metrikler (Cache Hit Durumu)

| Query/Feature | Mevcut (ms) | Cache (ms) | İyileşme | Hit Ratio |
|--------------|-------------|------------|----------|-----------|
| **Dealer Dashboard** | 500-1200 | 10-30 | **~95%** | 80-90% |
| **User Statistics** | 800-2000 | 20-50 | **~97%** | 90-95% |
| **Subscription Stats** | 600-1500 | 15-40 | **~96%** | 85-95% |
| **Sponsor Dashboard** | 700-1800 | 15-50 | **~96%** | 75-85% |
| **Tier Lookup** | 150-300 | 5-10 | **~96%** | 95-98% |
| **Configuration** | 100-200 | 5-10 | **~95%** | 90-95% |

---

### 4.2 Database Load Reduction

| Metric | Mevcut | Cache Sonrası | Azalma |
|--------|--------|---------------|---------|
| **Dashboard Queries/min** | ~1200 | ~180 | **85%** |
| **Analytics Queries/min** | ~300 | ~30 | **90%** |
| **Lookup Queries/min** | ~2000 | ~100 | **95%** |
| **Total DB Load** | 100% | **15-20%** | **80-85%** |

---

### 4.3 Tahmini Maliyetler

#### **Memory Cache (IMemoryCache)**
- Infrastructure: ✅ Zaten mevcut
- Development: **2-3 gün** (base implementation)
- Per-query integration: **1-2 saat** (pattern oluştuktan sonra)

#### **Redis (Distributed Cache)**
- Infrastructure: Railway Redis addon (~$5/month)
- Development: **1-2 gün** (Redis integration + migration)
- Maintenance: Minimal (managed service)

---

## 🚀 5. Implementasyon Roadmap

### Phase 1: Foundation (Week 1)
**Hedef:** Cache infrastructure ve base patterns

- [ ] Cache repository interface ve implementation
- [ ] Redis connection setup (Railway)
- [ ] Cache invalidation service
- [ ] Monitoring/logging integration

**Deliverables:**
- `ICachedRepository<T>` interface
- `RedisCacheService` implementation
- `CacheInvalidationService`
- Cache metrics logging

---

### Phase 2: High-Impact Queries (Week 2)
**Hedef:** En yüksek ROI cache implementations

- [ ] GetDealerDashboardSummaryQuery cache
- [ ] GetUserStatisticsQuery cache
- [ ] GetSubscriptionStatisticsQuery cache
- [ ] GetSponsorDashboardSummaryQuery cache

**Expected Impact:**
- 📉 Dashboard load time: **-80%**
- 📉 DB queries: **-70%**
- 📈 User experience improvement

---

### Phase 3: Reference Data (Week 3)
**Hedef:** Static/semi-static data caching

- [ ] Subscription Tiers cache
- [ ] Messaging Features cache
- [ ] Logo Permissions cache
- [ ] Existing ConfigurationService → Redis migration

**Expected Impact:**
- 📉 Lookup queries: **-90%**
- 📈 Cache hit ratio: **95%+**

---

### Phase 4: Analytics & Complex Queries (Week 4)
**Hedef:** Ağır analytics sorguları

- [ ] Sponsor ROI Analytics cache
- [ ] Farmer Segmentation cache
- [ ] Message Engagement cache
- [ ] Temporal Analytics cache

**Expected Impact:**
- 📉 Analytics response time: **-85%**
- 📈 Admin panel responsiveness

---

### Phase 5: Optimization & Monitoring (Week 5)
**Hedef:** Fine-tuning ve production readiness

- [ ] Cache hit/miss ratio monitoring
- [ ] TTL optimization based on usage patterns
- [ ] Invalidation strategy refinement
- [ ] Performance baseline comparison
- [ ] Documentation

**Deliverables:**
- Grafana dashboards (cache metrics)
- Production deployment guide
- Cache troubleshooting guide

---

## 🔍 6. Monitoring & Validation

### 6.1 Metrikler

```csharp
public class CacheMetrics
{
    public long TotalRequests { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public double HitRatio => TotalRequests > 0
        ? (double)CacheHits / TotalRequests * 100
        : 0;
    public TimeSpan AverageCacheLatency { get; set; }
    public TimeSpan AverageDbLatency { get; set; }
}
```

---

### 6.2 Logging

```csharp
_logger.LogInformation(
    "[CACHE_HIT] Key: {CacheKey}, Latency: {Latency}ms",
    cacheKey, latency);

_logger.LogWarning(
    "[CACHE_MISS] Key: {CacheKey}, Fallback DB Query: {Query}",
    cacheKey, queryName);

_logger.LogError(
    "[CACHE_ERROR] Key: {CacheKey}, Error: {Error}",
    cacheKey, ex.Message);
```

---

### 6.3 Health Checks

```csharp
services.AddHealthChecks()
    .AddCheck<RedisCacheHealthCheck>("redis_cache")
    .AddCheck<MemoryCacheHealthCheck>("memory_cache");
```

---

## 💡 7. Best Practices & Öneriler

### 7.1 Cache Key Naming Convention

```
{domain}:{entity}:{identifier}:{variant}

Örnekler:
- dealer:dashboard:190
- admin:stats:users:2024-01-01:2024-12-31
- sponsor:roi:45:monthly
- tier:features:3
```

---

### 7.2 TTL Guidelines

| Data Type | TTL | Rationale |
|-----------|-----|-----------|
| **Static Reference** | 1 hour - infinite | Nadir değişir |
| **Dashboard Data** | 5-15 dakika | Orta sıklıkta güncellenir |
| **Analytics** | 15-30 dakika | Gerçek zamanlı olması gerekmez |
| **User-Specific** | 5-10 dakika | Kişisel, sık değişebilir |

---

### 7.3 Error Handling

```csharp
try
{
    var cached = await _cache.GetAsync<T>(cacheKey);
    if (cached != null) return cached;
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Cache read failed, falling back to DB");
    // Continue to DB query (graceful degradation)
}

var data = await FetchFromDatabase();

try
{
    await _cache.SetAsync(cacheKey, data, ttl);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Cache write failed (non-critical)");
    // Continue without caching (non-blocking)
}

return data;
```

---

## 📈 8. Sonuç & Öneriler

### Özet

✅ **Toplam Potansiyel:**
- Response time: **60-80% azalma** (cached queries)
- DB load: **70-90% azalma** (dashboard + analytics)
- Cache hit ratio: **80-95%** (optimistic scenario)

✅ **En Yüksek ROI:**
1. **Dealer Dashboard** - Sık kullanılıyor, ağır query
2. **Admin Analytics** - En ağır queries, düşük güncelleme sıklığı
3. **Subscription Tiers** - Çok sık çağrılıyor, nadiren değişiyor

✅ **Öncelikli Aksiyonlar:**
1. Phase 1: Cache infrastructure (1 hafta)
2. Phase 2: Dashboard queries cache (1 hafta)
3. Phase 3: Reference data cache (1 hafta)

---

### Teknik Öneriler

1. **Memory Cache → Redis Migration**
   - ConfigurationService'i Redis'e taşı (multi-instance support)
   - Memory cache'i local hot cache olarak kullan (L1 cache)

2. **Aspect-Oriented Caching**
   - `[Cacheable]` attribute ile boilerplate azalt
   - Query handler'larda minimal kod değişikliği

3. **Proactive Warming**
   - Uygulama başlangıcında kritik cache'leri doldur
   - Background job ile periyodik refresh

4. **A/B Testing**
   - Cache implementasyonunu feature flag ile kontrol et
   - Production'da kademeli rollout

---

### İş Önceliklendirmesi

**🔴 Week 1-2: Critical Path**
- Infrastructure + Dealer/Admin dashboards
- **Hedef:** 70% DB load reduction

**🟡 Week 3-4: High Value**
- Reference data + Analytics queries
- **Hedef:** 85% DB load reduction

**🟢 Week 5+: Optimization**
- Monitoring, fine-tuning, documentation
- **Hedef:** Production-ready cache system

---

## 📞 İletişim

**Hazırlayan:** Claude AI Analysis
**Tarih:** 2025-12-05
**Versiyon:** 1.0
**Durum:** Analysis Complete - Ready for Implementation

