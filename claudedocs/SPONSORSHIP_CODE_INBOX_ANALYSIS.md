# Sponsorship Code Inbox - Detailed Analysis & Implementation Plan

## 📋 Executive Summary

**Objective**: Create a "Sponsorship Code Inbox" feature where farmers can see all sponsorship codes sent to them (via SMS) BEFORE redeeming, similar to the Dealer Invitation inbox pattern.

**Current Problem**:
- Sponsorship codes are sent via SMS (`SendSponsorshipLinkCommand`)
- Farmers MUST manually enter the code or click the deep link to redeem
- No way to see "all codes sent to me" in one place
- If farmer loses SMS or forgets code, they cannot recover it easily

**Proposed Solution**:
- Create a database entity to track "who received which code"
- Add API endpoint for farmers to query "my received codes"
- Frontend displays a list of codes with metadata (sender, date, status, tier)
- User can initiate redemption directly from the inbox list

---

## 🔍 Analysis: Dealer Invitation Pattern (Reference)

### Dealer Invitation Flow
```
1. Sponsor creates DealerInvitation record
   ├─ Email, Phone, DealerName
   ├─ InvitationToken (unique)
   ├─ Status: Pending → Accepted → Complete
   └─ Codes RESERVED via ReservedForInvitationId

2. System sends SMS with invitation link/token

3. Dealer views "My Invitations" endpoint
   ├─ GET /api/sponsorship/dealer-invitations
   └─ Returns list of invitations with status

4. Dealer accepts invitation
   ├─ POST /api/sponsorship/accept-dealer-invitation
   ├─ Creates sponsor account
   ├─ Transfers codes to dealer
   └─ Updates DealerInvitation.Status = "Accepted"
```

### Key Database Entity: `DealerInvitation`
```csharp
public class DealerInvitation : IEntity
{
    public int Id { get; set; }
    public int SponsorId { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string DealerName { get; set; }
    public string Status { get; set; } // Pending, Accepted, Expired, Cancelled
    public string InvitationToken { get; set; }
    public int CodeCount { get; set; }
    public int? CreatedDealerId { get; set; }
    public DateTime? AcceptedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
}
```

### Key Endpoints
```
GET  /api/sponsorship/dealer-invitations?phone={phone}
     → Returns list of invitations for this phone number

POST /api/sponsorship/accept-dealer-invitation
     → Creates dealer account and transfers codes
```

---

## 🎯 Proposed Solution: Sponsorship Code Inbox

### Pattern Similarities

| Aspect | Dealer Invitation | Sponsorship Code Inbox |
|--------|-------------------|------------------------|
| **Recipient Identification** | Phone/Email | Phone (RecipientPhone in SponsorshipCode) |
| **Status Tracking** | DealerInvitation.Status | CodeDistribution.Status (NEW entity) |
| **Pre-Acceptance View** | GET dealer-invitations | GET farmer-sponsorship-inbox |
| **Acceptance Action** | accept-dealer-invitation | redeem-sponsorship-code (existing) |
| **Database Record** | DealerInvitation entity | CodeDistribution entity (NEW) |

---

## 📊 Current Data Model Analysis

### `SponsorshipCode` Entity (Existing)
```csharp
public class SponsorshipCode : IEntity
{
    // Usage Information
    public bool IsUsed { get; set; }
    public int? UsedByUserId { get; set; }
    public DateTime? UsedDate { get; set; }

    // Link Distribution Fields (✅ ALREADY EXIST!)
    public string RecipientPhone { get; set; }      // ✅ Set by SendSponsorshipLinkCommand
    public string RecipientName { get; set; }       // ✅ Set by SendSponsorshipLinkCommand
    public DateTime? LinkSentDate { get; set; }     // ✅ Set by SendSponsorshipLinkCommand
    public string LinkSentVia { get; set; }         // ✅ SMS, WhatsApp
    public bool LinkDelivered { get; set; }         // ✅ Delivery confirmation
    public string RedemptionLink { get; set; }      // ✅ Generated deep link

    // Tracking
    public DateTime? LinkClickDate { get; set; }
    public int LinkClickCount { get; set; }
}
```

