#r "nuget: Npgsql, 8.0.4"

using System;
using Npgsql;

try 
{
    var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";
    
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Connected to PostgreSQL database");
    
    // Check if DeepLinks table exists
    var checkTableQuery = @"
        SELECT EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_name = 'DeepLinks'
        );";
    
    using var checkCommand = new NpgsqlCommand(checkTableQuery, connection);
    var tableExists = (bool)await checkCommand.ExecuteScalarAsync();
    
    if (tableExists)
    {
        Console.WriteLine("✅ DeepLinks table exists");
        
        // Check table structure
        var columnsQuery = @"
            SELECT column_name, data_type, is_nullable 
            FROM information_schema.columns 
            WHERE table_name = 'DeepLinks'
            ORDER BY ordinal_position;";
        
        using var columnsCommand = new NpgsqlCommand(columnsQuery, connection);
        using var reader = await columnsCommand.ExecuteReaderAsync();
        
        Console.WriteLine("\n📋 DeepLinks table structure:");
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"  {reader["column_name"]} - {reader["data_type"]} ({reader["is_nullable"]})");
        }
    }
    else
    {
        Console.WriteLine("❌ DeepLinks table does not exist - migration needed");
    }
    
    // Check DeepLinkClickRecords table
    var checkClickTableQuery = @"
        SELECT EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_name = 'DeepLinkClickRecords'
        );";
    
    using var checkClickCommand = new NpgsqlCommand(checkClickTableQuery, connection);
    var clickTableExists = (bool)await checkClickCommand.ExecuteScalarAsync();
    
    if (clickTableExists)
    {
        Console.WriteLine("\n✅ DeepLinkClickRecords table exists");
    }
    else
    {
        Console.WriteLine("\n❌ DeepLinkClickRecords table does not exist - migration needed");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    throw;
}