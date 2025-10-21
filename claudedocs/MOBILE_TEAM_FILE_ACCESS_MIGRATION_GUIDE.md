# Mobile Team Migration Guide: File Access API Changes

**Date**: 2025-10-21
**Impact**: Critical - Affects voice messages, attachments, and thumbnails
**Affected Endpoints**: `GET /api/v1/analysis-messages/conversation`

---

## 🚨 Breaking Changes Summary

### What Changed?
The file serving architecture was changed from **direct physical URLs** to **controller-based API endpoints** for security and authorization.

### Why?
1. **Security**: Publicly accessible URLs allowed unauthorized access
2. **Authorization**: No validation that user was message participant
3. **Audit Trail**: No logging of file access attempts
4. **URL Expiration**: Old signed URL approach had 15-minute expiration (rejected by product owner)

### Mobile App Impact
❌ **Thumbnails not showing** (missing field in DTO)
❌ **Voice messages not playing** (URL format changed)
❌ **Attachments showing "download soon"** (URL format changed)
❌ **Full-size images not opening** (URL format changed)

---

## 📋 Required Changes in Mobile App

### 1. Update HTTP Headers (CRITICAL)

**All file requests now require JWT authentication**

#### Before (❌ Wrong):
```dart
// Direct URL access - NO authentication
Image.network('https://ziraai.com/uploads/attachments/image_123.jpg')

AudioPlayer().play(UrlSource(
  'https://ziraai.com/voice-messages/voice_msg_123.m4a'
))
```

#### After (✅ Correct):
```dart
// API endpoint with JWT token
CachedNetworkImage(
  imageUrl: 'https://ziraai.com/api/v1/files/attachments/165/0',
  httpHeaders: {
    'Authorization': 'Bearer $jwtToken',  // REQUIRED
  },
)

AudioPlayer().play(UrlSource(
  'https://ziraai.com/api/v1/files/voice-messages/165',
  headers: {
    'Authorization': 'Bearer $jwtToken',  // REQUIRED
  },
))
```

---

### 2. Voice Message URL Format

#### Before (❌ Old Format):
```json
{
  "voiceMessageUrl": "https://ziraai.com/uploads/voice-messages/voice_msg_165_638965887035374118_1760991903.m4a"
}
```

#### After (✅ New Format):
```json
{
  "voiceMessageUrl": "https://ziraai.com/api/v1/files/voice-messages/165"
}
```

**Pattern**: `{BaseUrl}/api/v1/files/voice-messages/{messageId}`

**Mobile Implementation**:
```dart
Future<void> playVoiceMessage(AnalysisMessageDto message, String token) async {
  if (message.isVoiceMessage && message.voiceMessageUrl != null) {
    final player = AudioPlayer();

    await player.play(
      UrlSource(message.voiceMessageUrl),
      headers: {
        'Authorization': 'Bearer $token',
      },
    );
  }
}
```

---

### 3. Attachment URL Format

#### Before (❌ Old Format):
```json
{
  "attachmentUrls": [
    "https://ziraai.com/uploads/attachments/file_167_0_638965887100123456.jpg",
    "https://ziraai.com/uploads/attachments/file_167_1_638965887100123457.pdf"
  ]
}
```

#### After (✅ New Format):
```json
{
  "attachmentUrls": [
    "https://ziraai.com/api/v1/files/attachments/167/0",
    "https://ziraai.com/api/v1/files/attachments/167/1"
  ]
}
```

**Pattern**: `{BaseUrl}/api/v1/files/attachments/{messageId}/{attachmentIndex}`

**Note**: `attachmentIndex` is **zero-based** (first attachment = 0)

**Mobile Implementation**:
```dart
Widget buildAttachmentList(AnalysisMessageDto message, String token) {
  if (!message.hasAttachments || message.attachmentUrls == null) {
    return SizedBox.shrink();
  }

  return ListView.builder(
    itemCount: message.attachmentUrls!.length,
    itemBuilder: (context, index) {
      final url = message.attachmentUrls![index];
      final type = message.attachmentTypes?[index];
      final name = message.attachmentNames?[index];

      if (type?.startsWith('image/') == true) {
        return CachedNetworkImage(
          imageUrl: url,
          httpHeaders: {
            'Authorization': 'Bearer $token',
          },
        );
      } else {
        return AttachmentDownloadButton(
          url: url,
          name: name,
          token: token,
        );
      }
    },
  );
}
```

