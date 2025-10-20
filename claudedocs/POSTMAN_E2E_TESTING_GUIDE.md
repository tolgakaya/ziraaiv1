# Postman End-to-End Testing Guide - Chat Enhancements

**Collection**: ZiraAI - Chat Enhancements & Auth  
**Last Updated**: 2025-10-19  
**Feature Branch**: `feature/sponsor-farmer-messaging`

---

## 📋 Pre-Testing Setup

### Step 1: Import Postman Collection

1. Open Postman Desktop or Web
2. Click **Import** button (top left)
3. Select `ZiraAI_Chat_Enhancements_Postman_Collection.json`
4. Collection will appear in left sidebar: **ZiraAI - Chat Enhancements & Auth**

### Step 2: Configure Collection Variables

Click on collection → **Variables** tab:

| Variable | Default Value | Description |
|----------|---------------|-------------|
| `baseUrl` | `https://localhost:5001/api` | Change for staging/production |
| `token` | *(auto-filled)* | JWT access token (auto-extracted) |
| `refreshToken` | *(auto-filled)* | Refresh token (auto-extracted) |
| `userId` | *(auto-filled)* | Logged-in user ID (auto-extracted) |
| `phoneNumber` | `+905551234567` | Your test phone number |
| `otpCode` | *(manual)* | OTP code from logs/SMS |
| `messageId` | *(auto-filled)* | Created message ID |
| `plantAnalysisId` | `1` | Valid plant analysis ID for testing |

**Environment-Specific Configuration**:
- **Development**: `https://localhost:5001/api`
- **Staging**: `https://ziraai-api-sit.up.railway.app/api`
- **Production**: `https://api.ziraai.com/api`

### Step 3: Prepare Test Data

**Required Database Records**:
- At least 2 user accounts (for messaging between users)
- At least 1 plant analysis record (for conversation context)
- Different subscription tiers for tier validation testing

**Test Files** (prepare in advance):
- **Avatar**: `avatar.jpg` (any JPEG/PNG image)
- **Image Attachment**: `plant.jpg` (JPEG, <10MB for L tier)
- **Document**: `report.pdf` (PDF, <5MB for L tier)
- **Voice Message**: `voice.m4a` (M4A/AAC/MP3, <5MB, <60s for XL tier)

---

## 🧪 Complete End-to-End Test Flow

### Phase 0: Environment Check ✅

**Before starting, verify**:
```bash
# API is running
curl https://localhost:5001/api/health

# Database migrations applied (7 scripts)
psql -U ziraai -d ziraai_dev -c "SELECT COUNT(*) FROM \"MessagingFeatures\";"
# Expected: 9 features

# Check AnalysisMessages table has new columns
psql -U ziraai -d ziraai_dev -c "\d \"AnalysisMessages\";"
# Should show: MessageStatus, DeliveredDate, AttachmentTypes, VoiceMessageUrl, IsEdited, etc.
```

---

### Phase 1: Authentication Flow 🔐

**Goal**: Authenticate with phone number and obtain JWT token

#### Test 1.1: Send OTP Code

**Endpoint**: `1.1 Send OTP Code`  
**Method**: `POST /api/auth/login-phone`

**Request Body**:
```json
{
  "mobilePhone": "+905551234567"
}
```

**Expected Response** (200 OK):
```json
{
  "success": true,
  "message": "OTP kodu gönderildi."
}
```

**Validation**:
- ✅ Response status: 200
- ✅ `success: true`
- ✅ Check server logs for OTP code (Development environment)
- ✅ SMS received (Production environment)

**Get OTP Code**:
```bash
# Development: Check application logs
grep "OTP Code" logs/application.log
# Or check console output

# Production: Check SMS on mobile device
```

**Action**: Copy OTP code → Paste into collection variable `otpCode`

---

#### Test 1.2: Verify OTP & Login

**Endpoint**: `1.2 Verify Phone OTP`  
**Method**: `POST /api/auth/verify-phone-otp`