**✅ CRITICAL INSIGHT**: `SponsorshipCode` entity ALREADY tracks recipient information!
- `RecipientPhone` is set in `SendSponsorshipLinkCommand` (line 175)
- `RecipientName` is set in `SendSponsorshipLinkCommand` (line 176)
- `LinkSentDate`, `LinkSentVia`, `LinkDelivered` all tracked

**💡 Conclusion**: We DON'T need a new entity! We can query `SponsorshipCode` directly!

---

## 🏗️ Implementation Design

### Option 1: Direct Query (Recommended) ⭐

**Pros**:
- No new entity needed
- No data migration required
- All data already exists in `SponsorshipCode` table
- Simple implementation

**Cons**:
- `RecipientPhone` must be unique identifier (no user link yet)
- Cannot filter by UserId (only by phone)

**Implementation**:
```csharp
// Query: Get all codes sent to a phone number
var codes = await _sponsorshipCodeRepository.GetListAsync(c =>
    c.RecipientPhone == normalizedPhone &&
    c.LinkDelivered == true &&
    c.ExpiryDate > DateTime.Now);

// Return DTO with:
// - Code, SponsorName, TierName, SentDate, IsUsed, ExpiryDate
```

---

### Option 2: New Entity `CodeDistribution` (Future-Proof)

**Pros**:
- Separates "distribution" from "code ownership"
- Can track multiple distributions of same code
- Better for analytics and reporting
- Supports non-SMS distribution channels

**Cons**:
- Requires new entity, repository, migration
- Data duplication (info already in SponsorshipCode)
- More complex implementation

**Implementation**:
```csharp
public class CodeDistribution : IEntity
{
    public int Id { get; set; }
    public int SponsorshipCodeId { get; set; }      // FK to SponsorshipCode
    public int SponsorId { get; set; }              // Who sent it
    public string RecipientPhone { get; set; }      // Who received it
    public string RecipientName { get; set; }
    public string RecipientEmail { get; set; }      // Optional
    public DateTime SentDate { get; set; }
    public string SentVia { get; set; }             // SMS, WhatsApp, Email
    public bool Delivered { get; set; }
    public string Status { get; set; }              // Sent, Viewed, Redeemed
    public DateTime? ViewedDate { get; set; }       // When farmer viewed in inbox
    public DateTime? RedeemedDate { get; set; }     // When farmer redeemed
    public int? RedeemedByUserId { get; set; }      // FK to User
}
```

---

## 🚀 Recommended Implementation: Option 1 (Direct Query)

### Phase 1: Backend API

#### 1.1 Create Query Handler

**File**: `Business/Handlers/Sponsorship/Queries/GetFarmerSponsorshipInboxQuery.cs`

```csharp
public class GetFarmerSponsorshipInboxQuery : IRequest<IDataResult<List<FarmerSponsorshipInboxDto>>>
{
    public string Phone { get; set; }  // Required: Farmer's phone number
    public bool IncludeUsed { get; set; } = false;  // Optional: Show redeemed codes
}

public class GetFarmerSponsorshipInboxQueryHandler : IRequestHandler<...>
{
    private readonly ISponsorshipCodeRepository _codeRepository;
    private readonly ISponsorProfileRepository _sponsorProfileRepository;
    private readonly ISubscriptionTierRepository _tierRepository;

    public async Task<IDataResult<List<FarmerSponsorshipInboxDto>>> Handle(...)
    {
        // Normalize phone number (same as SendSponsorshipLinkCommand.FormatPhoneNumber)
        var normalizedPhone = FormatPhoneNumber(request.Phone);

        // Query codes sent to this phone
        var codesQuery = _codeRepository.Table
            .Where(c => c.RecipientPhone == normalizedPhone &&
                        c.LinkDelivered == true);

        // Filter by usage status
        if (!request.IncludeUsed)
        {
            codesQuery = codesQuery.Where(c => !c.IsUsed);
        }

        // Only non-expired codes (or allow showing expired?)
        codesQuery = codesQuery.Where(c => c.ExpiryDate > DateTime.Now);

        var codes = await codesQuery
            .OrderByDescending(c => c.LinkSentDate)
            .ToListAsync();

        // Get sponsor names
        var sponsorIds = codes.Select(c => c.SponsorId).Distinct().ToList();
        var sponsors = await _sponsorProfileRepository.GetListAsync(s =>
            sponsorIds.Contains(s.SponsorId));

        // Get tier names
        var tierIds = codes.Select(c => c.SubscriptionTierId).Distinct().ToList();
        var tiers = await _tierRepository.GetListAsync(t =>
            tierIds.Contains(t.Id));

        // Map to DTOs
        var result = codes.Select(code => new FarmerSponsorshipInboxDto
        {
            Code = code.Code,
            SponsorName = sponsors.FirstOrDefault(s => s.SponsorId == code.SponsorId)?.CompanyName ?? "Unknown Sponsor",
            TierName = tiers.FirstOrDefault(t => t.Id == code.SubscriptionTierId)?.TierName ?? "Unknown",
            SentDate = code.LinkSentDate ?? code.CreatedDate,
            SentVia = code.LinkSentVia,
            IsUsed = code.IsUsed,
            UsedDate = code.UsedDate,
            ExpiryDate = code.ExpiryDate,
            RedemptionLink = code.RedemptionLink,
            RecipientName = code.RecipientName
        }).ToList();

        return new SuccessDataResult<List<FarmerSponsorshipInboxDto>>(result,
            $"{result.Count} sponsorship code found");
    }

    private string FormatPhoneNumber(string phone)
    {
        // Same logic as SendSponsorshipLinkCommand
        var cleaned = new string(phone.Where(char.IsDigit).ToArray());
        if (!cleaned.StartsWith("90") && cleaned.Length == 10)
            cleaned = "90" + cleaned;
        if (!cleaned.StartsWith("+"))
            cleaned = "+" + cleaned;
        return cleaned;
    }
}
```

