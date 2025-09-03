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
    
    // Drop and recreate the table with correct structure
    var dropTableSql = @"DROP TABLE IF EXISTS ""SponsorProfiles"";";
    
    using var dropCommand = new NpgsqlCommand(dropTableSql, connection);
    await dropCommand.ExecuteNonQueryAsync();
    
    Console.WriteLine("🗑️ Dropped existing SponsorProfiles table.");
    
    // Create SponsorProfiles table with correct structure
    var createTableSql = @"
        CREATE TABLE ""SponsorProfiles"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""SponsorId"" integer NOT NULL UNIQUE,
            ""CompanyName"" character varying(200) NOT NULL,
            ""CompanyDescription"" character varying(1000),
            ""SponsorLogoUrl"" character varying(500),
            ""WebsiteUrl"" character varying(500),
            ""ContactEmail"" character varying(200),
            ""ContactPhone"" character varying(50),
            ""ContactPerson"" character varying(200),
            ""LinkedInUrl"" character varying(500),
            ""TwitterUrl"" character varying(500),
            ""FacebookUrl"" character varying(500),
            ""InstagramUrl"" character varying(500),
            ""TaxNumber"" character varying(50),
            ""TradeRegistryNumber"" character varying(50),
            ""Address"" character varying(500),
            ""City"" character varying(100),
            ""Country"" character varying(100),
            ""PostalCode"" character varying(20),
            ""IsVerifiedCompany"" boolean NOT NULL DEFAULT false,
            ""CompanyType"" character varying(100),
            ""BusinessModel"" character varying(100),
            ""IsVerified"" boolean NOT NULL DEFAULT false,
            ""VerificationDate"" timestamp without time zone,
            ""VerificationNotes"" character varying(1000),
            ""IsActive"" boolean NOT NULL DEFAULT true,
            ""TotalPurchases"" integer NOT NULL DEFAULT 0,
            ""TotalCodesGenerated"" integer NOT NULL DEFAULT 0,
            ""TotalCodesRedeemed"" integer NOT NULL DEFAULT 0,
            ""TotalInvestment"" decimal(18,2) NOT NULL DEFAULT 0,
            ""CreatedDate"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            ""UpdatedDate"" timestamp without time zone,
            ""CreatedByUserId"" integer,
            ""UpdatedByUserId"" integer
        );";
    
    using var command = new NpgsqlCommand(createTableSql, connection);
    await command.ExecuteNonQueryAsync();
    
    Console.WriteLine("✅ SponsorProfiles table created successfully with correct structure.");
    
    // Check if table was created
    var checkTableSql = @"
        SELECT column_name, data_type 
        FROM information_schema.columns 
        WHERE table_schema = 'public' AND table_name = 'SponsorProfiles'
        ORDER BY ordinal_position;";
    
    using var checkCommand = new NpgsqlCommand(checkTableSql, connection);
    using var reader = await checkCommand.ExecuteReaderAsync();
    
    Console.WriteLine("\n📋 Table structure:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  - {reader.GetString(0)}: {reader.GetString(1)}");
    }
    
    await connection.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("\n🚀 Script execution completed successfully!");