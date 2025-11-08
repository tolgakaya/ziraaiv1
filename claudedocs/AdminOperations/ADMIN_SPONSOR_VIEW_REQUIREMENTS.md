# Admin Sponsor View Requirements & Implementation Plan

## 📋 Executive Summary

Admin kullanıcılarının sponsor perspektifinden veri görebilmesi ve sponsorsuz çiftçilerin analizlerini izleyebilmesi için yeni endpoint'ler tasarlanması gerekiyor.

**Created:** 2025-11-08
**Status:** Planning Phase

---

## 🎯 Requirements

### 1. Admin Sponsor Perspective View
**User Story:** Admin olarak bir sponsor'un ne gördüğünü görmek istiyorum

**Current State:**
- ✅ Sponsorlar kendi kodlarıyla yapılan analizleri görebiliyor (`GET /api/sponsorship/analyses`)
- ✅ Sponsorlar mesajlaşma yapabiliyor (M, L, XL tier için)
- ✅ Sponsorlar farmer iletişim bilgilerini görebiliyor (XL tier için)
- ❌ Admin sponsor perspektifinden bakamıyor

**Desired State:**
- Admin herhangi bir sponsor olarak o sponsor'un gördüğü verileri görebilmeli
- Bu verilere dahil:
  - Sponsor'un kodlarıyla yapılan tüm analizler
  - Her analizdeki mesajlaşmalar (conversation history)
  - Tier-based feature permissions (hangi tier ne görebiliyor)
  - Farmer iletişim bilgileri (tier izin veriyorsa)
  - Analytics ve istatistikler

### 2. Non-Sponsored Farmer Analytics
**User Story:** Admin olarak sponsorsuz çiftçilerin analizlerini görmek istiyorum

**Current State:**
- ✅ Trial kullanıcılar analiz yapabiliyor
- ✅ Kendi subscription satın alan kullanıcılar analiz yapabiliyor
- ❌ Admin bu kullanıcıların analizlerini toplu olarak göremiyor
- ❌ Sponsorsuz vs sponsored analiz ayrımı yok

**Desired State:**
- Admin tüm sponsorsuz analizleri listeleyebilmeli
- Filter'lar:
  - Subscription tipi (Trial, S, M, L, XL)
  - Payment durumu (trial, paid)
  - Date range
  - Farmer bilgileri
  - Analysis status

---

## 🔍 Current System Analysis

### Sponsor Endpoints (SponsorshipController.cs)

```csharp
// Sponsor'un kendi gördüğü endpoint'ler
[Authorize(Roles = "Sponsor,Admin")]
[HttpGet("analyses")]
GetSponsoredAnalysesList() // SponsorId = GetUserId() (current user)

[Authorize(Roles = "Sponsor,Admin")]
[HttpGet("statistics")]
GetSponsorshipStatistics() // SponsorId = GetUserId()

[Authorize(Roles = "Sponsor,Admin")]
[HttpGet("dashboard-summary")]
GetDashboardSummary() // SponsorId = GetUserId()

[Authorize(Roles = "Sponsor,Admin")]
[HttpGet("code-analysis-statistics")]
GetCodeAnalysisStatistics() // SponsorId = GetUserId()
```

**Problem:** Bu endpoint'ler sadece current user için çalışıyor. Admin başka bir sponsor olarak bakamıyor.

### Analysis Query Structure

**GetSponsoredAnalysesListQuery** şu verileri dönüyor:

