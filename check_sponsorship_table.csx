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
    
    // SponsorshipPurchases tablosunun yapısını kontrol et
    var tableInfoQuery = @"
        SELECT column_name, data_type, is_nullable
        FROM information_schema.columns 
        WHERE table_name = 'SponsorshipPurchases' 
        ORDER BY ordinal_position;";
    
    using var command = new NpgsqlCommand(tableInfoQuery, connection);
    using var reader = await command.ExecuteReaderAsync();
    
    Console.WriteLine("\n📋 SponsorshipPurchases Table Structure:");
    Console.WriteLine("Column Name          | Data Type        | Nullable");
    Console.WriteLine("---------------------|------------------|----------");
    
    while (await reader.ReadAsync())
    {
        var columnName = reader.GetString(0);
        var dataType = reader.GetString(1);
        var isNullable = reader.GetString(2);
        
        Console.WriteLine($"{columnName,-20} | {dataType,-16} | {isNullable}");
    }
    
    await connection.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}