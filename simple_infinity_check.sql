-- Simple check for infinity values - tries different case variations

-- Option 1: Try with quoted names (PascalCase)
SELECT 'Checking with quoted PascalCase names:' as info;
SELECT COUNT(*) as infinity_count FROM "Users" WHERE "BirthDate"::text = 'infinity' OR "UpdateContactDate"::text = 'infinity' OR "RecordDate"::text = 'infinity';

-- Option 2: Try with lowercase names  
SELECT 'Checking with lowercase names:' as info;
SELECT COUNT(*) as infinity_count FROM users WHERE birthdate::text = 'infinity' OR updatecontactdate::text = 'infinity' OR recorddate::text = 'infinity';

-- Option 3: Show table structure
SELECT 'Table structure:' as info;
SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'Users' OR table_name = 'users' ORDER BY ordinal_position;