```csharp
public class SponsoredAnalysisListResponseDto
{
    public SponsoredAnalysisSummaryDto[] Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public SponsoredAnalysisListSummaryDto Summary { get; set; }
}

public class SponsoredAnalysisSummaryDto
{
    // Core Analysis Data
    public int AnalysisId { get; set; }
    public DateTime AnalysisDate { get; set; }
    public string CropType { get; set; }
    public decimal OverallHealthScore { get; set; }

    // Tier-Based Features
    public string TierName { get; set; }
    public bool CanMessage { get; set; }
    public bool CanViewLogo { get; set; }

    // Messaging Data
    public int UnreadMessageCount { get; set; }
    public int TotalMessageCount { get; set; }
    public DateTime? LastMessageDate { get; set; }
    public string LastMessagePreview { get; set; }
    public bool HasUnreadFromFarmer { get; set; }

    // Farmer Data (XL tier only)
    public string FarmerName { get; set; }
    public string FarmerPhone { get; set; }
    public string FarmerEmail { get; set; }
}
```

### Database Schema

**PlantAnalysis Table:**
```csharp
public class PlantAnalysis
{
    public int Id { get; set; }
    public int? UserId { get; set; } // Farmer
    public int? SponsorUserId { get; set; } // Sponsor (code owner)
    public int? DealerId { get; set; } // Dealer (code distributor)
    public int? ActiveSponsorshipId { get; set; } // UserSubscription ID

    // Analysis data
    public DateTime AnalysisDate { get; set; }
    public string CropType { get; set; }
    public decimal OverallHealthScore { get; set; }
    // ... diğer analysis fields
}
```

**Sponsorship Detection:**
- `SponsorUserId != null` → Sponsored analysis
- `SponsorUserId == null` → Non-sponsored (trial veya kendi subscription)

---

## 🏗️ Proposed Solution

### Option 1: Add sponsorId Parameter to Existing Endpoints

**Pros:**
- Minimal kod değişikliği
- Mevcut logic'i kullanır
- Authorization check eklemek kolay

**Cons:**
- Swagger/API docs karmaşıklaşır
- Farmer endpoint'leri ile karışabilir

**Example:**
```csharp
[Authorize(Roles = "Admin")]
[HttpGet("admin/sponsor/{sponsorId}/analyses")]
public async Task<IActionResult> GetSponsorAnalysesAsAdmin(int sponsorId)
{
    var query = new GetSponsoredAnalysesListQuery
    {
        SponsorId = sponsorId // Admin başka sponsor olarak bakıyor
    };
    return Ok(await Mediator.Send(query));
}
```

### Option 2: Create Dedicated Admin Controller

**Pros:**
- Clear separation of concerns
- Admin-specific features eklemek kolay
- Documentation daha temiz

**Cons:**
- Kod duplikasyonu riski
- Maintenance overhead

**Example:**
```csharp
// WebAPI/Controllers/AdminSponsorViewController.cs
[Route("api/admin/sponsor-view")]
public class AdminSponsorViewController : AdminBaseController
{
    [HttpGet("{sponsorId}/analyses")]
    GetSponsorAnalyses(int sponsorId, [FromQuery] filters...)

    [HttpGet("{sponsorId}/messages")]
    GetSponsorMessages(int sponsorId, [FromQuery] filters...)

    [HttpGet("{sponsorId}/statistics")]
    GetSponsorStatistics(int sponsorId)

    [HttpGet("non-sponsored-analyses")]
    GetNonSponsoredAnalyses([FromQuery] filters...)
}
```

### Option 3: Extend AdminSponsorshipController

**Pros:**
- ✅ Tüm admin sponsorship operations tek yerde
- ✅ Existing authorization pattern kullanır
- ✅ Documentation consistency

**Cons:**
- Controller büyük olabilir
- Sponsorship purchase operations ile karışabilir

**Recommendation: Option 3 (Extend AdminSponsorshipController)**

---

## 📝 Implementation Plan

### Phase 1: Admin Sponsor View Endpoints

#### 1.1 Get Sponsor Analyses (Admin View)
```
GET /api/admin/sponsorship/sponsors/{sponsorId}/analyses

Purpose: Admin olarak belirli bir sponsor'un analiz listesini görme
Returns: Sponsor'un GetSponsoredAnalysesList ile aynı formatta veri
```

