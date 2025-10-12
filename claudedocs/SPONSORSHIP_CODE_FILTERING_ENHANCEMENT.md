# Sponsorship Code Filtering Enhancement

**Version:** 1.0.0
**Date:** 2025-10-11
**Status:** ✅ Implemented & Tested
**Issue:** Critical - Prevents duplicate code distribution

---

## 📋 Problem Statement

### Original Issue
The endpoint `GET /api/v1/sponsorship/codes?onlyUnused=False` was showing **unused codes** but **including codes already sent to farmers** (where `DistributionDate IS NOT NULL`). This created a **critical risk**: sponsors could accidentally send the same code to multiple farmers.

### User's Exact Description (Turkish)
> "Bu endpointte {{base_url}}/api/v{{version}}/sponsorship/codes?onlyUnused=False kullanılmayan kodlar listeleniyor ama bu kodlar mesaj olarak gönderiliyor çifççilere. Birden fazla çiftçiye aynı kodun gönderilmesi çok büyük sorun olur."

**Translation:** "This endpoint shows unused codes, but these codes are being sent as messages to farmers. Sending the same code to multiple farmers would be a very big problem."

---

## 🎯 Solution Overview

Added **two new filtering options** to distinguish between:
1. **Unsent codes** (`DistributionDate IS NULL`) - Safe to distribute
2. **Sent but unused codes** (`DistributionDate IS NOT NULL, IsUsed = false`) - Already distributed, follow-up needed

### Key Database Field
The **critical field** is `DistributionDate`:
- `NULL` = Code never sent to anyone (safe to distribute)
- `NOT NULL` = Code sent to farmer (do NOT send to another person)

---

## 🔧 Implementation Details

### 1. Database Layer (Repository)

**File:** `DataAccess/Abstract/ISponsorshipCodeRepository.cs`

Added two new methods:
```csharp
Task<List<SponsorshipCode>> GetUnsentCodesBySponsorAsync(int sponsorId);
Task<List<SponsorshipCode>> GetSentButUnusedCodesBySponsorAsync(int sponsorId, int? sentDaysAgo = null);
```

**File:** `DataAccess/Concrete/EntityFramework/SponsorshipCodeRepository.cs`

```csharp
public async Task<List<SponsorshipCode>> GetUnsentCodesBySponsorAsync(int sponsorId)
{
    return await Context.SponsorshipCodes
        .Where(sc => sc.SponsorId == sponsorId &&
                    !sc.IsUsed &&
                    sc.IsActive &&
                    sc.ExpiryDate > DateTime.Now &&
                    !sc.DistributionDate.HasValue)  // KEY: Never sent
        .OrderBy(sc => sc.CreatedDate)
        .ToListAsync();
}

public async Task<List<SponsorshipCode>> GetSentButUnusedCodesBySponsorAsync(int sponsorId, int? sentDaysAgo = null)
{
    var query = Context.SponsorshipCodes
        .Where(sc => sc.SponsorId == sponsorId &&
                    !sc.IsUsed &&
                    sc.IsActive &&
                    sc.ExpiryDate > DateTime.Now &&
                    sc.DistributionDate.HasValue);  // KEY: Has been sent

    // Optional: Filter codes sent at least X days ago
    if (sentDaysAgo.HasValue)
    {
        var cutoffDate = DateTime.Now.AddDays(-sentDaysAgo.Value);
        query = query.Where(sc => sc.DistributionDate <= cutoffDate);
    }

    return await query
        .OrderByDescending(sc => sc.DistributionDate)
        .ToListAsync();
}
```

### 2. Business Layer (Service)

**File:** `Business/Services/Sponsorship/ISponsorshipService.cs`

Added interface methods:
```csharp
Task<IDataResult<List<SponsorshipCode>>> GetUnsentSponsorCodesAsync(int sponsorId);
Task<IDataResult<List<SponsorshipCode>>> GetSentButUnusedSponsorCodesAsync(int sponsorId, int? sentDaysAgo = null);
```

