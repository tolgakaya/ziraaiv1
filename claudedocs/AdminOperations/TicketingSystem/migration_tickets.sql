-- Migration Script: Ticketing System for Customer Support
-- Date: 2024
-- Description: Creates Tickets and TicketMessages tables for customer support system

-- =============================================
-- Create Tickets Table
-- =============================================
CREATE TABLE IF NOT EXISTS "Tickets" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "UserRole" VARCHAR(20) NOT NULL,
    "Subject" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(2000) NOT NULL,
    "Category" VARCHAR(50) NOT NULL,
    "Priority" VARCHAR(20) NOT NULL,
    "Status" VARCHAR(20) NOT NULL,
    "AssignedToUserId" INTEGER NULL,
    "ResolvedDate" TIMESTAMP NULL,
    "ClosedDate" TIMESTAMP NULL,
    "ResolutionNotes" VARCHAR(1000) NULL,
    "SatisfactionRating" INTEGER NULL,
    "SatisfactionFeedback" VARCHAR(500) NULL,
    "CreatedDate" TIMESTAMP NOT NULL,
    "UpdatedDate" TIMESTAMP NOT NULL,
    "LastResponseDate" TIMESTAMP NULL,

    CONSTRAINT "FK_Tickets_Users_UserId" FOREIGN KEY ("UserId")
        REFERENCES "Users" ("UserId") ON DELETE RESTRICT,
    CONSTRAINT "FK_Tickets_Users_AssignedToUserId" FOREIGN KEY ("AssignedToUserId")
        REFERENCES "Users" ("UserId") ON DELETE SET NULL
);

-- Create indexes for Tickets
CREATE INDEX IF NOT EXISTS "IX_Tickets_UserId" ON "Tickets" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Tickets_Status" ON "Tickets" ("Status");
CREATE INDEX IF NOT EXISTS "IX_Tickets_CreatedDate" ON "Tickets" ("CreatedDate");
CREATE INDEX IF NOT EXISTS "IX_Tickets_Priority" ON "Tickets" ("Priority");
CREATE INDEX IF NOT EXISTS "IX_Tickets_Category" ON "Tickets" ("Category");

-- =============================================
-- Create TicketMessages Table
-- =============================================
CREATE TABLE IF NOT EXISTS "TicketMessages" (
    "Id" SERIAL PRIMARY KEY,
    "TicketId" INTEGER NOT NULL,
    "FromUserId" INTEGER NOT NULL,
    "Message" VARCHAR(2000) NOT NULL,
    "IsAdminResponse" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsInternal" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "ReadDate" TIMESTAMP NULL,
    "CreatedDate" TIMESTAMP NOT NULL,

    CONSTRAINT "FK_TicketMessages_Tickets_TicketId" FOREIGN KEY ("TicketId")
        REFERENCES "Tickets" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TicketMessages_Users_FromUserId" FOREIGN KEY ("FromUserId")
        REFERENCES "Users" ("UserId") ON DELETE RESTRICT
);

-- Create indexes for TicketMessages
CREATE INDEX IF NOT EXISTS "IX_TicketMessages_TicketId" ON "TicketMessages" ("TicketId");
CREATE INDEX IF NOT EXISTS "IX_TicketMessages_FromUserId" ON "TicketMessages" ("FromUserId");
CREATE INDEX IF NOT EXISTS "IX_TicketMessages_CreatedDate" ON "TicketMessages" ("CreatedDate");

-- =============================================
-- Verification Queries
-- =============================================
-- Run these after migration to verify tables were created correctly:

-- SELECT table_name FROM information_schema.tables WHERE table_name IN ('Tickets', 'TicketMessages');

-- SELECT column_name, data_type, character_maximum_length, is_nullable
-- FROM information_schema.columns
-- WHERE table_name = 'Tickets'
-- ORDER BY ordinal_position;

-- SELECT column_name, data_type, character_maximum_length, is_nullable
-- FROM information_schema.columns
-- WHERE table_name = 'TicketMessages'
-- ORDER BY ordinal_position;
