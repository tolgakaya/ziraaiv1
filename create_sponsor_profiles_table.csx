#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using System;
using Npgsql;

// Connection string - update with your actual connection details
var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("✅ Connected to PostgreSQL database successfully.");
    
    // Create SponsorProfiles table
    var createTableSql = @"
        CREATE TABLE IF NOT EXISTS ""SponsorProfiles"" (
            ""SponsorId"" integer NOT NULL PRIMARY KEY,
            ""CompanyName"" character varying(200) NOT NULL,
            ""CompanyDescription"" character varying(1000),
            ""SponsorLogoUrl"" character varying(500),
            ""WebsiteUrl"" character varying(500),
            ""ContactEmail"" character varying(200),
            ""ContactPhone"" character varying(50),
            ""ContactPerson"" character varying(200),
            ""CompanyType"" character varying(100),
            ""BusinessModel"" character varying(100),
            ""TotalPurchases"" integer NOT NULL DEFAULT 0,
            ""TotalCodesGenerated"" integer NOT NULL DEFAULT 0,
            ""TotalCodesRedeemed"" integer NOT NULL DEFAULT 0,
            ""TotalInvestment"" decimal(18,2) NOT NULL DEFAULT 0,
            ""CreatedDate"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            ""UpdatedDate"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            ""IsActive"" boolean NOT NULL DEFAULT true
        );";
    
    using var command = new NpgsqlCommand(createTableSql, connection);
    await command.ExecuteNonQueryAsync();
    
    Console.WriteLine("✅ SponsorProfiles table created successfully.");
    
    // Check if table was created
    var checkTableSql = @"
        SELECT COUNT(*) FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_name = 'SponsorProfiles';";
    
    using var checkCommand = new NpgsqlCommand(checkTableSql, connection);
    var tableExists = (long)await checkCommand.ExecuteScalarAsync();
    
    if (tableExists > 0)
    {
        Console.WriteLine("🎉 Table verification successful. SponsorProfiles table exists.");
    }
    else
    {
        Console.WriteLine("❌ Table verification failed. SponsorProfiles table does not exist.");
    }
    
    await connection.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("\n🚀 Script execution completed successfully!");