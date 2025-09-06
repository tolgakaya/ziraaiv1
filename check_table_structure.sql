-- Check actual table and column names in PostgreSQL
SELECT table_name, column_name, data_type 
FROM information_schema.columns 
WHERE table_name ILIKE '%user%'
ORDER BY table_name, ordinal_position;