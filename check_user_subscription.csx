#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Connected to staging database successfully");

    // User ID 45'in subscription durumunu kontrol et
    using var cmd = new NpgsqlCommand(@"
        SELECT 
            us.""Id"" as SubscriptionId,
            us.""UserId"",
            us.""PaymentMethod"",
            us.""PaymentReference"",
            us.""IsActive"",
            st.""TierName"",
            st.""DisplayName""
        FROM ""UserSubscriptions"" us
        INNER JOIN ""SubscriptionTiers"" st ON us.""SubscriptionTierId"" = st.""Id""
        WHERE us.""UserId"" = 45
        ORDER BY us.""CreatedDate"" DESC
    ", connection);

    using var reader = await cmd.ExecuteReaderAsync();
    
    Console.WriteLine("\n🔍 User 45 Subscription Status:");
    Console.WriteLine("ID | UserId | Tier | PaymentMethod | PaymentRef     | Active");
    Console.WriteLine("---|--------|------|---------------|----------------|--------");
    
    var found = false;
    while (await reader.ReadAsync())
    {
        found = true;
        var subscriptionId = reader.GetInt32(0);
        var userId = reader.GetInt32(1);
        var paymentMethod = reader.IsDBNull(2) ? "NULL" : reader.GetString(2);
        var paymentRef = reader.IsDBNull(3) ? "NULL" : reader.GetString(3);
        var isActive = reader.GetBoolean(4);
        var tierName = reader.GetString(5);
        var displayName = reader.GetString(6);
        
        Console.WriteLine($"{subscriptionId,2} | {userId,6} | {tierName,-4} | {paymentMethod,-13} | {paymentRef,-14} | {isActive}");
    }
    reader.Close();
    
    if (!found)
    {
        Console.WriteLine("❌ No subscription found for user 45");
        return;
    }

    // SponsorshipCodes table structure kontrol et
    using var structureCmd = new NpgsqlCommand(@"
        SELECT column_name, data_type 
        FROM information_schema.columns 
        WHERE table_name = 'SponsorshipCodes' AND table_schema = 'public'
        ORDER BY ordinal_position
    ", connection);
    
    using var structureReader = await structureCmd.ExecuteReaderAsync();
    Console.WriteLine("\n📋 SponsorshipCodes table columns:");
    while (await structureReader.ReadAsync())
    {
        Console.WriteLine($"   - {structureReader.GetString(0)} ({structureReader.GetString(1)})");
    }
    structureReader.Close();

    // PaymentReference'a göre sponsorship code'u kontrol et
    using var sponsorCmd = new NpgsqlCommand(@"
        SELECT 
            sc.""Id"" as CodeId,
            sc.""Code"",
            sc.""SponsorId"",
            sc.""IsUsed""
        FROM ""SponsorshipCodes"" sc
        WHERE sc.""Code"" = 'AGRI-2025-86652F89'
    ", connection);

    using var sponsorReader = await sponsorCmd.ExecuteReaderAsync();
    
    Console.WriteLine("\n🏷️ Sponsorship Code: AGRI-2025-86652F89");
    Console.WriteLine("CodeId | Code          | SponsorId | Used");
    Console.WriteLine("-------|---------------|-----------|------");
    
    var foundSponsor = false;
    while (await sponsorReader.ReadAsync())
    {
        foundSponsor = true;
        var codeId = sponsorReader.GetInt32(0);
        var code = sponsorReader.GetString(1);
        var sponsorId = sponsorReader.IsDBNull(2) ? "NULL" : sponsorReader.GetInt32(2).ToString();
        var isUsed = sponsorReader.GetBoolean(3);
        
        Console.WriteLine($"{codeId,6} | {code,-13} | {sponsorId,-9} | {isUsed}");
    }
    
    if (!foundSponsor)
    {
        Console.WriteLine("❌ No sponsorship code found: AGRI-2025-86652F89");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
    }
}