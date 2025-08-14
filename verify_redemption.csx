#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;
using System.Threading.Tasks;

// Configuration
var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

// Get parameters from command line or use defaults
var testCode = Args.Length > 0 ? Args[0] : "SPONSOR-2025-ABC123";
var testPhone = Args.Length > 1 ? Args[1] : "905551234567";

try
{
    Console.WriteLine("🔍 Verifying Sponsorship Link Redemption");
    Console.WriteLine("========================================");
    Console.WriteLine($"📋 Code: {testCode}");
    Console.WriteLine($"📱 Phone: {testPhone}");
    Console.WriteLine();
    
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("✅ Connected to staging database");
    Console.WriteLine();
    
    // Step 1: Check sponsorship code status
    Console.WriteLine("📋 Checking Sponsorship Code Status...");
    Console.WriteLine("─────────────────────────────────────");
    
    var codeCmd = new NpgsqlCommand(@"
        SELECT ""Id"", ""Code"", ""FarmerName"", ""FarmerPhone"", ""Amount"", ""Description"",
               ""IsRedeemed"", ""RedemptionDate"", ""ExpiryDate"", ""Status"",
               ""RedemptionLink"", ""LinkClickCount"", ""LinkClickDate"", 
               ""LastClickIpAddress"", ""RecipientPhone"", ""RecipientName"",
               ""LinkSentDate"", ""LinkSentVia"", ""LinkDelivered""
        FROM ""SponsorshipCodes"" 
        WHERE ""Code"" = @code", connection);
    
    codeCmd.Parameters.AddWithValue("@code", testCode);
    
    await using var codeReader = await codeCmd.ExecuteReaderAsync();
    
    if (await codeReader.ReadAsync())
    {
        var id = codeReader.GetInt32("Id");
        var code = codeReader.GetString("Code");
        var farmerName = codeReader.IsDBNull("FarmerName") ? "N/A" : codeReader.GetString("FarmerName");
        var farmerPhone = codeReader.IsDBNull("FarmerPhone") ? "N/A" : codeReader.GetString("FarmerPhone");
        var amount = codeReader.GetDecimal("Amount");
        var description = codeReader.IsDBNull("Description") ? "N/A" : codeReader.GetString("Description");
        var isRedeemed = codeReader.GetBoolean("IsRedeemed");
        var redemptionDate = codeReader.IsDBNull("RedemptionDate") ? null : codeReader.GetDateTime("RedemptionDate");
        var expiryDate = codeReader.IsDBNull("ExpiryDate") ? null : codeReader.GetDateTime("ExpiryDate");
        var status = codeReader.GetBoolean("Status");
        
        // Link tracking fields
        var redemptionLink = codeReader.IsDBNull("RedemptionLink") ? "N/A" : codeReader.GetString("RedemptionLink");
        var clickCount = codeReader.GetInt32("LinkClickCount");
        var clickDate = codeReader.IsDBNull("LinkClickDate") ? null : codeReader.GetDateTime("LinkClickDate");
        var lastIP = codeReader.IsDBNull("LastClickIpAddress") ? "N/A" : codeReader.GetString("LastClickIpAddress");
        var recipientPhone = codeReader.IsDBNull("RecipientPhone") ? "N/A" : codeReader.GetString("RecipientPhone");
        var recipientName = codeReader.IsDBNull("RecipientName") ? "N/A" : codeReader.GetString("RecipientName");
        var linkSentDate = codeReader.IsDBNull("LinkSentDate") ? null : codeReader.GetDateTime("LinkSentDate");
        var linkSentVia = codeReader.IsDBNull("LinkSentVia") ? "N/A" : codeReader.GetString("LinkSentVia");
        var linkDelivered = codeReader.GetBoolean("LinkDelivered");
        
        Console.WriteLine($"🆔 ID: {id}");
        Console.WriteLine($"📝 Code: {code}");
        Console.WriteLine($"👨 Farmer Name: {farmerName}");
        Console.WriteLine($"📱 Farmer Phone: {farmerPhone}");
        Console.WriteLine($"💰 Amount: {amount:C}");
        Console.WriteLine($"📄 Description: {description}");
        Console.WriteLine($"✅ Active: {status}");
        Console.WriteLine($"📅 Expires: {expiryDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"}");
        Console.WriteLine();
        
        Console.WriteLine("🔗 Link Distribution Info:");
        Console.WriteLine($"   📧 Recipient Name: {recipientName}");
        Console.WriteLine($"   📱 Recipient Phone: {recipientPhone}");
        Console.WriteLine($"   🔗 Redemption Link: {redemptionLink}");
        Console.WriteLine($"   📅 Link Sent Date: {linkSentDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not sent"}");
        Console.WriteLine($"   📡 Sent Via: {linkSentVia}");
        Console.WriteLine($"   ✅ Delivered: {linkDelivered}");
        Console.WriteLine();
        
        Console.WriteLine("📊 Usage Analytics:");
        Console.WriteLine($"   🖱️  Click Count: {clickCount}");
        Console.WriteLine($"   🕐 Last Click: {clickDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never clicked"}");
        Console.WriteLine($"   🌐 Last Click IP: {lastIP}");
        Console.WriteLine();
        
        Console.WriteLine("🎯 Redemption Status:");
        if (isRedeemed)
        {
            Console.WriteLine($"   ✅ Status: REDEEMED");
            Console.WriteLine($"   📅 Redeemed On: {redemptionDate?.ToString("yyyy-MM-dd HH:mm:ss")}");
        }
        else
        {
            Console.WriteLine($"   ⏳ Status: NOT REDEEMED");
            
            // Check if expired
            if (expiryDate.HasValue && expiryDate.Value < DateTime.Now)
            {
                Console.WriteLine($"   ⚠️  WARNING: Code expired on {expiryDate.Value:yyyy-MM-dd}");
            }
        }
    }
    else
    {
        Console.WriteLine($"❌ Sponsorship code '{testCode}' not found in database!");
        Console.WriteLine("💡 Available codes:");
        
        var availableCmd = new NpgsqlCommand(@"
            SELECT ""Code"", ""FarmerName"", ""IsRedeemed"", ""ExpiryDate""
            FROM ""SponsorshipCodes""
            ORDER BY ""RecordDate"" DESC 
            LIMIT 5", connection);
        
        await using var availableReader = await availableCmd.ExecuteReaderAsync();
        while (await availableReader.ReadAsync())
        {
            var code = availableReader.GetString("Code");
            var name = availableReader.IsDBNull("FarmerName") ? "N/A" : availableReader.GetString("FarmerName");
            var redeemed = availableReader.GetBoolean("IsRedeemed");
            var expiry = availableReader.IsDBNull("ExpiryDate") ? null : availableReader.GetDateTime("ExpiryDate");
            
            var status = redeemed ? "REDEEMED" : "AVAILABLE";
            Console.WriteLine($"   📝 {code} ({name}) - {status}");
        }
        
        return;
    }
    
    await codeReader.CloseAsync();
    
    // Step 2: Check if farmer user account exists
    Console.WriteLine();
    Console.WriteLine("👤 Checking Farmer User Account...");
    Console.WriteLine("──────────────────────────────────");
    
    var userCmd = new NpgsqlCommand(@"
        SELECT ""Id"", ""FullName"", ""Email"", ""MobilePhones"", ""Status"", 
               ""RecordDate"", ""UpdatedDate"", ""EmailConfirmed"", ""PhoneConfirmed""
        FROM ""Users"" 
        WHERE ""MobilePhones"" = @phone OR ""MobilePhones"" = @phoneWithoutCountry
        ORDER BY ""RecordDate"" DESC", connection);
    
    userCmd.Parameters.AddWithValue("@phone", testPhone);
    userCmd.Parameters.AddWithValue("@phoneWithoutCountry", testPhone.StartsWith("90") ? testPhone.Substring(2) : testPhone);
    
    await using var userReader = await userCmd.ExecuteReaderAsync();
    
    var userFound = false;
    while (await userReader.ReadAsync())
    {
        userFound = true;
        var userId = userReader.GetInt32("Id");
        var fullName = userReader.GetString("FullName");
        var email = userReader.GetString("Email");
        var phone = userReader.GetString("MobilePhones");
        var status = userReader.GetBoolean("Status");
        var recordDate = userReader.GetDateTime("RecordDate");
        var updatedDate = userReader.IsDBNull("UpdatedDate") ? null : userReader.GetDateTime("UpdatedDate");
        var emailConfirmed = userReader.GetBoolean("EmailConfirmed");
        var phoneConfirmed = userReader.GetBoolean("PhoneConfirmed");
        
        Console.WriteLine($"🆔 User ID: {userId}");
        Console.WriteLine($"👨 Full Name: {fullName}");
        Console.WriteLine($"📧 Email: {email}");
        Console.WriteLine($"📱 Phone: {phone}");
        Console.WriteLine($"✅ Active: {status}");
        Console.WriteLine($"📅 Created: {recordDate:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"📅 Updated: {updatedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"}");
        Console.WriteLine($"✉️  Email Confirmed: {emailConfirmed}");
        Console.WriteLine($"📞 Phone Confirmed: {phoneConfirmed}");
        
        // Check if account was created recently (likely from redemption)
        var accountAge = DateTime.Now - recordDate;
        if (accountAge.TotalMinutes < 30)
        {
            Console.WriteLine($"🆕 RECENTLY CREATED: {accountAge.TotalMinutes:F1} minutes ago");
        }
        
        Console.WriteLine();
    }
    
    if (!userFound)
    {
        Console.WriteLine($"❌ No user account found for phone: {testPhone}");
        
        // Show recent users for reference
        Console.WriteLine("💡 Recent user accounts:");
        
        await userReader.CloseAsync();
        
        var recentUsersCmd = new NpgsqlCommand(@"
            SELECT ""Id"", ""FullName"", ""Email"", ""MobilePhones"", ""RecordDate""
            FROM ""Users""
            WHERE ""RecordDate"" > @since
            ORDER BY ""RecordDate"" DESC 
            LIMIT 5", connection);
        
        recentUsersCmd.Parameters.AddWithValue("@since", DateTime.Now.AddHours(-1));
        
        await using var recentReader = await recentUsersCmd.ExecuteReaderAsync();
        while (await recentReader.ReadAsync())
        {
            var id = recentReader.GetInt32("Id");
            var name = recentReader.GetString("FullName");
            var email = recentReader.GetString("Email");
            var phone = recentReader.GetString("MobilePhones");
            var created = recentReader.GetDateTime("RecordDate");
            
            Console.WriteLine($"   👤 {id}: {name} ({phone}) - {created:HH:mm:ss}");
        }
    }
    
    // Step 3: Check user roles and permissions
    if (userFound)
    {
        await userReader.CloseAsync();
        
        Console.WriteLine("🔐 Checking User Roles...");
        Console.WriteLine("─────────────────────────");
        
        var rolesCmd = new NpgsqlCommand(@"
            SELECT g.""Name"" as GroupName
            FROM ""UserGroups"" ug
            INNER JOIN ""Groups"" g ON ug.""GroupId"" = g.""Id""
            INNER JOIN ""Users"" u ON ug.""UserId"" = u.""Id""
            WHERE u.""MobilePhones"" = @phone", connection);
        
        rolesCmd.Parameters.AddWithValue("@phone", testPhone);
        
        await using var rolesReader = await rolesCmd.ExecuteReaderAsync();
        var hasRoles = false;
        
        while (await rolesReader.ReadAsync())
        {
            hasRoles = true;
            var groupName = rolesReader.GetString("GroupName");
            Console.WriteLine($"   👥 Role: {groupName}");
        }
        
        if (!hasRoles)
        {
            Console.WriteLine("   ⚠️  No roles assigned to user");
        }
    }
    
    // Step 4: Summary and recommendations
    Console.WriteLine();
    Console.WriteLine("📋 VERIFICATION SUMMARY");
    Console.WriteLine("═══════════════════════");
    
    // Re-check the main data points
    var summaryCmd = new NpgsqlCommand(@"
        SELECT sc.""IsRedeemed"", sc.""LinkClickCount"", 
               CASE WHEN u.""Id"" IS NOT NULL THEN 1 ELSE 0 END as UserExists
        FROM ""SponsorshipCodes"" sc
        LEFT JOIN ""Users"" u ON u.""MobilePhones"" = @phone OR u.""MobilePhones"" = @phoneWithoutCountry
        WHERE sc.""Code"" = @code", connection);
    
    summaryCmd.Parameters.AddWithValue("@code", testCode);
    summaryCmd.Parameters.AddWithValue("@phone", testPhone);
    summaryCmd.Parameters.AddWithValue("@phoneWithoutCountry", testPhone.StartsWith("90") ? testPhone.Substring(2) : testPhone);
    
    await using var summaryReader = await summaryCmd.ExecuteReaderAsync();
    
    if (await summaryReader.ReadAsync())
    {
        var isRedeemed = summaryReader.GetBoolean("IsRedeemed");
        var clickCount = summaryReader.GetInt32("LinkClickCount");
        var userExists = summaryReader.GetInt32("UserExists") == 1;
        
        Console.WriteLine($"✅ Code Status: {(isRedeemed ? "REDEEMED" : "NOT REDEEMED")}");
        Console.WriteLine($"🖱️  Link Clicks: {clickCount}");
        Console.WriteLine($"👤 User Account: {(userExists ? "EXISTS" : "NOT FOUND")}");
        
        Console.WriteLine();
        Console.WriteLine("🎯 EXPECTED FLOW:");
        Console.WriteLine("1. Sponsor creates code ✅");
        Console.WriteLine("2. Sponsor sends link ✅");
        Console.WriteLine($"3. Farmer clicks link {(clickCount > 0 ? "✅" : "❌")}");
        Console.WriteLine($"4. User account created {(userExists ? "✅" : "❌")}");
        Console.WriteLine($"5. Code marked redeemed {(isRedeemed ? "✅" : "❌")}");
        
        Console.WriteLine();
        
        if (isRedeemed && userExists && clickCount > 0)
        {
            Console.WriteLine("🎉 SUCCESS: Complete redemption flow verified!");
        }
        else if (clickCount > 0 && !isRedeemed)
        {
            Console.WriteLine("⚠️  WARNING: Link was clicked but redemption incomplete");
            Console.WriteLine("💡 Possible issues:");
            Console.WriteLine("   - Account creation failed");
            Console.WriteLine("   - Database transaction failed");
            Console.WriteLine("   - Validation errors occurred");
        }
        else if (clickCount == 0)
        {
            Console.WriteLine("💡 INFO: Link not clicked yet - redemption pending");
            Console.WriteLine("🔗 Test the link manually:");
            
            // Get the redemption link
            var linkCmd = new NpgsqlCommand(@"
                SELECT ""RedemptionLink"" FROM ""SponsorshipCodes"" WHERE ""Code"" = @code", connection);
            linkCmd.Parameters.AddWithValue("@code", testCode);
            var link = await linkCmd.ExecuteScalarAsync() as string;
            
            if (!string.IsNullOrEmpty(link))
            {
                Console.WriteLine($"   {link}");
            }
        }
    }
    
    Console.WriteLine();
    Console.WriteLine("🔍 Verification completed!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error during verification: {ex.Message}");
    Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
}