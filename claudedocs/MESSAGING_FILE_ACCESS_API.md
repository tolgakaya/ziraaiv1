# Messaging File Access API Documentation

## Overview

This document describes the secure file access endpoints for voice messages and attachments in the messaging system. Files are served through controller endpoints with JWT authentication and authorization checks.

## Security Model

### Authorization
- **JWT Bearer Token Required**: All endpoints require valid authentication
- **Participant-Only Access**: Only message sender or receiver can access files
- **Database Validation**: Message existence and authorization checked before serving
- **Audit Logging**: All access attempts logged with user ID and file details

### URL Format
Files are accessed through API endpoints (not direct physical URLs):
- Voice messages: `{BaseUrl}/api/v1/files/voice-messages/{messageId}`
- Attachments: `{BaseUrl}/api/v1/files/attachments/{messageId}/{attachmentIndex}`

## Endpoints

### 1. Get Voice Message File

**Endpoint:** `GET /api/v1/files/voice-messages/{messageId}`

**Description:** Retrieve voice message audio file with authorization check

**Authentication:** Required (JWT Bearer)

**Authorization:** Only sender or receiver of the message

**Parameters:**
- `messageId` (path, int): The ID of the message containing the voice recording

**Responses:**

**200 OK** - Voice message file
- Content-Type: `audio/m4a`
- Range support enabled (allows audio seeking)
- Returns: Audio file stream

**401 Unauthorized** - Authentication required
```json
{
  "success": false,
  "message": "Authentication required"
}
```

**403 Forbidden** - User is not authorized to access this file
- User is neither sender nor receiver of the message

**404 Not Found** - Message or file not found
```json
{
  "success": false,
  "message": "Voice message not found"
}
```
or
```json
{
  "success": false,
  "message": "Voice file not found"
}
```
or
```json
{
  "success": false,
  "message": "Voice file not found on server"
}
```

**Example Request:**
```http
GET /api/v1/files/voice-messages/165 HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Example Response:**
```
HTTP/1.1 200 OK
Content-Type: audio/m4a
Content-Length: 245678
Accept-Ranges: bytes

[Binary audio data]
```

**Audit Log Entry:**
```
Voice message accessed. User: 123, Message: 165, File: voice_msg_165_638965887035374118_1760991903.m4a, Size: 245678 bytes
```

---

### 2. Get Attachment File

**Endpoint:** `GET /api/v1/files/attachments/{messageId}/{attachmentIndex}`

**Description:** Retrieve message attachment file with authorization check

**Authentication:** Required (JWT Bearer)

**Authorization:** Only sender or receiver of the message

**Parameters:**
- `messageId` (path, int): The ID of the message containing attachments
- `attachmentIndex` (path, int): Zero-based index of the attachment (0 = first attachment)

**Responses:**

**200 OK** - Attachment file
- Content-Type: Determined by file extension (see Content Types section)
- Range support enabled
- Returns: File stream

**401 Unauthorized** - Authentication required
```json
{
  "success": false,
  "message": "Authentication required"
}
```

**403 Forbidden** - User is not authorized to access this file
- User is neither sender nor receiver of the message

**404 Not Found** - Message, attachment, or file not found
```json
{
  "success": false,
  "message": "Message not found"
}
```
or
```json
{
  "success": false,
  "message": "Attachment not found"
}
```
or
```json
{
  "success": false,
  "message": "Invalid attachment data"
}
```
or
```json
{
  "success": false,
  "message": "Attachment file not found on server"
}
```

**Example Request:**
```http
GET /api/v1/files/attachments/167/0 HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Example Response:**
```
HTTP/1.1 200 OK
Content-Type: image/jpeg
Content-Length: 156789
Accept-Ranges: bytes

[Binary image data]
```

**Audit Log Entry:**
```
Attachment accessed. User: 123, Message: 167, Index: 0, File: attachment_167_0_638965887100123456_987654321.jpg, Type: image/jpeg, Size: 156789 bytes
```

---

## Content Types

The API automatically determines content type based on file extension:

### Audio
- `.m4a` → `audio/m4a`
- `.mp3` → `audio/mpeg`
- `.aac` → `audio/aac`
- `.wav` → `audio/wav`
- `.ogg` → `audio/ogg`

