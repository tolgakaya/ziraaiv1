# Farmer Sponsorship Invitation System - Design Document

**Project**: ZiraAI Backend - Sponsorship System Enhancement
**Branch**: `feature/remove-sms-listener-logic`
**Date**: 2026-01-02
**Status**: Design Phase

---

## 📋 Executive Summary

**Problem**: Google Play SDK 35+ compliance removed SMS listener permissions from mobile app. SMS messages containing actual sponsorship codes (e.g., "Kod: AGRI-X3K9") can no longer be auto-extracted by the app.

**Solution**: Implement invitation-based system similar to DealerInvitation pattern, where SMS contains invitation token instead of actual codes. User accepts invitation → codes are revealed in app.

**Critical Requirement**: ⚠️ **MUST NOT BREAK** existing code distribution, redemption, or statistics features.

---

## 🎯 Design Goals

### Primary Objectives
1. ✅ Token-based invitation system for farmer sponsorships
2. ✅ Support both registered and unregistered users (like dealer invitations)
3. ✅ Deep link integration for mobile app
4. ✅ Preserve ALL existing statistics and analytics
5. ✅ Backward compatibility with existing direct-send codes

### Non-Goals
- ❌ UI/Mobile implementation (backend only)
- ❌ Changes to existing dealer invitation system
- ❌ Changes to code redemption flow (RedeemSponsorshipCodeCommand)

---

## 🏗️ Architecture Overview

### Current Flow (Broken)
```
Sponsor → SendSponsorshipLinkCommand → SMS with actual code "AGRI-X3K9"
→ Farmer receives SMS → Manual copy (SMS listener removed) → Redeem code
```

### New Flow (Invitation-Based)
```
Sponsor → CreateFarmerInvitationCommand → SMS with token "FARMER-abc123xyz"
→ Farmer clicks deep link → Opens app → Accepts invitation
→ AcceptFarmerInvitationCommand → Codes assigned to farmer → Redeem codes
```

### Hybrid Approach (Backward Compatible)
- **Old codes** (already sent): Continue working with direct redemption
- **New invitations**: Token-based acceptance flow
- **Statistics**: Work for both old and new codes

---

## 📊 Database Schema Changes

### 1. New Table: FarmerInvitation

Similar to `DealerInvitation` but for farmer sponsorships.

```sql
CREATE TABLE "FarmerInvitation" (
    "Id" SERIAL PRIMARY KEY,

    -- Sponsor Information
    "SponsorId" INTEGER NOT NULL,

    -- Farmer Information
    "Phone" VARCHAR(20) NOT NULL,
    "FarmerName" VARCHAR(200),
    "Email" VARCHAR(200),

    -- Invitation Details
    "InvitationToken" VARCHAR(100) NOT NULL UNIQUE,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Accepted, Expired, Cancelled
    "InvitationType" VARCHAR(50) NOT NULL DEFAULT 'Invite', -- Invite (for consistency with dealer)

    -- Code Information
    "CodeCount" INTEGER NOT NULL,
    "PackageTier" VARCHAR(10), -- Optional: S, M, L, XL filter

    -- Acceptance Tracking
    "AcceptedByUserId" INTEGER,
    "AcceptedDate" TIMESTAMP,

    -- SMS Tracking
    "LinkSentDate" TIMESTAMP,
    "LinkSentVia" VARCHAR(50), -- SMS, WhatsApp, Email
    "LinkDelivered" BOOLEAN DEFAULT FALSE,

    -- Lifecycle
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "ExpiryDate" TIMESTAMP NOT NULL,
    "CancelledDate" TIMESTAMP,
    "Notes" TEXT,

    CONSTRAINT "FK_FarmerInvitation_Sponsor" FOREIGN KEY ("SponsorId") REFERENCES "Users"("UserId"),
    CONSTRAINT "FK_FarmerInvitation_AcceptedBy" FOREIGN KEY ("AcceptedByUserId") REFERENCES "Users"("UserId")
);

CREATE INDEX "IX_FarmerInvitation_SponsorId" ON "FarmerInvitation"("SponsorId");
CREATE INDEX "IX_FarmerInvitation_Token" ON "FarmerInvitation"("InvitationToken");
CREATE INDEX "IX_FarmerInvitation_Phone" ON "FarmerInvitation"("Phone");
CREATE INDEX "IX_FarmerInvitation_Status" ON "FarmerInvitation"("Status");
```