#### 1.2 Create DTO

**File**: `Entities/Dtos/FarmerSponsorshipInboxDto.cs`

```csharp
public class FarmerSponsorshipInboxDto
{
    public string Code { get; set; }                  // AGRI-2025-XXXXX
    public string SponsorName { get; set; }           // Company name
    public string TierName { get; set; }              // S, M, L, XL
    public DateTime SentDate { get; set; }            // When sent
    public string SentVia { get; set; }               // SMS, WhatsApp
    public bool IsUsed { get; set; }                  // Redeemed?
    public DateTime? UsedDate { get; set; }           // When redeemed
    public DateTime ExpiryDate { get; set; }          // Expiry date
    public string RedemptionLink { get; set; }        // Deep link
    public string RecipientName { get; set; }         // Farmer name

    // Computed properties for frontend
    public bool IsExpired => ExpiryDate < DateTime.Now;
    public int DaysUntilExpiry => (ExpiryDate - DateTime.Now).Days;
    public string Status => IsUsed ? "Kullanıldı" : IsExpired ? "Süresi Doldu" : "Aktif";
}
```

#### 1.3 Create Controller Endpoint

**File**: `WebAPI/Controllers/SponsorshipController.cs`

```csharp
/// <summary>
/// Get all sponsorship codes sent to a phone number (Farmer Inbox)
/// </summary>
/// <param name="phone">Farmer's phone number</param>
/// <param name="includeUsed">Include already redeemed codes</param>
/// <returns>List of sponsorship codes with sponsor info</returns>
[HttpGet("farmer-inbox")]
public async Task<IActionResult> GetFarmerInbox(
    [FromQuery] string phone,
    [FromQuery] bool includeUsed = false)
{
    if (string.IsNullOrWhiteSpace(phone))
        return BadRequest("Phone number is required");

    var result = await Mediator.Send(new GetFarmerSponsorshipInboxQuery
    {
        Phone = phone,
        IncludeUsed = includeUsed
    });

    return result.Success
        ? Ok(result)
        : BadRequest(result);
}
```

---

### Phase 2: Frontend Implementation

#### 2.1 Farmer Inbox Screen (Flutter)

**Screen**: `SponsorshipInboxScreen`

