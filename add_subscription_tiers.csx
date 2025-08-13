#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("✅ Connected to staging database successfully");
    
    // Define subscription tiers
    var tiers = new[]
    {
        new { Name = "Trial", Display = "Trial", Desc = "30-day trial with limited access", Daily = 1, Monthly = 30, Price = 0.00m, YearPrice = 0.00m, Order = 0, Hours = 72, Priority = false, Analytics = false, Api = false, Features = "[\"Basic plant analysis\",\"Email notifications\",\"Trial access\"]" },
        new { Name = "S", Display = "Small", Desc = "Perfect for small farms and hobbyists", Daily = 5, Monthly = 50, Price = 99.99m, YearPrice = 999.99m, Order = 1, Hours = 48, Priority = false, Analytics = false, Api = false, Features = "[\"Basic plant analysis\",\"Email notifications\",\"Basic reports\"]" },
        new { Name = "M", Display = "Medium", Desc = "Ideal for medium-sized farms", Daily = 20, Monthly = 200, Price = 299.99m, YearPrice = 2999.99m, Order = 2, Hours = 24, Priority = false, Analytics = true, Api = false, Features = "[\"Advanced plant analysis\",\"Email & SMS notifications\",\"Detailed reports\",\"Historical data access\",\"Basic API access\"]" },
        new { Name = "L", Display = "Large", Desc = "Best for large commercial farms", Daily = 50, Monthly = 500, Price = 599.99m, YearPrice = 5999.99m, Order = 3, Hours = 12, Priority = true, Analytics = true, Api = true, Features = "[\"Premium plant analysis with AI insights\",\"All notification channels\",\"Custom reports\",\"Full historical data\",\"Full API access\",\"Priority support\",\"Export capabilities\"]" },
        new { Name = "XL", Display = "Extra Large", Desc = "Enterprise solution for agricultural corporations", Daily = 200, Monthly = 2000, Price = 1499.99m, YearPrice = 14999.99m, Order = 4, Hours = 6, Priority = true, Analytics = true, Api = true, Features = "[\"Enterprise AI analysis with custom models\",\"All features included\",\"Dedicated support team\",\"Custom integrations\",\"White-label options\",\"SLA guarantee\",\"Training sessions\",\"Unlimited data retention\"]" }
    };
    
    foreach (var tier in tiers)
    {
        // Check if tier exists
        var checkSql = "SELECT COUNT(*) FROM \"SubscriptionTiers\" WHERE \"TierName\" = @tierName";
        await using var checkCmd = new NpgsqlCommand(checkSql, connection);
        checkCmd.Parameters.AddWithValue("tierName", tier.Name);
        
        var count = (long)await checkCmd.ExecuteScalarAsync();
        
        if (count == 0)
        {
            var insertSql = @"
                INSERT INTO ""SubscriptionTiers"" (
                    ""TierName"", ""DisplayName"", ""Description"", 
                    ""DailyRequestLimit"", ""MonthlyRequestLimit"", 
                    ""MonthlyPrice"", ""YearlyPrice"", ""Currency"",
                    ""PrioritySupport"", ""AdvancedAnalytics"", ""ApiAccess"", 
                    ""ResponseTimeHours"", ""AdditionalFeatures"",
                    ""IsActive"", ""DisplayOrder"", ""CreatedDate""
                ) VALUES (
                    @tierName, @displayName, @description,
                    @dailyLimit, @monthlyLimit,
                    @monthlyPrice, @yearlyPrice, @currency,
                    @prioritySupport, @advancedAnalytics, @apiAccess,
                    @responseTimeHours, @additionalFeatures,
                    @isActive, @displayOrder, @createdDate
                )";
                
            await using var insertCmd = new NpgsqlCommand(insertSql, connection);
            insertCmd.Parameters.AddWithValue("tierName", tier.Name);
            insertCmd.Parameters.AddWithValue("displayName", tier.Display);
            insertCmd.Parameters.AddWithValue("description", tier.Desc);
            insertCmd.Parameters.AddWithValue("dailyLimit", tier.Daily);
            insertCmd.Parameters.AddWithValue("monthlyLimit", tier.Monthly);
            insertCmd.Parameters.AddWithValue("monthlyPrice", tier.Price);
            insertCmd.Parameters.AddWithValue("yearlyPrice", tier.YearPrice);
            insertCmd.Parameters.AddWithValue("currency", "TRY");
            insertCmd.Parameters.AddWithValue("prioritySupport", tier.Priority);
            insertCmd.Parameters.AddWithValue("advancedAnalytics", tier.Analytics);
            insertCmd.Parameters.AddWithValue("apiAccess", tier.Api);
            insertCmd.Parameters.AddWithValue("responseTimeHours", tier.Hours);
            insertCmd.Parameters.AddWithValue("additionalFeatures", tier.Features);
            insertCmd.Parameters.AddWithValue("isActive", true);
            insertCmd.Parameters.AddWithValue("displayOrder", tier.Order);
            insertCmd.Parameters.AddWithValue("createdDate", DateTime.UtcNow);
            
            await insertCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✅ Added {tier.Name} tier successfully");
        }
        else
        {
            Console.WriteLine($"✅ {tier.Name} tier already exists");
        }
    }
    
    // Display final results
    var selectSql = @"SELECT ""Id"", ""TierName"", ""DisplayName"", ""DailyRequestLimit"", ""MonthlyRequestLimit"", ""MonthlyPrice"", ""IsActive""
                      FROM ""SubscriptionTiers"" 
                      ORDER BY ""DisplayOrder""";
    
    await using var selectCmd = new NpgsqlCommand(selectSql, connection);
    await using var reader = await selectCmd.ExecuteReaderAsync();
    
    Console.WriteLine($"\n🎉 Final subscription tiers in staging database:");
    Console.WriteLine("ID | Tier | Display Name     | Daily | Monthly | Price   | Active");
    Console.WriteLine("---|------|------------------|-------|---------|---------|-------");
    
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var tierName = reader.GetString(1);
        var displayName = reader.GetString(2);
        var dailyLimit = reader.GetInt32(3);
        var monthlyLimit = reader.GetInt32(4);
        var monthlyPrice = reader.GetDecimal(5);
        var isActive = reader.GetBoolean(6);
        
        Console.WriteLine($"{id,2} | {tierName,-4} | {displayName,-16} | {dailyLimit,5} | {monthlyLimit,7} | {monthlyPrice,7:F2} | {isActive}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}