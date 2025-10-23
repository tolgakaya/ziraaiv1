# Admin Operations - Database Migrations Guide

**Date:** 2025-10-23  
**Phase:** 1.1 - Base Infrastructure  
**Total Migrations:** 3

---

## 📋 Migration Files

### 1. `001_AddAdminOperationLogs.sql`
**Purpose:** Create audit trail table for all admin operations

**Changes:**
- ✅ Create `AdminOperationLogs` table with full audit fields
- ✅ Add 8 indexes for performance optimization
- ✅ Add foreign keys to Users table
- ✅ Add comments for documentation

**Tables Modified:** None  
**Tables Created:** `AdminOperationLogs`  
**Estimated Time:** 1-2 minutes

---

### 2. `002_AddUserAdminActionColumns.sql`
**Purpose:** Track user deactivation and admin actions on user accounts

**Changes:**
- ✅ Add `IsActive` column (default TRUE)
- ✅ Add `DeactivatedDate`, `DeactivatedBy`, `DeactivationReason` columns
- ✅ Add foreign key to Users table (self-reference)
- ✅ Add 3 indexes for performance
- ✅ Update existing users to active status

**Tables Modified:** `Users`  
**Tables Created:** None  
**Estimated Time:** 1-2 minutes

---

### 3. `003_AddPlantAnalysesOBOColumns.sql`
**Purpose:** Track when admin creates analysis on behalf of a farmer

**Changes:**
- ✅ Add `CreatedByAdminId` column
- ✅ Add `IsOnBehalfOf` column (default FALSE)
- ✅ Add foreign key to Users table
- ✅ Add 4 indexes for performance
- ✅ Add check constraint for data consistency
- ✅ Update existing analyses (IsOnBehalfOf = FALSE)

**Tables Modified:** `PlantAnalyses`  
**Tables Created:** None  
**Estimated Time:** 2-3 minutes

---

## 🚀 Execution Order

**IMPORTANT:** Run migrations in order (001 → 002 → 003)

### Step 1: Run Migration 001
```bash
psql -U your_username -d ziraai_db -f claudedocs/migrations/001_AddAdminOperationLogs.sql
```

**Verification:**
```sql
-- Should return TRUE
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_schema = 'public' 
    AND table_name = 'AdminOperationLogs'
) AS table_exists;

-- Should return 8 indexes
SELECT COUNT(*) FROM pg_indexes WHERE tablename = 'AdminOperationLogs';
```

---

### Step 2: Run Migration 002
```bash
psql -U your_username -d ziraai_db -f claudedocs/migrations/002_AddUserAdminActionColumns.sql
```

**Verification:**
```sql
-- Should return 4 columns
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Users'
    AND column_name IN ('IsActive', 'DeactivatedDate', 'DeactivatedBy', 'DeactivationReason');

-- All users should be active
SELECT COUNT(*) AS total, 
       SUM(CASE WHEN "IsActive" THEN 1 ELSE 0 END) AS active
FROM "Users";
```

---

### Step 3: Run Migration 003
```bash
psql -U your_username -d ziraai_db -f claudedocs/migrations/003_AddPlantAnalysesOBOColumns.sql
```

**Verification:**
```sql
-- Should return 2 columns
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'PlantAnalyses'
    AND column_name IN ('CreatedByAdminId', 'IsOnBehalfOf');

-- Check constraint should exist
SELECT conname FROM pg_constraint 
WHERE conrelid = 'PlantAnalyses'::regclass 
AND conname = 'CHK_PlantAnalyses_OBO_Consistency';
```

---

## ✅ Complete Verification Checklist

After running all 3 migrations:

### Database Structure
- [ ] `AdminOperationLogs` table exists
- [ ] `Users` table has 4 new columns
- [ ] `PlantAnalyses` table has 2 new columns
- [ ] All foreign keys created successfully
- [ ] All indexes created successfully
- [ ] All check constraints created successfully

### Data Integrity
- [ ] All existing users have `IsActive = TRUE`
- [ ] All existing analyses have `IsOnBehalfOf = FALSE`
- [ ] No data loss occurred
- [ ] All foreign keys valid

### Performance
- [ ] Total of 15 new indexes created
- [ ] No locking issues during migration
- [ ] Query performance acceptable

---

## 🔄 Rollback Instructions

If you need to rollback ALL migrations:

