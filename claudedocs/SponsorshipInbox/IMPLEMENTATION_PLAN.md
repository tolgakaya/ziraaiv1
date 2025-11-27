# Sponsorship Code Inbox - Implementation Plan

## 📋 Document Purpose

Bu doküman, "Sponsorship Code Inbox" feature'ının step-by-step implementasyon planıdır. Context kaybı veya hata durumlarında bu dokümanı takip ederek devam edilir.

---

## 🎯 Feature Overview

**Problem**:
- Sponsor'lar SMS ile sponsorship code gönderiyor
- Farmer SMS'i kaybederse, kodu recover edemez
- Tüm gelen kodları bir arada göremez

**Çözüm**:
- Farmer'ların kendilerine gönderilen tüm sponsorship code'ları görebileceği bir "inbox" ekranı
- Dealer Invitation pattern'ine benzer yaklaşım
- Mevcut `SponsorshipCode` entity'sini kullanarak (yeni entity gerekmez!)

**Referans Pattern**: Dealer Invitation
- Entity: `DealerInvitation`
- Query: `GetDealerInvitationsQuery`
- Endpoint: `GET /api/sponsorship/dealer-invitations`

---

## 🏗️ Architecture Decision

### ✅ SELECTED: Option 1 - Direct Query

**Neden**:
- `SponsorshipCode` entity'sinde zaten gerekli tüm alanlar mevcut
- `RecipientPhone`, `RecipientName`, `LinkSentDate`, `LinkSentVia` → `SendSponsorshipLinkCommand` tarafından set ediliyor
- Yeni entity, migration, repository gerekmez
- Basit ve hızlı implementasyon (1 hafta)

**Data Model**:
```csharp
// Mevcut entity - DEĞİŞMEYECEK!
public class SponsorshipCode : IEntity
{
    // ... existing fields ...

    // ✅ Bu alanlar zaten mevcut ve dolu!
    public string RecipientPhone { get; set; }      // Set by SendSponsorshipLinkCommand
    public string RecipientName { get; set; }       // Set by SendSponsorshipLinkCommand
    public DateTime? LinkSentDate { get; set; }     // Set by SendSponsorshipLinkCommand
    public string LinkSentVia { get; set; }         // SMS, WhatsApp
    public bool LinkDelivered { get; set; }         // Delivery confirmation
    public string RedemptionLink { get; set; }      // Deep link

    // Usage tracking
    public bool IsUsed { get; set; }
    public DateTime? UsedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
}
```

---

## 📝 Implementation Steps

### ✅ PHASE 0: Prerequisites Check

**Files to Review**:
- [x] `Entities/Concrete/SponsorshipCode.cs` - Verify fields exist
- [x] `Business/Handlers/Sponsorship/Commands/SendSponsorshipLinkCommand.cs` - Verify data population
- [x] `Business/Handlers/Sponsorship/Queries/GetDealerInvitationsQuery.cs` - Reference pattern
- [x] `DataAccess/Abstract/ISponsorshipCodeRepository.cs` - Check available methods

**Verification Query** (Run in DB):
```sql
-- Check if data exists
SELECT
    Code,
    RecipientPhone,
    RecipientName,
    LinkSentDate,
    LinkSentVia,
    LinkDelivered,
    IsUsed,
    ExpiryDate
FROM SponsorshipCodes
WHERE RecipientPhone IS NOT NULL
  AND LinkDelivered = TRUE
ORDER BY LinkSentDate DESC
LIMIT 10;

-- Expected: See codes with recipient info populated
```

---

### 🔨 PHASE 1: Backend Implementation

#### Step 1.1: Create DTO

**File**: `Entities/Dtos/FarmerSponsorshipInboxDto.cs` (NEW)

**Location**: `c:\Users\Asus\Documents\Visual Studio 2022\ziraai\Entities\Dtos\`

```csharp
namespace Entities.Dtos
{
    /// <summary>
    /// DTO for farmer's sponsorship code inbox
    /// Shows codes sent to farmer's phone number
    /// </summary>
    public class FarmerSponsorshipInboxDto
    {
        /// <summary>
        /// Sponsorship code (e.g., AGRI-2025-X3K9)
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Sponsor company name
        /// </summary>
        public string SponsorName { get; set; }

        /// <summary>
        /// Subscription tier (S, M, L, XL)
        /// </summary>
        public string TierName { get; set; }

        /// <summary>
        /// When the SMS was sent
        /// </summary>
        public DateTime SentDate { get; set; }

        /// <summary>
        /// How it was sent (SMS, WhatsApp)
        /// </summary>
        public string SentVia { get; set; }

        /// <summary>
        /// Whether code has been redeemed
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// When code was redeemed (null if not used)
        /// </summary>
        public DateTime? UsedDate { get; set; }

        /// <summary>
        /// Code expiry date
        /// </summary>
        public DateTime ExpiryDate { get; set; }

        /// <summary>
        /// Deep link for redemption
        /// </summary>
        public string RedemptionLink { get; set; }

        /// <summary>
        /// Farmer's name (from SMS recipient)
        /// </summary>
        public string RecipientName { get; set; }

        /// <summary>
        /// Computed: Is code expired?
        /// </summary>
        public bool IsExpired => ExpiryDate < DateTime.Now;

        /// <summary>
        /// Computed: Days until expiry (negative if expired)
        /// </summary>
        public int DaysUntilExpiry => (ExpiryDate - DateTime.Now).Days;

