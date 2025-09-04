#r "nuget: Npgsql, 8.0.4"
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ PostgreSQL connection successful!");
    
    var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"SponsorshipCodes\"", connection);
    var count = await cmd.ExecuteScalarAsync();
    Console.WriteLine($"📊 SponsorshipCodes table has {count} records");
    
} catch (Exception ex) {
    Console.WriteLine($"❌ Connection failed: {ex.Message}");
}