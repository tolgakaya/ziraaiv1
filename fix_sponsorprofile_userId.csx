#r "nuget: Npgsql, 8.0.4"

using System;
using Npgsql;

try 
{
    var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";
    
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Connected to PostgreSQL database");
    
    // Check if UserId column exists in SponsorProfiles
    var checkColumnQuery = @"
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'SponsorProfiles' AND column_name = 'UserId'
        );";
    
    using var checkCommand = new NpgsqlCommand(checkColumnQuery, connection);
    var columnExists = (bool)await checkCommand.ExecuteScalarAsync();
    
    if (columnExists)
    {
        Console.WriteLine("⚠️  UserId column exists in SponsorProfiles - removing...");
        
        // Drop UserId column
        var dropColumnQuery = @"ALTER TABLE ""SponsorProfiles"" DROP COLUMN ""UserId"";";
        using var dropCommand = new NpgsqlCommand(dropColumnQuery, connection);
        await dropCommand.ExecuteNonQueryAsync();
        Console.WriteLine("✅ UserId column dropped from SponsorProfiles table");
    }
    else
    {
        Console.WriteLine("✅ UserId column does not exist in SponsorProfiles table");
    }
    
    // Mark migration as applied
    var insertMigrationQuery = @"
        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") 
        VALUES ('20250819151216_RemoveSponsorProfileUserId', '9.0.0')
        ON CONFLICT (""MigrationId"") DO NOTHING;";
    
    using var migrationCommand = new NpgsqlCommand(insertMigrationQuery, connection);
    await migrationCommand.ExecuteNonQueryAsync();
    Console.WriteLine("✅ Migration marked as applied in migration history");
    
    Console.WriteLine("🎉 Database UserId column fix completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    throw;
}