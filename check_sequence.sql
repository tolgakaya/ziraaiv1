-- Check current sequence value
SELECT last_value, is_called FROM "Users_UserId_seq";

-- Check max UserId in Users table
SELECT MAX("UserId") as max_user_id FROM "Users";

-- Fix sequence to start from 167 (166 + 1)
SELECT setval('"Users_UserId_seq"', 167, false);

-- Verify the fix
SELECT last_value, is_called FROM "Users_UserId_seq";
