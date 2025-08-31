#r "nuget: Npgsql, 8.0.4"
#r "nuget: System.Security.Cryptography.Algorithms, 4.3.1"
using Npgsql;
using System.Security.Cryptography;
using System.Text;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("🔍 Checking User ID 34 (pg-sponsor@test.com)...");
    
    // Check current user details
    var getUserQuery = @"SELECT ""Id"", ""FirstName"", ""LastName"", ""Email"", ""PasswordHash"", ""PasswordSalt"" FROM ""Users"" WHERE ""Id"" = 34";
    using var getUserCmd = new NpgsqlCommand(getUserQuery, connection);
    using var reader = await getUserCmd.ExecuteReaderAsync();
    
    if (await reader.ReadAsync())
    {
        var userId = reader["Id"];
        var firstName = reader["FirstName"];
        var lastName = reader["LastName"];
        var email = reader["Email"];
        var currentHash = reader["PasswordHash"];
        var currentSalt = reader["PasswordSalt"];
        
        Console.WriteLine($"📋 User found:");
        Console.WriteLine($"  ID: {userId}");
        Console.WriteLine($"  Name: {firstName} {lastName}");
        Console.WriteLine($"  Email: {email}");
        Console.WriteLine($"  Has Password: {!string.IsNullOrEmpty(currentHash?.ToString())}");
        Console.WriteLine($"  Has Salt: {!string.IsNullOrEmpty(currentSalt?.ToString())}");
    }
    else
    {
        Console.WriteLine("❌ User ID 34 not found!");
        return;
    }
    
    await reader.CloseAsync();
    
    // Reset password to a known value
    string newPassword = "TestPass123!";
    Console.WriteLine($"\n🔧 Setting password to: {newPassword}");
    
    // Generate salt and hash (using simple approach for testing)
    byte[] saltBytes = new byte[16];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(saltBytes);
    }
    string salt = Convert.ToBase64String(saltBytes);
    
    // Create hash using SHA256 (simplified approach)
    using (var sha256 = SHA256.Create())
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(newPassword + salt);
        byte[] hashBytes = sha256.ComputeHash(passwordBytes);
        string hash = Convert.ToBase64String(hashBytes);
        
        Console.WriteLine($"📝 Generated salt: {salt.Substring(0, 10)}...");
        Console.WriteLine($"📝 Generated hash: {hash.Substring(0, 20)}...");
        
        // Update password
        var updateQuery = @"UPDATE ""Users"" SET ""PasswordHash"" = @hash, ""PasswordSalt"" = @salt WHERE ""Id"" = 34";
        using var updateCmd = new NpgsqlCommand(updateQuery, connection);
        updateCmd.Parameters.AddWithValue("hash", hash);
        updateCmd.Parameters.AddWithValue("salt", salt);
        
        int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
        
        if (rowsAffected > 0)
        {
            Console.WriteLine("✅ Password updated successfully!");
            Console.WriteLine($"\n🔑 Test login with:");
            Console.WriteLine($"  Email: pg-sponsor@test.com");
            Console.WriteLine($"  Password: {newPassword}");
        }
        else
        {
            Console.WriteLine("❌ Failed to update password");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}