#r "nuget: Npgsql, 8.0.4"
using Npgsql;
using System.Security.Cryptography;
using System.Text;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

// Password hashing using the same approach as HashingHelper
static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
{
    using var hmac = new HMACSHA512();
    passwordSalt = hmac.Key;
    passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
}

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    string newPassword = "TestPass123!";
    Console.WriteLine($"🔧 Setting password for User ID 34 to: {newPassword}");
    
    // Generate password hash and salt using the same method as the application
    CreatePasswordHash(newPassword, out byte[] passwordHash, out byte[] passwordSalt);
    
    Console.WriteLine($"📝 Generated salt length: {passwordSalt.Length} bytes");
    Console.WriteLine($"📝 Generated hash length: {passwordHash.Length} bytes");
    
    // Update password for user 34
    var updateQuery = @"UPDATE ""Users"" SET ""PasswordHash"" = @hash, ""PasswordSalt"" = @salt WHERE ""UserId"" = 34";
    using var updateCmd = new NpgsqlCommand(updateQuery, connection);
    updateCmd.Parameters.AddWithValue("hash", passwordHash);
    updateCmd.Parameters.AddWithValue("salt", passwordSalt);
    
    int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
    
    if (rowsAffected > 0)
    {
        Console.WriteLine("✅ Password updated successfully!");
        Console.WriteLine($"\n🔑 Test login with:");
        Console.WriteLine($"  Email: pg-sponsor@test.com");
        Console.WriteLine($"  Password: {newPassword}");
        
        // Verify the user exists and has correct details
        var verifyQuery = @"SELECT ""UserId"", ""FullName"", ""Email"", ""Status"" FROM ""Users"" WHERE ""UserId"" = 34";
        using var verifyCmd = new NpgsqlCommand(verifyQuery, connection);
        using var reader = await verifyCmd.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            Console.WriteLine($"\n📋 User details:");
            Console.WriteLine($"  ID: {reader["UserId"]}");
            Console.WriteLine($"  Name: {reader["FullName"]}");
            Console.WriteLine($"  Email: {reader["Email"]}");
            Console.WriteLine($"  Status: {reader["Status"]}");
        }
    }
    else
    {
        Console.WriteLine("❌ Failed to update password - user not found");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}