### Images
- `.jpg`, `.jpeg` → `image/jpeg`
- `.png` → `image/png`
- `.gif` → `image/gif`
- `.webp` → `image/webp`
- `.bmp` → `image/bmp`
- `.svg` → `image/svg+xml`
- `.tiff`, `.tif` → `image/tiff`

### Video
- `.mp4` → `video/mp4`
- `.mov` → `video/quicktime`
- `.avi` → `video/x-msvideo`
- `.webm` → `video/webm`

### Documents
- `.pdf` → `application/pdf`
- `.doc` → `application/msword`
- `.docx` → `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- `.xls` → `application/vnd.ms-excel`
- `.xlsx` → `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- `.ppt` → `application/vnd.ms-powerpoint`
- `.pptx` → `application/vnd.openxmlformats-officedocument.presentationml.presentation`
- `.txt` → `text/plain`

### Archives
- `.zip` → `application/zip`
- `.rar` → `application/x-rar-compressed`
- `.7z` → `application/x-7z-compressed`

### Default
- Unknown extensions → `application/octet-stream`

---

## Range Support

Both endpoints support HTTP range requests, enabling:
- **Audio Seeking**: Jump to specific timestamps in voice messages
- **Resumable Downloads**: Resume interrupted downloads
- **Partial Content**: Download only needed portions

**Example Range Request:**
```http
GET /api/v1/files/voice-messages/165 HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Range: bytes=100000-200000
```

**Example Range Response:**
```
HTTP/1.1 206 Partial Content
Content-Type: audio/m4a
Content-Range: bytes 100000-200000/245678
Content-Length: 100001
Accept-Ranges: bytes

[Binary audio data from byte 100000 to 200000]
```

---

## Error Handling

### Authentication Errors
- **401 Unauthorized**: Missing or invalid JWT token
  - Action: Refresh token or re-authenticate

### Authorization Errors
- **403 Forbidden**: User is not participant in message
  - Action: This is expected - user should not access other users' files
  - Logged as security event

### Not Found Errors
- **404 Not Found**: Message, attachment index, or physical file missing
  - Possible causes:
    - Message deleted
    - Invalid attachment index
    - File moved/deleted from storage
    - Database inconsistency

---

## Integration Examples

### Flutter (Mobile App)

#### Playing Voice Message
```dart
import 'package:audioplayers/audioplayers.dart';
import 'package:http/http.dart' as http;

Future<void> playVoiceMessage(int messageId, String token) async {
  final url = 'https://ziraai-api-sit.up.railway.app/api/v1/files/voice-messages/$messageId';

  final player = AudioPlayer();

  await player.play(UrlSource(url),
    headers: {
      'Authorization': 'Bearer $token',
    },
  );
}
```

#### Displaying Attachment Image
```dart
import 'package:cached_network_image/cached_network_image.dart';

Widget buildAttachmentImage(int messageId, int index, String token) {
  final url = 'https://ziraai-api-sit.up.railway.app/api/v1/files/attachments/$messageId/$index';

  return CachedNetworkImage(
    imageUrl: url,
    httpHeaders: {
      'Authorization': 'Bearer $token',
    },
    placeholder: (context, url) => CircularProgressIndicator(),
    errorWidget: (context, url, error) => Icon(Icons.error),
  );
}
```

#### Downloading Attachment
```dart
import 'package:dio/dio.dart';

Future<void> downloadAttachment(int messageId, int index, String token, String savePath) async {
  final url = 'https://ziraai-api-sit.up.railway.app/api/v1/files/attachments/$messageId/$index';

  final dio = Dio();
  dio.options.headers['Authorization'] = 'Bearer $token';

  await dio.download(url, savePath,
    onReceiveProgress: (received, total) {
      if (total != -1) {
        print('${(received / total * 100).toStringAsFixed(0)}%');
      }
    },
  );
}
```

### JavaScript/React

