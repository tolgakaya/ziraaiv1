# Admin User Creation Guide

**Last Updated:** 2025-01-23

## Overview

This guide shows how to create an admin user for the ZiraAI Admin Operations API.

---

## Method 1: Using Registration Endpoint (Recommended)

### Step 1: Register New User

**POST** `/api/auth/register`

```json
{
  "fullName": "System Administrator",
  "email": "admin@ziraai.com",
  "password": "Admin@123!Strong",
  "mobilePhones": "+905550000000"
}
```

**cURL Example:**
```bash
curl -X POST "https://localhost:5001/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "System Administrator",
    "email": "admin@ziraai.com",
    "password": "Admin@123!Strong",
    "mobilePhones": "+905550000000"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "User registered successfully",
  "data": {
    "userId": 123,
    "email": "admin@ziraai.com",
    "accessToken": "eyJhbG..."
  }
}
```

**Save the userId** - you'll need it for the next step!

---

### Step 2: Assign Admin Claims via SQL

Now connect to your PostgreSQL database and run:

```sql
-- Replace 123 with your actual userId from Step 1
DO $$
DECLARE
    v_user_id INTEGER := 123;  -- CHANGE THIS to your userId
BEGIN
    -- First, ensure admin operation claims exist
    INSERT INTO "OperationClaims" ("Name")
    VALUES
        ('Admin'),
        ('admin.users.manage'),
        ('admin.subscriptions.manage'),
        ('admin.sponsorship.manage'),
        ('admin.analytics.view'),
        ('admin.audit.view'),
        ('admin.plantanalysis.manage')
    ON CONFLICT ("Name") DO NOTHING;

    -- Assign all admin claims to the user
    INSERT INTO "UserOperationClaims" ("UserId", "OperationClaimId")
    SELECT
        v_user_id,
        "Id"
    FROM "OperationClaims"
    WHERE "Name" LIKE 'admin.%' OR "Name" = 'Admin';

    -- Output confirmation
    RAISE NOTICE 'Admin claims assigned to user %', v_user_id;
END $$;
```

---

### Step 3: Verify Admin User

**Check via SQL:**
```sql
SELECT
    u."UserId",
    u."FullName",
    u."Email",
    u."IsActive",
    STRING_AGG(oc."Name", ', ') as "Claims"
FROM "Users" u
LEFT JOIN "UserOperationClaims" uoc ON u."UserId" = uoc."UserId"
LEFT JOIN "OperationClaims" oc ON uoc."OperationClaimId" = oc."Id"
WHERE u."Email" = 'admin@ziraai.com'
GROUP BY u."UserId", u."FullName", u."Email", u."IsActive";
```

**Expected Output:**
```
UserId | FullName              | Email              | IsActive | Claims
-------+-----------------------+--------------------+----------+---------------------------------------------------
123    | System Administrator  | admin@ziraai.com   | t        | Admin, admin.users.manage, admin.subscriptions.manage, ...
```

---

### Step 4: Test Login

**POST** `/api/auth/login`

```json
{
  "email": "admin@ziraai.com",
  "password": "Admin@123!Strong"
}
```

**cURL Example:**
```bash
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@ziraai.com",
    "password": "Admin@123!Strong"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh-token-here",
    "expiration": "2025-01-23T15:00:00Z",
    "claims": [
      "Admin",
      "admin.users.manage",
      "admin.subscriptions.manage",
      "admin.sponsorship.manage",
      "admin.analytics.view",
      "admin.audit.view",
      "admin.plantanalysis.manage"
    ]
  }
}
```

---

### Step 5: Test Admin Endpoint

**GET** `/api/admin/users?page=1&pageSize=10`