**Query Parameters:**
- `page`, `pageSize` - Pagination
- `sortBy`, `sortOrder` - Sorting
- `filterByTier` - S, M, L, XL
- `filterByCropType` - Crop type filter
- `startDate`, `endDate` - Date range
- `filterByMessageStatus` - Message status
- `hasUnreadMessages` - Unread filter
- `dealerId` - Specific dealer filter

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "analysisId": 123,
        "analysisDate": "2025-11-08T10:30:00",
        "cropType": "Tomato",
        "overallHealthScore": 85.5,
        "tierName": "XL",
        "canMessage": true,
        "canViewLogo": true,
        "unreadMessageCount": 3,
        "totalMessageCount": 15,
        "farmerName": "Ahmet Yılmaz",
        "farmerPhone": "+905551234567",
        "farmerEmail": "ahmet@example.com"
      }
    ],
    "totalCount": 150,
    "page": 1,
    "pageSize": 20,
    "summary": {
      "totalAnalyses": 150,
      "averageHealthScore": 82.3,
      "contactedAnalyses": 45,
      "notContactedAnalyses": 105
    }
  }
}
```

**Handler:**
```csharp
// Business/Handlers/AdminSponsorship/Queries/GetSponsorAnalysesAsAdminQuery.cs
public class GetSponsorAnalysesAsAdminQuery : IRequest<IDataResult<SponsoredAnalysesListResponseDto>>
{
    public int SponsorId { get; set; } // Admin-specified sponsor

    // Same filter parameters as GetSponsoredAnalysesListQuery
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "date";
    public string SortOrder { get; set; } = "desc";
    public string FilterByTier { get; set; }
    public string FilterByCropType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DealerId { get; set; }
    public string FilterByMessageStatus { get; set; }
    public bool? HasUnreadMessages { get; set; }

    // Handler reuses GetSponsoredAnalysesListQueryHandler logic
}
```

**Operation Claim:**
- Claim ID: 133
- Name: `GetSponsorAnalysesAsAdmin`
- Description: "Admin olarak sponsor analizlerini görüntüleme"

#### 1.2 Get Sponsor Analysis Detail (Admin View)

```
GET /api/admin/sponsorship/sponsors/{sponsorId}/analyses/{analysisId}

Purpose: Admin olarak belirli bir analizin detayını ve mesajlaşma geçmişini görme
Returns: Full analysis details + complete message history
```

**Response:**
```json
{
  "success": true,
  "data": {
    "analysis": {
      "analysisId": 123,
      "analysisDate": "2025-11-08T10:30:00",
      "cropType": "Tomato",
      "overallHealthScore": 85.5,
      "tierName": "XL",
      "farmerInfo": {
        "userId": 456,
        "fullName": "Ahmet Yılmaz",
        "phone": "+905551234567",
        "email": "ahmet@example.com"
      },
      "sponsorInfo": {
        "sponsorId": 159,
        "companyName": "AgriTech Solutions",
        "logoUrl": "https://...",
        "tierName": "XL"
      },
      "codeUsed": {
        "code": "AGRI-XL-2025-001",
        "purchaseId": 789
      }
    },
    "messages": [
      {
        "messageId": 1,
        "sentAt": "2025-11-08T11:00:00",
        "senderRole": "sponsor",
        "senderName": "AgriTech Support",
        "messageText": "Merhaba, bitkilerinize yardımcı olmak isteriz",
        "isRead": true,
        "readAt": "2025-11-08T11:15:00"
      },
      {
        "messageId": 2,
        "sentAt": "2025-11-08T12:00:00",
        "senderRole": "farmer",
        "senderName": "Ahmet Yılmaz",
        "messageText": "Teşekkürler, önerilerinizi deneyeceğim",
        "isRead": false
      }
    ],
    "messageStatistics": {
      "totalMessages": 15,
      "sponsorMessages": 8,
      "farmerMessages": 7,
      "unreadMessages": 3,
      "firstMessageDate": "2025-11-01T10:00:00",
      "lastMessageDate": "2025-11-08T12:00:00",
      "conversationStatus": "Active"
    }
  }
}
```

**Handler:**
```csharp
// Business/Handlers/AdminSponsorship/Queries/GetSponsorAnalysisDetailAsAdminQuery.cs
public class GetSponsorAnalysisDetailAsAdminQuery : IRequest<IDataResult<AdminSponsorAnalysisDetailDto>>
{
    public int SponsorId { get; set; }
    public int AnalysisId { get; set; }
}
```

**Operation Claim:**
- Claim ID: 134
- Name: `GetSponsorAnalysisDetailAsAdmin`
- Description: "Admin olarak sponsor analiz detayı görüntüleme"

#### 1.3 Get Sponsor Messages (Admin View)

```
GET /api/admin/sponsorship/sponsors/{sponsorId}/messages

