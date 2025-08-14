#r "nuget: Npgsql, 8.0.4"

using System;
using System.Threading.Tasks;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("📋 STAGING DATABASE TABLOLARI:");
    Console.WriteLine(new string('=', 50));
    
    // List all tables
    var tablesSql = @"
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
        ORDER BY table_name";
    
    await using var tablesCmd = new NpgsqlCommand(tablesSql, connection);
    await using var tablesReader = await tablesCmd.ExecuteReaderAsync();
    
    Console.WriteLine("Mevcut Tablolar:");
    while (await tablesReader.ReadAsync())
    {
        Console.WriteLine($"  - {tablesReader.GetString(0)}");
    }
    
    await tablesReader.CloseAsync();
    
    // Check Users table columns
    Console.WriteLine("\n📊 USERS TABLOSU KOLONLARI:");
    Console.WriteLine(new string('-', 40));
    
    var columnsSql = @"
        SELECT column_name, data_type 
        FROM information_schema.columns 
        WHERE table_name = 'Users' 
        ORDER BY ordinal_position";
    
    await using var columnsCmd = new NpgsqlCommand(columnsSql, connection);
    await using var columnsReader = await columnsCmd.ExecuteReaderAsync();
    
    while (await columnsReader.ReadAsync())
    {
        Console.WriteLine($"  - {columnsReader.GetString(0)} ({columnsReader.GetString(1)})");
    }
    
    await columnsReader.CloseAsync();
    
    // Simple count query to test Users table
    Console.WriteLine("\n🔢 KULLANICI SAYILARI:");
    Console.WriteLine(new string('-', 30));
    
    var countSql = @"SELECT COUNT(*) FROM ""Users""";
    await using var countCmd = new NpgsqlCommand(countSql, connection);
    var userCount = (long)await countCmd.ExecuteScalarAsync();
    Console.WriteLine($"  Toplam Kullanıcı: {userCount}");
    
    // Count subscriptions  
    var subCountSql = @"SELECT COUNT(*) FROM ""UserSubscriptions""";
    await using var subCountCmd = new NpgsqlCommand(subCountSql, connection);
    var subCount = (long)await subCountCmd.ExecuteScalarAsync();
    Console.WriteLine($"  Toplam Subscription: {subCount}");
    
    // Count active subscriptions
    var activeSubSql = @"SELECT COUNT(*) FROM ""UserSubscriptions"" WHERE ""IsActive"" = true";
    await using var activeSubCmd = new NpgsqlCommand(activeSubSql, connection);
    var activeSubCount = (long)await activeSubCmd.ExecuteScalarAsync();
    Console.WriteLine($"  Aktif Subscription: {activeSubCount}");
    
    // Count trial subscriptions
    var trialSql = @"SELECT COUNT(*) FROM ""UserSubscriptions"" WHERE ""IsTrialSubscription"" = true AND ""IsActive"" = true";
    await using var trialCmd = new NpgsqlCommand(trialSql, connection);
    var trialCount = (long)await trialCmd.ExecuteScalarAsync();
    Console.WriteLine($"  Aktif Trial Subscription: {trialCount}");
    
    Console.WriteLine("\n" + new string('=', 50));
    Console.WriteLine("✅ Tablo analizi tamamlandı!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Hata: {ex.Message}");
}