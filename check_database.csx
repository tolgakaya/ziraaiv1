#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;
using System.Threading.Tasks;

// Simple database connection test - no parameters needed
var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    Console.WriteLine("🔍 Quick Database Check");
    Console.WriteLine("=====================");
    Console.WriteLine($"🔗 Connection: localhost:5432/ziraai_dev");
    Console.WriteLine();
    
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("✅ Database connection successful");
    Console.WriteLine();
    
    // Check key tables exist
    Console.WriteLine("📋 Checking Key Tables...");
    var tables = new[] { "Users", "SponsorshipCodes", "UserSubscriptions", "SubscriptionTiers" };
    
    foreach (var table in tables)
    {
        var checkCmd = new NpgsqlCommand($@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name = '{table}'
            )", connection);
        
        var exists = (bool)await checkCmd.ExecuteScalarAsync();
        var status = exists ? "✅" : "❌";
        Console.WriteLine($"   {status} {table}: {(exists ? "Found" : "Missing")}");
    }
    
    // Quick count of recent data
    Console.WriteLine();
    Console.WriteLine("📊 Recent Data Counts...");
    
    try 
    {
        // Users count
        var usersCmd = new NpgsqlCommand(@"SELECT COUNT(*) FROM ""Users""", connection);
        var usersCount = (long)await usersCmd.ExecuteScalarAsync();
        Console.WriteLine($"   👤 Users: {usersCount}");
        
        // Sponsorship codes count
        var codesCmd = new NpgsqlCommand(@"SELECT COUNT(*) FROM ""SponsorshipCodes""", connection);
        var codesCount = (long)await codesCmd.ExecuteScalarAsync();
        Console.WriteLine($"   🎫 Sponsorship Codes: {codesCount}");
        
        // Recent codes (last 24 hours)
        var recentCmd = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM ""SponsorshipCodes"" 
            WHERE ""RecordDate"" > NOW() - INTERVAL '24 hours'", connection);
        var recentCount = (long)await recentCmd.ExecuteScalarAsync();
        Console.WriteLine($"   🔥 Recent Codes (24h): {recentCount}");
        
        // Subscription tiers
        var tiersCmd = new NpgsqlCommand(@"SELECT COUNT(*) FROM ""SubscriptionTiers""", connection);
        var tiersCount = (long)await tiersCmd.ExecuteScalarAsync();
        Console.WriteLine($"   🎯 Subscription Tiers: {tiersCount}");
        
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ⚠️  Data count failed: {ex.Message}");
    }
    
    // Check if link fields exist
    Console.WriteLine();
    Console.WriteLine("🔗 Checking Link Distribution Fields...");
    
    var linkFields = new[] { 
        "RedemptionLink", "LinkClickCount", "LinkClickDate", 
        "RecipientPhone", "LinkSentDate", "LinkSentVia" 
    };
    
    foreach (var field in linkFields)
    {
        try
        {
            var fieldCmd = new NpgsqlCommand($@"
                SELECT EXISTS (
                    SELECT FROM information_schema.columns 
                    WHERE table_name = 'SponsorshipCodes' 
                    AND column_name = '{field}'
                )", connection);
            
            var exists = (bool)await fieldCmd.ExecuteScalarAsync();
            var status = exists ? "✅" : "❌";
            Console.WriteLine($"   {status} {field}: {(exists ? "Present" : "Missing")}");
        }
        catch 
        {
            Console.WriteLine($"   ❌ {field}: Check failed");
        }
    }
    
    Console.WriteLine();
    Console.WriteLine("🎉 Database check completed!");
    Console.WriteLine();
    Console.WriteLine("💡 Usage:");
    Console.WriteLine("   For detailed verification: dotnet script verify_redemption.csx [CODE] [PHONE]");
    Console.WriteLine("   Example: dotnet script verify_redemption.csx SPONSOR-2025-ABC123 5551234567");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database connection failed: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("💡 Troubleshooting:");
    Console.WriteLine("   1. Check if PostgreSQL is running");
    Console.WriteLine("   2. Verify connection string: localhost:5432");
    Console.WriteLine("   3. Check database credentials: ziraai/devpass");
    Console.WriteLine("   4. Ensure database 'ziraai_dev' exists");
    Environment.Exit(1);
}