```bash
curl -X GET "https://localhost:5001/api/admin/users?page=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

**Expected:** 200 OK with user list

✅ **Success!** Your admin user is ready to use.

---

## Method 2: Database Only (If Registration is Disabled)

If you need to create admin without using the registration endpoint:

### Step 1: Get Password Hash

First, you need to generate a proper password hash. You can:

**Option A: Use existing user's hash format**
```sql
-- Look at an existing user's password hash format
SELECT "PasswordHash", "PasswordSalt"
FROM "Users"
WHERE "Email" = 'any-existing-user@example.com'
LIMIT 1;
```

**Option B: Create via code**

Create a temporary C# console app or use the API's password hashing service:

```csharp
using Core.Utilities.Security.Hashing;

var password = "Admin@123!Strong";
byte[] passwordHash, passwordSalt;
HashingHelper.CreatePasswordHash(password, out passwordHash, out passwordSalt);

Console.WriteLine($"PasswordHash: {Convert.ToBase64String(passwordHash)}");
Console.WriteLine($"PasswordSalt: {Convert.ToBase64String(passwordSalt)}");
```

### Step 2: Insert User with Hash

```sql
-- IMPORTANT: Replace the hash values with actual generated hashes
INSERT INTO "Users" (
    "FullName",
    "Email",
    "MobilePhones",
    "PasswordHash",
    "PasswordSalt",
    "IsActive",
    "Status",
    "CreatedDate"
)
VALUES (
    'System Administrator',
    'admin@ziraai.com',
    '+905550000000',
    decode('YOUR_PASSWORD_HASH_BASE64', 'base64'),  -- Replace with actual hash
    decode('YOUR_PASSWORD_SALT_BASE64', 'base64'),  -- Replace with actual salt
    true,
    true,
    NOW()
)
RETURNING "UserId";
```

### Step 3: Assign Claims

Same as Method 1, Step 2 (see above)

---

## Method 3: Using Existing User

If you already have a user account and want to make it admin:

### Find Your User ID

```sql
SELECT "UserId", "FullName", "Email"
FROM "Users"
WHERE "Email" = 'your-email@example.com';
```

### Assign Admin Claims

```sql
-- Replace 123 with your actual UserId
INSERT INTO "UserOperationClaims" ("UserId", "OperationClaimId")
SELECT
    123,  -- Your UserId
    "Id"
FROM "OperationClaims"
WHERE "Name" LIKE 'admin.%' OR "Name" = 'Admin'
ON CONFLICT DO NOTHING;
```

### Verify

```sql
SELECT oc."Name"
FROM "UserOperationClaims" uoc
INNER JOIN "OperationClaims" oc ON uoc."OperationClaimId" = oc."Id"
WHERE uoc."UserId" = 123  -- Your UserId
ORDER BY oc."Name";
```

---

## Required Admin Claims

Ensure these operation claims exist in your database:

```sql
-- Check if admin claims exist
SELECT "Id", "Name"
FROM "OperationClaims"
WHERE "Name" IN (
    'Admin',
    'admin.users.manage',
    'admin.subscriptions.manage',
    'admin.sponsorship.manage',
    'admin.analytics.view',
    'admin.audit.view',
    'admin.plantanalysis.manage'
)
ORDER BY "Name";
```

If any are missing:

```sql
INSERT INTO "OperationClaims" ("Name")
VALUES
    ('Admin'),
    ('admin.users.manage'),
    ('admin.subscriptions.manage'),
    ('admin.sponsorship.manage'),
    ('admin.analytics.view'),
    ('admin.audit.view'),
    ('admin.plantanalysis.manage')
ON CONFLICT ("Name") DO NOTHING;
```

---

## Troubleshooting

### Issue: "Operation claims not found"

**Solution:**
```sql
-- Create all admin claims
INSERT INTO "OperationClaims" ("Name")
VALUES
    ('Admin'),
    ('admin.users.manage'),
    ('admin.subscriptions.manage'),
    ('admin.sponsorship.manage'),
    ('admin.analytics.view'),
    ('admin.audit.view'),
    ('admin.plantanalysis.manage')
