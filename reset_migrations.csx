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
    
    // Clear migrations history
    var clearHistorySql = @"DELETE FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" LIKE '%Sponsor%';";
    
    using var clearCommand = new NpgsqlCommand(clearHistorySql, connection);
    var deletedRows = await clearCommand.ExecuteNonQueryAsync();
    
    Console.WriteLine($"🗑️ Cleared {deletedRows} sponsorship-related migration entries.");
    
    await connection.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

Console.WriteLine("🚀 Migration history reset completed!");