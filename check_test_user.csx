#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Connected to staging database successfully");

    // List all tables first
    using var tablesCmd = new NpgsqlCommand(@"
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
        ORDER BY table_name
    ", connection);
    
    using var tablesReader = await tablesCmd.ExecuteReaderAsync();
    Console.WriteLine("\n📋 Available tables:");
    while (await tablesReader.ReadAsync())
    {
        Console.WriteLine($"   - {tablesReader.GetString(0)}");
    }
    tablesReader.Close();
    
    // Check Users table structure
    using var structureCmd = new NpgsqlCommand(@"
        SELECT column_name, data_type 
        FROM information_schema.columns 
        WHERE table_name = 'Users' AND table_schema = 'public'
        ORDER BY ordinal_position
    ", connection);
    
    using var structureReader = await structureCmd.ExecuteReaderAsync();
    Console.WriteLine("\n📋 Users table columns:");
    while (await structureReader.ReadAsync())
    {
        Console.WriteLine($"   - {structureReader.GetString(0)} ({structureReader.GetString(1)})");
    }
    structureReader.Close();
    
    // Check if any test users exist (using correct column names)
    using var cmd = new NpgsqlCommand(@"
        SELECT * FROM ""Users"" LIMIT 3
    ", connection);

    using var reader = await cmd.ExecuteReaderAsync();
    
    Console.WriteLine("\n🔍 Found test users in staging database:");
    Console.WriteLine("ID | Name                 | Email                    | Tier   | Active");
    Console.WriteLine("---|----------------------|--------------------------|--------|--------");
    
    var userCount = 0;
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var firstName = reader.IsDBNull(1) ? "" : reader.GetString(1);
        var lastName = reader.IsDBNull(2) ? "" : reader.GetString(2);
        var email = reader.GetString(3);
        var tier = reader.IsDBNull(4) ? "None" : reader.GetString(4);
        var hasActiveSubscription = reader.IsDBNull(5) ? false : reader.GetBoolean(5);
        
        var name = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrEmpty(name)) name = "N/A";
        
        Console.WriteLine($"{id,2} | {name,-20} | {email,-24} | {tier,-6} | {hasActiveSubscription}");
        userCount++;
    }
    
    if (userCount == 0)
    {
        Console.WriteLine("No test users found. We need to create a test user for security validation.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
    }
}