**Request Body**:
```json
{
  "mobilePhone": "+905551234567",
  "code": "{{otpCode}}"
}
```

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "userId": 123,
    "email": "user@example.com",
    "fullName": "John Doe",
    "mobilePhone": "+905551234567"
  },
  "message": "Giriş başarılı."
}
```

**Validation**:
- ✅ Response status: 200
- ✅ `success: true`
- ✅ `token` is present and starts with "eyJ"
- ✅ `refreshToken` is present
- ✅ `userId` matches expected user
- ✅ **Auto-extraction script runs**: Check Postman console for "✅ Login successful! Token saved."

**Collection Variables Auto-Updated**:
- `token` → JWT access token
- `refreshToken` → Refresh token
- `userId` → User ID

**Action**: All subsequent requests will now use this token automatically via `{{token}}` variable.

---

#### Test 1.3: Verify Token Works

**Endpoint**: `3.1 Get Messaging Features`  
**Method**: `GET /api/sponsorship/messaging/features`

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "userTier": "L",
    "availableFeatures": [
      {
        "id": 1,
        "featureName": "VoiceMessage",
        "isEnabled": true,
        "requiredTier": "XL"
      },
      {
        "id": 2,
        "featureName": "ImageAttachment",
        "isEnabled": true,
        "requiredTier": "L"
      }
      // ... more features
    ]
  }
}
```

**Validation**:
- ✅ Response status: 200 (not 401 Unauthorized)
- ✅ Bearer token is working
- ✅ User tier is displayed correctly
- ✅ Feature list contains 9 features

---

### Phase 2: Avatar Management 🖼️

**Goal**: Upload, retrieve, and delete user avatars

#### Test 2.1: Upload Avatar

**Endpoint**: `2.1 Upload Avatar`  
**Method**: `POST /api/users/avatar`

**Request Body** (form-data):
- `file`: Select `avatar.jpg` from your computer

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "avatarUrl": "https://freeimage.host/i/abc123",
    "avatarThumbnailUrl": "https://freeimage.host/i/abc123_thumb"
  },
  "message": "Avatar başarıyla yüklendi."
}
```

**Validation**:
- ✅ Response status: 200
- ✅ `avatarUrl` is a valid URL
- ✅ `avatarThumbnailUrl` is a valid URL
- ✅ Open URLs in browser → images load correctly
- ✅ Avatar is 512px max dimension
- ✅ Thumbnail is 128px max dimension

**Database Verification**:
```sql
SELECT "AvatarUrl", "AvatarThumbnailUrl", "AvatarUpdatedDate"
FROM "Users"
WHERE "UserId" = {{userId}};
```

---

#### Test 2.2: Get Avatar URL

**Endpoint**: `2.2 Get Avatar URL`  
**Method**: `GET /api/users/avatar/{{userId}}`

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "avatarUrl": "https://freeimage.host/i/abc123",
    "avatarThumbnailUrl": "https://freeimage.host/i/abc123_thumb"
  }
}
```

**Validation**:
- ✅ Same URLs as upload response
- ✅ Works for both own user and other users

---

#### Test 2.3: Delete Avatar

**Endpoint**: `2.3 Delete Avatar`  
**Method**: `DELETE /api/users/avatar`

**Expected Response** (200 OK):
```json
{
  "success": true,
  "message": "Avatar başarıyla silindi."
}
```

**Validation**:
- ✅ Response status: 200
- ✅ Re-run Test 2.2 → Should return null or 404

**Database Verification**:
```sql
SELECT "AvatarUrl", "AvatarThumbnailUrl"
FROM "Users"
WHERE "UserId" = {{userId}};
-- Expected: NULL, NULL
```

---

### Phase 3: Message Status & Read Receipts ✉️

**Prerequisites**: 
- Have at least 1 existing message in database
- Update collection variable `messageId` with valid message ID

#### Test 3.1: Mark Single Message as Read

**Endpoint**: `4.1 Mark Message as Read`  
**Method**: `PATCH /api/sponsorship/messages/{{messageId}}/read`

**Expected Response** (200 OK):
```json
{
  "success": true,
  "message": "Mesaj okundu olarak işaretlendi."
}
```

**Validation**:
- ✅ Response status: 200
- ✅ Only recipient can mark as read (not sender)

**Database Verification**:
```sql
SELECT "MessageStatus", "IsRead", "ReadDate"
FROM "AnalysisMessages"
WHERE "MessageId" = {{messageId}};
-- Expected: MessageStatus = 'Read', IsRead = true, ReadDate = NOW()
```

---

#### Test 3.2: Bulk Mark Messages as Read

**Endpoint**: `4.2 Bulk Mark as Read`  
**Method**: `PATCH /api/sponsorship/messages/read`

**Request Body**:
```json
{
  "messageIds": [1, 2, 3, 4, 5]
}
```

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "updatedCount": 5
  },
  "message": "5 mesaj okundu olarak işaretlendi."
}
```

**Validation**:
- ✅ Response status: 200
- ✅ `updatedCount` matches array length
- ✅ Only unread messages are updated

**Database Verification**:
```sql
SELECT COUNT(*)
FROM "AnalysisMessages"
WHERE "MessageId" IN (1,2,3,4,5)
  AND "MessageStatus" = 'Read';
