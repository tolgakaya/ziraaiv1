#r "nuget: Npgsql, 8.0.4"
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 Checking roles for test-sponsor@example.com (ID: 33):");
    
    var query = @"
        SELECT g.""GroupName"" 
        FROM ""UserGroups"" ug
        JOIN ""Groups"" g ON ug.""GroupId"" = g.""Id""
        WHERE ug.""UserId"" = 33";
    
    using var cmd = new NpgsqlCommand(query, connection);
    using var reader = await cmd.ExecuteReaderAsync();
    
    var hasRoles = false;
    while (await reader.ReadAsync())
    {
        hasRoles = true;
        Console.WriteLine($"✅ Role: {reader["GroupName"]}");
    }
    
    if (!hasRoles)
    {
        Console.WriteLine("❌ No roles found for this user");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}