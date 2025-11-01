# Dealer Dashboard - Missing Endpoint Analysis

**Date**: 2025-10-31
**Issue**: Dealer dashboard için "bana transfer edilmiş kodlar" endpoint'i eksik

---

## 🎯 İhtiyaç

**Senaryo**: Dealer (User 158) login olduğunda dashboard'ında görmesi gerekenler:

### Dealer Dashboard Bileşenleri

```
┌─────────────────────────────────────────────────┐
│  Bana Transfer Edilmiş Kodlar                   │
│  ──────────────────────────────────────────────  │
│  • Toplam Alınan: 50 kod                         │
│  • Farmer'lara Gönderilmiş: 40 kod              │
│  • Kullanılmış: 30 kod                           │
│  • Mevcut (Kullanılabilir): 10 kod               │
│  • Geri Alınan: 0 kod                            │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  Bekleyen Davetiyelerim (3)                      │
│  ──────────────────────────────────────────────  │
│  🏢 dort tarim - 20 kod - 5 gün kaldı            │
│  🏢 Green Farm - 30 kod - 2 gün kaldı            │
│  🏢 Agro Corp  - 15 kod - 6 gün kaldı            │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  Kod Listesi (Detaylı)                           │
│  ──────────────────────────────────────────────  │
│  AGRI-2025-ABC123 | S | Available | 2026-01-15  │
│  AGRI-2025-DEF456 | M | Sent      | 2026-01-20  │
│  AGRI-2025-GHI789 | L | Used      | 2026-02-01  │
└─────────────────────────────────────────────────┘
```

---

## ❌ Mevcut Durum: Endpoint Eksik

### Mevcut `/api/v1/sponsorship/codes` Endpoint'i

**Şu anda ne yapıyor:**
```csharp
public class GetSponsorshipCodesQuery
{
    public int SponsorId { get; set; } // Sadece SponsorId'ye göre filtreleme
    public bool OnlyUnused { get; set; }
    public bool OnlyUnsent { get; set; }
    // ...
}
```

**Problem:**
- ❌ `DealerId` parametresi yok
- ❌ Sadece sponsor'un kendisinin kodlarını döner
- ❌ Dealer'a transfer edilmiş kodları çekemiyor

### Entity'de Field Var

```csharp
public class SponsorshipCode
{
    public int? DealerId { get; set; } // ✅ Field mevcut
    public DateTime? TransferredAt { get; set; } // ✅ Transfer tarihi mevcut
    public DateTime? ReclaimedAt { get; set; } // ✅ Geri alma tarihi mevcut
}
```

**Field'lar hazır ama endpoint yok!**

---

## ✅ Çözüm: Yeni Endpoint Gerekli

### Önerilen Endpoint

```
GET /api/v1/sponsorship/dealer/my-codes
```

**Authorization**: `[Authorize(Roles = "Dealer,Sponsor")]`

**Query Parameters**:
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `onlyUnused` (bool, default: false)
- `onlyAvailable` (bool, default: false) - Not sent to farmers yet
- `includePending` (bool, default: false) - Include pending invitations

---

## 📋 Implementation Plan

### 1. Query Handler

```csharp
// Business/Handlers/Sponsorship/Queries/GetDealerCodesQuery.cs

public class GetDealerCodesQuery : IRequest<IDataResult<SponsorshipCodesPaginatedDto>>
{
    public int DealerId { get; set; } // Authenticated dealer ID
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public bool OnlyUnused { get; set; } = false;
    public bool OnlyAvailable { get; set; } = false;
    public bool IncludePending { get; set; } = false;
}

public class GetDealerCodesQueryHandler : IRequestHandler<GetDealerCodesQuery, IDataResult<SponsorshipCodesPaginatedDto>>
{
    private readonly ISponsorshipCodeRepository _codeRepository;
    private readonly IDealerInvitationRepository _invitationRepository;

    public GetDealerCodesQueryHandler(
        ISponsorshipCodeRepository codeRepository,
        IDealerInvitationRepository invitationRepository)
    {
        _codeRepository = codeRepository;
        _invitationRepository = invitationRepository;
    }

    public async Task<IDataResult<SponsorshipCodesPaginatedDto>> Handle(
        GetDealerCodesQuery request,
        CancellationToken cancellationToken)
    {
        // Base query: codes transferred to this dealer
        var query = _codeRepository.Query()
            .Where(c => c.DealerId == request.DealerId && c.ReclaimedAt == null);

        // Apply filters
        if (request.OnlyUnused)
        {
            query = query.Where(c => !c.IsUsed);
        }

        if (request.OnlyAvailable)
        {
            query = query.Where(c => !c.IsUsed &&
                                     c.DistributionDate == null &&
                                     c.ExpiryDate > DateTime.Now);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Paginate
        var codes = await query
            .OrderByDescending(c => c.TransferredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var result = new SponsorshipCodesPaginatedDto
        {
            Codes = codes.Select(c => MapToDto(c)).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };

        return new SuccessDataResult<SponsorshipCodesPaginatedDto>(result);
    }

    private SponsorshipCodeDto MapToDto(SponsorshipCode code)
    {
        return new SponsorshipCodeDto
        {
            Id = code.Id,
            Code = code.Code,
            SubscriptionTier = GetTierName(code.SubscriptionTierId),
            IsUsed = code.IsUsed,
            IsActive = code.IsActive,
            ExpiryDate = code.ExpiryDate,
            CreatedDate = code.CreatedDate,
            TransferredAt = code.TransferredAt,
            DistributionDate = code.DistributionDate,
            UsedDate = code.UsedDate,
            RecipientPhone = code.RecipientPhone,
            RecipientName = code.RecipientName
        };
    }
}
```

