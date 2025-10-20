# Database Migration Guide - Chat Enhancements

**Feature Branch**: `feature/sponsor-farmer-messaging`
**Total Migrations**: 7 SQL scripts
**Estimated Time**: 5-10 minutes

---

## 📋 Pre-Migration Checklist

- [ ] Database backup completed
- [ ] PostgreSQL connection verified
- [ ] Development/Staging environment confirmed
- [ ] Migration scripts reviewed

---

## 🔄 Migration Execution Order

### Step 1: User Avatar Support
```bash
psql -U ziraai -d ziraai_dev -f claudedocs/migrations/User_Avatar_Migration.sql
```
**Adds**: `AvatarUrl`, `AvatarThumbnailUrl`, `AvatarUpdatedDate` to Users table

**Expected Output**: `✅ User avatar columns added successfully`

---

### Step 2: Messaging Features Table
```bash
psql -U ziraai -d ziraai_dev -f claudedocs/migrations/MessagingFeatures_Migration.sql
```
**Creates**: `MessagingFeatures` table with foreign keys to Users

**Expected Output**: `✅ MessagingFeatures table created successfully`

**⚠️ Note**: Foreign keys reference `Users(UserId)` (NOT `Id`)

---

### Step 3: Feature Seed Data
```bash
psql -U ziraai -d ziraai_dev -f claudedocs/migrations/MessagingFeatures_SeedData.sql
```
**Inserts**: 9 default messaging features with tier requirements

**Expected Output**: `✅ Seed data inserted successfully` (Total messaging features: 9)

---

### Step 4: Message Status Fields
```bash
psql -U ziraai -d ziraai_dev -f claudedocs/migrations/AnalysisMessage_Status_Migration.sql
```
**Adds**: `MessageStatus`, `DeliveredDate` to AnalysisMessages table
**Updates**: Existing messages to have default status based on `IsRead`

**Expected Output**: `✅ AnalysisMessage status columns added successfully (Phase 1B)`

---

### Step 5: Attachment Metadata
```bash
psql -U ziraai -d ziraai_dev -f claudedocs/migrations/AnalysisMessage_Attachments_Migration.sql
```
**Adds**: `AttachmentTypes`, `AttachmentSizes`, `AttachmentNames`, `AttachmentCount`

**Expected Output**: `✅ AnalysisMessage attachment metadata columns added (Phase 2A)`

---

### Step 6: Voice Message Support
```bash
psql -U ziraai -d ziraai_dev -f claudedocs/migrations/AnalysisMessage_Phase2B_VoiceMessages.sql
```
**Adds**: `VoiceMessageUrl`, `VoiceMessageDuration`, `VoiceMessageWaveform`

**Expected Output**: `✅ Voice message columns added (Phase 2B)`

---

### Step 7: Edit/Delete/Forward Support
```bash
psql -U ziraai -d ziraai_dev -f claudedocs/migrations/AnalysisMessage_Phase4_EditDeleteForward.sql
```
**Adds**: `IsEdited`, `EditedDate`, `OriginalMessage`, `ForwardedFromMessageId`, `IsForwarded`

**Expected Output**: `✅ Edit/Delete/Forward columns added (Phase 4)`

---

## ✅ Verification

### Verify All Migrations Applied
```bash
psql -U ziraai -d ziraai_dev -f claudedocs/migrations/MessagingFeatures_Verification.sql
```

**Expected Results**:
- MessagingFeatures count: 9
- All 9 features listed with tier requirements
- Sample feature details shown

### Manual Verification Queries

```sql
-- Check User table has avatar columns
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'Users'
  AND column_name IN ('AvatarUrl', 'AvatarThumbnailUrl', 'AvatarUpdatedDate');

-- Check MessagingFeatures table exists
SELECT COUNT(*) FROM "MessagingFeatures";
-- Expected: 9

-- Check AnalysisMessages has all new columns
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
```

---

## 🔙 Rollback (If Needed)

### Rollback MessagingFeatures Only
```bash
psql -U ziraai -d ziraai_dev -f claudedocs/migrations/MessagingFeatures_Rollback.sql
```

### Full Rollback (Manual)
```sql
-- Remove User avatar columns
ALTER TABLE "Users"
DROP COLUMN IF EXISTS "AvatarUrl",
DROP COLUMN IF EXISTS "AvatarThumbnailUrl",
DROP COLUMN IF EXISTS "AvatarUpdatedDate";

-- Remove MessagingFeatures table
DROP TABLE IF EXISTS "MessagingFeatures" CASCADE;

-- Remove AnalysisMessage enhancements
ALTER TABLE "AnalysisMessages"
DROP COLUMN IF EXISTS "MessageStatus",
DROP COLUMN IF EXISTS "DeliveredDate",
DROP COLUMN IF EXISTS "AttachmentTypes",
DROP COLUMN IF EXISTS "AttachmentSizes",
DROP COLUMN IF EXISTS "AttachmentNames",
DROP COLUMN IF EXISTS "AttachmentCount",
DROP COLUMN IF EXISTS "VoiceMessageUrl",
DROP COLUMN IF EXISTS "VoiceMessageDuration",
DROP COLUMN IF EXISTS "VoiceMessageWaveform",
DROP COLUMN IF EXISTS "IsEdited",
DROP COLUMN IF EXISTS "EditedDate",
DROP COLUMN IF EXISTS "OriginalMessage",
DROP COLUMN IF EXISTS "ForwardedFromMessageId",
DROP COLUMN IF EXISTS "IsForwarded";
```

---

## 📊 Migration Summary

| Script | Tables Affected | Columns Added | Indexes Created |
|--------|----------------|---------------|-----------------|
| User_Avatar | Users | 3 | 1 |
| MessagingFeatures | MessagingFeatures | 14 (new table) | 3 |
| MessagingFeatures_SeedData | MessagingFeatures | - (inserts) | - |
| AnalysisMessage_Status | AnalysisMessages | 2 | 2 |
| AnalysisMessage_Attachments | AnalysisMessages | 4 | 2 |
| Phase2B_VoiceMessages | AnalysisMessages | 3 | 1 |
| Phase4_EditDeleteForward | AnalysisMessages | 5 | 2 |

**Total Columns Added**: 31
**Total Indexes Created**: 11

---

## 🚨 Common Issues & Solutions

### Issue 1: Foreign Key Error
```
ERROR: foreign key constraint "FK_MessagingFeatures_CreatedBy" references invalid column "Id"
```
**Solution**: Migration scripts have been fixed to reference `Users(UserId)` correctly. Re-download latest scripts.

### Issue 2: Column Already Exists
```
ERROR: column "MessageStatus" of relation "AnalysisMessages" already exists
```
**Solution**: Column already added by previous migration attempt. Skip this migration or use `ADD COLUMN IF NOT EXISTS`.

### Issue 3: Permission Denied
```
ERROR: permission denied for table Users
```
**Solution**: Ensure you're connected with appropriate user privileges (superuser or table owner).

---

## ✅ Post-Migration Steps

1. **Restart Application**: `dotnet run --project WebAPI/WebAPI.csproj`
2. **Test Feature Endpoint**: `GET /api/sponsorship/messaging/features`
3. **Verify Avatar Upload**: `POST /api/users/avatar`
4. **Check SignalR Hub**: Connect to `/hubs/plantanalysis`

---

## 📞 Support

If you encounter any issues during migration:
1. Check error logs in PostgreSQL
2. Verify database connection strings
3. Review migration script syntax
4. Contact backend team with error details

**Migration Created**: 2025-10-19
**Last Updated**: 2025-10-19
