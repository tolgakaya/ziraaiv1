# CRITICAL: Breaking Changes Analysis - Sponsorship Analyses Endpoint

**Date:** 2025-10-26
**Severity:** 🔴 CRITICAL - Mobile app breaking
**Endpoint:** `GET /api/v1/sponsorship/analyses`

---

## Missing Fields Comparison

### Old Response (Working) → New Response (BROKEN)

**Missing fields per analysis item:**

| Field Name | Type | Critical? | Mobile Usage |
|------------|------|-----------|--------------|
| `overallHealthScore` | int | ✅ YES | Display health rating |
| `plantSpecies` | string | ✅ YES | Show plant identification |
| `plantVariety` | string | ✅ YES | Show variety info |
| `growthStage` | string | ✅ YES | Display growth stage |
| `imageUrl` | string | ✅ YES | Show analysis image |
| `vigorScore` | int | ✅ YES | Display vigor rating |
| `healthSeverity` | string | ✅ YES | Show severity level |
| `primaryConcern` | string | ✅ YES | Display main issue |
| `location` | string | ⚠️ MEDIUM | Show location if available |

**Field Status:**
- **tierName**: Changed from actual tier (e.g., "L") to "Unknown" ❌
- **accessPercentage**: Changed from correct value (60) to 0 ❌
- **canMessage**: Changed from `true` to `false` ❌

---

## Impact Analysis

### Critical Issues

1. **No Analysis Details**: Mobile app cannot show:
   - Plant health score
   - Plant identification
   - Growth stage
   - Health concerns
   - Analysis images

2. **Broken Tier Features**: All tier-based features showing wrong:
   - Tier name showing "Unknown" instead of "L"
   - Access percentage showing 0 instead of 60
   - Messaging permission showing false instead of true

3. **User Experience Destroyed**:
   - List shows only dates and status
   - No actionable information visible
   - Cannot view analysis details
   - Messaging appears disabled

---

## Root Cause Investigation Needed

**Priority 1: Find the handler/DTO**
- Handler: `GetSponsoredAnalysesListQuery.cs`
- DTO: Need to identify response DTO

**Priority 2: Recent Changes**
- Dealer implementation changes to this query
- OR query modification for hybrid roles
- Possible DTO mapping changes

**Priority 3: Related Endpoints**
Check if these are also broken:
- `GET /api/v1/sponsorship/analyses/{id}` - Single analysis detail
- `GET /api/v1/PlantAnalyses/list` - Farmer's analysis list
- Other sponsorship endpoints

---

## Expected Fix

### Required DTO Structure

```csharp
public class SponsoredAnalysisListItemDto
{
    // Identification
    public int AnalysisId { get; set; }
    public DateTime AnalysisDate { get; set; }
    public string AnalysisStatus { get; set; }

    // Plant Details (MISSING - MUST RESTORE)
    public string CropType { get; set; }
    public int OverallHealthScore { get; set; }      // ✅ MUST ADD
    public string PlantSpecies { get; set; }         // ✅ MUST ADD
    public string PlantVariety { get; set; }         // ✅ MUST ADD
    public string GrowthStage { get; set; }          // ✅ MUST ADD
    public string ImageUrl { get; set; }             // ✅ MUST ADD
    public int VigorScore { get; set; }              // ✅ MUST ADD
    public string HealthSeverity { get; set; }       // ✅ MUST ADD
    public string PrimaryConcern { get; set; }       // ✅ MUST ADD
    public string Location { get; set; }             // ✅ MUST ADD

    // Tier & Permissions
    public string TierName { get; set; }
    public int AccessPercentage { get; set; }
    public bool CanMessage { get; set; }
    public bool CanViewLogo { get; set; }

    // Sponsor Info
    public SponsorInfoDto SponsorInfo { get; set; }

    // Messaging Status
    public MessagingStatusDto MessagingStatus { get; set; }

    // Legacy fields (for backward compatibility)
    public int UnreadMessageCount { get; set; }
    public int TotalMessageCount { get; set; }
    public DateTime? LastMessageDate { get; set; }
    public string LastMessagePreview { get; set; }
    public string LastMessageSenderRole { get; set; }
    public bool HasUnreadFromFarmer { get; set; }
    public string ConversationStatus { get; set; }
}
```

---

## Action Plan

1. ✅ Document the issue (this file)
2. ⏳ Find the handler and DTO
3. ⏳ Restore all missing fields
4. ⏳ Fix tier name, access percentage, canMessage
5. ⏳ Test on staging
6. ⏳ Search for similar issues in other endpoints
7. ⏳ Commit with clear message
8. ⏳ Create safety checklist for future changes

---

## Lessons Learned

### Rule Violations

1. **❌ Response Structure Changed Without Approval**
   - MUST get approval before changing any API response
   - Mobile app depends on exact field structure

2. **❌ No Backward Compatibility Check**
   - Should have verified old response structure
   - Should have tested mobile integration

3. **❌ Insufficient Testing**
   - E2E test only checked for existence of fields
   - Did not verify field VALUES
   - Did not check all fields present

### New Safety Rules

1. **Before ANY response change:**
   - Document current response structure
   - Get explicit approval
   - Create compatibility test
   - Test with actual mobile app

2. **Response change checklist:**
   - [ ] Old response documented
   - [ ] Change reason documented
   - [ ] Mobile team notified
   - [ ] Backward compatibility verified
   - [ ] Deprecation plan if needed
   - [ ] Approval received

---

**Status:** CRITICAL FIX IN PROGRESS
**Priority:** P0 - Fix immediately
**Estimated Time:** 1-2 hours
