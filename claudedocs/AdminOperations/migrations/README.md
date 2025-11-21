# Payment Integration Database Migrations

## Overview
This directory contains manual SQL migration scripts for the iyzico payment integration feature.

## Migration Files

### Forward Migrations (Apply in Order)

#### 001_create_payment_transactions.sql
**Purpose:** Creates the main `PaymentTransactions` table for tracking all payment operations.

**Key Features:**
- Tracks both sponsor bulk purchases and farmer subscriptions
- Stores iyzico token, payment ID, conversation ID
- Includes full initialize and verify responses for debugging
- Status tracking: Initialized, Pending, Success, Failed, Expired
- Token expiration tracking for security
- Comprehensive indexing for performance

**Schema:**
```
PaymentTransactions
├── Id (PK, SERIAL)
├── UserId (FK → Users)
├── FlowType (VARCHAR: 'SponsorBulkPurchase' | 'FarmerSubscription')
├── FlowDataJson (TEXT: JSON with flow-specific data)
├── SponsorshipPurchaseId (FK → SponsorshipPurchases, nullable)
├── UserSubscriptionId (FK → UserSubscriptions, nullable, future)
├── IyzicoToken (VARCHAR, UNIQUE)
├── IyzicoPaymentId (VARCHAR, nullable)
├── ConversationId (VARCHAR, UNIQUE)
├── Amount (NUMERIC)
├── Currency (VARCHAR, default 'TRY')
├── Status (VARCHAR)
├── InitializedAt (TIMESTAMP)
├── CompletedAt (TIMESTAMP, nullable)
├── ExpiresAt (TIMESTAMP)
├── InitializeResponse (TEXT, JSON)
├── VerifyResponse (TEXT, JSON)
├── ErrorMessage (TEXT, nullable)
├── CreatedDate (TIMESTAMP)
└── UpdatedDate (TIMESTAMP, nullable)
```

**Indexes:**
- `IDX_PaymentTransactions_IyzicoToken` - Payment completion lookups
- `IDX_PaymentTransactions_ConversationId` - Webhook processing
- `IDX_PaymentTransactions_Status` - Status filtering
- `IDX_PaymentTransactions_UserId` - User payment history
- `IDX_PaymentTransactions_CreatedDate` - Reporting and analytics
- `IDX_PaymentTransactions_FlowType` - Flow-specific queries

#### 002_alter_sponsorship_purchases.sql
**Purpose:** Links existing `SponsorshipPurchases` table to `PaymentTransactions`.

**Changes:**
- Adds `PaymentTransactionId` column (nullable FK)
- Creates foreign key constraint with ON DELETE SET NULL
- Creates index for foreign key lookups

**Why Nullable:**
- Supports backward compatibility with existing records
- Allows for non-payment purchase scenarios (admin-created, promotional, etc.)

### Rollback Migrations (Apply in Reverse Order)

#### rollback_002.sql
Removes `PaymentTransactionId` from `SponsorshipPurchases` table.

**⚠️ WARNING:** This will break the link between purchases and payment transactions.

#### rollback_001.sql
Drops `PaymentTransactions` table and all related indexes.

**⚠️ WARNING:** This will delete ALL payment transaction data permanently!

## Execution Instructions

### Applying Migrations (Forward)

**Step 1: Verify Prerequisites**
```sql
-- Ensure Users table exists
SELECT * FROM "Users" LIMIT 1;

-- Ensure SponsorshipPurchases table exists
SELECT * FROM "SponsorshipPurchases" LIMIT 1;
```

**Step 2: Apply Migrations in Order**
```bash
# Connect to staging database
psql -h <host> -U <username> -d <database>

# Apply migration 001
\i claudedocs/AdminOperations/migrations/001_create_payment_transactions.sql

# Verify table creation
SELECT COUNT(*) FROM "PaymentTransactions";

# Apply migration 002
\i claudedocs/AdminOperations/migrations/002_alter_sponsorship_purchases.sql

# Verify column addition
SELECT "PaymentTransactionId" FROM "SponsorshipPurchases" LIMIT 1;
```

**Step 3: Verification**
```sql
-- Check table structure
\d "PaymentTransactions"

-- Check indexes
\di "PaymentTransactions"*

-- Check foreign keys
SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.table_name = 'PaymentTransactions'
    AND tc.constraint_type = 'FOREIGN KEY';
```

### Rolling Back Migrations (Reverse)

**⚠️ CRITICAL WARNING:**
- Rollback will delete ALL payment transaction data
- Ensure you have a database backup before rollback
- Only rollback in emergency scenarios or during development

**Step 1: Backup Data**
```sql
-- Export PaymentTransactions data
COPY "PaymentTransactions" TO '/tmp/payment_transactions_backup.csv' CSV HEADER;

-- Export SponsorshipPurchases data
COPY "SponsorshipPurchases" TO '/tmp/sponsorship_purchases_backup.csv' CSV HEADER;
```

