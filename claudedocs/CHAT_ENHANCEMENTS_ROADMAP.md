# Sponsor-Farmer Chat Enhancements - Complete Roadmap

**Project:** ZiraAI Messaging System Advanced Features
**Branch:** `feature/sponsor-farmer-chat-enhancements`
**Started:** 2025-10-19
**Mobile Team Doc:** `claudedocs/BACKEND_REQUIREMENTS_FLUTTER_CHAT_UI_FEATURES.md`

---

## 📊 PROJECT STATUS OVERVIEW

### Overall Progress: 15% Complete

| Phase | Status | Progress | Est. Time | Actual Time |
|-------|--------|----------|-----------|-------------|
| **Foundation** | 🟡 In Progress | 60% | 1 day | - |
| **Phase 1** | ⚪ Not Started | 0% | 2 weeks | - |
| **Phase 2** | ⚪ Not Started | 0% | 2 weeks | - |
| **Phase 3** | ⚪ Not Started | 0% | 1 week | - |
| **Phase 4** | ⚪ Not Started | 0% | 1 week | - |
| **Deployment** | ⚪ Not Started | 0% | 2 days | - |

**Legend:** ✅ Complete | 🟡 In Progress | ⚪ Not Started | ❌ Blocked

---

## 🎯 FOUNDATION: FEATURE FLAG SYSTEM

**Goal:** Implement admin-controlled on/off switches for messaging features
**Priority:** ⭐⭐⭐ CRITICAL (Required for all phases)

### Progress: 60% Complete

#### ✅ Completed Tasks

1. **Entity Layer** (100%)
   - ✅ `Entities/Concrete/MessagingFeature.cs` - Feature flag entity
   - ✅ `Entities/Dtos/MessagingFeaturesDto.cs` - Response DTOs
   - ✅ `Entities/Dtos/MessagingFeatureDto.cs` - Individual feature DTO

2. **Data Access Layer** (100%)
   - ✅ `DataAccess/Abstract/IMessagingFeatureRepository.cs` - Repository interface
   - ✅ `DataAccess/Concrete/EntityFramework/MessagingFeatureRepository.cs` - Implementation
   - ✅ `DataAccess/Concrete/Configurations/MessagingFeatureEntityConfiguration.cs` - EF Config
   - ✅ `DataAccess/Concrete/EntityFramework/Contexts/ProjectDbContext.cs` - DbSet added

3. **Business Layer** (100%)
   - ✅ `Business/Services/Messaging/IMessagingFeatureService.cs` - Service interface
   - ✅ `Business/Services/Messaging/MessagingFeatureService.cs` - Service implementation

#### 🟡 In Progress Tasks

4. **Database Migration** (0%)
   - ⚪ Create migration script for MessagingFeatures table
   - ⚪ Create seed data SQL script (9 features)
   - ⚪ Create verification SQL script
   - ⚪ Apply to staging database

5. **Dependency Injection** (0%)
   - ⚪ Register `IMessagingFeatureRepository` in AutofacBusinessModule
   - ⚪ Register `IMessagingFeatureService` in AutofacBusinessModule

6. **API Endpoint** (0%)
   - ⚪ Create `GetMessagingFeaturesQuery` handler
   - ⚪ Add endpoint to `SponsorshipController` or create new `MessagingController`
   - ⚪ Test endpoint with Postman

7. **Admin Endpoints** (0%)
   - ⚪ Create `UpdateMessagingFeatureCommand` handler
   - ⚪ Add admin endpoint for feature toggle
   - ⚪ Add authorization check (Admin role only)

#### Next Steps
1. Create database migration (MessagingFeatures table)
2. Register services in DI container
3. Create API endpoint
4. Test with Postman
5. Deploy to staging

---

## 🎯 PHASE 1: AVATAR & MESSAGE STATUS

**Goal:** Basic UI/UX improvements (critical for mobile app)
**Priority:** ⭐⭐⭐ HIGH
**Estimated Time:** 2 weeks
**Status:** ⚪ Not Started (0%)

### Part A: Avatar Support (0%)

#### Database Changes
- ⚪ Add `AvatarUrl` to Users table
- ⚪ Add `AvatarThumbnailUrl` to Users table
- ⚪ Add `AvatarUploadedDate` to Users table
- ⚪ Create migration script
- ⚪ Apply migration to staging

