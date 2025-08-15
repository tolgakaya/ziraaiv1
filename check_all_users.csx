#r "nuget: Npgsql, 8.0.4"
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("👥 Recent users in database:");
    
    var query = @"
        SELECT ""UserId"", ""FullName"", ""Email"", ""RecordDate""
        FROM ""Users""
        ORDER BY ""RecordDate"" DESC
        LIMIT 10";
    
    using var cmd = new NpgsqlCommand(query, connection);
    using var reader = await cmd.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"ID: {reader["UserId"]}, Name: {reader["FullName"]}, Email: {reader["Email"]}, Date: {reader["RecordDate"]}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}