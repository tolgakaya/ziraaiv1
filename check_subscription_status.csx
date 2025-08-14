#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔗 Staging database bağlantısı başarılı!");
    Console.WriteLine(new string('=', 80));
    
    // 1. Subscription Tier'ları kontrol et
    Console.WriteLine("📊 SUBSCRIPTION TIERS:");
    Console.WriteLine(new string('-', 50));
    
    var tierSql = @"
        SELECT ""Id"", ""TierName"", ""DisplayName"", ""DailyRequestLimit"", 
               ""MonthlyRequestLimit"", ""MonthlyPrice"", ""IsActive""
        FROM ""SubscriptionTiers"" 
        ORDER BY ""DisplayOrder""";
    
    await using var tierCmd = new NpgsqlCommand(tierSql, connection);
    await using var tierReader = await tierCmd.ExecuteReaderAsync();
    
    Console.WriteLine($"{"ID",-3} | {"Tier",-5} | {"Name",-12} | {"Daily",-5} | {"Monthly",-7} | {"Price",-8} | {"Active",-6}");
    Console.WriteLine(new string('-', 60));
    
    while (await tierReader.ReadAsync())
    {
        var id = tierReader.GetInt32(0);
        var tierName = tierReader.GetString(1);
        var displayName = tierReader.GetString(2);
        var dailyLimit = tierReader.GetInt32(3);
        var monthlyLimit = tierReader.GetInt32(4);
        var monthlyPrice = tierReader.GetDecimal(5);
        var isActive = tierReader.GetBoolean(6);
        
        Console.WriteLine($"{id,-3} | {tierName,-5} | {displayName,-12} | {dailyLimit,-5} | {monthlyLimit,-7} | ₺{monthlyPrice,-7:F2} | {isActive,-6}");
    }
    
    await tierReader.CloseAsync();
    
    Console.WriteLine("\n🔍 TRIAL TIER DETAYLI BİLGİ:");
    Console.WriteLine(new string('-', 50));
    
    var trialDetailSql = @"
        SELECT ""Id"", ""TierName"", ""DisplayName"", ""Description"",
               ""DailyRequestLimit"", ""MonthlyRequestLimit"", ""MonthlyPrice"",
               ""PrioritySupport"", ""AdvancedAnalytics"", ""ApiAccess"",
               ""ResponseTimeHours"", ""AdditionalFeatures"", ""CreatedDate""
        FROM ""SubscriptionTiers"" 
        WHERE ""TierName"" = 'Trial'";
    
    await using var trialCmd = new NpgsqlCommand(trialDetailSql, connection);
    await using var trialReader = await trialCmd.ExecuteReaderAsync();
    
    if (await trialReader.ReadAsync())
    {
        Console.WriteLine($"✅ Trial Tier Bulundu!");
        Console.WriteLine($"   ID: {trialReader.GetInt32(0)}");
        Console.WriteLine($"   İsim: {trialReader.GetString(2)}");
        Console.WriteLine($"   Açıklama: {trialReader.GetString(3)}");
        Console.WriteLine($"   Günlük Limit: {trialReader.GetInt32(4)} analiz");
        Console.WriteLine($"   Aylık Limit: {trialReader.GetInt32(5)} analiz");
        Console.WriteLine($"   Aylık Ücret: ₺{trialReader.GetDecimal(6):F2}");
        Console.WriteLine($"   Öncelikli Destek: {trialReader.GetBoolean(7)}");
        Console.WriteLine($"   Gelişmiş Analytics: {trialReader.GetBoolean(8)}");
        Console.WriteLine($"   API Erişimi: {trialReader.GetBoolean(9)}");
        Console.WriteLine($"   Yanıt Süresi: {trialReader.GetInt32(10)} saat");
        Console.WriteLine($"   Oluşturma Tarihi: {trialReader.GetDateTime(12):yyyy-MM-dd HH:mm:ss}");
    }
    else
    {
        Console.WriteLine("❌ Trial Tier BULUNAMADI!");
    }
    
    await trialReader.CloseAsync();
    
    // 2. User Subscription'ları kontrol et
    Console.WriteLine("\n👥 KULLANICI SUBSCRIPTION'LARI:");
    Console.WriteLine(new string('-', 50));
    
    var userSubSql = @"
        SELECT COUNT(*) as TotalSubscriptions,
               COUNT(CASE WHEN ""IsActive"" = true THEN 1 END) as ActiveSubscriptions,
               COUNT(CASE WHEN ""IsTrialSubscription"" = true THEN 1 END) as TrialSubscriptions
        FROM ""UserSubscriptions""";
    
    await using var userSubCmd = new NpgsqlCommand(userSubSql, connection);
    await using var userSubReader = await userSubCmd.ExecuteReaderAsync();
    
    if (await userSubReader.ReadAsync())
    {
        var total = userSubReader.GetInt64(0);
        var active = userSubReader.GetInt64(1);
        var trial = userSubReader.GetInt64(2);
        
        Console.WriteLine($"📊 Toplam Subscription: {total}");
        Console.WriteLine($"✅ Aktif Subscription: {active}");
        Console.WriteLine($"🆓 Trial Subscription: {trial}");
    }
    
    await userSubReader.CloseAsync();
    
    // 3. Son 10 kullanıcı ve subscription durumları
    Console.WriteLine("\n👤 SON 10 KULLANICI VE SUBSCRIPTION DURUMLARI:");
    Console.WriteLine(new string('-', 80));
    
    var recentUsersSql = @"
        SELECT u.""Id"" as UserId, u.""FirstName"", u.""LastName"", u.""Email"",
               us.""Id"" as SubscriptionId, st.""TierName"", st.""DisplayName"",
               us.""IsActive"", us.""IsTrialSubscription"", us.""StartDate"", us.""EndDate"",
               us.""CurrentDailyUsage"", us.""CurrentMonthlyUsage""
        FROM ""Users"" u
        LEFT JOIN ""UserSubscriptions"" us ON u.""Id"" = us.""UserId"" AND us.""IsActive"" = true
        LEFT JOIN ""SubscriptionTiers"" st ON us.""SubscriptionTierId"" = st.""Id""
        ORDER BY u.""Id"" DESC
        LIMIT 10";
    
    await using var recentCmd = new NpgsqlCommand(recentUsersSql, connection);
    await using var recentReader = await recentCmd.ExecuteReaderAsync();
    
    Console.WriteLine($"{"UserID",-6} | {"Name",-15} | {"Email",-25} | {"Tier",-5} | {"Trial",-5} | {"Usage",-10}");
    Console.WriteLine(new string('-', 80));
    
    while (await recentReader.ReadAsync())
    {
        var userId = recentReader.GetInt32(0);
        var firstName = recentReader.IsDBNull(1) ? "" : recentReader.GetString(1);
        var lastName = recentReader.IsDBNull(2) ? "" : recentReader.GetString(2);
        var email = recentReader.IsDBNull(3) ? "" : recentReader.GetString(3);
        var tierName = recentReader.IsDBNull(5) ? "NONE" : recentReader.GetString(5);
        var isTrial = recentReader.IsDBNull(8) ? false : recentReader.GetBoolean(8);
        var dailyUsage = recentReader.IsDBNull(11) ? 0 : recentReader.GetInt32(11);
        var monthlyUsage = recentReader.IsDBNull(12) ? 0 : recentReader.GetInt32(12);
        
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrEmpty(fullName)) fullName = "N/A";
        
        Console.WriteLine($"{userId,-6} | {fullName,-15} | {email,-25} | {tierName,-5} | {(isTrial ? "YES" : "NO"),-5} | {dailyUsage}/{monthlyUsage}");
    }
    
    await recentReader.CloseAsync();
    
    // 4. Farmer rolündeki kullanıcıları kontrol et
    Console.WriteLine("\n🚜 FARMER ROLÜNDE KULLANICILAR:");
    Console.WriteLine(new string('-', 50));
    
    var farmerSql = @"
        SELECT COUNT(DISTINCT u.""Id"") as TotalFarmers,
               COUNT(DISTINCT us.""UserId"") as FarmersWithSubscription
        FROM ""Users"" u
        INNER JOIN ""UserOperationClaims"" uoc ON u.""Id"" = uoc.""UserId""
        INNER JOIN ""OperationClaims"" oc ON uoc.""OperationClaimId"" = oc.""Id""
        LEFT JOIN ""UserSubscriptions"" us ON u.""Id"" = us.""UserId"" AND us.""IsActive"" = true
        WHERE oc.""Name"" = 'Farmer'";
    
    await using var farmerCmd = new NpgsqlCommand(farmerSql, connection);
    await using var farmerReader = await farmerCmd.ExecuteReaderAsync();
    
    if (await farmerReader.ReadAsync())
    {
        var totalFarmers = farmerReader.GetInt64(0);
        var farmersWithSub = farmerReader.GetInt64(1);
        
        Console.WriteLine($"🚜 Toplam Farmer: {totalFarmers}");
        Console.WriteLine($"📊 Subscription'lı Farmer: {farmersWithSub}");
        Console.WriteLine($"❓ Subscription'sız Farmer: {totalFarmers - farmersWithSub}");
        
        if (totalFarmers > 0)
        {
            var percentage = (double)farmersWithSub / totalFarmers * 100;
            Console.WriteLine($"📈 Kapsama Oranı: %{percentage:F1}");
        }
    }
    
    await farmerReader.CloseAsync();
    
    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("✅ Veritabanı kontrolü tamamlandı!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Hata: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}