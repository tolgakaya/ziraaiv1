#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;
using System.Threading.Tasks;

Console.WriteLine("Checking subscription tiers...");

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    var sql = @"SELECT ""Id"", ""TierName"", ""DisplayName"", ""MonthlyPrice"" FROM ""SubscriptionTiers"" ORDER BY ""Id""";
    await using var cmd = new NpgsqlCommand(sql, connection);
    await using var reader = await cmd.ExecuteReaderAsync();

    Console.WriteLine("Subscription Tiers:");
    Console.WriteLine("ID | TierName | DisplayName | MonthlyPrice");
    Console.WriteLine("---|----------|-------------|-------------");

    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var tierName = reader.GetString(1);
        var displayName = reader.GetString(2);
        var monthlyPrice = reader.GetDecimal(3);
        Console.WriteLine($"{id} | {tierName} | {displayName} | {monthlyPrice}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}