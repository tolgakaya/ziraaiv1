#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using System;
using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

Console.WriteLine("Lütfen yeni kayıt yaptığınız kullanıcının email adresini girin:");
var userEmail = Console.ReadLine();

if (string.IsNullOrEmpty(userEmail))
{
    Console.WriteLine("❌ Email adresi gerekli!");
    return;
}

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("✅ Connected to PostgreSQL database successfully.");
    
    // Kullanıcı bilgilerini kontrol et
    var userQuery = @"
        SELECT u.""Id"", u.""Email"", u.""FullName"", u.""Status""
        FROM ""Users"" u
        WHERE u.""Email"" = @email;";
    
    using var userCommand = new NpgsqlCommand(userQuery, connection);
    userCommand.Parameters.AddWithValue("@email", userEmail);
    
    using var userReader = await userCommand.ExecuteReaderAsync();
    
    if (!await userReader.ReadAsync())
    {
        Console.WriteLine($"❌ '{userEmail}' email adresi ile kullanıcı bulunamadı!");
        return;
    }
    
    var userId = userReader.GetInt32(0);
    var email = userReader.GetString(1);
    var fullName = userReader.GetString(2);
    var status = userReader.GetBoolean(3);
    
    Console.WriteLine($"\n👤 Kullanıcı Bilgileri:");
    Console.WriteLine($"   ID: {userId}");
    Console.WriteLine($"   Email: {email}");
    Console.WriteLine($"   Ad Soyad: {fullName}");
    Console.WriteLine($"   Aktif: {status}");
    
    await userReader.CloseAsync();
    
    // Kullanıcının gruplarını kontrol et
    var groupQuery = @"
        SELECT g.""GroupName""
        FROM ""UserGroups"" ug
        INNER JOIN ""Groups"" g ON ug.""GroupId"" = g.""Id""
        WHERE ug.""UserId"" = @userId;";
    
    using var groupCommand = new NpgsqlCommand(groupQuery, connection);
    groupCommand.Parameters.AddWithValue("@userId", userId);
    
    using var groupReader = await groupCommand.ExecuteReaderAsync();
    
    Console.WriteLine($"\n👥 Kullanıcının Grupları:");
    var hasGroups = false;
    while (await groupReader.ReadAsync())
    {
        hasGroups = true;
        var groupName = groupReader.GetString(0);
        Console.WriteLine($"   - {groupName}");
    }
    
    if (!hasGroups)
    {
        Console.WriteLine("   ❌ Kullanıcının hiç grubu yok!");
    }
    
    await groupReader.CloseAsync();
    
    // Mevcut grupları listele
    var allGroupsQuery = @"SELECT ""Id"", ""GroupName"" FROM ""Groups"" ORDER BY ""GroupName"";";
    
    using var allGroupsCommand = new NpgsqlCommand(allGroupsQuery, connection);
    using var allGroupsReader = await allGroupsCommand.ExecuteReaderAsync();
    
    Console.WriteLine($"\n📋 Sistemdeki Tüm Gruplar:");
    while (await allGroupsReader.ReadAsync())
    {
        var groupId = allGroupsReader.GetInt32(0);
        var groupName = allGroupsReader.GetString(1);
        Console.WriteLine($"   ID: {groupId} - {groupName}");
    }
    
    await allGroupsReader.CloseAsync();
    
    // Sponsor grubu var mı kontrol et
    var sponsorGroupQuery = @"
        SELECT ""Id"" FROM ""Groups"" 
        WHERE ""GroupName"" = 'Sponsor' OR ""GroupName"" = 'sponsor';";
    
    using var sponsorGroupCommand = new NpgsqlCommand(sponsorGroupQuery, connection);
    var sponsorGroupId = await sponsorGroupCommand.ExecuteScalarAsync();
    
    if (sponsorGroupId == null)
    {
        Console.WriteLine("\n❌ 'Sponsor' grubu sistemde bulunamadı!");
        Console.WriteLine("Grup oluşturmak gerekebilir.");
    }
    else
    {
        Console.WriteLine($"\n✅ Sponsor grubu mevcut (ID: {sponsorGroupId})");
        
        // Kullanıcıyı Sponsor grubuna ekle
        Console.WriteLine($"\nKullanıcıyı Sponsor grubuna eklemek ister misiniz? (y/n):");
        var answer = Console.ReadLine();
        
        if (answer?.ToLower() == "y" || answer?.ToLower() == "yes")
        {
            var addUserQuery = @"
                INSERT INTO ""UserGroups"" (""UserId"", ""GroupId"")
                VALUES (@userId, @groupId)
                ON CONFLICT DO NOTHING;";
            
            using var addUserCommand = new NpgsqlCommand(addUserQuery, connection);
            addUserCommand.Parameters.AddWithValue("@userId", userId);
            addUserCommand.Parameters.AddWithValue("@groupId", sponsorGroupId);
            
            await addUserCommand.ExecuteNonQueryAsync();
            Console.WriteLine("✅ Kullanıcı Sponsor grubuna eklendi!");
        }
    }
    
    await connection.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

Console.WriteLine("\n🚀 Kontrol tamamlandı!");