Purpose: Admin olarak sponsor'un tüm mesajlaşmalarını görme
Returns: Paginated message list across all analyses
```

**Query Parameters:**
- `page`, `pageSize`
- `filterByAnalysisId` - Specific analysis
- `filterByFarmerId` - Specific farmer
- `onlyUnread` - Only unread messages
- `startDate`, `endDate` - Date range

**Operation Claim:**
- Claim ID: 135
- Name: `GetSponsorMessagesAsAdmin`
- Description: "Admin olarak sponsor mesajlarını görüntüleme"

#### 1.4 Send Message As Sponsor (Admin)

```
POST /api/admin/sponsorship/sponsors/{sponsorId}/analyses/{analysisId}/messages

Purpose: Admin olarak sponsor adına mesaj gönderme
Returns: Created message
```

**Request Body:**
```json
{
  "messageText": "Merhaba, size yardımcı olmak isteriz",
  "attachmentUrl": "https://..." // optional
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "messageId": 123,
    "analysisId": 456,
    "sponsorId": 159,
    "messageText": "Merhaba, size yardımcı olmak isteriz",
    "sentAt": "2025-11-08T15:30:00",
    "senderRole": "sponsor",
    "senderName": "AgriTech Solutions"
  },
  "message": "Message sent successfully as sponsor"
}
```

**Operation Claim:**
- Claim ID: 139
- Name: `SendMessageAsSponsor`
- Description: "Admin olarak sponsor adına mesaj gönderme"

### Phase 2: Non-Sponsored Farmer Analytics

#### 2.1 Get Non-Sponsored Analyses

```
GET /api/admin/sponsorship/non-sponsored-analyses

Purpose: Admin olarak sponsorsuz çiftçilerin analizlerini görme
Returns: Analyses where SponsorUserId IS NULL
```

**Query Parameters:**
- `page`, `pageSize`
- `subscriptionType` - Trial, S, M, L, XL
- `paymentStatus` - trial, paid, expired
- `startDate`, `endDate`
- `searchTerm` - Farmer email/name search
- `sortBy` - date, healthScore, subscriptionType
- `sortOrder` - asc, desc

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "analysisId": 789,
        "analysisDate": "2025-11-08T14:00:00",
        "cropType": "Wheat",
        "overallHealthScore": 78.5,
        "farmer": {
          "userId": 123,
          "fullName": "Mehmet Demir",
          "email": "mehmet@example.com",
          "phone": "+905559876543"
        },
        "subscription": {
          "tierName": "Trial",
          "status": "Active",
          "expiryDate": "2025-11-15T00:00:00",
          "paymentStatus": "trial"
        }
      }
    ],
    "totalCount": 450,
    "page": 1,
    "pageSize": 50,
    "summary": {
      "totalAnalyses": 450,
      "byTier": {
        "trial": 320,
        "S": 50,
        "M": 40,
        "L": 25,
        "XL": 15
      },
      "byPaymentStatus": {
        "trial": 320,
        "paid": 100,
        "expired": 30
      },
      "averageHealthScore": 75.2
    }
  }
}
```