-- Expected: 5
```

---

### Phase 4: Image & File Attachments 📎

**Prerequisites**: Subscription tier L or XL

#### Test 4.1: Send Message with Image Attachment

**Endpoint**: `5.1 Send Message with Attachments`  
**Method**: `POST /api/sponsorship/messages/attachments`

**Request Body** (form-data):
- `toUserId`: `2` (recipient user ID)
- `plantAnalysisId`: `{{plantAnalysisId}}`
- `message`: `Check these plant photos`
- `attachments`: Select `plant1.jpg` (JPEG, <10MB)
- `attachments`: Select `plant2.jpg` (JPEG, <10MB)

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "messageId": 456,
    "attachmentUrls": [
      "https://storage.com/attachments/abc123.jpg",
      "https://storage.com/attachments/def456.jpg"
    ],
    "attachmentCount": 2
  },
  "message": "Mesaj eklerle gönderildi."
}
```

**Validation**:
- ✅ Response status: 200
- ✅ `attachmentUrls` array has 2 items
- ✅ `attachmentCount` = 2
- ✅ Open URLs in browser → images load

**Database Verification**:
```sql
SELECT "MessageContent", "AttachmentUrls", "AttachmentTypes", 
       "AttachmentSizes", "AttachmentNames", "AttachmentCount"
FROM "AnalysisMessages"
WHERE "MessageId" = 456;
```

**Expected**:
- `AttachmentUrls`: `["https://...", "https://..."]`
- `AttachmentTypes`: `["image/jpeg", "image/jpeg"]`
- `AttachmentSizes`: `[245000, 189000]` (example sizes in bytes)
- `AttachmentNames`: `["plant1.jpg", "plant2.jpg"]`
- `AttachmentCount`: `2`

---

#### Test 4.2: Send Message with PDF Document

**Endpoint**: `5.1 Send Message with Attachments`

**Request Body** (form-data):
- `toUserId`: `2`
- `plantAnalysisId`: `{{plantAnalysisId}}`
- `message`: `Plant care guide attached`
- `attachments`: Select `care_guide.pdf` (PDF, <5MB)

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "messageId": 457,
    "attachmentUrls": ["https://storage.com/docs/guide.pdf"],
    "attachmentCount": 1
  }
}
```

**Validation**:
- ✅ PDF type accepted for L tier
- ✅ `AttachmentTypes[0]` = `"application/pdf"`

---

#### Test 4.3: Tier Validation - Image Upload (Insufficient Tier)

**Prerequisites**: User with tier S or lower

**Endpoint**: `5.1 Send Message with Attachments`

**Request Body** (form-data):
- `toUserId`: `2`
- `plantAnalysisId`: `{{plantAnalysisId}}`
- `message`: `Test`
- `attachments`: Select `image.jpg`

**Expected Response** (403 Forbidden):
```json
{
  "success": false,
  "message": "L tier veya üzeri abonelik gereklidir.",
  "errors": ["ImageAttachment özelliği için L tier gereklidir."]
}
```

**Validation**:
- ✅ Response status: 403
- ✅ Error message mentions tier requirement
- ✅ No message created in database

---

### Phase 5: Voice Messages 🎤

**Prerequisites**: Subscription tier XL only

#### Test 5.1: Send Voice Message (XL Tier)

**Endpoint**: `6.1 Send Voice Message`  
**Method**: `POST /api/sponsorship/messages/voice`

**Request Body** (form-data):
- `toUserId`: `2`
- `plantAnalysisId`: `{{plantAnalysisId}}`
- `voiceFile`: Select `voice_note.m4a` (M4A, <5MB, <60 seconds)
- `duration`: `45` (seconds)
- `waveform`: `[0.2, 0.5, 0.8, 0.6, 0.3, ...]` (optional JSON array)

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "messageId": 458,
    "voiceMessageUrl": "https://storage.com/voice/abc123.m4a",
    "duration": 45
  },
  "message": "Sesli mesaj gönderildi."
}
```

**Validation**:
- ✅ Response status: 200
- ✅ `voiceMessageUrl` is valid and accessible
- ✅ Audio duration matches uploaded file

**Database Verification**:
```sql
SELECT "VoiceMessageUrl", "VoiceMessageDuration", "VoiceMessageWaveform"
FROM "AnalysisMessages"
WHERE "MessageId" = 458;
```

**Expected**:
- `VoiceMessageUrl`: `"https://storage.com/voice/abc123.m4a"`
- `VoiceMessageDuration`: `45`
- `VoiceMessageWaveform`: JSON array or NULL

