# Adding New Features to Tier System - Complete Guide

**Date:** 2025-10-27
**Purpose:** Step-by-step guide for adding new features and assigning them to subscription tiers

---

## Example Scenario

**Feature Name:** Custom Reports (custom_reports)
**Target Tier:** XL (Extra Large)
**Description:** Advanced custom report generation and scheduling

---

## Prerequisites

Before adding a new feature, ensure you have:

1. Clear feature requirements and description
2. Target tier(s) identified
3. Configuration requirements (if any)
4. Database access to execute SQL scripts
5. Understanding of existing feature structure from `Feature.txt`

---

## Step-by-Step Process

### Step 1: Define Feature Metadata

First, gather all required information:

```
FeatureKey: custom_reports
DisplayName: Custom Reports
Description: Advanced custom report generation and scheduling with export options
Category: Analytics (optional field - may be NULL)
DefaultConfigJson: {"maxReports": 10, "allowScheduling": true, "exportFormats": ["PDF", "Excel"]}
RequiresConfiguration: true
IsActive: true
IsDeprecated: false
```

### Step 2: Check Current Feature IDs

Before inserting, verify the next available Feature ID:

```sql
-- Check current max ID in Features table
SELECT MAX("Id") as "MaxId" FROM "Features";

-- Expected result: 7 (after COMPLETE_TIER_FEATURE_RESET.sql)
-- Next available ID: 8
```

### Step 3: Insert New Feature

```sql
-- Insert the new feature
INSERT INTO "Features"
  ("FeatureKey", "DisplayName", "Description", "Category", "DefaultConfigJson",
   "RequiresConfiguration", "IsActive", "IsDeprecated", "CreatedDate", "CreatedByUserId")
VALUES
  (
    'custom_reports',
    'Custom Reports',
    'Advanced custom report generation and scheduling with export options',
    NULL,  -- Category is optional
    '{"maxReports": 10, "allowScheduling": true, "exportFormats": ["PDF", "Excel"]}',
    true,  -- Requires configuration
    true,  -- Is active
    false, -- Not deprecated
    NOW(),
    1      -- System user ID
  )
RETURNING "Id";

-- Expected result: Id = 8
```

### Step 4: Verify Feature Insertion

```sql
-- Verify the feature was created correctly
SELECT
  "Id",
  "FeatureKey",
  "DisplayName",
  "RequiresConfiguration",
  "IsActive",
  "DefaultConfigJson"
FROM "Features"
WHERE "FeatureKey" = 'custom_reports';
```

**Expected Result:**
```
Id | FeatureKey      | DisplayName     | RequiresConfiguration | IsActive | DefaultConfigJson
---|-----------------|-----------------|----------------------|----------|------------------
8  | custom_reports  | Custom Reports  | true                 | true     | {"maxReports": 10, ...}
```

### Step 5: Assign Feature to Tier (XL)

```sql
-- XL tier has SubscriptionTierId = 4
-- Feature ID = 8 (from step 3)
-- Configuration: Override default with XL-specific settings

INSERT INTO "TierFeatures"
  ("SubscriptionTierId", "FeatureId", "IsEnabled", "ConfigurationJson",
   "CreatedDate", "UpdatedDate", "CreatedByUserId", "ModifiedByUserId")
VALUES
  (
    4,     -- XL tier
    8,     -- custom_reports feature
    true,  -- Enabled
    '{"maxReports": 50, "allowScheduling": true, "exportFormats": ["PDF", "Excel", "CSV", "JSON"], "advancedFilters": true}',
    NOW(),
    NULL,  -- UpdatedDate starts as NULL
    1,     -- Created by system user
    NULL   -- ModifiedByUserId starts as NULL
  );
```

### Step 6: Verify TierFeature Assignment

```sql
-- Verify the tier-feature mapping
SELECT
  tf."Id" as "TierFeatureId",
  st."TierName",
  st."DisplayName" as "TierDisplayName",
  f."FeatureKey",
  f."DisplayName" as "FeatureDisplayName",
  tf."IsEnabled",
  tf."ConfigurationJson"
FROM "TierFeatures" tf
JOIN "SubscriptionTiers" st ON st."Id" = tf."SubscriptionTierId"
JOIN "Features" f ON f."Id" = tf."FeatureId"
WHERE f."FeatureKey" = 'custom_reports';
```

**Expected Result:**
```
TierFeatureId | TierName | TierDisplayName | FeatureKey      | FeatureDisplayName | IsEnabled | ConfigurationJson
--------------|----------|-----------------|-----------------|-------------------|-----------|------------------
20            | XL       | Extra Large     | custom_reports  | Custom Reports    | true      | {"maxReports": 50, ...}
```

