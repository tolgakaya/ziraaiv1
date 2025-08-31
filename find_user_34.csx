#r "nuget: Npgsql, 8.0.4"
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 Looking for User ID 34...");
    
    // Check if user 34 exists
    var checkQuery = @"SELECT ""UserId"", ""FullName"", ""Email"" FROM ""Users"" WHERE ""UserId"" = 34";
    using var checkCmd = new NpgsqlCommand(checkQuery, connection);
    using var reader = await checkCmd.ExecuteReaderAsync();
    
    if (await reader.ReadAsync())
    {
        Console.WriteLine("✅ Found User ID 34:");
        Console.WriteLine($"  Name: {reader["FullName"]}");
        Console.WriteLine($"  Email: {reader["Email"]}");
    }
    else
    {
        Console.WriteLine("❌ User ID 34 not found");
        await reader.CloseAsync();
        
        // Find highest user ID
        var maxQuery = @"SELECT MAX(""UserId"") FROM ""Users""";
        using var maxCmd = new NpgsqlCommand(maxQuery, connection);
        var maxId = await maxCmd.ExecuteScalarAsync();
        
        Console.WriteLine($"📊 Highest User ID: {maxId}");
        
        // Get the last few users
        var lastUsersQuery = @"SELECT ""UserId"", ""FullName"", ""Email"" FROM ""Users"" ORDER BY ""UserId"" DESC LIMIT 5";
        using var lastUsersCmd = new NpgsqlCommand(lastUsersQuery, connection);
        using var lastUsersReader = await lastUsersCmd.ExecuteReaderAsync();
        
        Console.WriteLine("\n📋 Last few users:");
        while (await lastUsersReader.ReadAsync())
        {
            var id = lastUsersReader["UserId"];
            var email = lastUsersReader.IsDBNull(lastUsersReader.GetOrdinal("Email")) ? "null" : lastUsersReader["Email"];
            var fullName = lastUsersReader.IsDBNull(lastUsersReader.GetOrdinal("FullName")) ? "null" : lastUsersReader["FullName"];
            
            Console.WriteLine($"  ID {id}: {fullName} ({email})");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}