---

#### Test 5.2: Voice Message - Tier Validation (L Tier User)

**Prerequisites**: User with tier L (not XL)

**Endpoint**: `6.1 Send Voice Message`

**Expected Response** (403 Forbidden):
```json
{
  "success": false,
  "message": "XL tier abonelik gereklidir.",
  "errors": ["VoiceMessage özelliği için XL tier gereklidir."]
}
```

**Validation**:
- ✅ Response status: 403
- ✅ Error explicitly mentions XL tier requirement

---

#### Test 5.3: Voice Message - Duration Validation (>60s)

**Endpoint**: `6.1 Send Voice Message`

**Request Body**:
- `voiceFile`: `long_voice.m4a` (>60 seconds)
- `duration`: `75`

**Expected Response** (400 Bad Request):
```json
{
  "success": false,
  "message": "Sesli mesaj en fazla 60 saniye olabilir.",
  "errors": ["VoiceMessageDuration must be ≤ 60 seconds"]
}
```

**Validation**:
- ✅ Response status: 400
- ✅ Duration limit enforced

---

### Phase 6: Edit Messages ✏️

**Prerequisites**: Tier M or higher

#### Test 6.1: Edit Recent Message (Within 1 Hour)

**Prerequisites**: 
- Create a message less than 1 hour ago
- Save its `messageId`

**Endpoint**: `7.1 Edit Message`  
**Method**: `PUT /api/sponsorship/messages/{{messageId}}`

**Request Body**:
```json
{
  "newContent": "Updated message content - typo fixed"
}
```

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "messageId": 459,
    "isEdited": true,
    "editedDate": "2025-10-19T14:30:00Z"
  },
  "message": "Mesaj güncellendi."
}
```

**Validation**:
- ✅ Response status: 200
- ✅ `isEdited: true`
- ✅ `editedDate` is current timestamp

**Database Verification**:
```sql
SELECT "MessageContent", "IsEdited", "EditedDate", "OriginalMessage"
FROM "AnalysisMessages"
WHERE "MessageId" = 459;
```

**Expected**:
- `MessageContent`: `"Updated message content - typo fixed"`
- `IsEdited`: `true`
- `EditedDate`: Recent timestamp
- `OriginalMessage`: `"Original message content before edit"`

---

#### Test 6.2: Edit Old Message (>1 Hour) - Should Fail

**Prerequisites**: Message created >1 hour ago

**Endpoint**: `7.1 Edit Message`

**Expected Response** (400 Bad Request):
```json
{
  "success": false,
  "message": "Mesajlar sadece 1 saat içinde düzenlenebilir.",
  "errors": ["Edit time limit (1 hour) exceeded"]
}
```

**Validation**:
- ✅ Response status: 400
- ✅ Time limit enforced correctly

---

#### Test 6.3: Edit Message - Tier Validation (S Tier User)

**Prerequisites**: User with tier S

**Endpoint**: `7.1 Edit Message`

**Expected Response** (403 Forbidden):
```json
{
  "success": false,
  "message": "M tier veya üzeri abonelik gereklidir.",
  "errors": ["EditMessage özelliği için M tier gereklidir."]
}
```

**Validation**:
- ✅ Response status: 403
- ✅ Tier requirement enforced

---

### Phase 7: Delete Messages 🗑️

**Prerequisites**: All tiers (free feature)

#### Test 7.1: Delete Recent Message (Within 24 Hours)

**Prerequisites**: Message created <24 hours ago

**Endpoint**: `7.2 Delete Message`  
**Method**: `DELETE /api/sponsorship/messages/{{messageId}}`

**Expected Response** (200 OK):
```json
{
  "success": true,
  "message": "Mesaj silindi."
}
```

**Validation**:
- ✅ Response status: 200

**Database Verification**:
```sql
SELECT "MessageContent", "IsActive"
FROM "AnalysisMessages"
WHERE "MessageId" = {{messageId}};
```

**Expected**:
- `MessageContent`: `"[Mesaj silindi]"` or similar
- `IsActive`: `false` (soft delete)

---

#### Test 7.2: Delete Old Message (>24 Hours) - Should Fail

**Prerequisites**: Message created >24 hours ago

**Endpoint**: `7.2 Delete Message`

**Expected Response** (400 Bad Request):
```json
{
  "success": false,
  "message": "Mesajlar sadece 24 saat içinde silinebilir.",
  "errors": ["Delete time limit (24 hours) exceeded"]
}
```

**Validation**:
- ✅ Response status: 400
- ✅ 24-hour limit enforced

---

### Phase 8: Forward Messages ⏩

**Prerequisites**: Tier M or higher

#### Test 8.1: Forward Text Message

**Prerequisites**: 
- Have a message to forward (messageId)
- Have a different conversation (different plantAnalysisId or toUserId)

**Endpoint**: `8.1 Forward Message`  
**Method**: `POST /api/sponsorship/messages/{{messageId}}/forward`

**Request Body**:
```json
{
  "toUserId": 3,
  "plantAnalysisId": 2
}
```

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "newMessageId": 460,
    "forwardedFromMessageId": 123,
    "isForwarded": true
  },
  "message": "Mesaj iletildi."
}
```

