-- Migration: Add Tier Feature Management System
-- Date: 2025-10-26
-- Description: Database-driven tier-based feature permissions
-- Replaces hard-coded tier checks with configurable Feature and TierFeature tables

-- =====================================================
-- 1. CREATE FEATURES TABLE
-- =====================================================
CREATE TABLE "Features" (
    "Id" SERIAL PRIMARY KEY,
    "FeatureKey" VARCHAR(100) NOT NULL,
    "DisplayName" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(1000),
    "DefaultConfigJson" VARCHAR(2000),
    "RequiresConfiguration" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "IsDeprecated" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMP,
    CONSTRAINT "UQ_Features_FeatureKey" UNIQUE ("FeatureKey")
);

CREATE INDEX "IX_Features_FeatureKey" ON "Features" ("FeatureKey");
CREATE INDEX "IX_Features_IsActive" ON "Features" ("IsActive");

-- =====================================================
-- 2. CREATE TIERFEATURES TABLE
-- =====================================================
CREATE TABLE "TierFeatures" (
    "Id" SERIAL PRIMARY KEY,
    "SubscriptionTierId" INTEGER NOT NULL,
    "FeatureId" INTEGER NOT NULL,
    "IsEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "ConfigurationJson" VARCHAR(2000),
    "EffectiveDate" TIMESTAMP,
    "ExpiryDate" TIMESTAMP,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMP,
    "CreatedByUserId" INTEGER NOT NULL,
    "ModifiedByUserId" INTEGER,
    CONSTRAINT "FK_TierFeatures_SubscriptionTiers" FOREIGN KEY ("SubscriptionTierId") 
        REFERENCES "SubscriptionTiers"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TierFeatures_Features" FOREIGN KEY ("FeatureId") 
        REFERENCES "Features"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_TierFeatures_TierId_FeatureId" UNIQUE ("SubscriptionTierId", "FeatureId")
);

CREATE INDEX "IX_TierFeatures_SubscriptionTierId" ON "TierFeatures" ("SubscriptionTierId");
CREATE INDEX "IX_TierFeatures_FeatureId" ON "TierFeatures" ("FeatureId");
CREATE INDEX "IX_TierFeatures_IsEnabled" ON "TierFeatures" ("IsEnabled");

-- =====================================================
-- 3. SEED FEATURES DATA
-- =====================================================

-- Feature 1: Messaging
INSERT INTO "Features" ("FeatureKey", "DisplayName", "Description", "RequiresConfiguration", "IsActive", "CreatedDate")
VALUES ('messaging', 'Messaging', 'Allows sponsors to send messages to farmers', FALSE, TRUE, NOW());

-- Feature 2: Voice Messages
INSERT INTO "Features" ("FeatureKey", "DisplayName", "Description", "RequiresConfiguration", "IsActive", "CreatedDate")
VALUES ('voice_messages', 'Voice Messages', 'Allows sponsors to send voice messages to farmers', FALSE, TRUE, NOW());

-- Feature 3: Smart Links
INSERT INTO "Features" ("FeatureKey", "DisplayName", "Description", "RequiresConfiguration", "IsActive", "CreatedDate")
VALUES ('smart_links', 'Smart Links', 'Advanced smart link management and analytics', FALSE, TRUE, NOW());

-- Feature 4: Advanced Analytics
INSERT INTO "Features" ("FeatureKey", "DisplayName", "Description", "RequiresConfiguration", "IsActive", "CreatedDate")
VALUES ('advanced_analytics', 'Advanced Analytics', 'Access to detailed analytics and reports', FALSE, TRUE, NOW());

-- Feature 5: API Access
INSERT INTO "Features" ("FeatureKey", "DisplayName", "Description", "RequiresConfiguration", "IsActive", "CreatedDate")
VALUES ('api_access', 'API Access', 'Access to REST API endpoints for integration', FALSE, TRUE, NOW());

-- Feature 6: Sponsor Visibility
INSERT INTO "Features" ("FeatureKey", "DisplayName", "Description", "DefaultConfigJson", "RequiresConfiguration", "IsActive", "CreatedDate")
VALUES ('sponsor_visibility', 'Sponsor Visibility', 'Profile and logo visibility to farmers', '{"showLogo": false, "showProfile": false}', TRUE, TRUE, NOW());

