#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 EN SON EKLENEn KULLANICI KONTROLÜ:");
    Console.WriteLine(new string('=', 50));
    
    // En son kullanıcıyı bul
    var latestUserSql = @"
        SELECT ""UserId"", ""FullName"", ""Email"", ""Status"", ""RecordDate""
        FROM ""Users""
        ORDER BY ""UserId"" DESC
        LIMIT 1";
    
    await using var userCmd = new NpgsqlCommand(latestUserSql, connection);
    await using var userReader = await userCmd.ExecuteReaderAsync();
    
    int latestUserId = 0;
    if (await userReader.ReadAsync())
    {
        latestUserId = userReader.GetInt32(0);
        var fullName = userReader.IsDBNull(1) ? "N/A" : userReader.GetString(1);
        var email = userReader.IsDBNull(2) ? "N/A" : userReader.GetString(2);
        var status = userReader.IsDBNull(3) ? false : userReader.GetBoolean(3);
        var recordDate = userReader.IsDBNull(4) ? DateTime.MinValue : userReader.GetDateTime(4);
        
        Console.WriteLine($"👤 En Son Kullanıcı:");
        Console.WriteLine($"   ID: {latestUserId}");
        Console.WriteLine($"   İsim: {fullName}");
        Console.WriteLine($"   Email: {email}");
        Console.WriteLine($"   Status: {status}");
        Console.WriteLine($"   Kayıt Tarihi: {recordDate:yyyy-MM-dd HH:mm:ss}");
    }
    
    await userReader.CloseAsync();
    
    if (latestUserId > 0)
    {
        Console.WriteLine();
        
        // Bu kullanıcının subscription'ını kontrol et
        var subCheckSql = @"
            SELECT us.""Id"", us.""UserId"", us.""SubscriptionTierId"", 
                   st.""TierName"", st.""DisplayName"",
                   us.""StartDate"", us.""EndDate"", us.""IsActive"", 
                   us.""IsTrialSubscription"", us.""Status"", us.""CreatedDate""
            FROM ""UserSubscriptions"" us
            LEFT JOIN ""SubscriptionTiers"" st ON us.""SubscriptionTierId"" = st.""Id""
            WHERE us.""UserId"" = @userId
            ORDER BY us.""Id"" DESC";
        
        await using var subCmd = new NpgsqlCommand(subCheckSql, connection);
        subCmd.Parameters.AddWithValue("userId", latestUserId);
        await using var subReader = await subCmd.ExecuteReaderAsync();
        
        Console.WriteLine($"📊 UserID {latestUserId} İçin Subscription Durumu:");
        Console.WriteLine(new string('-', 40));
        
        bool hasSubscription = false;
        while (await subReader.ReadAsync())
        {
            hasSubscription = true;
            var subId = subReader.GetInt32(0);
            var tierName = subReader.IsDBNull(3) ? "N/A" : subReader.GetString(3);
            var displayName = subReader.IsDBNull(4) ? "N/A" : subReader.GetString(4);
            var startDate = subReader.IsDBNull(5) ? DateTime.MinValue : subReader.GetDateTime(5);
            var endDate = subReader.IsDBNull(6) ? DateTime.MinValue : subReader.GetDateTime(6);
            var isActive = subReader.IsDBNull(7) ? false : subReader.GetBoolean(7);
            var isTrial = subReader.IsDBNull(8) ? false : subReader.GetBoolean(8);
            var status = subReader.IsDBNull(9) ? "N/A" : subReader.GetString(9);
            var createdDate = subReader.IsDBNull(10) ? DateTime.MinValue : subReader.GetDateTime(10);
            
            Console.WriteLine($"   ✅ Subscription ID: {subId}");
            Console.WriteLine($"   🏷️ Tier: {tierName} ({displayName})");
            Console.WriteLine($"   📅 Başlangıç: {startDate:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"   📅 Bitiş: {endDate:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"   🔄 Aktif: {isActive}");
            Console.WriteLine($"   🆓 Trial: {isTrial}");
            Console.WriteLine($"   📊 Status: {status}");
            Console.WriteLine($"   📝 Oluşturma: {createdDate:yyyy-MM-dd HH:mm}");
        }
        
        if (!hasSubscription)
        {
            Console.WriteLine("   ❌ Bu kullanıcıya ait subscription BULUNAMADI!");
            Console.WriteLine();
            
            // Trial tier'ın varlığını kontrol et
            Console.WriteLine("🔍 Trial Tier Kontrolü:");
            Console.WriteLine(new string('-', 25));
            
            var trialCheckSql = @"
                SELECT ""Id"", ""TierName"", ""IsActive"", ""CreatedDate""
                FROM ""SubscriptionTiers""
                WHERE ""TierName"" = 'Trial'";
            
            await using var trialCmd = new NpgsqlCommand(trialCheckSql, connection);
            await using var trialReader = await trialCmd.ExecuteReaderAsync();
            
            if (await trialReader.ReadAsync())
            {
                var tierId = trialReader.GetInt32(0);
                var tierName = trialReader.GetString(1);
                var isActive = trialReader.GetBoolean(2);
                var createdDate = trialReader.GetDateTime(3);
                
                Console.WriteLine($"   ✅ Trial Tier Mevcut:");
                Console.WriteLine($"      ID: {tierId}");
                Console.WriteLine($"      İsim: {tierName}");
                Console.WriteLine($"      Aktif: {isActive}");
                Console.WriteLine($"      Oluşturma: {createdDate:yyyy-MM-dd HH:mm}");
                
                // Manuel subscription oluştur
                Console.WriteLine();
                Console.WriteLine("🔧 Manuel Trial Subscription Oluşturuluyor...");
                
                await trialReader.CloseAsync();
                
                var createSubSql = @"
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
                
                await using var createCmd = new NpgsqlCommand(createSubSql, connection);
                createCmd.Parameters.AddWithValue("userId", latestUserId);
                createCmd.Parameters.AddWithValue("tierId", tierId);
                createCmd.Parameters.AddWithValue("startDate", DateTime.UtcNow);
                createCmd.Parameters.AddWithValue("endDate", DateTime.UtcNow.AddDays(30));
                createCmd.Parameters.AddWithValue("isActive", true);
                createCmd.Parameters.AddWithValue("autoRenew", false);
                createCmd.Parameters.AddWithValue("paymentMethod", "Trial");
                createCmd.Parameters.AddWithValue("paidAmount", 0.0m);
                createCmd.Parameters.AddWithValue("currency", "TRY");
                createCmd.Parameters.AddWithValue("dailyUsage", 0);
                createCmd.Parameters.AddWithValue("monthlyUsage", 0);
                createCmd.Parameters.AddWithValue("lastReset", DateTime.UtcNow);
                createCmd.Parameters.AddWithValue("monthlyReset", DateTime.UtcNow);
                createCmd.Parameters.AddWithValue("status", "Active");
                createCmd.Parameters.AddWithValue("isTrial", true);
                createCmd.Parameters.AddWithValue("trialEnd", DateTime.UtcNow.AddDays(30));
                createCmd.Parameters.AddWithValue("createdDate", DateTime.UtcNow);
                createCmd.Parameters.AddWithValue("createdUserId", latestUserId);
                
                var newSubId = (int)await createCmd.ExecuteScalarAsync();
                Console.WriteLine($"   ✅ Trial subscription oluşturuldu! ID: {newSubId}");
            }
            else
            {
                Console.WriteLine("   ❌ Trial Tier BULUNAMADI!");
            }
        }
        
        await subReader.CloseAsync();
    }
    
    Console.WriteLine("\n" + new string('=', 50));
    Console.WriteLine("✅ Kontrol tamamlandı!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Hata: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}