**Validation**:
- ✅ Response status: 200
- ✅ `newMessageId` created
- ✅ `forwardedFromMessageId` matches original message

**Database Verification**:
```sql
-- Original message
SELECT "MessageContent", "AttachmentUrls"
FROM "AnalysisMessages"
WHERE "MessageId" = 123;

-- Forwarded message
SELECT "MessageContent", "IsForwarded", "ForwardedFromMessageId", "AttachmentUrls"
FROM "AnalysisMessages"
WHERE "MessageId" = 460;
```

**Expected**:
- Forwarded message has same `MessageContent` and `AttachmentUrls`
- `IsForwarded`: `true`
- `ForwardedFromMessageId`: `123`

---

#### Test 8.2: Forward Message with Attachments

**Prerequisites**: Message with image/file attachments

**Endpoint**: `8.1 Forward Message`

**Request Body**:
```json
{
  "toUserId": 3,
  "plantAnalysisId": 2
}
```

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "newMessageId": 461,
    "attachmentsCopied": true
  }
}
```

**Database Verification**:
```sql
SELECT "AttachmentUrls", "AttachmentTypes", "AttachmentSizes", "AttachmentCount"
FROM "AnalysisMessages"
WHERE "MessageId" = 461;
```

**Expected**: All attachment metadata copied from original message

---

#### Test 8.3: Forward Voice Message

**Prerequisites**: Message with voice recording

**Endpoint**: `8.1 Forward Message`

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "newMessageId": 462,
    "voiceMessageCopied": true
  }
}
```

**Database Verification**:
```sql
SELECT "VoiceMessageUrl", "VoiceMessageDuration", "VoiceMessageWaveform"
FROM "AnalysisMessages"
WHERE "MessageId" = 462;
```

**Expected**: Voice message URL and metadata copied

---

#### Test 8.4: Forward - Tier Validation (S Tier)

**Prerequisites**: User with tier S

**Endpoint**: `8.1 Forward Message`

**Expected Response** (403 Forbidden):
```json
{
  "success": false,
  "message": "M tier veya üzeri abonelik gereklidir.",
  "errors": ["ForwardMessage özelliği için M tier gereklidir."]
}
```

**Validation**:
- ✅ Response status: 403
- ✅ Tier requirement enforced

---

## 🎯 Complete Test Sequence (Happy Path)

**Run endpoints in this order for full end-to-end validation**:

```
✅ Phase 1: Authentication
   → 1.1 Send OTP Code
   → 1.2 Verify Phone OTP (get token)
   → 3.1 Get Messaging Features (verify token)

✅ Phase 2: Avatar
   → 2.1 Upload Avatar
   → 2.2 Get Avatar URL
   → 2.3 Delete Avatar

✅ Phase 3: Message Status
   → 4.1 Mark Message as Read
   → 4.2 Bulk Mark as Read

✅ Phase 4: Attachments
   → 5.1 Send with Image
   → 5.1 Send with PDF

✅ Phase 5: Voice (XL tier only)
   → 6.1 Send Voice Message

✅ Phase 6: Edit
   → 7.1 Edit Recent Message

✅ Phase 7: Delete
   → 7.2 Delete Message

✅ Phase 8: Forward
   → 8.1 Forward Text Message
   → 8.1 Forward with Attachments
   → 8.1 Forward Voice Message
```

**Total Time**: ~15-20 minutes for complete flow

---

## 🚨 Error Scenarios & Troubleshooting

### Common Errors

#### 1. `401 Unauthorized`

**Error Response**:
```json
{
  "success": false,
  "message": "Unauthorized access"
}
```

**Causes**:
- Token expired (60 minute lifetime)
- Token not set in collection variables
- Invalid token format

**Solution**:
```
1. Re-run Phase 1 authentication (1.1 + 1.2)
2. Verify collection variable "token" is populated
3. Check Authorization header: "Bearer {{token}}"
4. Use refresh token endpoint (1.5) if available
```

