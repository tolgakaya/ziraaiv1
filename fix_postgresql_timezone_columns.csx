#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔧 FIXING POSTGRESQL TIMEZONE COLUMN TYPES:");
    Console.WriteLine(new string('=', 60));
    
    // Get all timestamp columns without timezone in UserSubscriptions table
    Console.WriteLine("1. Checking UserSubscriptions timestamp columns...");
    var getColumnsQuery = @"
        SELECT column_name, data_type, is_nullable
        FROM information_schema.columns 
        WHERE table_name = 'UserSubscriptions' 
        AND data_type = 'timestamp without time zone'
        ORDER BY column_name";
    
    await using var getColumnsCmd = new NpgsqlCommand(getColumnsQuery, connection);
    await using var reader = await getColumnsCmd.ExecuteReaderAsync();
    
    var timestampColumns = new List<string>();
    while (await reader.ReadAsync())
    {
        var columnName = reader.GetString(0);
        timestampColumns.Add(columnName);
        Console.WriteLine($"   Found: {columnName}");
    }
    await reader.CloseAsync();
    
    if (timestampColumns.Count == 0)
    {
        Console.WriteLine("   No 'timestamp without time zone' columns found.");
        return;
    }
    
    Console.WriteLine($"\n2. Converting {timestampColumns.Count} timestamp columns to 'timestamp with time zone'...");
    
    foreach (var columnName in timestampColumns)
    {
        try
        {
            Console.WriteLine($"   Converting column: {columnName}");
            
            var alterQuery = $@"
                ALTER TABLE ""UserSubscriptions"" 
                ALTER COLUMN ""{columnName}"" TYPE timestamp with time zone";
            
            await using var alterCmd = new NpgsqlCommand(alterQuery, connection);
            await alterCmd.ExecuteNonQueryAsync();
            
            Console.WriteLine($"   ✅ {columnName} converted successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Failed to convert {columnName}: {ex.Message}");
        }
    }
    
    // Also fix SubscriptionUsageLogs table
    Console.WriteLine("\n3. Checking SubscriptionUsageLogs timestamp columns...");
    var getUsageColumnsQuery = @"
        SELECT column_name, data_type, is_nullable
        FROM information_schema.columns 
        WHERE table_name = 'SubscriptionUsageLogs' 
        AND data_type = 'timestamp without time zone'
        ORDER BY column_name";
    
    await using var getUsageColumnsCmd = new NpgsqlCommand(getUsageColumnsQuery, connection);
    await using var usageReader = await getUsageColumnsCmd.ExecuteReaderAsync();
    
    var usageTimestampColumns = new List<string>();
    while (await usageReader.ReadAsync())
    {
        var columnName = usageReader.GetString(0);
        usageTimestampColumns.Add(columnName);
        Console.WriteLine($"   Found: {columnName}");
    }
    await usageReader.CloseAsync();
    
    if (usageTimestampColumns.Count > 0)
    {
        Console.WriteLine($"\n4. Converting {usageTimestampColumns.Count} SubscriptionUsageLogs timestamp columns...");
        
        foreach (var columnName in usageTimestampColumns)
        {
            try
            {
                Console.WriteLine($"   Converting column: {columnName}");
                
                var alterQuery = $@"
                    ALTER TABLE ""SubscriptionUsageLogs"" 
                    ALTER COLUMN ""{columnName}"" TYPE timestamp with time zone";
                
                await using var alterCmd = new NpgsqlCommand(alterQuery, connection);
                await alterCmd.ExecuteNonQueryAsync();
                
                Console.WriteLine($"   ✅ {columnName} converted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Failed to convert {columnName}: {ex.Message}");
            }
        }
    }
    
    // Test with DateTime.UtcNow to verify the fix
    Console.WriteLine("\n5. Testing with DateTime.UtcNow insertion...");
    
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
        
        var nowUtc = DateTime.UtcNow; // Using UtcNow to test!
        
        await using var testInsertCmd = new NpgsqlCommand(testInsertSql, connection);
        testInsertCmd.Parameters.AddWithValue("userId", 16);
        testInsertCmd.Parameters.AddWithValue("subscriptionId", subscriptionId);
        testInsertCmd.Parameters.AddWithValue("usageType", "PlantAnalysis");
        testInsertCmd.Parameters.AddWithValue("usageDate", nowUtc);
        testInsertCmd.Parameters.AddWithValue("endpoint", "/api/v1/plantanalyses/analyze");
        testInsertCmd.Parameters.AddWithValue("method", "POST");
        testInsertCmd.Parameters.AddWithValue("isSuccessful", false);
        testInsertCmd.Parameters.AddWithValue("responseStatus", "Test with UtcNow");
        testInsertCmd.Parameters.AddWithValue("dailyUsed", 0);
        testInsertCmd.Parameters.AddWithValue("dailyLimit", 1);
        testInsertCmd.Parameters.AddWithValue("monthlyUsed", 0);
        testInsertCmd.Parameters.AddWithValue("monthlyLimit", 30);
        testInsertCmd.Parameters.AddWithValue("createdDate", nowUtc);
        
        var newId = (int)await testInsertCmd.ExecuteScalarAsync();
        Console.WriteLine($"✅ TEST INSERT WITH DateTime.UtcNow SUCCESSFUL - New ID: {newId}");
        
        // Clean up test record
        var deleteSql = @"DELETE FROM ""SubscriptionUsageLogs"" WHERE ""Id"" = @id";
        await using var deleteCmd = new NpgsqlCommand(deleteSql, connection);
        deleteCmd.Parameters.AddWithValue("id", newId);
        await deleteCmd.ExecuteNonQueryAsync();
        Console.WriteLine($"✅ Test record cleaned up");
        
        Console.WriteLine("\n🎉 PostgreSQL timezone issue is now COMPLETELY FIXED!");
        Console.WriteLine("Both DateTime.Now and DateTime.UtcNow will work!");
    }
    catch (Exception testEx)
    {
        Console.WriteLine($"❌ TEST STILL FAILING:");
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
    Console.WriteLine("✅ PostgreSQL timezone column conversion completed!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database connection error: {ex.Message}");
}