```dart
class SponsorshipInboxScreen extends StatefulWidget {
  @override
  _SponsorshipInboxScreenState createState() => _SponsorshipInboxScreenState();
}

class _SponsorshipInboxScreenState extends State<SponsorshipInboxScreen> {
  List<FarmerSponsorshipInboxDto> _codes = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _loadInbox();
  }

  Future<void> _loadInbox() async {
    final phone = await AuthService.getCurrentUserPhone();
    final result = await SponsorshipApi.getFarmerInbox(phone);

    setState(() {
      _codes = result.data;
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return LoadingSpinner();

    if (_codes.isEmpty) {
      return EmptyState(
        icon: Icons.inbox,
        message: "Henüz sponsorluk kodu gönderilmedi"
      );
    }

    return ListView.builder(
      itemCount: _codes.length,
      itemBuilder: (context, index) {
        final code = _codes[index];
        return SponsorshipCodeCard(
          code: code,
          onRedeem: () => _redeemCode(code),
        );
      },
    );
  }

  Future<void> _redeemCode(FarmerSponsorshipInboxDto code) async {
    // Navigate to redemption flow
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => RedeemCodeScreen(code: code.code),
      ),
    );

    // Refresh inbox after redemption
    await _loadInbox();
  }
}
```

#### 2.2 Code Card Widget

```dart
class SponsorshipCodeCard extends StatelessWidget {
  final FarmerSponsorshipInboxDto code;
  final VoidCallback onRedeem;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: EdgeInsets.all(8),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Sponsor name & tier
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  code.sponsorName,
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                ),
                Chip(
                  label: Text(code.tierName),
                  backgroundColor: _getTierColor(code.tierName),
                ),
              ],
            ),

            SizedBox(height: 8),

            // Code (masked or full?)
            Text(
              code.code,
              style: TextStyle(
                fontSize: 16,
                fontFamily: 'monospace',
                color: Colors.blue,
              ),
            ),

            SizedBox(height: 8),

            // Sent date & expiry
            Row(
              children: [
                Icon(Icons.calendar_today, size: 16, color: Colors.grey),
                SizedBox(width: 4),
                Text(
                  "Gönderim: ${_formatDate(code.sentDate)}",
                  style: TextStyle(color: Colors.grey),
                ),
                Spacer(),
                Text(
                  "${code.daysUntilExpiry} gün kaldı",
                  style: TextStyle(
                    color: code.daysUntilExpiry < 7 ? Colors.red : Colors.green,
                  ),
                ),
              ],
            ),

            SizedBox(height: 12),

            // Action button
            if (!code.isUsed && !code.isExpired)
              ElevatedButton(
                onPressed: onRedeem,
                child: Text("Kullan"),
                style: ElevatedButton.styleFrom(
                  minimumSize: Size(double.infinity, 44),
                ),
              )
            else if (code.isUsed)
              Text(
                "✓ Kullanıldı (${_formatDate(code.usedDate)})",
                style: TextStyle(color: Colors.green),
              )
            else if (code.isExpired)
              Text(
                "Süresi doldu",
                style: TextStyle(color: Colors.red),
              ),
          ],
        ),
      ),
    );
  }

  Color _getTierColor(String tier) {
    switch (tier) {
      case 'S': return Colors.blue.shade100;
      case 'M': return Colors.green.shade100;
      case 'L': return Colors.orange.shade100;
      case 'XL': return Colors.purple.shade100;
      default: return Colors.grey.shade100;
    }
  }

  String _formatDate(DateTime? date) {
    if (date == null) return '-';
    return DateFormat('dd MMM yyyy').format(date);
  }
}
```

---

## 🔄 User Flow Comparison

### Current Flow (Without Inbox)
```
1. Sponsor sends code via SMS
   ↓
2. Farmer receives SMS
   ↓
3. Farmer manually copies code OR clicks deep link
   ↓
4. Farmer enters code in app OR app auto-fills from deep link
   ↓
5. Farmer redeems code
```

**Problem**: If farmer loses SMS, code is lost forever!

### New Flow (With Inbox)
```
1. Sponsor sends code via SMS
   ↓
2. Farmer receives SMS
   ↓
3. [NEW] Farmer opens app → "Sponsorship Inbox" tab
   ↓
4. [NEW] Sees list of all codes sent to their phone
   ↓
5. [NEW] Taps "Kullan" button on code card
   ↓
6. App navigates to redemption screen with pre-filled code
   ↓
7. Farmer confirms and redeems
```

**Benefits**:
- ✅ No lost codes
- ✅ See all codes in one place
- ✅ Check expiry dates
- ✅ Know who sent each code
- ✅ See already-used codes (history)

