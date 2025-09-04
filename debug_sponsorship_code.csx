#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;
using System.Threading.Tasks;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using (var connection = new NpgsqlConnection(connectionString))
    {
        await connection.OpenAsync();
        Console.WriteLine("✅ Connected to database successfully");

        // Get all sponsorship codes to debug
        var query = @"
            SELECT 
                sc.""Id"",
                sc.""Code"",
                sc.""SponsorId"",
                sc.""SubscriptionTierId"",
                sc.""SponsorshipPurchaseId"",
                sc.""IsUsed"",
                sc.""IsActive"",
                sc.""CreatedDate"",
                sc.""ExpiryDate"",
                sc.""UsedByUserId"",
                sc.""UsedDate"",
                st.""TierName"",
                st.""DisplayName"",
                CASE 
                    WHEN sc.""ExpiryDate"" > NOW() THEN 'Valid'
                    ELSE 'Expired'
                END as ""ExpiryStatus""
            FROM ""SponsorshipCodes"" sc
            LEFT JOIN ""SubscriptionTiers"" st ON sc.""SubscriptionTierId"" = st.""Id""
            ORDER BY sc.""CreatedDate"" DESC
            LIMIT 20;
        ";

        using (var cmd = new NpgsqlCommand(query, connection))
        {
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                Console.WriteLine("\n🔍 Recent Sponsorship Codes:");
                Console.WriteLine("ID | Code | Tier | IsUsed | IsActive | ExpiryStatus | CreatedDate");
                Console.WriteLine("---|------|------|--------|----------|-------------|------------");

                while (await reader.ReadAsync())
                {
                    var id = reader["Id"];
                    var code = reader["Code"];
                    var tierName = reader.IsDBNull(reader.GetOrdinal("TierName")) ? "NULL" : reader["TierName"];
                    var isUsed = reader["IsUsed"];
                    var isActive = reader["IsActive"];
                    var expiryStatus = reader["ExpiryStatus"];
                    var createdDate = ((DateTime)reader["CreatedDate"]).ToString("yyyy-MM-dd HH:mm");

                    Console.WriteLine($"{id} | {code} | {tierName} | {isUsed} | {isActive} | {expiryStatus} | {createdDate}");
                }
            }
        }

        // Get latest sponsorship purchases
        var purchaseQuery = @"
            SELECT 
                sp.""Id"",
                sp.""SponsorId"",
                sp.""SubscriptionTierId"",
                sp.""Quantity"",
                sp.""CodesGenerated"",
                sp.""CodesUsed"",
                sp.""CodePrefix"",
                sp.""ValidityDays"",
                sp.""CreatedDate"",
                st.""TierName""
            FROM ""SponsorshipPurchases"" sp
            LEFT JOIN ""SubscriptionTiers"" st ON sp.""SubscriptionTierId"" = st.""Id""
            ORDER BY sp.""CreatedDate"" DESC
            LIMIT 10;
        ";

        using (var cmd2 = new NpgsqlCommand(purchaseQuery, connection))
        {
            using (var reader2 = await cmd2.ExecuteReaderAsync())
            {
                Console.WriteLine("\n💰 Recent Sponsorship Purchases:");
                Console.WriteLine("ID | SponsorId | Tier | Quantity | Generated | Used | ValidityDays | CreatedDate");
                Console.WriteLine("---|-----------|------|----------|-----------|------|-------------|------------");

                while (await reader2.ReadAsync())
                {
                    var id = reader2["Id"];
                    var sponsorId = reader2["SponsorId"];
                    var tierName = reader2.IsDBNull(reader2.GetOrdinal("TierName")) ? "NULL" : reader2["TierName"];
                    var quantity = reader2["Quantity"];
                    var codesGenerated = reader2["CodesGenerated"];
                    var codesUsed = reader2["CodesUsed"];
                    var validityDays = reader2["ValidityDays"];
                    var createdDate = ((DateTime)reader2["CreatedDate"]).ToString("yyyy-MM-dd HH:mm");

                    Console.WriteLine($"{id} | {sponsorId} | {tierName} | {quantity} | {codesGenerated} | {codesUsed} | {validityDays} | {createdDate}");
                }
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
}