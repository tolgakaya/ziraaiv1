#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;
using System.Threading.Tasks;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    Console.WriteLine("🔧 Syncing migration history with actual database state...");
    
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("✅ Connected to staging database");
    
    // List of migrations to add to history (these exist in the database but are missing from history)
    var missingMigrations = new[]
    {
        ("20250813053837_AddSubscriptionSystem", "9.0.0"),
        ("20250813061611_UpdateSubscriptionEntities", "9.0.0"),
        ("20250813135944_AddSubscriptionSystemFinal", "9.0.0"),
        ("20250813161221_AddTrialSubscriptionTier", "9.0.0"),
        ("20250813161327_UpdateSubscriptionTiersWithTrial", "9.0.0"),
        ("20250813161354_FinalTrialTierUpdate", "9.0.0"),
        ("20250813161429_FixedDateTrialTier", "9.0.0"),
        ("20250813212907_AddSponsorshipSystem", "9.0.0"),
        ("20250813214715_AppliedSponsorshipSchemaManually", "9.0.0"),
        ("20250813221146_AddEmailAlreadyExistsMessage", "9.0.0")
    };
    
    Console.WriteLine($"📝 Adding {missingMigrations.Length} missing migrations to history...");
    
    foreach (var (migrationId, version) in missingMigrations)
    {
        var insertCmd = new NpgsqlCommand(@"
            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            VALUES (@migrationId, @version)
            ON CONFLICT (""MigrationId"") DO NOTHING", connection);
        
        insertCmd.Parameters.AddWithValue("@migrationId", migrationId);
        insertCmd.Parameters.AddWithValue("@version", version);
        
        var rowsAffected = await insertCmd.ExecuteNonQueryAsync();
        if (rowsAffected > 0)
        {
            Console.WriteLine($"   ✅ Added: {migrationId}");
        }
        else
        {
            Console.WriteLine($"   ⏭️  Skipped (already exists): {migrationId}");
        }
    }
    
    Console.WriteLine("\n🎉 Migration history sync completed!");
    Console.WriteLine("\n📋 Current migration history:");
    
    var getMigrationsCmd = new NpgsqlCommand(@"
        SELECT ""MigrationId"", ""ProductVersion"" 
        FROM ""__EFMigrationsHistory"" 
        ORDER BY ""MigrationId""", connection);
    
    await using var reader = await getMigrationsCmd.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        var migrationId = reader.GetString(0);
        var productVersion = reader.GetString(1);
        Console.WriteLine($"   📝 {migrationId} (v{productVersion})");
    }
    
    Console.WriteLine("\n✅ Ready to apply the AddSponsorshipLinkFields migration!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}