-- Check for infinity values in User table
SELECT 
    userid, 
    fullname, 
    email, 
    birthdate, 
    updatecontactdate, 
    recorddate
FROM users 
WHERE 
    birthdate = 'infinity' 
    OR updatecontactdate = 'infinity' 
    OR recorddate = 'infinity'
    OR birthdate::text = 'infinity'
    OR updatecontactdate::text = 'infinity'
    OR recorddate::text = 'infinity';