**Step 2: Apply Rollbacks in Reverse Order**
```bash
# Apply rollback 002 first (removes FK from SponsorshipPurchases)
\i claudedocs/AdminOperations/migrations/rollback_002.sql

# Verify column removal
\d "SponsorshipPurchases"

# Apply rollback 001 (drops PaymentTransactions table)
\i claudedocs/AdminOperations/migrations/rollback_001.sql

# Verify table removal
\dt "PaymentTransactions"
```

## Railway Deployment Notes

### Staging Environment
1. Connect to Railway staging database via CLI or web console
2. Copy SQL script contents
3. Execute scripts manually in order
4. Verify execution with provided verification queries

### Production Environment (Future)
1. **NEVER** run migrations directly in production without approval
2. Test migrations in staging first
3. Create database backup before applying
4. Schedule maintenance window if needed
5. Have rollback scripts ready
6. Monitor application logs after deployment

## Testing After Migration

### Test Data Insertion
```sql
-- Insert test payment transaction
INSERT INTO "PaymentTransactions" (
    "UserId",
    "FlowType",
    "FlowDataJson",
    "IyzicoToken",
    "ConversationId",
    "Amount",
    "Currency",
    "Status",
    "ExpiresAt"
) VALUES (
    1,
    'SponsorBulkPurchase',
    '{"tierId": 2, "quantity": 100, "tierName": "M"}',
    'test-token-' || gen_random_uuid()::text,
    'conv-' || gen_random_uuid()::text,
    5000.00,
    'TRY',
    'Initialized',
    CURRENT_TIMESTAMP + INTERVAL '30 minutes'
);

-- Verify insertion
SELECT * FROM "PaymentTransactions" ORDER BY "Id" DESC LIMIT 1;

-- Test foreign key relationship
UPDATE "SponsorshipPurchases"
SET "PaymentTransactionId" = (SELECT "Id" FROM "PaymentTransactions" ORDER BY "Id" DESC LIMIT 1)
WHERE "Id" = (SELECT "Id" FROM "SponsorshipPurchases" ORDER BY "Id" DESC LIMIT 1);

-- Verify relationship
SELECT
    sp."Id" AS PurchaseId,
    sp."PaymentTransactionId",
    pt."Status" AS PaymentStatus,
    pt."Amount" AS PaymentAmount
FROM "SponsorshipPurchases" sp
LEFT JOIN "PaymentTransactions" pt ON sp."PaymentTransactionId" = pt."Id"
WHERE sp."PaymentTransactionId" IS NOT NULL
LIMIT 5;
```

### Cleanup Test Data
```sql
-- Remove test records
DELETE FROM "PaymentTransactions" WHERE "IyzicoToken" LIKE 'test-token-%';
UPDATE "SponsorshipPurchases" SET "PaymentTransactionId" = NULL WHERE "PaymentTransactionId" IS NOT NULL;
```

## Troubleshooting

### Common Issues

**Issue: Foreign key constraint violation**
```
ERROR: insert or update on table "SponsorshipPurchases" violates foreign key constraint
```
**Solution:** Ensure PaymentTransactions table exists and migration 001 was applied successfully.

**Issue: Column already exists**
```
ERROR: column "PaymentTransactionId" of relation "SponsorshipPurchases" already exists
```
**Solution:** Migration 002 was already applied. Skip or use `IF NOT EXISTS` clause.

**Issue: Table already exists**
```
ERROR: relation "PaymentTransactions" already exists
```
**Solution:** Migration 001 was already applied. Skip or verify table structure.

### Verification Queries

```sql
-- Check migration status
SELECT
    table_name,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'PaymentTransactions') as column_count
FROM information_schema.tables
WHERE table_name IN ('PaymentTransactions', 'SponsorshipPurchases');

-- Check indexes
SELECT
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'PaymentTransactions';

-- Check foreign keys
SELECT
    conname AS constraint_name,
    conrelid::regclass AS table_name,
    confrelid::regclass AS referenced_table
FROM pg_constraint
WHERE contype = 'f'
    AND (conrelid::regclass::text = 'PaymentTransactions'
         OR conrelid::regclass::text = 'SponsorshipPurchases');
```

## Next Steps After Migration

1. ✅ Build verification: `dotnet build`
2. ⏳ Update Entity Framework DbContext (Phase 4)
3. ⏳ Create Entity classes (Phase 4)
4. ⏳ Create Repository layer (Phase 5)
5. ⏳ Implement payment service (Phase 6)

## Contact & Support

For migration issues or questions:
- Check implementation plan: `claudedocs/AdminOperations/SPONSOR_PAYMENT_IMPLEMENTATION_PLAN.md`
- Review analysis documents: `claudedocs/iyzico-payment-integration-*.md`
- Consult with database administrator before production deployment
