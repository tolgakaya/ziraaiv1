#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 SUBSCRIPTION USAGE LOGS TABLE ANALYSIS:");
    Console.WriteLine(new string('=', 60));
    
    // Check if table exists
    Console.WriteLine("📋 Checking if SubscriptionUsageLogs table exists:");
    Console.WriteLine(new string('-', 50));
    
    var tableExistsSql = @"
        SELECT EXISTS (
            SELECT FROM information_schema.tables 
            WHERE table_name = 'SubscriptionUsageLogs'
        )";
    
    await using var existsCmd = new NpgsqlCommand(tableExistsSql, connection);
    var tableExists = (bool)await existsCmd.ExecuteScalarAsync();
    
    Console.WriteLine($"Table exists: {tableExists}");
    
    if (!tableExists)
    {
        Console.WriteLine("❌ SubscriptionUsageLogs table does not exist!");
        Console.WriteLine("This explains the save error. The table needs to be created.");
        return;
    }
    
    // If table exists, check its structure
    Console.WriteLine("\n📋 Table Structure:");
    Console.WriteLine(new string('-', 50));
    
    var columnsSql = @"
        SELECT column_name, data_type, is_nullable, column_default
        FROM information_schema.columns 
        WHERE table_name = 'SubscriptionUsageLogs' 
        ORDER BY ordinal_position";
    
    await using var columnsCmd = new NpgsqlCommand(columnsSql, connection);
    await using var columnsReader = await columnsCmd.ExecuteReaderAsync();
    
    Console.WriteLine($"{"Column",-25} | {"Type",-20} | {"Nullable",-8} | {"Default",-15}");
    Console.WriteLine(new string('-', 75));
    
    bool hasColumns = false;
    while (await columnsReader.ReadAsync())
    {
        hasColumns = true;
        var columnName = columnsReader.GetString(0);
        var dataType = columnsReader.GetString(1);
        var isNullable = columnsReader.GetString(2);
        var columnDefault = columnsReader.IsDBNull(3) ? "NULL" : columnsReader.GetString(3);
        
        Console.WriteLine($"{columnName,-25} | {dataType,-20} | {isNullable,-8} | {columnDefault,-15}");
    }
    await columnsReader.CloseAsync();
    
    if (!hasColumns)
    {
        Console.WriteLine("❌ No columns found in SubscriptionUsageLogs table!");
    }
    
    // Check foreign key constraints
    Console.WriteLine("\n🔗 Foreign Key Constraints:");
    Console.WriteLine(new string('-', 40));
    
    var fkSql = @"
        SELECT
            tc.constraint_name,
            tc.table_name,
            kcu.column_name,
            ccu.table_name AS foreign_table_name,
            ccu.column_name AS foreign_column_name
        FROM information_schema.table_constraints AS tc 
        JOIN information_schema.key_column_usage AS kcu
            ON tc.constraint_name = kcu.constraint_name
            AND tc.table_schema = kcu.table_schema
        JOIN information_schema.constraint_column_usage AS ccu
            ON ccu.constraint_name = tc.constraint_name
            AND ccu.table_schema = tc.table_schema
        WHERE tc.constraint_type = 'FOREIGN KEY' 
        AND tc.table_name = 'SubscriptionUsageLogs'";
    
    await using var fkCmd = new NpgsqlCommand(fkSql, connection);
    await using var fkReader = await fkCmd.ExecuteReaderAsync();
    
    bool hasForeignKeys = false;
    while (await fkReader.ReadAsync())
    {
        hasForeignKeys = true;
        var constraintName = fkReader.GetString(0);
        var tableName = fkReader.GetString(1);
        var columnName = fkReader.GetString(2);
        var foreignTable = fkReader.GetString(3);
        var foreignColumn = fkReader.GetString(4);
        
        Console.WriteLine($"   {constraintName}:");
        Console.WriteLine($"      {tableName}.{columnName} -> {foreignTable}.{foreignColumn}");
    }
    
    if (!hasForeignKeys)
    {
        Console.WriteLine("   No foreign key constraints found");
    }
    
    await fkReader.CloseAsync();
    
    // Try to perform a test insert
    Console.WriteLine("\n🧪 TEST INSERT ATTEMPT:");
    Console.WriteLine(new string('-', 30));
    
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
        Console.WriteLine($"❌ TEST INSERT FAILED:");
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
    Console.WriteLine("✅ SubscriptionUsageLogs table analysis completed!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database connection error: {ex.Message}");
}