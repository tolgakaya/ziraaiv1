#r "nuget: Npgsql, 8.0.4"
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔧 Assigning Sponsor role to sponsor-test-2025@example.com...");
    
    // Get user ID
    var getUserQuery = @"SELECT ""UserId"" FROM ""Users"" WHERE ""Email"" = 'sponsor-test-2025@example.com'";
    using var getUserCmd = new NpgsqlCommand(getUserQuery, connection);
    var userId = await getUserCmd.ExecuteScalarAsync();
    
    if (userId == null)
    {
        Console.WriteLine("❌ User not found");
        return;
    }
    
    Console.WriteLine($"✅ Found user ID: {userId}");
    
    // Get Sponsor group ID
    var getSponsorGroupQuery = @"SELECT ""Id"" FROM ""Groups"" WHERE ""GroupName"" = 'Sponsor'";
    using var getSponsorGroupCmd = new NpgsqlCommand(getSponsorGroupQuery, connection);
    var sponsorGroupId = await getSponsorGroupCmd.ExecuteScalarAsync();
    
    Console.WriteLine($"✅ Found Sponsor group ID: {sponsorGroupId}");
    
    // Remove Farmer role if exists
    var removeFarmerQuery = @"
        DELETE FROM ""UserGroups"" 
        WHERE ""UserId"" = @userId 
        AND ""GroupId"" = (SELECT ""Id"" FROM ""Groups"" WHERE ""GroupName"" = 'Farmer')";
    using var removeFarmerCmd = new NpgsqlCommand(removeFarmerQuery, connection);
    removeFarmerCmd.Parameters.AddWithValue("userId", userId);
    await removeFarmerCmd.ExecuteNonQueryAsync();
    
    // Add Sponsor role
    var addSponsorQuery = @"
        INSERT INTO ""UserGroups"" (""UserId"", ""GroupId"") 
        VALUES (@userId, @sponsorGroupId)
        ON CONFLICT (""UserId"", ""GroupId"") DO NOTHING";
    using var addSponsorCmd = new NpgsqlCommand(addSponsorQuery, connection);
    addSponsorCmd.Parameters.AddWithValue("userId", userId);
    addSponsorCmd.Parameters.AddWithValue("sponsorGroupId", sponsorGroupId);
    await addSponsorCmd.ExecuteNonQueryAsync();
    
    Console.WriteLine("✅ Sponsor role assigned successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}