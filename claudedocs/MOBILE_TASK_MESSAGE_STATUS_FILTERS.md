# 📱 Mobile Task: Message Status Filters Implementation

**Date:** 2025-10-22  
**Priority:** High  
**Estimated Time:** 8-10 hours  
**For:** Flutter Mobile Team  
**Backend Status:** ✅ Ready on Staging

---

## 🎯 Task Overview

**Goal:** Implement message status filtering in the sponsored analyses list screen to allow sponsors to filter analyses by messaging activity.

**Business Value:**
- Sponsors can see which analyses they've already contacted
- Sponsors can find new opportunities (not contacted analyses)
- Sponsors can prioritize responses (analyses with farmer replies)
- Improved sponsor engagement and user experience

---

## ✅ Backend Status

**Environment:** Staging  
**Base URL:** `https://ziraai-api-sit.up.railway.app/api/v1`  
**Endpoint:** `GET /api/v1/sponsorship/analyses`  
**Status:** ✅ Implemented and tested

**Breaking Changes:** NONE - Fully backward compatible

---

## 📋 Required UI Changes

### 1. Filter Chips/Tabs (Priority 1)

Add filter options for sponsors to toggle between different message statuses:

**Filter Options:**

| Filter | Label (Turkish) | Label (English) | Description |
|--------|----------------|-----------------|-------------|
| All | Tümü | All | No filter (default) |
| Contacted | Mesaj Gönderilen | Contacted | Analyses where sponsor sent message |
| Not Contacted | Mesaj Gönderilmeyen | Not Contacted | Analyses not yet contacted |
| Has Response | Cevap Alanlar | Has Response | Farmer replied to sponsor |
| No Response | Cevap Bekleyenler | No Response | Waiting for farmer reply |
| Active | Aktif Konuşmalar | Active | Recent conversation (< 7 days) |
| Idle | Pasif Konuşmalar | Idle | Old conversation (≥ 7 days) |

**UI Component:** Horizontal scrollable chip list or segmented control

**Design:**
```
┌─────────────────────────────────────────────────────────┐
│  [Tümü] [Mesaj Gönderilen] [Mesaj Gönderilmeyen]       │
│  [Cevap Alanlar] [Cevap Bekleyenler] [Aktif] [Pasif]   │
└─────────────────────────────────────────────────────────┘
```

**Selected State:** Blue background, white text  
**Unselected State:** White background, gray border, dark text

---

### 2. Analysis Card Updates (Priority 2)

Add messaging status indicators to each analysis card:

**New Elements:**

1. **Message Icon Badge** (if has messages)
   - Icon: Message bubble icon
   - Badge: Show unread count if > 0
   - Position: Top right corner of card
   - Color: Blue if unread, gray if all read

2. **Status Label** (optional, if space allows)
   - Text: Conversation status
   - Position: Below analysis date
   - Color: 
     - Green for "Active"
     - Orange for "Pending"
     - Gray for "Idle"
     - Transparent for "No Contact"

3. **Last Message Preview** (if has messages)
   - Text: Last message text (truncated to 50 chars)
   - Position: Below crop type
   - Format: "Son mesaj: {preview}..." or "Last message: {preview}..."

**Example Card Layout:**
```
┌─────────────────────────────────────────┐
│  Domates Analizi          [💬 2]        │
│  Sağlık Skoru: 85%                      │
│  Tarih: 15 Ekim 2025                    │
│  🟢 Aktif Konuşma                        │
│  Son mesaj: Teşekkürler, çok yardımcı...│
└─────────────────────────────────────────┘
```

---

### 3. Summary Statistics (Priority 3)

Add messaging statistics to the screen header/summary section:

**New Statistics:**
- Total contacted: X analyses
- Not contacted: X analyses  
- Active conversations: X
- Unread messages: X total

**UI Component:** Info cards or statistics row

**Example:**
```
┌──────────────────────────────────────────────────────┐
│  Mesaj Gönderilen: 87  │  Gönderilmeyen: 58         │
│  Aktif Konuşma: 23     │  Okunmamış: 15             │
└──────────────────────────────────────────────────────┘
```

---

## 🔌 API Integration

### Endpoint Details

**URL:** `GET /api/v1/sponsorship/analyses`

**Authentication:** Bearer token (Sponsor role)

### Query Parameters

**Existing (keep as is):**
- `page` (integer, default: 1)
- `pageSize` (integer, default: 20)
- `sortBy` (string, default: "date")
- `sortOrder` (string, default: "desc")
- `filterByTier` (string, optional: "S", "M", "L", "XL")
- `filterByCropType` (string, optional)
- `startDate` (datetime, optional)
- `endDate` (datetime, optional)

