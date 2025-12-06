# 📱 Messaging API Changes - Mobile Implementation Guide

**Date**: 2025-12-04
**Target**: Mobile Development Team
**Backend Version**: After commits `56ade2f1` + `a1daf4fd`

---

## 🎯 Overview

Two critical changes have been implemented in the messaging system:

1. **Permission Fix**: Dual-role users (Farmer+Sponsor) can now send messages correctly
2. **Reverse Pagination**: Message list now returns newest messages first (WhatsApp/Telegram pattern)

---

## 📨 1. Send Message Endpoint

### Endpoint
```
POST /api/v1/sponsorship/messages
```

### ✅ What Changed: NOTHING for Mobile
The endpoint parameters and response structure **remain exactly the same**. Only backend permission logic was fixed.

### Request Body
```json
{
  "fromUserId": 190,           // Current user ID (required)
  "toUserId": 159,             // Recipient user ID (required)
  "plantAnalysisId": 196,      // Analysis ID (required)
  "message": "Aleykm selam",   // Message content (required)
  "messageType": "Information" // Optional, defaults to "Information"
}
```

### Success Response (200 OK)
```json
{
  "success": true,
  "message": "Message sent successfully",
  "data": {
    "id": 123,
    "plantAnalysisId": 196,
    "fromUserId": 190,
    "toUserId": 159,
    "message": "Aleykm selam",
    "messageType": "Information",
    "messageStatus": "Sent",
    "isRead": false,
    "sentDate": "2025-12-04T12:54:33.671Z",
    "senderRole": "Farmer",
    "senderName": "John Doe",
    "senderAvatarUrl": "https://...",
    "hasAttachments": false,
    "isVoiceMessage": false,
    "isActive": true
  }
}
```

### Error Responses

#### Before Fix (🔴 Broken)
```json
// Farmer replying to sponsor → 400 Bad Request
{
  "success": false,
  "message": "You can only message farmers for analyses done using sponsorship codes you purchased or distributed"
}
```

#### After Fix (✅ Working)
```json
// Same request → 200 OK (message sent successfully)
```

### What Was Fixed
- **Problem**: Dual-role users were blocked by duplicate validation in service layer
- **Solution**: Removed duplicate validation, kept only context-based validation in command handler
- **Impact**: Farmers can now reply to sponsors in their own analyses regardless of having Sponsor role

---

## 💬 2. Get Conversation Messages Endpoint

### Endpoint
```
GET /api/v1/sponsorship/messages/conversation
```

### 🔄 What Changed: Message Order (DESC instead of ASC)

#### Before (Old Implementation)
```
Page 1: [Message #1, Message #2, Message #3, ..., Message #20]
        ↑ Oldest                                    Newest ↑

Mobile had to:
- Scroll to bottom on page 1 to see latest message
- Load newer pages to see recent messages
- Reverse-engineer pagination logic
```

#### After (New Implementation)
```
Page 1: [Message #20, Message #19, Message #18, ..., Message #1]
        ↑ Newest                                    Oldest ↑

Mobile should:
- Show page 1 at top (latest messages visible immediately)
- Load page 2+ when user scrolls UP (to see older messages)
- Standard WhatsApp/Telegram pagination pattern
```

