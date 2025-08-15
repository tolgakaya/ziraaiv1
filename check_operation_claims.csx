#r "nuget: Npgsql, 8.0.4"
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 Checking existing operation claims...");
    
    // Get all operation claims
    var claimsQuery = @"SELECT ""Id"", ""Name"" FROM ""OperationClaims"" ORDER BY ""Name""";
    using var claimsCmd = new NpgsqlCommand(claimsQuery, connection);
    using var reader = await claimsCmd.ExecuteReaderAsync();
    
    Console.WriteLine("📋 Operation Claims in database:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  ID {reader["Id"]}: {reader["Name"]}");
    }
    
    await reader.CloseAsync();
    
    // Check if SendSponsorshipLinkCommand claim exists
    var checkClaimQuery = @"SELECT COUNT(*) FROM ""OperationClaims"" WHERE ""Name"" = 'SendSponsorshipLinkCommand'";
    using var checkCmd = new NpgsqlCommand(checkClaimQuery, connection);
    var exists = (long)await checkCmd.ExecuteScalarAsync() > 0;
    
    Console.WriteLine($"\n🔍 SendSponsorshipLinkCommand claim exists: {exists}");
    
    if (!exists)
    {
        Console.WriteLine("❌ SendSponsorshipLinkCommand claim is missing! This is why SecuredOperation fails.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}