#### Backend Implementation
- ⚪ Create `UserAvatarDto.cs`
- ⚪ Create `UploadAvatarCommand.cs` handler
- ⚪ Create `GetUserAvatarQuery.cs` handler
- ⚪ Add avatar fields to AnalysisMessage response DTOs
- ⚪ Integrate with image processing service (thumbnail generation)
- ⚪ Add CDN upload logic (existing FileStorage service)

#### API Endpoints
- ⚪ `POST /api/v1/users/avatar` - Upload avatar
- ⚪ `GET /api/v1/users/{userId}/avatar` - Get avatar URL
- ⚪ `DELETE /api/v1/users/avatar` - Remove avatar

#### Validation
- ⚪ File size: Max 5MB
- ⚪ File types: JPG, PNG, WebP only
- ⚪ Auto-generate 150x150px thumbnail
- ⚪ Feature flag: Check `IsEnabled` for avatar uploads

#### Testing Checklist
- ⚪ Upload valid image → Success + thumbnail created
- ⚪ Upload >5MB → Error
- ⚪ Upload .txt file → Error
- ⚪ Message response includes sender avatar URLs
- ⚪ Feature disabled → Error message

---

### Part B: Message Status & Read Receipts (0%)

#### Database Changes
- ⚪ Add `Status` VARCHAR(20) to AnalysisMessages (sent/delivered/seen)
- ⚪ Add `DeliveredDate` DATETIME to AnalysisMessages
- ⚪ Add `IsEdited` BOOLEAN to AnalysisMessages
- ⚪ Add `EditedDate` DATETIME to AnalysisMessages
- ⚪ Update existing records: SET Status='sent'
- ⚪ Create migration script
- ⚪ Apply migration to staging

#### Backend Implementation
- ⚪ Create `MarkMessageAsReadCommand.cs` handler
- ⚪ Create `MarkMessagesAsReadBulkCommand.cs` handler
- ⚪ Update message DTOs to include status fields
- ⚪ Add SignalR event for message status updates

#### API Endpoints
- ⚪ `PATCH /api/v1/sponsorship/messages/{id}/read` - Mark single message as read
- ⚪ `POST /api/v1/sponsorship/messages/mark-read` - Bulk mark as read
- ⚪ Update conversation endpoint to return status fields

#### SignalR Integration
- ⚪ Add `MessageStatusUpdated` event to PlantAnalysisHub
- ⚪ Broadcast when message marked as read
- ⚪ Include messageId, status, timestamp in payload

#### Testing Checklist
- ⚪ Send message → Status = 'sent'
- ⚪ Mark as read → Status = 'seen', ReadDate set
- ⚪ Bulk mark → All messages updated
- ⚪ SignalR event fired → Sender receives notification
- ⚪ Conversation endpoint returns all status fields

---

## 🎯 PHASE 2: ATTACHMENTS & VOICE MESSAGES

**Goal:** Multimedia support (images, files, voice)
**Priority:** ⭐⭐⭐ HIGH
**Estimated Time:** 2 weeks
**Status:** ⚪ Not Started (0%)

### Part A: Image & File Attachments (0%)

#### Database Changes
- ⚪ Add `AttachmentType` VARCHAR(50) to AnalysisMessages (image/video/file/voice)
- ⚪ Add `AttachmentSize` BIGINT to AnalysisMessages
- ⚪ Add `AttachmentFilename` VARCHAR(255) to AnalysisMessages
- ⚪ Add `AttachmentMimeType` VARCHAR(100) to AnalysisMessages
- ⚪ Add `AttachmentThumbnailUrl` VARCHAR(500) to AnalysisMessages
- ⚪ Add `AttachmentDuration` INT to AnalysisMessages (for video/voice)
- ⚪ Update `AttachmentUrls` → migrate to new structure or deprecate
- ⚪ Create migration script
- ⚪ Apply migration to staging

#### Backend Implementation
- ⚪ Create `SendMessageWithAttachmentCommand.cs` handler
- ⚪ Create `AttachmentDto.cs` for response
- ⚪ Integrate with FileStorage service (S3/ImgBB/FreeImageHost)
- ⚪ Add thumbnail generation for images
- ⚪ Add virus scanning (optional but recommended)
- ⚪ Validate file size using MessagingFeatureService
- ⚪ Validate MIME type using MessagingFeatureService