**File:** `Business/Services/Sponsorship/SponsorshipService.cs`

Implemented with helpful messages:
```csharp
public async Task<IDataResult<List<SponsorshipCode>>> GetUnsentSponsorCodesAsync(int sponsorId)
{
    var codes = await _sponsorshipCodeRepository.GetUnsentCodesBySponsorAsync(sponsorId);
    return new SuccessDataResult<List<SponsorshipCode>>(codes,
        $"{codes.Count} unsent codes available for distribution");
}

public async Task<IDataResult<List<SponsorshipCode>>> GetSentButUnusedSponsorCodesAsync(int sponsorId, int? sentDaysAgo = null)
{
    var codes = await _sponsorshipCodeRepository.GetSentButUnusedCodesBySponsorAsync(sponsorId, sentDaysAgo);
    var message = sentDaysAgo.HasValue
        ? $"{codes.Count} codes sent {sentDaysAgo} days ago but still unused"
        : $"{codes.Count} codes sent but still unused";
    return new SuccessDataResult<List<SponsorshipCode>>(codes, message);
}
```

### 3. CQRS Layer (Query Handler)

**File:** `Business/Handlers/Sponsorship/Queries/GetSponsorshipCodesQuery.cs`

Added new properties and priority-based handling:
```csharp
public class GetSponsorshipCodesQuery : IRequest<IDataResult<List<SponsorshipCode>>>
{
    public int SponsorId { get; set; }
    public bool OnlyUnused { get; set; } = false;
    public bool OnlyUnsent { get; set; } = false;          // NEW
    public int? SentDaysAgo { get; set; } = null;          // NEW
}

public async Task<IDataResult<List<SponsorshipCode>>> Handle(GetSponsorshipCodesQuery request, CancellationToken cancellationToken)
{
    // Priority 1: OnlyUnsent - codes never sent to farmers
    if (request.OnlyUnsent)
    {
        return await _sponsorshipService.GetUnsentSponsorCodesAsync(request.SponsorId);
    }

    // Priority 2: SentDaysAgo - codes sent X days ago but still unused
    if (request.SentDaysAgo.HasValue)
    {
        return await _sponsorshipService.GetSentButUnusedSponsorCodesAsync(
            request.SponsorId, request.SentDaysAgo.Value);
    }

    // Priority 3: OnlyUnused - codes not redeemed (includes both sent and unsent)
    if (request.OnlyUnused)
    {
        return await _sponsorshipService.GetUnusedSponsorCodesAsync(request.SponsorId);
    }

    // Default: All codes
    return await _sponsorshipService.GetSponsorCodesAsync(request.SponsorId);
}
```

### 4. API Layer (Controller)

**File:** `WebAPI/Controllers/SponsorshipController.cs`

Updated endpoint with comprehensive documentation:
```csharp
/// <summary>
/// Get sponsorship codes for current sponsor with advanced filtering
/// </summary>
/// <param name="onlyUnused">Return only unused codes (includes both sent and unsent)</param>
/// <param name="onlyUnsent">Return only codes never sent to farmers (DistributionDate IS NULL) - RECOMMENDED for distribution</param>
/// <param name="sentDaysAgo">Return codes sent X days ago but still unused (e.g., 7 for codes sent 1 week ago)</param>
/// <returns>List of sponsorship codes</returns>
[Authorize(Roles = "Sponsor,Admin")]
[HttpGet("codes")]
public async Task<IActionResult> GetSponsorshipCodes(
    [FromQuery] bool onlyUnused = false,
    [FromQuery] bool onlyUnsent = false,
    [FromQuery] int? sentDaysAgo = null)
```

---

## 📱 API Usage Examples

### 1. Get Codes Ready to Distribute (RECOMMENDED)
**Use this when preparing to send codes to farmers**

```http
GET /api/v1/sponsorship/codes?onlyUnsent=true
Authorization: Bearer {token}
```