### 2. SponsorshipCode - New Optional Fields

**Add to existing table** (nullable for backward compatibility):

```sql
ALTER TABLE "SponsorshipCodes"
    ADD COLUMN "FarmerInvitationId" INTEGER NULL,
    ADD COLUMN "ReservedForFarmerInvitationId" INTEGER NULL,
    ADD COLUMN "ReservedForFarmerAt" TIMESTAMP NULL;

ALTER TABLE "SponsorshipCodes"
    ADD CONSTRAINT "FK_SponsorshipCode_FarmerInvitation"
    FOREIGN KEY ("FarmerInvitationId") REFERENCES "FarmerInvitation"("Id");

CREATE INDEX "IX_SponsorshipCode_FarmerInvitationId" ON "SponsorshipCodes"("FarmerInvitationId");
CREATE INDEX "IX_SponsorshipCode_ReservedForFarmerInvitationId" ON "SponsorshipCodes"("ReservedForFarmerInvitationId");
```

**Field Descriptions**:
- `FarmerInvitationId`: Set when invitation is accepted and codes are assigned to farmer
- `ReservedForFarmerInvitationId`: Set when invitation is created, codes reserved but not yet assigned
- `ReservedForFarmerAt`: Timestamp when codes were reserved for invitation

---

## 🔧 Implementation Components

### Phase 1: Database & Entities

#### 1.1 Entity Class
**File**: `Entities/Concrete/FarmerInvitation.cs`

```csharp
public class FarmerInvitation : IEntity
{
    public int Id { get; set; }

    // Sponsor Information
    public int SponsorId { get; set; }

    // Farmer Information
    public string Phone { get; set; }
    public string FarmerName { get; set; }
    public string Email { get; set; }

    // Invitation Details
    public string InvitationToken { get; set; }
    public string Status { get; set; } = "Pending";
    public string InvitationType { get; set; } = "Invite";

    // Code Information
    public int CodeCount { get; set; }
    public string PackageTier { get; set; } // Optional tier filter

    // Acceptance Tracking
    public int? AcceptedByUserId { get; set; }
    public DateTime? AcceptedDate { get; set; }

    // SMS Tracking
    public DateTime? LinkSentDate { get; set; }
    public string LinkSentVia { get; set; }
    public bool LinkDelivered { get; set; } = false;

    // Lifecycle
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ExpiryDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string Notes { get; set; }
}
```

#### 1.2 Repository Interface
**File**: `DataAccess/Abstract/IFarmerInvitationRepository.cs`

#### 1.3 Repository Implementation
**File**: `DataAccess/Concrete/EntityFramework/FarmerInvitationRepository.cs`

#### 1.4 EF Configuration
**File**: `DataAccess/Concrete/Configurations/FarmerInvitationEntityConfiguration.cs`

#### 1.5 Update ProjectDbContext
Add `DbSet<FarmerInvitation> FarmerInvitations { get; set; }`

---

### Phase 2: Core Commands

#### 2.1 CreateFarmerInvitationCommand

**File**: `Business/Handlers/Sponsorship/Commands/CreateFarmerInvitationCommand.cs`

**Purpose**: Replace SendSponsorshipLinkCommand for farmer code distribution

**Flow**:
1. Validate sponsor has available codes
2. Apply tier filter if specified
3. Create FarmerInvitation record with token
4. Reserve codes (set `ReservedForFarmerInvitationId`)
5. Generate deep link: `{baseUrl}/FARMER-{token}`
6. Send SMS with token (NOT actual codes)
7. Return invitation details

