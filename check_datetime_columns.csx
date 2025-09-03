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
    
    // Check all datetime columns in SponsorProfiles table
    var columnsSql = @"
        SELECT column_name, data_type, is_nullable
        FROM information_schema.columns
        WHERE table_name = 'SponsorProfiles' 
        AND data_type LIKE '%timestamp%'
        ORDER BY ordinal_position;";
    
    using var columnsCommand = new NpgsqlCommand(columnsSql, connection);
    using var reader = await columnsCommand.ExecuteReaderAsync();
    
    Console.WriteLine("\n📋 DateTime columns in SponsorProfiles table:");
    Console.WriteLine("Column Name              | Data Type                        | Nullable");
    Console.WriteLine("-------------------------|----------------------------------|----------");
    
    while (await reader.ReadAsync())
    {
        var columnName = reader.GetString(0);
        var dataType = reader.GetString(1);
        var isNullable = reader.GetString(2);
        
        Console.WriteLine($"{columnName,-25} | {dataType,-32} | {isNullable}");
    }
    
    await connection.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

Console.WriteLine("\n🚀 Database inspection completed!");