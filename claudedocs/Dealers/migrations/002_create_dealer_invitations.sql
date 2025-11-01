-- =====================================================
-- Migration: Create DealerInvitations table
-- Date: 2025-01-26
-- Purpose: Support dealer onboarding (invite and auto-create methods)
-- =====================================================

CREATE TABLE "DealerInvitations" (
    "Id" SERIAL PRIMARY KEY,
    
    -- Sponsor information
    "SponsorId" INT NOT NULL,
    
    -- Dealer information
    "Email" VARCHAR(255),
    "Phone" VARCHAR(20),
    "DealerName" VARCHAR(255) NOT NULL,
    
    -- Invitation status and type
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Accepted, Expired, Cancelled
    "InvitationType" VARCHAR(50) NOT NULL, -- Invite, AutoCreate
    "InvitationToken" VARCHAR(255) UNIQUE,
    
    -- Package and code information
    "PurchaseId" INT NOT NULL,
    "CodeCount" INT NOT NULL,
    
    -- Dealer creation tracking
    "CreatedDealerId" INT NULL, -- Set when dealer is created/linked
    "AcceptedDate" TIMESTAMP NULL,
    "AutoCreatedPassword" VARCHAR(255) NULL, -- Encrypted password for auto-created accounts
    
    -- Audit fields
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiryDate" TIMESTAMP NOT NULL, -- Default: 7 days from creation
    "CancelledDate" TIMESTAMP NULL,
    "CancelledByUserId" INT NULL,
    "Notes" TEXT,
    
    -- Foreign key constraints
    CONSTRAINT "FK_DealerInvitations_Sponsor" 
        FOREIGN KEY ("SponsorId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    
    CONSTRAINT "FK_DealerInvitations_Purchase" 
        FOREIGN KEY ("PurchaseId") REFERENCES "SponsorshipPurchases"("Id") ON DELETE CASCADE,
    
    CONSTRAINT "FK_DealerInvitations_CreatedDealer" 
        FOREIGN KEY ("CreatedDealerId") REFERENCES "Users"("Id") ON DELETE SET NULL,
    
    CONSTRAINT "FK_DealerInvitations_CancelledBy" 
        FOREIGN KEY ("CancelledByUserId") REFERENCES "Users"("Id") ON DELETE SET NULL,
    
    -- Business rule constraints
    CONSTRAINT "CHK_DealerInvitations_Status" 
        CHECK ("Status" IN ('Pending', 'Accepted', 'Expired', 'Cancelled')),
    
    CONSTRAINT "CHK_DealerInvitations_Type" 
        CHECK ("InvitationType" IN ('Invite', 'AutoCreate')),
    
    CONSTRAINT "CHK_DealerInvitations_Contact" 
        CHECK ("Email" IS NOT NULL OR "Phone" IS NOT NULL),
    
    CONSTRAINT "CHK_DealerInvitations_CodeCount" 
        CHECK ("CodeCount" > 0)
);

-- Create indexes for performance
CREATE INDEX "IX_DealerInvitations_SponsorId" ON "DealerInvitations"("SponsorId");
CREATE INDEX "IX_DealerInvitations_Token" ON "DealerInvitations"("InvitationToken");
CREATE INDEX "IX_DealerInvitations_Status" ON "DealerInvitations"("Status");
CREATE INDEX "IX_DealerInvitations_Email" ON "DealerInvitations"("Email");
CREATE INDEX "IX_DealerInvitations_Phone" ON "DealerInvitations"("Phone");
CREATE INDEX "IX_DealerInvitations_CreatedDealer" ON "DealerInvitations"("CreatedDealerId");
CREATE INDEX "IX_DealerInvitations_ExpiryDate" ON "DealerInvitations"("ExpiryDate");

-- Add comment for documentation
COMMENT ON TABLE "DealerInvitations" IS 'Tracks dealer invitations and auto-created dealer profiles for code distribution';
COMMENT ON COLUMN "DealerInvitations"."InvitationType" IS 'Invite: Email invitation with link, AutoCreate: Automatic account creation';
COMMENT ON COLUMN "DealerInvitations"."Status" IS 'Pending: Waiting for acceptance, Accepted: Dealer linked, Expired: Token expired, Cancelled: Sponsor cancelled';

-- =====================================================
-- Sample query to check pending invitations
-- =====================================================
-- SELECT 
--     di."Id",
--     di."DealerName",
--     di."Email",
--     di."Phone",
--     di."InvitationType",
--     di."Status",
--     di."CodeCount",
--     u."FullName" as "SponsorName",
--     di."CreatedDate",
--     di."ExpiryDate"
-- FROM "DealerInvitations" di
-- JOIN "Users" u ON di."SponsorId" = u."Id"
-- WHERE di."Status" = 'Pending'
-- ORDER BY di."CreatedDate" DESC;
