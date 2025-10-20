# File Storage Configuration Guide

**Last Updated**: 2025-10-19
**Feature Branch**: `feature/sponsor-farmer-chat-enhancements`

---

## 📦 Overview

All file uploads in the chat enhancement system (Avatars, Attachments, Voice Messages) use the existing `IFileStorageService` infrastructure with provider-based configuration.

---

## 🏗️ Storage Providers by File Type

### **1. Avatar & Image Attachments**

**Provider**: `FreeImageHost` (Development/Staging) or `Local` (Production)

**Used By**:
- Avatar upload (`AvatarService.cs`)
- Image attachments (`SendMessageWithAttachmentCommand.cs`)
- Document attachments (PDF, DOCX, etc.)

**Configuration**:
```json
{
  "FileStorage": {
    "Provider": "FreeImageHost",  // Auto-selected via DI
    "FreeImageHost": {
      "ApiKey": "6d207e02198a847aa98d0a2a901485a5"
    }
  }
}
```

**Upload Flow**:
```
IFormFile → AvatarService/AttachmentCommand
    ↓
IFileStorageService (injected)
    ↓
[Autofac DI selects provider based on config]
    ↓
FreeImageHostStorageService.UploadFileAsync()
    ↓
Returns: "https://freeimage.host/i/abc123"
```

**Storage Location**: External API (FreeImageHost servers)
**Persistence**: Permanent (free tier)
**Supports**: Images (JPEG, PNG, WebP, GIF), Documents (PDF)

---

### **2. Voice Messages** ⭐

**Provider**: `LocalFileStorageService` (ALWAYS - Direct injection)

**Used By**:
- Voice message upload (`SendVoiceMessageCommand.cs`)

**Why Local Only**:
- ⚠️ FreeImageHost **DOES NOT** support audio files
- Voice messages are audio (M4A, AAC, MP3)
- Direct injection bypasses global IFileStorageService provider

**Configuration**:
```json
{
  "FileStorage": {
    "Local": {
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "https://localhost:5001"  // or Railway public domain
    }
  }
}
```

**Upload Flow**:
```
IFormFile (M4A/AAC/MP3) → SendVoiceMessageCommand
    ↓
LocalFileStorageService (direct injection)
    ↓
UploadFileAsync(stream, fileName, contentType, "voice-messages")
    ↓
Saves to: wwwroot/uploads/voice-messages/voice_msg_123_638123456789.m4a
    ↓
Returns: "https://localhost:5001/voice-messages/voice_msg_123_638123456789.m4a"
```

**Storage Location**:
- **Development**: `wwwroot/uploads/voice-messages/`
- **Production (Railway)**: `wwwroot/uploads/voice-messages/` (ephemeral disk ⚠️)

**File Naming**: `voice_msg_{userId}_{timestamp}.{extension}`
**Subfolder**: `voice-messages/`

**Supported Formats**:
- `.m4a` (Apple M4A)
- `.aac` (AAC audio)
- `.mp3` (MPEG audio)

---

## 🔧 Implementation Details

### **Voice Message Command** (`SendVoiceMessageCommand.cs`)

**Key Changes**:

#### **1. Direct LocalFileStorageService Injection**
```csharp
public class SendVoiceMessageCommandHandler : IRequestHandler<SendVoiceMessageCommand, IDataResult<AnalysisMessage>>
{
    private readonly LocalFileStorageService _localFileStorage; // Direct injection

    public SendVoiceMessageCommandHandler(
        IAnalysisMessageRepository messageRepository,
        IAnalysisMessagingService messagingService,
        IMessagingFeatureService featureService,
        LocalFileStorageService localFileStorage) // Not IFileStorageService
    {
        _localFileStorage = localFileStorage;
    }
}
```

**Why Direct Injection?**
- Bypasses global `IFileStorageService` provider configuration
- Ensures voice messages ALWAYS use local storage
- FreeImageHost doesn't support audio → must use local

#### **2. Upload with Subfolder**
```csharp
// Preserve original file extension
var extension = Path.GetExtension(request.VoiceFile.FileName).ToLowerInvariant();
var fileName = $"voice_msg_{request.FromUserId}_{DateTime.Now.Ticks}{extension}";

var voiceUrl = await _localFileStorage.UploadFileAsync(
    request.VoiceFile.OpenReadStream(),
    fileName,
    request.VoiceFile.ContentType,
    "voice-messages"); // ← Subfolder for organization
```

**Result**:
- Development: `https://localhost:5001/voice-messages/voice_msg_2_638123456789.m4a`
- Production: `https://railway-domain.up.railway.app/voice-messages/voice_msg_2_638123456789.m4a`

#### **3. Database Storage**
```csharp
var message = new AnalysisMessage
{
    Message = "[Voice Message]",
    MessageType = "VoiceMessage",

    // Voice-specific fields
    VoiceMessageUrl = voiceUrl,              // Full URL
    VoiceMessageDuration = request.Duration,  // Seconds
    VoiceMessageWaveform = request.Waveform   // JSON array (optional)
};
```