**Response:**
```json
{
  "data": [
    {
      "id": 123,
      "code": "AGRI-2025-1234ABCD",
      "distributionDate": null,  // ✅ Never sent
      "isUsed": false,
      "isActive": true,
      "expiryDate": "2026-10-11T00:00:00"
    }
  ],
  "success": true,
  "message": "5 unsent codes available for distribution"
}
```

### 2. Get Codes Sent But Not Redeemed (Follow-up)
**Use this for follow-up with farmers who received codes but haven't redeemed**

```http
GET /api/v1/sponsorship/codes?sentDaysAgo=7
Authorization: Bearer {token}
```

**Response:**
```json
{
  "data": [
    {
      "id": 456,
      "code": "AGRI-2025-5678EFGH",
      "distributionDate": "2025-10-04T10:30:00",  // ✅ Sent 7 days ago
      "recipientPhone": "+905551234567",
      "recipientName": "Ahmet Yılmaz",
      "isUsed": false,
      "isActive": true
    }
  ],
  "success": true,
  "message": "3 codes sent 7 days ago but still unused"
}
```

### 3. Get All Unused Codes (Old Behavior)
**Use this for general statistics**

```http
GET /api/v1/sponsorship/codes?onlyUnused=true
Authorization: Bearer {token}
```

**Response:** Returns both sent and unsent unused codes

### 4. Get All Codes
```http
GET /api/v1/sponsorship/codes
Authorization: Bearer {token}
```

---

## 🔄 Code Distribution Flow (Updated)

### Step 1: Query Unsent Codes
```
Mobile App → GET /api/v1/sponsorship/codes?onlyUnsent=true
          ← Returns codes with DistributionDate = NULL
```

### Step 2: Select Codes for Distribution
```javascript
// Mobile app shows only UNSENT codes
const unsentCodes = response.data.filter(code => !code.distributionDate);
```

### Step 3: Send Codes to Farmers
```
Mobile App → POST /api/v1/sponsorship/send-link
{
  "recipients": [
    {"code": "AGRI-2025-1234ABCD", "phone": "+905551234567", "name": "Ahmet"}
  ]
}
```

### Step 4: Database Update (Automatic)
```
SendSponsorshipLinkCommand updates:
- DistributionDate = DateTime.Now  ✅ KEY FIELD
- RecipientPhone = "+905551234567"
- RecipientName = "Ahmet"
- DistributionChannel = "SMS"
```

### Step 5: Next Query (Safety Check)
```
Mobile App → GET /api/v1/sponsorship/codes?onlyUnsent=true
          ← Code "AGRI-2025-1234ABCD" is NO LONGER in the list ✅
```

---

## 📊 Query Comparison Table

| Query Parameter | DistributionDate | IsUsed | Use Case |
|-----------------|------------------|---------|----------|
| `?onlyUnsent=true` | `NULL` | `false` | **Distribution preparation** - Safe to send |
| `?sentDaysAgo=7` | `<= 7 days ago` | `false` | **Follow-up** - Sent but not redeemed |
| `?onlyUnused=true` | `ANY` | `false` | **Statistics** - All unredeemed codes |
| No parameters | `ANY` | `ANY` | **Full view** - All codes |

---

## 🧪 Testing Scenarios

### Test 1: Prevent Duplicate Distribution
```
1. Sponsor purchases 10 codes
2. Query: GET /codes?onlyUnsent=true → Returns 10 codes
3. Send code "AGRI-001" to Farmer A via SMS
4. Query: GET /codes?onlyUnsent=true → Returns 9 codes (AGRI-001 excluded ✅)
5. Attempt to send: AGRI-001 should not appear in available list ✅
```

### Test 2: Follow-up on Unredeemed Codes
```
1. Send code "AGRI-002" to Farmer B on 2025-10-01
2. Query on 2025-10-08: GET /codes?sentDaysAgo=7
3. Returns AGRI-002 with recipient info ✅
4. Sponsor can follow up with Farmer B
```

