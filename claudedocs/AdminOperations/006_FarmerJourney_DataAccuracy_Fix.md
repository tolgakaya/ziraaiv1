# Farmer Journey Data Accuracy Fix

**Date**: 2025-11-12
**Commit**: 9e6c658
**Feature**: Sponsor Advanced Analytics - Farmer Journey
**Branch**: feature/sponsor-advanced-analytics

---

## Problem Report

User reported missing/incorrect data in Farmer Journey endpoint response ([jurney.json](./jurney.json)):

### ❌ Issues Found:

1. **`commonIssues` returns empty array** despite 13 analyses with disease data
2. **`messageResponseRate` = 94.1%** - Suspiciously high, using wrong metric
3. **`averageMessageResponseTimeHours` = 0** - Not calculated at all

---

## Root Cause Analysis

### Issue 1: Common Issues Empty Array

**Problem**:
- `Diseases` field in PlantAnalysis is a JSON array: `["Disease1", "Disease2"]`
- Original code treated entire JSON string as single disease
- GroupBy couldn't find common patterns → empty result

**Original Code**:
```csharp
var commonIssues = analyses
    .Where(a => !string.IsNullOrEmpty(a.Diseases))
    .GroupBy(a => a.Diseases)  // ❌ Groups entire JSON strings
    .OrderByDescending(g => g.Count())
    .Take(5)
    .Select(g => g.Key)
    .ToList();
```

**Fixed Code**:
```csharp
var commonIssues = analyses
    .Where(a => !string.IsNullOrEmpty(a.Diseases))
    .SelectMany(a =>
    {
        try
        {
            // Parse JSON array to extract individual diseases
            var diseases = System.Text.Json.JsonSerializer.Deserialize<List<string>>(a.Diseases);
            return diseases ?? new List<string>();
        }
        catch
        {
            // Fallback: treat as single disease if not JSON
            return new List<string> { a.Diseases };
        }
    })
    .Where(d => !string.IsNullOrWhiteSpace(d))
    .GroupBy(d => d)  // ✅ Groups individual disease names
    .OrderByDescending(g => g.Count())
    .Take(5)
    .Select(g => g.Key)
    .ToList();
```

### Issue 2: Message Response Rate Incorrect

**Problem**:
- Used `IsRead` field which means "sponsor message was read by farmer"
- Does NOT mean "farmer responded to sponsor message"
- Result: 94%+ false positive rate

**Original Code**:
```csharp
// ❌ Counts messages marked as "read", not actual responses
var totalMessages = messages.Count;
var respondedMessages = messages.Count(m => m.IsRead == true);
var messageResponseRate = totalMessages > 0
    ? (decimal)respondedMessages / totalMessages * 100
    : 0;
```

**Fixed Code**:
```csharp
// ✅ Counts actual farmer responses to sponsor messages
var sponsorMessages = messages.Where(m => m.ToUserId == farmerId).ToList();
var farmerResponses = messages.Where(m => m.FromUserId == farmerId).ToList();
var totalSponsorMessages = sponsorMessages.Count;

// Check if farmer responded via ParentMessageId or same analysis with later timestamp
var sponsorMessagesWithResponses = sponsorMessages
    .Where(sm => farmerResponses.Any(fr =>
        fr.ParentMessageId == sm.Id ||  // Thread-based response
        (fr.PlantAnalysisId == sm.PlantAnalysisId && fr.CreatedDate > sm.CreatedDate))) // Same analysis response
    .Count();

var messageResponseRate = totalSponsorMessages > 0
    ? (decimal)sponsorMessagesWithResponses / totalSponsorMessages * 100
    : 0;
```

### Issue 3: Average Response Time Not Calculated

**Problem**:
- Field always returned `0`
- Comment said "Would need message timestamps to calculate"
- But timestamps exist: `AnalysisMessage.CreatedDate`

**Original Code**:
```csharp
AverageMessageResponseTimeHours = 0, // Would need message timestamps to calculate
```

**Fixed Code**:
```csharp
// Calculate time difference between sponsor messages and farmer responses
var responseTimes = new List<double>();
foreach (var sponsorMsg in sponsorMessages)
{
    var farmerResponse = farmerResponses
        .Where(fr => fr.ParentMessageId == sponsorMsg.Id ||
                   (fr.PlantAnalysisId == sponsorMsg.PlantAnalysisId && fr.CreatedDate > sponsorMsg.CreatedDate))
        .OrderBy(fr => fr.CreatedDate)
        .FirstOrDefault();

    if (farmerResponse != null)
    {
        var responseTime = (farmerResponse.CreatedDate - sponsorMsg.CreatedDate).TotalHours;
        if (responseTime > 0) // Only count positive response times
        {
            responseTimes.Add(responseTime);
        }
    }
}

var avgResponseTimeHours = responseTimes.Any()
    ? (decimal)responseTimes.Average()
    : 0;

// ...
AverageMessageResponseTimeHours = avgResponseTimeHours
```

---

## Technical Changes

### Method Signature Update

Added `farmerId` parameter to `AnalyzeBehavioralPatterns`:

