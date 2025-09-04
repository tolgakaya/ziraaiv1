#r "nuget: Npgsql, 8.0.4"
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔧 Assigning Sponsor role to User ID 34 (pg-sponsor@test.com)...");
    
    // Get Sponsor group ID
    var getSponsorGroupQuery = @"SELECT ""Id"" FROM ""Groups"" WHERE ""GroupName"" = 'Sponsor'";
    using var getSponsorGroupCmd = new NpgsqlCommand(getSponsorGroupQuery, connection);
    var sponsorGroupId = await getSponsorGroupCmd.ExecuteScalarAsync();
    
    Console.WriteLine($"✅ Found Sponsor group ID: {sponsorGroupId}");
    
    // Remove Farmer role
    var removeFarmerQuery = @"
        DELETE FROM ""UserGroups"" 
        WHERE ""UserId"" = 34 
        AND ""GroupId"" = (SELECT ""Id"" FROM ""Groups"" WHERE ""GroupName"" = 'Farmer')";
    using var removeFarmerCmd = new NpgsqlCommand(removeFarmerQuery, connection);
    await removeFarmerCmd.ExecuteNonQueryAsync();
    
    // Add Sponsor role
    var addSponsorQuery = @"
        INSERT INTO ""UserGroups"" (""UserId"", ""GroupId"") 
        VALUES (34, @sponsorGroupId)
        ON CONFLICT (""UserId"", ""GroupId"") DO NOTHING";
    using var addSponsorCmd = new NpgsqlCommand(addSponsorQuery, connection);
    addSponsorCmd.Parameters.AddWithValue("sponsorGroupId", sponsorGroupId);
    await addSponsorCmd.ExecuteNonQueryAsync();
    
    Console.WriteLine("✅ Sponsor role assigned successfully to User ID 34!");
    
    // Verify
    var verifyQuery = @"
        SELECT g.""GroupName"" 
        FROM ""UserGroups"" ug
        JOIN ""Groups"" g ON ug.""GroupId"" = g.""Id""
        WHERE ug.""UserId"" = 34";
    using var verifyCmd = new NpgsqlCommand(verifyQuery, connection);
    using var reader = await verifyCmd.ExecuteReaderAsync();
    
    Console.WriteLine("📋 Current roles for User ID 34:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  - {reader["GroupName"]}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}