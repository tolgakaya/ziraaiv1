#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 KULLANICI VE FARMER ANALIZI:");
    Console.WriteLine(new string('=', 60));
    
    // Son kullanıcıları ve subscription'larını listele
    var usersSql = @"
        SELECT u.""Id"" as UserId, u.""FirstName"", u.""LastName"", u.""Email"",
               us.""Id"" as SubId, st.""TierName"",
               us.""IsActive"" as SubActive, us.""IsTrialSubscription"",
               us.""CurrentDailyUsage"", us.""CurrentMonthlyUsage"",
               us.""StartDate"", us.""EndDate""
        FROM ""Users"" u
        LEFT JOIN ""UserSubscriptions"" us ON u.""Id"" = us.""UserId"" AND us.""IsActive"" = true
        LEFT JOIN ""SubscriptionTiers"" st ON us.""SubscriptionTierId"" = st.""Id""
        ORDER BY u.""Id"" DESC
        LIMIT 10";
    
    await using var usersCmd = new NpgsqlCommand(usersSql, connection);
    await using var usersReader = await usersCmd.ExecuteReaderAsync();
    
    Console.WriteLine("👥 SON 10 KULLANICI:");
    Console.WriteLine($"{"ID",-4} | {"Name",-20} | {"Email",-25} | {"Tier",-6} | {"Trial?",-6} | {"Usage",-8} | {"End Date",-12}");
    Console.WriteLine(new string('-', 90));
    
    while (await usersReader.ReadAsync())
    {
        var userId = usersReader.GetInt32(0);
        var firstName = usersReader.IsDBNull(1) ? "" : usersReader.GetString(1);
        var lastName = usersReader.IsDBNull(2) ? "" : usersReader.GetString(2);
        var email = usersReader.IsDBNull(3) ? "no-email" : usersReader.GetString(3);
        var tierName = usersReader.IsDBNull(5) ? "NONE" : usersReader.GetString(5);
        var isTrial = usersReader.IsDBNull(7) ? false : usersReader.GetBoolean(7);
        var dailyUsage = usersReader.IsDBNull(8) ? 0 : usersReader.GetInt32(8);
        var monthlyUsage = usersReader.IsDBNull(9) ? 0 : usersReader.GetInt32(9);
        var endDate = usersReader.IsDBNull(11) ? "N/A" : usersReader.GetDateTime(11).ToString("MM/dd/yyyy");
        
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrEmpty(fullName)) fullName = "N/A";
        
        Console.WriteLine($"{userId,-4} | {fullName,-20} | {email,-25} | {tierName,-6} | {(isTrial ? "YES" : "NO"),-6} | {dailyUsage}/{monthlyUsage,-6} | {endDate,-12}");
    }
    
    await usersReader.CloseAsync();
    
    Console.WriteLine();
    
    // Farmer rolündeki kullanıcıları bul
    var farmersSql = @"
        SELECT u.""Id"", u.""FirstName"", u.""LastName"", u.""Email"",
               us.""Id"" as SubId, st.""TierName"",
               us.""IsTrialSubscription"",
               us.""CurrentDailyUsage"", us.""CurrentMonthlyUsage""
        FROM ""Users"" u
        INNER JOIN ""UserOperationClaims"" uoc ON u.""Id"" = uoc.""UserId""
        INNER JOIN ""OperationClaims"" oc ON uoc.""OperationClaimId"" = oc.""Id""
        LEFT JOIN ""UserSubscriptions"" us ON u.""Id"" = us.""UserId"" AND us.""IsActive"" = true
        LEFT JOIN ""SubscriptionTiers"" st ON us.""SubscriptionTierId"" = st.""Id""
        WHERE oc.""Name"" = 'Farmer'
        ORDER BY u.""Id"" DESC";
    
    await using var farmersCmd = new NpgsqlCommand(farmersSql, connection);
    await using var farmersReader = await farmersCmd.ExecuteReaderAsync();
    
    Console.WriteLine("🚜 FARMER ROLÜNDEKI KULLANICILAR:");
    Console.WriteLine($"{"ID",-4} | {"Name",-20} | {"Email",-25} | {"Tier",-6} | {"Trial?",-6} | {"Usage",-8}");
    Console.WriteLine(new string('-', 78));
    
    int farmerCount = 0;
    int farmersWithTrial = 0;
    int farmersWithSubscription = 0;
    
    while (await farmersReader.ReadAsync())
    {
        farmerCount++;
        
        var userId = farmersReader.GetInt32(0);
        var firstName = farmersReader.IsDBNull(1) ? "" : farmersReader.GetString(1);
        var lastName = farmersReader.IsDBNull(2) ? "" : farmersReader.GetString(2);
        var email = farmersReader.IsDBNull(3) ? "no-email" : farmersReader.GetString(3);
        var tierName = farmersReader.IsDBNull(5) ? "NONE" : farmersReader.GetString(5);
        var isTrial = farmersReader.IsDBNull(6) ? false : farmersReader.GetBoolean(6);
        var dailyUsage = farmersReader.IsDBNull(7) ? 0 : farmersReader.GetInt32(7);
        var monthlyUsage = farmersReader.IsDBNull(8) ? 0 : farmersReader.GetInt32(8);
        
        if (!farmersReader.IsDBNull(4)) farmersWithSubscription++;
        if (isTrial) farmersWithTrial++;
        
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrEmpty(fullName)) fullName = "N/A";
        
        Console.WriteLine($"{userId,-4} | {fullName,-20} | {email,-25} | {tierName,-6} | {(isTrial ? "YES" : "NO"),-6} | {dailyUsage}/{monthlyUsage,-6}");
    }
    
    await farmersReader.CloseAsync();
    
    Console.WriteLine();
    Console.WriteLine("📊 FARMER İSTATİSTİKLERİ:");
    Console.WriteLine($"   🚜 Toplam Farmer: {farmerCount}");
    Console.WriteLine($"   📊 Subscription'lı Farmer: {farmersWithSubscription}");
    Console.WriteLine($"   🆓 Trial Subscription'lı Farmer: {farmersWithTrial}");
    Console.WriteLine($"   ❌ Subscription'sız Farmer: {farmerCount - farmersWithSubscription}");
    
    if (farmerCount > 0)
    {
        var coveragePercent = (double)farmersWithSubscription / farmerCount * 100;
        var trialPercent = (double)farmersWithTrial / farmerCount * 100;
        Console.WriteLine($"   📈 Subscription Kapsama Oranı: %{coveragePercent:F1}");
        Console.WriteLine($"   🎯 Trial Kapsama Oranı: %{trialPercent:F1}");
    }
    
    Console.WriteLine("\n" + new string('=', 60));
    Console.WriteLine("✅ Kullanıcı analizi tamamlandı!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Hata: {ex.Message}");
}