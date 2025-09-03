#r "nuget: Npgsql, 8.0.4"
using Npgsql;
using System;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔧 Assigning Sponsor role to test user...");
    
    // First, get the user ID for test-sponsor@example.com
    var getUserQuery = @"SELECT ""UserId"" FROM ""Users"" WHERE ""Email"" = 'test-sponsor@example.com'";
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
    
    if (sponsorGroupId == null)
    {
        Console.WriteLine("❌ Sponsor group not found");
        return;
    }
    
    Console.WriteLine($"✅ Found Sponsor group ID: {sponsorGroupId}");
    
    // Remove existing Farmer role
    var removeFarmerQuery = @"
        DELETE FROM ""UserGroups"" 
        WHERE ""UserId"" = @userId 
        AND ""GroupId"" = (SELECT ""Id"" FROM ""Groups"" WHERE ""GroupName"" = 'Farmer')";
    using var removeFarmerCmd = new NpgsqlCommand(removeFarmerQuery, connection);
    removeFarmerCmd.Parameters.AddWithValue("userId", userId);
    await removeFarmerCmd.ExecuteNonQueryAsync();
    
    Console.WriteLine("✅ Removed Farmer role");
    
    // Add Sponsor role
    var addSponsorQuery = @"
        INSERT INTO ""UserGroups"" (""UserId"", ""GroupId"") 
        VALUES (@userId, @sponsorGroupId)
        ON CONFLICT (""UserId"", ""GroupId"") DO NOTHING";
    using var addSponsorCmd = new NpgsqlCommand(addSponsorQuery, connection);
    addSponsorCmd.Parameters.AddWithValue("userId", userId);
    addSponsorCmd.Parameters.AddWithValue("sponsorGroupId", sponsorGroupId);
    await addSponsorCmd.ExecuteNonQueryAsync();
    
    Console.WriteLine("✅ Added Sponsor role");
    
    // Verify the change
    var verifyQuery = @"
        SELECT g.""GroupName"" 
        FROM ""UserGroups"" ug
        JOIN ""Groups"" g ON ug.""GroupId"" = g.""Id""
        WHERE ug.""UserId"" = @userId";
    using var verifyCmd = new NpgsqlCommand(verifyQuery, connection);
    verifyCmd.Parameters.AddWithValue("userId", userId);
    using var reader = await verifyCmd.ExecuteReaderAsync();
    
    Console.WriteLine("📋 User roles after update:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  - {reader["GroupName"]}");
    }
    
    Console.WriteLine("\n✅ Role assignment completed! User must login again to get new token.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}