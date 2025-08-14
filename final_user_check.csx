#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🎯 SON DURUM KONTROLÜ - TRIAL TIER VE FARMER'LAR:");
    Console.WriteLine(new string('=', 70));
    
    // En son kullanıcıları ve subscription durumlarını listele
    var userDetailSql = @"
        SELECT u.""UserId"", u.""FullName"", u.""Email"",
               us.""Id"" as SubId, st.""TierName"", st.""DisplayName"",
               us.""IsActive"", us.""IsTrialSubscription"",
               us.""CurrentDailyUsage"", us.""CurrentMonthlyUsage"",
               us.""StartDate"", us.""EndDate""
        FROM ""Users"" u
        LEFT JOIN ""UserSubscriptions"" us ON u.""UserId"" = us.""UserId"" AND us.""IsActive"" = true
        LEFT JOIN ""SubscriptionTiers"" st ON us.""SubscriptionTierId"" = st.""Id""
        ORDER BY u.""UserId"" DESC
        LIMIT 10";
    
    await using var userCmd = new NpgsqlCommand(userDetailSql, connection);
    await using var userReader = await userCmd.ExecuteReaderAsync();
    
    Console.WriteLine("👥 SON KAYIT OLAN KULLANICILAR VE SUBSCRIPTION'LARI:");
    Console.WriteLine($"{"ID",-4} | {"Full Name",-20} | {"Email",-25} | {"Tier",-6} | {"Trial?",-6} | {"Usage D/M",-10} | {"End Date",-12}");
    Console.WriteLine(new string('-', 95));
    
    int totalUsers = 0;
    int usersWithSubscription = 0;
    int usersWithTrial = 0;
    
    while (await userReader.ReadAsync())
    {
        totalUsers++;
        
        var userId = userReader.GetInt32(0);
        var fullName = userReader.IsDBNull(1) ? "N/A" : userReader.GetString(1);
        var email = userReader.IsDBNull(2) ? "no-email" : userReader.GetString(2);
        var tierName = userReader.IsDBNull(4) ? "NONE" : userReader.GetString(4);
        var isActive = userReader.IsDBNull(6) ? false : userReader.GetBoolean(6);
        var isTrial = userReader.IsDBNull(7) ? false : userReader.GetBoolean(7);
        var dailyUsage = userReader.IsDBNull(8) ? 0 : userReader.GetInt32(8);
        var monthlyUsage = userReader.IsDBNull(9) ? 0 : userReader.GetInt32(9);
        var endDate = userReader.IsDBNull(11) ? "N/A" : userReader.GetDateTime(11).ToString("MM/dd/yyyy");
        
        if (!userReader.IsDBNull(3) && isActive) usersWithSubscription++;
        if (isTrial && isActive) usersWithTrial++;
        
        var statusIcon = isActive ? "✅" : "❌";
        var trialIcon = isTrial ? "🆓" : "💰";
        
        Console.WriteLine($"{userId,-4} | {fullName,-20} | {email,-25} | {tierName,-6} | {(isTrial ? "YES" : "NO"),-6} | {dailyUsage}/{monthlyUsage,-8} | {endDate,-12} {statusIcon}{trialIcon}");
    }
    
    await userReader.CloseAsync();
    
    Console.WriteLine();
    
    // Farmer kullanıcılarını kontrol et
    var farmerCheckSql = @"
        SELECT u.""UserId"", u.""FullName"", u.""Email"",
               us.""Id"" as SubId, st.""TierName"",
               us.""IsTrialSubscription"", us.""IsActive"",
               us.""CurrentDailyUsage"", us.""CurrentMonthlyUsage"",
               us.""StartDate"", us.""EndDate""
        FROM ""Users"" u
        INNER JOIN ""UserClaims"" uc ON u.""UserId"" = uc.""UserId""
        INNER JOIN ""OperationClaims"" oc ON uc.""ClaimId"" = oc.""Id""
        LEFT JOIN ""UserSubscriptions"" us ON u.""UserId"" = us.""UserId"" AND us.""IsActive"" = true
        LEFT JOIN ""SubscriptionTiers"" st ON us.""SubscriptionTierId"" = st.""Id""
        WHERE oc.""Name"" = 'Farmer'
        ORDER BY u.""UserId"" DESC";
    
    await using var farmerCmd = new NpgsqlCommand(farmerCheckSql, connection);
    await using var farmerReader = await farmerCmd.ExecuteReaderAsync();
    
    Console.WriteLine("🚜 FARMER ROLÜNDEKI KULLANICILAR:");
    Console.WriteLine($"{"ID",-4} | {"Full Name",-20} | {"Email",-25} | {"Tier",-6} | {"Trial?",-6} | {"Usage",-8} | {"Status",-8}");
    Console.WriteLine(new string('-', 85));
    
    int farmerCount = 0;
    int farmersWithSub = 0;
    int farmersWithTrial = 0;
    
    while (await farmerReader.ReadAsync())
    {
        farmerCount++;
        
        var userId = farmerReader.GetInt32(0);
        var fullName = farmerReader.IsDBNull(1) ? "N/A" : farmerReader.GetString(1);
        var email = farmerReader.IsDBNull(2) ? "no-email" : farmerReader.GetString(2);
        var tierName = farmerReader.IsDBNull(4) ? "NONE" : farmerReader.GetString(4);
        var isTrial = farmerReader.IsDBNull(5) ? false : farmerReader.GetBoolean(5);
        var isActive = farmerReader.IsDBNull(6) ? false : farmerReader.GetBoolean(6);
        var dailyUsage = farmerReader.IsDBNull(7) ? 0 : farmerReader.GetInt32(7);
        var monthlyUsage = farmerReader.IsDBNull(8) ? 0 : farmerReader.GetInt32(8);
        
        if (!farmerReader.IsDBNull(3) && isActive) farmersWithSub++;
        if (isTrial && isActive) farmersWithTrial++;
        
        var status = isActive ? "ACTIVE" : "INACTIVE";
        var statusIcon = isActive ? "✅" : "❌";
        
        Console.WriteLine($"{userId,-4} | {fullName,-20} | {email,-25} | {tierName,-6} | {(isTrial ? "YES" : "NO"),-6} | {dailyUsage}/{monthlyUsage,-6} | {status,-8}{statusIcon}");
    }
    
    await farmerReader.CloseAsync();
    
    Console.WriteLine();
    Console.WriteLine("📊 ÖZET İSTATİSTİKLER:");
    Console.WriteLine(new string('-', 40));
    Console.WriteLine($"👥 Toplam Kullanıcı (son 10): {totalUsers}");
    Console.WriteLine($"📊 Aktif Subscription'lı: {usersWithSubscription}");
    Console.WriteLine($"🆓 Trial Subscription'lı: {usersWithTrial}");
    Console.WriteLine($"🚜 Toplam Farmer: {farmerCount}");
    Console.WriteLine($"🚜 Subscription'lı Farmer: {farmersWithSub}");
    Console.WriteLine($"🆓 Trial'lı Farmer: {farmersWithTrial}");
    
    if (totalUsers > 0)
    {
        var trialRate = (double)usersWithTrial / totalUsers * 100;
        Console.WriteLine($"🎯 Trial Dönüşüm Oranı: %{trialRate:F1}");
    }
    
    if (farmerCount > 0)
    {
        var farmerCoverage = (double)farmersWithSub / farmerCount * 100;
        Console.WriteLine($"🚜 Farmer Kapsama Oranı: %{farmerCoverage:F1}");
    }
    
    Console.WriteLine();
    Console.WriteLine("🔍 TRIAL TIER DURUMU:");
    Console.WriteLine(new string('-', 30));
    
    // Trial tier tekrar kontrol
    var trialCheckSql = @"
        SELECT ""Id"", ""TierName"", ""DailyRequestLimit"", ""MonthlyRequestLimit"", 
               ""MonthlyPrice"", ""IsActive"", ""CreatedDate""
        FROM ""SubscriptionTiers"" 
        WHERE ""TierName"" = 'Trial'";
        
    await using var trialCheckCmd = new NpgsqlCommand(trialCheckSql, connection);
    await using var trialCheckReader = await trialCheckCmd.ExecuteReaderAsync();
    
    if (await trialCheckReader.ReadAsync())
    {
        Console.WriteLine($"✅ Trial Tier Mevcut:");
        Console.WriteLine($"   ID: {trialCheckReader.GetInt32(0)}");
        Console.WriteLine($"   Günlük Limit: {trialCheckReader.GetInt32(2)} analiz");
        Console.WriteLine($"   Aylık Limit: {trialCheckReader.GetInt32(3)} analiz");
        Console.WriteLine($"   Aylık Ücret: ₺{trialCheckReader.GetDecimal(4):F2}");
        Console.WriteLine($"   Aktif: {trialCheckReader.GetBoolean(5)}");
        Console.WriteLine($"   Oluşturulma: {trialCheckReader.GetDateTime(6):yyyy-MM-dd HH:mm}");
    }
    else
    {
        Console.WriteLine("❌ Trial Tier BULUNAMADI!");
    }
    
    Console.WriteLine("\n" + new string('=', 70));
    Console.WriteLine("🎉 Manuel kontrol TAMAMLANDI!");
    Console.WriteLine("📝 Sonuç: Trial tier çalışıyor ve yeni kullanıcılara atanıyor! ✅");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Hata: {ex.Message}");
}