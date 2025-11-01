-- =====================================================
-- Verification Queries for Dealer Distribution System
-- Date: 2025-01-26
-- Purpose: Verify database setup and backward compatibility
-- =====================================================

-- =====================================================
-- STEP 1: Verify DealerId columns exist
-- =====================================================
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name IN ('SponsorshipCodes', 'PlantAnalyses')
  AND column_name IN ('DealerId', 'TransferredAt', 'TransferredByUserId', 'ReclaimedAt', 'ReclaimedByUserId')
ORDER BY table_name, column_name;

-- Expected: Should show all new columns

-- =====================================================
-- STEP 2: Verify DealerInvitations table structure
-- =====================================================
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'DealerInvitations'
ORDER BY ordinal_position;

-- Expected: Should show all 17 columns

-- =====================================================
-- STEP 3: Verify indexes created
-- =====================================================
SELECT 
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename IN ('SponsorshipCodes', 'PlantAnalyses', 'DealerInvitations')
  AND indexname LIKE '%Dealer%'
ORDER BY tablename, indexname;

-- Expected: Should show all dealer-related indexes

-- =====================================================
-- STEP 4: Verify foreign key constraints
-- =====================================================
SELECT
    tc.table_name,
    tc.constraint_name,
    tc.constraint_type,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.table_name IN ('SponsorshipCodes', 'PlantAnalyses', 'DealerInvitations')
  AND tc.constraint_type = 'FOREIGN KEY'
  AND (kcu.column_name LIKE '%Dealer%' OR tc.table_name = 'DealerInvitations')
ORDER BY tc.table_name, tc.constraint_name;

-- =====================================================
-- STEP 5: Backward Compatibility Verification
-- =====================================================

-- Check that all existing codes have NULL DealerId
SELECT 
    'SponsorshipCodes' as TableName,
    COUNT(*) as TotalRecords,
    COUNT("DealerId") as RecordsWithDealer,
    COUNT(*) - COUNT("DealerId") as RecordsWithoutDealer,
    CASE 
        WHEN COUNT("DealerId") = 0 THEN 'PASS - All existing codes have NULL DealerId'
        ELSE 'FAIL - Some codes have DealerId set'
    END as BackwardCompatibility
FROM "SponsorshipCodes"

UNION ALL

-- Check that all existing analyses have NULL DealerId
SELECT 
    'PlantAnalyses' as TableName,
    COUNT(*) as TotalRecords,
    COUNT("DealerId") as RecordsWithDealer,
    COUNT(*) - COUNT("DealerId") as RecordsWithoutDealer,
    CASE 
        WHEN COUNT("DealerId") = 0 THEN 'PASS - All existing analyses have NULL DealerId'
        ELSE 'FAIL - Some analyses have DealerId set'
    END as BackwardCompatibility
FROM "PlantAnalyses";

-- Expected: BackwardCompatibility should be 'PASS' for both tables

-- =====================================================
-- STEP 6: Test existing sponsor queries still work
-- =====================================================

-- Test 1: Sponsor codes query (should work with NULL DealerId)
SELECT 
    COUNT(*) as TotalCodes,
    COUNT(CASE WHEN "UsedByFarmerId" IS NOT NULL THEN 1 END) as UsedCodes,
    COUNT(CASE WHEN "UsedByFarmerId" IS NULL THEN 1 END) as AvailableCodes
FROM "SponsorshipCodes"
WHERE "SponsorId" = 1 -- Replace with actual sponsor ID
  AND "Status" = true;
-- Expected: Should return results regardless of DealerId

-- Test 2: Sponsor analyses query (should work with NULL DealerId)
SELECT 
    COUNT(*) as TotalAnalyses,
    COUNT(CASE WHEN "DealerId" IS NOT NULL THEN 1 END) as DealerAnalyses,
    COUNT(CASE WHEN "DealerId" IS NULL THEN 1 END) as DirectSponsorAnalyses
FROM "PlantAnalyses"
WHERE "SponsorId" = 1; -- Replace with actual sponsor ID
-- Expected: Should return all sponsor analyses (direct + dealer)

-- =====================================================
-- STEP 7: Sample data for testing (OPTIONAL - DO NOT RUN IN PRODUCTION)
-- =====================================================

-- Uncomment to create test invitation:
/*
INSERT INTO "DealerInvitations" (
    "SponsorId",
    "Email",
    "Phone",
    "DealerName",
    "Status",
    "InvitationType",
    "InvitationToken",
    "PurchaseId",
    "CodeCount",
    "CreatedDate",
    "ExpiryDate"
) VALUES (
    1, -- Replace with actual sponsor ID
    'testdealer@example.com',
    '+905551234567',
    'Test Dealer Company',
    'Pending',
    'Invite',
    'test_token_' || gen_random_uuid()::text,
    1, -- Replace with actual purchase ID
    10,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP + INTERVAL '7 days'
);
*/

-- =====================================================
-- SUCCESS MESSAGE
-- =====================================================
SELECT 'Database migration completed successfully. Run verification queries above to confirm.' as Status;
