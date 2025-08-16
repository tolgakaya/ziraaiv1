#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

var sql = @"
-- Add missing columns to AnalysisMessages table
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""IsArchived"" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""ArchivedDate"" timestamp without time zone NULL;
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""HasAttachments"" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""RequiresResponse"" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""ResponseDeadline"" timestamp without time zone NULL;
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""IsImportant"" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""EmailNotificationSent"" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""EmailSentDate"" timestamp without time zone NULL;
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""SmsNotificationSent"" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""SmsSentDate"" timestamp without time zone NULL;
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""PushNotificationSent"" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE ""AnalysisMessages"" ADD COLUMN IF NOT EXISTS ""PushSentDate"" timestamp without time zone NULL;
";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Connected to PostgreSQL successfully");
    
    using var command = new NpgsqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
    Console.WriteLine("✅ Missing columns added successfully!");
    
    // Verify columns exist
    var checkSql = @"
        SELECT column_name 
        FROM information_schema.columns 
        WHERE table_name = 'AnalysisMessages' 
        AND column_name IN ('IsArchived', 'ArchivedDate', 'HasAttachments', 'RequiresResponse', 'ResponseDeadline', 'IsImportant', 'EmailNotificationSent', 'EmailSentDate', 'SmsNotificationSent', 'SmsSentDate', 'PushNotificationSent', 'PushSentDate')
        ORDER BY column_name";
    
    using var checkCommand = new NpgsqlCommand(checkSql, connection);
    using var reader = await checkCommand.ExecuteReaderAsync();
    
    Console.WriteLine("✅ Added columns:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  - {reader.GetString(0)}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}