-- Feature 7: Data Access Percentage
INSERT INTO "Features" ("FeatureKey", "DisplayName", "Description", "DefaultConfigJson", "RequiresConfiguration", "IsActive", "CreatedDate")
VALUES ('data_access_percentage', 'Data Access Percentage', 'Percentage of farmer data accessible to sponsor', '{"percentage": 0}', TRUE, TRUE, NOW());

-- Feature 8: Priority Support
INSERT INTO "Features" ("FeatureKey", "DisplayName", "Description", "DefaultConfigJson", "RequiresConfiguration", "IsActive", "CreatedDate")
VALUES ('priority_support', 'Priority Support', 'Priority customer support with guaranteed response time', '{"responseTimeHours": 24}', TRUE, TRUE, NOW());

-- =====================================================
-- 4. SEED TIER-FEATURE MAPPINGS
-- =====================================================

-- TRIAL TIER (ID=1): No features

-- SMALL TIER (ID=2): Only data access 30%
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "ConfigurationJson", "CreatedByUserId", "CreatedDate")
VALUES (2, 7, TRUE, '{"percentage": 30}', 1, NOW());

-- MEDIUM TIER (ID=3): Advanced analytics, logo visibility, data access 60%
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "CreatedByUserId", "CreatedDate")
VALUES
    (3, 4, TRUE, 1, NOW()); -- advanced_analytics

INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "ConfigurationJson", "CreatedByUserId", "CreatedDate")
VALUES
    (3, 6, TRUE, '{"showLogo": true, "showProfile": false}', 1, NOW()), -- sponsor_visibility (logo only)
    (3, 7, TRUE, '{"percentage": 60}', 1, NOW()); -- data_access_percentage 60%

-- LARGE TIER (ID=4): Messaging, analytics, API, full visibility, data 100%, priority support 12h
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "CreatedByUserId", "CreatedDate")
VALUES 
    (4, 1, TRUE, 1, NOW()), -- messaging
    (4, 4, TRUE, 1, NOW()), -- advanced_analytics
    (4, 5, TRUE, 1, NOW()); -- api_access

INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "ConfigurationJson", "CreatedByUserId", "CreatedDate")
VALUES 
    (4, 6, TRUE, '{"showLogo": true, "showProfile": true}', 1, NOW()), -- sponsor_visibility (full)
    (4, 7, TRUE, '{"percentage": 100}', 1, NOW()), -- data_access_percentage 100%
    (4, 8, TRUE, '{"responseTimeHours": 12}', 1, NOW()); -- priority_support 12h

-- EXTRA LARGE TIER (ID=5): All features including voice messages and smart links
INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "CreatedByUserId", "CreatedDate")
VALUES 
    (5, 1, TRUE, 1, NOW()), -- messaging
    (5, 2, TRUE, 1, NOW()), -- voice_messages
    (5, 3, TRUE, 1, NOW()), -- smart_links
    (5, 4, TRUE, 1, NOW()), -- advanced_analytics
    (5, 5, TRUE, 1, NOW()); -- api_access

INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "ConfigurationJson", "CreatedByUserId", "CreatedDate")
VALUES 
    (5, 6, TRUE, '{"showLogo": true, "showProfile": true}', 1, NOW()), -- sponsor_visibility (full)
    (5, 7, TRUE, '{"percentage": 100}', 1, NOW()), -- data_access_percentage 100%
    (5, 8, TRUE, '{"responseTimeHours": 6}', 1, NOW()); -- priority_support 6h

-- =====================================================
-- 5. VERIFICATION QUERIES
-- =====================================================

-- Verify features created
SELECT * FROM "Features" ORDER BY "Id";

-- Verify tier-feature mappings
SELECT 
    tf."Id",
    st."TierName",
    f."FeatureKey",
    tf."IsEnabled",
    tf."ConfigurationJson"
FROM "TierFeatures" tf
JOIN "SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"
JOIN "Features" f ON tf."FeatureId" = f."Id"
ORDER BY tf."SubscriptionTierId", f."FeatureKey";

-- Count features per tier
SELECT 
    st."TierName",
    COUNT(tf."Id") as "FeatureCount"
FROM "SubscriptionTiers" st
LEFT JOIN "TierFeatures" tf ON st."Id" = tf."SubscriptionTierId" AND tf."IsEnabled" = TRUE
GROUP BY st."Id", st."TierName"
ORDER BY st."Id";