---

## ⚙️ Environment Configuration

### **Development** (`appsettings.Development.json`)

```json
{
  "FileStorage": {
    "Provider": "FreeImageHost",  // Images/docs use FreeImageHost
    "FreeImageHost": {
      "ApiKey": "6d207e02198a847aa98d0a2a901485a5"
    },
    "Local": {                    // Voice messages use Local
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "https://localhost:5001"
    }
  }
}
```

**Behavior**:
- **Avatars**: FreeImageHost → `https://freeimage.host/i/abc123`
- **Image Attachments**: FreeImageHost → `https://freeimage.host/i/def456`
- **Voice Messages**: Local → `https://localhost:5001/voice-messages/voice_msg_2_638123456789.m4a`

---

### **Staging** (`appsettings.Staging.json`)

```json
{
  "FileStorage": {
    "Provider": "FreeImageHost",
    "FreeImageHost": {
      "ApiKey": "6d207e02198a847aa98d0a2a901485a5"
    },
    "Local": {
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "https://ziraai-api-sit.up.railway.app"
    }
  }
}
```

**Behavior**: Same as development (FreeImageHost for images, Local for voice)

---

### **Production** (`appsettings.Production.json`)

```json
{
  "FileStorage": {
    "Provider": "Local",  // Currently using Local for everything
    "Local": {
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "${RAILWAY_PUBLIC_DOMAIN}"
    },
    "FreeImageHost": {
      "ApiKey": "${FREEIMAGEHOST_API_KEY}"  // Env variable
    }
  }
}
```

**⚠️ Production Risk**:
- Railway uses **ephemeral disk**
- Every deploy → `wwwroot/uploads/` gets **DELETED**
- Voice messages **LOST** on deploy
- Avatar/attachment URLs become **404**

**Recommended Fix**:
```bash
# Railway environment variable
FileStorage__Provider=FreeImageHost
```

This will:
- Use FreeImageHost for avatars/images (persistent)
- Still use Local for voice messages (best available option)

**Long-term Solution**:
- Implement S3 storage for voice messages
- Use CloudFront CDN for delivery
- Production-grade persistence

---

## 📂 Local Storage Structure

```
wwwroot/
└── uploads/
    ├── voice-messages/               ← Voice messages subfolder
    │   ├── voice_msg_2_638123456789.m4a
    │   ├── voice_msg_3_638123456790.aac
    │   └── voice_msg_2_638123456791.mp3
    │
    ├── avatar_123_638123456789.jpg   ← Avatars (if Local provider)
    ├── avatar_thumb_123_638123456789.jpg
    │
    └── msg_attachment_2_638123456789_photo.jpg  ← Attachments (if Local)
```

**Automatic Directory Creation**:
- `LocalFileStorageService` auto-creates directories
- Subfolder parameter: `UploadFileAsync(..., folder: "voice-messages")`

---

## 🧪 Testing Voice Messages

### **Test 1: Local Upload (Development)**

**Request**:
```http
POST /api/sponsorship/messages/voice
Content-Type: multipart/form-data
Authorization: Bearer {token}

toUserId: 2
plantAnalysisId: 1
voiceFile: voice_note.m4a (4.2MB, 45 seconds)
duration: 45
waveform: [0.2, 0.5, 0.8, 0.6, ...]
```

**Expected Response**:
```json
{
  "success": true,
  "data": {
    "messageId": 123,
    "voiceMessageUrl": "https://localhost:5001/voice-messages/voice_msg_2_638123456789.m4a",
    "duration": 45
  },
  "message": "Voice message sent (45s)"
}
```

**Verification**:
1. Check file exists: `wwwroot/uploads/voice-messages/voice_msg_2_638123456789.m4a`
2. Open URL in browser → Audio file downloads/plays
3. Database query:
```sql
SELECT "VoiceMessageUrl", "VoiceMessageDuration", "VoiceMessageWaveform"
FROM "AnalysisMessages"
WHERE "MessageId" = 123;
-- Expected: URL = "https://localhost:5001/...", Duration = 45
```

---

### **Test 2: Tier Validation (XL Tier Required)**

**Request** (User with L tier):
```http
POST /api/sponsorship/messages/voice
voiceFile: voice.m4a
```

**Expected Response**:
```json
{
  "success": false,
  "message": "XL tier veya üzeri abonelik gereklidir.",
  "errors": ["VoiceMessage özelliği için XL tier gereklidir."]
}
```

---

### **Test 3: Duration Limit (60 seconds max)**

**Request**:
```http
POST /api/sponsorship/messages/voice
voiceFile: long_voice.m4a
duration: 75
```

**Expected Response**:
```json
{
  "success": false,
  "message": "Sesli mesaj en fazla 60 saniye olabilir.",
  "errors": ["VoiceMessageDuration must be ≤ 60 seconds"]
}
```