**Handler:**
```csharp
// Business/Handlers/AdminSponsorship/Queries/GetNonSponsoredAnalysesQuery.cs
public class GetNonSponsoredAnalysesQuery : IRequest<IDataResult<NonSponsoredAnalysesResponseDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SubscriptionType { get; set; } // Trial, S, M, L, XL
    public string PaymentStatus { get; set; } // trial, paid, expired
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string SearchTerm { get; set; }
    public string SortBy { get; set; } = "date";
    public string SortOrder { get; set; } = "desc";

    public class Handler : IRequestHandler<GetNonSponsoredAnalysesQuery, IDataResult<NonSponsoredAnalysesResponseDto>>
    {
        // Query: WHERE SponsorUserId IS NULL
        // Join: UserSubscription, User, SubscriptionTier
        // Return: Full farmer + subscription info
    }
}
```

**Operation Claim:**
- Claim ID: 136
- Name: `GetNonSponsoredAnalyses`
- Description: "Admin olarak sponsorsuz analizleri görüntüleme"

#### 2.2 Get Non-Sponsored Farmer Detail

```
GET /api/admin/sponsorship/non-sponsored-farmers/{farmerId}

Purpose: Belirli bir sponsorsuz farmer'ın tüm analizleri ve subscription geçmişi
```

**Operation Claim:**
- Claim ID: 137
- Name: `GetNonSponsoredFarmerDetail`
- Description: "Admin olarak sponsorsuz çiftçi detayı görüntüleme"

### Phase 3: Comparison & Analytics

#### 3.1 Get Sponsor vs Non-Sponsor Analytics

```
GET /api/admin/sponsorship/comparison-analytics

Purpose: Sponsored vs Non-sponsored analysis comparison
```

**Response:**
```json
{
  "success": true,
  "data": {
    "sponsored": {
      "totalAnalyses": 15000,
      "totalFarmers": 3500,
      "averageHealthScore": 82.5,
      "topTier": "XL",
      "topTierPercentage": 35
    },
    "nonSponsored": {
      "totalAnalyses": 4500,
      "totalFarmers": 2000,
      "averageHealthScore": 75.2,
      "trialPercentage": 71,
      "paidPercentage": 29
    },
    "trends": {
      "sponsorGrowthRate": 15.5,
      "conversionRate": 8.2
    }
  }
}
```

**Operation Claim:**
- Claim ID: 138
- Name: `GetSponsorshipComparisonAnalytics`
- Description: "Admin sponsor karşılaştırma analitiği görüntüleme"

---

## 🔐 Authorization & Security

### New Operation Claims

```sql
-- Add new admin operation claims for sponsor view functionality
INSERT INTO OperationClaims (Id, Name, Alias, Description, CreatedAt, UpdatedAt)
VALUES
(133, 'GetSponsorAnalysesAsAdmin', 'Admin Sponsor Analyses View', 'Admin olarak sponsor analizlerini görüntüleme', NOW(), NOW()),
(134, 'GetSponsorAnalysisDetailAsAdmin', 'Admin Sponsor Analysis Detail View', 'Admin olarak sponsor analiz detayı görüntüleme', NOW(), NOW()),
(135, 'GetSponsorMessagesAsAdmin', 'Admin Sponsor Messages View', 'Admin olarak sponsor mesajlarını görüntüleme', NOW(), NOW()),
(136, 'GetNonSponsoredAnalyses', 'Admin Non-Sponsored Analyses View', 'Admin olarak sponsorsuz analizleri görüntüleme', NOW(), NOW()),
(137, 'GetNonSponsoredFarmerDetail', 'Admin Non-Sponsored Farmer Detail', 'Admin olarak sponsorsuz çiftçi detayı görüntüleme', NOW(), NOW()),
(138, 'GetSponsorshipComparisonAnalytics', 'Admin Sponsorship Comparison Analytics', 'Admin sponsor karşılaştırma analitiği görüntüleme', NOW(), NOW()),
(139, 'SendMessageAsSponsor', 'Admin Send Message As Sponsor', 'Admin olarak sponsor adına mesaj gönderme', NOW(), NOW());

-- Grant to Administrators group (GroupId = 1)
INSERT INTO GroupClaims (GroupId, ClaimId)
VALUES
(1, 133),
(1, 134),
(1, 135),
(1, 136),
(1, 137),
(1, 138),
(1, 139);
```

