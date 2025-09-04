#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using System;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("✅ Connected to PostgreSQL database successfully.");
    
    // Check SponsorProfiles table structure
    var tableSql = @"
        SELECT column_name, data_type, is_nullable, column_default
        FROM information_schema.columns
        WHERE table_name = 'SponsorProfiles'
        ORDER BY ordinal_position;";
    
    using var tableCommand = new NpgsqlCommand(tableSql, connection);
    using var reader = await tableCommand.ExecuteReaderAsync();
    
    Console.WriteLine("\n📋 SponsorProfiles table structure:");
    Console.WriteLine("Column Name              | Data Type            | Nullable | Default");
    Console.WriteLine("-------------------------|---------------------|----------|--------");
    
    var columnExists = false;
    while (await reader.ReadAsync())
    {
        columnExists = true;
        var columnName = reader.GetString(0);
        var dataType = reader.GetString(1);
        var isNullable = reader.GetString(2);
        var defaultValue = reader.IsDBNull(3) ? "NULL" : reader.GetString(3);
        
        Console.WriteLine($"{columnName,-25} | {dataType,-20} | {isNullable,-8} | {defaultValue}");
        
        // Check for problematic columns
        if (columnName == "DataAccessLevel" || columnName == "VisibilityLevel")
        {
            Console.WriteLine($"⚠️  Found deprecated column: {columnName}");
        }
    }
    
    if (!columnExists)
    {
        Console.WriteLine("❌ SponsorProfiles table not found or has no columns.");
    }
    
    await connection.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

Console.WriteLine("\n🚀 Database inspection completed!");