```csharp
// Before
private BehavioralPatternsDto AnalyzeBehavioralPatterns(
    List<Entities.Concrete.PlantAnalysis> analyses,
    List<Entities.Concrete.AnalysisMessage> messages,
    List<Entities.Concrete.UserSubscription> subscriptions)

// After
private BehavioralPatternsDto AnalyzeBehavioralPatterns(
    List<Entities.Concrete.PlantAnalysis> analyses,
    List<Entities.Concrete.AnalysisMessage> messages,
    List<Entities.Concrete.UserSubscription> subscriptions,
    int farmerId)  // ← Added parameter
```

### Caller Update

```csharp
// Call site updated to pass farmerId
var behavioralPatterns = AnalyzeBehavioralPatterns(
    relevantAnalyses, messagesList, subscriptionsList, request.FarmerId);
```

---

## Expected Impact

### Before Fix (Example from jurney.json):

```json
{
  "behavioralPatterns": {
    "commonIssues": [],  // ❌ Empty despite 13 analyses
    "messageResponseRate": 94.11764705882352941176470588,  // ❌ False positive
    "averageMessageResponseTimeHours": 0  // ❌ Not calculated
  }
}
```

### After Fix (Expected):

```json
{
  "behavioralPatterns": {
    "commonIssues": ["Fungal Disease", "Nutrient Deficiency", "Pest Infestation"],  // ✅ Real diseases
    "messageResponseRate": 45.2,  // ✅ Realistic engagement rate
    "averageMessageResponseTimeHours": 8.5  // ✅ Real response time
  }
}
```

---

## Message Response Logic Explanation

### Two Detection Methods:

1. **Thread-Based (Preferred)**:
   ```
   Sponsor Message (Id: 100) → Farmer Response (ParentMessageId: 100)
   ```

2. **Timeline-Based (Fallback)**:
   ```
   PlantAnalysisId: 50
   ├─ Sponsor Message (2025-10-22 10:00) → from Sponsor
   └─ Farmer Response (2025-10-22 14:30) → from Farmer (same analysis, later time)
   ```

### Response Rate Calculation:

```
Response Rate = (Sponsor Messages with Responses / Total Sponsor Messages) × 100

Example:
- 100 sponsor messages sent to farmer
- Farmer responded to 45 of them (via thread or same analysis)
- Response Rate = (45 / 100) × 100 = 45%
```

### Response Time Calculation:

```
Response Time = Average(Farmer Response Time - Sponsor Message Time)

Example:
- Sponsor sent message at 10:00
- Farmer responded at 14:30
- Response Time for this pair = 4.5 hours
- Average across all responses = sum(all response times) / count
```

---

## Testing Recommendations

### 1. Test Common Issues
```sql
SELECT
    "Id",
    "Diseases",
    "CropType"
FROM "PlantAnalyses"
WHERE "UserId" = 165
ORDER BY "CreatedDate" DESC;
```

### 2. Test Message Response Rate
```sql
-- Sponsor messages to farmer 165
SELECT COUNT(*) as sponsor_messages
FROM "AnalysisMessages"
WHERE "ToUserId" = 165;

-- Farmer 165 responses
SELECT COUNT(*) as farmer_responses
FROM "AnalysisMessages"
WHERE "FromUserId" = 165;

-- Messages with thread relationships
SELECT
    sm."Id" as sponsor_msg_id,
    sm."CreatedDate" as sponsor_time,
    fr."Id" as farmer_response_id,
    fr."CreatedDate" as farmer_time,
    (EXTRACT(EPOCH FROM (fr."CreatedDate" - sm."CreatedDate")) / 3600) as response_hours
FROM "AnalysisMessages" sm
LEFT JOIN "AnalysisMessages" fr ON fr."ParentMessageId" = sm."Id" OR
    (fr."PlantAnalysisId" = sm."PlantAnalysisId" AND fr."FromUserId" = 165 AND fr."CreatedDate" > sm."CreatedDate")
WHERE sm."ToUserId" = 165
ORDER BY sm."CreatedDate" DESC;
```

### 3. Compare Before/After
- **Before**: Cache key `FarmerJourney:165:admin` or `FarmerJourney:165:{sponsorId}`
- **After**: Clear cache, re-fetch endpoint
- **Verify**: commonIssues populated, response rate realistic, response time > 0

---

## Files Modified

- `Business/Handlers/Sponsorship/Queries/GetFarmerJourneyQuery.cs`

## Build Status

✅ **Success** (0 errors, 0 warnings)

## Commit

```
fix: Improve Farmer Journey behavioral patterns accuracy

Fixed three data accuracy issues in GetFarmerJourneyQuery:
- Common Issues: Parse Diseases JSON array for individual disease names
- Message Response Rate: Count actual farmer responses, not IsRead status
- Average Response Time: Calculate real time difference between messages

Build Status: ✅ Success (0 errors, 0 warnings)
```

**Commit Hash**: 9e6c658
**Branch**: feature/sponsor-advanced-analytics
**Deployed**: Staging (auto-deploy enabled)