#### API Endpoints
- ⚪ `POST /api/v1/sponsorship/messages/with-attachment` - Send with attachment
- ⚪ Support multipart/form-data
- ⚪ Return attachment metadata in response

#### File Upload Flow
1. ⚪ Validate feature access (tier + enabled)
2. ⚪ Validate file size (feature.MaxFileSize)
3. ⚪ Validate MIME type (feature.AllowedMimeTypes)
4. ⚪ Generate thumbnail (if image)
5. ⚪ Upload to CDN
6. ⚪ Save message with attachment metadata
7. ⚪ Send SignalR NewMessage event

#### Testing Checklist
- ⚪ Upload image (2MB) → Success + thumbnail
- ⚪ Upload PDF (3MB) → Success
- ⚪ Upload >10MB → Error (feature limit)
- ⚪ Upload .exe → Error (MIME type blocked)
- ⚪ Feature disabled → Error
- ⚪ Insufficient tier → Error (requires L tier)

---

### Part B: Voice Messages (0%)

#### Database Changes
- ⚪ Already covered in Part A (AttachmentType, AttachmentDuration)
- ⚪ Add `WaveformData` TEXT to AnalysisMessages (JSON array of amplitudes)

#### Backend Implementation
- ⚪ Create `SendVoiceMessageCommand.cs` handler
- ⚪ Add waveform generation logic (analyze audio file)
- ⚪ Integrate with FileStorage service
- ⚪ Validate duration using MessagingFeatureService
- ⚪ Validate file size (max 5MB for voice)

#### API Endpoints
- ⚪ `POST /api/v1/sponsorship/messages/voice` - Send voice message
- ⚪ Support M4A, AAC, MP3 formats
- ⚪ Return waveform data in response

#### Voice Upload Flow
1. ⚪ Validate feature access (tier + enabled)
2. ⚪ Validate file size (max 5MB)
3. ⚪ Validate duration (max 60 seconds)
4. ⚪ Extract audio duration
5. ⚪ Generate waveform data
6. ⚪ Upload to CDN
7. ⚪ Save message with voice metadata
8. ⚪ Send SignalR NewMessage event

#### Testing Checklist
- ⚪ Upload 30sec M4A → Success + waveform
- ⚪ Upload 61sec audio → Error (exceeds limit)
- ⚪ Upload >5MB → Error
- ⚪ Feature disabled → Error
- ⚪ Insufficient tier (XL required) → Error

---

## 🎯 PHASE 3: REAL-TIME ENHANCEMENTS

**Goal:** Typing indicator and live message status updates
**Priority:** ⭐⭐ MEDIUM
**Estimated Time:** 1 week
**Status:** ⚪ Not Started (0%)

### Typing Indicator (0%)

#### SignalR Hub Enhancements
- ⚪ Add `UserTyping` event to PlantAnalysisHub
- ⚪ Add `UserStoppedTyping` event to PlantAnalysisHub
- ⚪ Add `JoinConversation(plantAnalysisId)` method
- ⚪ Add `LeaveConversation(plantAnalysisId)` method
- ⚪ Group-based broadcasting (analysis_{plantAnalysisId})

#### Event Payloads
```json
// UserTyping
{
  "plantAnalysisId": 123,
  "userId": 456,
  "userName": "Ahmet Yılmaz",
  "timestamp": "2025-10-19T10:30:00Z"
}

// UserStoppedTyping
{
  "plantAnalysisId": 123,
  "userId": 456,
  "timestamp": "2025-10-19T10:30:05Z"
}
```

