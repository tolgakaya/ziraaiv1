#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;
using System.Threading.Tasks;

Console.WriteLine("🚀 Adding EmailAlreadyExists translations to database...");

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Connected to database successfully");

    // Check if translations already exist
    var checkSql = @"SELECT COUNT(*) FROM ""Translates"" WHERE ""Code"" = 'EmailAlreadyExists'";
    await using var checkCmd = new NpgsqlCommand(checkSql, connection);
    var existingCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
    
    if (existingCount > 0)
    {
        Console.WriteLine($"✅ EmailAlreadyExists translations already exist ({existingCount} entries)");
        return;
    }

    // Insert Turkish translation (LangId = 1)
    var insertTurkishSql = @"
        INSERT INTO ""Translates"" (""Id"", ""LangId"", ""Code"", ""Value"") 
        VALUES (139, 1, 'EmailAlreadyExists', 'Bu e-posta adresi ile zaten bir hesap mevcut.')";
    
    // Insert English translation (LangId = 2)
    var insertEnglishSql = @"
        INSERT INTO ""Translates"" (""Id"", ""LangId"", ""Code"", ""Value"") 
        VALUES (140, 2, 'EmailAlreadyExists', 'An account with this email address already exists.')";

    await using var turkishCmd = new NpgsqlCommand(insertTurkishSql, connection);
    await turkishCmd.ExecuteNonQueryAsync();
    Console.WriteLine("✅ Turkish translation added");

    await using var englishCmd = new NpgsqlCommand(insertEnglishSql, connection);
    await englishCmd.ExecuteNonQueryAsync();
    Console.WriteLine("✅ English translation added");

    // Verify the insertions
    var verifySql = @"SELECT ""LangId"", ""Code"", ""Value"" FROM ""Translates"" WHERE ""Code"" = 'EmailAlreadyExists' ORDER BY ""LangId""";
    await using var verifyCmd = new NpgsqlCommand(verifySql, connection);
    await using var reader = await verifyCmd.ExecuteReaderAsync();
    
    Console.WriteLine("\n📋 Verification of inserted translations:");
    while (await reader.ReadAsync())
    {
        var langId = reader.GetInt32(0);
        var code = reader.GetString(1);
        var value = reader.GetString(2);
        var language = langId == 1 ? "Turkish" : "English";
        Console.WriteLine($"  {language} ({langId}): {value}");
    }

    Console.WriteLine("\n🎉 EmailAlreadyExists translations added successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
    }
}