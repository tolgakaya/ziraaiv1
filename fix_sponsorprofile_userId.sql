-- Manual fix for SponsorProfiles.UserId column issue
-- Remove UserId column from SponsorProfiles table if it exists

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SponsorProfiles' AND column_name = 'UserId'
    ) THEN
        ALTER TABLE "SponsorProfiles" DROP COLUMN "UserId";
        RAISE NOTICE 'UserId column dropped from SponsorProfiles table';
    ELSE
        RAISE NOTICE 'UserId column does not exist in SponsorProfiles table';
    END IF;
END$$;

-- Mark pending migrations as applied in migration history
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20250819151216_RemoveSponsorProfileUserId', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

SELECT 'Migration history updated' as status;