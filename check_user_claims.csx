#r "nuget: Npgsql, 8.0.4"
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 Checking user claims for User ID 39 (testsponsor42@example.com)...");
    
    // Get user's direct claims
    var userClaimsQuery = @"
        SELECT uc.""UserId"", oc.""Name"" as ClaimName 
        FROM ""UserClaims"" uc
        JOIN ""OperationClaims"" oc ON uc.""ClaimId"" = oc.""Id""
        WHERE uc.""UserId"" = 39";
    using var userClaimsCmd = new NpgsqlCommand(userClaimsQuery, connection);
    using var userReader = await userClaimsCmd.ExecuteReaderAsync();
    
    Console.WriteLine("📋 Direct user claims:");
    bool hasDirectClaims = false;
    while (await userReader.ReadAsync())
    {
        hasDirectClaims = true;
        Console.WriteLine($"  - {userReader["ClaimName"]}");
    }
    if (!hasDirectClaims)
    {
        Console.WriteLine("  (No direct claims)");
    }
    
    await userReader.CloseAsync();
    
    // Get user's group-based claims
    var groupClaimsQuery = @"
        SELECT u.""UserId"", u.""FullName"", g.""GroupName"", oc.""Name"" as ClaimName
        FROM ""Users"" u
        JOIN ""UserGroups"" ug ON u.""UserId"" = ug.""UserId""
        JOIN ""Groups"" g ON ug.""GroupId"" = g.""Id""
        JOIN ""GroupClaims"" gc ON g.""Id"" = gc.""GroupId""
        JOIN ""OperationClaims"" oc ON gc.""ClaimId"" = oc.""Id""
        WHERE u.""UserId"" = 39
        ORDER BY g.""GroupName"", oc.""Name""";
    using var groupClaimsCmd = new NpgsqlCommand(groupClaimsQuery, connection);
    using var groupReader = await groupClaimsCmd.ExecuteReaderAsync();
    
    Console.WriteLine("\n📋 Group-based claims:");
    bool hasGroupClaims = false;
    bool hasSendSponsorshipLinkCommand = false;
    while (await groupReader.ReadAsync())
    {
        hasGroupClaims = true;
        var claimName = groupReader["ClaimName"].ToString();
        Console.WriteLine($"  {groupReader["GroupName"]} -> {claimName}");
        
        if (claimName == "SendSponsorshipLinkCommand")
        {
            hasSendSponsorshipLinkCommand = true;
        }
    }
    if (!hasGroupClaims)
    {
        Console.WriteLine("  (No group-based claims)");
    }
    
    await groupReader.CloseAsync();
    
    Console.WriteLine($"\n🔍 Has SendSponsorshipLinkCommand claim: {hasSendSponsorshipLinkCommand}");
    
    if (!hasSendSponsorshipLinkCommand)
    {
        Console.WriteLine("❌ User does not have SendSponsorshipLinkCommand claim!");
        Console.WriteLine("💡 Need to add this claim to Sponsor group or directly to user");
    }
    else
    {
        Console.WriteLine("✅ User has the required claim!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}