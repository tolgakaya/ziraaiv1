-- Insert Trial subscription tier directly into staging database if it doesn't exist
DO $$
BEGIN
    -- Add Trial tier if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM "SubscriptionTiers" WHERE "TierName" = 'Trial') THEN
        INSERT INTO "SubscriptionTiers" (
            "TierName", "DisplayName", "Description", 
            "DailyRequestLimit", "MonthlyRequestLimit", 
            "MonthlyPrice", "YearlyPrice", "Currency",
            "PrioritySupport", "AdvancedAnalytics", "ApiAccess", 
            "ResponseTimeHours", "AdditionalFeatures",
            "IsActive", "DisplayOrder", "CreatedDate"
        ) VALUES (
            'Trial', 'Trial', '30-day trial with limited access',
            1, 30,
            0.00, 0.00, 'TRY',
            false, false, false,
            72, '["Basic plant analysis","Email notifications","Trial access"]',
            true, 0, '2025-08-13 16:00:00'
        );
        RAISE NOTICE 'Trial tier added successfully';
    ELSE
        RAISE NOTICE 'Trial tier already exists';
    END IF;

    -- Add S tier if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM "SubscriptionTiers" WHERE "TierName" = 'S') THEN
        INSERT INTO "SubscriptionTiers" (
            "TierName", "DisplayName", "Description", 
            "DailyRequestLimit", "MonthlyRequestLimit", 
            "MonthlyPrice", "YearlyPrice", "Currency",
            "PrioritySupport", "AdvancedAnalytics", "ApiAccess", 
            "ResponseTimeHours", "AdditionalFeatures",
            "IsActive", "DisplayOrder", "CreatedDate"
        ) VALUES (
            'S', 'Small', 'Perfect for small farms and hobbyists',
            5, 50,
            99.99, 999.99, 'TRY',
            false, false, false,
            48, '["Basic plant analysis","Email notifications","Basic reports"]',
            true, 1, '2025-08-13 16:00:00'
        );
        RAISE NOTICE 'S tier added successfully';
    ELSE
        RAISE NOTICE 'S tier already exists';
    END IF;

    -- Add M tier if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM "SubscriptionTiers" WHERE "TierName" = 'M') THEN
        INSERT INTO "SubscriptionTiers" (
            "TierName", "DisplayName", "Description", 
            "DailyRequestLimit", "MonthlyRequestLimit", 
            "MonthlyPrice", "YearlyPrice", "Currency",
            "PrioritySupport", "AdvancedAnalytics", "ApiAccess", 
            "ResponseTimeHours", "AdditionalFeatures",
            "IsActive", "DisplayOrder", "CreatedDate"
        ) VALUES (
            'M', 'Medium', 'Ideal for medium-sized farms',
            20, 200,
            299.99, 2999.99, 'TRY',
            false, true, false,
            24, '["Advanced plant analysis","Email & SMS notifications","Detailed reports","Historical data access","Basic API access"]',
            true, 2, '2025-08-13 16:00:00'
        );
        RAISE NOTICE 'M tier added successfully';
    ELSE
        RAISE NOTICE 'M tier already exists';
    END IF;

    -- Add L tier if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM "SubscriptionTiers" WHERE "TierName" = 'L') THEN
        INSERT INTO "SubscriptionTiers" (
            "TierName", "DisplayName", "Description", 
            "DailyRequestLimit", "MonthlyRequestLimit", 
            "MonthlyPrice", "YearlyPrice", "Currency",
            "PrioritySupport", "AdvancedAnalytics", "ApiAccess", 
            "ResponseTimeHours", "AdditionalFeatures",
            "IsActive", "DisplayOrder", "CreatedDate"
        ) VALUES (
            'L', 'Large', 'Best for large commercial farms',
            50, 500,
            599.99, 5999.99, 'TRY',
            true, true, true,
            12, '["Premium plant analysis with AI insights","All notification channels","Custom reports","Full historical data","Full API access","Priority support","Export capabilities"]',
            true, 3, '2025-08-13 16:00:00'
        );
        RAISE NOTICE 'L tier added successfully';
    ELSE
        RAISE NOTICE 'L tier already exists';
    END IF;

    -- Add XL tier if it doesn't exist  
    IF NOT EXISTS (SELECT 1 FROM "SubscriptionTiers" WHERE "TierName" = 'XL') THEN
        INSERT INTO "SubscriptionTiers" (
            "TierName", "DisplayName", "Description", 
            "DailyRequestLimit", "MonthlyRequestLimit", 
            "MonthlyPrice", "YearlyPrice", "Currency",
            "PrioritySupport", "AdvancedAnalytics", "ApiAccess", 
            "ResponseTimeHours", "AdditionalFeatures",
            "IsActive", "DisplayOrder", "CreatedDate"
        ) VALUES (
            'XL', 'Extra Large', 'Enterprise solution for agricultural corporations',
            200, 2000,
            1499.99, 14999.99, 'TRY',
            true, true, true,
            6, '["Enterprise AI analysis with custom models","All features included","Dedicated support team","Custom integrations","White-label options","SLA guarantee","Training sessions","Unlimited data retention"]',
            true, 4, '2025-08-13 16:00:00'
        );
        RAISE NOTICE 'XL tier added successfully';
    ELSE
        RAISE NOTICE 'XL tier already exists';
    END IF;
END $$;

-- Display final results
SELECT "Id", "TierName", "DisplayName", "DailyRequestLimit", "MonthlyRequestLimit", "MonthlyPrice", "IsActive"
FROM "SubscriptionTiers" 
ORDER BY "DisplayOrder";