### Step 7: Test Feature Access

```sql
-- Test if XL tier has access to custom_reports
SELECT
  st."TierName",
  f."FeatureKey",
  tf."IsEnabled",
  tf."ConfigurationJson"
FROM "SubscriptionTiers" st
LEFT JOIN "TierFeatures" tf ON tf."SubscriptionTierId" = st."Id"
LEFT JOIN "Features" f ON f."Id" = tf."FeatureId"
WHERE st."Id" = 4 AND f."FeatureKey" = 'custom_reports';
```

### Step 8: Clear Application Cache

**IMPORTANT:** The TierFeatureService uses 15-minute caching.

After database changes:
1. Wait 15 minutes for cache to expire, OR
2. Restart the application to clear cache immediately

```bash
# Restart the application
dotnet run --project ./WebAPI/WebAPI.csproj
```

---

## Complete SQL Script Template

Here is a complete script you can adapt for any new feature:

```sql
-- ============================================================================
-- ADD NEW FEATURE TEMPLATE
-- ============================================================================
-- Feature: custom_reports
-- Target Tier: XL (SubscriptionTierId = 4)
-- Date: 2025-10-27
-- ============================================================================

BEGIN;

-- Step 1: Insert Feature
INSERT INTO "Features"
  ("FeatureKey", "DisplayName", "Description", "Category", "DefaultConfigJson",
   "RequiresConfiguration", "IsActive", "IsDeprecated", "CreatedDate", "CreatedByUserId")
VALUES
  (
    'custom_reports',
    'Custom Reports',
    'Advanced custom report generation and scheduling with export options',
    NULL,
    '{"maxReports": 10, "allowScheduling": true, "exportFormats": ["PDF", "Excel"]}',
    true,
    true,
    false,
    NOW(),
    1
  )
RETURNING "Id";

-- Note the returned ID (should be 8 if following COMPLETE_TIER_FEATURE_RESET.sql)

-- Step 2: Assign to XL Tier
INSERT INTO "TierFeatures"
  ("SubscriptionTierId", "FeatureId", "IsEnabled", "ConfigurationJson",
   "CreatedDate", "CreatedByUserId")
VALUES
  (
    4,  -- XL tier
    8,  -- Use the ID from Step 1
    true,
    '{"maxReports": 50, "allowScheduling": true, "exportFormats": ["PDF", "Excel", "CSV", "JSON"], "advancedFilters": true}',
    NOW(),
    1
  );

-- Step 3: Verify
SELECT
  st."TierName",
  f."FeatureKey",
  f."DisplayName",
  tf."IsEnabled",
  tf."ConfigurationJson"
FROM "TierFeatures" tf
JOIN "SubscriptionTiers" st ON st."Id" = tf."SubscriptionTierId"
JOIN "Features" f ON f."Id" = tf."FeatureId"
WHERE f."FeatureKey" = 'custom_reports';

COMMIT;
```

---

## Configuration JSON Guidelines

### For Features with RequiresConfiguration = true

**DefaultConfigJson** in Features table:
- Defines default/base configuration
- Used as fallback if TierFeatures has NULL configuration
- Should be valid JSON with sensible defaults

**ConfigurationJson** in TierFeatures table:
- Overrides or extends DefaultConfigJson for specific tier
- Can be NULL (uses default from Features table)
- Tier-specific customization

### Common Configuration Patterns

#### Numeric Limits
```json
{
  "maxItems": 100,
  "maxFileSize": 5242880,
  "dailyLimit": 1000
}
```

#### Boolean Toggles
```json
{
  "allowExport": true,
  "enableAdvancedFilters": true,
  "showBranding": false
}
```

#### Arrays
```json
{
  "supportedFormats": ["PDF", "Excel", "CSV"],
  "allowedActions": ["create", "read", "update", "delete"]
}
```

#### Nested Objects
```json
{
  "limits": {
    "maxReports": 50,
    "maxScheduled": 10
  },
  "features": {
    "advancedFilters": true,
    "customTemplates": true
  }
}
```

---

## Validation Checklist

After adding a new feature, verify:

- [ ] Feature record exists in Features table with correct metadata
- [ ] Feature ID is sequential and matches expectations
- [ ] TierFeatures mapping exists for target tier(s)
- [ ] ConfigurationJson is valid JSON (not malformed)
- [ ] Foreign key constraints are satisfied
- [ ] Unique constraint (TierId + FeatureId) is not violated
- [ ] Application cache is cleared or expired
- [ ] Feature access API returns correct result
- [ ] Documentation is updated

---

## Testing the New Feature

### Backend Code Example

