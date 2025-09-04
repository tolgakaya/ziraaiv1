#r "nuget: Npgsql, 8.0.4"

using System;
using Npgsql;

try 
{
    var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";
    
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Connected to PostgreSQL database");
    
    // Create DeepLinks table
    var createDeepLinksTable = @"
        CREATE TABLE IF NOT EXISTS ""DeepLinks"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""LinkId"" VARCHAR(50) NOT NULL UNIQUE,
            ""Type"" VARCHAR(50) NOT NULL,
            ""PrimaryParameter"" VARCHAR(200),
            ""AdditionalParameters"" VARCHAR(500),
            ""DeepLinkUrl"" VARCHAR(500) NOT NULL,
            ""UniversalLinkUrl"" VARCHAR(500),
            ""WebFallbackUrl"" VARCHAR(500),
            ""ShortUrl"" VARCHAR(200),
            ""QrCodeUrl"" TEXT,
            ""CampaignSource"" VARCHAR(50),
            ""SponsorId"" VARCHAR(50),
            ""CreatedDate"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            ""ExpiryDate"" timestamp without time zone NOT NULL,
            ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
            ""TotalClicks"" INTEGER NOT NULL DEFAULT 0,
            ""MobileAppOpens"" INTEGER NOT NULL DEFAULT 0,
            ""WebFallbackOpens"" INTEGER NOT NULL DEFAULT 0,
            ""UniqueDevices"" INTEGER NOT NULL DEFAULT 0,
            ""LastClickDate"" timestamp without time zone
        );";
    
    using var createDeepLinksCommand = new NpgsqlCommand(createDeepLinksTable, connection);
    await createDeepLinksCommand.ExecuteNonQueryAsync();
    Console.WriteLine("✅ DeepLinks table created");
    
    // Create DeepLinkClickRecords table
    var createClickRecordsTable = @"
        CREATE TABLE IF NOT EXISTS ""DeepLinkClickRecords"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""LinkId"" VARCHAR(50) NOT NULL,
            ""UserAgent"" VARCHAR(500),
            ""IpAddress"" VARCHAR(45),
            ""Platform"" VARCHAR(20),
            ""DeviceId"" VARCHAR(100),
            ""Referrer"" VARCHAR(500),
            ""ClickDate"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            ""Country"" VARCHAR(100),
            ""City"" VARCHAR(100),
            ""Source"" VARCHAR(50),
            ""DidOpenApp"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""DidCompleteAction"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""ActionCompletedDate"" timestamp without time zone,
            ""ActionResult"" VARCHAR(50)
        );";
    
    using var createClickRecordsCommand = new NpgsqlCommand(createClickRecordsTable, connection);
    await createClickRecordsCommand.ExecuteNonQueryAsync();
    Console.WriteLine("✅ DeepLinkClickRecords table created");
    
    // Create indexes for performance
    var createIndexes = @"
        CREATE INDEX IF NOT EXISTS ""IX_DeepLinks_LinkId"" ON ""DeepLinks"" (""LinkId"");
        CREATE INDEX IF NOT EXISTS ""IX_DeepLinks_Type"" ON ""DeepLinks"" (""Type"");
        CREATE INDEX IF NOT EXISTS ""IX_DeepLinks_SponsorId"" ON ""DeepLinks"" (""SponsorId"");
        CREATE INDEX IF NOT EXISTS ""IX_DeepLinkClickRecords_LinkId"" ON ""DeepLinkClickRecords"" (""LinkId"");
        CREATE INDEX IF NOT EXISTS ""IX_DeepLinkClickRecords_ClickDate"" ON ""DeepLinkClickRecords"" (""ClickDate"");
        CREATE INDEX IF NOT EXISTS ""IX_DeepLinkClickRecords_Platform"" ON ""DeepLinkClickRecords"" (""Platform"");
    ";
    
    using var createIndexesCommand = new NpgsqlCommand(createIndexes, connection);
    await createIndexesCommand.ExecuteNonQueryAsync();
    Console.WriteLine("✅ Indexes created");
    
    // Add foreign key constraint (PostgreSQL doesn't support IF NOT EXISTS for constraints)
    var addForeignKey = @"
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM pg_constraint 
                WHERE conname = 'FK_DeepLinkClickRecords_DeepLinks_LinkId'
            ) THEN
                ALTER TABLE ""DeepLinkClickRecords"" 
                ADD CONSTRAINT ""FK_DeepLinkClickRecords_DeepLinks_LinkId"" 
                FOREIGN KEY (""LinkId"") REFERENCES ""DeepLinks"" (""LinkId"") ON DELETE CASCADE;
            END IF;
        END$$;
    ";
    
    using var addForeignKeyCommand = new NpgsqlCommand(addForeignKey, connection);
    await addForeignKeyCommand.ExecuteNonQueryAsync();
    Console.WriteLine("✅ Foreign key constraint added");
    
    // Mark migration as applied
    var insertMigrationQuery = @"
        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") 
        VALUES ('20250819192633_AddDeepLinksSystem', '9.0.0')
        ON CONFLICT (""MigrationId"") DO NOTHING;";
    
    using var migrationCommand = new NpgsqlCommand(insertMigrationQuery, connection);
    await migrationCommand.ExecuteNonQueryAsync();
    Console.WriteLine("✅ Migration marked as applied in migration history");
    
    Console.WriteLine("🎉 DeepLinks system tables created successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    throw;
}