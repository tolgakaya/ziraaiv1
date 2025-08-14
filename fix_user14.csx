#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔧 UserID 14 İÇİN TRIAL SUBSCRIPTION EKLEME:");
    Console.WriteLine(new string('=', 50));
    
    var userId = 14;
    
    // Subscription var mı kontrol et
    var checkSql = @"SELECT COUNT(*) FROM ""UserSubscriptions"" WHERE ""UserId"" = @userId";
    await using var checkCmd = new NpgsqlCommand(checkSql, connection);
    checkCmd.Parameters.AddWithValue("userId", userId);
    var existingCount = (long)await checkCmd.ExecuteScalarAsync();
    
    if (existingCount > 0)
    {
        Console.WriteLine($"✅ UserID {userId} zaten {existingCount} subscription'a sahip");
        return;
    }
    
    // Trial tier ID'sini al
    var trialSql = @"SELECT ""Id"" FROM ""SubscriptionTiers"" WHERE ""TierName"" = 'Trial' AND ""IsActive"" = true";
    await using var trialCmd = new NpgsqlCommand(trialSql, connection);
    var trialTierId = (int)await trialCmd.ExecuteScalarAsync();
    
    // Trial subscription ekle
    var insertSql = @"
        INSERT INTO ""UserSubscriptions"" (
            ""UserId"", ""SubscriptionTierId"", ""StartDate"", ""EndDate"",
            ""IsActive"", ""AutoRenew"", ""PaymentMethod"", ""PaidAmount"", ""Currency"",
            ""CurrentDailyUsage"", ""CurrentMonthlyUsage"", ""LastUsageResetDate"",
            ""MonthlyUsageResetDate"", ""Status"", ""IsTrialSubscription"",
            ""TrialEndDate"", ""CreatedDate"", ""CreatedUserId""
        ) VALUES (
            @userId, @tierId, @startDate, @endDate,
            true, false, 'Trial', 0.0, 'TRY',
            0, 0, @now, @now, 'Active', true,
            @trialEnd, @now, @userId
        ) RETURNING ""Id""";
    
    var now = DateTime.UtcNow;
    var trialEnd = now.AddDays(30);
    
    await using var insertCmd = new NpgsqlCommand(insertSql, connection);
    insertCmd.Parameters.AddWithValue("userId", userId);
    insertCmd.Parameters.AddWithValue("tierId", trialTierId);
    insertCmd.Parameters.AddWithValue("startDate", now);
    insertCmd.Parameters.AddWithValue("endDate", trialEnd);
    insertCmd.Parameters.AddWithValue("trialEnd", trialEnd);
    insertCmd.Parameters.AddWithValue("now", now);
    
    var newSubId = (int)await insertCmd.ExecuteScalarAsync();
    
    Console.WriteLine($"✅ UserID {userId} için Trial subscription oluşturuldu!");
    Console.WriteLine($"   Subscription ID: {newSubId}");
    Console.WriteLine($"   Trial Tier ID: {trialTierId}");
    Console.WriteLine($"   Başlangıç: {now:yyyy-MM-dd HH:mm}");
    Console.WriteLine($"   Bitiş: {trialEnd:yyyy-MM-dd HH:mm}");
    
    Console.WriteLine("\n" + new string('=', 50));
    Console.WriteLine("✅ İşlem tamamlandı!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Hata: {ex.Message}");
}