**Request DTO**:
```csharp
public class CreateFarmerInvitationCommand : IRequest<IDataResult<FarmerInvitationResponseDto>>
{
    public int SponsorId { get; set; } // From JWT
    public string Phone { get; set; } // Required
    public string FarmerName { get; set; } // Optional
    public string Email { get; set; } // Optional
    public int CodeCount { get; set; } // Required
    public string PackageTier { get; set; } // Optional: S, M, L, XL
    public string Channel { get; set; } = "SMS"; // SMS, WhatsApp, Email
    public string CustomMessage { get; set; } // Optional
}
```

**Response DTO**:
```csharp
public class FarmerInvitationResponseDto
{
    public int InvitationId { get; set; }
    public string InvitationToken { get; set; }
    public string InvitationLink { get; set; }
    public string Phone { get; set; }
    public string FarmerName { get; set; }
    public int CodeCount { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool SmsSent { get; set; }
    public string SmsDeliveryStatus { get; set; }
}
```

**Endpoint**: `POST /api/v1/sponsorship/farmer-invitations`

**Authorization**: `[SecuredOperation]` with `Sponsor` role

---

#### 2.2 AcceptFarmerInvitationCommand

**File**: `Business/Handlers/Sponsorship/Commands/AcceptFarmerInvitationCommand.cs`

**Purpose**: Farmer accepts invitation and receives codes

**Flow**:
1. Validate invitation token exists and is pending
2. Check expiry date
3. Verify phone number match (security check)
4. Get reserved codes for invitation
5. Assign codes to farmer (update SponsorshipCode fields)
6. Update invitation status to "Accepted"
7. **CRITICAL**: Populate same fields as SendSponsorshipLinkCommand for statistics compatibility
8. Return assigned codes

**Request DTO**:
```csharp
public class AcceptFarmerInvitationCommand : IRequest<IDataResult<FarmerInvitationAcceptResponseDto>>
{
    public string InvitationToken { get; set; } // From deep link
    public int CurrentUserId { get; set; } // From JWT (0 if not logged in)
    public string CurrentUserPhone { get; set; } // From JWT
}
```

**Response DTO**:
```csharp
public class FarmerInvitationAcceptResponseDto
{
    public int InvitationId { get; set; }
    public int FarmerId { get; set; }
    public int AssignedCodeCount { get; set; }
    public List<string> AssignedCodes { get; set; } // Actual codes revealed
    public DateTime AcceptedAt { get; set; }
    public string Message { get; set; }
}
```

**Endpoint**: `POST /api/v1/sponsorship/farmer-invitations/accept`

**Authorization**: `[AllowAnonymous]` (unregistered users can accept)

**Phone Matching Logic** (from DealerInvitation pattern):
```csharp
private string NormalizePhoneNumber(string phone)
{
    if (string.IsNullOrEmpty(phone)) return phone;

    var normalized = phone
        .Replace(" ", "")
        .Replace("-", "")
        .Replace("(", "")
        .Replace(")", "")
        .Replace("+", "");

    // Convert international format (90xxx) to Turkish format (0xxx)
    if (normalized.StartsWith("90") && normalized.Length == 12)
    {
        normalized = "0" + normalized.Substring(2);
    }

    return normalized;
}
```

---

### Phase 3: Query Endpoints

#### 3.1 GetFarmerInvitationsQuery

**File**: `Business/Handlers/Sponsorship/Queries/GetFarmerInvitationsQuery.cs`

**Purpose**: Sponsor views their farmer invitations

**Request**:
```csharp
public class GetFarmerInvitationsQuery : IRequest<IDataResult<FarmerInvitationsPaginatedDto>>
{
    public int SponsorId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string Status { get; set; } // Filter: Pending, Accepted, Expired, All
}
```

**Endpoint**: `GET /api/v1/sponsorship/farmer-invitations`

**Authorization**: `[SecuredOperation]` with `Sponsor` role

