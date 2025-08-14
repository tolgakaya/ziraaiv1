#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔧 TÜM SUBSCRIPTION'SIZ KULLANICILARA TRIAL SUBSCRIPTION EKLEME:");
    Console.WriteLine(new string('=', 70));
    
    // Trial tier ID'sini al
    var trialSql = @"SELECT ""Id"" FROM ""SubscriptionTiers"" WHERE ""TierName"" = 'Trial' AND ""IsActive"" = true";
    await using var trialCmd = new NpgsqlCommand(trialSql, connection);
    var trialTierId = (int)await trialCmd.ExecuteScalarAsync();
    Console.WriteLine($"🎯 Trial Tier ID: {trialTierId}");
    
    // Subscription'ı olmayan kullanıcıları bul
    var usersWithoutSubSql = @"
        SELECT u.""UserId"", u.""FullName"", u.""Email""
        FROM ""Users"" u
        LEFT JOIN ""UserSubscriptions"" us ON u.""UserId"" = us.""UserId"" AND us.""IsActive"" = true
        WHERE us.""Id"" IS NULL
        ORDER BY u.""UserId""";
    
    await using var usersCmd = new NpgsqlCommand(usersWithoutSubSql, connection);
    await using var usersReader = await usersCmd.ExecuteReaderAsync();
    
    var usersToFix = new List<(int UserId, string FullName, string Email)>();
    
    while (await usersReader.ReadAsync())
    {
        var userId = usersReader.GetInt32(0);
        var fullName = usersReader.IsDBNull(1) ? "N/A" : usersReader.GetString(1);
        var email = usersReader.IsDBNull(2) ? "no-email" : usersReader.GetString(2);
        
        usersToFix.Add((userId, fullName, email));
    }
    
    await usersReader.CloseAsync();
    
    Console.WriteLine($"📊 Subscription'sız kullanıcı sayısı: {usersToFix.Count}");
    
    if (usersToFix.Count == 0)
    {
        Console.WriteLine("✅ Tüm kullanıcıların subscription'ı mevcut!");
        return;
    }
    
    Console.WriteLine("\n👥 Subscription'sız kullanıcılar:");
    foreach (var user in usersToFix)
    {
        Console.WriteLine($"   - ID: {user.UserId}, Name: {user.FullName}, Email: {user.Email}");
    }
    
    Console.WriteLine($"\n🔧 {usersToFix.Count} kullanıcıya Trial subscription ekleniyor...");
    
    // Her kullanıcı için Trial subscription ekle
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
    
    int successCount = 0;
    int errorCount = 0;
    
    foreach (var user in usersToFix)
    {
        try
        {
            var now = DateTime.UtcNow;
            var trialEnd = now.AddDays(30);
            
            await using var insertCmd = new NpgsqlCommand(insertSql, connection);
            insertCmd.Parameters.AddWithValue("userId", user.UserId);
            insertCmd.Parameters.AddWithValue("tierId", trialTierId);
            insertCmd.Parameters.AddWithValue("startDate", now);
            insertCmd.Parameters.AddWithValue("endDate", trialEnd);
            insertCmd.Parameters.AddWithValue("trialEnd", trialEnd);
            insertCmd.Parameters.AddWithValue("now", now);
            
            var newSubId = (int)await insertCmd.ExecuteScalarAsync();
            
            Console.WriteLine($"   ✅ {user.Email} -> Subscription ID: {newSubId}");
            successCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ {user.Email} -> Hata: {ex.Message}");
            errorCount++;
        }
    }
    
    Console.WriteLine($"\n📊 SONUÇ:");
    Console.WriteLine($"   ✅ Başarılı: {successCount}");
    Console.WriteLine($"   ❌ Hatalı: {errorCount}");
    Console.WriteLine($"   📈 Başarı Oranı: %{(double)successCount / usersToFix.Count * 100:F1}");
    
    Console.WriteLine("\n" + new string('=', 70));
    Console.WriteLine("🎉 Toplu Trial subscription ekleme tamamlandı!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Genel Hata: {ex.Message}");
}