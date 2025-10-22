# 💬 Conversation Endpoint API Documentation for Mobile Team

**Version:** v1
**Date:** 2025-10-20
**Environment:** Staging: `https://ziraai-api-sit.up.railway.app`

---

## 📋 Table of Contents

1. [Endpoint Overview](#endpoint-overview)
2. [Authentication](#authentication)
3. [Request Specification](#request-specification)
4. [Response Specification](#response-specification)
5. [Pagination Guide](#pagination-guide)
6. [Usage Examples](#usage-examples)
7. [Error Handling](#error-handling)
8. [Implementation Guide](#implementation-guide)
9. [Testing Checklist](#testing-checklist)

---

## 🎯 Endpoint Overview

**Endpoint:** `GET /api/v1/sponsorship/messages/conversation`

**Purpose:** Retrieve paginated conversation messages between two users (sponsor ↔ farmer) for a specific plant analysis.

**Features:**
- ✅ Paginated responses (default: 20 messages per page)
- ✅ Supports text messages, attachments, and voice messages
- ✅ Includes sender/receiver avatars
- ✅ Message status tracking (sent, delivered, read)
- ✅ Optimized for infinite scroll UI pattern

---

## 🔐 Authentication

**Required:** Yes
**Type:** Bearer Token (JWT)
**Header:** `Authorization: Bearer {access_token}`

**Allowed Roles:**
- Sponsor
- Farmer
- Admin

---

## 📤 Request Specification

### Base URL
```
https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/messages/conversation
```

### Query Parameters

| Parameter | Type | Required | Default | Max | Description |
|-----------|------|----------|---------|-----|-------------|
| `plantAnalysisId` | integer | ✅ Yes | - | - | The plant analysis ID for context |
| `otherUserId` | integer | ✅ Yes | - | - | The other participant's user ID (sponsor or farmer) |
| `page` | integer | ❌ No | `1` | - | Page number (starts from 1) |
| `pageSize` | integer | ❌ No | `20` | `100` | Number of messages per page |

### Request Examples

#### Swift (iOS)
```swift
struct ConversationRequest {
    let plantAnalysisId: Int
    let otherUserId: Int
    let page: Int = 1
    let pageSize: Int = 20

    var urlString: String {
        let baseURL = "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/messages/conversation"
        return "\(baseURL)?plantAnalysisId=\(plantAnalysisId)&otherUserId=\(otherUserId)&page=\(page)&pageSize=\(pageSize)"
    }
}

// Usage
func fetchConversation(plantAnalysisId: Int, otherUserId: Int, page: Int = 1) async throws -> ConversationResponse {
    guard let url = URL(string: ConversationRequest(
        plantAnalysisId: plantAnalysisId,
        otherUserId: otherUserId,
        page: page
    ).urlString) else {
        throw NetworkError.invalidURL
    }

    var request = URLRequest(url: url)
    request.setValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")

    let (data, _) = try await URLSession.shared.data(for: request)
    return try JSONDecoder().decode(ConversationResponse.self, from: data)
}
```

#### Kotlin (Android)
```kotlin
data class ConversationRequest(
    val plantAnalysisId: Int,
    val otherUserId: Int,
    val page: Int = 1,
    val pageSize: Int = 20
)

// Retrofit interface
interface ConversationApi {
    @GET("api/v1/sponsorship/messages/conversation")
    suspend fun getConversation(
        @Query("plantAnalysisId") plantAnalysisId: Int,
        @Query("otherUserId") otherUserId: Int,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 20,
        @Header("Authorization") authorization: String
    ): ConversationResponse
}

// Usage
suspend fun fetchConversation(
    plantAnalysisId: Int,
    otherUserId: Int,
    page: Int = 1
): ConversationResponse {
    return api.getConversation(
        plantAnalysisId = plantAnalysisId,
        otherUserId = otherUserId,
        page = page,
        pageSize = 20,
        authorization = "Bearer $accessToken"
    )
}
```

#### cURL (Testing)
```bash
# First page (default 20 messages)
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/messages/conversation?plantAnalysisId=60&otherUserId=159" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

# Second page
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/messages/conversation?plantAnalysisId=60&otherUserId=159&page=2" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

# Custom page size (30 messages)
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/messages/conversation?plantAnalysisId=60&otherUserId=159&page=1&pageSize=30" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

---

## 📥 Response Specification

### Success Response (200 OK)

```json
{
  "data": [
    {
      "id": 21,
      "plantAnalysisId": 60,
      "fromUserId": 165,
      "toUserId": 159,
      "message": "Sana bir resimli mesaj gönderdim",
      "messageType": "Attachment",
      "subject": null,
      "messageStatus": "Sent",
      "isRead": false,
      "sentDate": "2025-10-20T14:30:00",
      "deliveredDate": null,
      "readDate": null,
      "senderRole": "Farmer",
      "senderName": "Ahmet Yılmaz",
      "senderCompany": "",
      "senderAvatarUrl": "https://example.com/avatars/165.jpg",
      "senderAvatarThumbnailUrl": "https://example.com/avatars/165_thumb.jpg",
      "receiverRole": "Sponsor",
      "receiverName": "Ziraat Sponsor A.Ş.",
      "receiverCompany": "Ziraat Sponsor",
      "receiverAvatarUrl": "https://example.com/avatars/159.jpg",
      "receiverAvatarThumbnailUrl": "https://example.com/avatars/159_thumb.jpg",
      "priority": "Normal",
      "category": "General",
      "hasAttachments": true,
      "attachmentCount": 1,
      "attachmentUrls": [
        "https://freeimage.host/i/example123.jpg"
      ],
      "attachmentTypes": [
        "image/jpeg"
      ],
      "attachmentSizes": [
        245632
      ],
      "attachmentNames": [
        "plant_photo.jpg"
      ],
      "isVoiceMessage": false,
      "voiceMessageUrl": null,
      "voiceMessageDuration": null,
      "voiceMessageWaveform": null,
      "isEdited": false,
      "editedDate": null,
      "isForwarded": false,
      "forwardedFromMessageId": null,
      "isActive": true
    },
    {
      "id": 22,
      "plantAnalysisId": 60,
      "fromUserId": 165,
      "toUserId": 159,
      "message": "[Voice Message]",
      "messageType": "VoiceMessage",
      "subject": null,
      "messageStatus": "Sent",
      "isRead": false,
      "sentDate": "2025-10-20T14:35:00",
      "deliveredDate": null,
      "readDate": null,
      "senderRole": "Farmer",
      "senderName": "Ahmet Yılmaz",
      "senderCompany": "",
      "senderAvatarUrl": "https://example.com/avatars/165.jpg",
      "senderAvatarThumbnailUrl": "https://example.com/avatars/165_thumb.jpg",
      "receiverRole": "Sponsor",
      "receiverName": "Ziraat Sponsor A.Ş.",
      "receiverCompany": "Ziraat Sponsor",
      "receiverAvatarUrl": "https://example.com/avatars/159.jpg",
      "receiverAvatarThumbnailUrl": "https://example.com/avatars/159_thumb.jpg",
      "priority": "Normal",
      "category": "General",
      "hasAttachments": false,
      "attachmentCount": 0,
      "attachmentUrls": null,
      "attachmentTypes": null,
      "attachmentSizes": null,
      "attachmentNames": null,
      "isVoiceMessage": true,
      "voiceMessageUrl": "https://example.com/voice/message123.m4a",
      "voiceMessageDuration": 45,
      "voiceMessageWaveform": "[0.2,0.5,0.8,0.6,0.3,...]",
      "isEdited": false,
      "editedDate": null,
      "isForwarded": false,
      "forwardedFromMessageId": null,
      "isActive": true
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalRecords": 45,
  "totalPages": 3,
  "firstPage": null,
  "lastPage": null,
  "nextPage": null,
  "previousPage": null,
  "success": true,
  "message": "List was paginated successfully."
}
```

### Response Field Definitions

#### Root Level Fields
| Field | Type | Description |
|-------|------|-------------|
| `data` | array | Array of message objects |
| `pageNumber` | integer | Current page number |
| `pageSize` | integer | Messages per page |
| `totalRecords` | integer | Total number of messages in conversation |
| `totalPages` | integer | Total number of pages available |
| `success` | boolean | Whether request was successful |
| `message` | string | Success/error message |

#### Message Object Fields

**Basic Message Info:**
| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `id` | integer | No | Unique message ID |
| `plantAnalysisId` | integer | No | Associated plant analysis ID |
| `fromUserId` | integer | No | Sender's user ID |
| `toUserId` | integer | No | Receiver's user ID |
| `message` | string | No | Message text content |
| `messageType` | string | No | Type: "Information", "Attachment", "VoiceMessage" |
| `subject` | string | Yes | Optional message subject |

**Message Status:**
| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `messageStatus` | string | No | Status: "Sent", "Delivered", "Read" |
| `isRead` | boolean | No | Whether message has been read |
| `sentDate` | datetime | No | When message was sent (ISO 8601) |
| `deliveredDate` | datetime | Yes | When message was delivered |
| `readDate` | datetime | Yes | When message was read |

**Sender Information:**
| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `senderRole` | string | No | "Farmer" or "Sponsor" |
| `senderName` | string | No | Sender's full name |
| `senderCompany` | string | Yes | Sender's company (sponsors only) |
| `senderAvatarUrl` | string | Yes | Sender's profile picture URL |
| `senderAvatarThumbnailUrl` | string | Yes | Sender's thumbnail (optimized) |

**Receiver Information:**
| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `receiverRole` | string | Yes | "Farmer" or "Sponsor" |
| `receiverName` | string | No | Receiver's full name |
| `receiverCompany` | string | Yes | Receiver's company |
| `receiverAvatarUrl` | string | Yes | Receiver's profile picture URL |
| `receiverAvatarThumbnailUrl` | string | Yes | Receiver's thumbnail |

**Classification:**
| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `priority` | string | Yes | "Normal", "High", "Urgent" |
| `category` | string | Yes | Message category |

**Attachments:**
| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `hasAttachments` | boolean | No | Whether message has attachments |
| `attachmentCount` | integer | No | Number of attachments (0 if none) |
| `attachmentUrls` | string[] | Yes | Array of attachment URLs |
| `attachmentTypes` | string[] | Yes | Array of MIME types (e.g., "image/jpeg") |
| `attachmentSizes` | long[] | Yes | Array of file sizes in bytes |
| `attachmentNames` | string[] | Yes | Array of original filenames |

**Voice Messages:**
| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `isVoiceMessage` | boolean | No | Whether this is a voice message |
| `voiceMessageUrl` | string | Yes | URL to voice file (.m4a, .mp3) |
| `voiceMessageDuration` | integer | Yes | Duration in seconds |
| `voiceMessageWaveform` | string | Yes | JSON array of waveform data for visualization |

**Edit/Delete/Forward:**
| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `isEdited` | boolean | No | Whether message was edited |
| `editedDate` | datetime | Yes | When message was last edited |
| `isForwarded` | boolean | No | Whether message was forwarded |
| `forwardedFromMessageId` | integer | Yes | Original message ID if forwarded |
| `isActive` | boolean | No | Whether message is active (not deleted) |

---

## 📖 Pagination Guide

### Understanding Pagination

**Total Messages:** 45
**Page Size:** 20
**Total Pages:** 3 (Math.ceil(45/20) = 3)

| Page | Messages Returned | Range |
|------|-------------------|-------|
| 1 | 20 messages | 1-20 |
| 2 | 20 messages | 21-40 |
| 3 | 5 messages | 41-45 |

### Calculating Pagination

```typescript
interface PaginationInfo {
  currentPage: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

function calculatePagination(response: ConversationResponse): PaginationInfo {
  const hasNextPage = response.pageNumber < response.totalPages;
  const hasPreviousPage = response.pageNumber > 1;

  return {
    currentPage: response.pageNumber,
    pageSize: response.pageSize,
    totalRecords: response.totalRecords,
    totalPages: response.totalPages,
    hasNextPage,
    hasPreviousPage
  };
}
```

### Load More Pattern

```swift
class ConversationViewModel: ObservableObject {
    @Published var messages: [Message] = []
    @Published var isLoading = false
    @Published var hasMorePages = true

    private var currentPage = 1
    private let pageSize = 20

    func loadInitialMessages() async {
        currentPage = 1
        messages = []
        await loadMessages()
    }

    func loadMoreMessages() async {
        guard !isLoading && hasMorePages else { return }
        currentPage += 1
        await loadMessages()
    }

    private func loadMessages() async {
        isLoading = true
        defer { isLoading = false }

        do {
            let response = try await fetchConversation(
                plantAnalysisId: plantAnalysisId,
                otherUserId: otherUserId,
                page: currentPage
            )

            messages.append(contentsOf: response.data)
            hasMorePages = response.pageNumber < response.totalPages
        } catch {
            print("Error loading messages: \(error)")
            currentPage -= 1 // Rollback on error
        }
    }
}
```

### Infinite Scroll Pattern (Kotlin)

```kotlin
class ConversationViewModel : ViewModel() {
    private val _messages = MutableStateFlow<List<Message>>(emptyList())
    val messages: StateFlow<List<Message>> = _messages.asStateFlow()

    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading.asStateFlow()

    private var currentPage = 1
    private var hasMorePages = true

    fun loadInitialMessages(plantAnalysisId: Int, otherUserId: Int) {
        viewModelScope.launch {
            currentPage = 1
            _messages.value = emptyList()
            loadMessages(plantAnalysisId, otherUserId)
        }
    }

    fun loadMoreMessages(plantAnalysisId: Int, otherUserId: Int) {
        if (_isLoading.value || !hasMorePages) return

        viewModelScope.launch {
            currentPage++
            loadMessages(plantAnalysisId, otherUserId)
        }
    }

    private suspend fun loadMessages(plantAnalysisId: Int, otherUserId: Int) {
        _isLoading.value = true
        try {
            val response = repository.getConversation(
                plantAnalysisId = plantAnalysisId,
                otherUserId = otherUserId,
                page = currentPage
            )

            _messages.value = _messages.value + response.data
            hasMorePages = response.pageNumber < response.totalPages
        } catch (e: Exception) {
            currentPage-- // Rollback on error
            Log.e("ConversationVM", "Error loading messages", e)
        } finally {
            _isLoading.value = false
        }
    }
}
```

---

## 💡 Usage Examples

### Example 1: Initial Load (First 20 Messages)

**Request:**
```http
GET /api/v1/sponsorship/messages/conversation?plantAnalysisId=60&otherUserId=159
Authorization: Bearer eyJhbGc...
```

**Response:**
```json
{
  "data": [ /* 20 messages */ ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalRecords": 45,
  "totalPages": 3,
  "success": true,
  "message": "List was paginated successfully."
}
```

**UI Action:** Display first 20 messages, show "Load More" button

---

### Example 2: Load More (Next Page)

**Request:**
```http
GET /api/v1/sponsorship/messages/conversation?plantAnalysisId=60&otherUserId=159&page=2
Authorization: Bearer eyJhbGc...
```

**Response:**
```json
{
  "data": [ /* 20 more messages */ ],
  "pageNumber": 2,
  "pageSize": 20,
  "totalRecords": 45,
  "totalPages": 3,
  "success": true,
  "message": "List was paginated successfully."
}
```

**UI Action:** Append 20 messages to existing list, keep "Load More" visible

---

### Example 3: Last Page (Remaining Messages)

**Request:**
```http
GET /api/v1/sponsorship/messages/conversation?plantAnalysisId=60&otherUserId=159&page=3
Authorization: Bearer eyJhbGc...
```

**Response:**
```json
{
  "data": [ /* 5 messages */ ],
  "pageNumber": 3,
  "pageSize": 20,
  "totalRecords": 45,
  "totalPages": 3,
  "success": true,
  "message": "List was paginated successfully."
}
```

**UI Action:** Append final 5 messages, hide "Load More" button (no more pages)

---

## ⚠️ Error Handling

### 401 Unauthorized
**Cause:** Missing or invalid JWT token
**Action:** Redirect to login

```json
{
  "success": false,
  "message": "Unauthorized"
}
```

### 400 Bad Request
**Cause:** Invalid parameters (e.g., invalid plantAnalysisId)

```json
{
  "data": null,
  "pageNumber": 0,
  "pageSize": 0,
  "totalRecords": 0,
  "totalPages": 0,
  "success": false,
  "message": "Invalid request parameters"
}
```

### 403 Forbidden
**Cause:** User doesn't have permission to view this conversation

```json
{
  "success": false,
  "message": "You don't have permission to access this conversation"
}
```

### Error Handling Example (Swift)

```swift
enum ConversationError: Error {
    case unauthorized
    case forbidden
    case invalidParameters
    case networkError(Error)
    case unknown
}

func handleConversationError(_ error: Error) -> ConversationError {
    if let urlError = error as? URLError {
        return .networkError(urlError)
    }

    if let httpResponse = (error as NSError).userInfo["response"] as? HTTPURLResponse {
        switch httpResponse.statusCode {
        case 401:
            return .unauthorized
        case 403:
            return .forbidden
        case 400:
            return .invalidParameters
        default:
            return .unknown
        }
    }

    return .unknown
}
```

---

## 🛠 Implementation Guide

### Step 1: Create Data Models

```swift
// Swift Models
struct ConversationResponse: Codable {
    let data: [Message]
    let pageNumber: Int
    let pageSize: Int
    let totalRecords: Int
    let totalPages: Int
    let success: Bool
    let message: String
}

struct Message: Codable, Identifiable {
    let id: Int
    let plantAnalysisId: Int
    let fromUserId: Int
    let toUserId: Int
    let message: String
    let messageType: String
    let messageStatus: String
    let isRead: Bool
    let sentDate: Date
    let deliveredDate: Date?
    let readDate: Date?

    // Sender info
    let senderRole: String
    let senderName: String
    let senderCompany: String?
    let senderAvatarUrl: String?
    let senderAvatarThumbnailUrl: String?

    // Receiver info
    let receiverName: String
    let receiverAvatarUrl: String?
    let receiverAvatarThumbnailUrl: String?

    // Attachments
    let hasAttachments: Bool
    let attachmentCount: Int
    let attachmentUrls: [String]?
    let attachmentTypes: [String]?
    let attachmentSizes: [Int]?
    let attachmentNames: [String]?

    // Voice
    let isVoiceMessage: Bool
    let voiceMessageUrl: String?
    let voiceMessageDuration: Int?
    let voiceMessageWaveform: String?

    let isActive: Bool
}
```

```kotlin
// Kotlin Models
data class ConversationResponse(
    val data: List<Message>,
    val pageNumber: Int,
    val pageSize: Int,
    val totalRecords: Int,
    val totalPages: Int,
    val success: Boolean,
    val message: String
)

data class Message(
    val id: Int,
    val plantAnalysisId: Int,
    val fromUserId: Int,
    val toUserId: Int,
    val message: String,
    val messageType: String,
    val messageStatus: String,
    val isRead: Boolean,
    val sentDate: String,
    val deliveredDate: String?,
    val readDate: String?,

    // Sender info
    val senderRole: String,
    val senderName: String,
    val senderCompany: String?,
    val senderAvatarUrl: String?,
    val senderAvatarThumbnailUrl: String?,

    // Receiver info
    val receiverName: String,
    val receiverAvatarUrl: String?,
    val receiverAvatarThumbnailUrl: String?,

    // Attachments
    val hasAttachments: Boolean,
    val attachmentCount: Int,
    val attachmentUrls: List<String>?,
    val attachmentTypes: List<String>?,
    val attachmentSizes: List<Long>?,
    val attachmentNames: List<String>?,

    // Voice
    val isVoiceMessage: Boolean,
    val voiceMessageUrl: String?,
    val voiceMessageDuration: Int?,
    val voiceMessageWaveform: String?,

    val isActive: Boolean
)
```

### Step 2: Implement Network Layer

```swift
// Swift Network Service
class ConversationService {
    private let baseURL = "https://ziraai-api-sit.up.railway.app"
    private let session = URLSession.shared

    func getConversation(
        plantAnalysisId: Int,
        otherUserId: Int,
        page: Int = 1,
        pageSize: Int = 20,
        accessToken: String
    ) async throws -> ConversationResponse {
        var components = URLComponents(string: "\(baseURL)/api/v1/sponsorship/messages/conversation")!
        components.queryItems = [
            URLQueryItem(name: "plantAnalysisId", value: "\(plantAnalysisId)"),
            URLQueryItem(name: "otherUserId", value: "\(otherUserId)"),
            URLQueryItem(name: "page", value: "\(page)"),
            URLQueryItem(name: "pageSize", value: "\(pageSize)")
        ]

        var request = URLRequest(url: components.url!)
        request.setValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")

        let (data, response) = try await session.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse,
              (200...299).contains(httpResponse.statusCode) else {
            throw ConversationError.unknown
        }

        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return try decoder.decode(ConversationResponse.self, from: data)
    }
}
```

### Step 3: Implement UI (SwiftUI Example)

```swift
struct ConversationView: View {
    @StateObject private var viewModel = ConversationViewModel()
    let plantAnalysisId: Int
    let otherUserId: Int

    var body: some View {
        ScrollView {
            LazyVStack {
                ForEach(viewModel.messages) { message in
                    MessageRow(message: message)
                }

                if viewModel.hasMorePages {
                    Button("Load More") {
                        Task {
                            await viewModel.loadMoreMessages()
                        }
                    }
                    .disabled(viewModel.isLoading)
                }

                if viewModel.isLoading {
                    ProgressView()
                }
            }
        }
        .task {
            await viewModel.loadInitialMessages()
        }
    }
}

struct MessageRow: View {
    let message: Message

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            // Avatar
            AsyncImage(url: URL(string: message.senderAvatarThumbnailUrl ?? "")) { image in
                image.resizable()
            } placeholder: {
                Circle().fill(Color.gray)
            }
            .frame(width: 40, height: 40)
            .clipShape(Circle())

            VStack(alignment: .leading, spacing: 4) {
                // Sender name
                Text(message.senderName)
                    .font(.caption)
                    .foregroundColor(.secondary)

                // Message content
                if message.isVoiceMessage {
                    VoiceMessageView(url: message.voiceMessageUrl, duration: message.voiceMessageDuration)
                } else if message.hasAttachments {
                    AttachmentView(urls: message.attachmentUrls ?? [])
                } else {
                    Text(message.message)
                }

                // Timestamp
                Text(message.sentDate, style: .time)
                    .font(.caption2)
                    .foregroundColor(.secondary)
            }
        }
        .padding()
    }
}
```

---

## ✅ Testing Checklist

### Before Release

- [ ] Test initial load (first page)
- [ ] Test pagination (load more pages)
- [ ] Test last page (no more messages)
- [ ] Test empty conversation (no messages)
- [ ] Test with different page sizes (10, 20, 50)
- [ ] Test error handling (401, 403, 400)
- [ ] Test with slow network (loading states)
- [ ] Test with attachment messages
- [ ] Test with voice messages
- [ ] Test with mixed message types
- [ ] Test avatar loading (with and without avatars)
- [ ] Test read/unread status
- [ ] Test message timestamps (timezone handling)
- [ ] Verify no duplicate messages on pagination
- [ ] Test memory usage (large conversations)
- [ ] Test scroll position preservation

### Edge Cases

- [ ] User with no avatar
- [ ] Very long message text
- [ ] Multiple attachments in one message
- [ ] Conversation with only 1 message
- [ ] Requesting page beyond total pages
- [ ] Page size = 0 or negative
- [ ] Page number = 0 or negative
- [ ] Invalid plantAnalysisId
- [ ] Invalid otherUserId
- [ ] Concurrent page requests

---

## 📞 Support

**For Questions:**
- Backend Team: [Backend Team Contact]
- API Issues: Create ticket in project management system

**Staging Environment:**
- Base URL: `https://ziraai-api-sit.up.railway.app`
- Status: Active
- Update Frequency: Daily deployments

**Production Environment:**
- Base URL: TBD
- Status: Not deployed yet
- Expected: After staging validation

---

## 📝 Change Log

**Version 1.0 (2025-10-20)**
- Initial documentation
- Added pagination support
- Default page size: 20 messages
- Max page size: 100 messages
- Added attachment support
- Added voice message support
- Added sender/receiver avatars

---

## 🚀 Quick Start Summary

1. **Authenticate:** Get JWT token from login endpoint
2. **First Request:** `GET /messages/conversation?plantAnalysisId=X&otherUserId=Y`
3. **Load More:** `GET /messages/conversation?plantAnalysisId=X&otherUserId=Y&page=2`
4. **Check Pagination:** Use `pageNumber < totalPages` to determine if more pages exist
5. **Display Messages:** Show text, attachments, or voice messages based on type
6. **Handle Errors:** Show appropriate error messages for 401, 403, 400

**That's it! You're ready to implement the conversation feature.** 🎉
