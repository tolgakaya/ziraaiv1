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
    
    // En son oluşturulan kullanıcıyı bul
    var latestUserQuery = @"
        SELECT u.""Id"", u.""Email"", u.""FullName"", u.""Status""
        FROM ""Users"" u
        ORDER BY u.""Id"" DESC
        LIMIT 5;";
    
    using var latestUserCommand = new NpgsqlCommand(latestUserQuery, connection);
    using var latestUserReader = await latestUserCommand.ExecuteReaderAsync();
    
    Console.WriteLine($"\n👤 En Son Oluşturulan 5 Kullanıcı:");
    while (await latestUserReader.ReadAsync())
    {
        var userId = latestUserReader.GetInt32(0);
        var email = latestUserReader.GetString(1);
        var fullName = latestUserReader.GetString(2);
        var status = latestUserReader.GetBoolean(3);
        
        Console.WriteLine($"   ID: {userId} | Email: {email} | Ad: {fullName} | Aktif: {status}");
    }
    
    await latestUserReader.CloseAsync();
    
    // Sponsor grubu var mı kontrol et
    var sponsorGroupQuery = @"
        SELECT ""Id"", ""GroupName"" FROM ""Groups"" 
        WHERE ""GroupName"" ILIKE '%sponsor%' OR ""GroupName"" ILIKE '%admin%';";
    
    using var sponsorGroupCommand = new NpgsqlCommand(sponsorGroupQuery, connection);
    using var sponsorGroupReader = await sponsorGroupCommand.ExecuteReaderAsync();
    
    Console.WriteLine($"\n👥 Sponsor/Admin Grupları:");
    var sponsorGroupExists = false;
    while (await sponsorGroupReader.ReadAsync())
    {
        sponsorGroupExists = true;
        var groupId = sponsorGroupReader.GetInt32(0);
        var groupName = sponsorGroupReader.GetString(1);
        Console.WriteLine($"   ID: {groupId} | Grup: {groupName}");
    }
    
    if (!sponsorGroupExists)
    {
        Console.WriteLine("   ❌ Sponsor grubu bulunamadı!");
    }
    
    await sponsorGroupReader.CloseAsync();
    
    // Sponsor grubu yoksa oluştur
    if (!sponsorGroupExists)
    {
        Console.WriteLine("\n🔧 Sponsor grubu oluşturuluyor...");
        
        var createGroupQuery = @"
            INSERT INTO ""Groups"" (""GroupName"")
            VALUES ('Sponsor')
            RETURNING ""Id"";";
        
        using var createGroupCommand = new NpgsqlCommand(createGroupQuery, connection);
        var newGroupId = await createGroupCommand.ExecuteScalarAsync();
        
        Console.WriteLine($"✅ Sponsor grubu oluşturuldu (ID: {newGroupId})");
    }
    
    // En son kullanıcının ID'sini al
    Console.WriteLine($"\nLütfen sponsor yapmak istediğiniz kullanıcının ID'sini girin:");
    var userIdStr = Console.ReadLine();
    
    if (int.TryParse(userIdStr, out var selectedUserId))
    {
        // Kullanıcıyı Sponsor grubuna ekle
        var sponsorGroupId = await GetGroupIdAsync(connection, "Sponsor");
        
        if (sponsorGroupId.HasValue)
        {
            var addUserToGroupQuery = @"
                INSERT INTO ""UserGroups"" (""UserId"", ""GroupId"")
                VALUES (@userId, @groupId)
                ON CONFLICT (""UserId"", ""GroupId"") DO NOTHING;";
            
            using var addUserCommand = new NpgsqlCommand(addUserToGroupQuery, connection);
            addUserCommand.Parameters.AddWithValue("@userId", selectedUserId);
            addUserCommand.Parameters.AddWithValue("@groupId", sponsorGroupId.Value);
            
            await addUserCommand.ExecuteNonQueryAsync();
            Console.WriteLine($"✅ Kullanıcı {selectedUserId} Sponsor grubuna eklendi!");
            
            // Doğrulama
            var verifyQuery = @"
                SELECT g.""GroupName""
                FROM ""UserGroups"" ug
                INNER JOIN ""Groups"" g ON ug.""GroupId"" = g.""Id""
                WHERE ug.""UserId"" = @userId;";
            
            using var verifyCommand = new NpgsqlCommand(verifyQuery, connection);
            verifyCommand.Parameters.AddWithValue("@userId", selectedUserId);
            
            using var verifyReader = await verifyCommand.ExecuteReaderAsync();
            
            Console.WriteLine($"\n✅ Kullanıcı {selectedUserId}'nin güncel rolleri:");
            while (await verifyReader.ReadAsync())
            {
                var groupName = verifyReader.GetString(0);
                Console.WriteLine($"   - {groupName}");
            }
        }
    }
    
    await connection.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

static async Task<int?> GetGroupIdAsync(NpgsqlConnection connection, string groupName)
{
    var query = @"SELECT ""Id"" FROM ""Groups"" WHERE ""GroupName"" = @groupName;";
    using var command = new NpgsqlCommand(query, connection);
    command.Parameters.AddWithValue("@groupName", groupName);
    
    var result = await command.ExecuteScalarAsync();
    return result as int?;
}

Console.WriteLine("\n🚀 İşlem tamamlandı!");