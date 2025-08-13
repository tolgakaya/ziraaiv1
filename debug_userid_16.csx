#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 DEBUGGING USERID 16 SUBSCRIPTION ISSUE:");
    Console.WriteLine(new string('=', 60));
    
    // Check if user 16 exists
    Console.WriteLine("1. Checking if user 16 exists...");
    var userExistsSql = @"SELECT ""UserId"", ""Email"", ""FullName"" FROM ""Users"" WHERE ""UserId"" = 16";
    
    await using var userCmd = new NpgsqlCommand(userExistsSql, connection);
    await using var userReader = await userCmd.ExecuteReaderAsync();
    
    if (await userReader.ReadAsync())
    {
        var userId = userReader.GetInt32(0);
        var email = userReader.GetString(1);
        var fullName = userReader.GetString(2);
        Console.WriteLine($"✅ User found: {userId} - {email} ({fullName})");
    }
    else
    {
        Console.WriteLine("❌ User 16 does not exist!");
        return;
    }
    await userReader.CloseAsync();
    
    // Check user's subscription
    Console.WriteLine("\n2. Checking user 16's subscription...");
    var subscriptionSql = @"
        SELECT us.""Id"", us.""SubscriptionTierId"", us.""IsActive"", us.""Status"", 
               us.""StartDate"", us.""EndDate"", st.""TierName""
        FROM ""UserSubscriptions"" us
        JOIN ""SubscriptionTiers"" st ON us.""SubscriptionTierId"" = st.""Id""
        WHERE us.""UserId"" = 16
        ORDER BY us.""Id"" DESC";
    
    await using var subCmd = new NpgsqlCommand(subscriptionSql, connection);
    await using var subReader = await subCmd.ExecuteReaderAsync();
    
    bool hasSubscription = false;
    while (await subReader.ReadAsync())
    {
        hasSubscription = true;
        var subId = subReader.GetInt32(0);
        var tierId = subReader.GetInt32(1);
        var isActive = subReader.GetBoolean(2);
        var status = subReader.GetString(3);
        var startDate = subReader.GetDateTime(4);
        var endDate = subReader.GetDateTime(5);
        var tierName = subReader.GetString(6);
        
        Console.WriteLine($"   Subscription ID: {subId}");
        Console.WriteLine($"   Tier: {tierName} (ID: {tierId})");
        Console.WriteLine($"   Active: {isActive}");
        Console.WriteLine($"   Status: {status}");
        Console.WriteLine($"   Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        Console.WriteLine($"   Current Date: {DateTime.Now:yyyy-MM-dd}");
        Console.WriteLine($"   Expired: {(endDate < DateTime.Now ? "YES" : "NO")}");
        Console.WriteLine();
    }
    
    if (!hasSubscription)
    {
        Console.WriteLine("❌ User 16 has NO subscriptions!");
    }
    
    await subReader.CloseAsync();
    
    // Test manual insert into SubscriptionUsageLogs for user 16
    Console.WriteLine("3. Testing manual SubscriptionUsageLogs insert for user 16...");
    
    try
    {
        // Get user 16's active subscription ID
        var getActiveSubSql = @"
            SELECT ""Id"" FROM ""UserSubscriptions"" 
            WHERE ""UserId"" = 16 AND ""IsActive"" = true
            ORDER BY ""Id"" DESC LIMIT 1";
        
        await using var getActiveCmd = new NpgsqlCommand(getActiveSubSql, connection);
        var activeSubIdObj = await getActiveCmd.ExecuteScalarAsync();
        
        if (activeSubIdObj == null)
        {
            Console.WriteLine("❌ User 16 has NO ACTIVE subscription!");
            Console.WriteLine("This explains why ValidateAndLogUsageAsync fails!");
            return;
        }
        
        var activeSubId = (int)activeSubIdObj;
        Console.WriteLine($"✅ Found active subscription ID: {activeSubId}");
        
        // Test insert
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
        testInsertCmd.Parameters.AddWithValue("subscriptionId", activeSubId);
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
        Console.WriteLine($"❌ MANUAL INSERT FAILED:");
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
    Console.WriteLine("✅ Debug analysis completed!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database connection error: {ex.Message}");
}