#### Feature Flag Validation
- ⚪ Check `TypingIndicator` feature before broadcasting
- ⚪ If disabled, silently ignore (don't broadcast)

#### Testing Checklist
- ⚪ User starts typing → Event broadcasted to other user
- ⚪ User stops typing → Event broadcasted
- ⚪ Feature disabled → No events broadcasted
- ⚪ Multiple users in same conversation → All receive events

---

### Real-time Message Status Updates (0%)

#### SignalR Integration
- ⚪ Enhance existing `NewMessage` event with status field
- ⚪ Add `MessageDelivered` event (when recipient opens chat)
- ⚪ Add `MessageSeen` event (when recipient views message)

#### Backend Logic
- ⚪ Auto-mark as 'delivered' when conversation loaded
- ⚪ Auto-mark as 'seen' when message appears in viewport (client triggers)
- ⚪ Broadcast status changes to sender

#### Testing Checklist
- ⚪ Send message → Sender sees 'sent'
- ⚪ Recipient opens chat → Sender sees 'delivered'
- ⚪ Recipient views message → Sender sees 'seen'
- ⚪ Status persists after app restart

---

## 🎯 PHASE 4: ADVANCED FEATURES

**Goal:** Edit, delete, forward, link preview
**Priority:** ⭐⭐ MEDIUM
**Estimated Time:** 1 week
**Status:** ⚪ Not Started (0%)

### Part A: Message Edit & Delete (0%)

#### Backend Implementation
- ⚪ Create `EditMessageCommand.cs` handler
- ⚪ Create `DeleteMessageCommand.cs` handler
- ⚪ Validate time limits using MessagingFeatureService
- ⚪ Validate ownership (only sender can edit/delete)
- ⚪ Soft delete (IsDeleted flag, not physical delete)
- ⚪ Store edit history (optional: MessageEditHistory table)

#### API Endpoints
- ⚪ `PATCH /api/v1/sponsorship/messages/{id}` - Edit message
- ⚪ `DELETE /api/v1/sponsorship/messages/{id}` - Delete message

#### Business Rules
- ⚪ Edit: Within 1 hour (feature.TimeLimit)
- ⚪ Delete: Within 24 hours (feature.TimeLimit)
- ⚪ Only sender can edit/delete
- ⚪ Edited messages show "Edited" flag
- ⚪ Deleted messages show "Message deleted" placeholder

#### SignalR Events
- ⚪ `MessageEdited` event with new content
- ⚪ `MessageDeleted` event with messageId

#### Testing Checklist
- ⚪ Edit within 1 hour → Success
- ⚪ Edit after 1 hour → Error
- ⚪ Delete within 24 hours → Success
- ⚪ Delete after 24 hours → Error
- ⚪ Non-owner tries to edit → Error
- ⚪ Feature disabled → Error

---

### Part B: Message Forward (0%)

#### Backend Implementation
- ⚪ Create `ForwardMessageCommand.cs` handler
- ⚪ Validate recipient (must be in user's contacts or sponsored farmers)
- ⚪ Create new message with forwarded content
- ⚪ Add `ForwardedFromMessageId` reference (optional)

#### API Endpoints
- ⚪ `POST /api/v1/sponsorship/messages/{id}/forward` - Forward message

#### Business Rules
- ⚪ Can only forward to valid recipients
- ⚪ Attachments are copied (not re-uploaded)
- ⚪ Forwarded messages show "Forwarded" indicator
- ⚪ Feature flag validation

#### Testing Checklist
- ⚪ Forward text message → Success
- ⚪ Forward with attachment → Success, attachment copied
- ⚪ Forward to invalid recipient → Error
- ⚪ Feature disabled → Error

---

### Part C: Link Preview (0%)

#### Database Changes
- ⚪ Create `MessageLinkPreviews` table
  - messageId (FK)
  - url
  - title
  - description
  - imageUrl
  - siteName
  - createdDate

#### Backend Implementation
- ⚪ Create `GenerateLinkPreviewCommand.cs` handler
- ⚪ Integrate with OpenGraph scraper library
- ⚪ Extract metadata (title, description, image)
- ⚪ Store in MessageLinkPreviews table
- ⚪ Return preview data in message response

#### API Endpoints
- ⚪ `POST /api/v1/sponsorship/messages/link-preview` - Generate preview
- ⚪ Auto-detect URLs in message content
- ⚪ Return preview metadata

#### Business Rules
- ⚪ Max 3 previews per message
- ⚪ Timeout: 5 seconds per URL
- ⚪ Whitelist domains for security
- ⚪ Cache previews (24 hours)
- ⚪ Feature flag validation

#### Testing Checklist
- ⚪ Send URL → Preview generated
- ⚪ Invalid URL → No preview
- ⚪ Timeout → Graceful fallback
- ⚪ Feature disabled → No preview generated

---

## 📦 DATABASE MIGRATIONS SUMMARY

### Migration 1: MessagingFeatures Table (Foundation)
**Status:** ⚪ Not Started
**File:** `claudedocs/migrations/MessagingFeatures_Migration.sql`

```sql
CREATE TABLE MessagingFeatures (
    Id SERIAL PRIMARY KEY,
    FeatureName VARCHAR(100) UNIQUE NOT NULL,
    DisplayName VARCHAR(200),
    IsEnabled BOOLEAN DEFAULT true,
    RequiredTier VARCHAR(20) DEFAULT 'None',
    MaxFileSize BIGINT,
    MaxDuration INT,
    AllowedMimeTypes VARCHAR(1000),
    TimeLimit INT,
    Description VARCHAR(500),
    ConfigurationJson TEXT,
    CreatedDate TIMESTAMP DEFAULT NOW(),
    UpdatedDate TIMESTAMP,
    CreatedByUserId INT,
    UpdatedByUserId INT,
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id) ON DELETE SET NULL,
    FOREIGN KEY (UpdatedByUserId) REFERENCES Users(Id) ON DELETE SET NULL
);

CREATE INDEX idx_messaging_features_name ON MessagingFeatures(FeatureName);
```

**Seed Data:** 9 features (VoiceMessages, ImageAttachments, etc.)

---

### Migration 2: User Avatar Support (Phase 1A)
**Status:** ⚪ Not Started
**File:** `claudedocs/migrations/UserAvatar_Migration.sql`

```sql
ALTER TABLE Users ADD COLUMN AvatarUrl VARCHAR(500);
ALTER TABLE Users ADD COLUMN AvatarThumbnailUrl VARCHAR(500);
ALTER TABLE Users ADD COLUMN AvatarUploadedDate TIMESTAMP;

CREATE INDEX idx_users_avatar ON Users(AvatarUrl);
```

---

### Migration 3: Message Status Enhancement (Phase 1B)
**Status:** ⚪ Not Started
**File:** `claudedocs/migrations/MessageStatus_Migration.sql`

```sql
ALTER TABLE AnalysisMessages ADD COLUMN Status VARCHAR(20) DEFAULT 'sent';
ALTER TABLE AnalysisMessages ADD COLUMN DeliveredDate TIMESTAMP;
ALTER TABLE AnalysisMessages ADD COLUMN IsEdited BOOLEAN DEFAULT false;
ALTER TABLE AnalysisMessages ADD COLUMN EditedDate TIMESTAMP;

-- Update existing records
UPDATE AnalysisMessages SET Status = 'sent' WHERE Status IS NULL;

CREATE INDEX idx_messages_status ON AnalysisMessages(Status);
CREATE INDEX idx_messages_delivered ON AnalysisMessages(DeliveredDate);
```

---

### Migration 4: Attachment Enhancement (Phase 2A)
**Status:** ⚪ Not Started
**File:** `claudedocs/migrations/AttachmentEnhancement_Migration.sql`

```sql
ALTER TABLE AnalysisMessages ADD COLUMN AttachmentType VARCHAR(50);
ALTER TABLE AnalysisMessages ADD COLUMN AttachmentSize BIGINT;
ALTER TABLE AnalysisMessages ADD COLUMN AttachmentFilename VARCHAR(255);
ALTER TABLE AnalysisMessages ADD COLUMN AttachmentMimeType VARCHAR(100);
ALTER TABLE AnalysisMessages ADD COLUMN AttachmentThumbnailUrl VARCHAR(500);
ALTER TABLE AnalysisMessages ADD COLUMN AttachmentDuration INT;
ALTER TABLE AnalysisMessages ADD COLUMN WaveformData TEXT;

CREATE INDEX idx_messages_attachment_type ON AnalysisMessages(AttachmentType);
```

---

### Migration 5: Link Previews (Phase 4C)
**Status:** ⚪ Not Started
**File:** `claudedocs/migrations/LinkPreviews_Migration.sql`

```sql
CREATE TABLE MessageLinkPreviews (
    Id SERIAL PRIMARY KEY,
    MessageId INT NOT NULL,
    Url VARCHAR(1000) NOT NULL,
    Title VARCHAR(500),
    Description TEXT,
    ImageUrl VARCHAR(500),
    SiteName VARCHAR(255),
    CreatedDate TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (MessageId) REFERENCES AnalysisMessages(Id) ON DELETE CASCADE
);

CREATE INDEX idx_link_previews_message ON MessageLinkPreviews(MessageId);
CREATE INDEX idx_link_previews_url ON MessageLinkPreviews(Url);
```

---

## 🔌 API ENDPOINTS SUMMARY

### Foundation Endpoints
- ⚪ `GET /api/v1/messaging/features` - Get user's available features
- ⚪ `PATCH /api/v1/admin/messaging/features/{id}` - Toggle feature (admin)

### Phase 1 Endpoints
- ⚪ `POST /api/v1/users/avatar` - Upload avatar
- ⚪ `GET /api/v1/users/{userId}/avatar` - Get avatar
- ⚪ `DELETE /api/v1/users/avatar` - Delete avatar
- ⚪ `PATCH /api/v1/sponsorship/messages/{id}/read` - Mark as read
- ⚪ `POST /api/v1/sponsorship/messages/mark-read` - Bulk mark as read

### Phase 2 Endpoints
- ⚪ `POST /api/v1/sponsorship/messages/with-attachment` - Send with attachment
- ⚪ `POST /api/v1/sponsorship/messages/voice` - Send voice message

### Phase 3 Endpoints
- ⚪ SignalR: `UserTyping`, `UserStoppedTyping` events
- ⚪ SignalR: `MessageDelivered`, `MessageSeen` events

### Phase 4 Endpoints
- ⚪ `PATCH /api/v1/sponsorship/messages/{id}` - Edit message
- ⚪ `DELETE /api/v1/sponsorship/messages/{id}` - Delete message
- ⚪ `POST /api/v1/sponsorship/messages/{id}/forward` - Forward message
- ⚪ `POST /api/v1/sponsorship/messages/link-preview` - Generate link preview

---

## 📚 DOCUMENTATION REQUIREMENTS

### For Mobile Team
- ⚪ Update `MESSAGING_MOBILE_INTEGRATION.md` with new features
- ⚪ Create feature-specific guides:
  - ⚪ Avatar integration guide
  - ⚪ Message status UI guide
  - ⚪ Attachment handling guide
  - ⚪ Voice message player guide
  - ⚪ Typing indicator guide

### For Backend Team
- ⚪ API documentation (Swagger annotations)
- ⚪ Feature flag configuration guide
- ⚪ Migration execution guide
- ⚪ Testing scenarios document

### For DevOps
- ⚪ CDN configuration for avatars/attachments
- ⚪ S3 bucket setup (if not already done)
- ⚪ SignalR scaling considerations
- ⚪ Database backup before migrations

---

## 🧪 TESTING STRATEGY

### Unit Tests
- ⚪ MessagingFeatureService tests
- ⚪ Feature validation tests
- ⚪ Tier hierarchy tests
- ⚪ File size validation tests
- ⚪ MIME type validation tests

### Integration Tests
- ⚪ Feature flag endpoint tests
- ⚪ Avatar upload/download tests
- ⚪ Message status update tests
- ⚪ Attachment upload tests
- ⚪ Voice message tests

### E2E Tests
- ⚪ Complete messaging flow with features
- ⚪ SignalR event delivery tests
- ⚪ Tier-based access tests
- ⚪ Feature toggle impact tests

---

## 🚀 DEPLOYMENT CHECKLIST

### Pre-deployment
- ⚪ All migrations reviewed and tested locally
- ⚪ Seed data verified
- ⚪ API endpoints tested with Postman
- ⚪ SignalR events tested
- ⚪ Documentation updated
- ⚪ Mobile team notified

### Staging Deployment
- ⚪ Create PR to staging
- ⚪ Apply migrations in Railway console
- ⚪ Verify table structures
- ⚪ Run seed data scripts
- ⚪ Test all endpoints
- ⚪ Verify SignalR events
- ⚪ Load testing (optional)

### Production Deployment
- ⚪ Merge to master
- ⚪ Database backup
- ⚪ Apply migrations
- ⚪ Monitor error logs
- ⚪ Performance monitoring
- ⚪ Mobile app release coordination

---

## 📝 SESSION NOTES

### Session 1: 2025-10-19
**Branch Created:** `feature/sponsor-farmer-chat-enhancements`
**Work Done:**
- ✅ Created MessagingFeature entity
- ✅ Created MessagingFeaturesDto DTOs
- ✅ Created IMessagingFeatureRepository + implementation
- ✅ Created MessagingFeatureEntityConfiguration
- ✅ Added DbSet to ProjectDbContext
- ✅ Created IMessagingFeatureService + implementation
- ⚪ Database migration pending
- ⚪ DI registration pending
- ⚪ API endpoints pending

**Next Session Tasks:**
1. Create MessagingFeatures migration SQL
2. Create seed data SQL (9 features)
3. Register services in AutofacBusinessModule
4. Create GetMessagingFeaturesQuery handler
5. Add API endpoint
6. Test with Postman

---

## 🔗 RELATED DOCUMENTS

- `BACKEND_REQUIREMENTS_FLUTTER_CHAT_UI_FEATURES.md` - Mobile team requirements
- `SPONSOR_FARMER_MESSAGING_SYSTEM.md` - Original messaging system docs
- `MESSAGING_MOBILE_INTEGRATION.md` - Mobile integration guide
- `MESSAGING_END_TO_END_TESTS.md` - Testing scenarios
- `mobile-farmer-reply-feature.md` - Farmer reply feature docs
- `signalr-messaging-integration.md` - SignalR integration guide

---

## 📊 FEATURE PRIORITY MATRIX

| Feature | Mobile Priority | Backend Complexity | User Impact | Implementation Order |
|---------|----------------|-------------------|-------------|---------------------|
| Feature Flags | ⭐⭐⭐ | Low | High | 1 |
| Avatar | ⭐⭐⭐ | Low | High | 2 |
| Message Status | ⭐⭐⭐ | Medium | High | 3 |
| Image Attachments | ⭐⭐⭐ | Medium | High | 4 |
| Voice Messages | ⭐⭐ | High | Medium | 5 |
| Typing Indicator | ⭐⭐ | Low | Medium | 6 |
| Message Edit | ⭐⭐ | Medium | Medium | 7 |
| Message Delete | ⭐⭐ | Low | Medium | 8 |
| Message Forward | ⭐ | Low | Low | 9 |
| Link Preview | ⭐ | Medium | Low | 10 |

---

## 🎯 SUCCESS CRITERIA

### Foundation
- ✅ Feature flags working
- ✅ Admin can toggle features
- ✅ Mobile app receives correct feature config
- ✅ Tier-based access enforced

### Phase 1
- ✅ Users can upload/view avatars
- ✅ Messages show sender avatars
- ✅ Message status (sent/delivered/seen) working
- ✅ Read receipts delivered via SignalR

### Phase 2
- ✅ Image/file attachments working
- ✅ Thumbnails generated
- ✅ Voice messages with waveform
- ✅ All file size limits enforced

### Phase 3
- ✅ Typing indicator working
- ✅ Real-time status updates
- ✅ SignalR events delivered reliably

### Phase 4
- ✅ Edit/delete working with time limits
- ✅ Forward working correctly
- ✅ Link previews generated

---

## 🐛 KNOWN ISSUES / RISKS

### Current Issues
- None yet (project just started)

### Potential Risks
1. **SignalR Scaling:** May need Redis backplane for multiple server instances
2. **CDN Costs:** Image/voice storage could increase costs
3. **Waveform Generation:** May be CPU-intensive for voice messages
4. **Link Preview:** External URL fetching could be slow/unreliable
5. **Migration Downtime:** Large tables may require maintenance window

### Mitigation Strategies
1. Implement Redis backplane from start
2. Set aggressive file size limits, enable compression
3. Use background jobs for waveform generation
4. Cache link previews, set timeout, whitelist domains
5. Apply migrations during low-traffic hours, use online migrations

---

## 📞 CONTACTS & RESOURCES

### Team Contacts
- **Backend Lead:** TBD
- **Mobile Team Lead:** TBD
- **DevOps:** TBD

### Useful Resources
- Railway Console: https://railway.app/
- Postman Collection: `ZiraAI_Complete_API_Collection_v6.1.json`
- Staging API: https://ziraai-api-sit.up.railway.app
- SignalR Hub: wss://ziraai-api-sit.up.railway.app/hubs/plantanalysis

---

**Last Updated:** 2025-10-19
**Updated By:** Claude (Session 1)
**Next Review Date:** After each phase completion