```csharp
// In your service or handler
public async Task<bool> CanGenerateCustomReports(int plantAnalysisId)
{
    // Get the analysis
    var analysis = await _plantAnalysisRepository.GetAsync(
        a => a.Id == plantAnalysisId);

    if (analysis == null || !analysis.ActiveSponsorshipId.HasValue)
        return false;

    // Get the subscription tier
    var subscription = await _userSubscriptionRepository.GetAsync(
        us => us.Id == analysis.ActiveSponsorshipId.Value);

    if (subscription == null)
        return false;

    // Check feature access
    var hasAccess = await _tierFeatureService.HasFeatureAccessAsync(
        subscription.SubscriptionTierId,
        "custom_reports");

    return hasAccess;
}

// Get configuration for the feature
public async Task<dynamic> GetCustomReportsConfig(int tierId)
{
    var config = await _tierFeatureService.GetFeatureConfigurationAsync(
        tierId,
        "custom_reports");

    return config;
}
```

### SQL Testing Query

```sql
-- Test feature access for a specific analysis
SELECT
    pa."Id" as "AnalysisId",
    us."SubscriptionTierId",
    st."TierName",
    f."FeatureKey",
    tf."IsEnabled" as "HasCustomReports",
    tf."ConfigurationJson"
FROM "PlantAnalyses" pa
JOIN "UserSubscriptions" us ON us."Id" = pa."ActiveSponsorshipId"
JOIN "SubscriptionTiers" st ON st."Id" = us."SubscriptionTierId"
LEFT JOIN "TierFeatures" tf ON tf."SubscriptionTierId" = us."SubscriptionTierId"
LEFT JOIN "Features" f ON f."Id" = tf."FeatureId" AND f."FeatureKey" = 'custom_reports'
WHERE pa."Id" = YOUR_ANALYSIS_ID;
```

---

## Common Mistakes to Avoid

1. **Forgetting to use quoted identifiers**: Always use "TableName" and "ColumnName" in PostgreSQL
2. **Incorrect Feature ID**: Always verify the next available ID before inserting
3. **Invalid JSON**: Test ConfigurationJson validity before inserting
4. **Wrong Tier ID**: Verify SubscriptionTier IDs (1=S, 2=M, 3=L, 4=XL, 5=Trial)
5. **Duplicate mappings**: Unique constraint prevents same tier+feature combination
6. **Cache not cleared**: Changes not visible until cache expires or app restarts
7. **NULL vs empty JSON**: Use NULL if no configuration needed, not empty object
8. **Hardcoded IDs in code**: Use FeatureKey string lookups, not Feature.Id

---

## Multiple Tier Assignment Example

If you want to assign the same feature to multiple tiers (e.g., L and XL):

```sql
-- Assign custom_reports to both L and XL tiers
INSERT INTO "TierFeatures"
  ("SubscriptionTierId", "FeatureId", "IsEnabled", "ConfigurationJson",
   "CreatedDate", "CreatedByUserId")
VALUES
  -- L tier: Limited custom reports
  (
    3,  -- L tier
    8,  -- custom_reports
    true,
    '{"maxReports": 10, "allowScheduling": false, "exportFormats": ["PDF"]}',
    NOW(),
    1
  ),
  -- XL tier: Full custom reports
  (
    4,  -- XL tier
    8,  -- custom_reports
    true,
    '{"maxReports": 50, "allowScheduling": true, "exportFormats": ["PDF", "Excel", "CSV", "JSON"], "advancedFilters": true}',
    NOW(),
    1
  );
```

---

## Summary Reference

### Quick Command Sequence

```sql
-- 1. Check next ID
SELECT MAX("Id") FROM "Features";

-- 2. Insert feature
INSERT INTO "Features" (...) VALUES (...) RETURNING "Id";

-- 3. Assign to tier
INSERT INTO "TierFeatures" (...) VALUES (...);

-- 4. Verify
SELECT * FROM "TierFeatures" tf
JOIN "Features" f ON f."Id" = tf."FeatureId"
WHERE f."FeatureKey" = 'your_feature_key';
```

### Tier ID Reference

| Tier Name | Tier ID | Typical Use Case |
|-----------|---------|------------------|
| Trial     | 5       | Free trial users |
| S (Small) | 1       | Hobby/small farms |
| M (Medium)| 2       | Professional farmers with visibility |
| L (Large) | 3       | Large farms with full communication |
| XL (Extra Large) | 4 | Enterprise with all features |

---

**Last Updated:** 2025-10-27
**Related Files:**
- `COMPLETE_TIER_FEATURE_RESET.sql` - Base tier-feature setup
- `TIER_FEATURES_FINAL_DOCUMENTATION.md` - Complete feature matrix
- `Feature.txt` - DDL and table structures
