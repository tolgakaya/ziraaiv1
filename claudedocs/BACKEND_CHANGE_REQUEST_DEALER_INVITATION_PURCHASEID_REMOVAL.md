# Backend Change Request: Dealer Invitation `purchaseId` Removal

**Document Version**: 1.0
**Date**: 2025-10-30
**Requested By**: Mobile Team
**Priority**: Medium
**Impact**: API Breaking Change (with backward compatibility option)

---

## 📋 Executive Summary

The current dealer invitation endpoint (`POST /api/v1/sponsorship/dealer/invite-via-sms`) requires a `purchaseId` parameter that creates unnecessary friction and limits business flexibility. This document proposes removing `purchaseId` and implementing intelligent code selection based on sponsor's available code pool.

**Key Benefits**:
- ✅ Improved UX (one less required field)
- ✅ Business logic alignment (tier matters, not purchase)
- ✅ Automatic code optimization (FIFO + expiry-first)
- ✅ Multi-purchase flexibility (codes from any purchase)

**Estimated Implementation Time**: 4-6 hours
**Risk Level**: Low (backward compatible migration possible)

---

## 🔍 Current Implementation Analysis

### Current Endpoint Signature

```http
POST /api/v1/sponsorship/dealer/invite-via-sms
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body**:
```json
{
  "email": "dealer@example.com",
  "phone": "+905551234567",
  "dealerName": "ABC Tarım Bayii",
  "purchaseId": 26,        // ← PROBLEMATIC FIELD
  "codeCount": 10
}
```

**Response**:
```json
{
  "data": {
    "invitationId": 15,
    "invitationToken": "a1b2c3d4e5f67890...",
    "invitationLink": "https://...",
    "codeCount": 10,
    "status": "Pending",
    // ...
  },
  "success": true,
  "message": "📱 Bayilik daveti gönderildi"
}
```

### Current Backend Logic

**File**: `Business/Handlers/Sponsorship/Commands/InviteDealerViaSmsCommand.cs`

```csharp
// Simplified current implementation
public async Task<IDataResult<DealerInvitationDto>> Handle(
    InviteDealerViaSmsCommand request,
    CancellationToken cancellationToken)
{
    // 1. Validate purchaseId belongs to sponsor
    var purchase = await _context.SponsorPackages
        .Where(p => p.Id == request.PurchaseId)
        .Where(p => p.SponsorId == currentSponsorId)
        .FirstOrDefaultAsync();

    if (purchase == null)
        return new ErrorDataResult<>("Geçersiz paket ID");

    // 2. Get codes from specific purchase
    var availableCodes = await _context.SponsorshipCodes
        .Where(c => c.PurchaseId == request.PurchaseId)
        .Where(c => !c.IsUsed)
        .Where(c => c.DealerId == null)
        .Where(c => c.ExpiryDate > DateTime.UtcNow)
        .Take(request.CodeCount)
        .ToListAsync();

    if (availableCodes.Count < request.CodeCount)
        return new ErrorDataResult<>(
            $"Yetersiz kod. Mevcut: {availableCodes.Count}, İstenen: {request.CodeCount}"
        );

    // 3. Create invitation and transfer codes
    // ... (rest of logic)
}
```

### Database Schema

**DealerInvitations Table**:
```sql
CREATE TABLE "DealerInvitations" (
    "Id" serial4 PRIMARY KEY,
    "SponsorId" int4 NOT NULL,
    "Email" varchar(255),
    "Phone" varchar(20),
    "DealerName" varchar(255) NOT NULL,
    "PurchaseId" int4 NOT NULL,        -- ← Currently required
    "CodeCount" int4 NOT NULL,
    "InvitationToken" varchar(255) UNIQUE,
    "Status" varchar(50) DEFAULT 'Pending',
    "CreatedDate" timestamp DEFAULT NOW(),
    "ExpiryDate" timestamp NOT NULL,
    -- ... other fields
);
```

**SponsorshipCodes Table** (code transfer tracking):
```sql
UPDATE "SponsorshipCodes"
SET
    "DealerId" = {dealerId},
    "TransferredAt" = NOW(),
    "TransferredByUserId" = {sponsorId}
