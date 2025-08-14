#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 DATABASE CONSTRAINT VE COLUMN KONTROLÜ:");
    Console.WriteLine(new string('=', 60));
    
    // UserSubscriptions tablosunun column'larını kontrol et
    Console.WriteLine("📋 UserSubscriptions Table Columns:");
    Console.WriteLine(new string('-', 40));
    
    var columnsSql = @"
        SELECT column_name, data_type, is_nullable, column_default
        FROM information_schema.columns 
        WHERE table_name = 'UserSubscriptions' 
        ORDER BY ordinal_position";
    
    await using var columnsCmd = new NpgsqlCommand(columnsSql, connection);
    await using var columnsReader = await columnsCmd.ExecuteReaderAsync();
    
    Console.WriteLine($"{"Column",-25} | {"Type",-15} | {"Nullable",-8} | {"Default",-15}");
    Console.WriteLine(new string('-', 70));
    
    while (await columnsReader.ReadAsync())
    {
        var columnName = columnsReader.GetString(0);
        var dataType = columnsReader.GetString(1);
        var isNullable = columnsReader.GetString(2);
        var columnDefault = columnsReader.IsDBNull(3) ? "NULL" : columnsReader.GetString(3);
        
        Console.WriteLine($"{columnName,-25} | {dataType,-15} | {isNullable,-8} | {columnDefault,-15}");
    }
    await columnsReader.CloseAsync();
    
    // Foreign key constraint'leri kontrol et
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
        AND tc.table_name = 'UserSubscriptions'";
    
    await using var fkCmd = new NpgsqlCommand(fkSql, connection);
    await using var fkReader = await fkCmd.ExecuteReaderAsync();
    
    while (await fkReader.ReadAsync())
    {
        var constraintName = fkReader.GetString(0);
        var tableName = fkReader.GetString(1);
        var columnName = fkReader.GetString(2);
        var foreignTable = fkReader.GetString(3);
        var foreignColumn = fkReader.GetString(4);
        
        Console.WriteLine($"   {constraintName}:");
        Console.WriteLine($"      {tableName}.{columnName} -> {foreignTable}.{foreignColumn}");
    }
    await fkReader.CloseAsync();
    
    // Check constraints
    Console.WriteLine("\n✅ Check Constraints:");
    Console.WriteLine(new string('-', 30));
    
    var checkSql = @"
        SELECT
            tc.constraint_name,
            cc.check_clause
        FROM information_schema.table_constraints AS tc
        JOIN information_schema.check_constraints AS cc
            ON tc.constraint_name = cc.constraint_name
        WHERE tc.table_name = 'UserSubscriptions'";
    
    await using var checkCmd = new NpgsqlCommand(checkSql, connection);
    await using var checkReader = await checkCmd.ExecuteReaderAsync();
    
    bool hasCheckConstraints = false;
    while (await checkReader.ReadAsync())
    {
        hasCheckConstraints = true;
        var constraintName = checkReader.GetString(0);
        var checkClause = checkReader.GetString(1);
        
        Console.WriteLine($"   {constraintName}: {checkClause}");
    }
    
    if (!hasCheckConstraints)
    {
        Console.WriteLine("   No check constraints found");
    }
    
    await checkReader.CloseAsync();
    
    // Test değerleriyle UserSubscription insert denemesi
    Console.WriteLine("\n🧪 TEST INSERT (UserSubscription):");
    Console.WriteLine(new string('-', 40));
    
    // Bir test UserId seç (Users tablosundan)
    var testUserSql = @"SELECT ""UserId"" FROM ""Users"" ORDER BY ""UserId"" DESC LIMIT 1";
    await using var testUserCmd = new NpgsqlCommand(testUserSql, connection);
    var testUserId = (int)await testUserCmd.ExecuteScalarAsync();
    
    // Trial tier ID'sini al
    var trialTierSql = @"SELECT ""Id"" FROM ""SubscriptionTiers"" WHERE ""TierName"" = 'Trial'";
    await using var trialTierCmd = new NpgsqlCommand(trialTierSql, connection);
    var trialTierId = (int)await trialTierCmd.ExecuteScalarAsync();
    
    Console.WriteLine($"Test Parametreleri:");
    Console.WriteLine($"   UserId: {testUserId}");
    Console.WriteLine($"   SubscriptionTierId: {trialTierId}");
    
    try
    {
        var testInsertSql = @"
            INSERT INTO ""UserSubscriptions"" (
                ""UserId"", ""SubscriptionTierId"", ""StartDate"", ""EndDate"",
                ""IsActive"", ""AutoRenew"", ""PaymentMethod"", ""PaidAmount"", ""Currency"",
                ""CurrentDailyUsage"", ""CurrentMonthlyUsage"", ""LastUsageResetDate"",
                ""MonthlyUsageResetDate"", ""Status"", ""IsTrialSubscription"",
                ""TrialEndDate"", ""CreatedDate"", ""CreatedUserId""
            ) VALUES (
                @userId, @tierId, @startDate, @endDate,
                @isActive, @autoRenew, @paymentMethod, @paidAmount, @currency,
                @dailyUsage, @monthlyUsage, @lastReset,
                @monthlyReset, @status, @isTrial,
                @trialEnd, @createdDate, @createdUserId
            ) RETURNING ""Id""";
        
        var now = DateTime.UtcNow;
        var trialEnd = now.AddDays(30);
        
        await using var testInsertCmd = new NpgsqlCommand(testInsertSql, connection);
        testInsertCmd.Parameters.AddWithValue("userId", testUserId + 1000); // fake userId to avoid duplicates
        testInsertCmd.Parameters.AddWithValue("tierId", trialTierId);
        testInsertCmd.Parameters.AddWithValue("startDate", now);
        testInsertCmd.Parameters.AddWithValue("endDate", trialEnd);
        testInsertCmd.Parameters.AddWithValue("isActive", true);
        testInsertCmd.Parameters.AddWithValue("autoRenew", false);
        testInsertCmd.Parameters.AddWithValue("paymentMethod", "Trial");
        testInsertCmd.Parameters.AddWithValue("paidAmount", 0.0m);
        testInsertCmd.Parameters.AddWithValue("currency", "TRY");
        testInsertCmd.Parameters.AddWithValue("dailyUsage", 0);
        testInsertCmd.Parameters.AddWithValue("monthlyUsage", 0);
        testInsertCmd.Parameters.AddWithValue("lastReset", now);
        testInsertCmd.Parameters.AddWithValue("monthlyReset", now);
        testInsertCmd.Parameters.AddWithValue("status", "Active");
        testInsertCmd.Parameters.AddWithValue("isTrial", true);
        testInsertCmd.Parameters.AddWithValue("trialEnd", trialEnd);
        testInsertCmd.Parameters.AddWithValue("createdDate", now);
        testInsertCmd.Parameters.AddWithValue("createdUserId", testUserId);
        
        var newId = (int)await testInsertCmd.ExecuteScalarAsync();
        Console.WriteLine($"✅ TEST INSERT BAŞARILI - New ID: {newId}");
        
        // Test kaydını sil
        var deleteSql = @"DELETE FROM ""UserSubscriptions"" WHERE ""Id"" = @id";
        await using var deleteCmd = new NpgsqlCommand(deleteSql, connection);
        deleteCmd.Parameters.AddWithValue("id", newId);
        await deleteCmd.ExecuteNonQueryAsync();
        Console.WriteLine($"✅ Test kaydı silindi");
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
    Console.WriteLine("✅ Constraint analizi tamamlandı!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Hata: {ex.Message}");
}