---

#### 3.2 GetFarmerInvitationByTokenQuery

**File**: `Business/Handlers/Sponsorship/Queries/GetFarmerInvitationByTokenQuery.cs`

**Purpose**: Mobile app gets invitation details before acceptance

**Request**:
```csharp
public class GetFarmerInvitationByTokenQuery : IRequest<IDataResult<FarmerInvitationDetailDto>>
{
    public string InvitationToken { get; set; }
}
```

**Response**:
```csharp
public class FarmerInvitationDetailDto
{
    public int InvitationId { get; set; }
    public string SponsorCompanyName { get; set; }
    public int CodeCount { get; set; }
    public string PackageTier { get; set; }
    public string Status { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
    public bool CanAccept { get; set; }
}
```

**Endpoint**: `GET /api/v1/sponsorship/farmer-invitations/{token}`

**Authorization**: `[AllowAnonymous]`

---

### Phase 4: Statistics Compatibility

#### 4.1 Field Population Strategy

**CRITICAL**: When invitation is accepted, populate ALL fields that statistics queries depend on:

```csharp
// In AcceptFarmerInvitationCommand
foreach (var code in assignedCodes)
{
    // Core assignment
    code.FarmerInvitationId = invitation.Id;
    code.ReservedForFarmerInvitationId = null; // Clear reservation
    code.ReservedForFarmerAt = null;

    // ⚡ CRITICAL: Populate fields for statistics compatibility
    code.RecipientPhone = invitation.Phone;
    code.RecipientName = invitation.FarmerName;
    code.LinkSentDate = invitation.LinkSentDate; // From invitation SMS send
    code.LinkSentVia = invitation.LinkSentVia;
    code.LinkDelivered = invitation.LinkDelivered;
    code.DistributionDate = DateTime.Now; // Acceptance time = distribution time
    code.DistributedTo = $"{invitation.FarmerName} ({invitation.Phone})";
    code.RedemptionLink = invitation deep link; // For tracking

    _codeRepository.Update(code);
}
```

**Why Critical**:
- `LinkSentDate.HasValue` → Statistics query considers code as "distributed"
- `DistributionDate.HasValue` → Package distribution statistics
- `LinkSentVia` → Channel statistics (SMS/WhatsApp/Email)
- `LinkDelivered` → Delivery rate statistics

#### 4.2 Analytics Cache Updates

**No changes needed** to existing analytics:
- `SponsorDealerAnalyticsCacheService.OnCodeDistributedAsync()` - NOT called for farmer invitations (dealer-specific)
- `RedeemSponsorshipCodeCommand` cache invalidation - Works as-is when codes are redeemed
- All statistics queries - Work automatically due to field population

---

### Phase 5: Configuration

#### 5.1 Configuration Settings

**File**: `appsettings.json`

```json
{
  "FarmerInvitation": {
    "DeepLinkBaseUrl": "https://ziraai.com/farmer-invite/",
    "TokenExpiryDays": 7,
    "SmsTemplate": "{sponsorName} size {codeCount} adet ZiraAI abonelik kodu gönderdi! Kodlarınızı almak için: {deepLink}\n\nUygulama: {playStoreLink}",
    "DefaultChannel": "SMS"
  }
}
```

**Staging** (`appsettings.Staging.json`):
```json
{
  "FarmerInvitation": {
    "DeepLinkBaseUrl": "https://ziraai-api-sit.up.railway.app/farmer-invite/"
  }
}
```

**Railway Environment Variables**:
```
FARMER_INVITATION__DEEPLINKBASEURL=https://ziraai.com/farmer-invite/
FARMER_INVITATION__TOKENEXPIRYDAYS=7
```

#### 5.2 Configuration Service

**File**: `Business/Services/FarmerInvitation/IFarmerInvitationConfigurationService.cs`

**Reference**: Similar to `IDealerInvitationConfigurationService`

---

## 🔄 Backward Compatibility Strategy

