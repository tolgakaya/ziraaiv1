#r "nuget: Npgsql, 8.0.4"
using Npgsql;
using System.Security.Cryptography;
using System.Text;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

// Password verification using the same approach as HashingHelper
static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
{
    using (var hmac = new HMACSHA512(passwordSalt))
    {
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        for (var i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != passwordHash[i])
            {
                return false;
            }
        }
    }
    return true;
}

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 Verifying password in database for User ID 34...");
    
    // Get user with password data
    var query = @"SELECT ""UserId"", ""FullName"", ""Email"", ""Status"", ""PasswordHash"", ""PasswordSalt"" FROM ""Users"" WHERE ""UserId"" = 34";
    using var cmd = new NpgsqlCommand(query, connection);
    using var reader = await cmd.ExecuteReaderAsync();
    
    if (await reader.ReadAsync())
    {
        var userId = reader["UserId"];
        var fullName = reader["FullName"];
        var email = reader["Email"];
        var status = reader["Status"];
        
        var passwordHashBytes = reader["PasswordHash"] as byte[];
        var passwordSaltBytes = reader["PasswordSalt"] as byte[];
        
        Console.WriteLine($"📋 User details:");
        Console.WriteLine($"  ID: {userId}");
        Console.WriteLine($"  Name: {fullName}");
        Console.WriteLine($"  Email: {email}");
        Console.WriteLine($"  Status: {status}");
        Console.WriteLine($"  Has Password Hash: {passwordHashBytes != null} (Length: {passwordHashBytes?.Length ?? 0})");
        Console.WriteLine($"  Has Password Salt: {passwordSaltBytes != null} (Length: {passwordSaltBytes?.Length ?? 0})");
        
        if (passwordHashBytes != null && passwordSaltBytes != null)
        {
            string testPassword = "TestPass123!";
            bool isValid = VerifyPasswordHash(testPassword, passwordHashBytes, passwordSaltBytes);
            
            Console.WriteLine($"\n🔐 Password verification test:");
            Console.WriteLine($"  Test password: {testPassword}");
            Console.WriteLine($"  Verification result: {(isValid ? "✅ VALID" : "❌ INVALID")}");
        }
        else
        {
            Console.WriteLine("\n❌ No password hash or salt found!");
        }
    }
    else
    {
        Console.WriteLine("❌ User ID 34 not found!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}