---

## 📊 Database Query Performance

### Query Pattern
```sql
-- Get codes sent to a phone number
SELECT
    sc.Code,
    sc.SponsorId,
    sc.SubscriptionTierId,
    sc.RecipientPhone,
    sc.RecipientName,
    sc.LinkSentDate,
    sc.LinkSentVia,
    sc.IsUsed,
    sc.UsedDate,
    sc.ExpiryDate,
    sc.RedemptionLink,
    sp.CompanyName AS SponsorName,
    st.TierName
FROM SponsorshipCodes sc
LEFT JOIN SponsorProfiles sp ON sc.SponsorId = sp.SponsorId
LEFT JOIN SubscriptionTiers st ON sc.SubscriptionTierId = st.Id
WHERE sc.RecipientPhone = '+905551234567'
  AND sc.LinkDelivered = TRUE
  AND sc.ExpiryDate > NOW()
ORDER BY sc.LinkSentDate DESC;
```

### Recommended Index
```sql
-- Add index for fast lookups by RecipientPhone
CREATE INDEX IX_SponsorshipCodes_RecipientPhone_LinkDelivered_ExpiryDate
ON SponsorshipCodes (RecipientPhone, LinkDelivered, ExpiryDate)
WHERE RecipientPhone IS NOT NULL;
```

**Performance**:
- Expected rows: 5-20 per farmer (low)
- Query time: <10ms with index
- No pagination needed (small dataset)

---

## 🔐 Security Considerations

### Authentication
```csharp
// Option A: No auth required (public endpoint)
// - Query by phone number only
// - Anyone with phone number can see codes sent to that number
// - Risk: Phone number enumeration attack

// Option B: Require authentication
[Authorize] // User must be logged in
[HttpGet("farmer-inbox")]
public async Task<IActionResult> GetFarmerInbox()
{
    // Get phone from authenticated user's claims
    var userId = GetUserIdFromClaims();
    var user = await _userRepository.GetAsync(u => u.Id == userId);
    var phone = user.MobilePhone;

    var result = await Mediator.Send(new GetFarmerSponsorshipInboxQuery
    {
        Phone = phone
    });

    return Ok(result);
}
```

**Recommendation**: **Option B (Require Authentication)**
- Prevents phone number enumeration
- Ensures only the actual farmer sees their codes
- Aligns with app security model

### Code Visibility
- ❌ Do NOT expose full code in list view (security risk)
- ✅ Show masked code: `AGRI-2025-****`
- ✅ Reveal full code only when "Kullan" button clicked
- ✅ Or: Show full code but require biometric/PIN to view

---

## 🧪 Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task GetFarmerInbox_ReturnsOnlyCodesForGivenPhone()
{
    // Arrange
    var phone = "+905551234567";
    var otherPhone = "+905559876543";

    await _codeRepository.AddAsync(new SponsorshipCode
    {
        Code = "AGRI-2025-TEST1",
        RecipientPhone = phone,
        LinkDelivered = true
    });

    await _codeRepository.AddAsync(new SponsorshipCode
    {
        Code = "AGRI-2025-TEST2",
        RecipientPhone = otherPhone,
        LinkDelivered = true
    });

    // Act
    var result = await _handler.Handle(new GetFarmerSponsorshipInboxQuery
    {
        Phone = phone
    });

    // Assert
    Assert.Single(result.Data);
    Assert.Equal("AGRI-2025-TEST1", result.Data[0].Code);
}

[Fact]
public async Task GetFarmerInbox_ExcludesUsedCodes_WhenIncludeUsedIsFalse()
{
    // Test filtering logic
}

[Fact]
public async Task GetFarmerInbox_ExcludesExpiredCodes()
{
    // Test expiry filtering
}

