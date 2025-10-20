# 📊 Messaging Status Analysis & Recommendations

**Date:** 2025-10-20
**Feature:** Sponsor Analysis List with Messaging Status Filters
**Scope:** Enable sponsors to track and filter analyses by messaging interaction status

---

## 📋 Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Business Requirements](#business-requirements)
3. [Technical Recommendations](#technical-recommendations)
4. [Implementation Plan](#implementation-plan)
5. [API Changes](#api-changes)
6. [Database Changes](#database-changes)
7. [Mobile UI/UX Recommendations](#mobile-uiux-recommendations)
8. [Testing Strategy](#testing-strategy)

---

## 🔍 Current State Analysis

### Existing Endpoint

**Endpoint:** `GET /api/v1/sponsorship/analyses`

**Current Capabilities:**
✅ Pagination (page, pageSize)
✅ Sorting (date, healthScore, cropType)
✅ Filter by tier (S, M, L, XL)
✅ Filter by crop type
✅ Filter by date range (startDate, endDate)
✅ Returns messaging permission: `CanMessage` field

**Current DTO Structure:**
```csharp
public class SponsoredAnalysisSummaryDto
{
    // ... other fields ...

    public bool CanMessage { get; set; }  // ✅ Exists - permission to message

    // ❌ Missing - actual messaging status:
    // - HasMessages (has any messages been sent?)
    // - MessageCount (how many messages total?)
    // - UnreadMessageCount (how many unread from farmer?)
    // - LastMessageDate (when was last message?)
    // - LastMessagePreview (preview of last message)
    // - MessageInitiator (who sent first message: sponsor/farmer)
}
```

### What's Missing?

Currently sponsors can see:
- ✅ Whether they CAN message (based on tier)
- ❌ Whether they HAVE messaged
- ❌ Whether farmer has replied
- ❌ How many unread messages they have
- ❌ When last message was sent

---

## 💼 Business Requirements

### User Stories

**As a Sponsor, I want to:**

1. **See which analyses I've already contacted**
   - Filter: "Analyses I've messaged"
   - Visual indicator: Message icon + count
   - Sort by: Last message date

2. **See which farmers have replied**
   - Filter: "Farmers who responded"
   - Visual indicator: Unread count badge
   - Sort by: Last reply date

3. **See which analyses need follow-up**
   - Filter: "No farmer response"
   - Visual indicator: Pending status
   - Sort by: Days since last message

4. **Prioritize active conversations**
   - Filter: "Active conversations" (replied within 7 days)
   - Badge: "Active" / "Idle"
   - Sort by: Most recent activity

5. **Find unopened conversations**
   - Filter: "Not yet contacted"
   - Help identify untapped opportunities
   - Sort by: Analysis date (newest first)

### Business Value

**For Sponsors:**
- 📈 Better engagement tracking
- 🎯 Focused outreach efforts
- 💬 Improved response rates
- ⏰ Time-saving filters

**For Platform:**
- 📊 Engagement metrics
- 🔔 Potential for "nudge" notifications
- 💡 Insights into sponsor behavior
- 📱 Enhanced mobile UX

---

## 🛠 Technical Recommendations

### Option 1: Add Messaging Status Fields to Existing DTO (RECOMMENDED)

**Pros:**
- ✅ Backward compatible
- ✅ Single API call
- ✅ Better mobile performance
- ✅ Easier to implement filters

**Cons:**
- ⚠️ Slightly larger response payload
- ⚠️ Need to query messages table (can be optimized with aggregation)

**Changes Required:**

#### 1.1 Update DTO
```csharp
public class SponsoredAnalysisSummaryDto
{
    // ... existing fields ...

    // NEW: Messaging Status Fields
    public MessagingStatusDto MessagingStatus { get; set; }
}

public class MessagingStatusDto
{
    public bool HasMessages { get; set; }
    public int TotalMessageCount { get; set; }
    public int UnreadCount { get; set; }
    public DateTime? LastMessageDate { get; set; }
    public string LastMessagePreview { get; set; }  // First 50 chars
    public string LastMessageBy { get; set; }  // "sponsor" or "farmer"
    public bool HasFarmerResponse { get; set; }
    public DateTime? LastFarmerResponseDate { get; set; }
    public string ConversationStatus { get; set; }  // "NoContact", "Pending", "Active", "Idle"
}
```

#### 1.2 Add Query Filters
```csharp
public class GetSponsoredAnalysesListQuery
{
    // ... existing filters ...

    // NEW: Message Filters
    public string FilterByMessageStatus { get; set; }  // "contacted", "notContacted", "hasResponse", "noResponse", "active", "idle"
    public int? UnreadMessagesMin { get; set; }  // Filter analyses with X+ unread messages
    public bool? HasUnreadMessages { get; set; }  // Quick filter for any unread
}
```

#### 1.3 Database Query Optimization
```sql
-- Efficient aggregation query (add to handler)
SELECT
    pa.Id AS AnalysisId,
    COUNT(am.Id) AS TotalMessageCount,
    SUM(CASE WHEN am.IsRead = false AND am.ToUserId = @SponsorId THEN 1 ELSE 0 END) AS UnreadCount,
    MAX(am.SentDate) AS LastMessageDate,
    MAX(CASE WHEN am.FromUserId = @SponsorId THEN am.SentDate ELSE NULL END) AS LastSponsorMessageDate,
    MAX(CASE WHEN am.ToUserId = @SponsorId THEN am.SentDate ELSE NULL END) AS LastFarmerMessageDate
FROM PlantAnalyses pa
LEFT JOIN AnalysisMessages am ON pa.Id = am.PlantAnalysisId AND am.IsDeleted = false
WHERE pa.SponsorUserId = @SponsorId
GROUP BY pa.Id
```

---

### Option 2: Separate Messaging Summary Endpoint

**Pros:**
- ✅ Keeps main endpoint lean
- ✅ Can be cached separately

**Cons:**
- ❌ Two API calls needed
- ❌ More complex mobile implementation
- ❌ Not ideal for list view

**NOT RECOMMENDED** for this use case.

---

## 📝 Implementation Plan

### Phase 1: Backend Changes (2-3 hours)

#### Step 1: Database Layer
- [ ] Add messaging aggregation method to `IAnalysisMessageRepository`
- [ ] Implement `GetMessagingStatusForAnalysesAsync(sponsorId, analysisIds[])`
- [ ] Add indexes for performance: `(PlantAnalysisId, IsDeleted, SentDate)`

```csharp
// DataAccess/Abstract/IAnalysisMessageRepository.cs
Task<Dictionary<int, MessagingStatusDto>> GetMessagingStatusForAnalysesAsync(
    int sponsorId,
    int[] analysisIds);
```

#### Step 2: DTO Updates
- [ ] Add `MessagingStatusDto` class to `Entities/Dtos/`
- [ ] Update `SponsoredAnalysisSummaryDto` to include messaging status
- [ ] Add `ConversationStatus` enum (NoContact, Pending, Active, Idle)

#### Step 3: Query Handler Updates
- [ ] Update `GetSponsoredAnalysesListQuery` with new filter parameters
- [ ] Update query handler to fetch messaging status
- [ ] Apply filters based on message status
- [ ] Update sorting to include message-based sorts

#### Step 4: Controller Updates
- [ ] Add new query parameters to controller method
- [ ] Update XML documentation
- [ ] Update Swagger annotations

### Phase 2: API Documentation (1 hour)

- [ ] Update mobile API documentation
- [ ] Add filter parameter descriptions
- [ ] Add response field examples
- [ ] Create filtering guide

### Phase 3: Testing (2 hours)

- [ ] Unit tests for messaging status aggregation
- [ ] Integration tests for filters
- [ ] Performance tests with large datasets
- [ ] Mobile team validation

**Total Estimated Time: 5-6 hours**

---

## 🌐 API Changes

### Request Parameters (NEW)

```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=contacted&page=1&pageSize=20
```

| Parameter | Type | Values | Description |
|-----------|------|--------|-------------|
| `filterByMessageStatus` | string | `all` (default) | No filter |
| | | `contacted` | Analyses where sponsor has sent at least one message |
| | | `notContacted` | Analyses where sponsor has never messaged |
| | | `hasResponse` | Farmer has replied to sponsor's message |
| | | `noResponse` | Sponsor messaged but no farmer reply |
| | | `active` | Last message within 7 days |
| | | `idle` | Last message > 7 days ago |
| `hasUnreadMessages` | boolean | `true`/`false` | Filter analyses with unread messages from farmer |
| `unreadMessagesMin` | integer | 1-999 | Filter analyses with at least X unread messages |
| `sortBy` | string | `lastMessage` | NEW: Sort by last message date |

### Response Example (UPDATED)

```json
{
  "data": {
    "items": [
      {
        "analysisId": 123,
        "analysisDate": "2025-10-15T10:30:00",
        "cropType": "Tomato",
        "overallHealthScore": 85.5,
        "farmerName": "Ahmet Yılmaz",
        "canMessage": true,

        "messagingStatus": {
          "hasMessages": true,
          "totalMessageCount": 5,
          "unreadCount": 2,
          "lastMessageDate": "2025-10-19T14:20:00",
          "lastMessagePreview": "Teşekkürler, tavsiyelerinizi uygulayacağım...",
          "lastMessageBy": "farmer",
          "hasFarmerResponse": true,
          "lastFarmerResponseDate": "2025-10-19T14:20:00",
          "conversationStatus": "Active"
        },

        "tierName": "XL",
        "accessPercentage": 100
      },
      {
        "analysisId": 124,
        "analysisDate": "2025-10-18T09:15:00",
        "cropType": "Wheat",
        "overallHealthScore": 72.3,
        "farmerName": "Mehmet Demir",
        "canMessage": true,

        "messagingStatus": {
          "hasMessages": true,
          "totalMessageCount": 2,
          "unreadCount": 0,
          "lastMessageDate": "2025-10-18T11:30:00",
          "lastMessagePreview": "Merhaba, analiz sonuçlarınızı inceledim...",
          "lastMessageBy": "sponsor",
          "hasFarmerResponse": false,
          "lastFarmerResponseDate": null,
          "conversationStatus": "Pending"
        },

        "tierName": "L",
        "accessPercentage": 60
      },
      {
        "analysisId": 125,
        "analysisDate": "2025-10-20T08:00:00",
        "cropType": "Pepper",
        "overallHealthScore": 91.0,
        "farmerName": "Ali Kaya",
        "canMessage": true,

        "messagingStatus": {
          "hasMessages": false,
          "totalMessageCount": 0,
          "unreadCount": 0,
          "lastMessageDate": null,
          "lastMessagePreview": null,
          "lastMessageBy": null,
          "hasFarmerResponse": false,
          "lastFarmerResponseDate": null,
          "conversationStatus": "NoContact"
        },

        "tierName": "M",
        "accessPercentage": 30
      }
    ],
    "totalCount": 145,
    "page": 1,
    "pageSize": 20,
    "totalPages": 8,
    "summary": {
      "totalAnalyses": 145,
      "contactedAnalyses": 87,
      "notContactedAnalyses": 58,
      "activeConversations": 23,
      "pendingResponses": 42,
      "totalUnreadMessages": 15
    }
  },
  "success": true,
  "message": "Analyses retrieved successfully"
}
```

### Conversation Status Enum

```csharp
public enum ConversationStatus
{
    NoContact,   // No messages sent yet
    Pending,     // Sponsor sent message, waiting for farmer reply
    Active,      // Two-way conversation, recent activity (< 7 days)
    Idle         // Conversation exists but no recent activity (> 7 days)
}
```

**Status Logic:**
```
NoContact:  totalMessageCount == 0
Pending:    totalMessageCount > 0 && !hasFarmerResponse
Active:     hasFarmerResponse && (now - lastMessageDate) < 7 days
Idle:       hasFarmerResponse && (now - lastMessageDate) >= 7 days
```

---

## 💾 Database Changes

### New Repository Method

```csharp
// IAnalysisMessageRepository.cs
public interface IAnalysisMessageRepository
{
    // ... existing methods ...

    /// <summary>
    /// Get messaging status summary for multiple analyses
    /// </summary>
    Task<Dictionary<int, MessagingStatusDto>> GetMessagingStatusForAnalysesAsync(
        int sponsorId,
        int[] analysisIds);

    /// <summary>
    /// Get analyses with messaging status filter applied
    /// </summary>
    Task<List<int>> GetAnalysisIdsByMessageStatusAsync(
        int sponsorId,
        string messageStatus);
}
```

### Implementation (EF Core)

```csharp
public async Task<Dictionary<int, MessagingStatusDto>> GetMessagingStatusForAnalysesAsync(
    int sponsorId,
    int[] analysisIds)
{
    var result = await Context.AnalysisMessages
        .Where(m => analysisIds.Contains(m.PlantAnalysisId) && !m.IsDeleted)
        .GroupBy(m => m.PlantAnalysisId)
        .Select(g => new
        {
            AnalysisId = g.Key,
            TotalMessageCount = g.Count(),
            UnreadCount = g.Count(m => !m.IsRead && m.ToUserId == sponsorId),
            LastMessageDate = g.Max(m => m.SentDate),
            LastMessage = g.OrderByDescending(m => m.SentDate).FirstOrDefault(),
            HasFarmerResponse = g.Any(m => m.ToUserId == sponsorId),
            LastFarmerResponseDate = g.Where(m => m.ToUserId == sponsorId)
                .Max(m => (DateTime?)m.SentDate)
        })
        .ToDictionaryAsync(
            x => x.AnalysisId,
            x => new MessagingStatusDto
            {
                HasMessages = true,
                TotalMessageCount = x.TotalMessageCount,
                UnreadCount = x.UnreadCount,
                LastMessageDate = x.LastMessageDate,
                LastMessagePreview = x.LastMessage != null
                    ? (x.LastMessage.Message.Length > 50
                        ? x.LastMessage.Message.Substring(0, 50) + "..."
                        : x.LastMessage.Message)
                    : null,
                LastMessageBy = x.LastMessage.FromUserId == sponsorId ? "sponsor" : "farmer",
                HasFarmerResponse = x.HasFarmerResponse,
                LastFarmerResponseDate = x.LastFarmerResponseDate,
                ConversationStatus = CalculateConversationStatus(
                    x.TotalMessageCount,
                    x.HasFarmerResponse,
                    x.LastMessageDate)
            });

    // Add analyses with no messages
    foreach (var analysisId in analysisIds.Where(id => !result.ContainsKey(id)))
    {
        result[analysisId] = new MessagingStatusDto
        {
            HasMessages = false,
            TotalMessageCount = 0,
            UnreadCount = 0,
            ConversationStatus = ConversationStatus.NoContact
        };
    }

    return result;
}

private static ConversationStatus CalculateConversationStatus(
    int totalCount,
    bool hasResponse,
    DateTime lastMessageDate)
{
    if (totalCount == 0)
        return ConversationStatus.NoContact;

    if (!hasResponse)
        return ConversationStatus.Pending;

    var daysSinceLastMessage = (DateTime.Now - lastMessageDate).Days;
    return daysSinceLastMessage < 7
        ? ConversationStatus.Active
        : ConversationStatus.Idle;
}
```

### Performance Optimization: Database Index

```sql
-- Add index for faster messaging status queries
CREATE INDEX IX_AnalysisMessages_PlantAnalysisId_IsDeleted_SentDate
ON AnalysisMessages (PlantAnalysisId, IsDeleted, SentDate DESC)
INCLUDE (FromUserId, ToUserId, IsRead, Message);
```

**Migration Command:**
```bash
dotnet ef migrations add AddMessagingStatusIndexes --project DataAccess --startup-project WebAPI --context ProjectDbContext --output-dir Migrations/Pg
```

---

## 📱 Mobile UI/UX Recommendations

### List View Design

```
┌─────────────────────────────────────────┐
│ Filter: [All ▼] [Contacted] [Unread]   │
├─────────────────────────────────────────┤
│                                         │
│ ┌─────────────────────────────────────┐ │
│ │ 🌱 Tomato - 85% Health              │ │
│ │ Ahmet Yılmaz • Oct 15, 2025         │ │
│ │ 💬 5 messages • 2 unread • Active   │ │
│ │ "Teşekkürler, tavsiyelerinizi..."   │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ ┌─────────────────────────────────────┐ │
│ │ 🌾 Wheat - 72% Health               │ │
│ │ Mehmet Demir • Oct 18, 2025         │ │
│ │ ⏳ 2 messages • Waiting for reply   │ │
│ │ "Merhaba, analiz sonuçlarınızı..." │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ ┌─────────────────────────────────────┐ │
│ │ 🌶️ Pepper - 91% Health              │ │
│ │ Ali Kaya • Oct 20, 2025             │ │
│ │ 📧 Not contacted yet                │ │
│ │                                     │ │
│ └─────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

### Visual Indicators

**Status Badges:**
- 💬 **Active** (green) - Recent conversation
- ⏳ **Pending** (yellow) - Waiting for farmer reply
- 📧 **New** (blue) - Not contacted yet
- 💤 **Idle** (gray) - No recent activity

**Unread Count:**
- Red badge with count: `🔴 2`
- Position: Top right of card

**Message Preview:**
- Show last 50 characters
- Italic font, gray color
- Truncate with "..."

### Filter Chips (Top of List)

```
┌──────────────────────────────────────────────┐
│ [All ▼] [Contacted] [Not Contacted]         │
│ [Has Response] [Unread] [Active]            │
└──────────────────────────────────────────────┘
```

**Quick Filters:**
1. **All** - Default, no filter
2. **Contacted** - At least one message sent
3. **Not Contacted** - No messages yet (opportunity!)
4. **Has Response** - Farmer replied
5. **Unread** - Has unread farmer messages
6. **Active** - Recent conversation (< 7 days)

### Sort Options

```
Sort by: [Most Recent ▼]
  - Most Recent Activity
  - Unread First
  - Not Contacted First
  - Health Score (High to Low)
  - Health Score (Low to High)
  - Analysis Date (Newest)
  - Analysis Date (Oldest)
```

---

## 🧪 Testing Strategy

### Unit Tests

```csharp
[TestFixture]
public class MessagingStatusTests
{
    [Test]
    public async Task GetMessagingStatus_ShouldReturn_NoContact_WhenNoMessages()
    {
        // Arrange
        var analysisId = 123;
        var sponsorId = 456;

        // Act
        var status = await _repository.GetMessagingStatusForAnalysesAsync(
            sponsorId,
            new[] { analysisId });

        // Assert
        Assert.That(status[analysisId].ConversationStatus, Is.EqualTo(ConversationStatus.NoContact));
        Assert.That(status[analysisId].HasMessages, Is.False);
        Assert.That(status[analysisId].TotalMessageCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GetMessagingStatus_ShouldReturn_Pending_WhenOnlySponsorMessaged()
    {
        // Arrange: Create scenario where sponsor sent message but no farmer reply

        // Act
        var status = await _repository.GetMessagingStatusForAnalysesAsync(...);

        // Assert
        Assert.That(status[analysisId].ConversationStatus, Is.EqualTo(ConversationStatus.Pending));
        Assert.That(status[analysisId].HasFarmerResponse, Is.False);
    }

    [Test]
    public async Task FilterByMessageStatus_Contacted_ShouldReturnOnlyContactedAnalyses()
    {
        // Test filter logic
    }

    [Test]
    public async Task Performance_ShouldHandleLargeDataset_Within2Seconds()
    {
        // Test with 1000+ analyses
    }
}
```

### Integration Tests

```csharp
[TestFixture]
public class SponsoredAnalysesListIntegrationTests
{
    [Test]
    public async Task GetAnalysesList_WithContactedFilter_ShouldReturnCorrectResults()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get,
            "/api/v1/sponsorship/analyses?filterByMessageStatus=contacted");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sponsorToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SponsoredAnalysesListResponseDto>(content);

        Assert.That(result.Items.All(i => i.MessagingStatus.HasMessages), Is.True);
    }
}
```

### Manual Testing Scenarios

**Scenario 1: New Sponsor (No Messages)**
- [ ] Create sponsor account
- [ ] View analyses list
- [ ] Verify all show "Not contacted"
- [ ] Apply "notContacted" filter
- [ ] Verify count matches total

**Scenario 2: Active Messaging**
- [ ] Send message to farmer
- [ ] Verify status changes to "Pending"
- [ ] Farmer replies
- [ ] Verify status changes to "Active"
- [ ] Apply "hasResponse" filter
- [ ] Verify analysis appears

**Scenario 3: Unread Messages**
- [ ] Farmer sends 3 messages
- [ ] Verify unread count badge shows "3"
- [ ] Apply "hasUnread" filter
- [ ] Verify analysis appears first
- [ ] Read 1 message
- [ ] Verify count updates to "2"

**Scenario 4: Performance**
- [ ] Test with 500+ analyses
- [ ] Measure load time (< 2 seconds)
- [ ] Test filters with large dataset
- [ ] Verify smooth scrolling

---

## 🎯 Success Metrics

**Technical Metrics:**
- [ ] API response time < 2 seconds (with 500+ analyses)
- [ ] 100% test coverage for messaging status logic
- [ ] Zero N+1 query issues
- [ ] Mobile app handles gracefully with slow network

**Business Metrics:**
- [ ] Increased sponsor engagement rate
- [ ] Higher message response rates
- [ ] Reduced "abandoned" analyses (never contacted)
- [ ] Positive sponsor feedback on feature

---

## 🚀 Quick Implementation Checklist

### Backend (Must Have)
- [ ] Add `MessagingStatusDto` class
- [ ] Update `SponsoredAnalysisSummaryDto`
- [ ] Add repository method `GetMessagingStatusForAnalysesAsync()`
- [ ] Update query handler to fetch and populate messaging status
- [ ] Add filter parameters to query
- [ ] Implement filter logic
- [ ] Add database index for performance
- [ ] Update controller parameters
- [ ] Add unit tests
- [ ] Add integration tests

### Backend (Nice to Have)
- [ ] Cache messaging status (Redis, 5 min TTL)
- [ ] Add real-time SignalR updates for status changes
- [ ] Add webhook for mobile push notifications

### Documentation
- [ ] Update API documentation
- [ ] Create mobile implementation guide
- [ ] Add Postman collection examples

### Mobile (Recommendation for Team)
- [ ] Add messaging status UI components
- [ ] Implement filter chips
- [ ] Add unread badge overlay
- [ ] Update list card design
- [ ] Add pull-to-refresh
- [ ] Implement sorting options
- [ ] Add empty states ("No contacted analyses yet")

---

## 📊 Summary & Recommendation

### ⭐ RECOMMENDED APPROACH

**Option 1: Extend Existing Endpoint** (5-6 hours implementation)

**Add to `SponsoredAnalysisSummaryDto`:**
```csharp
public MessagingStatusDto MessagingStatus { get; set; }
```

**Add Query Filters:**
- `filterByMessageStatus`: contacted, notContacted, hasResponse, noResponse, active, idle
- `hasUnreadMessages`: true/false
- `sortBy`: lastMessage (new option)

**Database Optimization:**
- Single aggregation query for all messaging status
- Add composite index on (PlantAnalysisId, IsDeleted, SentDate)

**Mobile Benefits:**
- Single API call
- Rich filtering capabilities
- Real-time status tracking
- Better user experience

### 🎁 Business Value

**High Impact Features:**
1. ✅ "Not Contacted" filter - Find untapped opportunities
2. ✅ "Has Response" filter - Track farmer engagement
3. ✅ Unread count badges - Never miss farmer replies
4. ✅ Conversation status - Prioritize active conversations

**ROI:**
- Estimated 30% increase in sponsor-farmer engagement
- Better sponsor satisfaction scores
- More data for platform analytics
- Competitive differentiator

---

## 📞 Next Steps

1. **Review & Approve** - Product team review this document
2. **Prioritize** - Add to sprint backlog
3. **Implement** - Backend team (5-6 hours)
4. **Document** - Update API docs for mobile team
5. **Mobile Integration** - Mobile team implements UI (estimated 8-10 hours)
6. **Test** - QA validation
7. **Deploy** - Staging → Production
8. **Monitor** - Track engagement metrics

---

## 💻 IMPLEMENTATION CODE GUIDE

> **⭐ This section contains complete, copy-paste ready code for implementation**

### Step 1: Create ConversationStatus Enum

**File:** `Entities/Concrete/ConversationStatus.cs` (NEW)

```csharp
namespace Entities.Concrete
{
    /// <summary>
    /// Represents the status of a conversation between sponsor and farmer
    /// </summary>
    public enum ConversationStatus
    {
        /// <summary>
        /// No messages have been sent yet
        /// </summary>
        NoContact = 0,

        /// <summary>
        /// Sponsor sent message(s), waiting for farmer reply
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Two-way conversation with recent activity (within 7 days)
        /// </summary>
        Active = 2,

        /// <summary>
        /// Conversation exists but no recent activity (7+ days)
        /// </summary>
        Idle = 3
    }
}
```

---

### Step 2: Create MessagingStatusDto

**File:** `Entities/Dtos/MessagingStatusDto.cs` (NEW)

```csharp
using Entities.Concrete;
using System;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO containing messaging status information for a sponsor-farmer conversation
    /// </summary>
    public class MessagingStatusDto
    {
        /// <summary>
        /// Whether any messages exist in this conversation
        /// </summary>
        public bool HasMessages { get; set; }

        /// <summary>
        /// Total number of messages exchanged (both directions)
        /// </summary>
        public int TotalMessageCount { get; set; }

        /// <summary>
        /// Number of unread messages from farmer to sponsor
        /// </summary>
        public int UnreadCount { get; set; }

        /// <summary>
        /// Date/time of the most recent message (either direction)
        /// </summary>
        public DateTime? LastMessageDate { get; set; }

        /// <summary>
        /// Preview of last message (first 50 characters)
        /// </summary>
        public string LastMessagePreview { get; set; }

        /// <summary>
        /// Who sent the last message: "sponsor" or "farmer"
        /// </summary>
        public string LastMessageBy { get; set; }

        /// <summary>
        /// Whether farmer has sent at least one reply
        /// </summary>
        public bool HasFarmerResponse { get; set; }

        /// <summary>
        /// Date/time of farmer's most recent message
        /// </summary>
        public DateTime? LastFarmerResponseDate { get; set; }

        /// <summary>
        /// Current status of the conversation
        /// </summary>
        public ConversationStatus ConversationStatus { get; set; }
    }
}
```

---

### Step 3: Update SponsoredAnalysisSummaryDto

**File:** `Entities/Dtos/SponsoredAnalysisSummaryDto.cs`

**Add this property to the existing class:**

```csharp
/// <summary>
/// Messaging status information for this analysis
/// </summary>
public MessagingStatusDto MessagingStatus { get; set; }
```

---

### Step 4: Update IAnalysisMessageRepository Interface

**File:** `DataAccess/Abstract/IAnalysisMessageRepository.cs`

**Add this method to the interface:**

```csharp
/// <summary>
/// Get messaging status summary for multiple analyses efficiently
/// Uses a single query with grouping for optimal performance
/// </summary>
/// <param name="sponsorId">ID of the sponsor</param>
/// <param name="analysisIds">Array of analysis IDs to get status for</param>
/// <returns>Dictionary mapping analysis ID to messaging status</returns>
Task<Dictionary<int, MessagingStatusDto>> GetMessagingStatusForAnalysesAsync(
    int sponsorId,
    int[] analysisIds);
```

---

### Step 5: Implement Repository Method

**File:** `DataAccess/Concrete/EntityFramework/AnalysisMessageRepository.cs`

**Add this method to the class:**

```csharp
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public async Task<Dictionary<int, MessagingStatusDto>> GetMessagingStatusForAnalysesAsync(
    int sponsorId,
    int[] analysisIds)
{
    // Single efficient query with grouping
    var result = await Context.AnalysisMessages
        .Where(m => analysisIds.Contains(m.PlantAnalysisId) && !m.IsDeleted)
        .GroupBy(m => m.PlantAnalysisId)
        .Select(g => new
        {
            AnalysisId = g.Key,
            TotalMessageCount = g.Count(),
            UnreadCount = g.Count(m => !m.IsRead && m.ToUserId == sponsorId),
            LastMessageDate = g.Max(m => m.SentDate),
            LastMessage = g.OrderByDescending(m => m.SentDate).FirstOrDefault(),
            HasFarmerResponse = g.Any(m => m.ToUserId == sponsorId),
            LastFarmerResponseDate = g.Where(m => m.ToUserId == sponsorId)
                .Max(m => (DateTime?)m.SentDate)
        })
        .ToDictionaryAsync(
            x => x.AnalysisId,
            x => new MessagingStatusDto
            {
                HasMessages = true,
                TotalMessageCount = x.TotalMessageCount,
                UnreadCount = x.UnreadCount,
                LastMessageDate = x.LastMessageDate,
                LastMessagePreview = x.LastMessage != null && !string.IsNullOrEmpty(x.LastMessage.Message)
                    ? (x.LastMessage.Message.Length > 50
                        ? x.LastMessage.Message.Substring(0, 50) + "..."
                        : x.LastMessage.Message)
                    : null,
                LastMessageBy = x.LastMessage != null
                    ? (x.LastMessage.FromUserId == sponsorId ? "sponsor" : "farmer")
                    : null,
                HasFarmerResponse = x.HasFarmerResponse,
                LastFarmerResponseDate = x.LastFarmerResponseDate,
                ConversationStatus = CalculateConversationStatus(
                    x.TotalMessageCount,
                    x.HasFarmerResponse,
                    x.LastMessageDate)
            });

    // Add default status for analyses with no messages
    foreach (var analysisId in analysisIds.Where(id => !result.ContainsKey(id)))
    {
        result[analysisId] = new MessagingStatusDto
        {
            HasMessages = false,
            TotalMessageCount = 0,
            UnreadCount = 0,
            LastMessageDate = null,
            LastMessagePreview = null,
            LastMessageBy = null,
            HasFarmerResponse = false,
            LastFarmerResponseDate = null,
            ConversationStatus = ConversationStatus.NoContact
        };
    }

    return result;
}

/// <summary>
/// Calculate conversation status based on message counts and dates
/// </summary>
private static ConversationStatus CalculateConversationStatus(
    int totalCount,
    bool hasResponse,
    DateTime lastMessageDate)
{
    if (totalCount == 0)
        return ConversationStatus.NoContact;

    if (!hasResponse)
        return ConversationStatus.Pending;

    var daysSinceLastMessage = (DateTime.Now - lastMessageDate).Days;
    return daysSinceLastMessage < 7
        ? ConversationStatus.Active
        : ConversationStatus.Idle;
}
```

---

### Step 6: Update GetSponsoredAnalysesListQuery

**File:** `Business/Handlers/PlantAnalyses/Queries/GetSponsoredAnalysesListQuery.cs`

**Add new filter properties to the query class:**

```csharp
// NEW: Message Status Filters
/// <summary>
/// Filter by message status: all, contacted, notContacted, hasResponse, noResponse, active, idle
/// </summary>
public string FilterByMessageStatus { get; set; }

/// <summary>
/// Filter to show only analyses with unread messages from farmer
/// </summary>
public bool? HasUnreadMessages { get; set; }

/// <summary>
/// Filter to show analyses with at least this many unread messages
/// </summary>
public int? UnreadMessagesMin { get; set; }
```

---

### Step 7: Update Query Handler - Add Dependency

**File:** `Business/Handlers/PlantAnalyses/Queries/GetSponsoredAnalysesListQuery.cs`

**Update the Handler class constructor:**

```csharp
public class GetSponsoredAnalysesListQueryHandler : IRequestHandler<GetSponsoredAnalysesListQuery, IDataResult<SponsoredAnalysesListResponseDto>>
{
    private readonly IPlantAnalysisRepository _plantAnalysisRepository;
    private readonly ISponsorDataAccessService _dataAccessService;
    private readonly ISponsorProfileRepository _sponsorProfileRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionTierRepository _subscriptionTierRepository;
    private readonly IAnalysisMessageRepository _messageRepository; // NEW

    public GetSponsoredAnalysesListQueryHandler(
        IPlantAnalysisRepository plantAnalysisRepository,
        ISponsorDataAccessService dataAccessService,
        ISponsorProfileRepository sponsorProfileRepository,
        IUserRepository userRepository,
        ISubscriptionTierRepository subscriptionTierRepository,
        IAnalysisMessageRepository messageRepository) // NEW
    {
        _plantAnalysisRepository = plantAnalysisRepository;
        _dataAccessService = dataAccessService;
        _sponsorProfileRepository = sponsorProfileRepository;
        _userRepository = userRepository;
        _subscriptionTierRepository = subscriptionTierRepository;
        _messageRepository = messageRepository; // NEW
    }
}
```

---

### Step 8: Update Handler - Add Messaging Logic

**In the Handle method, add this code AFTER filtering and sorting, BEFORE pagination:**

```csharp
var filteredAnalyses = analysesQuery.ToList();

// NEW: Fetch messaging status for all analyses (BEFORE pagination)
var analysisIds = filteredAnalyses.Select(a => a.Id).ToArray();
var messagingStatuses = await _messageRepository.GetMessagingStatusForAnalysesAsync(
    request.SponsorId,
    analysisIds);

// NEW: Apply messaging filters
if (!string.IsNullOrEmpty(request.FilterByMessageStatus))
{
    filteredAnalyses = ApplyMessageStatusFilter(
        filteredAnalyses,
        messagingStatuses,
        request.FilterByMessageStatus).ToList();
}

if (request.HasUnreadMessages.HasValue && request.HasUnreadMessages.Value)
{
    filteredAnalyses = filteredAnalyses
        .Where(a => messagingStatuses.ContainsKey(a.Id) &&
                   messagingStatuses[a.Id].UnreadCount > 0)
        .ToList();
}

if (request.UnreadMessagesMin.HasValue)
{
    filteredAnalyses = filteredAnalyses
        .Where(a => messagingStatuses.ContainsKey(a.Id) &&
                   messagingStatuses[a.Id].UnreadCount >= request.UnreadMessagesMin.Value)
        .ToList();
}

// Update total count after messaging filters
var totalCount = filteredAnalyses.Count;

// Continue with pagination...
var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
var skip = (request.Page - 1) * request.PageSize;
var pagedAnalyses = filteredAnalyses.Skip(skip).Take(request.PageSize).ToList();

// Map to DTOs with messaging status
var items = pagedAnalyses.Select(analysis =>
{
    var dto = MapToSummaryDto(analysis, accessPercentage, sponsorProfile);

    // NEW: Add messaging status
    dto.MessagingStatus = messagingStatuses.ContainsKey(analysis.Id)
        ? messagingStatuses[analysis.Id]
        : new MessagingStatusDto
        {
            HasMessages = false,
            TotalMessageCount = 0,
            UnreadCount = 0,
            ConversationStatus = ConversationStatus.NoContact
        };

    return dto;
}).ToArray();
```

---

### Step 9: Add Filter Helper Method

**Add this private method to the Handler class:**

```csharp
/// <summary>
/// Apply message status filter to analyses list
/// </summary>
private IEnumerable<Entities.Concrete.PlantAnalysis> ApplyMessageStatusFilter(
    IEnumerable<Entities.Concrete.PlantAnalysis> analyses,
    Dictionary<int, MessagingStatusDto> messagingStatuses,
    string filterValue)
{
    return filterValue?.ToLower() switch
    {
        "contacted" => analyses.Where(a =>
            messagingStatuses.ContainsKey(a.Id) &&
            messagingStatuses[a.Id].HasMessages),

        "notcontacted" => analyses.Where(a =>
            !messagingStatuses.ContainsKey(a.Id) ||
            !messagingStatuses[a.Id].HasMessages),

        "hasresponse" => analyses.Where(a =>
            messagingStatuses.ContainsKey(a.Id) &&
            messagingStatuses[a.Id].HasFarmerResponse),

        "noresponse" => analyses.Where(a =>
            messagingStatuses.ContainsKey(a.Id) &&
            messagingStatuses[a.Id].HasMessages &&
            !messagingStatuses[a.Id].HasFarmerResponse),

        "active" => analyses.Where(a =>
            messagingStatuses.ContainsKey(a.Id) &&
            messagingStatuses[a.Id].ConversationStatus == ConversationStatus.Active),

        "idle" => analyses.Where(a =>
            messagingStatuses.ContainsKey(a.Id) &&
            messagingStatuses[a.Id].ConversationStatus == ConversationStatus.Idle),

        _ => analyses // "all" or invalid value - return unfiltered
    };
}
```

---

### Step 10: Update Summary Statistics

**Replace the summary calculation in Handle method:**

```csharp
// NEW: Calculate messaging summary statistics
var summary = new SponsoredAnalysesListSummaryDto
{
    TotalAnalyses = totalCount,
    AverageHealthScore = filteredAnalyses.Any()
        ? (decimal)filteredAnalyses.Average(a => a.OverallHealthScore)
        : 0,
    TopCropTypes = filteredAnalyses
        .Where(a => !string.IsNullOrEmpty(a.CropType))
        .GroupBy(a => a.CropType)
        .OrderByDescending(g => g.Count())
        .Take(5)
        .Select(g => g.Key)
        .ToArray(),
    AnalysesThisMonth = filteredAnalyses
        .Count(a => a.AnalysisDate.Month == DateTime.Now.Month &&
                    a.AnalysisDate.Year == DateTime.Now.Year),

    // NEW: Messaging statistics
    ContactedAnalyses = messagingStatuses.Count(kvp => kvp.Value.HasMessages),
    NotContactedAnalyses = totalCount - messagingStatuses.Count(kvp => kvp.Value.HasMessages),
    ActiveConversations = messagingStatuses.Count(kvp =>
        kvp.Value.ConversationStatus == ConversationStatus.Active),
    PendingResponses = messagingStatuses.Count(kvp =>
        kvp.Value.ConversationStatus == ConversationStatus.Pending),
    TotalUnreadMessages = messagingStatuses.Sum(kvp => kvp.Value.UnreadCount)
};
```

---

### Step 11: Update SponsoredAnalysesListSummaryDto

**File:** `Entities/Dtos/SponsoredAnalysesListSummaryDto.cs`

**Add new properties:**

```csharp
/// <summary>
/// Number of analyses where sponsor has sent at least one message
/// </summary>
public int ContactedAnalyses { get; set; }

/// <summary>
/// Number of analyses where sponsor has not sent any messages
/// </summary>
public int NotContactedAnalyses { get; set; }

/// <summary>
/// Number of active conversations (two-way, recent activity)
/// </summary>
public int ActiveConversations { get; set; }

/// <summary>
/// Number of conversations waiting for farmer response
/// </summary>
public int PendingResponses { get; set; }

/// <summary>
/// Total unread messages across all conversations
/// </summary>
public int TotalUnreadMessages { get; set; }
```

---

### Step 12: Update Controller

**File:** `WebAPI/Controllers/SponsorshipController.cs`

**Update the GetAnalyses endpoint parameters:**

```csharp
[HttpGet("analyses")]
[ProducesResponseType(typeof(IDataResult<SponsoredAnalysesListResponseDto>), 200)]
public async Task<IActionResult> GetAnalyses(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string sortBy = "date",
    [FromQuery] string sortOrder = "desc",
    [FromQuery] string filterByTier = null,
    [FromQuery] string filterByCropType = null,
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null,
    // NEW: Messaging filter parameters
    [FromQuery] string filterByMessageStatus = null,
    [FromQuery] bool? hasUnreadMessages = null,
    [FromQuery] int? unreadMessagesMin = null)
{
    var userId = User.GetUserId();
    if (!userId.HasValue)
    {
        return Unauthorized(new ErrorDataResult<SponsoredAnalysesListResponseDto>("User not authenticated"));
    }

    var query = new GetSponsoredAnalysesListQuery
    {
        SponsorId = userId.Value,
        Page = page,
        PageSize = pageSize,
        SortBy = sortBy,
        SortOrder = sortOrder,
        FilterByTier = filterByTier,
        FilterByCropType = filterByCropType,
        StartDate = startDate,
        EndDate = endDate,
        // NEW: Pass messaging filters
        FilterByMessageStatus = filterByMessageStatus,
        HasUnreadMessages = hasUnreadMessages,
        UnreadMessagesMin = unreadMessagesMin
    };

    var result = await Mediator.Send(query);
    return result.Success ? Ok(result) : BadRequest(result);
}
```

---

### Step 13: Create Database Migration

**Run these commands:**

```bash
# Create migration
dotnet ef migrations add AddMessagingStatusIndexes --project DataAccess --startup-project WebAPI --context ProjectDbContext --output-dir Migrations/Pg

# Apply migration
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext
```

---

## ✅ Implementation Checklist

### Files to Create
- [ ] `Entities/Concrete/ConversationStatus.cs`
- [ ] `Entities/Dtos/MessagingStatusDto.cs`

### Files to Modify
- [ ] `Entities/Dtos/SponsoredAnalysisSummaryDto.cs` - Add MessagingStatus property
- [ ] `Entities/Dtos/SponsoredAnalysesListSummaryDto.cs` - Add messaging statistics
- [ ] `DataAccess/Abstract/IAnalysisMessageRepository.cs` - Add method signature
- [ ] `DataAccess/Concrete/EntityFramework/AnalysisMessageRepository.cs` - Implement method + helper
- [ ] `Business/Handlers/PlantAnalyses/Queries/GetSponsoredAnalysesListQuery.cs` - Add filters, dependency, logic, helper method
- [ ] `WebAPI/Controllers/SponsorshipController.cs` - Add query parameters

### Commands to Run
- [ ] `dotnet build`
- [ ] `dotnet ef migrations add AddMessagingStatusIndexes --project DataAccess --startup-project WebAPI --context ProjectDbContext --output-dir Migrations/Pg`
- [ ] `dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext`
- [ ] `dotnet test`

### Testing
- [ ] Test NoContact status (no messages)
- [ ] Test Pending status (sponsor sent, no reply)
- [ ] Test Active status (two-way, recent)
- [ ] Test Idle status (two-way, old)
- [ ] Test all filters: contacted, notContacted, hasResponse, noResponse, active, idle
- [ ] Test hasUnreadMessages filter
- [ ] Test unreadMessagesMin filter
- [ ] Test performance with 500+ analyses
- [ ] Verify API response time < 2 seconds
- [ ] Check for N+1 queries

---

## 🎯 Quick Reference - Filter Values

| Parameter | Values | Description |
|-----------|--------|-------------|
| `filterByMessageStatus` | `all` | No filter |
| | `contacted` | Sponsor sent ≥1 message |
| | `notContacted` | No messages |
| | `hasResponse` | Farmer replied |
| | `noResponse` | No farmer reply |
| | `active` | Recent (< 7 days) |
| | `idle` | Old (≥ 7 days) |
| `hasUnreadMessages` | `true/false` | Has unread messages |
| `unreadMessagesMin` | `1-999` | Min unread count |

---

## 📚 Example API Calls

```http
# Get contacted analyses
GET /api/v1/sponsorship/analyses?filterByMessageStatus=contacted

# Get analyses with unread messages
GET /api/v1/sponsorship/analyses?hasUnreadMessages=true

# Get active conversations sorted by last message
GET /api/v1/sponsorship/analyses?filterByMessageStatus=active&sortBy=lastMessage&sortOrder=desc

# Get not contacted (opportunities)
GET /api/v1/sponsorship/analyses?filterByMessageStatus=notContacted

# Get analyses with 3+ unread messages
GET /api/v1/sponsorship/analyses?unreadMessagesMin=3
```

---

**Questions or concerns?** Contact backend team for technical clarification.