**NEW Parameters to implement:**
- `filterByMessageStatus` (string, optional) - Values: `contacted`, `notContacted`, `hasResponse`, `noResponse`, `active`, `idle`
- `hasUnreadMessages` (boolean, optional) - Filter only analyses with unread messages
- `unreadMessagesMin` (integer, optional) - Filter analyses with at least X unread messages

### Request Examples

**1. Show all contacted analyses:**
```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=contacted&page=1&pageSize=20
```

**2. Show not contacted (opportunities):**
```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=notContacted&page=1&pageSize=20
```

**3. Show analyses with farmer responses:**
```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=hasResponse&page=1&pageSize=20
```

**4. Show analyses waiting for farmer reply:**
```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=noResponse&page=1&pageSize=20
```

**5. Show active conversations (< 7 days):**
```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=active&page=1&pageSize=20
```

**6. Show idle conversations (≥ 7 days):**
```http
GET /api/v1/sponsorship/analyses?filterByMessageStatus=idle&page=1&pageSize=20
```

**7. Show only analyses with unread messages:**
```http
GET /api/v1/sponsorship/analyses?hasUnreadMessages=true&page=1&pageSize=20
```

**8. Show analyses with 3+ unread messages:**
```http
GET /api/v1/sponsorship/analyses?unreadMessagesMin=3&page=1&pageSize=20
```

---

## 📊 Response Format

### New Fields in Response

**Each analysis item now includes `messagingStatus` object:**

```json
{
  "id": 123,
  "cropType": "Domates",
  "healthScore": 85,
  "analysisDate": "2025-10-15T10:30:00",
  "canMessage": true,
  "messagingStatus": {
    "hasMessages": true,
    "totalMessageCount": 5,
    "unreadCount": 2,
    "lastMessageDate": "2025-10-19T14:20:00",
    "lastMessagePreview": "Teşekkürler, çok yardımcı oldu...",
    "lastMessageBy": "farmer",
    "hasFarmerResponse": true,
    "lastFarmerResponseDate": "2025-10-19T14:20:00",
    "conversationStatus": "Active"
  }
}
```

**Summary object includes new messaging statistics:**

```json
{
  "summary": {
    "totalAnalyses": 145,
    "totalPages": 8,
    "currentPage": 1,
    "contactedAnalyses": 87,
    "notContactedAnalyses": 58,
    "activeConversations": 23,
    "pendingResponses": 42,
    "totalUnreadMessages": 15
  }
}
```

---

## 📝 Implementation Steps

### Phase 1: API Layer (2-3 hours)

1. **Update API Service**
   - Add new query parameters to `getSponsoredAnalyses()` method
   - Add `filterByMessageStatus`, `hasUnreadMessages`, `unreadMessagesMin`
   - Test API calls with new parameters

2. **Update Models**
   - Add `MessagingStatus` model class
   - Add `messagingStatus` field to `SponsoredAnalysis` model
   - Add new fields to `AnalysesSummary` model:
     - `contactedAnalyses`
     - `notContactedAnalyses`
     - `activeConversations`
     - `pendingResponses`
     - `totalUnreadMessages`

3. **Test API Integration**
   - Test each filter value
   - Verify response parsing
   - Handle null cases gracefully

### Phase 2: UI Components (3-4 hours)

1. **Create Filter Chips Component**
   - Horizontal scrollable chip list
   - Selected/unselected states
   - Handle tap events
   - Update filter state

2. **Update Analysis Card**
   - Add message icon badge
   - Add unread count badge
   - Add status label
   - Add last message preview
   - Handle null messaging status

3. **Create Summary Statistics Widget**
   - Display messaging stats
   - Responsive layout
   - Update on filter change

### Phase 3: State Management (2-3 hours)

1. **Add Filter State**
   - Add `messageStatusFilter` to state
   - Add `hasUnreadMessagesFilter` to state
   - Add `unreadMessagesMinFilter` to state

2. **Implement Filter Logic**
   - Handle filter chip selection
   - Update API call with selected filters
   - Reset pagination on filter change
   - Show loading state during filter

3. **Persist Filters** (optional)
   - Save selected filter to SharedPreferences
   - Restore filter on screen reload

### Phase 4: Testing & Polish (1-2 hours)

1. **Test All Filters**
   - Test each filter value individually
   - Test combined filters
   - Test edge cases (no data, empty lists)

2. **UI/UX Polish**
   - Smooth animations
   - Loading states
   - Error states
   - Empty states with contextual messages

3. **Accessibility**
   - Screen reader labels
   - Touch target sizes
   - Color contrast

---

## 🎨 Design Specifications

### Colors

