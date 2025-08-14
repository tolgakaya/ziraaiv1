#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;
using System.Threading.Tasks;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    Console.WriteLine("🔍 Checking migration status in staging database...");
    
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("✅ Connected to staging database");
    
    // Check if __EFMigrationsHistory table exists
    var checkHistoryTableCmd = new NpgsqlCommand(@"
        SELECT EXISTS (
            SELECT FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name = '__EFMigrationsHistory'
        )", connection);
    
    var historyExists = (bool)await checkHistoryTableCmd.ExecuteScalarAsync();
    Console.WriteLine($"📋 Migration history table exists: {historyExists}");
    
    if (historyExists)
    {
        // Get applied migrations
        var getMigrationsCmd = new NpgsqlCommand(@"
            SELECT ""MigrationId"", ""ProductVersion"" 
            FROM ""__EFMigrationsHistory"" 
            ORDER BY ""MigrationId""", connection);
        
        await using var reader = await getMigrationsCmd.ExecuteReaderAsync();
        Console.WriteLine("\n📝 Applied migrations:");
        
        while (await reader.ReadAsync())
        {
            var migrationId = reader.GetString(0);
            var productVersion = reader.GetString(1);
            Console.WriteLine($"   ✅ {migrationId} (v{productVersion})");
        }
    }
    
    // Check if SponsorshipCodes table exists and its columns
    Console.WriteLine("\n🔍 Checking SponsorshipCodes table structure...");
    
    var checkSponsorshipCodesCmd = new NpgsqlCommand(@"
        SELECT EXISTS (
            SELECT FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name = 'SponsorshipCodes'
        )", connection);
    
    var sponsorshipCodesExists = (bool)await checkSponsorshipCodesCmd.ExecuteScalarAsync();
    Console.WriteLine($"🏷️  SponsorshipCodes table exists: {sponsorshipCodesExists}");
    
    if (sponsorshipCodesExists)
    {
        // Check for link-related columns
        var checkColumnsCmd = new NpgsqlCommand(@"
            SELECT column_name, data_type, is_nullable, column_default
            FROM information_schema.columns 
            WHERE table_name = 'SponsorshipCodes' 
                AND column_name IN (
                    'RedemptionLink', 
                    'LinkClickDate', 
                    'LinkClickCount', 
                    'RecipientPhone',
                    'RecipientName',
                    'LinkSentDate',
                    'LinkSentVia',
                    'LinkDelivered',
                    'LastClickIpAddress'
                )
            ORDER BY column_name", connection);
        
        await using var colReader = await checkColumnsCmd.ExecuteReaderAsync();
        Console.WriteLine("\n🔗 Link-related columns:");
        
        var foundColumns = new List<string>();
        while (await colReader.ReadAsync())
        {
            var columnName = colReader.GetString(0);
            var dataType = colReader.GetString(1);
            var isNullable = colReader.GetString(2);
            var columnDefault = colReader.IsDBNull(3) ? "NULL" : colReader.GetString(3);
            
            foundColumns.Add(columnName);
            Console.WriteLine($"   ✅ {columnName}: {dataType} (nullable: {isNullable}, default: {columnDefault})");
        }
        
        var expectedColumns = new[] { 
            "RedemptionLink", "LinkClickDate", "LinkClickCount", "RecipientPhone",
            "RecipientName", "LinkSentDate", "LinkSentVia", "LinkDelivered", "LastClickIpAddress" 
        };
        
        var missingColumns = expectedColumns.Except(foundColumns).ToList();
        if (missingColumns.Any())
        {
            Console.WriteLine($"\n❌ Missing columns: {string.Join(", ", missingColumns)}");
        }
        else
        {
            Console.WriteLine("\n✅ All link-related columns are present!");
        }
    }
    
    Console.WriteLine("\n🎉 Migration status check completed!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}