#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔧 FIXING SUBSCRIPTION USAGE LOGS TABLE:");
    Console.WriteLine(new string('=', 60));
    
    // Add missing CreatedDate column
    Console.WriteLine("1. Adding missing CreatedDate column...");
    var addCreatedDateSql = @"
        ALTER TABLE ""SubscriptionUsageLogs"" 
        ADD COLUMN IF NOT EXISTS ""CreatedDate"" timestamp without time zone DEFAULT CURRENT_TIMESTAMP";
    
    await using var addCreatedDateCmd = new NpgsqlCommand(addCreatedDateSql, connection);
    await addCreatedDateCmd.ExecuteNonQueryAsync();
    Console.WriteLine("✅ CreatedDate column added successfully");
    
    // Fix ResponseStatus column type (from integer to varchar)
    Console.WriteLine("\n2. Fixing ResponseStatus column type...");
    try
    {
        var fixResponseStatusSql = @"
            ALTER TABLE ""SubscriptionUsageLogs"" 
            ALTER COLUMN ""ResponseStatus"" TYPE character varying(50)";
        
        await using var fixResponseStatusCmd = new NpgsqlCommand(fixResponseStatusSql, connection);
        await fixResponseStatusCmd.ExecuteNonQueryAsync();
        Console.WriteLine("✅ ResponseStatus column type fixed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ ResponseStatus column fix failed (might already be correct): {ex.Message}");
    }
    
    // Test the fixed structure
    Console.WriteLine("\n3. Testing fixed table structure...");
    
    try
    {
        // Get a valid UserSubscriptionId
        var getSubscriptionSql = @"SELECT ""Id"" FROM ""UserSubscriptions"" WHERE ""IsActive"" = true LIMIT 1";
        await using var getSubCmd = new NpgsqlCommand(getSubscriptionSql, connection);
        var subscriptionIdObj = await getSubCmd.ExecuteScalarAsync();
        
        if (subscriptionIdObj == null)
        {
            Console.WriteLine("❌ No active subscriptions found for test");
            return;
        }
        
        var subscriptionId = (int)subscriptionIdObj;
        Console.WriteLine($"Using subscription ID: {subscriptionId}");
        
        var testInsertSql = @"
            INSERT INTO ""SubscriptionUsageLogs"" (
                ""UserId"", ""UserSubscriptionId"", ""UsageType"", ""UsageDate"",
                ""RequestEndpoint"", ""RequestMethod"", ""IsSuccessful"", ""ResponseStatus"",
                ""DailyQuotaUsed"", ""DailyQuotaLimit"", ""MonthlyQuotaUsed"", ""MonthlyQuotaLimit"",
                ""CreatedDate""
            ) VALUES (
                @userId, @subscriptionId, @usageType, @usageDate,
                @endpoint, @method, @isSuccessful, @responseStatus,
                @dailyUsed, @dailyLimit, @monthlyUsed, @monthlyLimit,
                @createdDate
            ) RETURNING ""Id""";
        
        var now = DateTime.Now;
        
        await using var testInsertCmd = new NpgsqlCommand(testInsertSql, connection);
        testInsertCmd.Parameters.AddWithValue("userId", 16);
        testInsertCmd.Parameters.AddWithValue("subscriptionId", subscriptionId);
        testInsertCmd.Parameters.AddWithValue("usageType", "PlantAnalysis");
        testInsertCmd.Parameters.AddWithValue("usageDate", now);
        testInsertCmd.Parameters.AddWithValue("endpoint", "/api/v1/plantanalyses/analyze");
        testInsertCmd.Parameters.AddWithValue("method", "POST");
        testInsertCmd.Parameters.AddWithValue("isSuccessful", false);
        testInsertCmd.Parameters.AddWithValue("responseStatus", "Test");
        testInsertCmd.Parameters.AddWithValue("dailyUsed", 0);
        testInsertCmd.Parameters.AddWithValue("dailyLimit", 1);
        testInsertCmd.Parameters.AddWithValue("monthlyUsed", 0);
        testInsertCmd.Parameters.AddWithValue("monthlyLimit", 30);
        testInsertCmd.Parameters.AddWithValue("createdDate", now);
        
        var newId = (int)await testInsertCmd.ExecuteScalarAsync();
        Console.WriteLine($"✅ TEST INSERT SUCCESSFUL - New ID: {newId}");
        
        // Clean up test record
        var deleteSql = @"DELETE FROM ""SubscriptionUsageLogs"" WHERE ""Id"" = @id";
        await using var deleteCmd = new NpgsqlCommand(deleteSql, connection);
        deleteCmd.Parameters.AddWithValue("id", newId);
        await deleteCmd.ExecuteNonQueryAsync();
        Console.WriteLine($"✅ Test record cleaned up");
    }
    catch (Exception testEx)
    {
        Console.WriteLine($"❌ TEST INSERT STILL FAILING:");
        Console.WriteLine($"   Error: {testEx.Message}");
        
        var innerEx = testEx.InnerException;
        var level = 1;
        while (innerEx != null)
        {
            Console.WriteLine($"   Inner Exception {level}: {innerEx.Message}");
            innerEx = innerEx.InnerException;
            level++;
        }
    }
    
    Console.WriteLine("\n" + new string('=', 60));
    Console.WriteLine("✅ SubscriptionUsageLogs table fix completed!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database connection error: {ex.Message}");
}