### Scenario 1: Old Codes (Already Sent via SendSponsorshipLinkCommand)
- ✅ Continue working normally
- ✅ Can be redeemed via RedeemSponsorshipCodeCommand
- ✅ Appear in statistics (have `LinkSentDate`, `DistributionDate`)
- ✅ Show in farmer inbox (have `RecipientPhone`, `LinkDelivered = true`)

### Scenario 2: New Codes (Sent via Invitation)
- ✅ Reserved when invitation created
- ✅ Assigned when invitation accepted
- ✅ Fields populated for statistics compatibility
- ✅ Can be redeemed via RedeemSponsorshipCodeCommand (same as old codes)
- ✅ Show in farmer inbox after acceptance

### Statistics Queries Compatibility
- ✅ `GetLinkStatisticsQuery` - Works (uses `LinkSentDate`, `LinkDelivered`)
- ✅ `GetPackageDistributionStatisticsQuery` - Works (uses `LinkSentDate`, `DistributionDate`)
- ✅ `GetSponsorTemporalAnalyticsQuery` - Works (uses `DistributionDate`, `UsedDate`)
- ✅ `GetFarmerSponsorshipInboxQuery` - Works (uses `RecipientPhone`, `LinkDelivered`)

### SendSponsorshipLinkCommand
**Decision**: Keep for backward compatibility, mark as deprecated

**Options**:
1. **Keep as-is** (deprecated but functional)
2. **Remove completely** (breaking change)
3. **Redirect to invitation** (convert old flow to new)

**Recommendation**: Option 1 - Keep but mark deprecated in API docs

---

## 📱 Deep Link Handling

### Mobile App Integration

**Deep Link Format**: `ziraai://farmer-invite/{token}`

**Universal Link** (Android/iOS): `https://ziraai.com/farmer-invite/{token}`

**Flow**:
1. User clicks SMS link
2. If app installed → Open app with token
3. If app not installed → Redirect to Play Store/App Store
4. App calls `GET /api/v1/sponsorship/farmer-invitations/{token}` to get details
5. App shows invitation acceptance screen
6. User clicks "Accept" → App calls `POST /api/v1/sponsorship/farmer-invitations/accept`
7. App receives assigned codes → User can now redeem them

**Unregistered Users**:
- Can accept invitation via deep link
- Phone number matching for security
- Codes assigned before user registration
- After registration, codes appear in their account

---

## 🔒 Security Considerations

### Authorization
- CreateFarmerInvitationCommand: Sponsor role required
- AcceptFarmerInvitationCommand: Phone number matching for security
- GetFarmerInvitationsQuery: Sponsor can only see their own invitations

### Phone Number Validation
- Normalize phone numbers (international vs Turkish format)
- Match invitation phone with current user phone
- Prevent invitation hijacking

### Token Security
- 32-character random token (same as dealer invitations)
- One-time use (status changes to "Accepted")
- Expiry check (default 7 days)
- Cannot accept expired invitations

---

## 🧪 Testing Strategy

### Unit Tests
- [ ] FarmerInvitation entity validation
- [ ] CreateFarmerInvitationCommand logic
- [ ] AcceptFarmerInvitationCommand logic
- [ ] Phone number normalization
- [ ] Token generation uniqueness

### Integration Tests
- [ ] End-to-end invitation flow
- [ ] Statistics queries with mixed old/new codes
- [ ] Expired invitation handling
- [ ] Phone mismatch security check
- [ ] Backward compatibility with existing codes

### Manual Testing (Staging)
- [ ] Create invitation via API
- [ ] Receive SMS with token
- [ ] Click deep link
- [ ] Accept invitation in app
- [ ] Verify codes assigned
- [ ] Redeem codes
- [ ] Check statistics accuracy

---

## 📊 Migration Strategy

### Database Migration Script

**File**: `claudedocs/AdminOperations/001_farmer_invitation_system.sql`