---

### 4. 🆕 Thumbnail Support (NEW FIELD)

#### ⚠️ CRITICAL: Missing Field in Current API Response

**Current Issue**: `attachmentThumbnails` field is missing from DTO

**Status**: Backend needs to add this field

**Expected Format**:
```json
{
  "hasAttachments": true,
  "attachmentCount": 2,
  "attachmentUrls": [
    "https://ziraai.com/api/v1/files/attachments/167/0",
    "https://ziraai.com/api/v1/files/attachments/167/1"
  ],
  "attachmentThumbnails": [
    "https://ziraai.com/api/v1/files/attachments/167/0?thumbnail=true",
    "https://ziraai.com/api/v1/files/attachments/167/1?thumbnail=true"
  ],
  "attachmentTypes": ["image/jpeg", "application/pdf"],
  "attachmentNames": ["plant_disease.jpg", "report.pdf"],
  "attachmentSizes": [245678, 1024567]
}
```

**Mobile Implementation (Once Backend Adds Field)**:
```dart
// Display thumbnail in chat list
Widget buildThumbnail(AnalysisMessageDto message, String token) {
  if (message.attachmentThumbnails != null &&
      message.attachmentThumbnails!.isNotEmpty) {
    return CachedNetworkImage(
      imageUrl: message.attachmentThumbnails![0],
      httpHeaders: {
        'Authorization': 'Bearer $token',
      },
      width: 60,
      height: 60,
      fit: BoxFit.cover,
    );
  }
  return Icon(Icons.attachment);
}

// Display full-size image on tap
void openFullImage(AnalysisMessageDto message, int index, String token) {
  Navigator.push(
    context,
    MaterialPageRoute(
      builder: (_) => FullImageViewer(
        imageUrl: message.attachmentUrls![index],
        token: token,
      ),
    ),
  );
}

class FullImageViewer extends StatelessWidget {
  final String imageUrl;
  final String token;

  const FullImageViewer({required this.imageUrl, required this.token});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Image')),
      body: InteractiveViewer(
        child: CachedNetworkImage(
          imageUrl: imageUrl,  // Full-size URL (same as thumbnail URL but without ?thumbnail=true)
          httpHeaders: {
            'Authorization': 'Bearer $token',
          },
        ),
      ),
    );
  }
}
```

---

## 🔧 Backend Changes Required

### Missing DTO Field

**File**: `Entities/Dtos/AnalysisMessageDto.cs`

**Add**:
```csharp
public string[] AttachmentThumbnails { get; set; }
```

**Expected Position**: After `AttachmentUrls` property

### Update GetConversationQuery

**File**: `Business/Handlers/AnalysisMessages/Queries/GetConversationQuery.cs`

**Add thumbnail URL generation**:
```csharp
// Generate attachment thumbnail URLs
string[] attachmentThumbnails = null;
if (m.HasAttachments && !string.IsNullOrEmpty(m.AttachmentUrls))
{
    var physicalUrls = System.Text.Json.JsonSerializer.Deserialize<string[]>(m.AttachmentUrls);
    if (physicalUrls != null && physicalUrls.Length > 0)
    {
        attachmentThumbnails = new string[physicalUrls.Length];
        for (int i = 0; i < physicalUrls.Length; i++)
        {
            // For images, add thumbnail query parameter
            // For non-images (PDF, etc), use same URL or icon
            var type = attachmentTypes?[i];
            if (type?.StartsWith("image/") == true)
            {
                attachmentThumbnails[i] = $"{baseUrl}/api/v1/files/attachments/{m.Id}/{i}?thumbnail=true";
            }
            else
            {
                attachmentThumbnails[i] = null; // Mobile will show icon
            }
        }
    }
}
```

### Update SendMessageWithAttachmentCommand

**File**: `Business/Handlers/AnalysisMessages/Commands/SendMessageWithAttachmentCommand.cs`