        /// <summary>
        /// Computed: User-friendly status
        /// </summary>
        public string Status => IsUsed ? "Kullanıldı"
                              : IsExpired ? "Süresi Doldu"
                              : "Aktif";
    }
}
```

**Checklist**:
- [ ] Create file in correct location
- [ ] Add XML documentation comments
- [ ] Verify computed properties work correctly
- [ ] Build and check for compilation errors

---

#### Step 1.2: Create Query

**File**: `Business/Handlers/Sponsorship/Queries/GetFarmerSponsorshipInboxQuery.cs` (NEW)

**Location**: `c:\Users\Asus\Documents\Visual Studio 2022\ziraai\Business\Handlers\Sponsorship\Queries\`

```csharp
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Query to get all sponsorship codes sent to a farmer's phone number
    /// Similar to GetDealerInvitationsQuery but for farmers
    /// </summary>
    public class GetFarmerSponsorshipInboxQuery : IRequest<IDataResult<List<FarmerSponsorshipInboxDto>>>
    {
        /// <summary>
        /// Farmer's phone number (will be normalized)
        /// Format: +905551234567 or 05551234567 or 555 123 45 67
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Include already redeemed codes in results
        /// Default: false (show only active/pending codes)
        /// </summary>
        public bool IncludeUsed { get; set; } = false;

        /// <summary>
        /// Include expired codes in results
        /// Default: false (show only non-expired codes)
        /// </summary>
        public bool IncludeExpired { get; set; } = false;
    }
}
```

**Checklist**:
- [ ] Create file in correct location
- [ ] Add XML documentation
- [ ] Verify namespace matches pattern
- [ ] Build and check for errors

---

#### Step 1.3: Create Query Handler

**File**: `Business/Handlers/Sponsorship/Queries/GetFarmerSponsorshipInboxQueryHandler.cs` (NEW)

**Location**: `c:\Users\Asus\Documents\Visual Studio 2022\ziraai\Business\Handlers\Sponsorship\Queries\`

```csharp
using Business.Handlers.Sponsorship.Queries;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    public class GetFarmerSponsorshipInboxQueryHandler
        : IRequestHandler<GetFarmerSponsorshipInboxQuery, IDataResult<List<FarmerSponsorshipInboxDto>>>
    {
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly ISubscriptionTierRepository _tierRepository;
        private readonly ILogger<GetFarmerSponsorshipInboxQueryHandler> _logger;

        public GetFarmerSponsorshipInboxQueryHandler(
            ISponsorshipCodeRepository codeRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            ISubscriptionTierRepository tierRepository,
            ILogger<GetFarmerSponsorshipInboxQueryHandler> logger)
        {
            _codeRepository = codeRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _tierRepository = tierRepository;
            _logger = logger;
        }

        public async Task<IDataResult<List<FarmerSponsorshipInboxDto>>> Handle(
            GetFarmerSponsorshipInboxQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("📥 Fetching sponsorship inbox for phone: {Phone}", request.Phone);

                // Step 1: Normalize phone number (same logic as SendSponsorshipLinkCommand)
                var normalizedPhone = FormatPhoneNumber(request.Phone);
                _logger.LogInformation("📞 Normalized phone: {NormalizedPhone}", normalizedPhone);

                // Step 2: Query codes sent to this phone
                var codesQuery = _codeRepository.Table
                    .Where(c => c.RecipientPhone == normalizedPhone &&
                                c.LinkDelivered == true);  // Only delivered codes

                // Step 3: Apply filters
                if (!request.IncludeUsed)
                {
                    codesQuery = codesQuery.Where(c => !c.IsUsed);
                    _logger.LogInformation("🔍 Filter: Excluding used codes");
                }

                if (!request.IncludeExpired)
                {
                    codesQuery = codesQuery.Where(c => c.ExpiryDate > DateTime.Now);
                    _logger.LogInformation("🔍 Filter: Excluding expired codes");
                }

                // Step 4: Execute query
                var codes = await codesQuery
                    .OrderByDescending(c => c.LinkSentDate)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("📋 Found {Count} codes for phone {Phone}",
                    codes.Count, normalizedPhone);

                if (codes.Count == 0)
                {
                    return new SuccessDataResult<List<FarmerSponsorshipInboxDto>>(
                        new List<FarmerSponsorshipInboxDto>(),
                        "Henüz sponsorluk kodu gönderilmemiş");
                }

                // Step 5: Get sponsor names (batch query)
                var sponsorIds = codes.Select(c => c.SponsorId).Distinct().ToList();
                var sponsors = await _sponsorProfileRepository.GetListAsync(s =>
                    sponsorIds.Contains(s.SponsorId));

                _logger.LogInformation("👥 Loaded {Count} sponsor profiles", sponsors.Count);

                // Step 6: Get tier names (batch query)
                var tierIds = codes.Select(c => c.SubscriptionTierId).Distinct().ToList();
                var tiers = await _tierRepository.GetListAsync(t =>
                    tierIds.Contains(t.Id));

                _logger.LogInformation("🎯 Loaded {Count} subscription tiers", tiers.Count);

                // Step 7: Map to DTOs
                var result = codes.Select(code => new FarmerSponsorshipInboxDto
                {
                    Code = code.Code,
                    SponsorName = sponsors
                        .FirstOrDefault(s => s.SponsorId == code.SponsorId)
                        ?.CompanyName ?? "Unknown Sponsor",
                    TierName = tiers
                        .FirstOrDefault(t => t.Id == code.SubscriptionTierId)
                        ?.TierName ?? "Unknown",
                    SentDate = code.LinkSentDate ?? code.CreatedDate,
                    SentVia = code.LinkSentVia ?? "SMS",
                    IsUsed = code.IsUsed,
                    UsedDate = code.UsedDate,
                    ExpiryDate = code.ExpiryDate,
                    RedemptionLink = code.RedemptionLink,
                    RecipientName = code.RecipientName
                }).ToList();

                _logger.LogInformation("✅ Successfully mapped {Count} codes to DTOs", result.Count);

                return new SuccessDataResult<List<FarmerSponsorshipInboxDto>>(
                    result,
                    $"{result.Count} sponsorluk kodu bulundu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching sponsorship inbox for phone: {Phone}",
                    request.Phone);
                return new ErrorDataResult<List<FarmerSponsorshipInboxDto>>(
                    "Sponsorluk kutusu yüklenirken hata oluştu");
            }
        }

        /// <summary>
        /// Format phone number to normalized format
        /// Same logic as SendSponsorshipLinkCommand.FormatPhoneNumber()
        /// </summary>
        private string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            // Remove all non-numeric characters
            var cleaned = new string(phone.Where(char.IsDigit).ToArray());

            // Add Turkey country code if not present
            if (!cleaned.StartsWith("90") && cleaned.Length == 10)
            {
                cleaned = "90" + cleaned;
            }

            // Add + prefix
            if (!cleaned.StartsWith("+"))
            {
                cleaned = "+" + cleaned;
            }

            return cleaned;
        }
    }
}
```

**Checklist**:
- [ ] Create file in correct location
- [ ] Verify all repository dependencies exist
- [ ] Add comprehensive logging
- [ ] Implement phone normalization (match SendSponsorshipLinkCommand)
- [ ] Handle empty results gracefully
- [ ] Build and check for errors

**Testing SQL** (for verification):
```sql
-- Test query manually
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
    sp.CompanyName AS SponsorName,
    st.TierName