---

#### 2. `403 Forbidden - Tier Requirement`

**Error Response**:
```json
{
  "success": false,
  "message": "L tier veya üzeri abonelik gereklidir.",
  "errors": ["ImageAttachment özelliği için L tier gereklidir."]
}
```

**Causes**:
- User subscription tier insufficient for feature
- Feature disabled by admin

**Solution**:
```sql
-- Check user's current tier
SELECT u."UserId", u."FullName", s."Tier"
FROM "Users" u
LEFT JOIN "Sponsorships" s ON u."UserId" = s."FarmerId"
WHERE u."UserId" = {{userId}};

-- Upgrade tier for testing (manual DB update)
UPDATE "Sponsorships"
SET "Tier" = 'XL'
WHERE "FarmerId" = {{userId}};
```

**Or**: Use admin endpoint to toggle feature:
```
PATCH /api/sponsorship/admin/messaging/features/{featureId}
Body: { "isEnabled": true }
```

---

#### 3. `400 Bad Request - Time Limit Exceeded`

**Error Response**:
```json
{
  "success": false,
  "message": "Mesajlar sadece 1 saat içinde düzenlenebilir.",
  "errors": ["Edit time limit (1 hour) exceeded"]
}
```

**Causes**:
- Trying to edit message older than 1 hour
- Trying to delete message older than 24 hours

**Solution**:
- Create a new message for testing
- Update database timestamp for testing:
```sql
UPDATE "AnalysisMessages"
SET "CreatedDate" = NOW() - INTERVAL '30 minutes'
WHERE "MessageId" = {{messageId}};
```

---

#### 4. `400 Bad Request - File Size/Duration Limit`

**Error Response**:
```json
{
  "success": false,
  "message": "Dosya boyutu çok büyük.",
  "errors": ["Maximum file size is 10MB for images"]
}
```

**Causes**:
- Image >10MB (L tier)
- Video >50MB (XL tier)
- Document >5MB (L tier)
- Voice message >60 seconds (XL tier)
- Voice file >5MB (XL tier)

**Solution**:
- Compress files before upload
- Use smaller test files
- Check file size limits per tier in documentation

**File Size Limits by Tier**:
| Feature | Tier | Max Size | Max Duration |
|---------|------|----------|--------------|
| Image | L | 10MB | - |
| Video | XL | 50MB | 60s |
| Document | L | 5MB | - |
| Voice | XL | 5MB | 60s |

---

#### 5. `404 Not Found - Invalid ID`

**Error Response**:
```json
{
  "success": false,
  "message": "Mesaj bulunamadı."
}
```

**Causes**:
- Invalid `messageId`
- Invalid `plantAnalysisId`
- Invalid `userId`

**Solution**:
```sql
-- Find valid message IDs
SELECT "MessageId", "MessageContent", "CreatedDate"
FROM "AnalysisMessages"
WHERE "ToUserId" = {{userId}} OR "FromUserId" = {{userId}}
ORDER BY "CreatedDate" DESC
LIMIT 10;

-- Find valid plant analysis IDs
SELECT "PlantAnalysisId", "Status"
FROM "PlantAnalyses"
WHERE "UserId" = {{userId}}
ORDER BY "CreatedDate" DESC
LIMIT 10;
```

---

#### 6. `500 Internal Server Error`

**Error Response**:
```json
{
  "success": false,
  "message": "Bir hata oluştu."
}
```

**Causes**:
- Database connection failure
- Missing database columns (migrations not applied)
- File storage service unavailable
- Null reference exception

**Solution**:
```bash
# Check API logs
tail -f logs/application.log

# Verify database migrations
psql -U ziraai -d ziraai_dev -f claudedocs/migrations/MessagingFeatures_Verification.sql

# Check database connection
psql -U ziraai -d ziraai_dev -c "SELECT NOW();"

# Restart API
dotnet run --project WebAPI/WebAPI.csproj
```

---

## ✅ Validation Checklist

### Before Starting Tests
- [ ] API is running and accessible
- [ ] Database migrations applied (7 scripts)
- [ ] Postman collection imported
- [ ] Collection variables configured (baseUrl)
- [ ] Test files prepared (images, PDFs, voice recordings)
- [ ] Test users exist with different tiers
- [ ] Valid `plantAnalysisId` exists

### Authentication
- [ ] OTP code received and verified
- [ ] Token auto-extracted to collection variable
- [ ] Token works for authenticated endpoints
- [ ] Refresh token mechanism works

