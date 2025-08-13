#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔧 ADDING MISSING RequestData COLUMN:");
    Console.WriteLine(new string('=', 50));
    
    // Add missing RequestData column
    Console.WriteLine("1. Adding missing RequestData column...");
    var addRequestDataSql = @"
        ALTER TABLE ""SubscriptionUsageLogs"" 
        ADD COLUMN IF NOT EXISTS ""RequestData"" character varying(4000)";
    
    await using var addRequestDataCmd = new NpgsqlCommand(addRequestDataSql, connection);
    await addRequestDataCmd.ExecuteNonQueryAsync();
    Console.WriteLine("✅ RequestData column added successfully");
    
    // Test insert to verify
    Console.WriteLine("\n2. Testing insert with RequestData column...");
    
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
                ""RequestData"", ""CreatedDate""
            ) VALUES (
                @userId, @subscriptionId, @usageType, @usageDate,
                @endpoint, @method, @isSuccessful, @responseStatus,
                @dailyUsed, @dailyLimit, @monthlyUsed, @monthlyLimit,
                @requestData, @createdDate
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
        testInsertCmd.Parameters.AddWithValue("responseStatus", "Test Debug");
        testInsertCmd.Parameters.AddWithValue("dailyUsed", 0);
        testInsertCmd.Parameters.AddWithValue("dailyLimit", 1);
        testInsertCmd.Parameters.AddWithValue("monthlyUsed", 0);
        testInsertCmd.Parameters.AddWithValue("monthlyLimit", 30);
        testInsertCmd.Parameters.AddWithValue("requestData", "Test RequestData");
        testInsertCmd.Parameters.AddWithValue("createdDate", now);
        
        var newId = (int)await testInsertCmd.ExecuteScalarAsync();
        Console.WriteLine($"✅ TEST INSERT WITH RequestData SUCCESSFUL - New ID: {newId}");
        
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
    
    Console.WriteLine("\n" + new string('=', 50));
    Console.WriteLine("✅ RequestData column fix completed!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database connection error: {ex.Message}");
}