#### Playing Voice Message
```javascript
async function playVoiceMessage(messageId, token) {
  const url = `https://ziraai-api-sit.up.railway.app/api/v1/files/voice-messages/${messageId}`;

  const audio = new Audio();
  audio.src = url;
  audio.headers = {
    'Authorization': `Bearer ${token}`
  };

  await audio.play();
}
```

#### Displaying Attachment
```jsx
function AttachmentImage({ messageId, index, token }) {
  const url = `https://ziraai-api-sit.up.railway.app/api/v1/files/attachments/${messageId}/${index}`;

  return (
    <img
      src={url}
      alt="Attachment"
      headers={{
        'Authorization': `Bearer ${token}`
      }}
    />
  );
}
```

---

## URL Migration from Previous Implementation

### Previous Format (Physical URLs)
```
https://ziraai-api-sit.up.railway.app/voice-messages/voice_msg_165_638965887035374118_1760991903.m4a
```

### New Format (API Endpoints)
```
https://ziraai-api-sit.up.railway.app/api/v1/files/voice-messages/165
```

### Why Changed?
1. **Security**: Authorization checked on every request
2. **No Expiration**: URLs work indefinitely (as long as user is participant)
3. **Audit Trail**: All access logged for security monitoring
4. **Flexibility**: Can change storage backend without URL changes
5. **Consistency**: Standard REST API pattern

### Migration Notes
- Database still stores physical paths internally
- API responses return new endpoint URLs
- FilesController translates endpoint URLs to physical paths
- Backward compatible with old physical URLs in database

---

## Security Best Practices

### For Client Applications

1. **Token Management**
   - Store JWT securely (iOS Keychain, Android KeyStore)
   - Refresh tokens before expiration
   - Clear tokens on logout

2. **Error Handling**
   - Handle 401: Refresh token or re-authenticate
   - Handle 403: Don't retry, user not authorized
   - Handle 404: Message may be deleted, update UI

3. **Caching**
   - Cache files locally to reduce API calls
   - Respect cache-control headers
   - Clear cache on logout

4. **Network Security**
   - Always use HTTPS
   - Validate SSL certificates
   - Implement certificate pinning for production

### For Backend

1. **Authorization**
   - Always verify user is message participant
   - Check message exists and is not deleted
   - Log unauthorized access attempts

2. **File Validation**
   - Verify file exists before serving
   - Validate file extensions
   - Scan for malware in production

3. **Rate Limiting**
   - Implement rate limits per user
   - Monitor for abuse patterns
   - Alert on suspicious activity

4. **Monitoring**
   - Track file access patterns
   - Monitor file storage usage
   - Alert on missing files

---

## Configuration

### appsettings.json
```json
{
  "FileStorage": {
    "Local": {
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "https://ziraai-api-sit.up.railway.app"
    }
  }
}
```

### Environment-Specific URLs
- **Development**: `https://localhost:5001`
- **Staging**: `https://ziraai-api-sit.up.railway.app`
- **Production**: `https://ziraai.com` (or production domain)

---

## Troubleshooting

### Voice Message Won't Play
1. Check JWT token is valid and not expired
2. Verify user is sender or receiver of message
3. Check message exists and is not deleted
4. Verify file exists on server storage
5. Check audio player supports m4a format
6. Review server logs for detailed error

### Attachment Not Loading
1. Verify attachment index is correct (0-based)
2. Check message has attachments
3. Verify user authorization
4. Check file exists on server
5. Verify content type is supported by client
6. Review server logs for errors

### 403 Forbidden Errors
- User is not sender or receiver
- This is expected security behavior
- Do not allow cross-user file access

### File Not Found Errors
- Check database and storage are in sync
- Verify file upload completed successfully
- Check file permissions on server
- Review migration/deployment logs

---

## API Versioning

Current version: `v1`

All endpoints use API versioning via route: `/api/v{version}/files/...`

Version header is also supported: `x-dev-arch-version: 1`

---

## Related Documentation

- [FILE_SECURITY_RECOMMENDATIONS.md](FILE_SECURITY_RECOMMENDATIONS.md) - Security analysis and recommendations
- [ZiraAI_Messaging_Status_Quick_Start.md](ZiraAI_Messaging_Status_Quick_Start.md) - Messaging system overview
- [ZiraAI_Messaging_Status_Implementation_Guide.md](ZiraAI_Messaging_Status_Implementation_Guide.md) - Complete implementation guide
