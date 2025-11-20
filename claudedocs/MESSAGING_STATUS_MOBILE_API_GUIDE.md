# 📱 Messaging Status API - Mobile Integration Guide

**Date:** 2025-10-21
**Version:** 1.0
**For:** Mobile Development Team (Flutter)
**API Version:** v1
**Base URL:** `https://ziraai-api-sit.up.railway.app/api/v1` (Staging)

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [What's New](#whats-new)
3. [Breaking Changes](#breaking-changes)
4. [Updated Endpoint](#updated-endpoint)
5. [Request Examples](#request-examples)
6. [Response Examples](#response-examples)
7. [Filter Values Reference](#filter-values-reference)
8. [Conversation Status Logic](#conversation-status-logic)
9. [UI/UX Recommendations](#uiux-recommendations)
10. [Error Handling](#error-handling)
11. [Testing Scenarios](#testing-scenarios)

---

## 🎯 Overview

The sponsor analysis list endpoint has been enhanced with **messaging status tracking and filtering**. This allows sponsors to:

- ✅ See which analyses they've contacted
- ✅ Filter analyses by messaging status
- ✅ View unread message counts
- ✅ Track farmer responses
- ✅ Prioritize active conversations

**⚠️ IMPORTANT:** This is a **NON-BREAKING** change. All existing requests will continue to work. New fields are added to the response.

---

## 🆕 What's New

### New Query Parameters (All Optional)

| Parameter | Type | Description |
|-----------|------|-------------|
| `filterByMessageStatus` | string | Filter by conversation status |
| `hasUnreadMessages` | boolean | Show only analyses with unread messages |
| `unreadMessagesMin` | integer | Show analyses with X+ unread messages |

### New Response Fields

**Added to each analysis item:**
- `messagingStatus` - Complete messaging information object

**Added to summary:**
- `contactedAnalyses` - Count of analyses with messages
- `notContactedAnalyses` - Count of analyses without messages
- `activeConversations` - Count of active conversations
- `pendingResponses` - Count waiting for farmer reply
- `totalUnreadMessages` - Total unread count across all

---

## 🚫 Breaking Changes

**NONE!** This update is 100% backward compatible.

- ✅ Existing requests work without changes
- ✅ All old fields remain in response
- ✅ New fields are added (not replaced)
- ✅ Default behavior unchanged when no filters applied

---

## 🔌 Updated Endpoint

### GET /api/v1/sponsorship/analyses

**Purpose:** Get paginated list of sponsored analyses with messaging status

**Authorization:** Required - Bearer Token (Sponsor or Admin role)

**Method:** `GET`

**Full URL:**
```
https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/analyses
```

---

## 📊 Request Parameters

### Existing Parameters (Unchanged)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | integer | 1 | Page number |
| `pageSize` | integer | 20 | Items per page (max: 100) |
| `sortBy` | string | "date" | Sort field: `date`, `healthScore`, `cropType` |
| `sortOrder` | string | "desc" | Sort order: `asc`, `desc` |
| `filterByTier` | string | null | Filter by tier: `S`, `M`, `L`, `XL` |
| `filterByCropType` | string | null | Filter by crop type |
| `startDate` | datetime | null | Filter by start date (ISO 8601) |
| `endDate` | datetime | null | Filter by end date (ISO 8601) |

### NEW Parameters ⭐

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `filterByMessageStatus` | string | null | Filter by message status (see values below) |
| `hasUnreadMessages` | boolean | null | If `true`, show only analyses with unread messages |
| `unreadMessagesMin` | integer | null | Show analyses with at least X unread messages |

---

## 📝 Request Examples

### Example 1: Basic Request (No Changes Needed)

```http
GET /api/v1/sponsorship/analyses?page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Response:** Same as before, but with added `messagingStatus` field in each item

---

### Example 2: Filter - Show Only Contacted Analyses

```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=contacted&page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Use Case:** "Show me all analyses I've already messaged"

**Returns:** Only analyses where sponsor has sent at least one message

---

### Example 3: Filter - Show Not Contacted (Opportunities)

```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=notContacted&page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Use Case:** "Show me analyses I haven't contacted yet"

**Returns:** Only analyses with no messages sent by sponsor

---

### Example 4: Filter - Show Analyses with Farmer Response

```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=hasResponse&page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Use Case:** "Show me analyses where farmers replied to me"

**Returns:** Only analyses where farmer has sent at least one message back

---

### Example 5: Filter - Show Pending Responses

```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=noResponse&page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Use Case:** "Show me analyses where I sent a message but farmer hasn't replied"

**Returns:** Only analyses where sponsor sent message but no farmer reply yet

---

### Example 6: Filter - Show Active Conversations

```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=active&page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Use Case:** "Show me recent active conversations (within 7 days)"

**Returns:** Only analyses with two-way conversation and last message within 7 days

---

### Example 7: Filter - Show Idle Conversations

```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=idle&page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Use Case:** "Show me conversations that went quiet (7+ days)"

**Returns:** Only analyses with two-way conversation but no recent activity

---

### Example 8: Filter - Show Analyses with Unread Messages

```http
GET /api/v1/sponsorship/analyses?hasUnreadMessages=true&page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Use Case:** "Show me analyses where I have unread messages from farmers"

**Returns:** Only analyses with unread messages (any count)

---

### Example 9: Filter - Show Analyses with 3+ Unread Messages

```http
GET /api/v1/sponsorship/analyses?unreadMessagesMin=3&page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Use Case:** "Show me analyses with many unread messages (high priority)"

**Returns:** Only analyses with at least 3 unread messages

---

### Example 10: Combined Filters

```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=active&hasUnreadMessages=true&sortBy=date&sortOrder=desc&page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Use Case:** "Show me active conversations with unread messages, newest first"

**Returns:** Active conversations with unread messages, sorted by date descending

---

### Example 11: Filter by Tier + Message Status

```http
GET /api/v1/sponsorship/analyses?filterByTier=XL&filterByMessageStatus=notContacted&page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Use Case:** "Show me XL tier analyses I haven't contacted yet"

**Returns:** XL tier analyses with no messages

---

### Example 12: Date Range + Unread Filter

```http
GET /api/v1/sponsorship/analyses?startDate=2025-10-01&endDate=2025-10-21&hasUnreadMessages=true&page=1&pageSize=20
Authorization: Bearer {your-token}
```

**Use Case:** "Show me analyses from October with unread messages"

**Returns:** October analyses with unread messages

---

## 📤 Response Structure

### Response Schema

```json
{
  "data": {
    "items": [
      {
        // EXISTING FIELDS (unchanged)
        "analysisId": 123,
        "analysisDate": "2025-10-15T10:30:00Z",
        "analysisStatus": "Completed",
        "cropType": "Tomato",
        "overallHealthScore": 85.5,
        "plantSpecies": "Solanum lycopersicum",
        "plantVariety": "Cherry Tomato",
        "growthStage": "Flowering",
        "imageUrl": "https://...",
        "vigorScore": 80.0,
        "healthSeverity": "Moderate",
        "primaryConcern": "Nutrient deficiency",
        "location": "Antalya",
        "farmerName": "Ahmet Yılmaz",
        "farmerPhone": "+905551234567",
        "farmerEmail": "ahmet@example.com",
        "tierName": "XL",
        "accessPercentage": 100,
        "canMessage": true,
        "canViewLogo": true,
        "sponsorInfo": {
          "sponsorId": 200,
          "companyName": "AgriTech Solutions",
          "logoUrl": "https://...",
          "websiteUrl": "https://..."
        },

        // NEW FIELD ⭐
        "messagingStatus": {
          "hasMessages": true,
          "totalMessageCount": 5,
          "unreadCount": 2,
          "lastMessageDate": "2025-10-19T14:20:00Z",
          "lastMessagePreview": "Teşekkürler, tavsiyelerinizi uygulayacağım...",
          "lastMessageBy": "farmer",
          "hasFarmerResponse": true,
          "lastFarmerResponseDate": "2025-10-19T14:20:00Z",
          "conversationStatus": "Active"
        }
      }
    ],
    "totalCount": 145,
    "page": 1,
    "pageSize": 20,
    "totalPages": 8,
    "hasNextPage": true,
    "hasPreviousPage": false,

    // EXISTING SUMMARY FIELDS
    "summary": {
      "totalAnalyses": 145,
      "averageHealthScore": 78.5,
      "topCropTypes": ["Tomato", "Wheat", "Pepper", "Corn", "Cucumber"],
      "analysesThisMonth": 42,

      // NEW SUMMARY FIELDS ⭐
      "contactedAnalyses": 87,
      "notContactedAnalyses": 58,
      "activeConversations": 23,
      "pendingResponses": 42,
      "totalUnreadMessages": 15
    }
  },
  "success": true,
  "message": "Retrieved 20 analyses (page 1 of 8)"
}
```

---

## 📦 MessagingStatus Object

### Field Descriptions

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `hasMessages` | boolean | Whether any messages exist in this conversation | `true` |
| `totalMessageCount` | integer | Total messages exchanged (both directions) | `5` |
| `unreadCount` | integer | Number of unread messages from farmer to sponsor | `2` |
| `lastMessageDate` | datetime | Date/time of most recent message (either direction) | `"2025-10-19T14:20:00Z"` |
| `lastMessagePreview` | string | Preview of last message (first 50 chars) | `"Teşekkürler, tavsiyelerinizi..."` |
| `lastMessageBy` | string | Who sent the last message: `"sponsor"` or `"farmer"` | `"farmer"` |
| `hasFarmerResponse` | boolean | Whether farmer has sent at least one reply | `true` |
| `lastFarmerResponseDate` | datetime | Date/time of farmer's most recent message | `"2025-10-19T14:20:00Z"` |
| `conversationStatus` | string | Current status: `"NoContact"`, `"Pending"`, `"Active"`, `"Idle"` | `"Active"` |

---

## 📋 Filter Values Reference

### filterByMessageStatus Values

| Value | Description | Logic |
|-------|-------------|-------|
| `contacted` | Analyses where sponsor has sent at least one message | `hasMessages == true` |
| `notContacted` | Analyses where sponsor has never messaged | `hasMessages == false` |
| `hasResponse` | Farmer has replied to sponsor's message | `hasFarmerResponse == true` |
| `noResponse` | Sponsor messaged but no farmer reply | `hasMessages == true && hasFarmerResponse == false` |
| `active` | Recent conversation (last message < 7 days) | `conversationStatus == "Active"` |
| `idle` | Conversation exists but no recent activity (≥ 7 days) | `conversationStatus == "Idle"` |

### Conversation Status Values

| Status | Description | Condition |
|--------|-------------|-----------|
| `NoContact` | No messages sent yet | `totalMessageCount == 0` |
| `Pending` | Sponsor sent message, waiting for farmer reply | `totalMessageCount > 0 && !hasFarmerResponse` |
| `Active` | Two-way conversation, recent activity (< 7 days) | `hasFarmerResponse && daysSince < 7` |
| `Idle` | Conversation exists but no recent activity (≥ 7 days) | `hasFarmerResponse && daysSince >= 7` |

---

## 🧮 Conversation Status Logic

### Calculation Algorithm

```dart
ConversationStatus calculateStatus(MessagingStatus status) {
  if (status.totalMessageCount == 0) {
    return ConversationStatus.noContact;
  }

  if (!status.hasFarmerResponse) {
    return ConversationStatus.pending;
  }

  final daysSince = DateTime.now().difference(status.lastMessageDate).inDays;

  if (daysSince < 7) {
    return ConversationStatus.active;
  } else {
    return ConversationStatus.idle;
  }
}
```

---

## 🎨 UI/UX Recommendations

### 1. List Item Design

**Suggested Layout:**

```
┌─────────────────────────────────────────────────┐
│ 🌱 Tomato - 85% Health             [2 Unread]  │
│ Ahmet Yılmaz • Oct 15, 2025                    │
│ 💬 Active • 5 messages                         │
│ "Teşekkürler, tavsiyelerinizi..."              │
└─────────────────────────────────────────────────┘
```

**Components:**
- **Unread Badge:** Red badge with count (top-right)
- **Status Indicator:** Color-coded chip
  - 🟢 Green = Active
  - 🟡 Yellow = Pending
  - 🔵 Blue = Not Contacted
  - ⚪ Gray = Idle
- **Message Preview:** Italic, gray text
- **Message Count:** Icon + count

### 2. Filter Chips (Top of List)

**Suggested Filters:**

```
┌──────────────────────────────────────────────┐
│ [All ▼] [Not Contacted] [Unread (15)]       │
│ [Active] [Pending] [Has Response]           │
└──────────────────────────────────────────────┘
```

**Quick Filters:**
1. **All** - No filter (default)
2. **Not Contacted** - `filterByMessageStatus=notContacted`
3. **Unread (count)** - `hasUnreadMessages=true`
4. **Active** - `filterByMessageStatus=active`
5. **Pending** - `filterByMessageStatus=noResponse`
6. **Has Response** - `filterByMessageStatus=hasResponse`

### 3. Visual Indicators

**Status Badges:**
- 🟢 **Active** - Green badge
- 🟡 **Pending** - Yellow badge
- 🔵 **New** - Blue badge (not contacted)
- ⚪ **Idle** - Gray badge

**Unread Count:**
- Red circular badge with number
- Position: Top-right of card
- Font: Bold, white text

**Message Preview:**
- Font: Italic, 14sp
- Color: Gray (#757575)
- Max length: 50 characters + "..."

### 4. Sort Options

**Suggested Sort Menu:**

```
Sort by: [Most Recent ▼]
  - Most Recent Activity (NEW)
  - Unread First (NEW)
  - Not Contacted First (NEW)
  - Health Score (High to Low)
  - Health Score (Low to High)
  - Analysis Date (Newest)
  - Analysis Date (Oldest)
```

### 5. Empty States

**No Contacted Analyses:**
```
┌─────────────────────────────────┐
│        📭                       │
│   No Contacted Analyses Yet     │
│                                 │
│  Start messaging farmers to     │
│  build relationships!           │
│                                 │
│  [Browse Not Contacted]         │
└─────────────────────────────────┘
```

**No Unread Messages:**
```
┌─────────────────────────────────┐
│        ✅                       │
│     All Caught Up!              │
│                                 │
│  You have no unread messages    │
└─────────────────────────────────┘
```

---

## 🎯 Use Cases & Filter Combinations

### Use Case 1: Daily Check - Unread Messages

**User Story:** "As a sponsor, I want to see my unread messages each morning"

**Filter:**
```http
?hasUnreadMessages=true&sortBy=date&sortOrder=desc
```

**UI Action:** "Unread" filter chip (shows count)

---

### Use Case 2: Find New Opportunities

**User Story:** "As a sponsor, I want to find analyses I haven't contacted yet"

**Filter:**
```http
?filterByMessageStatus=notContacted&sortBy=healthScore&sortOrder=asc
```

**UI Action:** "Not Contacted" filter chip + sort by health score (low first)

---

### Use Case 3: Follow Up on Pending

**User Story:** "As a sponsor, I want to follow up on messages where farmers haven't replied"

**Filter:**
```http
?filterByMessageStatus=noResponse&sortBy=date&sortOrder=asc
```

**UI Action:** "Pending" filter chip + sort by oldest first

---

### Use Case 4: Prioritize Active Conversations

**User Story:** "As a sponsor, I want to focus on my active conversations"

**Filter:**
```http
?filterByMessageStatus=active&hasUnreadMessages=true
```

**UI Action:** "Active" filter chip + "Unread" filter chip

---

### Use Case 5: Re-engage Idle Conversations

**User Story:** "As a sponsor, I want to re-engage conversations that went quiet"

**Filter:**
```http
?filterByMessageStatus=idle&sortBy=date&sortOrder=asc
```

**UI Action:** "Idle" filter chip + sort by oldest first

---

## ⚠️ Error Handling

### Possible Errors

#### 1. Unauthorized (401)

```json
{
  "success": false,
  "message": "User not authenticated"
}
```

**Cause:** Missing or invalid token
**Action:** Refresh token or re-login

---

#### 2. Forbidden (403)

```json
{
  "success": false,
  "message": "Access denied. Sponsor role required."
}
```

**Cause:** User is not a sponsor
**Action:** Show error message, redirect to appropriate screen

---

#### 3. Bad Request (400)

```json
{
  "success": false,
  "message": "Page must be greater than 0"
}
```

**Possible Causes:**
- Invalid page number (< 1)
- Invalid pageSize (< 1 or > 100)
- Invalid date format

**Action:** Validate input before sending

---

#### 4. Internal Server Error (500)

```json
{
  "success": false,
  "message": "An error occurred while processing your request"
}
```

**Cause:** Server-side error
**Action:** Show generic error message, retry after delay

---

## 🧪 Testing Scenarios

### Test Case 1: Backward Compatibility

**Objective:** Verify existing requests still work

**Request:**
```http
GET /api/v1/sponsorship/analyses?page=1&pageSize=20
```

**Expected:**
- ✅ Status 200
- ✅ All old fields present
- ✅ New `messagingStatus` field present
- ✅ New summary fields present

---

### Test Case 2: Filter - Contacted

**Objective:** Verify contacted filter works

**Setup:**
- User has 10 analyses
- User messaged 6 of them

**Request:**
```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=contacted
```

**Expected:**
- ✅ Status 200
- ✅ Returns 6 items
- ✅ All items have `messagingStatus.hasMessages = true`

---

### Test Case 3: Filter - Not Contacted

**Objective:** Verify not contacted filter works

**Setup:** Same as Test Case 2

**Request:**
```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=notContacted
```

**Expected:**
- ✅ Status 200
- ✅ Returns 4 items
- ✅ All items have `messagingStatus.hasMessages = false`

---

### Test Case 4: Unread Count

**Objective:** Verify unread count is correct

**Setup:**
- Analysis #123 has 3 unread messages from farmer

**Request:**
```http
GET /api/v1/sponsorship/analyses
```

**Expected:**
- ✅ Analysis #123 has `messagingStatus.unreadCount = 3`

---

### Test Case 5: Conversation Status - Active

**Objective:** Verify active status is correct

**Setup:**
- Analysis #123 has two-way conversation
- Last message was 2 days ago

**Request:**
```http
GET /api/v1/sponsorship/analyses
```

**Expected:**
- ✅ Analysis #123 has `messagingStatus.conversationStatus = "Active"`

---

### Test Case 6: Conversation Status - Idle

**Objective:** Verify idle status is correct

**Setup:**
- Analysis #124 has two-way conversation
- Last message was 10 days ago

**Request:**
```http
GET /api/v1/sponsorship/analyses
```

**Expected:**
- ✅ Analysis #124 has `messagingStatus.conversationStatus = "Idle"`

---

### Test Case 7: Summary Statistics

**Objective:** Verify summary statistics are correct

**Setup:**
- Total: 100 analyses
- Contacted: 60
- Active: 20
- Pending: 30
- Unread total: 45

**Request:**
```http
GET /api/v1/sponsorship/analyses
```

**Expected:**
```json
{
  "summary": {
    "contactedAnalyses": 60,
    "notContactedAnalyses": 40,
    "activeConversations": 20,
    "pendingResponses": 30,
    "totalUnreadMessages": 45
  }
}
```

---

### Test Case 8: Combined Filters

**Objective:** Verify multiple filters work together

**Request:**
```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=active&hasUnreadMessages=true
```

**Expected:**
- ✅ Returns only active conversations with unread messages
- ✅ All items have `conversationStatus = "Active"`
- ✅ All items have `unreadCount > 0`

---

## 📊 Performance Expectations

### Response Times

| Scenario | Expected Response Time |
|----------|------------------------|
| < 100 analyses | < 500ms |
| 100-500 analyses | < 1s |
| 500-1000 analyses | < 2s |
| With filters | +10-20% |

### Pagination Recommendations

- Default page size: 20
- Recommended max: 50
- Hard limit: 100

### Caching Strategy

**Client-side caching:**
- Cache duration: 5 minutes
- Invalidate on:
  - New message sent
  - Message read
  - Pull-to-refresh

---

## 🔄 Migration Guide

### For Existing Implementations

**Step 1: Update Model**

```dart
class SponsoredAnalysisSummary {
  // ... existing fields ...

  // ADD THIS ⭐
  final MessagingStatus? messagingStatus;

  SponsoredAnalysisSummary.fromJson(Map<String, dynamic> json)
    : // ... existing field parsing ...
      messagingStatus = json['messagingStatus'] != null
          ? MessagingStatus.fromJson(json['messagingStatus'])
          : null;
}

// NEW MODEL ⭐
class MessagingStatus {
  final bool hasMessages;
  final int totalMessageCount;
  final int unreadCount;
  final DateTime? lastMessageDate;
  final String? lastMessagePreview;
  final String? lastMessageBy;
  final bool hasFarmerResponse;
  final DateTime? lastFarmerResponseDate;
  final String conversationStatus;

  MessagingStatus.fromJson(Map<String, dynamic> json)
    : hasMessages = json['hasMessages'] ?? false,
      totalMessageCount = json['totalMessageCount'] ?? 0,
      unreadCount = json['unreadCount'] ?? 0,
      lastMessageDate = json['lastMessageDate'] != null
          ? DateTime.parse(json['lastMessageDate'])
          : null,
      lastMessagePreview = json['lastMessagePreview'],
      lastMessageBy = json['lastMessageBy'],
      hasFarmerResponse = json['hasFarmerResponse'] ?? false,
      lastFarmerResponseDate = json['lastFarmerResponseDate'] != null
          ? DateTime.parse(json['lastFarmerResponseDate'])
          : null,
      conversationStatus = json['conversationStatus'] ?? 'NoContact';
}
```

**Step 2: Update Summary Model**

```dart
class AnalysesSummary {
  // ... existing fields ...

  // ADD THESE ⭐
  final int contactedAnalyses;
  final int notContactedAnalyses;
  final int activeConversations;
  final int pendingResponses;
  final int totalUnreadMessages;

  AnalysesSummary.fromJson(Map<String, dynamic> json)
    : // ... existing field parsing ...
      contactedAnalyses = json['contactedAnalyses'] ?? 0,
      notContactedAnalyses = json['notContactedAnalyses'] ?? 0,
      activeConversations = json['activeConversations'] ?? 0,
      pendingResponses = json['pendingResponses'] ?? 0,
      totalUnreadMessages = json['totalUnreadMessages'] ?? 0;
}
```

**Step 3: Update API Service**

```dart
class SponsorshipApiService {
  Future<AnalysesResponse> getAnalyses({
    int page = 1,
    int pageSize = 20,
    String? sortBy,
    String? sortOrder,
    String? filterByTier,
    String? filterByCropType,
    DateTime? startDate,
    DateTime? endDate,
    // NEW PARAMETERS ⭐
    String? filterByMessageStatus,
    bool? hasUnreadMessages,
    int? unreadMessagesMin,
  }) async {
    final queryParams = <String, dynamic>{
      'page': page,
      'pageSize': pageSize,
      if (sortBy != null) 'sortBy': sortBy,
      if (sortOrder != null) 'sortOrder': sortOrder,
      if (filterByTier != null) 'filterByTier': filterByTier,
      if (filterByCropType != null) 'filterByCropType': filterByCropType,
      if (startDate != null) 'startDate': startDate.toIso8601String(),
      if (endDate != null) 'endDate': endDate.toIso8601String(),
      // NEW ⭐
      if (filterByMessageStatus != null) 'filterByMessageStatus': filterByMessageStatus,
      if (hasUnreadMessages != null) 'hasUnreadMessages': hasUnreadMessages,
      if (unreadMessagesMin != null) 'unreadMessagesMin': unreadMessagesMin,
    };

    final response = await dio.get('/sponsorship/analyses', queryParameters: queryParams);
    return AnalysesResponse.fromJson(response.data);
  }
}
```

**Step 4: Update UI (Optional - New Features)**

```dart
// Show unread badge
if (analysis.messagingStatus?.unreadCount ?? 0 > 0) {
  Badge(
    label: Text('${analysis.messagingStatus!.unreadCount}'),
    backgroundColor: Colors.red,
  )
}

// Show status indicator
Widget buildStatusChip(String status) {
  final config = {
    'Active': (color: Colors.green, icon: Icons.chat_bubble),
    'Pending': (color: Colors.orange, icon: Icons.schedule),
    'NoContact': (color: Colors.blue, icon: Icons.mail_outline),
    'Idle': (color: Colors.grey, icon: Icons.chat_bubble_outline),
  };

  final c = config[status] ?? config['NoContact']!;

  return Chip(
    avatar: Icon(c.icon, color: c.color, size: 16),
    label: Text(status),
    backgroundColor: c.color.withOpacity(0.1),
  );
}

// Show message preview
if (analysis.messagingStatus?.lastMessagePreview != null) {
  Text(
    analysis.messagingStatus!.lastMessagePreview!,
    style: TextStyle(fontStyle: FontStyle.italic, color: Colors.grey),
    maxLines: 1,
    overflow: TextOverflow.ellipsis,
  )
}
```

---

## 📞 Support & Questions

### Technical Questions
- Backend API: Contact backend team
- Implementation help: This document + Postman collection

### Reporting Issues
- API bugs: Create issue with reproduction steps
- Documentation unclear: Request clarification

### Testing Help
- Postman collection provided (see separate file)
- Staging environment: `https://ziraai-api-sit.up.railway.app`

---

## ✅ Implementation Checklist

### Backend (Already Done)
- [x] API endpoint updated
- [x] New fields added to response
- [x] Filters implemented
- [x] Database indexes optimized
- [x] Documentation created

### Mobile (To Do)
- [ ] Update models (MessagingStatus, Summary)
- [ ] Update API service with new parameters
- [ ] Test existing functionality (backward compatibility)
- [ ] Implement new UI components (badges, chips, previews)
- [ ] Add filter functionality
- [ ] Test all filter combinations
- [ ] Update error handling
- [ ] Performance testing

---

## 🎉 Summary

**What Changed:**
- ✅ New optional query parameters
- ✅ New response fields (messagingStatus, summary stats)
- ✅ No breaking changes

**What to Do:**
1. Update models to include new fields
2. Optionally implement new filter UI
3. Test backward compatibility
4. Deploy and monitor

**Timeline Estimate:**
- Model updates: 1-2 hours
- Basic UI (badges, previews): 2-3 hours
- Filter UI: 3-4 hours
- Testing: 2-3 hours
- **Total: 8-12 hours**

---

**Questions? Contact backend team or refer to Postman collection for live examples.**
