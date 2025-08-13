-- ZiraAI Subscription System Database Setup

-- 1. Create SubscriptionTiers table
CREATE TABLE IF NOT EXISTS "SubscriptionTiers" (
    "Id" SERIAL PRIMARY KEY,
    "TierName" VARCHAR(10) NOT NULL,
    "DisplayName" VARCHAR(50) NOT NULL,
    "Description" TEXT,
    "DailyRequestLimit" INTEGER NOT NULL,
    "MonthlyRequestLimit" INTEGER NOT NULL,
    "MonthlyPrice" DECIMAL(10,2) NOT NULL,
    "YearlyPrice" DECIMAL(10,2),
    "Currency" VARCHAR(3) DEFAULT 'TRY',
    "PrioritySupport" BOOLEAN DEFAULT FALSE,
    "AdvancedAnalytics" BOOLEAN DEFAULT FALSE,
    "ApiAccess" BOOLEAN DEFAULT TRUE,
    "ResponseTimeHours" INTEGER DEFAULT 48,
    "AdditionalFeatures" TEXT DEFAULT '[]',
    "IsActive" BOOLEAN DEFAULT TRUE,
    "DisplayOrder" INTEGER DEFAULT 0,
    "CreatedDate" TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    "UpdatedDate" TIMESTAMPTZ,
    "CreatedUserId" INTEGER,
    "UpdatedUserId" INTEGER
);

-- 2. Create UserSubscriptions table
CREATE TABLE IF NOT EXISTS "UserSubscriptions" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "SubscriptionTierId" INTEGER NOT NULL,
    "StartDate" TIMESTAMPTZ NOT NULL,
    "EndDate" TIMESTAMPTZ NOT NULL,
    "IsActive" BOOLEAN DEFAULT TRUE,
    "AutoRenew" BOOLEAN DEFAULT FALSE,
    
    -- Payment Information
    "PaymentMethod" VARCHAR(50),
    "PaymentReference" VARCHAR(100),
    "PaidAmount" DECIMAL(10,2),
    "Currency" VARCHAR(3) DEFAULT 'TRY',
    "LastPaymentDate" TIMESTAMPTZ,
    "NextPaymentDate" TIMESTAMPTZ,
    
    -- Usage Tracking
    "CurrentDailyUsage" INTEGER DEFAULT 0,
    "CurrentMonthlyUsage" INTEGER DEFAULT 0,
    "LastUsageResetDate" TIMESTAMPTZ,
    "MonthlyUsageResetDate" TIMESTAMPTZ,
    
    -- Status Management
    "Status" VARCHAR(20) DEFAULT 'Active',
    "CancellationReason" TEXT,
    "CancellationDate" TIMESTAMPTZ,
    
    -- Trial Support
    "IsTrialSubscription" BOOLEAN DEFAULT FALSE,
    "TrialEndDate" TIMESTAMPTZ,
    
    -- Audit fields
    "CreatedDate" TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    "UpdatedDate" TIMESTAMPTZ,
    "CreatedUserId" INTEGER,
    "UpdatedUserId" INTEGER,
    
    FOREIGN KEY ("UserId") REFERENCES "Users"("UserId"),
    FOREIGN KEY ("SubscriptionTierId") REFERENCES "SubscriptionTiers"("Id")
);

-- 3. Create SubscriptionUsageLogs table
CREATE TABLE IF NOT EXISTS "SubscriptionUsageLogs" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "UserSubscriptionId" INTEGER NOT NULL,
    "PlantAnalysisId" INTEGER,
    "UsageType" VARCHAR(50) NOT NULL,
    "UsageDate" TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    "RequestEndpoint" VARCHAR(200),
    "RequestMethod" VARCHAR(10),
    "IsSuccessful" BOOLEAN DEFAULT TRUE,
    "ResponseStatus" INTEGER,
    "ErrorMessage" TEXT,
    "ResponseTimeMs" BIGINT,
    
    -- Quota Snapshots
    "DailyQuotaUsed" INTEGER,
    "DailyQuotaLimit" INTEGER,
    "MonthlyQuotaUsed" INTEGER,
    "MonthlyQuotaLimit" INTEGER,
    
    -- Request Details
    "IpAddress" VARCHAR(45),
    "UserAgent" TEXT,
    
    FOREIGN KEY ("UserId") REFERENCES "Users"("UserId"),
    FOREIGN KEY ("UserSubscriptionId") REFERENCES "UserSubscriptions"("Id"),
    FOREIGN KEY ("PlantAnalysisId") REFERENCES "PlantAnalyses"("Id")
);

-- 4. Create indexes for performance
CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_UserId" ON "UserSubscriptions"("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_SubscriptionTierId" ON "UserSubscriptions"("SubscriptionTierId");
CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_IsActive_Status" ON "UserSubscriptions"("IsActive", "Status");
CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_EndDate" ON "UserSubscriptions"("EndDate");