---

## 🔍 Troubleshooting

### **Issue 1: Voice Message Upload Fails (404 Not Found)**

**Symptom**:
```
Failed to upload voice message
```

**Cause**: `wwwroot/uploads/voice-messages/` directory doesn't exist

**Solution**:
```bash
# Directory is auto-created by LocalFileStorageService
# Check logs for errors
tail -f logs/application.log | grep "voice_msg"
```

**Verify**:
```csharp
// LocalFileStorageService.cs:87-90
var directory = Path.GetDirectoryName(fullPath);
if (!Directory.Exists(directory))
{
    Directory.CreateDirectory(directory);
}
```

---

### **Issue 2: Production Voice Messages Return 404 After Deploy**

**Symptom**: Voice message URL works before deploy, 404 after deploy

**Cause**: Railway ephemeral disk - files deleted on deploy

**Solution** (Short-term):
```
⚠️ Accept data loss on deploy (development/testing only)
✅ Document to users: "Voice messages will be unavailable after updates"
```

**Solution** (Long-term):
```
1. Implement S3FileStorageService
2. Update SendVoiceMessageCommand to use S3
3. Configure Railway environment variables:
   - AWS_ACCESS_KEY_ID
   - AWS_SECRET_ACCESS_KEY
   - AWS_S3_BUCKET_NAME
```

---

### **Issue 3: Large Voice File Upload Fails**

**Symptom**: 413 Payload Too Large or timeout

**Cause**:
- File size exceeds 5MB limit
- ASP.NET Core request size limit
- Nginx/proxy timeout

**Solution**:
```csharp
// Check AttachmentValidationService.cs
// Max voice file size: 5MB (5 * 1024 * 1024 bytes)

// Increase limit if needed:
// Program.cs
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});
```

---

## 📊 Storage Comparison

| Feature | FreeImageHost | LocalFileStorageService | S3 (Future) |
|---------|---------------|------------------------|-------------|
| **Avatars** | ✅ Supported | ✅ Supported | ✅ Supported |
| **Images** | ✅ Supported | ✅ Supported | ✅ Supported |
| **Documents** | ✅ Supported | ✅ Supported | ✅ Supported |
| **Audio/Voice** | ❌ Not Supported | ✅ Supported | ✅ Supported |
| **Persistence** | ✅ Permanent | ⚠️ Ephemeral (Railway) | ✅ Permanent |
| **CDN** | ✅ Public URL | ❌ No CDN | ✅ CloudFront |
| **Cost** | 🆓 Free | 🆓 Free (Railway disk) | 💰 Pay-per-use |
| **Upload Speed** | ~1-2s | ~100-500ms | ~500-1000ms |
| **Reliability** | ⚠️ Unknown | ✅ High | ✅ 99.99% SLA |

**Recommendation**:
- **Development/Staging**: Current setup (FreeImageHost + Local) is fine
- **Production**: Migrate to S3 for all file types

---

## 🚀 Next Steps

### **Immediate** (Test Ready):
- ✅ Voice messages work in development (local storage)
- ✅ Tier validation enforced (XL tier only)
- ✅ File size and duration limits validated
- ⚠️ Production will lose voice messages on deploy (acceptable for testing)

### **Before Production Launch**:
1. **Implement S3FileStorageService**:
   - Create `S3FileStorageService.cs`
   - Add AWS SDK NuGet package
   - Configure bucket and CloudFront

2. **Update Voice Message Handler**:
   ```csharp
   // Option 1: Use S3 via IFileStorageService (if S3 is global provider)
   private readonly IFileStorageService _fileStorage;

   // Option 2: Conditional provider selection
   private readonly IFileStorageService _fileStorage; // Images
   private readonly S3FileStorageService _s3Storage;  // Audio
   ```

3. **Migration Strategy**:
   - Existing voice messages in Railway local → One-time upload to S3
   - Update `VoiceMessageUrl` in database
   - Delete old local files

4. **Environment Variables** (Railway):
   ```bash
   AWS_ACCESS_KEY_ID=your_access_key
   AWS_SECRET_ACCESS_KEY=your_secret_key
   AWS_S3_BUCKET_NAME=ziraai-voice-messages
   AWS_REGION=us-east-1
   ```

---

## 📝 Summary

**Current Implementation**:
- ✅ Avatars → FreeImageHost (dev/staging) or Local (production)
- ✅ Image/Doc Attachments → FreeImageHost (dev/staging) or Local (production)
- ✅ **Voice Messages** → **LocalFileStorageService ALWAYS** (direct injection)

**Why Voice Uses Local**:
- FreeImageHost doesn't support audio files
- Local is simplest working solution for testing
- Production needs S3 for persistence

**Testing Status**:
- ✅ Ready to test in development
- ✅ All validations in place (tier, size, duration)
- ⚠️ Production will have data loss on deploy (known limitation)

**Documentation Updated**: 2025-10-19 🎉
