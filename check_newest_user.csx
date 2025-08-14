#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("👤 EN YENİ KULLANICI KONTROL:");
    Console.WriteLine(new string('=', 40));
    
    // En yeni kullanıcıyı bul
    var latestUserSql = @"
        SELECT ""UserId"", ""FullName"", ""Email"", ""Status"", ""RecordDate""
        FROM ""Users""
        ORDER BY ""UserId"" DESC
        LIMIT 3";
    
    await using var userCmd = new NpgsqlCommand(latestUserSql, connection);
    await using var userReader = await userCmd.ExecuteReaderAsync();
    
    Console.WriteLine("📊 SON 3 KULLANICI:");
    Console.WriteLine($"{"ID",-4} | {"Name",-20} | {"Email",-25} | {"Date",-20}");
    Console.WriteLine(new string('-', 75));
    
    var userIds = new List<int>();
    
    while (await userReader.ReadAsync())
    {
        var userId = userReader.GetInt32(0);
        var fullName = userReader.IsDBNull(1) ? "N/A" : userReader.GetString(1);
        var email = userReader.IsDBNull(2) ? "N/A" : userReader.GetString(2);
        var recordDate = userReader.IsDBNull(4) ? DateTime.MinValue : userReader.GetDateTime(4);
        
        userIds.Add(userId);
        Console.WriteLine($"{userId,-4} | {fullName,-20} | {email,-25} | {recordDate:yyyy-MM-dd HH:mm:ss}");
    }
    
    await userReader.CloseAsync();
    
    // Bu kullanıcıların subscription durumlarını kontrol et
    Console.WriteLine();
    Console.WriteLine("📊 SUBSCRIPTION DURUMLARI:");
    Console.WriteLine(new string('-', 50));
    
    foreach (var userId in userIds)
    {
        var subSql = @"
            SELECT COUNT(*) as SubCount, 
                   COUNT(CASE WHEN ""IsActive"" = true THEN 1 END) as ActiveCount,
                   COUNT(CASE WHEN ""IsTrialSubscription"" = true AND ""IsActive"" = true THEN 1 END) as TrialCount
            FROM ""UserSubscriptions"" 
            WHERE ""UserId"" = @userId";
        
        await using var subCmd = new NpgsqlCommand(subSql, connection);
        subCmd.Parameters.AddWithValue("userId", userId);
        await using var subReader = await subCmd.ExecuteReaderAsync();
        
        if (await subReader.ReadAsync())
        {
            var totalSubs = subReader.GetInt64(0);
            var activeSubs = subReader.GetInt64(1);
            var trialSubs = subReader.GetInt64(2);
            
            var status = totalSubs > 0 ? (activeSubs > 0 ? "✅ ACTIVE" : "❌ INACTIVE") : "❌ NO SUB";
            var trialStatus = trialSubs > 0 ? " (TRIAL)" : "";
            
            Console.WriteLine($"   UserID {userId}: {status}{trialStatus} - Total: {totalSubs}, Active: {activeSubs}, Trial: {trialSubs}");
        }
        
        await subReader.CloseAsync();
    }
    
    Console.WriteLine();
    Console.WriteLine("🔍 TOPLAM İSTATİSTİKLER:");
    Console.WriteLine(new string('-', 30));
    
    // Toplam sayılar
    var statsSql = @"
        SELECT 
            (SELECT COUNT(*) FROM ""Users"") as TotalUsers,
            (SELECT COUNT(*) FROM ""UserSubscriptions"") as TotalSubs,
            (SELECT COUNT(*) FROM ""UserSubscriptions"" WHERE ""IsActive"" = true) as ActiveSubs,
            (SELECT COUNT(*) FROM ""UserSubscriptions"" WHERE ""IsTrialSubscription"" = true AND ""IsActive"" = true) as TrialSubs";
    
    await using var statsCmd = new NpgsqlCommand(statsSql, connection);
    await using var statsReader = await statsCmd.ExecuteReaderAsync();
    
    if (await statsReader.ReadAsync())
    {
        var totalUsers = statsReader.GetInt64(0);
        var totalSubs = statsReader.GetInt64(1);
        var activeSubs = statsReader.GetInt64(2);
        var trialSubs = statsReader.GetInt64(3);
        
        Console.WriteLine($"👥 Toplam Kullanıcı: {totalUsers}");
        Console.WriteLine($"📊 Toplam Subscription: {totalSubs}");
        Console.WriteLine($"✅ Aktif Subscription: {activeSubs}");
        Console.WriteLine($"🆓 Trial Subscription: {trialSubs}");
        
        var coverageRate = totalUsers > 0 ? (double)activeSubs / totalUsers * 100 : 0;
        Console.WriteLine($"📈 Kapsama Oranı: %{coverageRate:F1}");
    }
    
    Console.WriteLine("\n" + new string('=', 40));
    Console.WriteLine("✅ Kontrol tamamlandı!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Hata: {ex.Message}");
}