CREATE INDEX IF NOT EXISTS "IX_SubscriptionUsageLogs_UserId" ON "SubscriptionUsageLogs"("UserId");
CREATE INDEX IF NOT EXISTS "IX_SubscriptionUsageLogs_UserSubscriptionId" ON "SubscriptionUsageLogs"("UserSubscriptionId");
CREATE INDEX IF NOT EXISTS "IX_SubscriptionUsageLogs_UsageDate" ON "SubscriptionUsageLogs"("UsageDate");
CREATE INDEX IF NOT EXISTS "IX_SubscriptionUsageLogs_UsageType" ON "SubscriptionUsageLogs"("UsageType");

CREATE INDEX IF NOT EXISTS "IX_SubscriptionTiers_TierName" ON "SubscriptionTiers"("TierName");
CREATE INDEX IF NOT EXISTS "IX_SubscriptionTiers_IsActive" ON "SubscriptionTiers"("IsActive");

-- 5. Insert Subscription Tiers (S, M, L, XL)
INSERT INTO "SubscriptionTiers" (
    "TierName", "DisplayName", "Description", 
    "DailyRequestLimit", "MonthlyRequestLimit", 
    "MonthlyPrice", "YearlyPrice", "Currency",
    "PrioritySupport", "AdvancedAnalytics", "ApiAccess", 
    "ResponseTimeHours", "AdditionalFeatures", 
    "IsActive", "DisplayOrder"
) VALUES 
-- S Tier
('S', 'Small', 'Perfect for small farms and hobby gardeners', 
 5, 50, 99.99, 1079.99, 'TRY',
 false, false, true, 
 48, '["Basic email support", "Standard analysis reports", "Mobile app access"]', 
 true, 1),

-- M Tier  
('M', 'Medium', 'Ideal for professional farmers and medium operations', 
 20, 200, 299.99, 3239.99, 'TRY',
 true, false, true, 
 24, '["Priority email support", "Weekly usage reports", "Phone support"]', 
 true, 2),

-- L Tier
('L', 'Large', 'Designed for large farms and agricultural companies', 
 50, 500, 599.99, 6479.99, 'TRY',
 true, true, true, 
 12, '["Dedicated support", "Custom reports", "API webhooks", "Advanced analytics dashboard"]', 
 true, 3),

-- XL Tier
('XL', 'Extra Large', 'Enterprise solution for research institutions and large organizations', 
 200, 2000, 1499.99, 16199.99, 'TRY',
 true, true, true, 
 4, '["Dedicated account manager", "Custom integration", "SLA guarantee", "Priority processing", "White-label options"]', 
 true, 4)

ON CONFLICT ("TierName") DO NOTHING;

-- 6. Insert/Update Groups (Roles)
INSERT INTO "Groups" ("GroupName") VALUES 
('Admin'),
('Farmer'),
('Sponsor')
ON CONFLICT ("GroupName") DO NOTHING;

-- 7. Insert Operation Claims for Subscription System
INSERT INTO "OperationClaims" ("Name", "Alias", "Description") VALUES
('SubscriptionManagement', 'subscription.management', 'Manage subscription tiers and user subscriptions'),
('SubscriptionView', 'subscription.view', 'View subscription information and usage'),
('SubscriptionAnalytics', 'subscription.analytics', 'Access subscription usage analytics and reports'),
('PlantAnalysisCreate', 'plant.analysis.create', 'Create plant analysis requests'),
('PlantAnalysisView', 'plant.analysis.view', 'View plant analysis results'),
('AdminPanel', 'admin.panel', 'Access admin panel and system management')
ON CONFLICT ("Name") DO NOTHING;

-- 8. Assign claims to groups
-- Admin gets all permissions
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT g."Id", oc."Id"
FROM "Groups" g
CROSS JOIN "OperationClaims" oc
WHERE g."GroupName" = 'Admin'
ON CONFLICT ("GroupId", "ClaimId") DO NOTHING;

-- Farmer gets plant analysis and subscription view permissions
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT g."Id", oc."Id"
FROM "Groups" g
CROSS JOIN "OperationClaims" oc
WHERE g."GroupName" = 'Farmer' 
AND oc."Name" IN ('PlantAnalysisCreate', 'PlantAnalysisView', 'SubscriptionView')
ON CONFLICT ("GroupId", "ClaimId") DO NOTHING;

-- Sponsor gets view permissions
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT g."Id", oc."Id"
FROM "Groups" g
CROSS JOIN "OperationClaims" oc
WHERE g."GroupName" = 'Sponsor' 
AND oc."Name" IN ('PlantAnalysisView', 'SubscriptionView')
ON CONFLICT ("GroupId", "ClaimId") DO NOTHING;

-- Success message
SELECT 'ZiraAI Subscription System database setup completed successfully!' as message;