### Avatar Management
- [ ] Avatar upload successful (512px)
- [ ] Thumbnail generated (128px)
- [ ] Avatar retrieval works
- [ ] Avatar deletion works
- [ ] Database fields updated correctly

### Message Status
- [ ] Single message marked as read
- [ ] Bulk mark as read works
- [ ] Only recipient can mark as read
- [ ] Database `MessageStatus` and `ReadDate` updated

### Attachments
- [ ] Image upload works (L tier)
- [ ] PDF upload works (L tier)
- [ ] Multiple attachments in one message
- [ ] Attachment metadata saved correctly
- [ ] Tier validation works (S tier blocked)
- [ ] File size limits enforced

### Voice Messages
- [ ] Voice upload works (XL tier only)
- [ ] Duration limit enforced (60s)
- [ ] File size limit enforced (5MB)
- [ ] Tier validation works (L tier blocked)
- [ ] Waveform data optional

### Edit Messages
- [ ] Recent message edit works (M tier)
- [ ] Original message preserved
- [ ] `IsEdited` flag set correctly
- [ ] Time limit enforced (1 hour)
- [ ] Tier validation works (S tier blocked)

### Delete Messages
- [ ] Recent message delete works (all tiers)
- [ ] Soft delete implemented (IsActive = false)
- [ ] Time limit enforced (24 hours)
- [ ] Message content replaced with "[Mesaj silindi]"

### Forward Messages
- [ ] Text message forward works (M tier)
- [ ] Attachments copied correctly
- [ ] Voice message copied correctly
- [ ] `IsForwarded` flag set
- [ ] `ForwardedFromMessageId` references original
- [ ] Tier validation works (S tier blocked)

---

## 📊 Test Results Template

Copy this template to track your test execution:

```markdown
## Test Execution Report - [Date]

**Tester**: [Your Name]
**Environment**: Development / Staging / Production
**API Base URL**: [URL]

### Summary
- Total Tests: 30
- Passed: __
- Failed: __
- Skipped: __

### Test Results

#### Phase 1: Authentication
- [ ] ✅ 1.1 Send OTP Code
- [ ] ✅ 1.2 Verify Phone OTP
- [ ] ✅ 1.3 Verify Token Works

#### Phase 2: Avatar Management
- [ ] ✅ 2.1 Upload Avatar
- [ ] ✅ 2.2 Get Avatar URL
- [ ] ✅ 2.3 Delete Avatar

#### Phase 3: Message Status
- [ ] ✅ 3.1 Mark Single as Read
- [ ] ✅ 3.2 Bulk Mark as Read

#### Phase 4: Attachments
- [ ] ✅ 4.1 Send with Image
- [ ] ✅ 4.2 Send with PDF
- [ ] ✅ 4.3 Tier Validation (S tier blocked)

#### Phase 5: Voice Messages
- [ ] ✅ 5.1 Send Voice (XL tier)
- [ ] ✅ 5.2 Tier Validation (L tier blocked)
- [ ] ✅ 5.3 Duration Validation (>60s blocked)

#### Phase 6: Edit Messages
- [ ] ✅ 6.1 Edit Recent Message
- [ ] ✅ 6.2 Time Limit (>1 hour blocked)
- [ ] ✅ 6.3 Tier Validation (S tier blocked)

#### Phase 7: Delete Messages
- [ ] ✅ 7.1 Delete Recent Message
- [ ] ✅ 7.2 Time Limit (>24 hours blocked)

#### Phase 8: Forward Messages
- [ ] ✅ 8.1 Forward Text Message
- [ ] ✅ 8.2 Forward with Attachments
- [ ] ✅ 8.3 Forward Voice Message
- [ ] ✅ 8.4 Tier Validation (S tier blocked)

### Issues Found
| Test Case | Issue Description | Severity | Status |
|-----------|-------------------|----------|--------|
| 4.1 | Image upload timeout | Medium | Open |
| - | - | - | - |

### Notes
- [Any observations or comments]

**Test Duration**: [Total time]
**Completed By**: [Name] on [Date]
```

---

## 🔄 SignalR Testing (Optional)

**Note**: SignalR endpoints require WebSocket testing, not included in standard Postman collection.

### SignalR Hub Connection

**Hub URL**: `wss://localhost:5001/hubs/plantanalysis`

**Connection Headers**:
```
Authorization: Bearer {{token}}
```

**Client Events to Listen**:
- `UserTyping` - When someone starts/stops typing
- `NewMessage` - Real-time message delivery
- `MessageRead` - Read receipt notification