**Add thumbnail URL to response**:
```csharp
// Generate thumbnail URLs for response
var apiThumbnailUrls = new List<string>();
for (int i = 0; i < uploadedUrls.Count; i++)
{
    var type = attachmentTypes[i];
    if (type.StartsWith("image/"))
    {
        apiThumbnailUrls.Add($"{baseUrl}/api/v1/files/attachments/{message.Id}/{i}?thumbnail=true");
    }
    else
    {
        apiThumbnailUrls.Add(null);
    }
}

var messageDto = new AnalysisMessageDto
{
    // ... existing fields ...
    AttachmentUrls = apiAttachmentUrls.ToArray(),
    AttachmentThumbnails = apiThumbnailUrls.ToArray(),
    // ... rest of fields ...
};
```

---

## 📱 Mobile Development Checklist

### Immediate Actions (Before Backend Update)

- [ ] **Remove thumbnail code temporarily** - Backend doesn't return thumbnails yet
- [ ] **Update voice message player** - Add JWT token to headers
- [ ] **Update attachment display** - Add JWT token to headers
- [ ] **Test with new URL format** - Verify API endpoint access works
- [ ] **Handle 401 errors** - Token expired, re-authenticate user
- [ ] **Handle 403 errors** - User not authorized (shouldn't happen for own messages)
- [ ] **Handle 404 errors** - Message or file deleted

### After Backend Adds Thumbnails

- [ ] **Add thumbnail support** - Use `attachmentThumbnails` field
- [ ] **Implement full-image viewer** - Open full-size on thumbnail tap
- [ ] **Cache thumbnails** - Use `cached_network_image` package
- [ ] **Test thumbnail generation** - Verify thumbnails load correctly

---

## 🧪 Testing Guide

### Test Scenarios

#### 1. Voice Message Playback
```dart
// Test voice message plays with new URL format
test('voice message plays with JWT token', () async {
  final message = AnalysisMessageDto(
    voiceMessageUrl: 'https://ziraai-api-sit.up.railway.app/api/v1/files/voice-messages/165',
  );

  final player = AudioPlayer();
  await player.play(
    UrlSource(message.voiceMessageUrl!),
    headers: {'Authorization': 'Bearer $testToken'},
  );

  expect(player.state, PlayerState.playing);
});
```

#### 2. Image Attachment Display
```dart
// Test image attachment displays with JWT token
testWidgets('image attachment displays', (tester) async {
  final message = AnalysisMessageDto(
    hasAttachments: true,
    attachmentUrls: ['https://ziraai-api-sit.up.railway.app/api/v1/files/attachments/167/0'],
    attachmentTypes: ['image/jpeg'],
  );

  await tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: CachedNetworkImage(
          imageUrl: message.attachmentUrls![0],
          httpHeaders: {'Authorization': 'Bearer $testToken'},
        ),
      ),
    ),
  );

  await tester.pumpAndSettle();
  expect(find.byType(CachedNetworkImage), findsOneWidget);
});
```

#### 3. Error Handling
```dart
// Test 401 Unauthorized error
test('handles expired token', () async {
  final response = await http.get(
    Uri.parse('https://ziraai.com/api/v1/files/voice-messages/165'),
    headers: {'Authorization': 'Bearer expired_token'},
  );

  expect(response.statusCode, 401);
  // Trigger token refresh flow
});

// Test 403 Forbidden error
test('handles unauthorized access', () async {
  final response = await http.get(
    Uri.parse('https://ziraai.com/api/v1/files/voice-messages/999'),
    headers: {'Authorization': 'Bearer $validToken'},
  );

  expect(response.statusCode, 403);
  // Show "Access Denied" message
});
```

---

## 🔒 Security Considerations

### Token Management

**Critical**: JWT tokens MUST be included in all file requests

```dart
// ✅ CORRECT - Token in headers
CachedNetworkImage(
  imageUrl: fileUrl,
  httpHeaders: {
    'Authorization': 'Bearer ${AuthService.instance.token}',
  },
)

// ❌ WRONG - No token
Image.network(fileUrl)
```

### Token Refresh

```dart
class FileService {
  Future<Uint8List> downloadFile(String url) async {
    try {
      final response = await http.get(
        Uri.parse(url),
        headers: {'Authorization': 'Bearer ${AuthService.instance.token}'},
      );

      if (response.statusCode == 401) {
        // Token expired - refresh and retry
        await AuthService.instance.refreshToken();
        return downloadFile(url); // Retry with new token
      }

      if (response.statusCode == 403) {
        throw Exception('Access denied');
      }

      if (response.statusCode == 404) {
        throw Exception('File not found');
      }

      return response.bodyBytes;
    } catch (e) {
      throw Exception('Failed to download file: $e');
    }
  }
}
```

---

## 📊 Comparison Table

| Feature | Before (Old) | After (New) |
|---------|-------------|-------------|
| **Voice Message URL** | `{BaseUrl}/uploads/voice-messages/{filename}.m4a` | `{BaseUrl}/api/v1/files/voice-messages/{messageId}` |
| **Attachment URL** | `{BaseUrl}/uploads/attachments/{filename}.jpg` | `{BaseUrl}/api/v1/files/attachments/{messageId}/{index}` |
| **Thumbnail URL** | Same as attachment URL | `{BaseUrl}/api/v1/files/attachments/{messageId}/{index}?thumbnail=true` (coming soon) |
| **Authentication** | ❌ None (public access) | ✅ JWT Bearer token required |
| **Authorization** | ❌ None | ✅ Only sender/receiver can access |
| **URL Expiration** | ❌ 15 minutes (signed URLs) | ✅ Permanent (as long as user is participant) |
| **Audit Logging** | ❌ None | ✅ All access logged |

---

## 🆘 Troubleshooting

### Voice Messages Not Playing

**Symptom**: `AudioPlayerException: Failed to set source`

**Causes & Solutions**:
1. **Missing JWT token** → Add `Authorization` header
2. **Expired token** → Refresh token and retry
3. **Old URL format** → Update to new API endpoint format
4. **403 Forbidden** → User not message participant (shouldn't happen)
5. **404 Not Found** → Message deleted or file missing

### Thumbnails Not Showing

**Symptom**: Icons shown instead of image thumbnails

**Causes & Solutions**:
1. **Backend hasn't added field yet** → Wait for backend update
2. **Missing JWT token** → Add `Authorization` header
3. **Wrong URL** → Use `attachmentThumbnails` field, not `attachmentUrls`

### Attachments Showing "Download Soon"

**Symptom**: Download button appears but doesn't work

**Causes & Solutions**:
1. **Missing JWT token** → Add `Authorization` header
2. **Old URL format** → Update to new API endpoint format
3. **Network error** → Check internet connection

### Full-Size Images Not Opening

**Symptom**: Tapping thumbnail doesn't open full image

**Causes & Solutions**:
1. **Navigation not implemented** → Add tap handler to navigate to full image viewer
2. **Missing JWT token** → Add `Authorization` header to full image viewer
3. **Using thumbnail URL** → Use `attachmentUrls[index]` for full-size, not `attachmentThumbnails[index]`

---

## 📞 Backend Support Needed

### Immediate Request

**Add `AttachmentThumbnails` field to API response**

**Impact**: High - Thumbnails not showing in mobile app

**Required Changes**:
1. Add `AttachmentThumbnails` property to `AnalysisMessageDto`
2. Generate thumbnail URLs in `GetConversationQuery`
3. Generate thumbnail URLs in `SendMessageWithAttachmentCommand`

**Expected Timeline**: 1-2 hours development + testing

---

## 📝 Migration Timeline

### Phase 1: Immediate (Today)
- Backend adds `AttachmentThumbnails` field
- Mobile updates HTTP headers with JWT tokens
- Mobile tests voice message playback

### Phase 2: Short-term (This Week)
- Mobile implements thumbnail display
- Mobile implements full-image viewer
- End-to-end testing with real data

### Phase 3: Follow-up (Next Week)
- Performance optimization (caching, prefetching)
- Error handling improvements
- User experience polish

---

## 🔗 Related Documentation

- [MESSAGING_FILE_ACCESS_API.md](MESSAGING_FILE_ACCESS_API.md) - Complete API reference
- [CONTROLLER_BASED_FILE_SERVING_SUMMARY.md](CONTROLLER_BASED_FILE_SERVING_SUMMARY.md) - Backend implementation details
- [FILE_SECURITY_RECOMMENDATIONS.md](FILE_SECURITY_RECOMMENDATIONS.md) - Security analysis

---

## ✅ Sign-off

**Backend Developer**: [ ] Changes reviewed and approved
**Mobile Team Lead**: [ ] Migration plan approved
**QA Engineer**: [ ] Test scenarios documented
**Product Owner**: [ ] User experience validated
