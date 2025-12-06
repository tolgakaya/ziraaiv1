-- =====================================================
-- Cache Configuration Migration
-- =====================================================
-- Description: Add cache duration configurations for Redis cache TTL management
-- Created: 2025-12-05
-- Phase: Phase 1 - Cache Infrastructure Setup
-- =====================================================

-- =====================================================
-- PART 1: Create Cache Configurations
-- =====================================================

-- Dashboard Cache Duration (15 minutes default)
INSERT INTO "Configurations" ("Key", "Value", "Category", "Description", "ValueType", "IsActive")
SELECT
    'CACHE_DASHBOARD_DURATION_MINUTES',
    '15',
    'Cache',
    'Dashboard cache TTL in minutes (dealer/sponsor dashboards). Default: 15 minutes',
    'int',
    true
WHERE NOT EXISTS (
    SELECT 1 FROM "Configurations"
    WHERE "Key" = 'CACHE_DASHBOARD_DURATION_MINUTES'
);

-- Statistics Cache Duration (60 minutes default)
INSERT INTO "Configurations" ("Key", "Value", "Category", "Description", "ValueType", "IsActive")
SELECT
    'CACHE_STATISTICS_DURATION_MINUTES',
    '60',
    'Cache',
    'Statistics cache TTL in minutes (admin analytics). Default: 60 minutes',
    'int',
    true
WHERE NOT EXISTS (
    SELECT 1 FROM "Configurations"
    WHERE "Key" = 'CACHE_STATISTICS_DURATION_MINUTES'
);

-- Reference Data Cache Duration (1440 minutes = 24 hours default)
INSERT INTO "Configurations" ("Key", "Value", "Category", "Description", "ValueType", "IsActive")
SELECT
    'CACHE_REFERENCE_DATA_DURATION_MINUTES',
    '1440',
    'Cache',
    'Reference data cache TTL in minutes (subscription tiers, configurations). Default: 1440 minutes (24 hours)',
    'int',
    true
WHERE NOT EXISTS (
    SELECT 1 FROM "Configurations"
    WHERE "Key" = 'CACHE_REFERENCE_DATA_DURATION_MINUTES'
);

-- Analytics Cache Duration (15 minutes default)
INSERT INTO "Configurations" ("Key", "Value", "Category", "Description", "ValueType", "IsActive")
SELECT
    'CACHE_ANALYTICS_DURATION_MINUTES',
    '15',
    'Cache',
    'Analytics cache TTL in minutes (sponsor analytics). Default: 15 minutes',
    'int',
    true
WHERE NOT EXISTS (
    SELECT 1 FROM "Configurations"
    WHERE "Key" = 'CACHE_ANALYTICS_DURATION_MINUTES'
);

-- =====================================================
-- PART 2: Verification Queries
-- =====================================================

-- Verify all cache configurations were created
SELECT
    "Key",
    "Value",
    "Category",
    "Description",
    "ValueType",
    "IsActive",
    "CreatedDate"
FROM "Configurations"
WHERE "Category" = 'Cache'
ORDER BY "Key";

-- Expected Results:
-- 4 rows with keys:
-- 1. CACHE_ANALYTICS_DURATION_MINUTES = 15
-- 2. CACHE_DASHBOARD_DURATION_MINUTES = 15
-- 3. CACHE_REFERENCE_DATA_DURATION_MINUTES = 1440
-- 4. CACHE_STATISTICS_DURATION_MINUTES = 60

-- =====================================================
-- PART 3: Rollback Script (if needed)
-- =====================================================

-- Uncomment to rollback (NOT recommended after deployment)
/*
DELETE FROM "Configurations" WHERE "Key" = 'CACHE_DASHBOARD_DURATION_MINUTES';
DELETE FROM "Configurations" WHERE "Key" = 'CACHE_STATISTICS_DURATION_MINUTES';
DELETE FROM "Configurations" WHERE "Key" = 'CACHE_REFERENCE_DATA_DURATION_MINUTES';
DELETE FROM "Configurations" WHERE "Key" = 'CACHE_ANALYTICS_DURATION_MINUTES';
*/

-- =====================================================
-- END OF MIGRATION
-- =====================================================