```bash
# Rollback in REVERSE order (003 → 002 → 001)

# Step 1: Rollback 003
psql -U your_username -d ziraai_db -c "
DROP INDEX IF EXISTS IX_PlantAnalyses_UserId_IsOnBehalfOf;
DROP INDEX IF EXISTS IX_PlantAnalyses_CreatedByAdminId_CreatedDate;
DROP INDEX IF EXISTS IX_PlantAnalyses_IsOnBehalfOf;
DROP INDEX IF EXISTS IX_PlantAnalyses_CreatedByAdminId;
ALTER TABLE PlantAnalyses DROP CONSTRAINT IF EXISTS CHK_PlantAnalyses_OBO_Consistency;
ALTER TABLE PlantAnalyses DROP CONSTRAINT IF EXISTS FK_PlantAnalyses_CreatedByAdmin;
ALTER TABLE PlantAnalyses DROP COLUMN IF EXISTS IsOnBehalfOf;
ALTER TABLE PlantAnalyses DROP COLUMN IF EXISTS CreatedByAdminId;
"

# Step 2: Rollback 002
psql -U your_username -d ziraai_db -c "
DROP INDEX IF EXISTS IX_Users_IsActive_RecordDate;
DROP INDEX IF EXISTS IX_Users_DeactivatedBy_DeactivatedDate;
DROP INDEX IF EXISTS IX_Users_IsActive;
ALTER TABLE Users DROP CONSTRAINT IF EXISTS FK_Users_DeactivatedBy;
ALTER TABLE Users DROP COLUMN IF EXISTS DeactivationReason;
ALTER TABLE Users DROP COLUMN IF EXISTS DeactivatedBy;
ALTER TABLE Users DROP COLUMN IF EXISTS DeactivatedDate;
ALTER TABLE Users DROP COLUMN IF EXISTS IsActive;
"

# Step 3: Rollback 001
psql -U your_username -d ziraai_db -c "
DROP INDEX IF EXISTS IX_AdminOperationLogs_TargetUserId_Timestamp;
DROP INDEX IF EXISTS IX_AdminOperationLogs_AdminUserId_Timestamp;
DROP INDEX IF EXISTS IX_AdminOperationLogs_IsOnBehalfOf;
DROP INDEX IF EXISTS IX_AdminOperationLogs_Action;
DROP INDEX IF EXISTS IX_AdminOperationLogs_Timestamp;
DROP INDEX IF EXISTS IX_AdminOperationLogs_TargetUserId;
DROP INDEX IF EXISTS IX_AdminOperationLogs_AdminUserId;
DROP TABLE IF EXISTS AdminOperationLogs;
"
```

**OR** use the rollback scripts inside each migration file (commented at the bottom).

---

## 📊 Expected Results

### Table Sizes (Estimated)
- `AdminOperationLogs`: Empty (0 rows) - will grow as admin actions occur
- `Users`: Existing rows + 4 new columns
- `PlantAnalyses`: Existing rows + 2 new columns

### Index Count
- `AdminOperationLogs`: 8 indexes
- `Users`: +3 indexes (related to admin actions)
- `PlantAnalyses`: +4 indexes (related to OBO)

**Total New Indexes:** 15

---

## 🧪 Testing After Migration

### Test 1: AdminOperationLogs Insert
```sql
-- Test audit log insertion
INSERT INTO "AdminOperationLogs" (
    "AdminUserId", 
    "Action", 
    "Timestamp"
) VALUES (
    1,  -- Replace with actual admin user ID
    'TestMigration',
    NOW()
);

-- Verify
SELECT * FROM "AdminOperationLogs" WHERE "Action" = 'TestMigration';

-- Cleanup
DELETE FROM "AdminOperationLogs" WHERE "Action" = 'TestMigration';
```

### Test 2: User Deactivation
```sql
-- Test user deactivation (use a test user)
UPDATE "Users"
SET "IsActive" = FALSE,
    "DeactivatedDate" = NOW(),
    "DeactivatedBy" = 1,
    "DeactivationReason" = 'Test migration'
WHERE "UserId" = 999;  -- Use actual test user ID

-- Verify
SELECT "UserId", "FullName", "IsActive", "DeactivationReason"
FROM "Users"
WHERE "UserId" = 999;

-- Revert
UPDATE "Users"
SET "IsActive" = TRUE,
    "DeactivatedDate" = NULL,
    "DeactivatedBy" = NULL,
    "DeactivationReason" = NULL
WHERE "UserId" = 999;
```

### Test 3: OBO Analysis
```sql
-- Test OBO analysis creation (use test data)
-- NOTE: Don't test if you don't have test users
SELECT 
    COUNT(*) AS total_analyses,
    SUM(CASE WHEN "IsOnBehalfOf" THEN 1 ELSE 0 END) AS obo_count
FROM "PlantAnalyses";
```

---

## 📝 Post-Migration Checklist

### Immediate Actions
- [ ] Verify all 3 migrations ran successfully
- [ ] Check database logs for errors
- [ ] Verify foreign keys and indexes
- [ ] Test basic queries on new columns
- [ ] Commit migration files to git

### Next Steps (After Migration Success)
- [ ] Update Entity models in code (Task 1.1.2)
- [ ] Create repositories (Task 1.1.3)
- [ ] Create AdminAuditService (Task 1.1.4)
- [ ] Update execution plan with completion status

### If Migration Failed
1. Check error messages in console
2. Review database logs
3. Verify database user permissions
4. Check for table locking issues
5. Run rollback if necessary
6. Fix issues and retry

---

## 🔐 Security Notes

1. **Backup First**: Always backup database before running migrations
2. **Test Environment**: Test migrations in dev/staging first
3. **Monitor Performance**: Watch for locking or performance issues
4. **Audit Trail**: AdminOperationLogs will grow - plan for archiving
5. **Foreign Keys**: Cascading deletes are set - be careful with user deletion

---

## 📞 Support

If you encounter issues:
1. Check verification queries in each migration file
2. Review rollback scripts (at bottom of each file)
3. Check database logs: `SELECT * FROM pg_stat_activity;`
4. Verify table locks: `SELECT * FROM pg_locks;`

---

**Migration Guide Status:** ✅ Ready for Execution  
**Last Updated:** 2025-10-23  
**Next Task After Migrations:** Task 1.1.2 - Create Entity Models