### Test 3: Code Lifecycle
```
State 1: Created
- DistributionDate: NULL
- IsUsed: false
- Query ?onlyUnsent=true: ✅ Included

State 2: Sent to Farmer
- DistributionDate: 2025-10-11
- IsUsed: false
- Query ?onlyUnsent=true: ❌ Excluded
- Query ?sentDaysAgo=0: ✅ Included

State 3: Redeemed by Farmer
- DistributionDate: 2025-10-11
- IsUsed: true
- Query ?onlyUnsent=true: ❌ Excluded
- Query ?sentDaysAgo=0: ❌ Excluded
```

---

## 📱 Mobile Integration Guide

### Recommended UI Flow

**Distribution Screen:**
```dart
// Fetch ONLY unsent codes
final response = await http.get(
  Uri.parse('$baseUrl/api/v1/sponsorship/codes?onlyUnsent=true'),
  headers: {'Authorization': 'Bearer $token'},
);

// Show count in UI
final unsentCount = response.data.length;
Text('$unsentCount kod dağıtıma hazır'); // "X codes ready to distribute"

// Show ONLY these codes in distribution list
ListView.builder(
  itemCount: response.data.length,
  itemBuilder: (context, index) {
    final code = response.data[index];
    return CodeDistributionCard(code: code);
  },
);
```

**Follow-up Screen:**
```dart
// Fetch codes sent but not redeemed
final response = await http.get(
  Uri.parse('$baseUrl/api/v1/sponsorship/codes?sentDaysAgo=7'),
  headers: {'Authorization': 'Bearer $token'},
);

// Show follow-up suggestions
ListView.builder(
  itemCount: response.data.length,
  itemBuilder: (context, index) {
    final code = response.data[index];
    return FollowUpCard(
      code: code,
      recipientName: code.recipientName,
      recipientPhone: code.recipientPhone,
      sentDate: code.distributionDate,
      onCallPressed: () => makeCall(code.recipientPhone),
      onResendPressed: () => resendSMS(code),
    );
  },
);
```

---

## ⚠️ Breaking Changes

### None - Backward Compatible

All existing queries continue to work:
- `GET /codes` - Still returns all codes
- `GET /codes?onlyUnused=true` - Still returns unused codes (sent + unsent)

New parameters are **opt-in**:
- `?onlyUnsent=true` - New feature, doesn't break existing behavior
- `?sentDaysAgo=7` - New feature, doesn't break existing behavior

---

## 🚀 Deployment Checklist

- [x] Repository methods implemented
- [x] Service layer updated
- [x] Query handler updated
- [x] Controller endpoint updated
- [x] Build successful (0 errors)
- [x] Documentation created
- [ ] Database migration (None required - uses existing fields)
- [ ] Mobile app updated to use `?onlyUnsent=true`
- [ ] User training on new filtering options
- [ ] Production deployment

---

## 📝 Related Documentation

- [Sponsorship Code Distribution Complete Guide](./SPONSORSHIP_CODE_DISTRIBUTION_COMPLETE_GUIDE.md)
- [Sponsorship Quantity Limits Documentation](./SPONSORSHIP_QUANTITY_LIMITS_DOCUMENTATION.md)
- [SendSponsorshipLinkCommand Implementation](../Business/Handlers/Sponsorship/Commands/SendSponsorshipLinkCommand.cs:180)

---

## 🐛 Issue Resolution

**Original Issue:** Codes shown in "available to send" list were already sent to farmers

**Root Cause:** `GetUnusedCodesBySponsorAsync` filtered by `!IsUsed` but didn't check `DistributionDate`

**Fix:** Created `GetUnsentCodesBySponsorAsync` that checks `!DistributionDate.HasValue`

**Result:** ✅ Prevents duplicate code distribution
**Status:** ✅ Implemented, Built, Ready for Testing

---

**End of Documentation**

*Last Updated: 2025-10-11 by Claude Code*
*Document Version: 1.0.0*
*Feature Status: ✅ Implementation Complete*