### 2. Controller Endpoint

```csharp
// WebAPI/Controllers/SponsorshipController.cs

/// <summary>
/// Get codes transferred to current dealer
/// Dealer can see codes they received from sponsors
/// </summary>
[Authorize(Roles = "Dealer,Sponsor")]
[HttpGet("dealer/my-codes")]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<SponsorshipCodesPaginatedDto>))]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetMyDealerCodes(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    [FromQuery] bool onlyUnused = false,
    [FromQuery] bool onlyAvailable = false)
{
    try
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var query = new GetDealerCodesQuery
        {
            DealerId = userId.Value,
            Page = page,
            PageSize = pageSize,
            OnlyUnused = onlyUnused,
            OnlyAvailable = onlyAvailable
        };

        var result = await Mediator.Send(query);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting dealer codes for user {UserId}", GetUserId());
        return StatusCode(500, new ErrorResult($"Dealer codes retrieval failed: {ex.Message}"));
    }
}
```

### 3. Summary Endpoint (Optional but Recommended)

```csharp
// Business/Handlers/Sponsorship/Queries/GetDealerDashboardSummaryQuery.cs

public class GetDealerDashboardSummaryQuery : IRequest<IDataResult<DealerDashboardSummaryDto>>
{
    public int DealerId { get; set; }
}

public class DealerDashboardSummaryDto
{
    public int TotalCodesReceived { get; set; }
    public int CodesSent { get; set; }
    public int CodesUsed { get; set; }
    public int CodesAvailable { get; set; }
    public int CodesReclaimed { get; set; }
    public decimal UsageRate { get; set; }
    public int PendingInvitationsCount { get; set; }
    public List<PendingInvitationSummary> PendingInvitations { get; set; }
}

// Controller
[Authorize(Roles = "Dealer,Sponsor")]
[HttpGet("dealer/my-dashboard")]
public async Task<IActionResult> GetMyDealerDashboard()
{
    var userId = GetUserId();
    if (!userId.HasValue)
        return Unauthorized();

    var query = new GetDealerDashboardSummaryQuery { DealerId = userId.Value };
    var result = await Mediator.Send(query);

    return result.Success ? Ok(result) : BadRequest(result);
}
```

---

## 📊 Response Examples

### GET /dealer/my-codes

```json
{
  "data": {
    "codes": [
      {
        "id": 945,
        "code": "AGRI-2025-36767AD6",
        "subscriptionTier": "L",
        "isUsed": false,
        "isActive": true,
        "expiryDate": "2026-01-29T07:29:31.763",
        "createdDate": "2025-10-29T07:29:31.763",
        "transferredAt": "2025-10-31T08:31:22.980",
        "distributionDate": null,
        "usedDate": null,
        "recipientPhone": null,
        "recipientName": null
      }
    ],
    "totalCount": 50,
    "page": 1,
    "pageSize": 50,
    "totalPages": 1
  },
  "success": true,
  "message": "Dealer codes retrieved successfully"
}
```

### GET /dealer/my-dashboard

```json
{
  "data": {
    "totalCodesReceived": 50,
    "codesSent": 40,
    "codesUsed": 30,
    "codesAvailable": 10,
    "codesReclaimed": 0,
    "usageRate": 75.0,
    "pendingInvitationsCount": 3,
    "pendingInvitations": [
      {
        "invitationId": 7,
        "sponsorCompanyName": "dort tarim",
        "codeCount": 20,
        "expiresAt": "2025-11-07T08:31:22.790",
        "remainingDays": 6
      }
    ]
  },
  "success": true
}
```

---

## 🧪 Test Scenarios

### Test 1: Get Dealer Codes

