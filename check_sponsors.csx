#r "nuget: Npgsql, 8.0.4"
using Npgsql;
using System;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 Checking for existing sponsor users...");
    
    var query = @"
        SELECT u.""UserId"", u.""FullName"", u.""Email"", g.""GroupName""
        FROM ""Users"" u
        JOIN ""UserGroups"" ug ON u.""UserId"" = ug.""UserId""
        JOIN ""Groups"" g ON ug.""GroupId"" = g.""Id""
        WHERE g.""GroupName"" = 'Sponsor' AND u.""Status"" = true
        LIMIT 5";
    
    using var cmd = new NpgsqlCommand(query, connection);
    using var reader = await cmd.ExecuteReaderAsync();
    
    var sponsorFound = false;
    while (await reader.ReadAsync())
    {
        sponsorFound = true;
        Console.WriteLine($"✅ Sponsor User ID: {reader["UserId"]}, Name: {reader["FullName"]}, Email: {reader["Email"]}");
    }
    
    if (!sponsorFound)
    {
        Console.WriteLine("❌ No sponsor users found. Need to create test sponsor.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}