FROM SponsorshipCodes sc
LEFT JOIN SponsorProfiles sp ON sc.SponsorId = sp.SponsorId
LEFT JOIN SubscriptionTiers st ON sc.SubscriptionTierId = st.Id
WHERE sc.RecipientPhone = '+905551234567'  -- Replace with test phone
  AND sc.LinkDelivered = TRUE
ORDER BY sc.LinkSentDate DESC;
```

---

#### Step 1.4: Create Controller Endpoint

**File**: `WebAPI/Controllers/SponsorshipController.cs` (MODIFY)

**Location**: Add to existing controller

```csharp
/// <summary>
/// Get all sponsorship codes sent to a farmer's phone number (Farmer Inbox)
/// No authentication required - uses phone number as identifier
/// </summary>
/// <param name="phone">Farmer's phone number (will be normalized)</param>
/// <param name="includeUsed">Include already redeemed codes (default: false)</param>
/// <param name="includeExpired">Include expired codes (default: false)</param>
/// <returns>List of sponsorship codes with sponsor info</returns>
[HttpGet("farmer-inbox")]
[ProducesResponseType(typeof(SuccessDataResult<List<FarmerSponsorshipInboxDto>>), 200)]
[ProducesResponseType(typeof(ErrorDataResult<List<FarmerSponsorshipInboxDto>>), 400)]
public async Task<IActionResult> GetFarmerSponsorshipInbox(
    [FromQuery] string phone,
    [FromQuery] bool includeUsed = false,
    [FromQuery] bool includeExpired = false)
{
    if (string.IsNullOrWhiteSpace(phone))
    {
        return BadRequest(new ErrorDataResult<List<FarmerSponsorshipInboxDto>>(
            "Telefon numarası gereklidir"));
    }

    var result = await Mediator.Send(new GetFarmerSponsorshipInboxQuery
    {
        Phone = phone,
        IncludeUsed = includeUsed,
        IncludeExpired = includeExpired
    });

    if (result.Success)
    {
        return Ok(result);
    }

    return BadRequest(result);
}
```

**Checklist**:
- [ ] Add to existing `SponsorshipController.cs`
- [ ] Add XML documentation comments
- [ ] Add Swagger annotations
- [ ] Verify endpoint routing
- [ ] Build and check for errors

**Endpoint Details**:
```
GET /api/sponsorship/farmer-inbox?phone={phone}&includeUsed={bool}&includeExpired={bool}

Example:
GET /api/sponsorship/farmer-inbox?phone=05551234567
GET /api/sponsorship/farmer-inbox?phone=+905551234567&includeUsed=true
```

---

#### Step 1.5: Database Index (Optional but Recommended)

**File**: Create migration (if using EF migrations)

**SQL Script**: `claudedocs/SponsorshipInbox/add_recipientphone_index.sql`

```sql
-- Add index for fast lookups by RecipientPhone
-- This significantly improves query performance

CREATE INDEX IX_SponsorshipCodes_RecipientPhone_LinkDelivered_ExpiryDate
ON SponsorshipCodes (RecipientPhone, LinkDelivered, ExpiryDate)
WHERE RecipientPhone IS NOT NULL;

-- Analyze query performance
EXPLAIN ANALYZE
SELECT *
FROM SponsorshipCodes
WHERE RecipientPhone = '+905551234567'
  AND LinkDelivered = TRUE
  AND ExpiryDate > NOW();

