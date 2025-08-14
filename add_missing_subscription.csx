#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔧 EKSIK SUBSCRIPTION EKLEME:");
    Console.WriteLine(new string('=', 40));
    
    // En son kullanıcıyı bul
    var userSql = @"SELECT ""UserId"", ""FullName"", ""Email"" FROM ""Users"" ORDER BY ""UserId"" DESC LIMIT 1";
    await using var userCmd = new NpgsqlCommand(userSql, connection);
    await using var userReader = await userCmd.ExecuteReaderAsync();
    
    int userId = 0;
    string email = "";
    
    if (await userReader.ReadAsync())
    {
        userId = userReader.GetInt32(0);
        email = userReader.IsDBNull(2) ? "" : userReader.GetString(2);
        Console.WriteLine($"👤 Kullanıcı: ID={userId}, Email={email}");
    }
    await userReader.CloseAsync();
    
    if (userId == 0)
    {
        Console.WriteLine("❌ Kullanıcı bulunamadı");
        return;
    }
    
    // Subscription var mı kontrol et
    var existingSubSql = @"SELECT COUNT(*) FROM ""UserSubscriptions"" WHERE ""UserId"" = @userId";
    await using var existingCmd = new NpgsqlCommand(existingSubSql, connection);
    existingCmd.Parameters.AddWithValue("userId", userId);
    var existingCount = (long)await existingCmd.ExecuteScalarAsync();
    
    if (existingCount > 0)
    {
        Console.WriteLine($"✅ Kullanıcının zaten {existingCount} subscription'ı var");
        return;
    }
    
    // Trial tier ID'sini al
    var trialSql = @"SELECT ""Id"" FROM ""SubscriptionTiers"" WHERE ""TierName"" = 'Trial' AND ""IsActive"" = true";
    await using var trialCmd = new NpgsqlCommand(trialSql, connection);
    var trialTierId = (int)await trialCmd.ExecuteScalarAsync();
    
    Console.WriteLine($"🎯 Trial Tier ID: {trialTierId}");
    
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
    
    Console.WriteLine($"✅ Trial subscription oluşturuldu!");
    Console.WriteLine($"   Subscription ID: {newSubId}");
    Console.WriteLine($"   Kullanıcı ID: {userId}");
    Console.WriteLine($"   Email: {email}");
    Console.WriteLine($"   Başlangıç: {now:yyyy-MM-dd HH:mm}");
    Console.WriteLine($"   Bitiş: {trialEnd:yyyy-MM-dd HH:mm}");
    Console.WriteLine($"   Süre: 30 gün");
    
    Console.WriteLine("\n" + new string('=', 40));
    Console.WriteLine("🎉 İşlem başarılı!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Hata: {ex.Message}");
}