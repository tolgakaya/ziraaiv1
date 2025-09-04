-- Create AnalysisMessages table manually
CREATE TABLE IF NOT EXISTS "AnalysisMessages" (
    "Id" SERIAL PRIMARY KEY,
    "PlantAnalysisId" integer NOT NULL,
    "FromUserId" integer NOT NULL,
    "ToUserId" integer NOT NULL,
    "ParentMessageId" integer NULL,
    "Message" character varying(4000) NOT NULL,
    "MessageType" character varying(50) NOT NULL,
    "Subject" character varying(200) NULL,
    "IsRead" boolean NOT NULL DEFAULT FALSE,
    "SentDate" timestamp without time zone NOT NULL,
    "ReadDate" timestamp without time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "DeletedDate" timestamp without time zone NULL,
    "SenderRole" character varying(50) NULL,
    "SenderName" character varying(100) NULL,
    "SenderCompany" character varying(200) NULL,
    "AttachmentUrls" character varying(2000) NULL,
    "LinkedProducts" character varying(2000) NULL,
    "RecommendedActions" character varying(2000) NULL,
    "Priority" character varying(20) NULL,
    "Category" character varying(50) NULL,
    "IsFlagged" boolean NOT NULL DEFAULT FALSE,
    "FlagReason" character varying(500) NULL,
    "IsApproved" boolean NOT NULL DEFAULT TRUE,
    "ApprovedByUserId" integer NULL,
    "ApprovedDate" timestamp without time zone NULL,
    "Rating" integer NULL,
    "RatingFeedback" character varying(500) NULL,
    "ModerationNotes" character varying(500) NULL,
    "IpAddress" character varying(50) NULL,
    "UserAgent" character varying(500) NULL,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_AnalysisMessages_PlantAnalysisId" ON "AnalysisMessages" ("PlantAnalysisId");
CREATE INDEX IF NOT EXISTS "IX_AnalysisMessages_FromUserId" ON "AnalysisMessages" ("FromUserId");
CREATE INDEX IF NOT EXISTS "IX_AnalysisMessages_ToUserId" ON "AnalysisMessages" ("ToUserId");
CREATE INDEX IF NOT EXISTS "IX_AnalysisMessages_SentDate" ON "AnalysisMessages" ("SentDate");
CREATE INDEX IF NOT EXISTS "IX_AnalysisMessages_IsRead" ON "AnalysisMessages" ("IsRead");
CREATE INDEX IF NOT EXISTS "IX_AnalysisMessages_IsDeleted" ON "AnalysisMessages" ("IsDeleted");
CREATE INDEX IF NOT EXISTS "IX_AnalysisMessages_MessageType" ON "AnalysisMessages" ("MessageType");
CREATE INDEX IF NOT EXISTS "IX_AnalysisMessages_Priority" ON "AnalysisMessages" ("Priority");
CREATE INDEX IF NOT EXISTS "IX_AnalysisMessages_ToUserId_IsRead" ON "AnalysisMessages" ("ToUserId", "IsRead");

-- Add foreign key constraints
ALTER TABLE "AnalysisMessages" 
ADD CONSTRAINT "FK_AnalysisMessages_PlantAnalyses_PlantAnalysisId" 
FOREIGN KEY ("PlantAnalysisId") REFERENCES "PlantAnalyses" ("Id") ON DELETE CASCADE;

ALTER TABLE "AnalysisMessages" 
ADD CONSTRAINT "FK_AnalysisMessages_Users_FromUserId" 
FOREIGN KEY ("FromUserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT;

ALTER TABLE "AnalysisMessages" 
ADD CONSTRAINT "FK_AnalysisMessages_Users_ToUserId" 
FOREIGN KEY ("ToUserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT;

ALTER TABLE "AnalysisMessages" 
ADD CONSTRAINT "FK_AnalysisMessages_AnalysisMessages_ParentMessageId" 
FOREIGN KEY ("ParentMessageId") REFERENCES "AnalysisMessages" ("Id") ON DELETE RESTRICT;

ALTER TABLE "AnalysisMessages" 
ADD CONSTRAINT "FK_AnalysisMessages_Users_ApprovedByUserId" 
FOREIGN KEY ("ApprovedByUserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT;