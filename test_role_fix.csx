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
    
    // Test kullanıcısını oluşturalım (sponsorX formatında)
    var testEmail = $"sponsor{DateTime.Now.Ticks % 10000}@company.com";
    
    Console.WriteLine($"🧪 Test kullanıcısı email: {testEmail}");
    Console.WriteLine("Şimdi bu email ile register isteği gönderin:");
    Console.WriteLine($"POST /api/v1/auth/register");
    Console.WriteLine("{");
    Console.WriteLine($"  \"email\": \"{testEmail}\",");
    Console.WriteLine("  \"password\": \"SecurePass123!\",");
    Console.WriteLine("  \"fullName\": \"Test Sponsor Company\",");
    Console.WriteLine("  \"role\": \"Sponsor\"");
    Console.WriteLine("}");
    
    Console.WriteLine("\nRegister yaptıktan sonra Enter'a basın...");
    Console.ReadLine();
    
    // Register sonrası kullanıcının rollerini kontrol et
    var userQuery = @"
        SELECT u.""UserId"", u.""Email"", u.""FullName""
        FROM ""Users"" u
        WHERE u.""Email"" = @email;";
    
    using var userCommand = new NpgsqlCommand(userQuery, connection);
    userCommand.Parameters.AddWithValue("@email", testEmail);
    
    using var userReader = await userCommand.ExecuteReaderAsync();
    
    if (await userReader.ReadAsync())
    {
        var userId = userReader.GetInt32(0);
        var email = userReader.GetString(1);
        var fullName = userReader.GetString(2);
        
        Console.WriteLine($"\n👤 Kullanıcı bulundu:");
        Console.WriteLine($"   ID: {userId}");
        Console.WriteLine($"   Email: {email}");
        Console.WriteLine($"   Ad: {fullName}");
        
        await userReader.CloseAsync();
        
        // Kullanıcının rollerini kontrol et
        var rolesQuery = @"
            SELECT g.""GroupName""
            FROM ""UserGroups"" ug
            INNER JOIN ""Groups"" g ON ug.""GroupId"" = g.""Id""
            WHERE ug.""UserId"" = @userId;";
        
        using var rolesCommand = new NpgsqlCommand(rolesQuery, connection);
        rolesCommand.Parameters.AddWithValue("@userId", userId);
        
        using var rolesReader = await rolesCommand.ExecuteReaderAsync();
        
        Console.WriteLine($"\n👥 {email} kullanıcısının rolleri:");
        var hasSponsorRole = false;
        while (await rolesReader.ReadAsync())
        {
            var roleName = rolesReader.GetString(0);
            Console.WriteLine($"   - {roleName}");
            if (roleName == "Sponsor")
                hasSponsorRole = true;
        }
        
        if (hasSponsorRole)
        {
            Console.WriteLine("\n🎉 SUCCESS! Kullanıcı Sponsor rolüne sahip!");
            Console.WriteLine("✅ JsonPropertyName düzeltmesi çalıştı!");
        }
        else
        {
            Console.WriteLine("\n❌ FAIL: Kullanıcı hala Sponsor rolüne sahip değil!");
            Console.WriteLine("Register payload'ında 'role': 'Sponsor' kullandığınızdan emin olun.");
        }
    }
    else
    {
        Console.WriteLine($"\n❌ {testEmail} kullanıcısı bulunamadı!");
        Console.WriteLine("Register işlemini tekrar deneyin.");
    }
    
    await connection.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

Console.WriteLine("\n🚀 Test tamamlandı!");