ON CONFLICT ("Name") DO NOTHING;
```

### Issue: "Insufficient permissions" after login

**Check assigned claims:**
```sql
SELECT u."Email", oc."Name"
FROM "Users" u
INNER JOIN "UserOperationClaims" uoc ON u."UserId" = uoc."UserId"
INNER JOIN "OperationClaims" oc ON uoc."OperationClaimId" = oc."Id"
WHERE u."Email" = 'admin@ziraai.com';
```

**Expected:** Should see all admin.* claims

### Issue: "Invalid username or password"

**Check user exists and is active:**
```sql
SELECT "UserId", "Email", "IsActive", "Status"
FROM "Users"
WHERE "Email" = 'admin@ziraai.com';
```

**Expected:** IsActive = true, Status = true

### Issue: "Token validation failed"

**Verify JWT settings in appsettings.json:**
```json
{
  "TokenOptions": {
    "Audience": "ziraai.com",
    "Issuer": "ziraai.com",
    "AccessTokenExpiration": 60,
    "SecurityKey": "your-secret-key-here"
  }
}
```

---

## Security Best Practices

### 1. Strong Password
```
✅ Minimum 8 characters
✅ Upper and lowercase
✅ Numbers and special characters
✅ Not a dictionary word
```

### 2. Change Default Credentials
```bash
# Immediately after first login, change password
POST /api/auth/change-password
{
  "oldPassword": "Admin@123!Strong",
  "newPassword": "YourStrongNewPassword123!"
}
```

### 3. Secure Email
- Use a dedicated admin email
- Enable 2FA if available
- Don't use personal email

### 4. Rotate Tokens Regularly
```bash
# Use refresh token to get new access token
POST /api/auth/refresh-token
{
  "refreshToken": "your-refresh-token"
}
```

### 5. Monitor Admin Activity
```sql
-- Check recent admin operations
SELECT *
FROM "AdminOperationLogs"
WHERE "AdminUserId" = (
    SELECT "UserId" FROM "Users" WHERE "Email" = 'admin@ziraai.com'
)
ORDER BY "CreatedDate" DESC
LIMIT 20;
```

---

## Quick Start Commands

### For Local Development

```bash
# 1. Start your API
cd WebAPI
dotnet run

# 2. Register admin user
curl -X POST "https://localhost:5001/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Dev Admin",
    "email": "admin@dev.local",
    "password": "DevAdmin@123",
    "mobilePhones": "+905550000000"
  }'

# 3. Copy the userId from response

# 4. Connect to database and run:
psql -U ziraai -d ziraai_dev
\i claudedocs/AdminOperations/CREATE_ADMIN_USER.sql

# 5. Login
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@dev.local",
    "password": "DevAdmin@123"
  }'
```

---

## Complete Example Workflow

```bash
# Step 1: Register
curl -X POST "https://localhost:5001/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "System Administrator",
    "email": "admin@ziraai.com",
    "password": "SecureAdmin@2025",
    "mobilePhones": "+905550000000"
  }'

# Response: { "data": { "userId": 123 } }

# Step 2: Assign admin claims (via psql)
psql -U postgres -d ziraai -c "
INSERT INTO \"UserOperationClaims\" (\"UserId\", \"OperationClaimId\")
SELECT 123, \"Id\" FROM \"OperationClaims\"
WHERE \"Name\" LIKE 'admin.%' OR \"Name\" = 'Admin';
"

# Step 3: Login
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@ziraai.com",
    "password": "SecureAdmin@2025"
  }'

# Response: { "data": { "accessToken": "eyJ..." } }

# Step 4: Test admin endpoint
curl -X GET "https://localhost:5001/api/admin/users?page=1&pageSize=5" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Success! You now have a working admin user
```

---

## Support

If you encounter issues:

1. Check the [Troubleshooting](#troubleshooting) section
2. Verify database schema is up to date
3. Check API logs for detailed error messages
4. Review `ADMIN_OPERATIONS_API_COMPLETE_GUIDE.md` for authentication details

---

**Last Updated:** 2025-01-23
**Version:** 1.0
