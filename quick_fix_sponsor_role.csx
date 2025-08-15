#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using System;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("✅ Connected to PostgreSQL database successfully.");
    
    // Sponsor grubu var mı kontrol et, yoksa oluştur
    var sponsorGroupQuery = @"
        SELECT ""Id"" FROM ""Groups"" 
        WHERE ""GroupName"" = 'Sponsor';";
    
    using var sponsorGroupCommand = new NpgsqlCommand(sponsorGroupQuery, connection);
    var sponsorGroupId = await sponsorGroupCommand.ExecuteScalarAsync();
    
    if (sponsorGroupId == null)
    {
        Console.WriteLine("🔧 Sponsor grubu bulunamadı, oluşturuluyor...");
        
        var createGroupQuery = @"
            INSERT INTO ""Groups"" (""GroupName"")
            VALUES ('Sponsor')
            RETURNING ""Id"";";
        
        using var createGroupCommand = new NpgsqlCommand(createGroupQuery, connection);
        sponsorGroupId = await createGroupCommand.ExecuteScalarAsync();
        
        Console.WriteLine($"✅ Sponsor grubu oluşturuldu (ID: {sponsorGroupId})");
    }
    else
    {
        Console.WriteLine($"✅ Sponsor grubu mevcut (ID: {sponsorGroupId})");
    }
    
    // En son 3 kullanıcıyı göster
    var latestUsersQuery = @"
        SELECT u.""UserId"", u.""Email"", u.""FullName""
        FROM ""Users"" u
        ORDER BY u.""UserId"" DESC
        LIMIT 3;";
    
    using var latestUsersCommand = new NpgsqlCommand(latestUsersQuery, connection);
    using var latestUsersReader = await latestUsersCommand.ExecuteReaderAsync();
    
    Console.WriteLine($"\n👤 En Son Kayıt Olan Kullanıcılar:");
    while (await latestUsersReader.ReadAsync())
    {
        var userId = latestUsersReader.GetInt32(0);
        var email = latestUsersReader.GetString(1);
        var fullName = latestUsersReader.GetString(2);
        
        Console.WriteLine($"   ID: {userId} | {email} | {fullName}");
    }
    
    await latestUsersReader.CloseAsync();
    
    // En son kullanıcıyı otomatik sponsor yap
    var lastUserQuery = @"
        SELECT ""UserId"", ""Email"" FROM ""Users"" 
        ORDER BY ""UserId"" DESC 
        LIMIT 1;";
    
    using var lastUserCommand = new NpgsqlCommand(lastUserQuery, connection);
    using var lastUserReader = await lastUserCommand.ExecuteReaderAsync();
    
    if (await lastUserReader.ReadAsync())
    {
        var lastUserId = lastUserReader.GetInt32(0);
        var lastUserEmail = lastUserReader.GetString(1);
        
        await lastUserReader.CloseAsync();
        
        Console.WriteLine($"\n🎯 En son kullanıcıyı ({lastUserEmail}) Sponsor yapıyor...");
        
        // Kullanıcıyı Sponsor grubuna ekle
        var addUserToGroupQuery = @"
            INSERT INTO ""UserGroups"" (""UserId"", ""GroupId"")
            VALUES (@userId, @groupId)
            ON CONFLICT (""UserId"", ""GroupId"") DO NOTHING;";
        
        using var addUserCommand = new NpgsqlCommand(addUserToGroupQuery, connection);
        addUserCommand.Parameters.AddWithValue("@userId", lastUserId);
        addUserCommand.Parameters.AddWithValue("@groupId", sponsorGroupId);
        
        await addUserCommand.ExecuteNonQueryAsync();
        
        Console.WriteLine($"✅ {lastUserEmail} kullanıcısı Sponsor grubuna eklendi!");
        
        // Doğrulama
        var verifyQuery = @"
            SELECT g.""GroupName""
            FROM ""UserGroups"" ug
            INNER JOIN ""Groups"" g ON ug.""GroupId"" = g.""Id""
            WHERE ug.""UserId"" = @userId;";
        
        using var verifyCommand = new NpgsqlCommand(verifyQuery, connection);
        verifyCommand.Parameters.AddWithValue("@userId", lastUserId);
        
        using var verifyReader = await verifyCommand.ExecuteReaderAsync();
        
        Console.WriteLine($"\n✅ {lastUserEmail} kullanıcısının rolleri:");
        while (await verifyReader.ReadAsync())
        {
            var groupName = verifyReader.GetString(0);
            Console.WriteLine($"   - {groupName}");
        }
        
        Console.WriteLine($"\n🎉 Artık {lastUserEmail} ile giriş yapıp sponsor profili oluşturabilirsiniz!");
    }
    
    await connection.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

Console.WriteLine("\n🚀 İşlem tamamlandı!");