**Steps**:
1. Create FarmerInvitation table
2. Add indexes
3. Alter SponsorshipCodes table (add new columns)
4. Add foreign keys

**Rollback Script**: Also provided

### Data Migration
- ❌ No data migration needed (new feature)
- ✅ Existing codes work as-is
- ✅ New invitations use new tables

---

## 📈 Success Metrics

### Technical Metrics
- [ ] All existing unit tests pass
- [ ] Build succeeds without errors
- [ ] No performance degradation in statistics queries
- [ ] Backward compatibility verified

### Business Metrics
- [ ] Farmer invitation acceptance rate >60%
- [ ] SMS delivery success rate >95%
- [ ] Code redemption rate improvement
- [ ] Mobile app deep link success rate >80%

---

## 🚀 Deployment Plan

### Phase 1: Database (Manual SQL)
1. Run migration script on staging database
2. Verify tables created
3. Test foreign keys

### Phase 2: Backend API (Railway Auto-Deploy)
1. Push to `feature/remove-sms-listener-logic` branch
2. Railway auto-deploys to staging
3. Verify endpoints in Swagger
4. Test with Postman

### Phase 3: Configuration
1. Add Railway environment variables
2. Verify configuration service reads correctly
3. Test SMS sending with token

### Phase 4: Mobile Integration
1. Provide API documentation to mobile team
2. Mobile team implements deep link handling
3. End-to-end testing in staging

### Phase 5: Production (After Staging Verification)
1. Merge to master
2. Run migration script on production database
3. Deploy backend to production
4. Mobile app update released
5. Monitor metrics

---

## 📝 API Documentation Files

Following Rule #9, create these documentation files after each endpoint:

1. `FARMER_INVITATION_API_CREATE.md` - CreateFarmerInvitationCommand
2. `FARMER_INVITATION_API_ACCEPT.md` - AcceptFarmerInvitationCommand
3. `FARMER_INVITATION_API_LIST.md` - GetFarmerInvitationsQuery
4. `FARMER_INVITATION_API_DETAIL.md` - GetFarmerInvitationByTokenQuery

Each file includes:
- Endpoint URL
- HTTP method
- Request payload example
- Response payload example (success + error)
- Authentication requirements
- Use cases
- Integration notes for mobile/frontend teams

---

## ⚠️ Risks & Mitigation

### Risk 1: Breaking Existing Statistics
**Mitigation**: Field population strategy ensures statistics queries continue working

### Risk 2: SMS Delivery Failures
**Mitigation**: Track delivery status, provide manual link sharing option

### Risk 3: Token Collision
**Mitigation**: Use GUID-based token generation (extremely low collision probability)

### Risk 4: Phone Number Format Issues
**Mitigation**: Use proven normalization logic from DealerInvitation

### Risk 5: Dependency Errors
**Mitigation**: Build after each phase (Rule #3)

---

## 📚 References

### Existing Code Patterns
- `DealerInvitation` entity and flow
- `InviteDealerViaSmsCommand`
- `AcceptDealerInvitationCommand`
- `SendSponsorshipLinkCommand` (current broken approach)

### Configuration References
- Storage service configuration pattern
- Dealer invitation configuration service

### Authorization References
- Sponsor analytics endpoints (recent correct pattern)
- `SECUREDOPERATION_GUIDE.md`
- `operation_claims.csv`

---

## ✅ Definition of Done

### Phase Completion Criteria
- [ ] Code compiles without errors (`dotnet build`)
- [ ] Existing features still work (manual verification)
- [ ] API documentation created
- [ ] Development plan updated
- [ ] Pushed to feature branch
- [ ] Verified in staging environment

### Project Completion Criteria
- [ ] All endpoints implemented
- [ ] All tests passing
- [ ] Statistics backward compatible
- [ ] API documentation complete
- [ ] Mobile integration guide provided
- [ ] Staging testing successful
- [ ] Production deployment successful

---

**Document Version**: 1.0
**Last Updated**: 2026-01-02
**Next Review**: After Phase 1 completion