[Fact]
public async Task GetFarmerInbox_NormalizesPhoneNumber()
{
    // Test phone normalization
    // Input: "555 123 45 67" → "+905551234567"
}
```

### Integration Tests
```csharp
[Fact]
public async Task FarmerInbox_EndToEnd_Flow()
{
    // 1. Sponsor sends code via SendSponsorshipLinkCommand
    var sendResult = await SendSponsorshipLink(sponsorId, farmerPhone, code);
    Assert.True(sendResult.Success);

    // 2. Farmer queries inbox
    var inboxResult = await GetFarmerInbox(farmerPhone);
    Assert.Single(inboxResult.Data);
    Assert.Equal(code, inboxResult.Data[0].Code);

    // 3. Farmer redeems code
    var redeemResult = await RedeemCode(code, farmerId);
    Assert.True(redeemResult.Success);

    // 4. Code disappears from inbox (if IncludeUsed=false)
    var inboxAfterRedeem = await GetFarmerInbox(farmerPhone);
    Assert.Empty(inboxAfterRedeem.Data);
}
```

### Manual Testing Checklist
- [ ] Sponsor sends code via SMS
- [ ] Code appears in farmer's inbox within 5 seconds
- [ ] Correct sponsor name and tier displayed
- [ ] Expiry date countdown accurate
- [ ] "Kullan" button navigates to redemption
- [ ] After redemption, code marked as "Kullanıldı"
- [ ] Expired codes show "Süresi doldu"
- [ ] Multiple codes from different sponsors displayed correctly
- [ ] Phone number normalization works (with/without +90, spaces, dashes)

---

## 📈 Analytics & Monitoring

### New Metrics to Track
```csharp
// 1. Inbox View Rate
// How many farmers actually check their inbox?
await _analyticsService.TrackEventAsync("SponsorshipInboxViewed", new {
    UserId = farmerId,
    CodesCount = inboxCodes.Count
});

// 2. Inbox-to-Redemption Conversion
// How many codes are redeemed via inbox vs direct SMS link?
await _analyticsService.TrackEventAsync("CodeRedeemedViaInbox", new {
    Code = code,
    SponsorId = sponsorId,
    Source = "Inbox" // vs "SMS_Link" or "Manual_Entry"
});

