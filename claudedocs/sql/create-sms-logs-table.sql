-- SMS Logs Table
-- Purpose: Temporary logging of SMS content for debugging
-- Actions: DealerInvite, CodeDistribute, Referral
-- Controlled by: SmsLogging:Enabled config flag

CREATE TABLE "SmsLogs" (
    "Id" SERIAL PRIMARY KEY,
    "Action" VARCHAR(50) NOT NULL,
    "SenderUserId" INTEGER NULL,
    "Content" TEXT NOT NULL,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX "IX_SmsLogs_Action" ON "SmsLogs" ("Action");
CREATE INDEX "IX_SmsLogs_SenderUserId" ON "SmsLogs" ("SenderUserId");
CREATE INDEX "IX_SmsLogs_CreatedDate" ON "SmsLogs" ("CreatedDate");

-- Sample queries for manual inspection

-- Get all dealer invite SMS
-- SELECT * FROM "SmsLogs" WHERE "Action" = 'DealerInvite' ORDER BY "CreatedDate" DESC LIMIT 10;

-- Get all code distribution SMS
-- SELECT * FROM "SmsLogs" WHERE "Action" = 'CodeDistribute' ORDER BY "CreatedDate" DESC LIMIT 10;

-- Get all referral SMS
-- SELECT * FROM "SmsLogs" WHERE "Action" = 'Referral' ORDER BY "CreatedDate" DESC LIMIT 10;

-- Get recent SMS logs (last hour)
-- SELECT * FROM "SmsLogs" WHERE "CreatedDate" > NOW() - INTERVAL '1 hour' ORDER BY "CreatedDate" DESC;

-- Get SMS count by action type
-- SELECT "Action", COUNT(*) as "Count" FROM "SmsLogs" GROUP BY "Action";

-- Clean up old logs (older than 7 days)
-- DELETE FROM "SmsLogs" WHERE "CreatedDate" < NOW() - INTERVAL '7 days';

-- Drop table when no longer needed
-- DROP TABLE IF EXISTS "SmsLogs";
