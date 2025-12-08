-- ==========================================
-- SINGLE USER DELETION & SEQUENCE FIX
-- Production Database Cleanup Script
-- ==========================================
-- USAGE: Replace @USER_ID with actual UserId to delete
-- ==========================================

-- STEP 1: IDENTIFY USER TO DELETE
-- ==========================================
SELECT "UserId", "FullName", "Email", "CreatedDate"
FROM public."Users"
WHERE "UserId" = 190; -- CHANGE THIS

-- STEP 2: CHECK RELATED DATA (BEFORE DELETION)
-- ==========================================
SELECT
    'PlantAnalyses' as table_name,
    COUNT(*) as record_count
FROM public."PlantAnalyses"
WHERE "UserId" = 190 -- CHANGE THIS
UNION ALL
SELECT
    'UserSubscriptions',
    COUNT(*)
FROM public."UserSubscriptions"
WHERE "UserId" = 190 -- CHANGE THIS
UNION ALL
SELECT
    'ReferralTracking (as referee)',
    COUNT(*)
FROM public."ReferralTracking"
WHERE "RefereeUserId" = 190 -- CHANGE THIS
UNION ALL
SELECT
    'ReferralTracking (as referrer)',
    COUNT(*)
FROM public."ReferralTracking"
WHERE "ReferrerUserId" = 190 -- CHANGE THIS
UNION ALL
SELECT
    'ReferralCodes',
    COUNT(*)
FROM public."ReferralCodes"
WHERE "UserId" = 190 -- CHANGE THIS
UNION ALL
SELECT
    'MobileLogins',
    COUNT(*)
FROM public."MobileLogins"
WHERE "UserId" = 190; -- CHANGE THIS

-- STEP 3: DELETE USER & RELATED DATA
-- ==========================================
-- CRITICAL: Run these in order due to FK constraints

-- 3a. Delete PlantAnalyses (has FK to Users)
DELETE FROM public."PlantAnalyses" WHERE "UserId" = 190; -- CHANGE THIS

-- 3b. Delete UserSubscriptions (has FK to Users)
DELETE FROM public."UserSubscriptions" WHERE "UserId" = 190; -- CHANGE THIS

-- 3c. Delete ReferralTracking (both referee and referrer)
DELETE FROM public."ReferralTracking" WHERE "RefereeUserId" = 190 OR "ReferrerUserId" = 190; -- CHANGE THIS

-- 3d. Delete ReferralCodes
DELETE FROM public."ReferralCodes" WHERE "UserId" = 190; -- CHANGE THIS

-- 3e. Delete MobileLogins
DELETE FROM public."MobileLogins" WHERE "UserId" = 190; -- CHANGE THIS

-- 3f. Delete User (final step)
DELETE FROM public."Users" WHERE "UserId" = 190; -- CHANGE THIS

-- STEP 4: FIX SEQUENCES (ONLY IF NEEDED)
-- ==========================================
-- Run this to check if sequences need fixing
SELECT
    'Users' as table_name,
    (SELECT MAX("UserId") FROM public."Users") as max_id,
    currval('public."Users_UserId_seq"') as current_sequence,
    CASE
        WHEN currval('public."Users_UserId_seq"') >= COALESCE((SELECT MAX("UserId") FROM public."Users"), 1)
        THEN 'OK'
        ELSE 'NEEDS FIX'
    END as status
UNION ALL
SELECT
    'PlantAnalyses',
    (SELECT MAX("Id") FROM public."PlantAnalyses"),
    currval('public."PlantAnalyses_Id_seq"'),
    CASE
        WHEN currval('public."PlantAnalyses_Id_seq"') >= COALESCE((SELECT MAX("Id") FROM public."PlantAnalyses"), 1)
        THEN 'OK'
        ELSE 'NEEDS FIX'
    END
UNION ALL
SELECT
    'UserSubscriptions',
    (SELECT MAX("Id") FROM public."UserSubscriptions"),
    currval('public."UserSubscriptions_Id_seq"'),
    CASE
        WHEN currval('public."UserSubscriptions_Id_seq"') >= COALESCE((SELECT MAX("Id") FROM public."UserSubscriptions"), 1)
        THEN 'OK'
        ELSE 'NEEDS FIX'
    END
UNION ALL
SELECT
    'ReferralTracking',
    (SELECT MAX("Id") FROM public."ReferralTracking"),
    currval('public."ReferralTracking_Id_seq"'),
    CASE
        WHEN currval('public."ReferralTracking_Id_seq"') >= COALESCE((SELECT MAX("Id") FROM public."ReferralTracking"), 1)
        THEN 'OK'
        ELSE 'NEEDS FIX'
    END