```bash
# Login as dealer (User 158)
TOKEN="dealer_jwt_token_here"

# Get all codes
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/my-codes" \
  -H "Authorization: Bearer $TOKEN" \
  -H "x-dev-arch-version: 1.0"

# Get only available codes
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/my-codes?onlyAvailable=true" \
  -H "Authorization: Bearer $TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

### Test 2: Get Dashboard Summary

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/my-dashboard" \
  -H "Authorization: Bearer $TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

---

## 📱 Mobile Integration

### Flutter Example

```dart
class DealerDashboardService {
  final Dio _dio;
  final String _jwtToken;

  DealerDashboardService(this._dio, this._jwtToken);

  Future<DealerDashboardData> getDashboardData() async {
    try {
      // Get dashboard summary
      final summaryResponse = await _dio.get(
        '/api/v1/sponsorship/dealer/my-dashboard',
        options: Options(headers: {
          'Authorization': 'Bearer $_jwtToken',
          'x-dev-arch-version': '1.0',
        }),
      );

      return DealerDashboardData.fromJson(summaryResponse.data['data']);
    } catch (e) {
      print('Error loading dealer dashboard: $e');
      rethrow;
    }
  }

  Future<List<SponsorshipCodeDto>> getMyCodes({
    int page = 1,
    int pageSize = 50,
    bool onlyAvailable = false,
  }) async {
    try {
      final response = await _dio.get(
        '/api/v1/sponsorship/dealer/my-codes',
        queryParameters: {
          'page': page,
          'pageSize': pageSize,
          'onlyAvailable': onlyAvailable,
        },
        options: Options(headers: {
          'Authorization': 'Bearer $_jwtToken',
          'x-dev-arch-version': '1.0',
        }),
      );

      final List<dynamic> codes = response.data['data']['codes'];
      return codes.map((json) => SponsorshipCodeDto.fromJson(json)).toList();
    } catch (e) {
      print('Error loading dealer codes: $e');
      rethrow;
    }
  }
}

class DealerDashboardData {
  final int totalCodesReceived;
  final int codesSent;
  final int codesUsed;
  final int codesAvailable;
  final int codesReclaimed;
  final double usageRate;
  final int pendingInvitationsCount;
  final List<PendingInvitationSummary> pendingInvitations;

  DealerDashboardData({
    required this.totalCodesReceived,
    required this.codesSent,
    required this.codesUsed,
    required this.codesAvailable,
    required this.codesReclaimed,
    required this.usageRate,
    required this.pendingInvitationsCount,
    required this.pendingInvitations,
  });

  factory DealerDashboardData.fromJson(Map<String, dynamic> json) {
    return DealerDashboardData(
      totalCodesReceived: json['totalCodesReceived'],
      codesSent: json['codesSent'],
      codesUsed: json['codesUsed'],
      codesAvailable: json['codesAvailable'],
      codesReclaimed: json['codesReclaimed'],
      usageRate: json['usageRate'].toDouble(),
      pendingInvitationsCount: json['pendingInvitationsCount'],
      pendingInvitations: (json['pendingInvitations'] as List)
          .map((i) => PendingInvitationSummary.fromJson(i))
          .toList(),
    );
  }
}
```

---

## ⏱️ Estimated Development Time

- **Query Handler**: 1-2 hours
- **Controller Endpoint**: 30 minutes
- **Dashboard Summary (Optional)**: 1-2 hours
- **Testing**: 1 hour
- **Documentation**: 30 minutes

**Total**: ~3-6 hours

---

## ✅ Acceptance Criteria

- [ ] Dealer can GET their transferred codes via `/dealer/my-codes`
- [ ] Filtering works: `onlyUnused`, `onlyAvailable`
- [ ] Pagination works correctly
- [ ] Only dealer's own codes returned (DealerId filter)
- [ ] Reclaimed codes excluded (ReclaimedAt != null)
- [ ] Dashboard summary endpoint (optional but recommended)
- [ ] Authorization: only Dealer or Sponsor roles
- [ ] Unit tests written
- [ ] Integration tests written
- [ ] Mobile team documentation provided

---

## 🔗 Related Endpoints

**Already Exists:**
- ✅ `GET /dealer/invitations/my-pending` - Pending invitations
- ✅ `GET /dealer/summary` - Sponsor view of all dealers
- ✅ `GET /dealer/analytics/{dealerId}` - Sponsor view of specific dealer

**Missing (Needed for Dealer Dashboard):**
- ❌ `GET /dealer/my-codes` - **THIS IS WHAT'S NEEDED**
- ❌ `GET /dealer/my-dashboard` - Summary endpoint (recommended)

---

**Document Version**: 1.0
**Created**: 2025-10-31
**Status**: ❌ Endpoint Missing - Development Required
**Priority**: 🔴 High (Blocks dealer dashboard feature)
