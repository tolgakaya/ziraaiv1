# Manual Database Fix Commands

## STAGING Environment Fix
Connection: `Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass`

Bu SQL komutlarını bir PostgreSQL client (pgAdmin, DBeaver, VS Code PostgreSQL extension) ile çalıştırınız:

```sql
-- Check current state
SELECT 
    table_name,
    column_name, 
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name IN ('Users', 'users')
AND column_name IN ('BirthDate', 'birthdate', 'Gender', 'gender')
ORDER BY table_name, column_name;

-- Fix BirthDate to allow NULL
ALTER TABLE "Users" ALTER COLUMN "BirthDate" DROP NOT NULL;

-- Fix Gender to allow NULL  
ALTER TABLE "Users" ALTER COLUMN "Gender" DROP NOT NULL;

-- Verify changes
SELECT 
    'AFTER FIX:' as status,
    table_name,
    column_name, 
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name IN ('Users', 'users')
AND column_name IN ('BirthDate', 'birthdate', 'Gender', 'gender')
ORDER BY table_name, column_name;
```

## PRODUCTION Environment Fix (Apply AFTER staging test success)
Connection: `Host=yamabiko.proxy.rlwy.net;Port=41760;Database=railway;Username=postgres;Password=rcrHmHyxJLKYacWzzJoqVRwtJadyEBDQ;SSL Mode=Require;Trust Server Certificate=true`

**Aynı SQL komutlarını production'da da çalıştırın.**

## Test Steps
1. Yukarıdaki SQL'i staging'de çalıştır
2. Register endpoint'ini test et
3. Başarılı olursa, production için onay al
4. Production'da aynı SQL'i çalıştır