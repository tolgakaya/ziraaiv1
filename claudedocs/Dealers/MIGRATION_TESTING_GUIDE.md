# Migration Testing Guide - Dealer Invitation v2.0

**Version**: 2.0  
**Date**: 2025-10-30  
**Status**: Production Ready  
**Migration Required**: Yes

---

## 🎯 Overview

This guide provides step-by-step instructions for testing the migration from `purchaseId`-based dealer invitation system to the new intelligent `packageTier`-based code selection system.

---

## ⚠️ Pre-Migration Checklist

### 1. Database Backup
```bash
# PostgreSQL backup (Railway Production)
pg_dump -U postgres -h containers-us-west-xxx.railway.app \
  -p xxxx -d railway > backup_dealer_invitation_$(date +%Y%m%d_%H%M%S).sql

# Local backup
pg_dump -U postgres -d ziraai_dev > backup_local_$(date +%Y%m%d_%H%M%S).sql
```

### 2. Verify Current State
```sql
-- Check existing invitations
SELECT COUNT(*) as total_invitations,
       COUNT(DISTINCT "PurchaseId") as unique_purchases,
       COUNT(CASE WHEN "Status" = 'Pending' THEN 1 END) as pending
FROM "DealerInvitations";

-- Check code distribution
SELECT sp."Id" as purchase_id,
       st."TierName",
       COUNT(*) as total_codes,
       COUNT(CASE WHEN "IsUsed" = false AND "DealerId" IS NULL THEN 1 END) as available
FROM "SponsorshipCodes" sc
INNER JOIN "SponsorshipPurchases" sp ON sc."SponsorshipPurchaseId" = sp."Id"
INNER JOIN "SubscriptionTiers" st ON sc."SubscriptionTierId" = st."Id"
GROUP BY sp."Id", st."TierName"
ORDER BY sp."Id";
```

### 3. Environment Configuration
```bash
# Verify API is running
curl -X GET https://ziraai-api-sit.up.railway.app/health

# Check database connectivity
psql -U postgres -h your-railway-host -p port -d railway -c "SELECT 1;"
```

---

## 🔧 Migration Execution

### Step 1: Run Migration Script

**File**: `claudedocs/Dealers/migrations/001_remove_purchaseid_add_packagetier_and_reservation.sql`

```bash
# On Railway (Staging)
psql -U postgres -h containers-us-west-xxx.railway.app \
  -p xxxx -d railway \
  -f claudedocs/Dealers/migrations/001_remove_purchaseid_add_packagetier_and_reservation.sql

# On Local Dev
psql -U postgres -d ziraai_dev \
  -f claudedocs/Dealers/migrations/001_remove_purchaseid_add_packagetier_and_reservation.sql
```

**Expected Output:**
```
NOTICE:  Column PurchaseId is now nullable
NOTICE:  Column PackageTier added to DealerInvitations
NOTICE:  Column ReservedForInvitationId added to SponsorshipCodes
NOTICE:  Column ReservedAt added to SponsorshipCodes
NOTICE:  Foreign key constraint added
CREATE INDEX
CREATE INDEX
CREATE INDEX
UPDATE 15  -- Number of existing invitations migrated
```

### Step 2: Verify Migration Success

```sql
-- 1. Check new columns exist
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'DealerInvitations'
  AND column_name IN ('PurchaseId', 'PackageTier');

-- Expected:
-- PurchaseId | integer | YES
-- PackageTier | character varying | YES

-- 2. Check reservation columns
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'SponsorshipCodes'
  AND column_name IN ('ReservedForInvitationId', 'ReservedAt');

-- Expected:
-- ReservedForInvitationId | integer
-- ReservedAt | timestamp without time zone

-- 3. Verify data migration
SELECT 
    "Id",
    "DealerName",
    "PurchaseId",
    "PackageTier",
    "CodeCount",
    "Status"
FROM "DealerInvitations"
WHERE "PurchaseId" IS NOT NULL
LIMIT 5;

-- Expected: PackageTier should match tier from original purchase
```

### Step 3: Verify Indexes Created

```sql
-- Check performance indexes
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'SponsorshipCodes'
  AND indexname LIKE '%IntelligentSelection%';

-- Expected: 3 indexes
-- IX_SponsorshipCodes_IntelligentSelection
-- IX_SponsorshipCodes_Reservation
-- IX_DealerInvitations_PackageTier
```

---