-- Expected: Index Scan instead of Sequential Scan
```

**Migration Command** (if using EF):
```bash
dotnet ef migrations add AddSponsorshipCodeRecipientPhoneIndex --project DataAccess --startup-project WebAPI --context ProjectDbContext --output-dir Migrations/Pg
```

**Checklist**:
- [ ] Create SQL script
- [ ] Test on staging database
- [ ] Measure query performance (before/after)
- [ ] Apply to production (after testing)

---

### 🧪 PHASE 2: Testing

#### Step 2.1: Unit Tests

**File**: `Tests/Business/Handlers/Sponsorship/Queries/GetFarmerSponsorshipInboxQueryTests.cs` (NEW)

```csharp
using Business.Handlers.Sponsorship.Queries;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Business.Handlers.Sponsorship.Queries
{
    public class GetFarmerSponsorshipInboxQueryTests
    {
        private readonly Mock<ISponsorshipCodeRepository> _codeRepositoryMock;
        private readonly Mock<ISponsorProfileRepository> _sponsorProfileRepositoryMock;
        private readonly Mock<ISubscriptionTierRepository> _tierRepositoryMock;
        private readonly Mock<ILogger<GetFarmerSponsorshipInboxQueryHandler>> _loggerMock;
        private readonly GetFarmerSponsorshipInboxQueryHandler _handler;

        public GetFarmerSponsorshipInboxQueryTests()
        {
            _codeRepositoryMock = new Mock<ISponsorshipCodeRepository>();
            _sponsorProfileRepositoryMock = new Mock<ISponsorProfileRepository>();
            _tierRepositoryMock = new Mock<ISubscriptionTierRepository>();
            _loggerMock = new Mock<ILogger<GetFarmerSponsorshipInboxQueryHandler>>();

            _handler = new GetFarmerSponsorshipInboxQueryHandler(
                _codeRepositoryMock.Object,
                _sponsorProfileRepositoryMock.Object,
                _tierRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetFarmerInbox_ReturnsOnlyCodesForGivenPhone()
        {
            // Arrange
            var phone = "+905551234567";
            var otherPhone = "+905559876543";

            var codes = new List<SponsorshipCode>
            {
                new SponsorshipCode
                {
                    Id = 1,
                    Code = "AGRI-2025-TEST1",
                    RecipientPhone = phone,
                    LinkDelivered = true,
                    IsUsed = false,
                    ExpiryDate = DateTime.Now.AddDays(30),
                    SponsorId = 1,
                    SubscriptionTierId = 1
                },
                new SponsorshipCode
                {
                    Id = 2,
                    Code = "AGRI-2025-TEST2",
                    RecipientPhone = otherPhone,
                    LinkDelivered = true,
                    IsUsed = false,
                    ExpiryDate = DateTime.Now.AddDays(30),
                    SponsorId = 1,
                    SubscriptionTierId = 1
                }
            };

            _codeRepositoryMock.Setup(r => r.GetListAsync(It.IsAny<Func<SponsorshipCode, bool>>()))
                .ReturnsAsync(codes.Where(c => c.RecipientPhone == phone).ToList());

            // Act
            var result = await _handler.Handle(new GetFarmerSponsorshipInboxQuery
            {
                Phone = phone
            }, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Single(result.Data);
            Assert.Equal("AGRI-2025-TEST1", result.Data[0].Code);
        }

        [Fact]
        public async Task GetFarmerInbox_ExcludesUsedCodes_WhenIncludeUsedIsFalse()
        {
            // Arrange
            var phone = "+905551234567";

            var codes = new List<SponsorshipCode>
            {
                new SponsorshipCode
                {
                    Code = "AGRI-2025-ACTIVE",
                    RecipientPhone = phone,
                    LinkDelivered = true,
                    IsUsed = false,
                    ExpiryDate = DateTime.Now.AddDays(30)
                },
                new SponsorshipCode
                {
                    Code = "AGRI-2025-USED",
                    RecipientPhone = phone,
                    LinkDelivered = true,
                    IsUsed = true,
                    ExpiryDate = DateTime.Now.AddDays(30)
                }
            };

            _codeRepositoryMock.Setup(r => r.GetListAsync(It.IsAny<Func<SponsorshipCode, bool>>()))
                .ReturnsAsync(codes.Where(c => !c.IsUsed).ToList());

            // Act
            var result = await _handler.Handle(new GetFarmerSponsorshipInboxQuery
            {
                Phone = phone,
                IncludeUsed = false
            }, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Single(result.Data);
            Assert.Equal("AGRI-2025-ACTIVE", result.Data[0].Code);
        }

        [Fact]
        public async Task GetFarmerInbox_ExcludesExpiredCodes_WhenIncludeExpiredIsFalse()
        {
            // Arrange
            var phone = "+905551234567";

            var codes = new List<SponsorshipCode>
            {
                new SponsorshipCode
                {
                    Code = "AGRI-2025-VALID",
                    RecipientPhone = phone,
                    LinkDelivered = true,
                    IsUsed = false,
                    ExpiryDate = DateTime.Now.AddDays(30)
                },
                new SponsorshipCode
                {
                    Code = "AGRI-2025-EXPIRED",
                    RecipientPhone = phone,
                    LinkDelivered = true,
                    IsUsed = false,
                    ExpiryDate = DateTime.Now.AddDays(-5)
                }
            };

            _codeRepositoryMock.Setup(r => r.GetListAsync(It.IsAny<Func<SponsorshipCode, bool>>()))
                .ReturnsAsync(codes.Where(c => c.ExpiryDate > DateTime.Now).ToList());

            // Act
            var result = await _handler.Handle(new GetFarmerSponsorshipInboxQuery
            {
                Phone = phone,
                IncludeExpired = false
            }, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Single(result.Data);
            Assert.Equal("AGRI-2025-VALID", result.Data[0].Code);
        }

        [Theory]
        [InlineData("5551234567", "+905551234567")]
        [InlineData("05551234567", "+905551234567")]
        [InlineData("+905551234567", "+905551234567")]
        [InlineData("555 123 45 67", "+905551234567")]
        public async Task GetFarmerInbox_NormalizesPhoneNumber(string inputPhone, string expectedNormalized)
        {
            // Test that phone normalization works correctly
            // This is critical for matching codes sent with different phone formats

            // Arrange & Act
            var result = await _handler.Handle(new GetFarmerSponsorshipInboxQuery
            {
                Phone = inputPhone
            }, CancellationToken.None);

            // Assert
            // Verify that the normalized phone was used in repository query
            _codeRepositoryMock.Verify(r =>
                r.GetListAsync(It.IsAny<Func<SponsorshipCode, bool>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetFarmerInbox_ReturnsEmptyList_WhenNoCodesFound()
        {
            // Arrange
            _codeRepositoryMock.Setup(r => r.GetListAsync(It.IsAny<Func<SponsorshipCode, bool>>()))
                .ReturnsAsync(new List<SponsorshipCode>());

            // Act
            var result = await _handler.Handle(new GetFarmerSponsorshipInboxQuery
            {
                Phone = "+905551234567"
            }, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.Data);
            Assert.Contains("Henüz sponsorluk kodu gönderilmemiş", result.Message);
        }
    }
}
```

**Run Tests**:
```bash
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~GetFarmerSponsorshipInboxQueryTests"
```

**Checklist**:
- [ ] Create test file
- [ ] Test phone normalization
- [ ] Test filtering (used/unused, expired/active)
- [ ] Test empty results
- [ ] Test with multiple sponsors/tiers
- [ ] All tests pass

---

#### Step 2.2: Integration Tests (Postman/cURL)

**File**: `claudedocs/SponsorshipInbox/postman_tests.json`

**Test Case 1: Basic Query**
```bash
# Test 1: Get inbox for phone number
curl -X GET "http://localhost:5001/api/sponsorship/farmer-inbox?phone=05551234567" \
  -H "accept: application/json"

# Expected Response:
{
  "success": true,
  "message": "3 sponsorluk kodu bulundu",
  "data": [
    {
      "code": "AGRI-2025-XXXXX",
      "sponsorName": "Test Sponsor Company",
      "tierName": "M",
      "sentDate": "2025-01-24T10:30:00",
      "sentVia": "SMS",
      "isUsed": false,
      "usedDate": null,
      "expiryDate": "2025-02-23T10:30:00",
      "redemptionLink": "https://ziraai.com/redeem/AGRI-2025-XXXXX",
      "recipientName": "Test Farmer",
      "isExpired": false,
      "daysUntilExpiry": 30,
      "status": "Aktif"
    }
  ]
}
```

**Test Case 2: Include Used Codes**
```bash
curl -X GET "http://localhost:5001/api/sponsorship/farmer-inbox?phone=05551234567&includeUsed=true"
```

**Test Case 3: Include Expired Codes**
```bash
curl -X GET "http://localhost:5001/api/sponsorship/farmer-inbox?phone=05551234567&includeExpired=true"
```

**Test Case 4: Invalid Phone**
```bash
curl -X GET "http://localhost:5001/api/sponsorship/farmer-inbox?phone=" \
  -H "accept: application/json"

# Expected: 400 Bad Request
```

**Test Case 5: No Codes Found**
```bash
curl -X GET "http://localhost:5001/api/sponsorship/farmer-inbox?phone=05559999999"

# Expected:
{
  "success": true,
  "message": "Henüz sponsorluk kodu gönderilmemiş",
  "data": []
}
```

**Checklist**:
- [ ] Test with real data (after running SendSponsorshipLinkCommand)
- [ ] Test phone normalization (various formats)
- [ ] Test filtering options
- [ ] Test error cases
- [ ] Document all test cases

---

### 📱 PHASE 3: Frontend Implementation (Optional)

**Note**: Frontend implementation will be done by mobile team. This section provides guidance.

#### Step 3.1: API Service (Flutter)

**File**: `lib/services/api/sponsorship_api.dart`

```dart
class SponsorshipApi {
  static const String baseUrl = "https://api.ziraai.com/api/sponsorship";

  /// Get farmer's sponsorship inbox
  static Future<ApiResult<List<FarmerSponsorshipInboxDto>>> getFarmerInbox({
    required String phone,
    bool includeUsed = false,
    bool includeExpired = false,
  }) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/farmer-inbox')
          .replace(queryParameters: {
            'phone': phone,
            'includeUsed': includeUsed.toString(),
            'includeExpired': includeExpired.toString(),
          }),
      );

      if (response.statusCode == 200) {
        final json = jsonDecode(response.body);
        final List<FarmerSponsorshipInboxDto> codes = (json['data'] as List)
          .map((item) => FarmerSponsorshipInboxDto.fromJson(item))
          .toList();

        return ApiResult.success(codes);
      } else {
        return ApiResult.error('Failed to load inbox');
      }
    } catch (e) {
      return ApiResult.error('Network error: $e');
    }
  }
}
```

#### Step 3.2: Screen Implementation

**File**: `lib/screens/sponsorship_inbox_screen.dart`

```dart
class SponsorshipInboxScreen extends StatefulWidget {
  @override
  _SponsorshipInboxScreenState createState() => _SponsorshipInboxScreenState();
}

class _SponsorshipInboxScreenState extends State<SponsorshipInboxScreen> {
  List<FarmerSponsorshipInboxDto> _codes = [];
  bool _loading = true;
  bool _showUsed = false;

  @override
  void initState() {
    super.initState();
    _loadInbox();
  }

  Future<void> _loadInbox() async {
    setState(() => _loading = true);

    final phone = await AuthService.getCurrentUserPhone();
    final result = await SponsorshipApi.getFarmerInbox(
      phone: phone,
      includeUsed: _showUsed,
    );

    setState(() {
      _codes = result.data ?? [];
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Sponsorluk Kutum'),
        actions: [
          IconButton(
            icon: Icon(_showUsed ? Icons.filter_list_off : Icons.filter_list),
            onPressed: () {
              setState(() => _showUsed = !_showUsed);
              _loadInbox();
            },
          ),
        ],
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return Center(child: CircularProgressIndicator());
    }

    if (_codes.isEmpty) {
      return _buildEmptyState();
    }

    return RefreshIndicator(
      onRefresh: _loadInbox,
      child: ListView.builder(
        itemCount: _codes.length,
        itemBuilder: (context, index) => SponsorshipCodeCard(
          code: _codes[index],
          onRedeem: () => _redeemCode(_codes[index]),
        ),
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.inbox, size: 64, color: Colors.grey),
          SizedBox(height: 16),
          Text(
            'Henüz sponsorluk kodu almadınız',
            style: TextStyle(fontSize: 16, color: Colors.grey),
          ),
        ],
      ),
    );
  }

  Future<void> _redeemCode(FarmerSponsorshipInboxDto code) async {
    // Navigate to redemption screen
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => RedeemCodeScreen(code: code.code),
      ),
    );

    // Refresh after redemption
    await _loadInbox();
  }
}
```

**Navigation Integration**:
```dart
// Add to bottom navigation bar
BottomNavigationBarItem(
  icon: Icon(Icons.inbox),
  label: 'Sponsorluk Kutum',
)

// Route
case '/sponsorship-inbox':
  return MaterialPageRoute(
    builder: (context) => SponsorshipInboxScreen(),
  );
```

---

## 🔄 End-to-End Flow

### Complete User Journey

```
STEP 1: Sponsor sends code
─────────────────────────────
POST /api/sponsorship/send-sponsorship-link
{
  "sponsorId": 123,
  "recipients": [
    {
      "code": "AGRI-2025-X3K9",
      "phone": "05551234567",
      "name": "Test Farmer"
    }
  ],
  "channel": "SMS"
}

↓ (Backend sets RecipientPhone, RecipientName, LinkSentDate)

STEP 2: Farmer receives SMS
─────────────────────────────
SMS Message:
"Test Sponsor Company size sponsorluk paketi hediye etti!

Sponsorluk Kodunuz: AGRI-2025-X3K9

Hemen kullanmak icin tiklayin:
https://ziraai.com/redeem/AGRI-2025-X3K9"

↓ (Farmer may lose SMS or want to see all codes)

STEP 3: Farmer opens inbox
─────────────────────────────
GET /api/sponsorship/farmer-inbox?phone=05551234567

Response:
{
  "success": true,
  "data": [
    {
      "code": "AGRI-2025-X3K9",
      "sponsorName": "Test Sponsor Company",
      "tierName": "M",
      "sentDate": "2025-01-24T10:30:00",
      "sentVia": "SMS",
      "isUsed": false,
      "status": "Aktif",
      "daysUntilExpiry": 30
    }
  ]
}

↓ (Farmer sees code in app)

STEP 4: Farmer redeems code
─────────────────────────────
POST /api/sponsorship/redeem
{
  "code": "AGRI-2025-X3K9",
  "userId": 456
}

↓ (Code marked as used)

STEP 5: Farmer checks inbox again
─────────────────────────────────
GET /api/sponsorship/farmer-inbox?phone=05551234567&includeUsed=true

Response:
{
  "success": true,
  "data": [
    {
      "code": "AGRI-2025-X3K9",
      "isUsed": true,
      "usedDate": "2025-01-24T11:00:00",
      "status": "Kullanıldı"
    }
  ]
}
```

---

## 🚨 Error Handling & Edge Cases

### Case 1: Phone Number Format Variations
```
Input: "555 123 45 67"
Normalized: "+905551234567"

Input: "05551234567"
Normalized: "+905551234567"

Input: "+90 555 123 45 67"
Normalized: "+905551234567"
```

**Solution**: `FormatPhoneNumber()` method handles all formats

### Case 2: No Codes Found
```json
{
  "success": true,
  "message": "Henüz sponsorluk kodu gönderilmemiş",
  "data": []
}
```

**Frontend**: Show empty state with helpful message

### Case 3: Expired Codes
```
Default: Excluded (includeExpired=false)
Option: Include with "Süresi Doldu" status
```

**Frontend**: Show expired codes in grey with warning icon

### Case 4: Multiple Sponsors
```
Codes from different sponsors shown in list
Sorted by SentDate (newest first)
```

**Frontend**: Group by sponsor or show chronologically

### Case 5: Same Code Sent Multiple Times
```
Should NOT happen (business rule)
But if it does, show all occurrences
```

**Backend**: Query returns all matching records

---

## 📊 Performance Considerations

### Database Query Optimization

**Without Index**:
```
Query Time: ~500ms (full table scan)
Explain: Seq Scan on SponsorshipCodes
```

**With Index**:
```
Query Time: ~5ms (index scan)
Explain: Index Scan using IX_SponsorshipCodes_RecipientPhone
```

**Recommendation**: Add index in Phase 1.5

### Caching Strategy (Optional)

```csharp
// Cache inbox results for 5 minutes
var cacheKey = $"SponsorshipInbox:{normalizedPhone}";
var cachedResult = await _cacheManager.GetAsync<List<FarmerSponsorshipInboxDto>>(cacheKey);

if (cachedResult != null)
{
    return new SuccessDataResult<List<FarmerSponsorshipInboxDto>>(cachedResult);
}

// ... fetch from DB ...

await _cacheManager.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
```

**Trade-off**: Faster response vs stale data

---

## 🎯 Success Criteria

### Phase 1 (Backend) ✅
- [ ] DTO created and compiles
- [ ] Query and Handler implemented
- [ ] Endpoint added to controller
- [ ] Unit tests pass (>80% coverage)
- [ ] Postman tests pass
- [ ] Database index added
- [ ] No performance degradation (<50ms response time)

### Phase 2 (Testing) ✅
- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] Postman collection documented
- [ ] Edge cases tested
- [ ] Performance benchmarked

### Phase 3 (Frontend) ✅
- [ ] API service implemented
- [ ] Screen implemented
- [ ] Navigation integrated
- [ ] Empty states handled
- [ ] Error handling implemented
- [ ] Beta testing complete

### Acceptance Criteria ✅
- [ ] Farmer can see all codes sent to their phone
- [ ] Correct sponsor names displayed
- [ ] Expiry dates shown correctly
- [ ] Used codes marked appropriately
- [ ] Redemption flow works from inbox
- [ ] Response time <100ms (95th percentile)
- [ ] Zero downtime deployment

---

## 🔐 Security Checklist

### Authentication Decision: NO AUTH (Public Endpoint)

**Rationale**:
- Phone number is the identifier
- Similar to password reset flow (public endpoint with phone)
- Farmer may not be logged in when checking SMS

**Risks**:
- Phone number enumeration attack
- Anyone with phone can see codes sent to that number

**Mitigations**:
- Rate limiting on endpoint (10 requests/minute per IP)
- No personal data exposed (only codes)
- Codes are one-time use (can't be reused if stolen)

**Alternative**: Require authentication (implement in Phase 4 if needed)

### Data Exposure

**Exposed**:
- ✅ Code (public, sent via SMS)
- ✅ Sponsor name (public info)
- ✅ Tier name (public info)
- ✅ Expiry date (non-sensitive)

**Not Exposed**:
- ❌ Sponsor UserId
- ❌ Purchase details
- ❌ Internal IDs

---

## 📈 Monitoring & Analytics

### Metrics to Track

```csharp
// 1. Inbox View Rate
await _analytics.TrackEventAsync("SponsorshipInboxViewed", new {
    Phone = phone,
    CodesCount = codes.Count,
    HasUsedCodes = codes.Any(c => c.IsUsed),
    HasExpiredCodes = codes.Any(c => c.IsExpired)
});

// 2. Redemption Source
await _analytics.TrackEventAsync("CodeRedeemed", new {
    Code = code,
    Source = "Inbox" // vs "SMS_Link" or "Manual_Entry"
});

// 3. Time to Redemption
var timeToRedeem = (redeemDate - sentDate).TotalHours;
await _analytics.TrackMetricAsync("InboxTimeToRedeemHours", timeToRedeem);
```

### Dashboard KPIs

```
Sponsor Dashboard > Code Distribution
├─ Total Sent: 1,000 codes
├─ Inbox Views: 750 (75%) [NEW]
├─ Redeemed: 600 (60%)
└─ Redeemed via Inbox: 450 (75% of redemptions) [NEW]

Farmer Engagement
├─ Avg Codes Per Farmer: 2.3
├─ Avg Inbox Visits: 3.5
├─ Inbox-to-Redemption Rate: 80%
```

---

## 🚀 Deployment Plan

### Pre-Deployment Checklist
- [ ] All code merged to `feature/sponsorship-inbox` branch
- [ ] Unit tests pass (100%)
- [ ] Integration tests pass
- [ ] Code review approved
- [ ] Database backup taken
- [ ] Rollback plan documented

### Staging Deployment
```bash
# 1. Deploy to staging
git checkout staging
git merge feature/sponsorship-inbox
git push origin staging

# 2. Run migrations (if any)
dotnet ef database update --project DataAccess --startup-project WebAPI

# 3. Test on staging
curl https://staging-api.ziraai.com/api/sponsorship/farmer-inbox?phone=05551234567

# 4. Monitor logs
tail -f /var/log/ziraai/staging.log | grep "SponsorshipInbox"
```

### Production Deployment
```bash
# 1. Merge to master
git checkout master
git merge feature/sponsorship-inbox
git tag v1.5.0-sponsorship-inbox

# 2. Deploy to production
# (Use CI/CD pipeline or manual deployment)

# 3. Add database index (minimal downtime)
psql -h production-db -U ziraai -d ziraai_production \
  -c "CREATE INDEX CONCURRENTLY IX_SponsorshipCodes_RecipientPhone_LinkDelivered_ExpiryDate ON SponsorshipCodes (RecipientPhone, LinkDelivered, ExpiryDate) WHERE RecipientPhone IS NOT NULL;"

# 4. Smoke test
curl https://api.ziraai.com/api/sponsorship/farmer-inbox?phone=05551234567

# 5. Monitor metrics
# - Response time <100ms
# - Error rate <1%
# - Memory/CPU usage normal
```

### Rollback Plan
```bash
# If issues occur:
git revert HEAD
git push origin master

# Redeploy previous version
# No database changes to rollback (backward compatible)
```

---

## 📚 Documentation

### API Documentation (Swagger)

```yaml
/api/sponsorship/farmer-inbox:
  get:
    summary: Get farmer's sponsorship code inbox
    description: Returns all sponsorship codes sent to the farmer's phone number
    parameters:
      - name: phone
        in: query
        required: true
        schema:
          type: string
        description: Farmer's phone number (will be normalized)
        example: "05551234567"
      - name: includeUsed
        in: query
        required: false
        schema:
          type: boolean
          default: false
        description: Include already redeemed codes
      - name: includeExpired
        in: query
        required: false
        schema:
          type: boolean
          default: false
        description: Include expired codes
    responses:
      200:
        description: Successful response
        content:
          application/json:
            schema:
              type: object
              properties:
                success:
                  type: boolean
                message:
                  type: string
                data:
                  type: array
                  items:
                    $ref: '#/components/schemas/FarmerSponsorshipInboxDto'
      400:
        description: Bad request (invalid phone number)
```

### User Documentation (README)

**File**: `claudedocs/SponsorshipInbox/USER_GUIDE.md`

```markdown
# Sponsorluk Kutusu - Kullanıcı Kılavuzu

## Farmer (Çiftçi) İçin

### Özellik Nedir?
Size SMS ile gönderilen tüm sponsorluk kodlarını tek bir yerde görebilirsiniz.

### Nasıl Kullanılır?
1. ZiraAI uygulamasını açın
2. "Sponsorluk Kutum" sekmesine gidin
3. Gelen kodları görün
4. "Kullan" butonuna basarak kodu aktif edin

### Faydalar
- SMS kaybolsa bile kodlara ulaşabilirsiniz
- Hangi sponsor gönderdi görebilirsiniz
- Kodların son kullanma tarihini takip edebilirsiniz
- Kullanılmış kodları geçmişte görebilirsiniz

## Sponsor İçin

### Değişen Bir Şey Var mı?
Hayır! Mevcut kod gönderme süreciniz aynı kalıyor.

### Ek Bilgi
Artık farmer'lar SMS'i kaybetseler bile kodlara ulaşabiliyor.
Bu, redemption rate'inizin artmasına yardımcı olur.
```

---

## 🎓 Knowledge Transfer

### Key Decisions Documented

1. **Why No New Entity?**
   - `SponsorshipCode` zaten gerekli alanları içeriyor
   - `RecipientPhone` by `SendSponsorshipLinkCommand` set ediliyor
   - Daha basit ve hızlı implementation

2. **Why No Authentication?**
   - Phone number is identifier (like password reset)
   - Simpler UX (no login required)
   - Risk mitigation via rate limiting

3. **Why This Endpoint Design?**
   - RESTful: GET /farmer-inbox (resource-based)
   - Query parameters for filtering (standard pattern)
   - Similar to existing endpoints (consistency)

### Related Documentation

- [Dealer Invitation Pattern](../DealerInvitation/)
- [Sponsorship Queue Flow](../SPONSORSHIP_QUEUE_FLOW_ANALYSIS.md)
- [SendSponsorshipLinkCommand](../../Business/Handlers/Sponsorship/Commands/SendSponsorshipLinkCommand.cs)

---

## ✅ Final Checklist

### Before Starting Implementation
- [ ] Read this document completely
- [ ] Understand the reference pattern (Dealer Invitation)
- [ ] Verify `SponsorshipCode` fields exist
- [ ] Set up development environment
- [ ] Create feature branch: `feature/sponsorship-inbox`

### During Implementation
- [ ] Follow step-by-step guide
- [ ] Check off completed steps
- [ ] Run tests after each phase
- [ ] Commit frequently with descriptive messages
- [ ] Ask for help if stuck

### After Implementation
- [ ] All tests pass
- [ ] Code review requested
- [ ] Documentation updated
- [ ] Deployed to staging
- [ ] Stakeholder demo completed

---

## 🆘 Troubleshooting

### Issue: Query returns empty results
```sql
-- Debug: Check if RecipientPhone is populated
SELECT COUNT(*), RecipientPhone IS NULL AS IsNull
FROM SponsorshipCodes
GROUP BY RecipientPhone IS NULL;

-- Expected: Some rows with RecipientPhone NOT NULL
```

**Solution**: Verify `SendSponsorshipLinkCommand` is setting `RecipientPhone`

### Issue: Phone normalization not working
```csharp
// Test normalization
var test = FormatPhoneNumber("555 123 45 67");
Console.WriteLine(test); // Should be: +905551234567
```

**Solution**: Check `FormatPhoneNumber` logic matches `SendSponsorshipLinkCommand`

### Issue: Slow query performance
```sql
-- Check if index exists
SELECT * FROM pg_indexes
WHERE tablename = 'SponsorshipCodes'
  AND indexname LIKE '%RecipientPhone%';
```

**Solution**: Create index (Step 1.5)

### Issue: Wrong sponsor names
```sql
-- Verify join is working
SELECT sc.Code, sp.CompanyName
FROM SponsorshipCodes sc
LEFT JOIN SponsorProfiles sp ON sc.SponsorId = sp.SponsorId
WHERE sc.RecipientPhone = '+905551234567';
```

**Solution**: Check `SponsorProfileRepository` query logic

---

## 📞 Support

### Questions?
- Check this document first
- Review reference implementation: `GetDealerInvitationsQuery`
- Ask team lead

### Found a Bug?
- Create issue in GitHub
- Include: steps to reproduce, expected vs actual, logs
- Tag: `sponsorship-inbox`

### Need Help?
- Slack: #ziraai-dev
- Email: dev@ziraai.com

---

**Document Version**: 1.0
**Created**: 2025-01-24
**Last Updated**: 2025-01-24
**Author**: ZiraAI Dev Team
**Status**: 🟢 Ready for Implementation