### Authorization Pattern

```csharp
[SecuredOperation(Priority = 1)]
[RequiresClaim("GetSponsorAnalysesAsAdmin")]
[LogAspect(typeof(FileLogger))]
public async Task<IDataResult<...>> Handle(GetSponsorAnalysesAsAdminQuery request, ...)
{
    // Admin-only logic
}
```

---

## 📊 Data Access Patterns

### Sponsor View Queries

```csharp
// Get analyses for specific sponsor
var analyses = _plantAnalysisRepository.Query()
    .Where(a => a.SponsorUserId == sponsorId || a.DealerId == sponsorId)
    .Where(a => a.AnalysisStatus != null);

// Get messages for specific sponsor analyses
var messages = _analysisMessageRepository.Query()
    .Where(m => m.SponsorUserId == sponsorId);
```

### Non-Sponsored Queries

```csharp
// Get non-sponsored analyses
var nonSponsoredAnalyses = _plantAnalysisRepository.Query()
    .Where(a => a.SponsorUserId == null)
    .Include(a => a.User) // Farmer
    .Include(a => a.ActiveSponsorship) // UserSubscription
        .ThenInclude(s => s.SubscriptionTier); // Tier info
```

---

## 🧪 Testing Strategy

### Unit Tests

```csharp
[Fact]
public async Task GetSponsorAnalysesAsAdmin_ValidSponsorId_ReturnsAnalyses()
{
    // Arrange
    var sponsorId = 159;
    var query = new GetSponsorAnalysesAsAdminQuery { SponsorId = sponsorId };

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.True(result.Success);
    Assert.NotEmpty(result.Data.Items);
    Assert.All(result.Data.Items, item =>
        Assert.True(item.SponsorInfo.SponsorId == sponsorId));
}

[Fact]
public async Task GetNonSponsoredAnalyses_TrialFilter_ReturnsOnlyTrialAnalyses()
{
    // Arrange
    var query = new GetNonSponsoredAnalysesQuery
    {
        SubscriptionType = "Trial"
    };

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.True(result.Success);
    Assert.All(result.Data.Items, item =>
        Assert.Equal("Trial", item.Subscription.TierName));
}
```

### Integration Tests

```csharp
[Fact]
public async Task AdminSponsorView_E2E_Success()
{
    // 1. Create sponsor with profile
    // 2. Create purchase and codes
    // 3. Farmer redeems code
    // 4. Farmer creates analysis
    // 5. Admin gets sponsor analyses
    // 6. Verify admin sees farmer's analysis
}
```

---

## 📚 Documentation Updates

### API Documentation Files

1. **`claudedocs/AdminOperations/ADMIN_SPONSOR_VIEW_API.md`**
   - All new endpoints
   - Request/response examples
   - Authorization requirements
   - Use cases

2. **`claudedocs/AdminOperations/NON_SPONSORED_ANALYTICS_API.md`**
   - Non-sponsored farmer endpoints
   - Filtering and sorting options
   - Analytics capabilities

3. Update **`ADMIN_SPONSORSHIP_OPERATIONS_GUIDE.md`**
   - Add new operations to summary
   - Link to new documentation

---

## 🚀 Implementation Timeline