## 🧪 Functional Testing

### Test Suite 1: Basic Tier-Based Invitation

#### Test 1.1: Invite with Tier M
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invite-via-sms" \
  -H "Authorization: Bearer YOUR_SPONSOR_TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "email": "dealer1@test.com",
    "phone": "+905551111111",
    "dealerName": "Test Dealer 1",
    "packageTier": "M",
    "codeCount": 5
  }'
```

**Expected Result:**
```json
{
  "success": true,
  "message": "📱 Bayilik daveti +905551111111 numarasına SMS ile gönderildi",
  "data": {
    "invitationId": 123,
    "codeCount": 5,
    "status": "Pending",
    "smsSent": true
  }
}
```

**Verify in Database:**
```sql
-- Check invitation created
SELECT "Id", "PackageTier", "CodeCount", "Status"
FROM "DealerInvitations"
WHERE "Email" = 'dealer1@test.com';

-- Check codes reserved
SELECT COUNT(*) as reserved_codes,
       st."TierName"
FROM "SponsorshipCodes" sc
INNER JOIN "DealerInvitations" di ON sc."ReservedForInvitationId" = di."Id"
INNER JOIN "SubscriptionTiers" st ON sc."SubscriptionTierId" = st."Id"
WHERE di."Email" = 'dealer1@test.com'
GROUP BY st."TierName";

-- Expected: 5 codes with TierName = 'M'
```

#### Test 1.2: Insufficient Tier Codes
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invite-via-sms" \
  -H "Authorization: Bearer YOUR_SPONSOR_TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "email": "dealer2@test.com",
    "phone": "+905552222222",
    "dealerName": "Test Dealer 2",
    "packageTier": "XL",
    "codeCount": 100
  }'
```

**Expected Result:**
```json
{
  "success": false,
  "message": "Yetersiz kod (XL tier). Mevcut: 3, İstenen: 100"
}
```

### Test Suite 2: Multi-Purchase Selection

#### Test 2.1: Auto-Selection from Multiple Purchases
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invite-via-sms" \
  -H "Authorization: Bearer YOUR_SPONSOR_TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "email": "dealer3@test.com",
    "phone": "+905553333333",
    "dealerName": "Multi-Purchase Dealer",
    "codeCount": 20
  }'
```

**Verify FIFO Ordering:**
```sql
-- Check codes selected (should be expiring soonest first)
SELECT sc."Id",
       sc."Code",
       sc."ExpiryDate",
       sc."CreatedDate",
       st."TierName",
       sp."Id" as purchase_id
FROM "SponsorshipCodes" sc
INNER JOIN "DealerInvitations" di ON sc."ReservedForInvitationId" = di."Id"
INNER JOIN "SubscriptionTiers" st ON sc."SubscriptionTierId" = st."Id"
INNER JOIN "SponsorshipPurchases" sp ON sc."SponsorshipPurchaseId" = sp."Id"
WHERE di."Email" = 'dealer3@test.com'
ORDER BY sc."ExpiryDate", sc."CreatedDate";

-- Expected: Codes ordered by ExpiryDate ASC, then CreatedDate ASC
-- May come from MULTIPLE purchases
```

### Test Suite 3: Backward Compatibility

#### Test 3.1: Old purchaseId Still Works
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/transfer-codes" \
  -H "Authorization: Bearer YOUR_SPONSOR_TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "dealerId": 158,
    "purchaseId": 26,
    "codeCount": 3
  }'
```

**Expected Result:**
```json
{
  "success": true,
  "message": "Bayiye başarıyla 3 kod aktarıldı",
  "data": {
    "transferredCodeIds": [940, 941, 942],
    "transferredCount": 3
  }
}
```

**Verify:**
```sql
-- All codes should be from purchase 26
SELECT sc."Id", sp."Id" as purchase_id
FROM "SponsorshipCodes" sc
INNER JOIN "SponsorshipPurchases" sp ON sc."SponsorshipPurchaseId" = sp."Id"
WHERE sc."Id" IN (940, 941, 942);

-- Expected: All rows have purchase_id = 26
```

#### Test 3.2: Combined purchaseId + packageTier
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/transfer-codes" \
  -H "Authorization: Bearer YOUR_SPONSOR_TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "dealerId": 158,
    "purchaseId": 26,
    "packageTier": "M",
    "codeCount": 5
  }'