### Request Parameters
```
GET /api/v1/sponsorship/messages/conversation?otherUserId=159&plantAnalysisId=196&page=1&pageSize=20
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `otherUserId` | int | ✅ Yes | The other user in conversation (not current user) |
| `plantAnalysisId` | int | ✅ Yes | Analysis ID for this conversation |
| `page` | int | ❌ No | Page number (default: 1) |
| `pageSize` | int | ❌ No | Messages per page (default: 20) |

### Success Response (200 OK)
```json
{
  "success": true,
  "data": [
    {
      "id": 125,
      "plantAnalysisId": 196,
      "fromUserId": 190,
      "toUserId": 159,
      "message": "Aleykm selam",
      "messageType": "Information",
      "messageStatus": "Sent",
      "isRead": false,
      "sentDate": "2025-12-04T12:54:33.671Z",
      "senderRole": "Farmer",
      "senderName": "John Doe",
      "senderAvatarUrl": "https://...",
      "hasAttachments": false,
      "isVoiceMessage": false
    },
    {
      "id": 124,
      "plantAnalysisId": 196,
      "fromUserId": 159,
      "toUserId": 190,
      "message": "selam",
      "messageType": "Information",
      "messageStatus": "Delivered",
      "isRead": true,
      "sentDate": "2025-12-04T12:10:15.432Z",
      "readDate": "2025-12-04T12:11:00.123Z",
      "senderRole": "Sponsor",
      "senderName": "Jane Sponsor",
      "senderAvatarUrl": "https://...",
      "hasAttachments": false,
      "isVoiceMessage": false
    }
    // ... 18 more messages (newest to oldest)
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalRecords": 45,
    "totalPages": 3
  }
}
```

### Pagination Logic

#### Example Scenario: 45 Total Messages

**Page 1** (Latest Messages)
```
Messages: #45, #44, #43, ..., #26
Order: Newest → Oldest
Display: Show at top of chat
```

**Page 2** (Older Messages)
```
Messages: #25, #24, #23, ..., #6
Order: Newest → Oldest
Display: Load when scrolling UP
```

**Page 3** (Oldest Messages)
```
Messages: #5, #4, #3, #2, #1
Order: Newest → Oldest
Display: Load when scrolling UP to history
```

---

## 📱 Mobile Implementation Changes Required

### 1. Message List UI (⚠️ CHANGE REQUIRED)

#### Before (Old Logic)
```dart
// ❌ OLD: Scroll to bottom on load
void loadMessages() async {
  final messages = await getConversation(page: 1);
  setState(() {
    _messages = messages;
  });
  // Scroll to bottom to see latest message
  _scrollController.jumpTo(_scrollController.position.maxScrollExtent);
}
```

#### After (New Logic)
```dart
// ✅ NEW: Keep scroll at top (latest messages visible)
void loadMessages() async {
  final messages = await getConversation(page: 1);
  setState(() {
    _messages = messages;
  });
  // No need to scroll - latest messages already at top
}
```

### 2. Scroll Direction (⚠️ CHANGE REQUIRED)

#### Before (Old Logic)
```dart
// ❌ OLD: Scroll DOWN to load newer messages
_scrollController.addListener(() {
  if (_scrollController.position.pixels == _scrollController.position.maxScrollExtent) {
    loadNextPage(); // Load newer messages
  }
});
```

#### After (New Logic)
```dart
// ✅ NEW: Scroll UP to load older messages (WhatsApp pattern)
_scrollController.addListener(() {
  if (_scrollController.position.pixels == 0) {
    loadNextPage(); // Load older messages
  }
});
```

### 3. Message Display Order (⚠️ VERIFY)

Backend now returns messages in **descending order by sentDate**.

**Option A: Display as-is (Recommended)**
```dart
// ✅ Display messages in the order received from API
ListView.builder(
  itemCount: messages.length,
  itemBuilder: (context, index) {
    final message = messages[index]; // Already in correct order
    return MessageBubble(message: message);
  },
);
```

**Option B: Reverse for chat UI (if using bottom-up layout)**
```dart
// If your chat UI is bottom-up (like WhatsApp), reverse the list
ListView.builder(
  reverse: true, // Display from bottom to top
  itemCount: messages.length,
  itemBuilder: (context, index) {
    final message = messages[messages.length - 1 - index];
    return MessageBubble(message: message);
  },
);
```

### 4. New Message Insertion (⚠️ VERIFY)

When receiving new messages via SignalR or polling:

```dart
// ✅ NEW: Insert at beginning (index 0), not end
void onNewMessage(Message newMessage) {
  setState(() {
    _messages.insert(0, newMessage); // Insert at top
  });
}
```

---

## 🧪 Testing Checklist

### Send Message Tests

- [ ] **Test 1**: Sponsor sends message to Farmer
  - Expected: ✅ 200 OK, message sent

- [ ] **Test 2**: Farmer replies to Sponsor (on their own analysis)
  - Expected: ✅ 200 OK, message sent (THIS WAS BROKEN BEFORE)

- [ ] **Test 3**: Dual-role user (Farmer+Sponsor) replies to sponsor
  - Expected: ✅ 200 OK, message sent (THIS WAS BROKEN BEFORE)

### Pagination Tests

- [ ] **Test 4**: Load page 1 (45 total messages)
  - Expected: Messages #45-#26 (newest 20)
  - Display: Latest message visible at top

- [ ] **Test 5**: Scroll up, load page 2
  - Expected: Messages #25-#6 (next 20 older)
  - Display: Smoothly appended above page 1

- [ ] **Test 6**: Scroll up, load page 3
  - Expected: Messages #5-#1 (oldest 5)
  - Display: Complete conversation history loaded

### UI/UX Tests

- [ ] **Test 7**: Open conversation
  - Expected: Latest message visible immediately (no auto-scroll to bottom)

- [ ] **Test 8**: Send new message
  - Expected: New message appears at top of chat

- [ ] **Test 9**: Receive new message (SignalR)
  - Expected: New message appears at top of chat

---

## 🚨 Breaking Changes

### For Mobile Team

| Change | Impact | Action Required |
|--------|--------|-----------------|
| **Message order** | Messages now DESC (newest first) | ✅ Update scroll logic |
| **Pagination direction** | Scroll UP for older messages | ✅ Reverse scroll listener |
| **Initial scroll position** | Stay at top (don't scroll to bottom) | ✅ Remove auto-scroll code |
| **New message insertion** | Insert at index 0, not append | ✅ Update insertion logic |

### No Changes Required

| Feature | Status |
|---------|--------|
| **Send message endpoint** | ✅ No changes (same params, same response) |
| **Attachment endpoint** | ✅ No changes |
| **Voice message endpoint** | ✅ No changes |
| **Response structure** | ✅ No changes (same DTO fields) |
| **Authorization** | ✅ No changes (same header/token logic) |

---

## 📝 API Endpoints Summary

| Endpoint | Method | Change |
|----------|--------|--------|
| `/api/v1/sponsorship/messages` | POST | ✅ Fixed permissions (no param changes) |
| `/api/v1/sponsorship/messages/conversation` | GET | 🔄 Order changed (DESC) |
| `/api/v1/sponsorship/messages/attachments` | POST | ✅ No changes |
| `/api/v1/sponsorship/messages/voice` | POST | ✅ No changes |

---

## 🔗 Related Documentation

- **Bug Report**: `MESSAGING_PERMISSION_BUG_REPORT.md`
- **Backend Logs**: `application.txt` (line 162-163 for old error)
- **Commits**:
  - Permission fix: `56ade2f1`
  - Pagination fix: `a1daf4fd`

---

## 💬 Questions or Issues?

Contact backend team if you encounter:
- Different response structure than documented
- Authorization errors (403/401)
- Pagination not working as expected
- Performance issues with large message lists

---

**Last Updated**: 2025-12-04
**Backend Environment**: Staging (ziraai-api-sit.up.railway.app)
