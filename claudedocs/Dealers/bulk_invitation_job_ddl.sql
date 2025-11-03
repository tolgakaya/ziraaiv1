-- =====================================================
-- BulkInvitationJob Table DDL for PostgreSQL
-- Tracks bulk dealer invitation jobs processed via RabbitMQ
-- =====================================================

CREATE TABLE IF NOT EXISTS "BulkInvitationJobs" (
    "Id" SERIAL PRIMARY KEY,
    
    -- Sponsor Information
    "SponsorId" INTEGER NOT NULL,
    
    -- Invitation Configuration
    "InvitationType" VARCHAR(50) NOT NULL, -- 'Invite' or 'AutoCreate'
    "DefaultTier" VARCHAR(10), -- S, M, L, XL (nullable)
    "DefaultCodeCount" INTEGER NOT NULL DEFAULT 0,
    "SendSms" BOOLEAN NOT NULL DEFAULT true,
    
    -- Progress Tracking
    "TotalDealers" INTEGER NOT NULL DEFAULT 0,
    "ProcessedDealers" INTEGER NOT NULL DEFAULT 0,
    "SuccessfulInvitations" INTEGER NOT NULL DEFAULT 0,
    "FailedInvitations" INTEGER NOT NULL DEFAULT 0,
    
    -- Status: Pending, Processing, Completed, PartialSuccess, Failed
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending',
    
    -- Timestamps
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "StartedDate" TIMESTAMP NULL,
    "CompletedDate" TIMESTAMP NULL,
    
    -- File Information
    "OriginalFileName" VARCHAR(500) NOT NULL,
    "FileSize" INTEGER NOT NULL, -- in bytes
    
    -- Results
    "ResultFileUrl" VARCHAR(1000) NULL, -- URL to download result Excel
    "ErrorSummary" TEXT NULL, -- JSON array of error details
    
    -- Indexes for performance
    CONSTRAINT "FK_BulkInvitationJobs_Users_SponsorId" 
        FOREIGN KEY ("SponsorId") REFERENCES "Users"("UserId") ON DELETE CASCADE
);

-- Indexes for query performance
CREATE INDEX IF NOT EXISTS "IX_BulkInvitationJobs_SponsorId" 
    ON "BulkInvitationJobs"("SponsorId");

CREATE INDEX IF NOT EXISTS "IX_BulkInvitationJobs_Status" 
    ON "BulkInvitationJobs"("Status");

CREATE INDEX IF NOT EXISTS "IX_BulkInvitationJobs_CreatedDate" 
    ON "BulkInvitationJobs"("CreatedDate" DESC);

CREATE INDEX IF NOT EXISTS "IX_BulkInvitationJobs_SponsorId_Status" 
    ON "BulkInvitationJobs"("SponsorId", "Status");

-- Comments for documentation
COMMENT ON TABLE "BulkInvitationJobs" IS 'Tracks bulk dealer invitation jobs processed through RabbitMQ queue';
COMMENT ON COLUMN "BulkInvitationJobs"."InvitationType" IS 'Invite: Email/SMS invitation, AutoCreate: Automatic account creation';
COMMENT ON COLUMN "BulkInvitationJobs"."Status" IS 'Pending, Processing, Completed, PartialSuccess, Failed';
COMMENT ON COLUMN "BulkInvitationJobs"."ErrorSummary" IS 'JSON array: [{"rowNumber": 12, "email": "test@email.com", "error": "message", "timestamp": "2025-11-03T15:30:00Z"}]';