### Week 1: Sponsor View Endpoints
- Day 1-2: Create queries and handlers
- Day 3: Add controller endpoints
- Day 4: Write unit tests
- Day 5: Integration testing & documentation

### Week 2: Non-Sponsored Analytics
- Day 1-2: Create queries and handlers
- Day 3: Add controller endpoints
- Day 4: Write unit tests
- Day 5: Integration testing & documentation

### Week 3: Comparison Analytics & Polish
- Day 1-2: Comparison analytics endpoint
- Day 3: End-to-end testing
- Day 4-5: Documentation finalization, deployment

---

## 💡 Additional Considerations

### Performance Optimization

```csharp
// Use pagination for large datasets
public int MaxPageSize { get; set; } = 100; // Prevent excessive load

// Index suggestions for database
CREATE INDEX idx_plantanalysis_sponsoruserid_date
ON PlantAnalysis(SponsorUserId, AnalysisDate DESC);

CREATE INDEX idx_plantanalysis_nonsponsor_date
ON PlantAnalysis(AnalysisDate DESC)
WHERE SponsorUserId IS NULL;
```

### Caching Strategy

```csharp
// Cache sponsor analytics for 15 minutes
[CacheAspect(Duration = 15)]
public async Task<IDataResult<...>> GetSponsorStatistics(...)
```

### Audit Logging

```csharp
// Log all admin sponsor view access
[AdminOperationLog(Operation = "ViewSponsorData")]
public async Task<IActionResult> GetSponsorAnalyses(int sponsorId)
{
    // Operation logged with:
    // - AdminUserId
    // - TargetSponsorId
    // - Timestamp
    // - IP Address
}
```

---

## ✅ User Requirements (Confirmed)

1. **Message Access:** ✅ Admin mesaj gönderebilmeli (read + write)
   - Admin sponsor olarak mesaj okuyabilir
   - Admin sponsor olarak mesaj gönderebilir
   - Mesajlar sponsor adına gönderilir

2. **Farmer Contact Info:** ✅ Admin tüm bilgileri görebilmeli
   - Non-sponsored farmer'ların iletişim bilgileri her zaman gösterilir

3. **Real-time Updates:** ❌ Şimdilik gerek yok
   - Sayfa refresh yeterli (performance için)
   - Future enhancement olarak planlanabilir

4. **Export Functionality:** ❌ Şimdilik gerek yok
   - CSV/Excel export Phase 4 olarak planlanabilir
   - Future enhancement olarak planlanabilir

5. **Notification:** ❌ Gerek yok
   - Admin erişimi şeffaf (sponsor bilgilendirilmez)
   - Audit log'da kayıt tutulur ama sponsor'a bildirim gitmez

---

## ✅ Success Criteria

1. ✅ Admin herhangi bir sponsor olarak tüm verileri görebiliyor
2. ✅ Admin sponsorsuz farmer'ları filtreleyip analiz edebiliyor
3. ✅ Tüm tier-based permissions doğru çalışıyor
4. ✅ Performance acceptable (<2s response time)
5. ✅ Comprehensive documentation mevcut
6. ✅ Unit test coverage >80%
7. ✅ Integration tests passing
8. ✅ Authorization checks working
9. ✅ Audit logging operational

---

## 📝 Next Steps

1. **User Approval:** Bu planı gözden geçir ve onay ver
2. **Implementation Start:** Phase 1'den başla
3. **Iterative Development:** Her phase sonrası test ve review
4. **Documentation:** Her endpoint için detaylı doküman
5. **Deployment:** Production'a kademeli rollout

**Status:** ✅ Approved by user - Ready for implementation

**User Confirmed Requirements (2025-11-08):**
- ✅ Admin CAN send messages as sponsor (read + write access)
- ❌ Real-time updates NOT needed (page refresh sufficient)
- ❌ Export functionality NOT needed (future enhancement)
- ❌ Sponsor notification NOT needed (transparent admin access)