```

**Expected:** Only M-tier codes from purchase 26

### Test Suite 4: Reservation System

#### Test 4.1: Code Reservation on Invite
```bash
# Create invitation
INVITATION_ID=$(curl -X POST "..." | jq -r '.data.invitationId')

# Verify reservation
psql -c "
SELECT COUNT(*) as reserved_count,
       MIN(\"ReservedAt\") as first_reservation,
       MAX(\"ReservedAt\") as last_reservation
FROM \"SponsorshipCodes\"
WHERE \"ReservedForInvitationId\" = $INVITATION_ID;
"
```

**Expected:** 
- `reserved_count` = codeCount from invitation
- Timestamps within last minute

#### Test 4.2: Reservation Transfer on Accept
```bash
# Accept invitation
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/accept-invitation" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "invitationToken": "YOUR_INVITATION_TOKEN"
  }'
```

**Verify Reservation Cleared:**
```sql
SELECT COUNT(*) as transferred_codes,
       COUNT(CASE WHEN "ReservedForInvitationId" IS NULL THEN 1 END) as reservation_cleared,
       COUNT(CASE WHEN "DealerId" IS NOT NULL THEN 1 END) as dealer_assigned
FROM "SponsorshipCodes"
WHERE "TransferredByUserId" = 159;  -- Sponsor ID

-- Expected:
-- transferred_codes = 5
-- reservation_cleared = 5
-- dealer_assigned = 5
```

### Test Suite 5: Invalid Tier Validation

#### Test 5.1: Invalid Tier String
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invite-via-sms" \
  -H "Authorization: Bearer YOUR_SPONSOR_TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "email": "dealer@test.com",
    "phone": "+905554444444",
    "dealerName": "Invalid Tier Test",
    "packageTier": "XXL",
    "codeCount": 5
  }'
```

**Expected Result:**
```json
{
  "success": false,
  "message": "Geçersiz paket tier. Geçerli değerler: S, M, L, XL"
}
```

---

## 🔍 Performance Testing

### Test 1: Query Performance with Indexes

```sql
-- Enable query timing
\timing on

-- Test intelligent selection query (should use indexes)
EXPLAIN ANALYZE
SELECT sc."Id", sc."Code", sc."ExpiryDate", sc."CreatedDate"
FROM "SponsorshipCodes" sc
WHERE sc."SponsorId" = 159
  AND sc."IsUsed" = false
  AND sc."DealerId" IS NULL
  AND sc."ReservedForInvitationId" IS NULL
  AND sc."ExpiryDate" > NOW()
ORDER BY sc."ExpiryDate", sc."CreatedDate"
LIMIT 10;

-- Expected: 
-- Index Scan using IX_SponsorshipCodes_IntelligentSelection
-- Execution time: < 5ms
```

### Test 2: Concurrent Invitation Creation

```bash
# Run 5 invitations simultaneously
for i in {1..5}; do
  curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invite-via-sms" \
    -H "Authorization: Bearer YOUR_SPONSOR_TOKEN" \
    -H "Content-Type: application/json" \
    -H "x-dev-arch-version: 1.0" \
    -d "{
      \"email\": \"dealer${i}@test.com\",
      \"phone\": \"+90555000000${i}\",
      \"dealerName\": \"Concurrent Dealer ${i}\",
      \"packageTier\": \"M\",
      \"codeCount\": 3
    }" &
done
wait
```

**Verify No Double-Allocation:**
```sql
-- Each code should be reserved for exactly ONE invitation
SELECT "Id", COUNT(*) as reservation_count
FROM (
    SELECT sc."Id"
    FROM "SponsorshipCodes" sc
    WHERE sc."ReservedForInvitationId" IS NOT NULL
) subquery
GROUP BY "Id"
HAVING COUNT(*) > 1;

-- Expected: 0 rows (no duplicate reservations)
```

---

## 📊 Data Integrity Verification

### Verification Script
```sql
-- Run after all tests
DO $$
DECLARE
    v_total_invitations INTEGER;
    v_total_reserved INTEGER;
    v_orphaned_reservations INTEGER;
    v_double_allocations INTEGER;
BEGIN
    -- Count invitations
    SELECT COUNT(*) INTO v_total_invitations FROM "DealerInvitations";
    
    -- Count reserved codes
    SELECT COUNT(*) INTO v_total_reserved
    FROM "SponsorshipCodes"
    WHERE "ReservedForInvitationId" IS NOT NULL;
    
    -- Check orphaned reservations
    SELECT COUNT(*) INTO v_orphaned_reservations
    FROM "SponsorshipCodes" sc
    LEFT JOIN "DealerInvitations" di ON sc."ReservedForInvitationId" = di."Id"
    WHERE sc."ReservedForInvitationId" IS NOT NULL
      AND di."Id" IS NULL;
    
    -- Check double allocations
    SELECT COUNT(*) INTO v_double_allocations
    FROM (
        SELECT "ReservedForInvitationId", COUNT(*) as cnt
        FROM "SponsorshipCodes"
        WHERE "ReservedForInvitationId" IS NOT NULL
        GROUP BY "ReservedForInvitationId"
        HAVING COUNT(*) > (
            SELECT "CodeCount"
            FROM "DealerInvitations"
            WHERE "Id" = "ReservedForInvitationId"
        )
    ) overallocated;
    
    RAISE NOTICE '=== Data Integrity Report ===';
    RAISE NOTICE 'Total Invitations: %', v_total_invitations;
    RAISE NOTICE 'Total Reserved Codes: %', v_total_reserved;
    RAISE NOTICE 'Orphaned Reservations: %', v_orphaned_reservations;
    RAISE NOTICE 'Double Allocations: %', v_double_allocations;
    
    IF v_orphaned_reservations > 0 OR v_double_allocations > 0 THEN
        RAISE WARNING 'Data integrity issues detected!';
    ELSE
        RAISE NOTICE '✅ All integrity checks passed';
    END IF;
END $$;
```

---

## 🔄 Rollback Plan

### If Migration Fails

```sql
-- Rollback script
BEGIN;

-- Drop new columns
ALTER TABLE "DealerInvitations" DROP COLUMN IF EXISTS "PackageTier";
ALTER TABLE "SponsorshipCodes" DROP COLUMN IF EXISTS "ReservedForInvitationId";
ALTER TABLE "SponsorshipCodes" DROP COLUMN IF EXISTS "ReservedAt";

-- Make PurchaseId required again
ALTER TABLE "DealerInvitations" ALTER COLUMN "PurchaseId" SET NOT NULL;

-- Drop indexes
DROP INDEX IF EXISTS "IX_SponsorshipCodes_IntelligentSelection";
DROP INDEX IF EXISTS "IX_SponsorshipCodes_Reservation";
DROP INDEX IF EXISTS "IX_DealerInvitations_PackageTier";

COMMIT;

-- Restore from backup
psql -U postgres -d railway < backup_dealer_invitation_YYYYMMDD_HHMMSS.sql
```

---

## ✅ Post-Migration Checklist

- [ ] All migration parts executed successfully
- [ ] New columns exist in database
- [ ] Indexes created successfully
- [ ] Existing data migrated (PackageTier populated)
- [ ] Test Suite 1: Tier-based invitation ✅
- [ ] Test Suite 2: Multi-purchase selection ✅
- [ ] Test Suite 3: Backward compatibility ✅
- [ ] Test Suite 4: Reservation system ✅
- [ ] Test Suite 5: Invalid tier validation ✅
- [ ] Performance tests show index usage ✅
- [ ] Data integrity verification passed ✅
- [ ] API endpoints respond correctly
- [ ] Postman collection tests pass
- [ ] No orphaned reservations
- [ ] No double allocations
- [ ] Production deployment approved

---

## 📞 Support & Troubleshooting

### Common Issues

**Issue**: "Column PackageTier already exists"  
**Solution**: Migration is idempotent, re-running is safe. Check logs for "NOTICE" messages.

**Issue**: "Insufficient codes" errors  
**Solution**: Verify sponsor has available codes in requested tier:
```sql
SELECT st."TierName", COUNT(*) as available
FROM "SponsorshipCodes" sc
INNER JOIN "SubscriptionTiers" st ON sc."SubscriptionTierId" = st."Id"
WHERE sc."SponsorId" = ?
  AND sc."IsUsed" = false
  AND sc."DealerId" IS NULL
  AND sc."ReservedForInvitationId" IS NULL
GROUP BY st."TierName";
```

**Issue**: Codes not following FIFO order  
**Solution**: Check index usage in query plan, verify ExpiryDate and CreatedDate are set correctly.

---

**Version**: 2.0  
**Last Updated**: 2025-10-30  
**Author**: ZiraAI Backend Team
