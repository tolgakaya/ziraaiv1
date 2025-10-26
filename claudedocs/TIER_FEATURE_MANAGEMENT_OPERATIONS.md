# Tier Feature Management - Database Operations Guide

**Date:** 2025-10-26  
**System:** Database-Driven Tier Permissions  
**Tables:** Features, TierFeatures, SubscriptionTiers

---

## Overview

This guide provides SQL operations for managing subscription tier features after the initial migration. These operations allow you to add new features, create new tiers, and modify feature assignments without code changes.

---

## Table of Contents

1. [Adding New Features](#adding-new-features)
2. [Creating New Tiers](#creating-new-tiers)
3. [Assigning Features to Tiers](#assigning-features-to-tiers)
4. [Modifying Feature Assignments](#modifying-feature-assignments)
5. [Feature Configuration Management](#feature-configuration-management)
6. [Scheduled Features (Promotions)](#scheduled-features-promotions)
7. [Audit and Monitoring](#audit-and-monitoring)

---

## Adding New Features

### Step 1: Insert Feature Definition

```sql
-- Add a new feature to the registry
INSERT INTO "Features" (
    "FeatureKey", 
    "DisplayName", 
    "Description", 
    "Category",
    "DefaultConfigJson", 
    "RequiresConfiguration", 
    "IsActive", 
    "CreatedDate"
)
VALUES (
    'bulk_sms',                                    -- Unique key used in code
    'Bulk SMS',                                    -- Display name for admin UI
    'Send SMS to multiple farmers at once',       -- Description
    'Communication',                               -- Category
    '{"maxRecipients": 100}',                     -- Default configuration
    TRUE,                                          -- Requires configuration
    TRUE,                                          -- Is active
    NOW()                                          -- Created date
);
```

**Parameters Explained:**

- `FeatureKey`: Unique identifier used in code (`TierFeatureService.HasFeatureAccessAsync()`)
- `DisplayName`: Human-readable name for admin panel
- `Description`: Feature description for documentation
- `Category`: Grouping (Communication, Analytics, Marketing, etc.)
- `DefaultConfigJson`: Default JSON configuration (optional)
- `RequiresConfiguration`: If TRUE, tier must provide configuration when enabling
- `IsActive`: If FALSE, feature is hidden from admin UI
- `CreatedDate`: Timestamp of creation

### Step 2: Assign to Tiers

```sql
-- Assign the new feature to specific tiers
INSERT INTO "TierFeatures" (
    "SubscriptionTierId", 
    "FeatureId", 
    "IsEnabled", 
    "ConfigurationJson", 
    "CreatedByUserId", 
    "CreatedDate"
)
SELECT 
    5,                                   -- XL Tier ID
    f."Id",                             -- Feature ID (auto-lookup)
    TRUE,                               -- Enabled
    '{"maxRecipients": 500}',          -- Tier-specific config (overrides default)
    1,                                  -- Created by admin user
    NOW()                               -- Created date
FROM "Features" f
WHERE f."FeatureKey" = 'bulk_sms';
```

### Example: Adding Multiple Features at Once

```sql
-- Add multiple related features
INSERT INTO "Features" ("FeatureKey", "DisplayName", "Description", "Category", "IsActive", "CreatedDate")
VALUES 
    ('email_campaigns', 'Email Campaigns', 'Send email campaigns to farmers', 'Marketing', TRUE, NOW()),
    ('push_notifications', 'Push Notifications', 'Send mobile push notifications', 'Communication', TRUE, NOW()),
    ('whatsapp_messaging', 'WhatsApp Messaging', 'Send messages via WhatsApp', 'Communication', TRUE, NOW());

-- Assign all to XL tier
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "CreatedByUserId", "CreatedDate")
SELECT 5, f."Id", TRUE, 1, NOW()
FROM "Features" f
WHERE f."FeatureKey" IN ('email_campaigns', 'push_notifications', 'whatsapp_messaging');
```

---

## Creating New Tiers

### Step 1: Insert Tier Definition

```sql
-- Add a new subscription tier
INSERT INTO "SubscriptionTiers" (
    "TierName", 
    "DisplayName", 
    "Price", 
    "DurationDays", 
    "MaxRequests", 
    "Description", 
    "IsActive", 
    "CreatedDate"
)
VALUES (
    'XXL',                                        -- Short name (used in code)
    'Extra Extra Large',                          -- Display name
    999.99,                                       -- Monthly price
    30,                                           -- Duration in days
    10000,                                        -- Max API requests per month
    'Premium enterprise tier with all features',  -- Description
    TRUE,                                         -- Is active
    NOW()                                         -- Created date
);

-- Get the new tier ID
SELECT "Id", "TierName" FROM "SubscriptionTiers" WHERE "TierName" = 'XXL';
-- Example result: Id = 6
```

### Step 2: Assign Features to New Tier

```sql
-- Option A: Assign specific features
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "CreatedByUserId", "CreatedDate")
SELECT 6, f."Id", TRUE, 1, NOW()
FROM "Features" f
WHERE f."FeatureKey" IN (
    'messaging', 
    'voice_messages', 
    'smart_links', 
    'api_access', 
    'bulk_sms',
    'email_campaigns'
);

-- Option B: Assign ALL active features
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "CreatedByUserId", "CreatedDate")
SELECT 6, f."Id", TRUE, 1, NOW()
FROM "Features" f
WHERE f."IsActive" = TRUE 
  AND f."IsDeprecated" = FALSE;
```

### Step 3: Configure Tier-Specific Settings

```sql
-- Add custom configurations for specific features
UPDATE "TierFeatures" 
SET 
    "ConfigurationJson" = '{"maxRecipients": 1000}',
    "ModifiedDate" = NOW(),
    "ModifiedByUserId" = 1
WHERE "SubscriptionTierId" = 6 
  AND "FeatureId" = (SELECT "Id" FROM "Features" WHERE "FeatureKey" = 'bulk_sms');
```

---

## Assigning Features to Tiers

### Add Single Feature to Single Tier

```sql
-- Add messaging feature to Medium tier (if not already exists)
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "CreatedByUserId", "CreatedDate")
SELECT 3, f."Id", TRUE, 1, NOW()
FROM "Features" f
WHERE f."FeatureKey" = 'messaging'
  AND NOT EXISTS (
      SELECT 1 FROM "TierFeatures" 
      WHERE "SubscriptionTierId" = 3 
        AND "FeatureId" = f."Id"
  );
```

### Add Multiple Features to Single Tier

```sql
-- Add communication features to Large tier
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "CreatedByUserId", "CreatedDate")
SELECT 4, f."Id", TRUE, 1, NOW()
FROM "Features" f
WHERE f."FeatureKey" IN ('bulk_sms', 'email_campaigns', 'push_notifications')
  AND NOT EXISTS (
      SELECT 1 FROM "TierFeatures" 
      WHERE "SubscriptionTierId" = 4 
        AND "FeatureId" = f."Id"
  );
```

### Add Single Feature to Multiple Tiers

```sql
-- Add priority_support to both Large and XL tiers with different configs
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "ConfigurationJson", "CreatedByUserId", "CreatedDate")
SELECT 4, f."Id", TRUE, '{"responseTimeHours": 12}', 1, NOW()
FROM "Features" f
WHERE f."FeatureKey" = 'priority_support'
UNION ALL
SELECT 5, f."Id", TRUE, '{"responseTimeHours": 6}', 1, NOW()
FROM "Features" f
WHERE f."FeatureKey" = 'priority_support';
```

---

## Modifying Feature Assignments

### Enable/Disable Feature for Tier

```sql
-- Disable messaging for Medium tier (without deleting the record)
UPDATE "TierFeatures" 
SET 
    "IsEnabled" = FALSE,
    "ModifiedDate" = NOW(),
    "ModifiedByUserId" = 1
WHERE "SubscriptionTierId" = 3 
  AND "FeatureId" = (SELECT "Id" FROM "Features" WHERE "FeatureKey" = 'messaging');

-- Re-enable it later
UPDATE "TierFeatures" 
SET 
    "IsEnabled" = TRUE,
    "ModifiedDate" = NOW(),
    "ModifiedByUserId" = 1
WHERE "SubscriptionTierId" = 3 
  AND "FeatureId" = (SELECT "Id" FROM "Features" WHERE "FeatureKey" = 'messaging');
```

### Remove Feature from Tier

```sql
-- Permanently remove feature assignment (cannot be undone easily)
DELETE FROM "TierFeatures"
WHERE "SubscriptionTierId" = 3 
  AND "FeatureId" = (SELECT "Id" FROM "Features" WHERE "FeatureKey" = 'messaging');

-- Better approach: Disable instead of delete (see above)
```

### Deprecate Feature (All Tiers)

```sql
-- Mark feature as deprecated (still works but shows warning in admin UI)
UPDATE "Features" 
SET 
    "IsDeprecated" = TRUE,
    "ModifiedDate" = NOW()
WHERE "FeatureKey" = 'old_analytics';

-- Deactivate feature completely (hides from admin UI, still works if enabled)
UPDATE "Features" 
SET 
    "IsActive" = FALSE,
    "ModifiedDate" = NOW()
WHERE "FeatureKey" = 'legacy_feature';
```

---

## Feature Configuration Management

### Update Feature Configuration

```sql
-- Change configuration for a specific tier-feature mapping
UPDATE "TierFeatures" 
SET 
    "ConfigurationJson" = '{"maxRecipients": 300, "allowScheduling": true}',
    "ModifiedDate" = NOW(),
    "ModifiedByUserId" = 1
WHERE "SubscriptionTierId" = 4 
  AND "FeatureId" = (SELECT "Id" FROM "Features" WHERE "FeatureKey" = 'bulk_sms');
```

### Update Default Configuration

```sql
-- Change default configuration for all new tier assignments
UPDATE "Features" 
SET 
    "DefaultConfigJson" = '{"maxRecipients": 150}',
    "ModifiedDate" = NOW()
WHERE "FeatureKey" = 'bulk_sms';

-- Note: This does NOT affect existing tier-feature mappings, only new ones
```

### View Current Configurations

```sql
-- See all tier-specific configurations for a feature
SELECT 
    st."TierName",
    tf."ConfigurationJson" as "TierConfig",
    f."DefaultConfigJson" as "DefaultConfig",
    COALESCE(tf."ConfigurationJson", f."DefaultConfigJson") as "EffectiveConfig"
FROM "TierFeatures" tf
JOIN "SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"
JOIN "Features" f ON tf."FeatureId" = f."Id"
WHERE f."FeatureKey" = 'bulk_sms'
ORDER BY st."Id";
```

---

## Scheduled Features (Promotions)

### Add Time-Limited Feature

```sql
-- Black Friday Promotion: Give messaging to Small tier for 1 week
INSERT INTO "TierFeatures" (
    "SubscriptionTierId", 
    "FeatureId", 
    "IsEnabled", 
    "EffectiveDate", 
    "ExpiryDate", 
    "CreatedByUserId", 
    "CreatedDate"
)
SELECT 
    2,                                    -- Small tier
    f."Id",
    TRUE,
    '2025-11-25 00:00:00',              -- Black Friday start
    '2025-12-01 23:59:59',              -- Ends after 1 week
    1,
    NOW()
FROM "Features" f
WHERE f."FeatureKey" = 'messaging';
```

### A/B Testing Feature

```sql
-- Test new feature on XL tier only, starting next month
INSERT INTO "TierFeatures" (
    "SubscriptionTierId", 
    "FeatureId", 
    "IsEnabled", 
    "EffectiveDate", 
    "CreatedByUserId", 
    "CreatedDate"
)
SELECT 
    5,                                    -- XL tier only
    f."Id",
    TRUE,
    '2025-11-01 00:00:00',              -- Start date
    1,
    NOW()
FROM "Features" f
WHERE f."FeatureKey" = 'ai_recommendations';
```

### Check Active Scheduled Features

```sql
-- See all currently active scheduled features
SELECT 
    st."TierName",
    f."FeatureKey",
    f."DisplayName",
    tf."EffectiveDate",
    tf."ExpiryDate",
    CASE 
        WHEN tf."EffectiveDate" IS NULL AND tf."ExpiryDate" IS NULL THEN 'Permanent'
        WHEN NOW() < tf."EffectiveDate" THEN 'Scheduled'
        WHEN tf."ExpiryDate" IS NULL OR NOW() <= tf."ExpiryDate" THEN 'Active'
        ELSE 'Expired'
    END as "Status"
FROM "TierFeatures" tf
JOIN "SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"
JOIN "Features" f ON tf."FeatureId" = f."Id"
WHERE tf."IsEnabled" = TRUE
  AND (tf."EffectiveDate" IS NOT NULL OR tf."ExpiryDate" IS NOT NULL)
ORDER BY tf."EffectiveDate" DESC;
```

---

## Audit and Monitoring

### View All Features by Tier

```sql
SELECT 
    st."TierName",
    COUNT(tf."Id") as "FeatureCount",
    STRING_AGG(f."FeatureKey", ', ' ORDER BY f."FeatureKey") as "Features"
FROM "SubscriptionTiers" st
LEFT JOIN "TierFeatures" tf ON st."Id" = tf."SubscriptionTierId" AND tf."IsEnabled" = TRUE
LEFT JOIN "Features" f ON tf."FeatureId" = f."Id"
GROUP BY st."Id", st."TierName"
ORDER BY st."Id";
```

### View All Tiers for a Feature

```sql
SELECT 
    f."FeatureKey",
    f."DisplayName",
    STRING_AGG(st."TierName", ', ' ORDER BY st."Id") as "AvailableInTiers"
FROM "Features" f
LEFT JOIN "TierFeatures" tf ON f."Id" = tf."FeatureId" AND tf."IsEnabled" = TRUE
LEFT JOIN "SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"
GROUP BY f."Id", f."FeatureKey", f."DisplayName"
ORDER BY f."FeatureKey";
```

### Audit Trail

```sql
-- See who modified which feature assignments
SELECT 
    st."TierName",
    f."FeatureKey",
    tf."IsEnabled",
    tf."CreatedDate",
    u1."Name" as "CreatedBy",
    tf."ModifiedDate",
    u2."Name" as "ModifiedBy"
FROM "TierFeatures" tf
JOIN "SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"
JOIN "Features" f ON tf."FeatureId" = f."Id"
LEFT JOIN "Users" u1 ON tf."CreatedByUserId" = u1."Id"
LEFT JOIN "Users" u2 ON tf."ModifiedByUserId" = u2."Id"
WHERE tf."ModifiedDate" IS NOT NULL
ORDER BY tf."ModifiedDate" DESC
LIMIT 20;
```

### Find Unused Features

```sql
-- Features that are not assigned to any tier
SELECT 
    f."FeatureKey",
    f."DisplayName",
    f."IsActive",
    f."IsDeprecated"
FROM "Features" f
LEFT JOIN "TierFeatures" tf ON f."Id" = tf."FeatureId"
WHERE tf."Id" IS NULL
ORDER BY f."CreatedDate" DESC;
```

### Check Configuration Consistency

```sql
-- Find tier-features without required configuration
SELECT 
    st."TierName",
    f."FeatureKey",
    f."RequiresConfiguration",
    tf."ConfigurationJson",
    f."DefaultConfigJson"
FROM "TierFeatures" tf
JOIN "SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"
JOIN "Features" f ON tf."FeatureId" = f."Id"
WHERE f."RequiresConfiguration" = TRUE 
  AND tf."ConfigurationJson" IS NULL 
  AND f."DefaultConfigJson" IS NULL;
```

---

## Best Practices

### 1. Always Use Feature Keys, Not IDs

**Bad:**
```sql
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", ...)
VALUES (4, 7, ...);  -- What is feature 7?
```

**Good:**
```sql
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", ...)
SELECT 4, f."Id", ...
FROM "Features" f
WHERE f."FeatureKey" = 'bulk_sms';  -- Clear what feature we're adding
```

### 2. Disable Instead of Delete

**Bad:**
```sql
DELETE FROM "TierFeatures" WHERE ...;  -- Lost forever
```

**Good:**
```sql
UPDATE "TierFeatures" SET "IsEnabled" = FALSE WHERE ...;  -- Can re-enable
```

### 3. Always Set Audit Fields

```sql
INSERT INTO "TierFeatures" (..., "CreatedByUserId", "CreatedDate")
VALUES (..., 1, NOW());  -- Always track who and when

UPDATE "TierFeatures" 
SET ..., "ModifiedByUserId" = 1, "ModifiedDate" = NOW()
WHERE ...;  -- Always track modifications
```

### 4. Use Transactions for Multiple Changes

```sql
BEGIN;

-- Add new feature
INSERT INTO "Features" (...) VALUES (...);

-- Assign to multiple tiers
INSERT INTO "TierFeatures" (...) SELECT ...;
INSERT INTO "TierFeatures" (...) SELECT ...;

-- Verify before committing
SELECT COUNT(*) FROM "TierFeatures" WHERE ...;

COMMIT;  -- or ROLLBACK if something wrong
```

### 5. Test with NOT EXISTS Before Insert

```sql
-- Prevents duplicate key errors
INSERT INTO "TierFeatures" (...)
SELECT ...
FROM "Features" f
WHERE f."FeatureKey" = 'messaging'
  AND NOT EXISTS (
      SELECT 1 FROM "TierFeatures" 
      WHERE "SubscriptionTierId" = 3 AND "FeatureId" = f."Id"
  );
```

---

## Common Scenarios

### Scenario 1: Launch New Premium Feature

```sql
-- 1. Add the feature
INSERT INTO "Features" ("FeatureKey", "DisplayName", "Description", "Category", "IsActive", "CreatedDate")
VALUES ('ai_recommendations', 'AI Recommendations', 'ML-powered crop recommendations', 'Analytics', TRUE, NOW());

-- 2. Give it to XL tier only (premium)
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "CreatedByUserId", "CreatedDate")
SELECT 5, f."Id", TRUE, 1, NOW()
FROM "Features" f
WHERE f."FeatureKey" = 'ai_recommendations';
```

### Scenario 2: Upgrade All Medium Tier Users

```sql
-- Give Medium tier access to messaging (previously only L and XL)
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "CreatedByUserId", "CreatedDate")
SELECT 3, f."Id", TRUE, 1, NOW()
FROM "Features" f
WHERE f."FeatureKey" = 'messaging';
```

### Scenario 3: Holiday Promotion

```sql
-- Black Friday: Give Small tier users analytics for 1 week
INSERT INTO "TierFeatures" (
    "SubscriptionTierId", "FeatureId", "IsEnabled", 
    "EffectiveDate", "ExpiryDate", 
    "CreatedByUserId", "CreatedDate"
)
SELECT 
    2, f."Id", TRUE, 
    '2025-11-25 00:00:00', '2025-12-01 23:59:59',
    1, NOW()
FROM "Features" f
WHERE f."FeatureKey" = 'advanced_analytics';
```

### Scenario 4: Deprecate Old Feature

```sql
-- 1. Mark as deprecated
UPDATE "Features" 
SET "IsDeprecated" = TRUE, "ModifiedDate" = NOW()
WHERE "FeatureKey" = 'legacy_reports';

-- 2. Later, disable it for all tiers
UPDATE "TierFeatures" 
SET "IsEnabled" = FALSE, "ModifiedDate" = NOW(), "ModifiedByUserId" = 1
WHERE "FeatureId" = (SELECT "Id" FROM "Features" WHERE "FeatureKey" = 'legacy_reports');

-- 3. Eventually, deactivate completely
UPDATE "Features" 
SET "IsActive" = FALSE, "ModifiedDate" = NOW()
WHERE "FeatureKey" = 'legacy_reports';
```

---

## Quick Reference

| Task | SQL Template |
|------|--------------|
| Add feature | `INSERT INTO "Features" (...) VALUES (...)` |
| Assign to tier | `INSERT INTO "TierFeatures" (...) SELECT ... FROM "Features" WHERE "FeatureKey" = '...'` |
| Disable feature | `UPDATE "TierFeatures" SET "IsEnabled" = FALSE WHERE ...` |
| Update config | `UPDATE "TierFeatures" SET "ConfigurationJson" = '...' WHERE ...` |
| Schedule feature | `INSERT ... SET "EffectiveDate" = '...', "ExpiryDate" = '...'` |
| View tier features | `SELECT ... FROM "TierFeatures" JOIN "Features" WHERE "SubscriptionTierId" = ...` |

---

**Last Updated:** 2025-10-26  
**Related Files:**
- Migration: `DataAccess/Migrations/Pg/Manual/AddTierFeatureManagementSystem.sql`
- Service: `Business/Services/Subscription/TierFeatureService.cs`
- Guide: `claudedocs/TIER_PERMISSION_MANAGEMENT_COMPLETE_GUIDE.md`