UNION ALL
SELECT
    'ReferralCodes',
    (SELECT MAX("Id") FROM public."ReferralCodes"),
    currval('public."ReferralCodes_Id_seq"'),
    CASE
        WHEN currval('public."ReferralCodes_Id_seq"') >= COALESCE((SELECT MAX("Id") FROM public."ReferralCodes"), 1)
        THEN 'OK'
        ELSE 'NEEDS FIX'
    END
UNION ALL
SELECT
    'MobileLogins',
    (SELECT MAX("Id") FROM public."MobileLogins"),
    currval('public."MobileLogins_Id_seq"'),
    CASE
        WHEN currval('public."MobileLogins_Id_seq"') >= COALESCE((SELECT MAX("Id") FROM public."MobileLogins"), 1)
        THEN 'OK'
        ELSE 'NEEDS FIX'
    END;

-- STEP 5: RUN SEQUENCE FIXES (ONLY FOR 'NEEDS FIX' TABLES)
-- ==========================================
-- Users sequence fix
SELECT setval('public."Users_UserId_seq"', (SELECT COALESCE(MAX("UserId"), 1) FROM public."Users"));

-- PlantAnalyses sequence fix
SELECT setval('public."PlantAnalyses_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."PlantAnalyses"));

-- UserSubscriptions sequence fix
SELECT setval('public."UserSubscriptions_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."UserSubscriptions"));

-- ReferralTracking sequence fix
SELECT setval('public."ReferralTracking_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."ReferralTracking"));

-- ReferralCodes sequence fix
SELECT setval('public."ReferralCodes_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."ReferralCodes"));

-- MobileLogins sequence fix
SELECT setval('public."MobileLogins_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."MobileLogins"));

-- STEP 6: VERIFICATION
-- ==========================================
-- Re-run STEP 4 query to verify all sequences are now 'OK'
SELECT
    'Users' as table_name,
    (SELECT MAX("UserId") FROM public."Users") as max_id,
    currval('public."Users_UserId_seq"') as current_sequence,
    CASE
        WHEN currval('public."Users_UserId_seq"') >= COALESCE((SELECT MAX("UserId") FROM public."Users"), 1)
        THEN 'OK ✅'
        ELSE 'STILL BROKEN ❌'
    END as status
UNION ALL
SELECT
    'PlantAnalyses',
    (SELECT MAX("Id") FROM public."PlantAnalyses"),
    currval('public."PlantAnalyses_Id_seq"'),
    CASE
        WHEN currval('public."PlantAnalyses_Id_seq"') >= COALESCE((SELECT MAX("Id") FROM public."PlantAnalyses"), 1)
        THEN 'OK ✅'
        ELSE 'STILL BROKEN ❌'
    END
UNION ALL
SELECT
    'UserSubscriptions',
    (SELECT MAX("Id") FROM public."UserSubscriptions"),
    currval('public."UserSubscriptions_Id_seq"'),
    CASE
        WHEN currval('public."UserSubscriptions_Id_seq"') >= COALESCE((SELECT MAX("Id") FROM public."UserSubscriptions"), 1)
        THEN 'OK ✅'
        ELSE 'STILL BROKEN ❌'
    END
UNION ALL
SELECT
    'ReferralTracking',
    (SELECT MAX("Id") FROM public."ReferralTracking"),
    currval('public."ReferralTracking_Id_seq"'),
    CASE
        WHEN currval('public."ReferralTracking_Id_seq"') >= COALESCE((SELECT MAX("Id") FROM public."ReferralTracking"), 1)
        THEN 'OK ✅'
        ELSE 'STILL BROKEN ❌'
    END
UNION ALL
SELECT
    'ReferralCodes',
    (SELECT MAX("Id") FROM public."ReferralCodes"),
    currval('public."ReferralCodes_Id_seq"'),
    CASE
        WHEN currval('public."ReferralCodes_Id_seq"') >= COALESCE((SELECT MAX("Id") FROM public."ReferralCodes"), 1)
        THEN 'OK ✅'
        ELSE 'STILL BROKEN ❌'
    END
UNION ALL
SELECT
    'MobileLogins',
    (SELECT MAX("Id") FROM public."MobileLogins"),
    currval('public."MobileLogins_Id_seq"'),
    CASE
        WHEN currval('public."MobileLogins_Id_seq"') >= COALESCE((SELECT MAX("Id") FROM public."MobileLogins"), 1)
        THEN 'OK ✅'
        ELSE 'STILL BROKEN ❌'
    END
ORDER BY table_name;