// 3. Code Discovery Time
// How long between SMS sent and inbox view?
var discoveryTime = (inboxViewDate - linkSentDate).TotalHours;
await _analyticsService.TrackMetricAsync("InboxDiscoveryTimeHours", discoveryTime);
```

### Dashboard Additions (Sponsor View)
```
Sponsor Dashboard > "Code Distribution" Tab
├─ Sent: 100 codes
├─ Viewed in Inbox: 75 (75%)  [NEW METRIC]
├─ Redeemed: 60 (60%)
└─ Redeemed via Inbox: 45 (75% of redemptions)  [NEW METRIC]
```

---

## 🚧 Migration & Rollout Strategy

### Phase 1: Backend Only (Week 1)
- ✅ Implement API endpoint
- ✅ Add unit tests
- ✅ Add integration tests
- ✅ Deploy to staging
- ✅ Test with Postman
- ✅ Monitor query performance

### Phase 2: Mobile App (Week 2)
- ✅ Add "Sponsorluk Kutusu" tab in farmer app
- ✅ Implement inbox screen
- ✅ Add redemption flow integration
- ✅ Beta test with 10 farmers
- ✅ Collect feedback

### Phase 3: Production Rollout (Week 3)
- ✅ Deploy to production
- ✅ Add in-app notification: "Yeni özellik: Sponsorluk Kutunuz!"
- ✅ Monitor usage analytics
- ✅ Fix bugs based on feedback

### Phase 4: Enhancements (Week 4+)
- Push notifications when new code arrives
- Code expiry reminders (3 days before expiry)
- Filters: by sponsor, by tier, by status
- Search functionality

---

## 🔮 Future Enhancements

### 1. Push Notifications
```dart
// When SendSponsorshipLinkCommand is executed
await _notificationService.SendPushAsync(
    recipientPhone: farmerPhone,
    title: "Yeni Sponsorluk Kodu!",
    body: "{sponsorName} size {tierName} paketi gönderdi",
    data: { "type": "sponsorship_code", "code": code }
);
```

### 2. Code Sharing
```dart
// Allow farmer to share code with another farmer
shareCode(String code) async {
  await Share.share(
    "ZiraAI sponsorluk kodu: $code\nRedeem at: ${deepLink}",
    subject: "ZiraAI Sponsorluk Kodu"
  );
}
```

### 3. Code Transfer
```csharp
// Transfer unused code to another phone number
[HttpPost("transfer-code")]
public async Task<IActionResult> TransferCode(
    string code,
    string fromPhone,
    string toPhone)
{
    // Change RecipientPhone in SponsorshipCode
    // Send new SMS to recipient
}
```

### 4. Bulk Actions
```dart
// Redeem multiple codes at once
redeemMultipleCodes(List<String> codes) async {
  for (var code in codes) {
    await redeemCode(code);
  }
}
```

### 5. Favorite Sponsors
```dart
// Mark favorite sponsors for easy filtering
markFavorite(int sponsorId) async {
  await _userPrefsRepo.saveFavoriteSponsor(sponsorId);
}
```

---

## 📝 Implementation Checklist

### Backend Tasks
- [ ] Create `GetFarmerSponsorshipInboxQuery.cs`
- [ ] Create `GetFarmerSponsorshipInboxQueryHandler.cs`
- [ ] Create `FarmerSponsorshipInboxDto.cs`
- [ ] Add endpoint to `SponsorshipController.cs`
- [ ] Write unit tests for query handler
- [ ] Write integration tests for endpoint
- [ ] Add database index on `RecipientPhone`
- [ ] Update Swagger documentation
- [ ] Test with Postman

### Frontend Tasks (Flutter)
- [ ] Create `SponsorshipInboxScreen.dart`
- [ ] Create `SponsorshipCodeCard.dart` widget
- [ ] Add API service method `SponsorshipApi.getFarmerInbox()`
- [ ] Add navigation tab "Sponsorluk Kutusu"
- [ ] Integrate with redemption flow
- [ ] Add loading states and error handling
- [ ] Add empty state UI
- [ ] Test on Android & iOS
- [ ] Add analytics tracking

### DevOps Tasks
- [ ] Deploy backend to staging
- [ ] Deploy mobile app to TestFlight/Internal Testing
- [ ] Monitor API performance
- [ ] Monitor user adoption metrics
- [ ] Set up alerts for errors

---

## 💡 Key Insights

### Why This Will Work
1. **Data Already Exists**: `RecipientPhone` already set by `SendSponsorshipLinkCommand`
2. **Simple Query**: Just filter by phone number
3. **No Migration Needed**: Use existing `SponsorshipCode` table
4. **User Benefit**: Farmers can recover lost SMS codes
5. **Sponsor Benefit**: Higher redemption rates

### Potential Issues
1. **Phone Number Changes**: If farmer changes phone, old codes lost
   - **Solution**: Add email tracking + account linking
2. **Multiple Users, Same Phone**: Family members sharing device
   - **Solution**: Require login to view inbox
3. **Privacy**: Anyone with phone number can see codes
   - **Solution**: Require authentication

### Success Metrics
- **Adoption**: % of farmers who use inbox feature
- **Redemption Rate**: Increase from current baseline
- **Time to Redeem**: Decrease in average time from SMS to redemption
- **Support Tickets**: Decrease in "I lost my code" tickets

---

## 🎯 Conclusion

**Recommendation**: Implement **Option 1 (Direct Query)** first for MVP.

**Reasons**:
- ✅ Simplest implementation (1-2 days)
- ✅ No schema changes required
- ✅ All data already exists
- ✅ Low risk, high value
- ✅ Can iterate to Option 2 later if needed

**Next Steps**:
1. Get approval from stakeholders
2. Create backend implementation (Phase 1)
3. Test with Postman
4. Create frontend implementation (Phase 2)
5. Beta test with 10-20 farmers
6. Production rollout

**Estimated Timeline**:
- Backend: 2 days
- Frontend: 3 days
- Testing: 2 days
- **Total**: 1 week to production

---

## 📚 References

- Dealer Invitation Pattern: `Business/Handlers/Sponsorship/Queries/GetDealerInvitationsQuery.cs`
- Send Sponsorship Link: `Business/Handlers/Sponsorship/Commands/SendSponsorshipLinkCommand.cs`
- Sponsorship Code Entity: `Entities/Concrete/SponsorshipCode.cs`
- Queue Flow Analysis: `claudedocs/SPONSORSHIP_QUEUE_FLOW_ANALYSIS.md`

---

**Document Version**: 1.0
**Last Updated**: 2025-01-24
**Author**: Claude Code Agent
**Status**: Analysis Complete - Ready for Implementation
