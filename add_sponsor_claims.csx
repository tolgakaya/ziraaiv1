#r "nuget: Npgsql, 8.0.4"
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔧 Adding required claims to Sponsor group...");
    
    // Get Sponsor group ID
    var getSponsorGroupQuery = @"SELECT ""Id"" FROM ""Groups"" WHERE ""GroupName"" = 'Sponsor'";
    using var getSponsorGroupCmd = new NpgsqlCommand(getSponsorGroupQuery, connection);
    var sponsorGroupId = await getSponsorGroupCmd.ExecuteScalarAsync();
    
    if (sponsorGroupId == null)
    {
        Console.WriteLine("❌ Sponsor group not found!");
        return;
    }
    
    Console.WriteLine($"✅ Found Sponsor group ID: {sponsorGroupId}");
    
    // Claims that Sponsor group should have
    var sponsorClaims = new[] 
    {
        "SendSponsorshipLinkCommand",
        "GetLinkStatisticsQuery",
        "CreateSponsorshipCodeCommand",
        "GetSponsorshipCodesQuery",
        "GetSponsorshipPurchasesQuery",
        "GetSponsoredFarmersQuery",
        "GetSponsorshipStatisticsQuery"
    };
    
    foreach (var claimName in sponsorClaims)
    {
        // Get claim ID
        var getClaimIdQuery = @"SELECT ""Id"" FROM ""OperationClaims"" WHERE ""Name"" = @claimName";
        using var getClaimIdCmd = new NpgsqlCommand(getClaimIdQuery, connection);
        getClaimIdCmd.Parameters.AddWithValue("claimName", claimName);
        var claimId = await getClaimIdCmd.ExecuteScalarAsync();
        
        if (claimId == null)
        {
            Console.WriteLine($"⚠️ Claim '{claimName}' not found in OperationClaims table");
            continue;
        }
        
        // Check if group claim already exists
        var checkExistsQuery = @"SELECT COUNT(*) FROM ""GroupClaims"" WHERE ""GroupId"" = @groupId AND ""ClaimId"" = @claimId";
        using var checkCmd = new NpgsqlCommand(checkExistsQuery, connection);
        checkCmd.Parameters.AddWithValue("groupId", sponsorGroupId);
        checkCmd.Parameters.AddWithValue("claimId", claimId);
        var exists = (long)await checkCmd.ExecuteScalarAsync() > 0;
        
        if (exists)
        {
            Console.WriteLine($"✅ Claim '{claimName}' already exists for Sponsor group");
            continue;
        }
        
        // Add group claim
        var addGroupClaimQuery = @"INSERT INTO ""GroupClaims"" (""GroupId"", ""ClaimId"") VALUES (@groupId, @claimId)";
        using var addCmd = new NpgsqlCommand(addGroupClaimQuery, connection);
        addCmd.Parameters.AddWithValue("groupId", sponsorGroupId);
        addCmd.Parameters.AddWithValue("claimId", claimId);
        await addCmd.ExecuteNonQueryAsync();
        
        Console.WriteLine($"✅ Added claim '{claimName}' to Sponsor group");
    }
    
    Console.WriteLine("\n🎉 All required claims added to Sponsor group!");
    
    // Verify by checking claims again
    var verifyQuery = @"
        SELECT oc.""Name"" as ClaimName
        FROM ""GroupClaims"" gc
        JOIN ""OperationClaims"" oc ON gc.""ClaimId"" = oc.""Id""
        WHERE gc.""GroupId"" = @groupId
        ORDER BY oc.""Name""";
    using var verifyCmd = new NpgsqlCommand(verifyQuery, connection);
    verifyCmd.Parameters.AddWithValue("groupId", sponsorGroupId);
    using var reader = await verifyCmd.ExecuteReaderAsync();
    
    Console.WriteLine("\n📋 Current Sponsor group claims:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  - {reader["ClaimName"]}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}