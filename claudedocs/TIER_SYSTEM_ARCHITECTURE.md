# ZiraAI Tier System Architecture

**Date:** 2025-10-20  
**Critical:** READ THIS BEFORE IMPLEMENTING ANY TIER-RELATED FEATURES

---

## 🎯 Core Principle

> **USERS DON'T HAVE TIERS - ANALYSES HAVE TIERS**

This is the most critical concept in the ZiraAI tier system. Misunderstanding this leads to incorrect validation logic and broken features.

---

## 📚 Table of Contents

1. [System Overview](#system-overview)
2. [Tier Hierarchy](#tier-hierarchy)
3. [How Tiers Are Assigned](#how-tiers-are-assigned)
4. [Common Misconceptions](#common-misconceptions)
5. [Implementation Patterns](#implementation-patterns)
6. [Code Examples](#code-examples)
7. [Validation Rules](#validation-rules)
8. [Testing Guidelines](#testing-guidelines)

---

## 🏗️ System Overview

### Entities Involved

```
User (Farmer/Sponsor)
  └─> Purchases SponsorshipPackage
       └─> Creates UserSubscription (has SubscriptionTier)
            └─> Used for PlantAnalysis
                 └─> Analysis has tier from UserSubscription
```

### Key Tables

#### 1. SubscriptionTier
```sql
CREATE TABLE SubscriptionTier (
    Id INT PRIMARY KEY,
    TierName VARCHAR(50),  -- 'Trial', 'S', 'M', 'L', 'XL'
    DataAccessPercentage INT,  -- 0, 30, 60, 100
    ...
);
```

**Tier Mapping:**
- `1` = Trial (0% access)
- `2` = S (30% access)
- `3` = M (30% access)
- `4` = L (60% access)
- `5` = XL (100% access)

#### 2. UserSubscription
```sql
CREATE TABLE UserSubscription (
    Id INT PRIMARY KEY,
    UserId INT,  -- Sponsor who purchased
    SubscriptionTierId INT,  -- FK to SubscriptionTier
    RemainingRequests INT,
    ...
);
```

**Purpose:**
- Represents a purchased sponsorship package
- Contains the tier level (S, M, L, XL)
- Has request quota
- Can be assigned to analyses

#### 3. PlantAnalysis
```sql
CREATE TABLE PlantAnalysis (
    Id INT PRIMARY KEY,
    UserId INT,  -- Farmer who created analysis
    SponsorUserId INT,  -- Sponsor who sponsored (nullable)
    ActiveSponsorshipId INT,  -- FK to UserSubscription (THE TIER SOURCE)
    ...
);
```

**Critical Fields:**
- `UserId`: The farmer who owns the analysis
- `SponsorUserId`: The sponsor who paid for it
- `ActiveSponsorshipId`: **THIS DETERMINES THE TIER**

---

## 📊 Tier Hierarchy

### Tier Levels (Ascending Order)

```
None (0%)  →  Trial (0%)  →  S (30%)  →  M (30%)  →  L (60%)  →  XL (100%)
```

### Access Percentages

| Tier | Data Access | Messaging | Voice Messages | Attachments | Smart Links |
|------|-------------|-----------|----------------|-------------|-------------|
| None | 0% | ❌ | ❌ | ❌ | ❌ |
| Trial | 0% | ❌ | ❌ | ❌ | ❌ |
| S | 30% | ❌ | ❌ | ❌ | ❌ |
| M | 30% | ✅ | ❌ | ✅ | ❌ |
| L | 60% | ✅ | ✅ | ✅ | ❌ |
| XL | 100% | ✅ | ✅ | ✅ | ✅ |

### Feature Requirements

```csharp
public static class TierFeatures
{
    // Messaging Features
    public const string BASIC_MESSAGING = "M";      // Text messages
    public const string IMAGE_ATTACHMENTS = "M";    // Image files
    public const string VIDEO_ATTACHMENTS = "L";    // Video files
    public const string FILE_ATTACHMENTS = "M";     // Documents
    public const string VOICE_MESSAGES = "L";       // Voice recordings
    public const string SMART_LINKS = "XL";         // Advanced links
    
    // Data Access
    public const string BASIC_INFO = "S";           // 30% data
    public const string DETAILED_INFO = "L";        // 60% data
    public const string FULL_INFO = "XL";           // 100% data
}
```

---

## 🔄 How Tiers Are Assigned

### Scenario 1: Farmer Creates Analysis

```
Farmer (userId: 100)
  └─> Creates PlantAnalysis
       ├─> UserId = 100 (farmer owns it)
       ├─> SponsorUserId = NULL (not sponsored yet)
       └─> ActiveSponsorshipId = NULL (no tier yet)
```

**Result:** Analysis has NO tier (Trial tier by default for personal use)

### Scenario 2: Sponsor Purchases Package

```
Sponsor (userId: 200)
  └─> Purchases L Tier Package
       └─> Creates UserSubscription
            ├─> Id = 500
            ├─> UserId = 200 (sponsor)
            ├─> SubscriptionTierId = 4 (L tier)
            └─> RemainingRequests = 50
```

**Result:** Sponsor has L tier package available

### Scenario 3: Sponsor Sponsors an Analysis

```
PlantAnalysis (id: 300)
  ├─> UserId = 100 (farmer owns it)
  ├─> SponsorUserId = 200 (sponsor paid)
  └─> ActiveSponsorshipId = 500 (UserSubscription with L tier)
```

**Result:** Analysis is now L tier (from UserSubscription #500)

### Scenario 4: Farmer Uses Sponsored Analysis Features

```
Farmer (userId: 100, has M tier package for own analyses)
  └─> Sends voice message on Analysis #300
       └─> Analysis #300 has ActiveSponsorshipId = 500 (L tier)
            └─> Voice message ALLOWED ✅
                (because ANALYSIS is L tier, not because farmer has M tier)
```

**Critical:** Farmer can use L tier features on L tier analysis even if their personal package is M tier.

---

## ❌ Common Misconceptions

### Misconception #1: "User has a tier"

```csharp
// ❌ WRONG - Users don't have tiers
var userTier = await GetUserTierAsync(userId);
if (userTier == "L") { /* allow voice message */ }

// ✅ CORRECT - Analyses have tiers
var analysisTier = await GetAnalysisTierAsync(plantAnalysisId);
if (analysisTier == "L") { /* allow voice message */ }
```

**Why Wrong:**
- A farmer might have M tier package for their own analyses
- But they're messaging on a sponsor's L tier analysis
- Feature should be available because ANALYSIS is L tier

### Misconception #2: "Check user's subscription"

```csharp
// ❌ WRONG - Checking user's subscription
var userSubscription = await _userSubscriptionRepository.GetAsync(
    us => us.UserId == userId && us.IsActive);
var tier = userSubscription.SubscriptionTierId;

// ✅ CORRECT - Check analysis's subscription
var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == plantAnalysisId);
var sponsorship = await _userSubscriptionRepository.GetAsync(
    us => us.Id == analysis.ActiveSponsorshipId);
var tier = sponsorship.SubscriptionTierId;
```

**Why Wrong:**
- User's subscription is for their OWN analyses
- Current operation is on a DIFFERENT analysis (sponsored by someone else)
- Must check the analysis's sponsorship, not user's

### Misconception #3: "Sponsor's tier = All their analyses"

```csharp
// ❌ WRONG - Assuming all sponsor's analyses have same tier
var sponsor = await GetSponsorAsync(sponsorId);
var sponsorTier = sponsor.CurrentTier; // Doesn't exist!

// ✅ CORRECT - Each analysis can have different tier
foreach (var analysis in sponsorAnalyses)
{
    var tier = await GetAnalysisTierAsync(analysis.Id);
    // Each analysis might be S, M, L, or XL
}
```

**Why Wrong:**
- Sponsor can purchase multiple packages
- Analysis #1 might use S tier package
- Analysis #2 might use XL tier package
- Must check EACH analysis individually

### Misconception #4: "Tier is static"

```csharp
// ❌ WRONG - Caching tier at analysis creation
analysis.TierSnapshot = "L"; // No such field!

// ✅ CORRECT - Always look up current tier
var tier = await GetAnalysisTierAsync(analysis.Id);
// Uses ActiveSponsorshipId to get current tier
```

**Why Wrong:**
- Sponsor might upgrade/downgrade package
- ActiveSponsorshipId might change
- Always fetch tier dynamically

---

## 🛠️ Implementation Patterns

### Pattern 1: Feature Validation (CORRECT)

```csharp
public async Task<IResult> ValidateFeatureAccessAsync(
    string featureName,
    int plantAnalysisId,  // ✅ Takes analysisId, not userId
    long? fileSize = null,
    int? duration = null)
{
    // 1. Get the ANALYSIS tier
    var analysisTier = await GetAnalysisTierAsync(plantAnalysisId);
    
    // 2. Get feature requirements
    var feature = await _featureRepository.GetAsync(f => f.FeatureName == featureName);
    
    // 3. Check if analysis tier meets requirements
    if (!CheckTierAccess(feature.RequiredTier, analysisTier))
    {
        return new ErrorResult(
            $"{featureName} requires {feature.RequiredTier} tier. " +
            $"This analysis tier: {analysisTier}");
    }
    
    return new SuccessResult("Feature access granted");
}
```

### Pattern 2: Get Analysis Tier (CORRECT)

```csharp
private async Task<string> GetAnalysisTierAsync(int plantAnalysisId)
{
    // Step 1: Get the analysis
    var analysis = await _plantAnalysisRepository.GetAsync(
        a => a.Id == plantAnalysisId);
    
    if (analysis == null)
        return "None";
    
    // Step 2: Check if analysis has sponsorship
    if (!analysis.ActiveSponsorshipId.HasValue || 
        analysis.ActiveSponsorshipId.Value == 0)
        return "None";
    
    // Step 3: Get the sponsorship (UserSubscription)
    var sponsorship = await _userSubscriptionRepository.GetAsync(
        us => us.Id == analysis.ActiveSponsorshipId.Value);
    
    if (sponsorship == null || sponsorship.SubscriptionTierId == 0)
        return "None";
    
    // Step 4: Get the tier
    var tier = await _subscriptionTierRepository.GetAsync(
        t => t.Id == sponsorship.SubscriptionTierId);
    
    if (tier == null)
        return "None";
    
    // Step 5: Return tier name
    return tier.TierName; // "S", "M", "L", "XL"
}
```

### Pattern 3: Tier Comparison (CORRECT)

```csharp
private bool CheckTierAccess(string requiredTier, string analysisTier)
{
    var tierHierarchy = new Dictionary<string, int>
    {
        { "None", 0 },
        { "Trial", 1 },
        { "S", 2 },
        { "M", 3 },
        { "L", 4 },
        { "XL", 5 }
    };
    
    var requiredLevel = tierHierarchy.GetValueOrDefault(requiredTier, 0);
    var analysisLevel = tierHierarchy.GetValueOrDefault(analysisTier, 0);
    
    return analysisLevel >= requiredLevel;
}
```

---

## 💻 Code Examples

### Example 1: Voice Message Validation

```csharp
// ✅ CORRECT IMPLEMENTATION
public class SendVoiceMessageCommand : IRequest<IDataResult<AnalysisMessageDto>>
{
    public int FromUserId { get; set; }        // Who is sending
    public int ToUserId { get; set; }          // Who is receiving
    public int PlantAnalysisId { get; set; }   // Which analysis (TIER SOURCE)
    public IFormFile VoiceFile { get; set; }
    public int Duration { get; set; }
}

public class SendVoiceMessageCommandHandler : IRequestHandler<...>
{
    public async Task<IDataResult<AnalysisMessageDto>> Handle(...)
    {
        // ✅ CORRECT: Validate based on ANALYSIS tier
        var featureValidation = await _featureService.ValidateFeatureAccessAsync(
            "VoiceMessages",
            request.PlantAnalysisId,  // ✅ Pass analysisId, not userId
            request.VoiceFile?.Length,
            request.Duration);
        
        if (!featureValidation.Success)
            return new ErrorDataResult<AnalysisMessageDto>(
                featureValidation.Message);
        
        // Continue with upload...
    }
}
```

### Example 2: Attachment Validation

```csharp
// ✅ CORRECT IMPLEMENTATION
public interface IAttachmentValidationService
{
    // ✅ Takes plantAnalysisId, not userId
    Task<IResult> ValidateAttachmentAsync(
        IFormFile file,
        int plantAnalysisId,  // ✅ Analysis tier source
        string attachmentType);
    
    Task<IDataResult<List<string>>> ValidateAttachmentsAsync(
        List<IFormFile> files,
        int plantAnalysisId);  // ✅ Analysis tier source
}

public class AttachmentValidationService : IAttachmentValidationService
{
    public async Task<IResult> ValidateAttachmentAsync(
        IFormFile file,
        int plantAnalysisId,
        string attachmentType)
    {
        // ✅ CORRECT: Check ANALYSIS tier
        var validationResult = await _featureService.ValidateFeatureAccessAsync(
            attachmentType,
            plantAnalysisId,  // ✅ Pass analysisId
            file.Length);
        
        return validationResult;
    }
}
```

### Example 3: Messaging Permission Check

```csharp
// ✅ CORRECT IMPLEMENTATION
public class CanSendMessageForAnalysisAsync
{
    public async Task<(bool canSend, string errorMessage)> Execute(
        int sponsorId,
        int farmerId,
        int plantAnalysisId)
    {
        // 1. Check if analysis exists
        var analysis = await _plantAnalysisRepository.GetAsync(
            a => a.Id == plantAnalysisId);
        
        if (analysis == null)
            return (false, "Analysis not found");
        
        // 2. Check if sponsor owns this analysis
        if (analysis.SponsorUserId != sponsorId)
            return (false, "You don't sponsor this analysis");
        
        // 3. ✅ CORRECT: Check if analysis tier allows messaging
        var analysisTier = await GetAnalysisTierAsync(plantAnalysisId);
        var tierLevel = GetTierLevel(analysisTier);
        
        if (tierLevel < 3) // M tier required (level 3)
            return (false, $"Messaging requires M tier. This analysis tier: {analysisTier}");
        
        return (true, string.Empty);
    }
}
```

---

## ✅ Validation Rules

### Rule 1: Always Use PlantAnalysisId for Tier Checks

```csharp
// ❌ WRONG
public async Task<IResult> ValidateFeature(string feature, int userId) { }

// ✅ CORRECT
public async Task<IResult> ValidateFeature(string feature, int plantAnalysisId) { }
```

### Rule 2: Never Cache Tier at User Level

```csharp
// ❌ WRONG
public class User
{
    public string CurrentTier { get; set; }  // NO!
}

// ✅ CORRECT - Always fetch dynamically
var tier = await GetAnalysisTierAsync(plantAnalysisId);
```

### Rule 3: Tier Source is Always ActiveSponsorshipId

```csharp
// ✅ CORRECT CHAIN
PlantAnalysis.ActiveSponsorshipId
  → UserSubscription.Id
    → UserSubscription.SubscriptionTierId
      → SubscriptionTier.TierName
```

### Rule 4: Handle Missing Tiers Gracefully

```csharp
// ✅ CORRECT - Default to "None"
var tier = await GetAnalysisTierAsync(plantAnalysisId);
if (tier == "None")
{
    return new ErrorResult("This analysis is not sponsored");
}
```

### Rule 5: Tier Comparison Must Be Hierarchical

```csharp
// ❌ WRONG - String comparison
if (tier == "L") { /* allow */ }

// ✅ CORRECT - Hierarchical comparison
if (GetTierLevel(tier) >= GetTierLevel("L")) { /* allow */ }
```

---

## 🧪 Testing Guidelines

### Test Scenario 1: Different User Tier vs Analysis Tier

```csharp
[Test]
public async Task VoiceMessage_ShouldWork_WhenAnalysisIsL_EvenIfUserIsM()
{
    // Arrange
    var farmer = CreateFarmer(userId: 100);
    var farmerPackage = CreatePackage(tier: "M"); // Farmer has M
    
    var sponsor = CreateSponsor(userId: 200);
    var sponsorPackage = CreatePackage(tier: "L"); // Sponsor has L
    
    var analysis = CreateAnalysis(
        farmerId: 100,
        sponsorId: 200,
        activeSponsorshipId: sponsorPackage.Id); // Analysis uses L tier
    
    // Act
    var result = await SendVoiceMessage(
        fromUserId: 100,  // Farmer (has M)
        toUserId: 200,    // Sponsor
        plantAnalysisId: analysis.Id); // Analysis (is L)
    
    // Assert
    Assert.That(result.Success, Is.True, 
        "Voice message should work because ANALYSIS is L tier");
}
```

### Test Scenario 2: No Sponsorship

```csharp
[Test]
public async Task VoiceMessage_ShouldFail_WhenAnalysisNotSponsored()
{
    // Arrange
    var farmer = CreateFarmer(userId: 100);
    var analysis = CreateAnalysis(
        farmerId: 100,
        sponsorId: null,  // Not sponsored
        activeSponsorshipId: null); // No tier
    
    // Act
    var result = await SendVoiceMessage(
        fromUserId: 100,
        toUserId: 200,
        plantAnalysisId: analysis.Id);
    
    // Assert
    Assert.That(result.Success, Is.False);
    Assert.That(result.Message, Does.Contain("not sponsored"));
}
```

### Test Scenario 3: Multiple Analyses, Different Tiers

```csharp
[Test]
public async Task Sponsor_CanHave_MultipleAnalyses_WithDifferentTiers()
{
    // Arrange
    var sponsor = CreateSponsor(userId: 200);
    var packageS = CreatePackage(tier: "S");
    var packageXL = CreatePackage(tier: "XL");
    
    var analysisS = CreateAnalysis(sponsorId: 200, activeSponsorshipId: packageS.Id);
    var analysisXL = CreateAnalysis(sponsorId: 200, activeSponsorshipId: packageXL.Id);
    
    // Act
    var tierS = await GetAnalysisTier(analysisS.Id);
    var tierXL = await GetAnalysisTier(analysisXL.Id);
    
    // Assert
    Assert.That(tierS, Is.EqualTo("S"));
    Assert.That(tierXL, Is.EqualTo("XL"));
}
```

### Test Scenario 4: Tier Upgrade

```csharp
[Test]
public async Task AnalysisTier_ShouldUpdate_WhenSponsorshipChanges()
{
    // Arrange
    var analysis = CreateAnalysis(activeSponsorshipId: packageM.Id);
    var initialTier = await GetAnalysisTier(analysis.Id);
    Assert.That(initialTier, Is.EqualTo("M"));
    
    // Act - Upgrade to L
    analysis.ActiveSponsorshipId = packageL.Id;
    await _repository.SaveChangesAsync();
    
    var upgradedTier = await GetAnalysisTier(analysis.Id);
    
    // Assert
    Assert.That(upgradedTier, Is.EqualTo("L"));
}
```

---

## 🚨 Critical Errors to Avoid

### Error #1: Passing userId to Tier Validation

```csharp
// ❌ CRITICAL ERROR
var result = await _featureService.ValidateFeatureAccessAsync(
    "VoiceMessages",
    request.FromUserId);  // ❌ WRONG - this is user ID, not analysis ID

// ✅ CORRECT
var result = await _featureService.ValidateFeatureAccessAsync(
    "VoiceMessages",
    request.PlantAnalysisId);  // ✅ CORRECT - analysis determines tier
```

**Impact:** Voice messages fail for farmers using L tier analyses if they personally have M tier packages.

### Error #2: Checking User's Subscription Instead of Analysis

```csharp
// ❌ CRITICAL ERROR
var userSubscription = await _userSubscriptionRepository.GetAsync(
    us => us.UserId == userId);  // ❌ WRONG - user's subscription
var tier = userSubscription.SubscriptionTierId;

// ✅ CORRECT
var analysis = await _plantAnalysisRepository.GetAsync(
    a => a.Id == plantAnalysisId);
var sponsorship = await _userSubscriptionRepository.GetAsync(
    us => us.Id == analysis.ActiveSponsorshipId);  // ✅ CORRECT - analysis's subscription
var tier = sponsorship.SubscriptionTierId;
```

**Impact:** Features locked when they should be available based on analysis tier.

### Error #3: Assuming Tier Doesn't Change

```csharp
// ❌ CRITICAL ERROR
public class PlantAnalysis
{
    public string CachedTier { get; set; }  // ❌ WRONG - tier can change
}

// ✅ CORRECT - Always fetch dynamically
var tier = await GetAnalysisTierAsync(plantAnalysisId);
```

**Impact:** Stale tier data, features not updating when sponsorship changes.

---

## 📋 Checklist for New Features

When implementing ANY feature that depends on tier:

- [ ] Does method take `plantAnalysisId` instead of `userId`?
- [ ] Does it call `GetAnalysisTierAsync(plantAnalysisId)`?
- [ ] Does it use `ActiveSponsorshipId` to find tier?
- [ ] Does it handle `null` ActiveSponsorshipId (returns "None")?
- [ ] Does tier comparison use hierarchical check (not string equality)?
- [ ] Are error messages clear about ANALYSIS tier vs user tier?
- [ ] Are tests written for different user tier vs analysis tier?
- [ ] Is documentation updated to explain tier source?

---

## 🎓 Summary

### Key Takeaways

1. **Analyses have tiers, users don't**
   - Each analysis gets tier from its `ActiveSponsorshipId`
   - User's package is irrelevant for current analysis features

2. **Always use PlantAnalysisId for validation**
   - Feature validation needs `plantAnalysisId`
   - Never use `userId` to determine tier

3. **Tier source chain**
   - `PlantAnalysis.ActiveSponsorshipId`
   - → `UserSubscription.SubscriptionTierId`
   - → `SubscriptionTier.TierName`

4. **Dynamic tier lookup**
   - Never cache tier at entity level
   - Always fetch tier when needed
   - Tier can change when sponsorship changes

5. **Test comprehensively**
   - User has M, analysis is L → Features should work
   - User has L, analysis is M → Features should NOT work
   - No sponsorship → Features should fail gracefully

---

## 📞 Questions?

If you're implementing a tier-related feature and unsure:

1. **Ask:** "Does this feature depend on user tier or analysis tier?"
   - Answer: **Always analysis tier**

2. **Ask:** "Should I pass userId or plantAnalysisId?"
   - Answer: **Always plantAnalysisId**

3. **Ask:** "Where does tier come from?"
   - Answer: **ActiveSponsorshipId → UserSubscription → SubscriptionTier**

---

**Last Updated:** 2025-10-20  
**Related Commit:** 4c4da6d - fix: Use analysis tier instead of user tier for messaging feature validation