WHERE "Id" IN (945, 946, 947, ...);
```

---

## ❌ Problems with Current Implementation

### 1. Business Logic Mismatch

**Scenario**: Sponsor has multiple packages
```
Purchase 1 (ID: 23): 50 codes, M Tier, Expires 2025-11-15
Purchase 2 (ID: 26): 100 codes, M Tier, Expires 2025-12-01
Purchase 3 (ID: 31): 30 codes, L Tier, Expires 2025-11-10
```

**Problem**: To send 20 M-tier codes to dealer, sponsor must:
1. Remember or lookup purchaseId
2. Choose which specific purchase to use (23 or 26?)
3. Cannot automatically use codes from multiple purchases

**Reality**: Business doesn't care which purchase codes come from, only:
- How many codes (`codeCount`)
- Which tier (`packageTier`)
- Codes should be valid (unused, not expired)

### 2. User Experience Friction

**Current Flow** (Poor UX):
```
Sponsor Dashboard
  ↓
"Invite Dealer" Button
  ↓
Must select purchaseId from dropdown
  ↓ (sponsor doesn't remember IDs)
Navigate to "My Purchases" to find ID
  ↓
Copy purchaseId: 26
  ↓
Go back to invitation form
  ↓
Paste purchaseId and complete form
  ↓
Submit invitation
```

**Ideal Flow** (Good UX):
```
Sponsor Dashboard
  ↓
"Invite Dealer" Button
  ↓
Enter dealer info + code count
  ↓
Submit (backend auto-selects best codes)
```

**UX Impact**: 4 extra steps removed, cognitive load reduced

### 3. Limited Flexibility

**Problem**: Single-purchase limitation

**Example**:
- Purchase 1 has only 5 codes remaining
- Sponsor wants to send 10 codes
- **Current**: Error or must create 2 separate invitations
- **Proposed**: Automatically pull 5 from Purchase 1 + 5 from Purchase 2

### 4. Code Waste Risk

**Current**: No optimization for expiring codes

**Example**:
```
Purchase A: 50 codes, expires in 2 days
Purchase B: 100 codes, expires in 30 days

Sponsor selects Purchase B (more codes available)
→ Purchase A codes may expire unused (waste)
```

**Proposed**: Automatically prioritize codes closest to expiry

### 5. Validation Overhead

**Current**: Must validate `purchaseId` ownership
```csharp
// Extra query needed
var purchase = await _context.SponsorPackages
    .Where(p => p.Id == request.PurchaseId)
    .Where(p => p.SponsorId == currentSponsorId)
    .FirstOrDefaultAsync();
```

**Security Risk**: Sponsor could guess/try other purchase IDs

---

## ✅ Proposed Solution

### New Endpoint Signature

**Request Body** (Breaking Change):
```json
{
  "email": "dealer@example.com",
  "phone": "+905551234567",
  "dealerName": "ABC Tarım Bayii",
  "codeCount": 10,
  "packageTier": "M"        // ← NEW: Optional tier filter (S/M/L/XL)
  // purchaseId removed
}
```

**Validation Rules**:
```json
{
  "email": "string (optional, email format)",
  "phone": "string (required, E.164 format)",
  "dealerName": "string (required, 1-255 chars)",
  "codeCount": "integer (required, min: 1, max: 1000)",
  "packageTier": "string (optional, enum: S|M|L|XL)"
}
```

### New Backend Logic

**File**: `Business/Handlers/Sponsorship/Commands/InviteDealerViaSmsCommand.cs`

```csharp
public class InviteDealerViaSmsCommand : IRequest<IDataResult<DealerInvitationDto>>
{
    public string Email { get; set; }
    public string Phone { get; set; }
    public string DealerName { get; set; }
    public int CodeCount { get; set; }

    // NEW: Optional tier filter
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? PackageTier { get; set; }  // S, M, L, XL
}

public async Task<IDataResult<DealerInvitationDto>> Handle(
    InviteDealerViaSmsCommand request,
    CancellationToken cancellationToken)
{
    // 1. Validate tier (if provided)
    if (!string.IsNullOrEmpty(request.PackageTier))
    {
        var validTiers = new[] { "S", "M", "L", "XL" };
        if (!validTiers.Contains(request.PackageTier))
        {
            return new ErrorDataResult<DealerInvitationDto>(
                "Geçersiz paket tier. Geçerli değerler: S, M, L, XL"
            );
        }
    }

    // 2. Get sponsor's available codes with intelligent selection
    var codesToTransfer = await GetCodesToTransfer(
        currentSponsorId,
        request.CodeCount,
        request.PackageTier
    );

    // 3. Validate sufficient codes
    if (codesToTransfer.Count < request.CodeCount)
    {
        var tierMessage = !string.IsNullOrEmpty(request.PackageTier)
            ? $" ({request.PackageTier} tier)"
            : "";

        return new ErrorDataResult<DealerInvitationDto>(
            $"Yetersiz kod{tierMessage}. Mevcut: {codesToTransfer.Count}, İstenen: {request.CodeCount}"
        );
    }

    // 4. Create invitation
    var invitation = new DealerInvitation
    {
        SponsorId = currentSponsorId,
        Email = request.Email,
        Phone = request.Phone,
        DealerName = request.DealerName,
        CodeCount = request.CodeCount,
        PackageTier = request.PackageTier,  // NEW: Store tier filter
        // Note: PurchaseId removed
        InvitationToken = Guid.NewGuid().ToString("N"),
        Status = "Pending",
        CreatedDate = DateTime.UtcNow,
        ExpiryDate = DateTime.UtcNow.AddDays(7)
    };

    await _context.DealerInvitations.AddAsync(invitation);

    // 5. Reserve codes for this invitation (mark but don't transfer yet)
    foreach (var code in codesToTransfer)
    {
        code.ReservedForInvitationId = invitation.Id;  // NEW field
        code.ReservedAt = DateTime.UtcNow;
    }

    await _context.SaveChangesAsync();

    // 6. Send SMS/WhatsApp
    // ... existing SMS logic

    return new SuccessDataResult<DealerInvitationDto>(
        MapToDto(invitation),
        "📱 Bayilik daveti gönderildi"
    );
}

/// <summary>
/// Intelligent code selection algorithm
/// Priority: 1) Expiry date (FIFO) 2) Creation date (oldest first)
/// </summary>
private async Task<List<SponsorshipCode>> GetCodesToTransfer(
    int sponsorId,
    int codeCount,
    string? packageTier)
{
    // Start with base query
    var query = _context.SponsorshipCodes
        .Include(c => c.Package)
        .Where(c => c.SponsorId == sponsorId)
        .Where(c => !c.IsUsed)
        .Where(c => c.DealerId == null)  // Not already transferred
        .Where(c => c.ReservedForInvitationId == null)  // Not reserved
        .Where(c => c.ExpiryDate > DateTime.UtcNow);

    // Apply tier filter if specified
    if (!string.IsNullOrEmpty(packageTier))
    {
        query = query.Where(c => c.Package.TierId == packageTier);
    }

    // Intelligent ordering:
    // 1. Codes expiring soonest first (prevent waste)
    // 2. Oldest codes first (FIFO for same expiry date)
    var codes = await query
        .OrderBy(c => c.ExpiryDate)
        .ThenBy(c => c.CreatedDate)
        .Take(codeCount)
        .ToListAsync();

    return codes;
}
```

### Code Transfer Logic (on acceptance)

**File**: `Business/Handlers/Sponsorship/Commands/AcceptDealerInvitationCommand.cs`

```csharp
public async Task<IDataResult<AcceptInvitationDto>> Handle(
    AcceptDealerInvitationCommand request,
    CancellationToken cancellationToken)
{
    // ... existing validation logic

    // Get reserved codes for this invitation
    var codesToTransfer = await _context.SponsorshipCodes
        .Where(c => c.ReservedForInvitationId == invitation.Id)
        .ToListAsync();

    if (codesToTransfer.Count < invitation.CodeCount)
    {
        // Fallback: Get fresh codes if reservation expired or codes were used
        codesToTransfer = await GetCodesToTransfer(
            invitation.SponsorId,
            invitation.CodeCount,
            invitation.PackageTier
        );
    }

    // Transfer codes to dealer
    foreach (var code in codesToTransfer)
    {
        code.DealerId = currentUserId;
        code.TransferredAt = DateTime.UtcNow;
        code.TransferredByUserId = invitation.SponsorId;
        code.ReservedForInvitationId = null;  // Clear reservation
    }

    invitation.Status = "Accepted";
    invitation.AcceptedDate = DateTime.UtcNow;
    invitation.CreatedDealerId = currentUserId;

    await _context.SaveChangesAsync();

    return new SuccessDataResult<AcceptInvitationDto>(
        new AcceptInvitationDto
        {
            InvitationId = invitation.Id,
            DealerId = currentUserId,
            TransferredCodeCount = codesToTransfer.Count,
            TransferredCodeIds = codesToTransfer.Select(c => c.Id).ToList(),
            AcceptedAt = DateTime.UtcNow,
            Message = $"✅ Tebrikler! {codesToTransfer.Count} adet kod hesabınıza transfer edildi."
        },
        "Bayilik daveti başarıyla kabul edildi"
    );
}
```

---

## 🗄️ Database Changes

### Migration Script

**File**: `Migrations/AddDealerInvitationPackageTier.sql`

```sql
-- Migration: Remove purchaseId requirement, add packageTier and code reservation

-- Step 1: Make PurchaseId nullable (backward compatibility)
ALTER TABLE "DealerInvitations"
ALTER COLUMN "PurchaseId" DROP NOT NULL;

-- Step 2: Add PackageTier column
ALTER TABLE "DealerInvitations"
ADD COLUMN "PackageTier" VARCHAR(10) NULL;

COMMENT ON COLUMN "DealerInvitations"."PackageTier" IS
'Optional tier filter: S, M, L, XL. If null, invitation can use codes from any tier.';

-- Step 3: Add code reservation fields to SponsorshipCodes
ALTER TABLE "SponsorshipCodes"
ADD COLUMN "ReservedForInvitationId" INT4 NULL,
ADD COLUMN "ReservedAt" TIMESTAMP NULL;

COMMENT ON COLUMN "SponsorshipCodes"."ReservedForInvitationId" IS
'Invitation ID for which this code is reserved (prevents double-allocation)';

-- Step 4: Add foreign key
ALTER TABLE "SponsorshipCodes"
ADD CONSTRAINT "FK_SponsorshipCodes_ReservedForInvitation"
FOREIGN KEY ("ReservedForInvitationId")
REFERENCES "DealerInvitations"("Id")
ON DELETE SET NULL;

-- Step 5: Create index for reservation queries
CREATE INDEX "IX_SponsorshipCodes_ReservedForInvitationId"
ON "SponsorshipCodes"("ReservedForInvitationId")
WHERE "ReservedForInvitationId" IS NOT NULL;

-- Step 6: Create composite index for intelligent code selection
CREATE INDEX "IX_SponsorshipCodes_Selection"
ON "SponsorshipCodes"(
    "SponsorId",
    "IsUsed",
    "DealerId",
    "ExpiryDate",
    "CreatedDate"
)
WHERE "IsUsed" = false
  AND "DealerId" IS NULL
  AND "ReservedForInvitationId" IS NULL;

-- Step 7: Populate PackageTier for existing invitations (data migration)
UPDATE "DealerInvitations" di
SET "PackageTier" = (
    SELECT DISTINCT p."TierId"
    FROM "SponsorPackages" p
    WHERE p."Id" = di."PurchaseId"
    LIMIT 1
)
WHERE di."PurchaseId" IS NOT NULL;
```

### Rollback Script

```sql
-- Rollback migration (if needed)

-- Remove new columns
ALTER TABLE "SponsorshipCodes"
DROP COLUMN IF EXISTS "ReservedForInvitationId",
DROP COLUMN IF EXISTS "ReservedAt";

ALTER TABLE "DealerInvitations"
DROP COLUMN IF EXISTS "PackageTier";

-- Restore PurchaseId requirement
ALTER TABLE "DealerInvitations"
ALTER COLUMN "PurchaseId" SET NOT NULL;

-- Drop indexes
DROP INDEX IF EXISTS "IX_SponsorshipCodes_ReservedForInvitationId";
DROP INDEX IF EXISTS "IX_SponsorshipCodes_Selection";
```

---

## 🔄 Migration Strategy

### Phase 1: Backward Compatible (Weeks 1-2)

**Goal**: Support both old and new API formats

```csharp
public class InviteDealerViaSmsCommand
{
    // Legacy field (deprecated)
    [Obsolete("Use packageTier instead. Will be removed in v2.0")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? PurchaseId { get; set; }

    // New fields
    public int CodeCount { get; set; }
    public string? PackageTier { get; set; }
}

// Handler logic
public async Task<IDataResult<DealerInvitationDto>> Handle(...)
{
    List<SponsorshipCode> codes;

    if (request.PurchaseId.HasValue)
    {
        // OLD METHOD (deprecated but working)
        _logger.LogWarning(
            "Using deprecated purchaseId parameter. Please migrate to packageTier."
        );

        codes = await GetCodesFromPurchase(
            request.PurchaseId.Value,
            request.CodeCount
        );
    }
    else
    {
        // NEW METHOD (recommended)
        codes = await GetCodesToTransfer(
            currentSponsorId,
            request.CodeCount,
            request.PackageTier
        );
    }

    // ... rest of logic
}
```

**API Documentation Update**:
```yaml
/api/v1/sponsorship/dealer/invite-via-sms:
  post:
    requestBody:
      content:
        application/json:
          schema:
            properties:
              purchaseId:
                type: integer
                deprecated: true
                description: "⚠️ DEPRECATED: Use packageTier instead"
              codeCount:
                type: integer
                required: true
              packageTier:
                type: string
                enum: [S, M, L, XL]
                description: "NEW: Optional tier filter"
```

### Phase 2: Full Migration (Week 3)

**Goal**: Remove `purchaseId` completely

```csharp
public class InviteDealerViaSmsCommand
{
    // purchaseId removed completely
    public int CodeCount { get; set; }
    public string? PackageTier { get; set; }
}
```

**Breaking Change Notice**:
```
API Version 2.0 - Breaking Changes

Endpoint: POST /api/v1/sponsorship/dealer/invite-via-sms

REMOVED:
- purchaseId (integer) - No longer accepted

ADDED:
- packageTier (string, optional) - Filter codes by tier (S/M/L/XL)

Migration Guide:
OLD: { "purchaseId": 26, "codeCount": 10 }
NEW: { "codeCount": 10, "packageTier": "M" }  // Optional tier
NEW: { "codeCount": 10 }  // No tier filter (uses any available codes)
```

---

## 📊 Benefits Comparison

| Metric | Before (purchaseId) | After (packageTier) | Improvement |
|--------|---------------------|---------------------|-------------|
| **Required Fields** | 5 fields | 4 fields | -20% |
| **User Steps** | 7 steps | 3 steps | -57% |
| **Code Waste Risk** | High (no optimization) | Low (expiry-first) | ✅ Reduced |
| **Multi-Purchase Support** | ❌ No | ✅ Yes | ✅ Added |
| **Business Logic Alignment** | ❌ Poor | ✅ Good | ✅ Improved |
| **Validation Queries** | 2 queries | 1 query | -50% |
| **API Security** | Medium (ID guessing) | High (no ID exposure) | ✅ Improved |

---

## 🧪 Testing Plan

### Unit Tests

```csharp
[TestClass]
public class InviteDealerViaSmsCommandTests
{
    [TestMethod]
    public async Task Should_Select_Codes_From_Multiple_Purchases()
    {
        // Arrange
        var sponsor = CreateSponsor();
        var purchase1 = CreatePurchase(sponsor, tier: "M", codeCount: 5);
        var purchase2 = CreatePurchase(sponsor, tier: "M", codeCount: 10);

        var command = new InviteDealerViaSmsCommand
        {
            CodeCount = 10,
            PackageTier = "M"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(10, result.Data.CodeCount);
        // Should pull 5 from purchase1 + 5 from purchase2
    }

    [TestMethod]
    public async Task Should_Prioritize_Expiring_Codes()
    {
        // Arrange
        var sponsor = CreateSponsor();
        CreateCodesWithExpiry(sponsor, count: 10, daysUntilExpiry: 2);  // Expiring soon
        CreateCodesWithExpiry(sponsor, count: 50, daysUntilExpiry: 30); // Expiring later

        var command = new InviteDealerViaSmsCommand { CodeCount = 5 };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var transferredCodes = GetTransferredCodes(result.Data.InvitationId);
        Assert.IsTrue(transferredCodes.All(c => c.ExpiryDate < DateTime.UtcNow.AddDays(5)));
        // Should use codes expiring in 2 days, not 30 days
    }

    [TestMethod]
    public async Task Should_Filter_By_Tier_When_Specified()
    {
        // Arrange
        var sponsor = CreateSponsor();
        CreateCodes(sponsor, tier: "S", count: 20);
        CreateCodes(sponsor, tier: "M", count: 20);
        CreateCodes(sponsor, tier: "L", count: 20);

        var command = new InviteDealerViaSmsCommand
        {
            CodeCount = 10,
            PackageTier = "M"  // Only M tier
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var transferredCodes = GetTransferredCodes(result.Data.InvitationId);
        Assert.IsTrue(transferredCodes.All(c => c.Package.TierId == "M"));
    }

    [TestMethod]
    public async Task Should_Return_Error_When_Insufficient_Codes()
    {
        // Arrange
        var sponsor = CreateSponsor();
        CreateCodes(sponsor, tier: "M", count: 5);  // Only 5 codes

        var command = new InviteDealerViaSmsCommand
        {
            CodeCount = 10,
            PackageTier = "M"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("Yetersiz kod"));
        Assert.IsTrue(result.Message.Contains("Mevcut: 5"));
        Assert.IsTrue(result.Message.Contains("İstenen: 10"));
    }

    [TestMethod]
    public async Task Should_Accept_Null_Tier_And_Use_Any_Available_Codes()
    {
        // Arrange
        var sponsor = CreateSponsor();
        CreateCodes(sponsor, tier: "S", count: 3);
        CreateCodes(sponsor, tier: "M", count: 4);
        CreateCodes(sponsor, tier: "L", count: 5);

        var command = new InviteDealerViaSmsCommand
        {
            CodeCount = 10,
            PackageTier = null  // No tier filter
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.Success);
        var transferredCodes = GetTransferredCodes(result.Data.InvitationId);
        Assert.AreEqual(10, transferredCodes.Count);
        // Should pull from different tiers: 3S + 4M + 3L = 10
    }

    [TestMethod]
    public async Task Should_Validate_Invalid_Tier()
    {
        // Arrange
        var command = new InviteDealerViaSmsCommand
        {
            CodeCount = 10,
            PackageTier = "INVALID"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("Geçersiz paket tier"));
    }
}
```

### Integration Tests

```bash
# Test 1: Create invitation without purchaseId (new method)
curl -X POST "https://api.ziraai.com/api/v1/sponsorship/dealer/invite-via-sms" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "dealer@test.com",
    "phone": "+905551234567",
    "dealerName": "Test Dealer",
    "codeCount": 10,
    "packageTier": "M"
  }'

# Expected: 200 OK with invitation details

# Test 2: Create invitation without tier filter
curl -X POST "https://api.ziraai.com/api/v1/sponsorship/dealer/invite-via-sms" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "dealer@test.com",
    "phone": "+905551234567",
    "dealerName": "Test Dealer",
    "codeCount": 10
  }'

# Expected: 200 OK, uses codes from any tier

# Test 3: Insufficient codes error
curl -X POST "https://api.ziraai.com/api/v1/sponsorship/dealer/invite-via-sms" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "dealer@test.com",
    "phone": "+905551234567",
    "dealerName": "Test Dealer",
    "codeCount": 1000,
    "packageTier": "XL"
  }'

# Expected: 400 Bad Request - "Yetersiz kod (XL tier)"
```

### Performance Tests

```sql
-- Test query performance for code selection
EXPLAIN ANALYZE
SELECT *
FROM "SponsorshipCodes" c
INNER JOIN "SponsorPackages" p ON c."PackageId" = p."Id"
WHERE c."SponsorId" = 123
  AND c."IsUsed" = false
  AND c."DealerId" IS NULL
  AND c."ReservedForInvitationId" IS NULL
  AND c."ExpiryDate" > NOW()
  AND p."TierId" = 'M'
ORDER BY c."ExpiryDate" ASC, c."CreatedDate" ASC
LIMIT 10;

-- Expected: < 10ms with proper indexes
-- Should use: IX_SponsorshipCodes_Selection
```

---

## 📝 API Documentation Update

### Swagger/OpenAPI Changes

```yaml
paths:
  /api/v1/sponsorship/dealer/invite-via-sms:
    post:
      summary: Create dealer invitation and send via SMS
      tags:
        - Dealer Management
      security:
        - bearerAuth: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required:
                - phone
                - dealerName
                - codeCount
              properties:
                email:
                  type: string
                  format: email
                  description: Dealer email address (optional)
                  example: "dealer@example.com"
                phone:
                  type: string
                  pattern: '^\+\d{10,15}$'
                  description: Dealer phone number (E.164 format)
                  example: "+905551234567"
                dealerName:
                  type: string
                  minLength: 1
                  maxLength: 255
                  description: Dealer/company name
                  example: "ABC Tarım Bayii"
                codeCount:
                  type: integer
                  minimum: 1
                  maximum: 1000
                  description: Number of codes to transfer
                  example: 10
                packageTier:
                  type: string
                  enum: [S, M, L, XL]
                  description: |
                    Optional tier filter for code selection.
                    If not specified, codes from any tier can be used.
                  example: "M"
            examples:
              withTierFilter:
                summary: With tier filter
                value:
                  email: "dealer@example.com"
                  phone: "+905551234567"
                  dealerName: "ABC Tarım Bayii"
                  codeCount: 10
                  packageTier: "M"
              withoutTierFilter:
                summary: Without tier filter (uses any available codes)
                value:
                  phone: "+905551234567"
                  dealerName: "XYZ Distributor"
                  codeCount: 20
      responses:
        '200':
          description: Invitation created and SMS sent successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/DealerInvitationResponse'
        '400':
          description: Validation error or insufficient codes
          content:
            application/json:
              examples:
                insufficientCodes:
                  summary: Insufficient codes
                  value:
                    success: false
                    message: "Yetersiz kod (M tier). Mevcut: 5, İstenen: 10"
                invalidTier:
                  summary: Invalid tier
                  value:
                    success: false
                    message: "Geçersiz paket tier. Geçerli değerler: S, M, L, XL"
        '401':
          description: Unauthorized (invalid or missing JWT token)
```

---

## 🎯 Implementation Checklist

### Backend Tasks

- [ ] **Database Migration**
  - [ ] Create migration script
  - [ ] Test on dev database
  - [ ] Add indexes for performance
  - [ ] Verify foreign key constraints

- [ ] **Command/Query Updates**
  - [ ] Update `InviteDealerViaSmsCommand` model
  - [ ] Implement `GetCodesToTransfer()` method
  - [ ] Add tier validation logic
  - [ ] Update `AcceptDealerInvitationCommand` to use reservations

- [ ] **Code Changes**
  - [ ] Add `packageTier` property
  - [ ] Remove `purchaseId` requirement (make nullable)
  - [ ] Implement intelligent code selection query
  - [ ] Add code reservation logic
  - [ ] Update validation rules

- [ ] **Unit Tests**
  - [ ] Test multi-purchase code selection
  - [ ] Test expiry-first prioritization
  - [ ] Test tier filtering
  - [ ] Test insufficient codes error
  - [ ] Test null tier (any available codes)
  - [ ] Test invalid tier validation

- [ ] **Integration Tests**
  - [ ] Test end-to-end invitation flow
  - [ ] Test code transfer on acceptance
  - [ ] Test SMS sending
  - [ ] Test error scenarios

- [ ] **Performance Testing**
  - [ ] Benchmark code selection query (< 10ms)
  - [ ] Test with large datasets (1M+ codes)
  - [ ] Verify index usage (EXPLAIN ANALYZE)

- [ ] **Documentation**
  - [ ] Update Swagger/OpenAPI spec
  - [ ] Update API documentation
  - [ ] Create migration guide for clients
  - [ ] Update Postman collection

### Mobile Tasks (Future)

- [ ] **When sponsor UI is built**:
  - [ ] Remove `purchaseId` field from UI
  - [ ] Add optional `packageTier` dropdown
  - [ ] Update API request model
  - [ ] Update validation logic
  - [ ] Update error handling

---

## 📞 Contact & Support

**Questions or Clarifications:**
- Mobile Team Lead: [Contact Info]
- Backend Team Lead: [Contact Info]
- Product Manager: [Contact Info]

**Related Documents:**
- `SMS_BASED_DEALER_INVITATION_FLOW.md` - Current implementation
- `SPONSORSHIP_SYSTEM_DOCUMENTATION.md` - Sponsorship overview
- `API_DOCUMENTATION_TIER_SYSTEM.md` - Tier system details

---

## 🏁 Conclusion

This change request removes an unnecessary constraint (`purchaseId`) and replaces it with a more flexible, business-aligned approach. The proposed solution:

1. ✅ Simplifies UX (fewer fields, less cognitive load)
2. ✅ Aligns with business logic (tier matters, not purchase)
3. ✅ Optimizes code usage (prevents waste through expiry-first selection)
4. ✅ Enables multi-purchase flexibility
5. ✅ Maintains backward compatibility during migration

**Recommendation**: Approve and implement with Phase 1 (backward compatible) approach, then remove deprecated `purchaseId` in Phase 2 after 2-3 weeks.

**Estimated Implementation**: 4-6 hours development + 2 hours testing = 1 business day

---

**Document Status**: Ready for Backend Team Review
**Next Steps**: Schedule technical review meeting with backend team