**Filter Chips:**
- Selected: `#2196F3` (Blue 500)
- Unselected: `#FFFFFF` with `#E0E0E0` border
- Text Selected: `#FFFFFF`
- Text Unselected: `#424242`

**Status Labels:**
- Active: `#4CAF50` (Green 500)
- Pending: `#FF9800` (Orange 500)
- Idle: `#9E9E9E` (Gray 500)
- No Contact: Transparent

**Unread Badge:**
- Background: `#F44336` (Red 500)
- Text: `#FFFFFF`
- Min width: 20dp
- Padding: 4dp horizontal

### Typography

**Filter Chips:**
- Font: Medium
- Size: 14sp
- Letter spacing: 0.25

**Status Labels:**
- Font: Regular
- Size: 12sp
- Letter spacing: 0.4

**Message Preview:**
- Font: Regular
- Size: 13sp
- Color: `#757575` (Gray 600)
- Max lines: 1
- Overflow: Ellipsis

---

## 🧪 Testing Checklist

### API Testing

- [ ] Test with `filterByMessageStatus=contacted`
- [ ] Test with `filterByMessageStatus=notContacted`
- [ ] Test with `filterByMessageStatus=hasResponse`
- [ ] Test with `filterByMessageStatus=noResponse`
- [ ] Test with `filterByMessageStatus=active`
- [ ] Test with `filterByMessageStatus=idle`
- [ ] Test with `hasUnreadMessages=true`
- [ ] Test with `unreadMessagesMin=5`
- [ ] Test combined filters (e.g., contacted + hasUnread)
- [ ] Test with no filters (default behavior)
- [ ] Verify pagination works with filters
- [ ] Verify sorting works with filters

### UI Testing

- [ ] Filter chips display correctly
- [ ] Selected filter highlights properly
- [ ] Tapping filter updates list
- [ ] Message icon shows on cards with messages
- [ ] Unread badge shows correct count
- [ ] Status label shows correct status
- [ ] Message preview truncates long text
- [ ] Summary statistics update on filter change
- [ ] Loading state shows during API call
- [ ] Empty state shows when no results
- [ ] Error state shows on API failure

### Edge Cases

- [ ] Analyses with no messages (messagingStatus is null)
- [ ] Analyses with 0 unread messages
- [ ] Very long message previews
- [ ] Empty list after filtering
- [ ] Network timeout during filter
- [ ] Filter while list is loading

---

## 📚 Reference Documentation

**Complete API Documentation:**
- `claudedocs/MESSAGING_STATUS_MOBILE_API_GUIDE.md` (1054 lines)

**Backend Implementation:**
- `claudedocs/MESSAGING_STATUS_IMPLEMENTATION_COMPLETE.md`

**Testing Collection:**
- `claudedocs/ZiraAI_Messaging_Status_Postman_Collection.json`

---

## 🚀 Acceptance Criteria

### Must Have
- ✅ Filter chips visible and functional
- ✅ All 6 filter options work correctly
- ✅ Message icon shows on cards with messages
- ✅ Unread badge shows correct count
- ✅ API integration with new parameters
- ✅ Pagination works with filters
- ✅ Summary statistics display correctly

### Nice to Have
- ✅ Status label on each card
- ✅ Last message preview
- ✅ Smooth animations
- ✅ Filter persistence
- ✅ Pull to refresh updates filters

---

## 🐛 Known Issues & Solutions

### Issue 1: messagingStatus is null
**Cause:** Analysis has no messages yet  
**Solution:** Check for null before accessing properties
```dart
if (analysis.messagingStatus != null) {
  // Show message icon and details
}
```

### Issue 2: Empty list after filtering
**Cause:** No analyses match selected filter  
**Solution:** Show contextual empty state:
- "Henüz mesaj gönderilmemiş analiz yok"
- "Tüm çiftçilerle iletişim kurulmuş"
- etc.

### Issue 3: Filter + pagination performance
**Cause:** API fetches filtered data efficiently  
**Solution:** No action needed - backend optimized

---

## 📞 Support

**Questions?** Contact backend team:
- API endpoint issues
- Response format questions
- Performance concerns

**Backend Team Contact:**
- Staging URL: `https://ziraai-api-sit.up.railway.app`
- Postman Collection: Available in `claudedocs/`

---

## ✅ Definition of Done

- [ ] All 6 filter options implemented
- [ ] Message icons visible on cards
- [ ] Unread badges show correct counts
- [ ] Summary statistics display
- [ ] All tests pass
- [ ] Code reviewed and approved
- [ ] QA tested on staging
- [ ] Documentation updated
- [ ] Ready for production release

---

**Status:** 🟢 Backend Ready - Ready for Mobile Implementation  
**Environment:** Staging  
**Timeline:** Sprint XX (TBD)
