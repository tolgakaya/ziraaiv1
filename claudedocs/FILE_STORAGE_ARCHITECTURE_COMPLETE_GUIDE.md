# File Storage Architecture - Complete Guide

**Last Updated**: 2025-10-21
**Status**: Production Active
**Complexity**: High (Hybrid Storage System)

---

## 📋 Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Storage Providers](#storage-providers)
3. [File Serving Strategy](#file-serving-strategy)
4. [Security Model](#security-model)
5. [URL Formats & Transformations](#url-formats--transformations)
6. [Database Schema](#database-schema)
7. [Request Flow Diagrams](#request-flow-diagrams)
8. [Configuration](#configuration)
9. [Troubleshooting](#troubleshooting)
10. [Migration History](#migration-history)

---

## Architecture Overview

### System Design Philosophy

**Hybrid Storage Architecture**: Different file types use different storage providers based on:
- File type (image vs document)
- Size constraints
- Security requirements
- Cost optimization

### Component Map

```
┌─────────────────────────────────────────────────────────────┐
│                    Mobile/Web Client                         │
│  (Flutter App, Angular App, API Consumers)                   │
└────────────────────┬────────────────────────────────────────┘
                     │ JWT Token Required
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              FilesController (API Gateway)                   │
│  Route: /api/v1/files/{voice-messages|attachments}/...     │
│  ✅ Authorization Check                                     │
│  ✅ URL Type Detection (External vs Local)                  │
│  ✅ Redirect or Serve Decision                              │
└────────┬───────────────────────────────────┬────────────────┘
         │                                   │
         │ External URL                      │ Local Path
         │ (FreeImageHost)                   │ (wwwroot/uploads)
         │                                   │
         ▼                                   ▼
┌──────────────────────┐          ┌──────────────────────┐
│  HTTP 302 Redirect   │          │   PhysicalFile()     │
│  to External Storage │          │   Local Disk Serve   │
└──────────────────────┘          └──────────────────────┘
         │                                   │
         ▼                                   ▼
┌──────────────────────┐          ┌──────────────────────┐
│   FreeImageHost      │          │   Railway Volume     │
│   iili.io CDN        │          │   /app/wwwroot/      │
└──────────────────────┘          └──────────────────────┘
```

---

## Storage Providers

### 1. FreeImageHost (External CDN)

**Purpose**: Image file storage with CDN delivery
**Use Cases**:
- Plant analysis images
- Message attachments (images only)
- User avatars
- Thumbnails

**Configuration**:
```json
{
  "FileStorage": {
    "FreeImageHost": {
      "ApiKey": "{{SECRET_FROM_ENV}}",
      "UploadUrl": "https://freeimage.host/api/1/upload"
    }
  }
}
```

**Advantages**:
- ✅ Free CDN bandwidth
- ✅ No storage limits
- ✅ Fast global delivery
- ✅ Automatic image optimization

**Limitations**:
- ❌ Images only (no PDFs, documents)
- ❌ External dependency (uptime)
- ❌ No direct file control

**URL Format**: `https://iili.io/{fileId}.{ext}`

**Example**:
```
Upload: msg_attachment_165_638966569169588297.jpg
Result: https://iili.io/KSoGc4R.png
```

**Code Location**: `Business/Services/FileStorage/FreeImageHostStorageService.cs`

---

### 2. Local File Storage (Railway Volume)

**Purpose**: Document and non-image file storage
**Use Cases**:
- PDF documents
- Voice messages (m4a audio)
- Office documents (docx, xlsx, pptx)
- Archives (zip, rar)
- Any non-image attachments

**Configuration**:
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

**Directory Structure**:
```
/app/wwwroot/uploads/
├── plant-images/          # Plant analysis images (rarely used now)
├── voice-messages/        # Voice message audio files
├── attachments/           # Document attachments
└── avatars/              # User avatar images (if not using FreeImageHost)
```

**Advantages**:
- ✅ Full control over files
- ✅ Supports all file types
- ✅ Low latency (same server)
- ✅ No external API limits

**Limitations**:
- ❌ Storage costs (Railway volume)
- ❌ No CDN (slower for distant users)
- ❌ Manual backup needed

**URL Format (Internal)**: `uploads/{folder}/{filename}`

**Example**:
```
Upload: voice_msg_165_638965887035374118.m4a
Stored: uploads/voice-messages/voice_msg_165_638965887035374118.m4a
Database: uploads/voice-messages/voice_msg_165_638965887035374118.m4a
```

**Code Location**: `Business/Services/FileStorage/LocalFileStorageService.cs`

---

### 3. ImgBB (Backup Provider)

**Purpose**: Alternative image hosting
**Status**: Configured but not actively used
**Fallback**: Can replace FreeImageHost if needed

**Code Location**: `Business/Services/FileStorage/ImgBBStorageService.cs`

---

### 4. AWS S3 (Future)

**Purpose**: Scalable cloud storage
**Status**: Interface defined, not implemented
**Future Use**: Production-scale file storage

**Code Location**: `Business/Services/FileStorage/S3StorageService.cs` (stub)

---

## File Serving Strategy

### Decision Matrix: Which Storage Provider?

```csharp
// From SendMessageWithAttachmentCommand.cs
var isImage = file.ContentType.StartsWith("image/");

if (isImage)
{
    // FreeImageHost (External CDN)
    url = await _imageStorage.UploadFileAsync(...);
    // Result: https://iili.io/KSoGc4R.png
}
else
{
    // Local Storage (Railway Volume)
    url = await _localStorage.UploadFileAsync(...);
    // Result: uploads/attachments/document.pdf
}
```

### FilesController Serving Logic

```csharp
// From FilesController.cs GetAttachment()

var attachmentUrl = database.GetAttachmentUrl(messageId, index);

// STEP 1: Check if external URL
if (attachmentUrl.StartsWith("http://") || attachmentUrl.StartsWith("https://"))
{
    // External (FreeImageHost, ImgBB)
    // No authorization on external provider - trust our DB
    return Redirect(attachmentUrl);  // HTTP 302
}

// STEP 2: Local file serving
var filePath = ExtractFilePathFromUrl(attachmentUrl);
var fullPath = Path.Combine(_basePath, filePath);

if (!File.Exists(fullPath))
{
    return NotFound();
}

return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
```

### Why This Hybrid Approach?

**External Storage (FreeImageHost)**:
- Images benefit from CDN
- No storage costs
- Automatic optimization

**Local Storage**:
- Documents need controlled access
- Voice messages need range support (seeking)
- Full control over sensitive files

---

## Security Model

### Authorization Architecture

**Three-Layer Security**:

1. **JWT Authentication** (All requests)
2. **Participant Authorization** (Sender OR Receiver only)
3. **Database Validation** (Message exists and active)

### Security Flow Diagram

```
Client Request
     │
     ▼
┌─────────────────────────────────────────┐
│ [Authorize] Attribute                   │
│ Validates JWT Token                     │
│ Extracts UserId from Claims             │
└────────┬────────────────────────────────┘
         │ userId = 165
         ▼
┌─────────────────────────────────────────┐
│ Get Message from Database               │
│ message = Get(messageId = 22)           │
└────────┬────────────────────────────────┘
         │ message.FromUserId = 165
         │ message.ToUserId = 159
         ▼
┌─────────────────────────────────────────┐
│ Authorization Check                     │
│ if (message.FromUserId != userId &&     │
│     message.ToUserId != userId)         │
│     return Forbid()                     │
└────────┬────────────────────────────────┘
         │ ✅ userId matches sender
         ▼
┌─────────────────────────────────────────┐
│ Serve File or Redirect                  │
│ - External: Redirect to FreeImageHost   │
│ - Local: Serve from disk                │
└─────────────────────────────────────────┘
```

### Security Considerations by Storage Type

#### External Storage (FreeImageHost)

**Security Model**: Redirect-based

**Pros**:
- ✅ Publicly accessible URLs (no auth on CDN)
- ✅ Fast delivery (no proxy overhead)
- ✅ Authorization happens BEFORE redirect

**Cons**:
- ❌ URL can be shared (once obtained)
- ❌ No expiration mechanism
- ❌ No access revocation after redirect

**Mitigation**:
- Authorization required to GET the redirect URL
- Only participants can access /api/v1/files/attachments/{messageId}/{index}
- If message deleted, 404 returned (no redirect)

**Risk Assessment**: **Medium**
- Acceptable for non-sensitive images
- Once URL obtained, publicly accessible
- Consider signed URLs for sensitive images (future enhancement)

#### Local Storage (Voice Messages, Documents)

**Security Model**: Proxy-based

**Pros**:
- ✅ Full access control on every request
- ✅ No URL sharing (requires authentication)
- ✅ Range request support (audio seeking)
- ✅ Audit logging

**Cons**:
- ❌ Server bandwidth usage
- ❌ Latency overhead

**Risk Assessment**: **Low**
- Complete control over access
- Comprehensive audit trail
- Suitable for sensitive documents

---

## URL Formats & Transformations

### URL Journey: Upload → Database → API → Client

#### Scenario 1: Image Attachment (FreeImageHost)

**1. Upload Phase**:
```csharp
// SendMessageWithAttachmentCommand.cs
var url = await _imageStorage.UploadFileAsync(...);
// url = "https://iili.io/KSoGc4R.png"
```

**2. Database Storage**:
```json
{
  "AttachmentUrls": "[\"https://iili.io/KSoGc4R.png\"]"
}
```

**3. API Response (GetConversationQuery)**:
```json
{
  "attachmentUrls": [
    "https://ziraai-api-sit.up.railway.app/api/v1/files/attachments/22/0"
  ],
  "attachmentThumbnails": [
    "https://ziraai-api-sit.up.railway.app/api/v1/files/attachments/22/0"
  ]
}
```

**4. Client Request**:
```
GET /api/v1/files/attachments/22/0
Authorization: Bearer eyJhbGci...
```

**5. FilesController Response**:
```
HTTP 302 Found
Location: https://iili.io/KSoGc4R.png
```

**6. Client Follow Redirect**:
```
GET https://iili.io/KSoGc4R.png
(Direct to FreeImageHost CDN - no auth needed)
```

---

#### Scenario 2: Voice Message (Local Storage)

**1. Upload Phase**:
```csharp
// SendVoiceMessageCommand.cs
var url = await _localFileStorage.UploadFileAsync(...);
// url = "https://ziraai-api-sit.up.railway.app/uploads/voice-messages/voice_msg_165_12345.m4a"
```

**2. Database Storage**:
```json
{
  "VoiceMessageUrl": "https://ziraai-api-sit.up.railway.app/uploads/voice-messages/voice_msg_165_12345.m4a"
}
```

**3. API Response (GetConversationQuery)**:
```json
{
  "voiceMessageUrl": "https://ziraai-api-sit.up.railway.app/api/v1/files/voice-messages/165"
}
```

**4. Client Request**:
```
GET /api/v1/files/voice-messages/165
Authorization: Bearer eyJhbGci...
Range: bytes=0-1023
```

**5. FilesController Response**:
```
HTTP 200 OK
Content-Type: audio/m4a
Content-Range: bytes 0-1023/245678
Accept-Ranges: bytes

[Binary audio data]
```

---

#### Scenario 3: Document Attachment (Local Storage)

**1. Upload Phase**:
```csharp
// SendMessageWithAttachmentCommand.cs
var url = await _localStorage.UploadFileAsync(..., folder: "attachments");
// url = "https://ziraai-api-sit.up.railway.app/uploads/attachments/report_165_12345.pdf"
```

**2. Database Storage**:
```json
{
  "AttachmentUrls": "[\"https://ziraai-api-sit.up.railway.app/uploads/attachments/report_165_12345.pdf\"]"
}
```

**3. API Response**:
```json
{
  "attachmentUrls": [
    "https://ziraai-api-sit.up.railway.app/api/v1/files/attachments/22/0"
  ]
}
```

**4. Client Request**:
```
GET /api/v1/files/attachments/22/0
Authorization: Bearer eyJhbGci...
```

**5. FilesController Response**:
```
HTTP 200 OK
Content-Type: application/pdf
Content-Length: 1024567

[Binary PDF data]
```

---

### URL Transformation Table

| Phase | Image (FreeImageHost) | Voice (Local) | Document (Local) |
|-------|----------------------|---------------|------------------|
| **Upload Result** | `https://iili.io/KSoGc4R.png` | `https://ziraai.../uploads/voice-messages/file.m4a` | `https://ziraai.../uploads/attachments/file.pdf` |
| **Database Storage** | `https://iili.io/KSoGc4R.png` | `https://ziraai.../uploads/voice-messages/file.m4a` | `https://ziraai.../uploads/attachments/file.pdf` |
| **API Endpoint** | `/api/v1/files/attachments/22/0` | `/api/v1/files/voice-messages/165` | `/api/v1/files/attachments/22/0` |
| **FilesController Action** | Redirect to FreeImageHost | Serve from disk | Serve from disk |
| **Final URL** | `https://iili.io/KSoGc4R.png` | `/app/wwwroot/uploads/voice-messages/file.m4a` | `/app/wwwroot/uploads/attachments/file.pdf` |

---

## Database Schema

### AnalysisMessage Table

**Relevant Fields**:

```sql
CREATE TABLE "AnalysisMessages" (
    "Id" INTEGER PRIMARY KEY,
    "PlantAnalysisId" INTEGER NOT NULL,
    "FromUserId" INTEGER NOT NULL,
    "ToUserId" INTEGER NOT NULL,
    "Message" TEXT,
    "MessageType" VARCHAR(50),

    -- Attachment Support
    "HasAttachments" BOOLEAN DEFAULT FALSE,
    "AttachmentCount" INTEGER DEFAULT 0,
    "AttachmentUrls" TEXT,        -- JSON array of URLs
    "AttachmentTypes" TEXT,       -- JSON array of MIME types
    "AttachmentSizes" TEXT,       -- JSON array of file sizes
    "AttachmentNames" TEXT,       -- JSON array of file names

    -- Voice Message Support
    "VoiceMessageUrl" TEXT,       -- Single URL
    "VoiceMessageDuration" INTEGER,
    "VoiceMessageWaveform" TEXT,  -- JSON waveform data

    "IsDeleted" BOOLEAN DEFAULT FALSE,
    "CreatedDate" TIMESTAMP NOT NULL,
    "SentDate" TIMESTAMP NOT NULL
);
```

### Example Data

**Image Attachment Message**:
```json
{
  "Id": 22,
  "HasAttachments": true,
  "AttachmentCount": 1,
  "AttachmentUrls": "[\"https://iili.io/KSoGc4R.png\"]",
  "AttachmentTypes": "[\"image/png\"]",
  "AttachmentSizes": "[245678]",
  "AttachmentNames": "[\"plant_disease.png\"]"
}
```

**Voice Message**:
```json
{
  "Id": 165,
  "MessageType": "VoiceMessage",
  "VoiceMessageUrl": "https://ziraai-api-sit.up.railway.app/uploads/voice-messages/voice_msg_165_12345.m4a",
  "VoiceMessageDuration": 42,
  "VoiceMessageWaveform": "[0.2, 0.5, 0.8, ...]"
}
```

**Document Attachment Message**:
```json
{
  "Id": 30,
  "HasAttachments": true,
  "AttachmentCount": 2,
  "AttachmentUrls": "[\"https://ziraai.../uploads/attachments/report.pdf\", \"https://ziraai.../uploads/attachments/data.xlsx\"]",
  "AttachmentTypes": "[\"application/pdf\", \"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\"]",
  "AttachmentSizes": "[1024567, 567890]",
  "AttachmentNames": "[\"analysis_report.pdf\", \"raw_data.xlsx\"]"
}
```

---

## Request Flow Diagrams

### Flow 1: Image Attachment Access (FreeImageHost)

```
┌─────────────┐
│ Mobile App  │
└──────┬──────┘
       │ 1. GET /api/v1/files/attachments/22/0
       │    Authorization: Bearer {JWT}
       ▼
┌─────────────────────────────────────┐
│ FilesController                     │
│ ├─ Validate JWT ✅                  │
│ ├─ userId = 165                     │
│ └─ GetAttachment(22, 0)             │
└──────┬──────────────────────────────┘
       │ 2. SELECT * FROM AnalysisMessages WHERE Id=22
       ▼
┌─────────────────────────────────────┐
│ PostgreSQL Database                 │
│ Returns:                            │
│ {                                   │
│   FromUserId: 165,                  │
│   ToUserId: 159,                    │
│   AttachmentUrls: '["https://..."]'│
│ }                                   │
└──────┬──────────────────────────────┘
       │ 3. Authorization Check
       │    165 == 165 ✅ (sender)
       ▼
┌─────────────────────────────────────┐
│ FilesController                     │
│ ├─ Parse: ["https://iili.io/..."]  │
│ ├─ Check: StartsWith("https://")   │
│ ├─ Decision: External URL          │
│ └─ return Redirect(url)             │
└──────┬──────────────────────────────┘
       │ 4. HTTP 302 Found
       │    Location: https://iili.io/KSoGc4R.png
       ▼
┌─────────────┐
│ Mobile App  │ 5. Follow redirect
└──────┬──────┘    (automatic)
       │
       ▼
┌─────────────────────────────────────┐
│ FreeImageHost CDN                   │
│ ├─ Public URL (no auth)             │
│ └─ Returns image binary             │
└──────┬──────────────────────────────┘
       │ 6. HTTP 200 OK
       │    Content-Type: image/png
       │    [Binary image data]
       ▼
┌─────────────┐
│ Mobile App  │ Display image ✅
└─────────────┘
```

---

### Flow 2: Voice Message Access (Local Storage)

```
┌─────────────┐
│ Mobile App  │
└──────┬──────┘
       │ 1. GET /api/v1/files/voice-messages/165
       │    Authorization: Bearer {JWT}
       │    Range: bytes=0-1023
       ▼
┌─────────────────────────────────────┐
│ FilesController                     │
│ ├─ Validate JWT ✅                  │
│ ├─ userId = 165                     │
│ └─ GetVoiceMessage(165)             │
└──────┬──────────────────────────────┘
       │ 2. SELECT * FROM AnalysisMessages WHERE Id=165
       ▼
┌─────────────────────────────────────┐
│ PostgreSQL Database                 │
│ Returns:                            │
│ {                                   │
│   FromUserId: 165,                  │
│   ToUserId: 159,                    │
│   VoiceMessageUrl: "https://ziraai.│
│     ../uploads/voice-messages/...m4a"│
│ }                                   │
└──────┬──────────────────────────────┘
       │ 3. Authorization Check
       │    165 == 165 ✅ (sender)
       ▼
┌─────────────────────────────────────┐
│ FilesController                     │
│ ├─ Extract: "uploads/voice-messages│
│ │   /voice_msg_165_12345.m4a"      │
│ ├─ Full path: "/app/wwwroot/uploads│
│ │   /voice-messages/voice_msg_..."  │
│ ├─ File.Exists() ✅                 │
│ └─ PhysicalFile(path, "audio/m4a") │
└──────┬──────────────────────────────┘
       │ 4. HTTP 206 Partial Content
       │    Content-Type: audio/m4a
       │    Content-Range: bytes 0-1023/245678
       │    Accept-Ranges: bytes
       │    [Binary audio chunk]
       ▼
┌─────────────┐
│ Mobile App  │ Play audio ✅
└─────────────┘ (supports seeking)
```

---

### Flow 3: Unauthorized Access Attempt

```
┌─────────────┐
│ Attacker    │ Different user trying to access
└──────┬──────┘
       │ 1. GET /api/v1/files/attachments/22/0
       │    Authorization: Bearer {DIFFERENT_USER_JWT}
       ▼
┌─────────────────────────────────────┐
│ FilesController                     │
│ ├─ Validate JWT ✅                  │
│ ├─ userId = 999 (different user)   │
│ └─ GetAttachment(22, 0)             │
└──────┬──────────────────────────────┘
       │ 2. SELECT * FROM AnalysisMessages WHERE Id=22
       ▼
┌─────────────────────────────────────┐
│ PostgreSQL Database                 │
│ Returns:                            │
│ {                                   │
│   FromUserId: 165,                  │
│   ToUserId: 159,                    │
│   AttachmentUrls: '["https://..."]'│
│ }                                   │
└──────┬──────────────────────────────┘
       │ 3. Authorization Check
       │    999 != 165 ❌ (not sender)
       │    999 != 159 ❌ (not receiver)
       ▼
┌─────────────────────────────────────┐
│ FilesController                     │
│ ├─ Authorization FAILED             │
│ ├─ Log: Unauthorized access attempt │
│ └─ return Forbid()                  │
└──────┬──────────────────────────────┘
       │ 4. HTTP 403 Forbidden
       ▼
┌─────────────┐
│ Attacker    │ Access denied ❌
└─────────────┘
```

---

## Configuration

### Development Environment

**File**: `WebAPI/appsettings.Development.json`

```json
{
  "FileStorage": {
    "Local": {
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "https://localhost:5001"
    },
    "FreeImageHost": {
      "ApiKey": "6d207e02198a847aa98d0a2a901485a5",
      "UploadUrl": "https://freeimage.host/api/1/upload"
    },
    "ImgBB": {
      "ApiKey": "your-imgbb-api-key",
      "UploadUrl": "https://api.imgbb.com/1/upload"
    }
  }
}
```

---

### Staging Environment

**File**: `WebAPI/appsettings.Staging.json`

```json
{
  "FileStorage": {
    "Local": {
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "https://ziraai-api-sit.up.railway.app"
    },
    "FreeImageHost": {
      "ApiKey": "{{FROM_RAILWAY_ENV}}",
      "UploadUrl": "https://freeimage.host/api/1/upload"
    }
  }
}
```

**Railway Environment Variables**:
- `ASPNETCORE_ENVIRONMENT=Staging`
- `FileStorage__FreeImageHost__ApiKey={secret}`

---

### Production Environment

**File**: `WebAPI/appsettings.json` (gitignored)

```json
{
  "FileStorage": {
    "Local": {
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "https://ziraai.com"
    },
    "FreeImageHost": {
      "ApiKey": "{{FROM_RAILWAY_ENV}}",
      "UploadUrl": "https://freeimage.host/api/1/upload"
    }
  }
}
```

---

### Storage Provider Selection

**File**: `Business/DependencyResolvers/AutofacBusinessModule.cs`

```csharp
// Primary image storage provider
builder.RegisterType<FreeImageHostStorageService>()
    .As<IFileStorageService>()
    .Named<IFileStorageService>("imageStorage")
    .SingleInstance();

// Local storage for documents and voice
builder.RegisterType<LocalFileStorageService>()
    .AsSelf()
    .SingleInstance();

// Backup image storage (not used currently)
builder.RegisterType<ImgBBStorageService>()
    .Named<IFileStorageService>("imgbbStorage")
    .SingleInstance();
```

**Usage in Commands**:
```csharp
public class SendMessageWithAttachmentCommand
{
    private readonly IFileStorageService _imageStorage;  // FreeImageHost
    private readonly LocalFileStorageService _localStorage;  // Local

    public SendMessageWithAttachmentCommandHandler(
        [KeyFilter("imageStorage")] IFileStorageService imageStorage,
        LocalFileStorageService localStorage)
    {
        _imageStorage = imageStorage;
        _localStorage = localStorage;
    }
}
```

---

## Troubleshooting

### Problem 1: 404 on Image Attachments

**Symptoms**:
```
❌ THUMBNAIL ERROR: https://ziraai.../api/v1/files/attachments/22/0
Error: HttpException: Invalid statusCode: 404
```

**Backend Logs**:
```
[ERR] Attachment file missing on disk. Message: 22, Path: /app/wwwroot/uploads/KSoGc4R.png
```

**Root Cause**: Image stored on FreeImageHost but FilesController trying to serve from local disk

**Diagnosis**:
```sql
-- Check database URL
SELECT "AttachmentUrls" FROM "AnalysisMessages" WHERE "Id" = 22;
-- Result: ["https://iili.io/KSoGc4R.png"]
```

**Solution**: FilesController now redirects external URLs (commit 9adb020)

**Verification**:
```bash
# Should see redirect log
grep "Attachment redirect to external storage" WebApi.log
```

---

### Problem 2: Voice Messages Not Playing

**Symptoms**:
- Audio player fails to start
- 404 or 403 errors

**Diagnosis Checklist**:
1. **Check JWT Token**:
   ```
   Mobile log should show: JWT: eyJhbGci...
   ```

2. **Check Authorization**:
   ```sql
   SELECT "FromUserId", "ToUserId"
   FROM "AnalysisMessages"
   WHERE "Id" = 165;
   -- User must match one of these
   ```

3. **Check File Exists**:
   ```bash
   ls -la /app/wwwroot/uploads/voice-messages/
   ```

4. **Check Backend Logs**:
   ```
   [INF] Voice message accessed. User: 165, Message: 165
   OR
   [ERR] Voice file missing on disk
   ```

**Common Issues**:
- JWT expired: 401 Unauthorized
- Not participant: 403 Forbidden
- File deleted: 404 Not Found

---

### Problem 3: Thumbnails Not Showing

**Symptoms**:
- Icons shown instead of image thumbnails
- `attachmentThumbnails` is null in response

**Root Cause**: Backend not returning `AttachmentThumbnails` field

**Check API Response**:
```json
{
  "attachmentUrls": ["..."],
  "attachmentThumbnails": null  // ❌ Should be array
}
```

**Solution**: GetConversationQuery and SendMessageWithAttachmentCommand updated (commit 645b9a2)

**Verification**:
```json
{
  "attachmentUrls": ["https://.../api/v1/files/attachments/22/0"],
  "attachmentThumbnails": ["https://.../api/v1/files/attachments/22/0"]  // ✅
}
```

---

### Problem 4: API Versioning 404

**Symptoms**:
```
[ERR] Route not found: /api/v1/files/attachments/22/0
```

**Root Cause**: FilesController using `api/v{version:apiVersion}/files` but app calling `/api/v1/...`

**Diagnosis**:
```csharp
// Check route attribute
[Route("api/v{version:apiVersion}/files")]  // ❌ Requires header
vs
[Route("api/v1/files")]  // ✅ Direct URL match
```

**Solution**: Changed to fixed v1 route (commit b2d49a5)

---

### Problem 5: External URL Stored in Database

**Symptoms**:
```
Database: https://iili.io/KSoGc4R.png
FilesController tries: /app/wwwroot/uploads/KSoGc4R.png
Result: 404 Not Found
```

**Root Cause**: Hybrid storage - images on FreeImageHost, FilesController expects local

**Solution**: Detect external URLs and redirect (commit 9adb020)

```csharp
if (attachmentUrl.StartsWith("http"))
{
    return Redirect(attachmentUrl);  // ✅
}
```

---

## Migration History

### Phase 1: Public File URLs (REJECTED)

**Date**: 2025-10-18
**Implementation**: Direct physical URLs
**URL Format**: `https://ziraai.com/uploads/voice-messages/voice_msg_123.m4a`

**Problems**:
- ❌ No authorization
- ❌ Predictable URLs
- ❌ Cross-user access possible
- ❌ No audit trail

**Status**: Rejected by product owner

---

### Phase 2: Signed URLs (REJECTED)

**Date**: 2025-10-18
**Implementation**: Time-limited signed URLs (15 minutes)
**URL Format**: `https://ziraai.com/uploads/voice-messages/voice_msg_123.m4a?signature=xyz&expires=123456`

**Problems**:
- ❌ Old messages couldn't be played after 15 minutes
- ❌ Complex URL generation
- ❌ User feedback: "Bu implementasyonu beğenmedim"

**Status**: Rejected by product owner

**Commit**: 7693997 (later reverted)

---

### Phase 3: Controller-Based Serving (CURRENT)

**Date**: 2025-10-19
**Implementation**: FilesController with JWT auth
**URL Format**: `https://ziraai.com/api/v1/files/voice-messages/165`

**Advantages**:
- ✅ JWT authentication required
- ✅ Authorization check (sender/receiver only)
- ✅ No URL expiration
- ✅ Comprehensive audit logging
- ✅ Range request support

**Status**: Production Active

**Commits**:
- 9313878: Initial FilesController
- c80ed62: Fix URL storage (physical in DB, API in response)
- 2624f01: Dynamic BaseUrl for HTTPS
- 7711039: Documentation
- 645b9a2: Add AttachmentThumbnails
- b2d49a5: Fix API versioning route
- 9adb020: Handle external URLs (FreeImageHost)

---

### Phase 4: Hybrid Storage Implementation

**Date**: 2025-10-21
**Implementation**: FreeImageHost for images + Local for documents

**Why Hybrid**:
- Images: CDN delivery, no storage costs
- Documents: Full control, security
- Voice: Range support for seeking

**Status**: Production Active

---

## Best Practices

### For Backend Developers

1. **Always Use Appropriate Storage**:
   ```csharp
   if (file.ContentType.StartsWith("image/"))
       url = await _imageStorage.UploadFileAsync(...);  // External
   else
       url = await _localStorage.UploadFileAsync(...);  // Local
   ```

2. **Store Physical URLs in Database**:
   ```csharp
   message.VoiceMessageUrl = physicalUrl;  // Store as-is
   // Transform to API endpoint in DTO only
   ```

3. **Never Expose Physical Paths**:
   ```csharp
   // ❌ WRONG
   return new { url: "/app/wwwroot/uploads/file.pdf" };

   // ✅ CORRECT
   return new { url: $"{baseUrl}/api/v1/files/attachments/{id}/{index}" };
   ```

4. **Always Log Access**:
   ```csharp
   _logger.LogInformation(
       "File accessed. User: {UserId}, Message: {MessageId}",
       userId, messageId);
   ```

---

### For Mobile Developers

1. **Always Send JWT Token**:
   ```dart
   httpHeaders: {
     'Authorization': 'Bearer $token',
   }
   ```

2. **Handle Redirects Automatically**:
   ```dart
   // Most HTTP clients follow 302 redirects automatically
   final response = await http.get(url, headers: headers);
   // Will automatically follow to FreeImageHost
   ```

3. **Handle All HTTP Status Codes**:
   ```dart
   switch (response.statusCode) {
     case 200: // Success - local file
     case 302: // Redirect - external file
     case 401: // Refresh token
     case 403: // Access denied (shouldn't happen)
     case 404: // File not found
   }
   ```

4. **Cache Appropriately**:
   ```dart
   // External URLs (FreeImageHost) - cache aggressively
   CachedNetworkImage(imageUrl: externalUrl);

   // API endpoints - respect auth and expiry
   // (still works - controller handles redirect)
   ```

---

### For DevOps

1. **Railway Volume Backup**:
   ```bash
   # Backup uploads directory
   tar -czf uploads-backup-$(date +%Y%m%d).tar.gz /app/wwwroot/uploads/
   ```

2. **Monitor Storage Usage**:
   ```bash
   du -sh /app/wwwroot/uploads/*
   ```

3. **Log Monitoring**:
   ```bash
   # Check for file access errors
   grep "file missing on disk" WebApi.log

   # Check for unauthorized access attempts
   grep "Unauthorized.*access attempt" WebApi.log
   ```

4. **Environment Variables**:
   ```bash
   # Ensure these are set in Railway
   FileStorage__FreeImageHost__ApiKey
   FileStorage__Local__BaseUrl
   ```

---

## Future Enhancements

### Planned Improvements

1. **Thumbnail Generation** (Backend)
   - Generate actual thumbnails instead of using full-size
   - Resize images to 200x200 for chat list
   - Store in separate folder: `uploads/thumbnails/`

2. **CDN for Local Files**
   - Move local storage to CloudFlare R2 or AWS S3
   - Add CDN for global delivery
   - Maintain same authorization model

3. **Signed URLs for Images** (Security)
   - Generate time-limited tokens for FreeImageHost redirects
   - Prevent URL sharing
   - Configurable expiry

4. **File Compression**
   - Compress PDFs before storage
   - Optimize images automatically
   - Reduce storage costs

5. **Malware Scanning**
   - Scan uploads before storage
   - ClamAV integration
   - Quarantine suspicious files

6. **Rate Limiting**
   - Limit file access per user
   - Prevent DOS attacks
   - Alert on suspicious patterns

---

## Quick Reference

### File Type → Storage Provider

| File Type | Storage | URL Example |
|-----------|---------|-------------|
| JPEG, PNG, GIF, WebP | FreeImageHost | `https://iili.io/KSoGc4R.png` |
| PDF, DOCX, XLSX | Local | `uploads/attachments/doc.pdf` |
| M4A (voice) | Local | `uploads/voice-messages/voice.m4a` |
| ZIP, RAR | Local | `uploads/attachments/archive.zip` |

### HTTP Status Codes

| Code | Meaning | Action |
|------|---------|--------|
| 200 | Local file served | Display content |
| 206 | Partial content (range) | Continue buffering |
| 302 | Redirect to external | Follow automatically |
| 401 | Token expired | Refresh and retry |
| 403 | Not authorized | Don't retry |
| 404 | File not found | Show error |

### Key Files

| Component | File Path |
|-----------|-----------|
| FilesController | `WebAPI/Controllers/FilesController.cs` |
| FreeImageHost Service | `Business/Services/FileStorage/FreeImageHostStorageService.cs` |
| Local Storage Service | `Business/Services/FileStorage/LocalFileStorageService.cs` |
| Send Attachment Command | `Business/Handlers/AnalysisMessages/Commands/SendMessageWithAttachmentCommand.cs` |
| Send Voice Command | `Business/Handlers/AnalysisMessages/Commands/SendVoiceMessageCommand.cs` |
| Get Conversation Query | `Business/Handlers/AnalysisMessages/Queries/GetConversationQuery.cs` |

---

## Support & Contact

**Questions**: Check this document first
**Issues**: See Troubleshooting section
**Updates**: Document updated with each major change

**Last Major Update**: 2025-10-21 (External URL redirect implementation)

---

**End of Document**
