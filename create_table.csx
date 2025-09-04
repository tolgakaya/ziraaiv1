#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";

var sql = @"
-- Create AnalysisMessages table manually
CREATE TABLE IF NOT EXISTS ""AnalysisMessages"" (
    ""Id"" SERIAL PRIMARY KEY,
    ""PlantAnalysisId"" integer NOT NULL,
    ""FromUserId"" integer NOT NULL,
    ""ToUserId"" integer NOT NULL,
    ""ParentMessageId"" integer NULL,
    ""Message"" character varying(4000) NOT NULL,
    ""MessageType"" character varying(50) NOT NULL,
    ""Subject"" character varying(200) NULL,
    ""IsRead"" boolean NOT NULL DEFAULT FALSE,
    ""SentDate"" timestamp without time zone NOT NULL,
    ""ReadDate"" timestamp without time zone NULL,
    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
    ""DeletedDate"" timestamp without time zone NULL,
    ""SenderRole"" character varying(50) NULL,
    ""SenderName"" character varying(100) NULL,
    ""SenderCompany"" character varying(200) NULL,
    ""AttachmentUrls"" character varying(2000) NULL,
    ""LinkedProducts"" character varying(2000) NULL,
    ""RecommendedActions"" character varying(2000) NULL,
    ""Priority"" character varying(20) NULL,
    ""Category"" character varying(50) NULL,
    ""IsFlagged"" boolean NOT NULL DEFAULT FALSE,
    ""FlagReason"" character varying(500) NULL,
    ""IsApproved"" boolean NOT NULL DEFAULT TRUE,
    ""ApprovedByUserId"" integer NULL,
    ""ApprovedDate"" timestamp without time zone NULL,
    ""Rating"" integer NULL,
    ""RatingFeedback"" character varying(500) NULL,
    ""ModerationNotes"" character varying(500) NULL,
    ""IpAddress"" character varying(50) NULL,
    ""UserAgent"" character varying(500) NULL,
    ""CreatedDate"" timestamp without time zone NOT NULL,
    ""UpdatedDate"" timestamp without time zone NULL,
    ""IsActive"" boolean NOT NULL DEFAULT TRUE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS ""IX_AnalysisMessages_PlantAnalysisId"" ON ""AnalysisMessages"" (""PlantAnalysisId"");
CREATE INDEX IF NOT EXISTS ""IX_AnalysisMessages_FromUserId"" ON ""AnalysisMessages"" (""FromUserId"");
CREATE INDEX IF NOT EXISTS ""IX_AnalysisMessages_ToUserId"" ON ""AnalysisMessages"" (""ToUserId"");
CREATE INDEX IF NOT EXISTS ""IX_AnalysisMessages_SentDate"" ON ""AnalysisMessages"" (""SentDate"");
CREATE INDEX IF NOT EXISTS ""IX_AnalysisMessages_IsRead"" ON ""AnalysisMessages"" (""IsRead"");
CREATE INDEX IF NOT EXISTS ""IX_AnalysisMessages_IsDeleted"" ON ""AnalysisMessages"" (""IsDeleted"");
CREATE INDEX IF NOT EXISTS ""IX_AnalysisMessages_MessageType"" ON ""AnalysisMessages"" (""MessageType"");
CREATE INDEX IF NOT EXISTS ""IX_AnalysisMessages_Priority"" ON ""AnalysisMessages"" (""Priority"");
CREATE INDEX IF NOT EXISTS ""IX_AnalysisMessages_ToUserId_IsRead"" ON ""AnalysisMessages"" (""ToUserId"", ""IsRead"");
";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Connected to PostgreSQL successfully");
    
    using var command = new NpgsqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
    Console.WriteLine("✅ AnalysisMessages table created successfully!");
    
    // Verify table exists
    var checkSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'AnalysisMessages'";
    using var checkCommand = new NpgsqlCommand(checkSql, connection);
    var tableExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
    
    Console.WriteLine($"✅ Table verification: {(tableExists ? "AnalysisMessages table exists" : "Table creation failed")}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}