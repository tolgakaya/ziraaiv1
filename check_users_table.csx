#r "nuget: Npgsql, 8.0.4"
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 Checking Users table structure...");
    
    // Get table columns
    var columnsQuery = @"
        SELECT column_name, data_type 
        FROM information_schema.columns 
        WHERE table_name = 'Users' 
        ORDER BY ordinal_position";
    
    using var columnsCmd = new NpgsqlCommand(columnsQuery, connection);
    using var reader = await columnsCmd.ExecuteReaderAsync();
    
    Console.WriteLine("📋 Users table columns:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  - {reader["column_name"]} ({reader["data_type"]})");
    }
    
    await reader.CloseAsync();
    
    // Check if we have any users
    var countQuery = @"SELECT COUNT(*) FROM ""Users""";
    using var countCmd = new NpgsqlCommand(countQuery, connection);
    var count = await countCmd.ExecuteScalarAsync();
    
    Console.WriteLine($"\n📊 Total users in table: {count}");
    
    // Get a few users
    var usersQuery = @"SELECT ""UserId"", ""FullName"", ""Email"" FROM ""Users"" ORDER BY ""UserId"" LIMIT 10";
    using var usersCmd = new NpgsqlCommand(usersQuery, connection);
    using var usersReader = await usersCmd.ExecuteReaderAsync();
    
    Console.WriteLine("\n📋 Sample users:");
    while (await usersReader.ReadAsync())
    {
        var id = usersReader["UserId"];
        var email = usersReader.IsDBNull(usersReader.GetOrdinal("Email")) ? "null" : usersReader["Email"];
        var fullName = usersReader.IsDBNull(usersReader.GetOrdinal("FullName")) ? "null" : usersReader["FullName"];
        
        Console.WriteLine($"  ID {id}: {fullName} ({email})");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}