**Client Methods to Call**:
- `StartTyping(conversationUserId, plantAnalysisId)` - Notify typing started
- `StopTyping(conversationUserId, plantAnalysisId)` - Notify typing stopped
- `NotifyNewMessage(recipientUserId, messageId, plantAnalysisId)` - Send message notification
- `NotifyMessageRead(senderUserId, messageId)` - Send read receipt

**Testing Tools**:
- SignalR Client (JavaScript)
- Postman WebSocket (limited support)
- Browser DevTools Console

**Example JavaScript Client**:
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:5001/hubs/plantanalysis", {
        accessTokenFactory: () => "{{token}}"
    })
    .build();

// Listen for typing indicator
connection.on("UserTyping", (userId, isTyping) => {
    console.log(`User ${userId} is ${isTyping ? 'typing' : 'stopped typing'}`);
});

// Listen for new messages
connection.on("NewMessage", (messageId, senderId) => {
    console.log(`New message ${messageId} from user ${senderId}`);
});

// Start connection
await connection.start();

// Send typing notification
await connection.invoke("StartTyping", 2, 1); // conversationUserId, plantAnalysisId
```

---

## 📝 Final Notes

### Database Verification Queries

**Check all new columns exist**:
```sql
-- Users table
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'Users'
  AND column_name IN ('AvatarUrl', 'AvatarThumbnailUrl', 'AvatarUpdatedDate');

-- AnalysisMessages table
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'AnalysisMessages'
  AND column_name IN (
    'MessageStatus', 'DeliveredDate',
    'AttachmentTypes', 'AttachmentSizes', 'AttachmentNames', 'AttachmentCount',
    'VoiceMessageUrl', 'VoiceMessageDuration', 'VoiceMessageWaveform',
    'IsEdited', 'EditedDate', 'OriginalMessage',
    'ForwardedFromMessageId', 'IsForwarded'
  );
-- Expected: 14 rows

-- MessagingFeatures table
SELECT COUNT(*) FROM "MessagingFeatures";
-- Expected: 9
```

### Performance Benchmarks

**Expected Response Times** (Development environment):

| Endpoint | Avg Time | Max Acceptable |
|----------|----------|----------------|
| Login/OTP | 50-200ms | 500ms |
| Avatar Upload | 500-2000ms | 5000ms |
| Get Features | 10-50ms | 200ms (cached) |
| Mark as Read | 20-100ms | 300ms |
| Send Attachment | 1000-5000ms | 10000ms |
| Send Voice | 500-3000ms | 8000ms |
| Edit Message | 50-200ms | 500ms |
| Delete Message | 30-150ms | 400ms |
| Forward Message | 100-500ms | 1000ms |

**If response times exceed max**: Check database indexes, file storage service, network latency.

---

## 🎓 Testing Best Practices

1. **Sequential Testing**: Always run authentication first, then dependent endpoints
2. **Variable Reuse**: Leverage auto-extraction scripts for messageId, userId
3. **Database Verification**: After each test, verify database state matches expectations
4. **Error Scenario Coverage**: Test both success and failure paths
5. **Tier Validation**: Test with users of different subscription tiers
6. **Time Limit Testing**: Create messages at different times to test edit/delete limits
7. **File Preparation**: Have test files ready before starting (images, PDFs, audio)
8. **Log Monitoring**: Keep an eye on API logs during testing for errors
9. **Cleanup**: Delete test data between test runs to avoid pollution
10. **Documentation**: Record any issues or unexpected behavior

---

## 📞 Support & Debugging

**If tests fail consistently**:

1. **Check API Logs**:
```bash
tail -f logs/application.log
# Or
dotnet run --project WebAPI/WebAPI.csproj
```

2. **Verify Database State**:
```bash
psql -U ziraai -d ziraai_dev
\dt "MessagingFeatures"
\d "AnalysisMessages"
```

3. **Check Migration Status**:
```bash
dotnet ef migrations list --project DataAccess --startup-project WebAPI --context ProjectDbContext
```

4. **Rebuild Project**:
```bash
dotnet clean
dotnet build
```

5. **Reset Test Data**:
```sql
-- Clear test messages
DELETE FROM "AnalysisMessages" WHERE "FromUserId" IN (test_user_ids);

-- Reset user tiers
UPDATE "Sponsorships" SET "Tier" = 'Trial' WHERE "FarmerId" IN (test_user_ids);
```

---

**Testing Complete!** 🎉

You've now tested all chat enhancement features end-to-end. Report any issues to the backend team with:
- Test case name
- Request/response payloads
- API logs
- Database state

**Last Updated**: 2025-10-19  
**Document Version**: 1.0
