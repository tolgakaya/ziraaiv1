-- DROP SCHEMA public;

CREATE SCHEMA public AUTHORIZATION pg_database_owner;

COMMENT ON SCHEMA public IS 'standard public schema';

-- DROP SEQUENCE public."AdminOperationLogs_Id_seq";

CREATE SEQUENCE public."AdminOperationLogs_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."AdminOperationLogs_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."AdminOperationLogs_Id_seq" TO postgres;

-- DROP SEQUENCE public."AnalysisMessages_Id_seq";

CREATE SEQUENCE public."AnalysisMessages_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."AnalysisMessages_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."AnalysisMessages_Id_seq" TO postgres;

-- DROP SEQUENCE public."AppInfos_Id_seq";

CREATE SEQUENCE public."AppInfos_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."AppInfos_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."AppInfos_Id_seq" TO postgres;

-- DROP SEQUENCE public."BulkCodeDistributionJobs_Id_seq";

CREATE SEQUENCE public."BulkCodeDistributionJobs_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."BulkCodeDistributionJobs_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."BulkCodeDistributionJobs_Id_seq" TO postgres;

-- DROP SEQUENCE public."BulkInvitationJobs_Id_seq";

CREATE SEQUENCE public."BulkInvitationJobs_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."BulkInvitationJobs_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."BulkInvitationJobs_Id_seq" TO postgres;

-- DROP SEQUENCE public."BulkSubscriptionAssignmentJobs_Id_seq";

CREATE SEQUENCE public."BulkSubscriptionAssignmentJobs_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."BulkSubscriptionAssignmentJobs_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."BulkSubscriptionAssignmentJobs_Id_seq" TO postgres;

-- DROP SEQUENCE public."Configurations_Id_seq";

CREATE SEQUENCE public."Configurations_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."Configurations_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."Configurations_Id_seq" TO postgres;

-- DROP SEQUENCE public."DealerInvitations_Id_seq";

CREATE SEQUENCE public."DealerInvitations_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."DealerInvitations_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."DealerInvitations_Id_seq" TO postgres;

-- DROP SEQUENCE public."DeepLinkClickRecords_Id_seq";

CREATE SEQUENCE public."DeepLinkClickRecords_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."DeepLinkClickRecords_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."DeepLinkClickRecords_Id_seq" TO postgres;

-- DROP SEQUENCE public."DeepLinks_Id_seq";

CREATE SEQUENCE public."DeepLinks_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."DeepLinks_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."DeepLinks_Id_seq" TO postgres;

-- DROP SEQUENCE public."FarmerSponsorBlocks_Id_seq";

CREATE SEQUENCE public."FarmerSponsorBlocks_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."FarmerSponsorBlocks_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."FarmerSponsorBlocks_Id_seq" TO postgres;

-- DROP SEQUENCE public."Features_Id_seq";

CREATE SEQUENCE public."Features_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."Features_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."Features_Id_seq" TO postgres;

-- DROP SEQUENCE public."Groups_Id_seq";

CREATE SEQUENCE public."Groups_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."Groups_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."Groups_Id_seq" TO postgres;

-- DROP SEQUENCE public."Languages_Id_seq";

CREATE SEQUENCE public."Languages_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."Languages_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."Languages_Id_seq" TO postgres;

-- DROP SEQUENCE public."Logs_Id_seq";

CREATE SEQUENCE public."Logs_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."Logs_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."Logs_Id_seq" TO postgres;

-- DROP SEQUENCE public."MessagingFeatures_Id_seq";

CREATE SEQUENCE public."MessagingFeatures_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."MessagingFeatures_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."MessagingFeatures_Id_seq" TO postgres;

-- DROP SEQUENCE public."MobileLogins_Id_seq";

CREATE SEQUENCE public."MobileLogins_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."MobileLogins_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."MobileLogins_Id_seq" TO postgres;

-- DROP SEQUENCE public."OperationClaims_Id_seq";

CREATE SEQUENCE public."OperationClaims_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."OperationClaims_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."OperationClaims_Id_seq" TO postgres;

-- DROP SEQUENCE public."PaymentTransactions_Id_seq";

CREATE SEQUENCE public."PaymentTransactions_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."PaymentTransactions_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."PaymentTransactions_Id_seq" TO postgres;

-- DROP SEQUENCE public."PlantAnalyses_Id_seq";

CREATE SEQUENCE public."PlantAnalyses_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."PlantAnalyses_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."PlantAnalyses_Id_seq" TO postgres;

-- DROP SEQUENCE public."PlantAnalyses_Id_seq1";

CREATE SEQUENCE public."PlantAnalyses_Id_seq1"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."PlantAnalyses_Id_seq1" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."PlantAnalyses_Id_seq1" TO postgres;

-- DROP SEQUENCE public."ReferralCodes_Id_seq";

CREATE SEQUENCE public."ReferralCodes_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."ReferralCodes_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."ReferralCodes_Id_seq" TO postgres;

-- DROP SEQUENCE public."ReferralConfigurations_Id_seq";

CREATE SEQUENCE public."ReferralConfigurations_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."ReferralConfigurations_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."ReferralConfigurations_Id_seq" TO postgres;

-- DROP SEQUENCE public."ReferralRewards_Id_seq";

CREATE SEQUENCE public."ReferralRewards_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."ReferralRewards_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."ReferralRewards_Id_seq" TO postgres;

-- DROP SEQUENCE public."ReferralTracking_Id_seq";

CREATE SEQUENCE public."ReferralTracking_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."ReferralTracking_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."ReferralTracking_Id_seq" TO postgres;

-- DROP SEQUENCE public."Roles_Id_seq";

CREATE SEQUENCE public."Roles_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."Roles_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."Roles_Id_seq" TO postgres;

-- DROP SEQUENCE public."SmsLogs_Id_seq";

CREATE SEQUENCE public."SmsLogs_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."SmsLogs_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."SmsLogs_Id_seq" TO postgres;

-- DROP SEQUENCE public."SponsorAnalysisAccess_Id_seq";

CREATE SEQUENCE public."SponsorAnalysisAccess_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."SponsorAnalysisAccess_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."SponsorAnalysisAccess_Id_seq" TO postgres;

-- DROP SEQUENCE public."SponsorProfiles_Id_seq";

CREATE SEQUENCE public."SponsorProfiles_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."SponsorProfiles_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."SponsorProfiles_Id_seq" TO postgres;

-- DROP SEQUENCE public."SponsorshipCodes_Id_seq";

CREATE SEQUENCE public."SponsorshipCodes_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."SponsorshipCodes_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."SponsorshipCodes_Id_seq" TO postgres;

-- DROP SEQUENCE public."SponsorshipPurchases_Id_seq";

CREATE SEQUENCE public."SponsorshipPurchases_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."SponsorshipPurchases_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."SponsorshipPurchases_Id_seq" TO postgres;

-- DROP SEQUENCE public."SubscriptionTiers_Id_seq";

CREATE SEQUENCE public."SubscriptionTiers_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."SubscriptionTiers_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."SubscriptionTiers_Id_seq" TO postgres;

-- DROP SEQUENCE public."SubscriptionUsageLogs_Id_seq";

CREATE SEQUENCE public."SubscriptionUsageLogs_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."SubscriptionUsageLogs_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."SubscriptionUsageLogs_Id_seq" TO postgres;

-- DROP SEQUENCE public."TicketMessages_Id_seq";

CREATE SEQUENCE public."TicketMessages_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."TicketMessages_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."TicketMessages_Id_seq" TO postgres;

-- DROP SEQUENCE public."Tickets_Id_seq";

CREATE SEQUENCE public."Tickets_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."Tickets_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."Tickets_Id_seq" TO postgres;

-- DROP SEQUENCE public."TierFeatures_Id_seq";

CREATE SEQUENCE public."TierFeatures_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."TierFeatures_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."TierFeatures_Id_seq" TO postgres;

-- DROP SEQUENCE public."Translates_Id_seq";

CREATE SEQUENCE public."Translates_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."Translates_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."Translates_Id_seq" TO postgres;

-- DROP SEQUENCE public."UserRoles_Id_seq";

CREATE SEQUENCE public."UserRoles_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."UserRoles_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."UserRoles_Id_seq" TO postgres;

-- DROP SEQUENCE public."UserSubscriptions_Id_seq";

CREATE SEQUENCE public."UserSubscriptions_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."UserSubscriptions_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."UserSubscriptions_Id_seq" TO postgres;

-- DROP SEQUENCE public."Users_UserId_seq";

CREATE SEQUENCE public."Users_UserId_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."Users_UserId_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."Users_UserId_seq" TO postgres;

-- DROP SEQUENCE public.auth_provider_sync_history_id_seq;

CREATE SEQUENCE public.auth_provider_sync_history_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public.auth_provider_sync_history_id_seq OWNER TO postgres;
GRANT ALL ON SEQUENCE public.auth_provider_sync_history_id_seq TO postgres;

-- DROP SEQUENCE public.execution_annotations_id_seq;

CREATE SEQUENCE public.execution_annotations_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public.execution_annotations_id_seq OWNER TO postgres;
GRANT ALL ON SEQUENCE public.execution_annotations_id_seq TO postgres;

-- DROP SEQUENCE public.execution_entity_id_seq;

CREATE SEQUENCE public.execution_entity_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public.execution_entity_id_seq OWNER TO postgres;
GRANT ALL ON SEQUENCE public.execution_entity_id_seq TO postgres;

-- DROP SEQUENCE public.execution_metadata_temp_id_seq;

CREATE SEQUENCE public.execution_metadata_temp_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public.execution_metadata_temp_id_seq OWNER TO postgres;
GRANT ALL ON SEQUENCE public.execution_metadata_temp_id_seq TO postgres;

-- DROP SEQUENCE public.insights_by_period_id_seq;

CREATE SEQUENCE public.insights_by_period_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public.insights_by_period_id_seq OWNER TO postgres;
GRANT ALL ON SEQUENCE public.insights_by_period_id_seq TO postgres;

-- DROP SEQUENCE public."insights_metadata_metaId_seq";

CREATE SEQUENCE public."insights_metadata_metaId_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."insights_metadata_metaId_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."insights_metadata_metaId_seq" TO postgres;

-- DROP SEQUENCE public.insights_raw_id_seq;

CREATE SEQUENCE public.insights_raw_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public.insights_raw_id_seq OWNER TO postgres;
GRANT ALL ON SEQUENCE public.insights_raw_id_seq TO postgres;

-- DROP SEQUENCE public.migrations_id_seq;

CREATE SEQUENCE public.migrations_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public.migrations_id_seq OWNER TO postgres;
GRANT ALL ON SEQUENCE public.migrations_id_seq TO postgres;

-- DROP SEQUENCE public."plantanalyses_Id_seq";

CREATE SEQUENCE public."plantanalyses_Id_seq"
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;

-- Permissions

ALTER SEQUENCE public."plantanalyses_Id_seq" OWNER TO postgres;
GRANT ALL ON SEQUENCE public."plantanalyses_Id_seq" TO postgres;
-- public."AnalysisMessages" definition

-- Drop table

-- DROP TABLE public."AnalysisMessages";

CREATE TABLE public."AnalysisMessages" (
	"Id" serial4 NOT NULL,
	"PlantAnalysisId" int4 NOT NULL,
	"FromUserId" int4 NOT NULL,
	"ToUserId" int4 NOT NULL,
	"ParentMessageId" int4 NULL,
	"Message" varchar(4000) NOT NULL,
	"MessageType" varchar(50) NOT NULL,
	"Subject" varchar(200) NULL,
	"IsRead" bool DEFAULT false NOT NULL,
	"SentDate" timestamp NOT NULL,
	"ReadDate" timestamp NULL,
	"IsDeleted" bool DEFAULT false NOT NULL,
	"DeletedDate" timestamp NULL,
	"SenderRole" varchar(50) NULL,
	"SenderName" varchar(100) NULL,
	"SenderCompany" varchar(200) NULL,
	"AttachmentUrls" varchar(2000) NULL,
	"LinkedProducts" varchar(2000) NULL,
	"RecommendedActions" varchar(2000) NULL,
	"Priority" varchar(20) NULL,
	"Category" varchar(50) NULL,
	"IsFlagged" bool DEFAULT false NOT NULL,
	"FlagReason" varchar(500) NULL,
	"IsApproved" bool DEFAULT true NOT NULL,
	"ApprovedByUserId" int4 NULL,
	"ApprovedDate" timestamp NULL,
	"Rating" int4 NULL,
	"RatingFeedback" varchar(500) NULL,
	"ModerationNotes" varchar(500) NULL,
	"IpAddress" varchar(50) NULL,
	"UserAgent" varchar(500) NULL,
	"CreatedDate" timestamp NOT NULL,
	"UpdatedDate" timestamp NULL,
	"IsActive" bool DEFAULT true NOT NULL,
	"IsArchived" bool DEFAULT false NOT NULL,
	"ArchivedDate" timestamp NULL,
	"HasAttachments" bool DEFAULT false NOT NULL,
	"RequiresResponse" bool DEFAULT false NOT NULL,
	"ResponseDeadline" timestamp NULL,
	"IsImportant" bool DEFAULT false NOT NULL,
	"EmailNotificationSent" bool DEFAULT false NOT NULL,
	"EmailSentDate" timestamp NULL,
	"SmsNotificationSent" bool DEFAULT false NOT NULL,
	"SmsSentDate" timestamp NULL,
	"PushNotificationSent" bool DEFAULT false NOT NULL,
	"PushSentDate" timestamp NULL,
	"MessageStatus" varchar(20) DEFAULT 'Sent'::character varying NULL, -- Message delivery status: Sent, Delivered, Read
	"DeliveredDate" timestamp NULL, -- Timestamp when message was delivered to recipient
	"AttachmentTypes" text NULL, -- JSON array of MIME types (image/jpeg, application/pdf, etc.)
	"AttachmentSizes" text NULL, -- JSON array of file sizes in bytes
	"AttachmentNames" text NULL, -- JSON array of original filenames
	"AttachmentCount" int4 DEFAULT 0 NOT NULL, -- Total number of attachments in message
	"IsEdited" bool DEFAULT false NOT NULL, -- Message has been edited
	"EditedDate" timestamp NULL, -- When message was last edited
	"OriginalMessage" text NULL, -- Original message content before first edit
	"ForwardedFromMessageId" int4 NULL, -- ID of original message if forwarded
	"IsForwarded" bool DEFAULT false NOT NULL, -- Message is a forwarded copy
	"VoiceMessageUrl" varchar(500) NULL, -- URL to voice message audio file
	"VoiceMessageDuration" int4 NULL, -- Voice message duration in seconds
	"VoiceMessageWaveform" text NULL, -- JSON array of waveform visualization data
	CONSTRAINT "AnalysisMessages_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_AnalysisMessages_FromUserId" ON public."AnalysisMessages" USING btree ("FromUserId");
CREATE INDEX "IX_AnalysisMessages_IsDeleted" ON public."AnalysisMessages" USING btree ("IsDeleted");
CREATE INDEX "IX_AnalysisMessages_IsRead" ON public."AnalysisMessages" USING btree ("IsRead");
CREATE INDEX "IX_AnalysisMessages_MessageType" ON public."AnalysisMessages" USING btree ("MessageType");
CREATE INDEX "IX_AnalysisMessages_PlantAnalysisId" ON public."AnalysisMessages" USING btree ("PlantAnalysisId");
CREATE INDEX "IX_AnalysisMessages_PlantAnalysisId_IsDeleted_SentDate" ON public."AnalysisMessages" USING btree ("PlantAnalysisId", "IsDeleted", "SentDate" DESC) INCLUDE ("FromUserId", "ToUserId", "IsRead", "Message");
CREATE INDEX "IX_AnalysisMessages_Priority" ON public."AnalysisMessages" USING btree ("Priority");
CREATE INDEX "IX_AnalysisMessages_SentDate" ON public."AnalysisMessages" USING btree ("SentDate");
CREATE INDEX "IX_AnalysisMessages_ToUserId" ON public."AnalysisMessages" USING btree ("ToUserId");
CREATE INDEX "IX_AnalysisMessages_ToUserId_IsRead" ON public."AnalysisMessages" USING btree ("ToUserId", "IsRead");
CREATE INDEX idx_analysis_messages_attachment_count ON public."AnalysisMessages" USING btree ("AttachmentCount");
CREATE INDEX idx_analysis_messages_delivered_date ON public."AnalysisMessages" USING btree ("DeliveredDate");
CREATE INDEX idx_analysis_messages_edited ON public."AnalysisMessages" USING btree ("IsEdited") WHERE ("IsEdited" = true);
CREATE INDEX idx_analysis_messages_forwarded ON public."AnalysisMessages" USING btree ("ForwardedFromMessageId") WHERE ("ForwardedFromMessageId" IS NOT NULL);
CREATE INDEX idx_analysis_messages_has_attachments ON public."AnalysisMessages" USING btree ("HasAttachments");
CREATE INDEX idx_analysis_messages_status ON public."AnalysisMessages" USING btree ("MessageStatus");
CREATE INDEX idx_analysis_messages_voice ON public."AnalysisMessages" USING btree ("VoiceMessageUrl") WHERE ("VoiceMessageUrl" IS NOT NULL);

-- Column comments

COMMENT ON COLUMN public."AnalysisMessages"."MessageStatus" IS 'Message delivery status: Sent, Delivered, Read';
COMMENT ON COLUMN public."AnalysisMessages"."DeliveredDate" IS 'Timestamp when message was delivered to recipient';
COMMENT ON COLUMN public."AnalysisMessages"."AttachmentTypes" IS 'JSON array of MIME types (image/jpeg, application/pdf, etc.)';
COMMENT ON COLUMN public."AnalysisMessages"."AttachmentSizes" IS 'JSON array of file sizes in bytes';
COMMENT ON COLUMN public."AnalysisMessages"."AttachmentNames" IS 'JSON array of original filenames';
COMMENT ON COLUMN public."AnalysisMessages"."AttachmentCount" IS 'Total number of attachments in message';
COMMENT ON COLUMN public."AnalysisMessages"."IsEdited" IS 'Message has been edited';
COMMENT ON COLUMN public."AnalysisMessages"."EditedDate" IS 'When message was last edited';
COMMENT ON COLUMN public."AnalysisMessages"."OriginalMessage" IS 'Original message content before first edit';
COMMENT ON COLUMN public."AnalysisMessages"."ForwardedFromMessageId" IS 'ID of original message if forwarded';
COMMENT ON COLUMN public."AnalysisMessages"."IsForwarded" IS 'Message is a forwarded copy';
COMMENT ON COLUMN public."AnalysisMessages"."VoiceMessageUrl" IS 'URL to voice message audio file';
COMMENT ON COLUMN public."AnalysisMessages"."VoiceMessageDuration" IS 'Voice message duration in seconds';
COMMENT ON COLUMN public."AnalysisMessages"."VoiceMessageWaveform" IS 'JSON array of waveform visualization data';

-- Permissions

ALTER TABLE public."AnalysisMessages" OWNER TO postgres;
GRANT ALL ON TABLE public."AnalysisMessages" TO postgres;


-- public."AppInfos" definition

-- Drop table

-- DROP TABLE public."AppInfos";

CREATE TABLE public."AppInfos" (
	"Id" serial4 NOT NULL,
	"CompanyName" varchar(200) NULL,
	"CompanyDescription" varchar(2000) NULL,
	"AppVersion" varchar(50) NULL,
	"Address" varchar(500) NULL,
	"Email" varchar(200) NULL,
	"Phone" varchar(50) NULL,
	"WebsiteUrl" varchar(500) NULL,
	"FacebookUrl" varchar(500) NULL,
	"InstagramUrl" varchar(500) NULL,
	"YouTubeUrl" varchar(500) NULL,
	"TwitterUrl" varchar(500) NULL,
	"LinkedInUrl" varchar(500) NULL,
	"TermsOfServiceUrl" varchar(500) NULL,
	"PrivacyPolicyUrl" varchar(500) NULL,
	"CookiePolicyUrl" varchar(500) NULL,
	"IsActive" bool DEFAULT true NOT NULL,
	"CreatedDate" timestamp NOT NULL,
	"UpdatedDate" timestamp NOT NULL,
	"UpdatedByUserId" int4 NULL,
	CONSTRAINT "AppInfos_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_AppInfos_IsActive" ON public."AppInfos" USING btree ("IsActive");

-- Permissions

ALTER TABLE public."AppInfos" OWNER TO postgres;
GRANT ALL ON TABLE public."AppInfos" TO postgres;


-- public."BulkCodeDistributionJobs" definition

-- Drop table

-- DROP TABLE public."BulkCodeDistributionJobs";

CREATE TABLE public."BulkCodeDistributionJobs" (
	"Id" serial4 NOT NULL,
	"SponsorId" int4 NOT NULL, -- ID of sponsor initiating the bulk distribution
	"PurchaseId" int4 NOT NULL, -- Package purchase ID from which codes will be distributed
	"SendSms" bool NOT NULL,
	"DeliveryMethod" varchar(50) DEFAULT 'Direct'::character varying NOT NULL, -- Distribution method: Direct, SMS, or Both
	"TotalFarmers" int4 NOT NULL,
	"ProcessedFarmers" int4 DEFAULT 0 NOT NULL,
	"SuccessfulDistributions" int4 DEFAULT 0 NOT NULL,
	"FailedDistributions" int4 DEFAULT 0 NOT NULL,
	"Status" varchar(50) DEFAULT 'Pending'::character varying NOT NULL, -- Job status: Pending, Processing, Completed, PartialSuccess, or Failed
	"CreatedDate" timestamp NOT NULL,
	"StartedDate" timestamp NULL,
	"CompletedDate" timestamp NULL,
	"OriginalFileName" varchar(500) NOT NULL,
	"FileSize" int4 NOT NULL,
	"ResultFileUrl" varchar(1000) NULL,
	"ErrorSummary" text NULL,
	"TotalCodesDistributed" int4 DEFAULT 0 NOT NULL, -- Total number of codes distributed (can be multiple per farmer)
	"TotalSmsSent" int4 DEFAULT 0 NOT NULL, -- Total SMS messages sent during distribution
	CONSTRAINT "BulkCodeDistributionJobs_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_BulkCodeDistributionJobs_CreatedDate" ON public."BulkCodeDistributionJobs" USING btree ("CreatedDate");
CREATE INDEX "IX_BulkCodeDistributionJobs_SponsorId" ON public."BulkCodeDistributionJobs" USING btree ("SponsorId");
CREATE INDEX "IX_BulkCodeDistributionJobs_SponsorId_CreatedDate" ON public."BulkCodeDistributionJobs" USING btree ("SponsorId", "CreatedDate");
CREATE INDEX "IX_BulkCodeDistributionJobs_Status" ON public."BulkCodeDistributionJobs" USING btree ("Status");
COMMENT ON TABLE public."BulkCodeDistributionJobs" IS 'Tracks bulk farmer code distribution jobs initiated via Excel upload';

-- Column comments

COMMENT ON COLUMN public."BulkCodeDistributionJobs"."SponsorId" IS 'ID of sponsor initiating the bulk distribution';
COMMENT ON COLUMN public."BulkCodeDistributionJobs"."PurchaseId" IS 'Package purchase ID from which codes will be distributed';
COMMENT ON COLUMN public."BulkCodeDistributionJobs"."DeliveryMethod" IS 'Distribution method: Direct, SMS, or Both';
COMMENT ON COLUMN public."BulkCodeDistributionJobs"."Status" IS 'Job status: Pending, Processing, Completed, PartialSuccess, or Failed';
COMMENT ON COLUMN public."BulkCodeDistributionJobs"."TotalCodesDistributed" IS 'Total number of codes distributed (can be multiple per farmer)';
COMMENT ON COLUMN public."BulkCodeDistributionJobs"."TotalSmsSent" IS 'Total SMS messages sent during distribution';

-- Permissions

ALTER TABLE public."BulkCodeDistributionJobs" OWNER TO postgres;
GRANT ALL ON TABLE public."BulkCodeDistributionJobs" TO postgres;


-- public."BulkSubscriptionAssignmentJobs" definition

-- Drop table

-- DROP TABLE public."BulkSubscriptionAssignmentJobs";

CREATE TABLE public."BulkSubscriptionAssignmentJobs" (
	"Id" serial4 NOT NULL,
	"AdminId" int4 NOT NULL,
	"DefaultTierId" int4 NULL,
	"DefaultDurationDays" int4 NULL,
	"SendNotification" bool NOT NULL,
	"NotificationMethod" varchar(50) DEFAULT 'Email'::character varying NOT NULL,
	"AutoActivate" bool DEFAULT true NOT NULL,
	"TotalFarmers" int4 NOT NULL,
	"ProcessedFarmers" int4 DEFAULT 0 NOT NULL,
	"SuccessfulAssignments" int4 DEFAULT 0 NOT NULL,
	"FailedAssignments" int4 DEFAULT 0 NOT NULL,
	"Status" varchar(50) DEFAULT 'Pending'::character varying NOT NULL,
	"CreatedDate" timestamp NOT NULL,
	"StartedDate" timestamp NULL,
	"CompletedDate" timestamp NULL,
	"OriginalFileName" varchar(500) NOT NULL,
	"FileSize" int4 NOT NULL,
	"ResultFileUrl" varchar(1000) NULL,
	"ErrorSummary" text NULL,
	"NewSubscriptionsCreated" int4 DEFAULT 0 NOT NULL,
	"ExistingSubscriptionsUpdated" int4 DEFAULT 0 NOT NULL,
	"TotalNotificationsSent" int4 DEFAULT 0 NOT NULL,
	CONSTRAINT "BulkSubscriptionAssignmentJobs_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_BulkSubscriptionAssignmentJobs_AdminId" ON public."BulkSubscriptionAssignmentJobs" USING btree ("AdminId");
CREATE INDEX "IX_BulkSubscriptionAssignmentJobs_AdminId_CreatedDate" ON public."BulkSubscriptionAssignmentJobs" USING btree ("AdminId", "CreatedDate");
CREATE INDEX "IX_BulkSubscriptionAssignmentJobs_CreatedDate" ON public."BulkSubscriptionAssignmentJobs" USING btree ("CreatedDate");
CREATE INDEX "IX_BulkSubscriptionAssignmentJobs_Status" ON public."BulkSubscriptionAssignmentJobs" USING btree ("Status");

-- Permissions

ALTER TABLE public."BulkSubscriptionAssignmentJobs" OWNER TO postgres;
GRANT ALL ON TABLE public."BulkSubscriptionAssignmentJobs" TO postgres;


-- public."Configurations" definition

-- Drop table

-- DROP TABLE public."Configurations";

CREATE TABLE public."Configurations" (
	"Id" int4 GENERATED BY DEFAULT AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"Key" varchar(100) NOT NULL,
	"Value" varchar(500) NOT NULL,
	"Description" varchar(1000) NULL,
	"Category" varchar(50) NOT NULL,
	"ValueType" varchar(20) NOT NULL,
	"IsActive" bool DEFAULT true NOT NULL,
	"CreatedDate" timestamptz DEFAULT now() NOT NULL,
	"UpdatedDate" timestamptz NULL,
	"CreatedBy" int4 NULL,
	"UpdatedBy" int4 NULL,
	CONSTRAINT "PK_Configurations" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_Configurations_Category" ON public."Configurations" USING btree ("Category");
CREATE UNIQUE INDEX "IX_Configurations_Key" ON public."Configurations" USING btree ("Key");

-- Permissions

ALTER TABLE public."Configurations" OWNER TO postgres;
GRANT ALL ON TABLE public."Configurations" TO postgres;


-- public."DeepLinks" definition

-- Drop table

-- DROP TABLE public."DeepLinks";

CREATE TABLE public."DeepLinks" (
	"Id" serial4 NOT NULL,
	"LinkId" varchar(50) NOT NULL,
	"Type" varchar(50) NOT NULL,
	"PrimaryParameter" varchar(200) NULL,
	"AdditionalParameters" varchar(500) NULL,
	"DeepLinkUrl" varchar(500) NOT NULL,
	"UniversalLinkUrl" varchar(500) NULL,
	"WebFallbackUrl" varchar(500) NULL,
	"ShortUrl" varchar(200) NULL,
	"QrCodeUrl" text NULL,
	"CampaignSource" varchar(50) NULL,
	"SponsorId" varchar(50) NULL,
	"CreatedDate" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"ExpiryDate" timestamp NOT NULL,
	"IsActive" bool DEFAULT true NOT NULL,
	"TotalClicks" int4 DEFAULT 0 NOT NULL,
	"MobileAppOpens" int4 DEFAULT 0 NOT NULL,
	"WebFallbackOpens" int4 DEFAULT 0 NOT NULL,
	"UniqueDevices" int4 DEFAULT 0 NOT NULL,
	"LastClickDate" timestamp NULL,
	CONSTRAINT "DeepLinks_LinkId_key" UNIQUE ("LinkId"),
	CONSTRAINT "DeepLinks_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_DeepLinks_LinkId" ON public."DeepLinks" USING btree ("LinkId");
CREATE INDEX "IX_DeepLinks_SponsorId" ON public."DeepLinks" USING btree ("SponsorId");
CREATE INDEX "IX_DeepLinks_Type" ON public."DeepLinks" USING btree ("Type");

-- Permissions

ALTER TABLE public."DeepLinks" OWNER TO postgres;
GRANT ALL ON TABLE public."DeepLinks" TO postgres;


-- public."Features" definition

-- Drop table

-- DROP TABLE public."Features";

CREATE TABLE public."Features" (
	"Id" serial4 NOT NULL,
	"FeatureKey" varchar(100) NOT NULL,
	"DisplayName" varchar(200) NOT NULL,
	"Description" varchar(1000) NULL,
	"DefaultConfigJson" varchar(2000) NULL,
	"RequiresConfiguration" bool DEFAULT false NOT NULL,
	"IsActive" bool DEFAULT true NOT NULL,
	"IsDeprecated" bool DEFAULT false NOT NULL,
	"CreatedDate" timestamp DEFAULT now() NOT NULL,
	"UpdatedDate" timestamp NULL,
	CONSTRAINT "Features_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "UQ_Features_FeatureKey" UNIQUE ("FeatureKey")
);
CREATE INDEX "IX_Features_FeatureKey" ON public."Features" USING btree ("FeatureKey");
CREATE INDEX "IX_Features_IsActive" ON public."Features" USING btree ("IsActive");

-- Permissions

ALTER TABLE public."Features" OWNER TO postgres;
GRANT ALL ON TABLE public."Features" TO postgres;


-- public."GroupClaims" definition

-- Drop table

-- DROP TABLE public."GroupClaims";

CREATE TABLE public."GroupClaims" (
	"GroupId" int4 NOT NULL,
	"ClaimId" int4 NOT NULL,
	CONSTRAINT "PK_GroupClaims" PRIMARY KEY ("GroupId", "ClaimId")
);

-- Permissions

ALTER TABLE public."GroupClaims" OWNER TO postgres;
GRANT ALL ON TABLE public."GroupClaims" TO postgres;


-- public."Groups" definition

-- Drop table

-- DROP TABLE public."Groups";

CREATE TABLE public."Groups" (
	"Id" int4 GENERATED BY DEFAULT AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"GroupName" varchar(50) NOT NULL,
	CONSTRAINT "PK_Groups" PRIMARY KEY ("Id")
);

-- Permissions

ALTER TABLE public."Groups" OWNER TO postgres;
GRANT ALL ON TABLE public."Groups" TO postgres;


-- public."Languages" definition

-- Drop table

-- DROP TABLE public."Languages";

CREATE TABLE public."Languages" (
	"Id" int4 GENERATED BY DEFAULT AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"Name" varchar(10) NOT NULL,
	"Code" varchar(10) NOT NULL,
	CONSTRAINT "PK_Languages" PRIMARY KEY ("Id")
);

-- Permissions

ALTER TABLE public."Languages" OWNER TO postgres;
GRANT ALL ON TABLE public."Languages" TO postgres;


-- public."Logs" definition

-- Drop table

-- DROP TABLE public."Logs";

CREATE TABLE public."Logs" (
	"Id" int4 GENERATED BY DEFAULT AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"MessageTemplate" text NULL,
	"Level" text NULL,
	"TimeStamp" timestamp NOT NULL,
	"Exception" text NULL,
	CONSTRAINT "PK_Logs" PRIMARY KEY ("Id")
);

-- Permissions

ALTER TABLE public."Logs" OWNER TO postgres;
GRANT ALL ON TABLE public."Logs" TO postgres;


-- public."MobileLogins" definition

-- Drop table

-- DROP TABLE public."MobileLogins";

CREATE TABLE public."MobileLogins" (
	"Id" int4 GENERATED BY DEFAULT AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"Provider" int4 NOT NULL,
	"ExternalUserId" varchar(20) NOT NULL,
	"Code" int4 NOT NULL,
	"SendDate" timestamp NOT NULL,
	"IsSend" bool NOT NULL,
	"IsUsed" bool NOT NULL,
	CONSTRAINT "PK_MobileLogins" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_MobileLogins_ExternalUserId_Provider" ON public."MobileLogins" USING btree ("ExternalUserId", "Provider");

-- Permissions

ALTER TABLE public."MobileLogins" OWNER TO postgres;
GRANT ALL ON TABLE public."MobileLogins" TO postgres;


-- public."OperationClaims" definition

-- Drop table

-- DROP TABLE public."OperationClaims";

CREATE TABLE public."OperationClaims" (
	"Id" int4 GENERATED BY DEFAULT AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"Name" varchar(50) NOT NULL,
	"Alias" varchar(50) NULL,
	"Description" varchar(100) NULL,
	CONSTRAINT "PK_OperationClaims" PRIMARY KEY ("Id")
);

-- Permissions

ALTER TABLE public."OperationClaims" OWNER TO postgres;
GRANT ALL ON TABLE public."OperationClaims" TO postgres;


-- public."PlantAnalyses_yedek" definition

-- Drop table

-- DROP TABLE public."PlantAnalyses_yedek";

CREATE TABLE public."PlantAnalyses_yedek" (
	"Id" int4 GENERATED BY DEFAULT AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"ImagePath" varchar(500) NULL,
	"PlantType" varchar(100) NULL,
	"GrowthStage" varchar(100) NULL,
	"ElementDeficiencies" text NULL,
	"Diseases" text NULL,
	"Pests" text NULL,
	"AnalysisResult" text NULL,
	"AnalysisDate" timestamptz NOT NULL,
	"UserId" int4 NULL,
	"AnalysisStatus" varchar(50) NULL,
	"N8nWebhookResponse" text NULL,
	"Status" bool DEFAULT true NOT NULL,
	"CreatedDate" timestamptz NOT NULL,
	"UpdatedDate" timestamptz NULL,
	"DetailedAnalysisData" text NULL,
	"AdditionalInfo" text NULL,
	"AiModel" varchar(100) NULL,
	"Altitude" int4 NULL,
	"AnalysisId" varchar(200) NULL,
	"ConfidenceLevel" numeric(5, 2) NULL,
	"ContactEmail" varchar(100) NULL,
	"ContactPhone" varchar(50) NULL,
	"CropType" varchar(100) NULL,
	"CrossFactorInsights" text NULL,
	"DiseaseSymptoms" text NULL,
	"EstimatedYieldImpact" varchar(100) NULL,
	"ExpectedHarvestDate" timestamptz NULL,
	"FarmerId" varchar(100) NULL,
	"FieldId" varchar(100) NULL,
	"HealthSeverity" varchar(50) NULL,
	"Humidity" numeric(5, 2) NULL,
	"IdentificationConfidence" numeric(5, 2) NULL,
	"ImageSizeKb" numeric(10, 2) NULL,
	"LastFertilization" timestamptz NULL,
	"LastIrrigation" timestamptz NULL,
	"Latitude" numeric(18, 6) NULL,
	"Location" varchar(200) NULL,
	"Longitude" numeric(18, 6) NULL,
	"Notes" varchar(1000) NULL,
	"NutrientStatus" text NULL,
	"OverallHealthScore" int4 NULL,
	"PlantSpecies" varchar(200) NULL,
	"PlantVariety" varchar(100) NULL,
	"PlantingDate" timestamptz NULL,
	"PreviousTreatments" text NULL,
	"PrimaryConcern" varchar(500) NULL,
	"PrimaryDeficiency" varchar(100) NULL,
	"Prognosis" varchar(100) NULL,
	"Recommendations" text NULL,
	"SoilType" varchar(100) NULL,
	"SponsorId" varchar(100) NULL,
	"StressIndicators" text NULL,
	"Temperature" numeric(5, 2) NULL,
	"TotalCostTry" numeric(10, 4) NULL,
	"TotalCostUsd" numeric(10, 6) NULL,
	"TotalTokens" numeric(10, 2) NULL,
	"UrgencyLevel" varchar(50) NULL,
	"VigorScore" int4 NULL,
	"WeatherConditions" varchar(100) NULL,
	"SponsorUserId" int4 NULL,
	"SponsorshipCodeId" int4 NULL,
	"AffectedAreaPercentage" numeric NULL,
	"CalciumStatus" text NULL,
	"ChemicalDamage" text NULL,
	"CorrelationId" text NULL,
	"CriticalIssuesCount" int4 NULL,
	"DamagePattern" text NULL,
	"DiseasesDetected" text NULL,
	"Error" bool NULL,
	"ErrorMessage" text NULL,
	"ErrorType" text NULL,
	"GrowthPattern" text NULL,
	"IdentifyingFeatures" text NULL,
	"ImageFormat" text NULL,
	"ImageSizeBytes" int8 NULL,
	"ImageSizeMb" numeric NULL,
	"ImageUploadTimestamp" timestamp NULL,
	"ImageUrl" text NULL,
	"ImmediateRecommendations" text NULL,
	"IronStatus" text NULL,
	"LeafColor" text NULL,
	"LeafTexture" text NULL,
	"LightStress" text NULL,
	"MagnesiumStatus" text NULL,
	"Message" text NULL,
	"MessageId" text NULL,
	"MessagePriority" text NULL,
	"MonitoringRecommendations" text NULL,
	"NitrogenStatus" text NULL,
	"NutrientSeverity" text NULL,
	"ParseSuccess" bool NULL,
	"PestsDetected" text NULL,
	"PhosphorusStatus" text NULL,
	"PhysicalDamage" text NULL,
	"PotassiumStatus" text NULL,
	"PreventiveRecommendations" text NULL,
	"PrimaryIssue" text NULL,
	"PrimaryStressor" text NULL,
	"ProcessingTimeMs" int8 NULL,
	"ProcessingTimestamp" timestamp NULL,
	"ReceivedAt" timestamp NULL,
	"ResponseQueue" text NULL,
	"RetryCount" int4 NULL,
	"RoutingKey" text NULL,
	"SecondaryConcerns" text NULL,
	"SecondaryDeficiencies" text NULL,
	"ShortTermRecommendations" text NULL,
	"SoilIndicators" text NULL,
	"SpreadRisk" text NULL,
	"StructuralIntegrity" text NULL,
	"Success" bool NULL,
	"TemperatureStress" text NULL,
	"VisibleParts" text NULL,
	"WaterStatus" text NULL,
	"WorkflowVersion" text NULL,
	CONSTRAINT "PK_PlantAnalyses" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_PlantAnalyses_AnalysisDate" ON public."PlantAnalyses_yedek" USING btree ("AnalysisDate");
CREATE INDEX "IX_PlantAnalyses_AnalysisId" ON public."PlantAnalyses_yedek" USING btree ("AnalysisId");
CREATE INDEX "IX_PlantAnalyses_CropType" ON public."PlantAnalyses_yedek" USING btree ("CropType");
CREATE INDEX "IX_PlantAnalyses_FarmerId" ON public."PlantAnalyses_yedek" USING btree ("FarmerId");
CREATE INDEX "IX_PlantAnalyses_SponsorId" ON public."PlantAnalyses_yedek" USING btree ("SponsorId");
CREATE INDEX "IX_PlantAnalyses_UserId" ON public."PlantAnalyses_yedek" USING btree ("UserId");

-- Permissions

ALTER TABLE public."PlantAnalyses_yedek" OWNER TO postgres;
GRANT ALL ON TABLE public."PlantAnalyses_yedek" TO postgres;


-- public."Roles" definition

-- Drop table

-- DROP TABLE public."Roles";

CREATE TABLE public."Roles" (
	"Id" serial4 NOT NULL,
	"Name" varchar(50) NOT NULL,
	"Description" varchar(200) NULL,
	"Status" bool DEFAULT true NOT NULL,
	"CreatedDate" timestamp DEFAULT now() NOT NULL,
	"UpdatedDate" timestamp NULL,
	CONSTRAINT "Roles_pkey" PRIMARY KEY ("Id")
);

-- Permissions

ALTER TABLE public."Roles" OWNER TO postgres;
GRANT ALL ON TABLE public."Roles" TO postgres;


-- public."SmsLogs" definition

-- Drop table

-- DROP TABLE public."SmsLogs";

CREATE TABLE public."SmsLogs" (
	"Id" serial4 NOT NULL,
	"Action" varchar(50) NOT NULL,
	"SenderUserId" int4 NULL,
	"Content" text NOT NULL,
	"CreatedDate" timestamp DEFAULT now() NOT NULL,
	CONSTRAINT "SmsLogs_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_SmsLogs_Action" ON public."SmsLogs" USING btree ("Action");
CREATE INDEX "IX_SmsLogs_CreatedDate" ON public."SmsLogs" USING btree ("CreatedDate");
CREATE INDEX "IX_SmsLogs_SenderUserId" ON public."SmsLogs" USING btree ("SenderUserId");

-- Permissions

ALTER TABLE public."SmsLogs" OWNER TO postgres;
GRANT ALL ON TABLE public."SmsLogs" TO postgres;


-- public."SponsorProfiles" definition

-- Drop table

-- DROP TABLE public."SponsorProfiles";

CREATE TABLE public."SponsorProfiles" (
	"Id" serial4 NOT NULL,
	"SponsorId" int4 NOT NULL,
	"CompanyName" varchar(200) NOT NULL,
	"CompanyDescription" varchar(1000) NULL,
	"SponsorLogoUrl" varchar(500) NULL,
	"WebsiteUrl" varchar(500) NULL,
	"ContactEmail" varchar(200) NULL,
	"ContactPhone" varchar(50) NULL,
	"ContactPerson" varchar(200) NULL,
	"LinkedInUrl" varchar(500) NULL,
	"TwitterUrl" varchar(500) NULL,
	"FacebookUrl" varchar(500) NULL,
	"InstagramUrl" varchar(500) NULL,
	"TaxNumber" varchar(50) NULL,
	"TradeRegistryNumber" varchar(50) NULL,
	"Address" varchar(500) NULL,
	"City" varchar(100) NULL,
	"Country" varchar(100) NULL,
	"PostalCode" varchar(20) NULL,
	"IsVerifiedCompany" bool DEFAULT false NOT NULL,
	"CompanyType" varchar(100) NULL,
	"BusinessModel" varchar(100) NULL,
	"IsVerified" bool DEFAULT false NOT NULL,
	"VerificationDate" timestamp NULL,
	"VerificationNotes" varchar(1000) NULL,
	"IsActive" bool DEFAULT true NOT NULL,
	"TotalPurchases" int4 DEFAULT 0 NOT NULL,
	"TotalCodesGenerated" int4 DEFAULT 0 NOT NULL,
	"TotalCodesRedeemed" int4 DEFAULT 0 NOT NULL,
	"TotalInvestment" numeric(18, 2) DEFAULT 0 NOT NULL,
	"CreatedDate" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"UpdatedDate" timestamp NULL,
	"CreatedByUserId" int4 NULL,
	"UpdatedByUserId" int4 NULL,
	"SponsorLogoThumbnailUrl" varchar(500) NULL, -- Thumbnail URL for sponsor logo (128x128 pixels)
	CONSTRAINT "SponsorProfiles_SponsorId_key" UNIQUE ("SponsorId"),
	CONSTRAINT "SponsorProfiles_pkey" PRIMARY KEY ("Id")
);

-- Column comments

COMMENT ON COLUMN public."SponsorProfiles"."SponsorLogoThumbnailUrl" IS 'Thumbnail URL for sponsor logo (128x128 pixels)';

-- Permissions

ALTER TABLE public."SponsorProfiles" OWNER TO postgres;
GRANT ALL ON TABLE public."SponsorProfiles" TO postgres;


-- public."SubscriptionTiers" definition

-- Drop table

-- DROP TABLE public."SubscriptionTiers";

CREATE TABLE public."SubscriptionTiers" (
	"Id" serial4 NOT NULL,
	"TierName" varchar(10) NOT NULL,
	"DisplayName" varchar(50) NOT NULL,
	"Description" text NULL,
	"DailyRequestLimit" int4 NOT NULL,
	"MonthlyRequestLimit" int4 NOT NULL,
	"MonthlyPrice" numeric(10, 2) NOT NULL,
	"YearlyPrice" numeric(10, 2) NULL,
	"Currency" varchar(3) DEFAULT 'TRY'::character varying NULL,
	"PrioritySupport" bool DEFAULT false NULL,
	"AdvancedAnalytics" bool DEFAULT false NULL,
	"ApiAccess" bool DEFAULT true NULL,
	"ResponseTimeHours" int4 DEFAULT 48 NULL,
	"AdditionalFeatures" text DEFAULT '[]'::text NULL,
	"IsActive" bool DEFAULT true NULL,
	"DisplayOrder" int4 DEFAULT 0 NULL,
	"CreatedDate" timestamptz DEFAULT CURRENT_TIMESTAMP NULL,
	"UpdatedDate" timestamptz NULL,
	"CreatedUserId" int4 NULL,
	"UpdatedUserId" int4 NULL,
	"MinPurchaseQuantity" int4 DEFAULT 10 NOT NULL,
	"MaxPurchaseQuantity" int4 DEFAULT 10000 NOT NULL,
	"RecommendedQuantity" int4 DEFAULT 100 NOT NULL,
	CONSTRAINT "CK_SubscriptionTiers_MaxQuantity_GreaterThanMin" CHECK (("MaxPurchaseQuantity" >= "MinPurchaseQuantity")),
	CONSTRAINT "CK_SubscriptionTiers_MinQuantity_Positive" CHECK (("MinPurchaseQuantity" > 0)),
	CONSTRAINT "CK_SubscriptionTiers_RecommendedQuantity_InRange" CHECK ((("RecommendedQuantity" >= "MinPurchaseQuantity") AND ("RecommendedQuantity" <= "MaxPurchaseQuantity"))),
	CONSTRAINT "SubscriptionTiers_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT unique_tier_name UNIQUE ("TierName")
);
CREATE INDEX "IX_SubscriptionTiers_Quantities" ON public."SubscriptionTiers" USING btree ("MinPurchaseQuantity", "MaxPurchaseQuantity");

-- Permissions

ALTER TABLE public."SubscriptionTiers" OWNER TO postgres;
GRANT ALL ON TABLE public."SubscriptionTiers" TO postgres;


-- public."Translates" definition

-- Drop table

-- DROP TABLE public."Translates";

CREATE TABLE public."Translates" (
	"Id" int4 GENERATED BY DEFAULT AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"LangId" int4 NOT NULL,
	"Code" varchar(50) NOT NULL,
	"Value" varchar(500) NOT NULL,
	CONSTRAINT "PK_Translates" PRIMARY KEY ("Id")
);

-- Permissions

ALTER TABLE public."Translates" OWNER TO postgres;
GRANT ALL ON TABLE public."Translates" TO postgres;


-- public."UserClaims" definition

-- Drop table

-- DROP TABLE public."UserClaims";

CREATE TABLE public."UserClaims" (
	"UserId" int4 NOT NULL,
	"ClaimId" int4 NOT NULL,
	CONSTRAINT "PK_UserClaims" PRIMARY KEY ("UserId", "ClaimId")
);

-- Permissions

ALTER TABLE public."UserClaims" OWNER TO postgres;
GRANT ALL ON TABLE public."UserClaims" TO postgres;


-- public."UserGroups" definition

-- Drop table

-- DROP TABLE public."UserGroups";

CREATE TABLE public."UserGroups" (
	"GroupId" int4 NOT NULL,
	"UserId" int4 NOT NULL,
	CONSTRAINT "PK_UserGroups" PRIMARY KEY ("UserId", "GroupId")
);

-- Permissions

ALTER TABLE public."UserGroups" OWNER TO postgres;
GRANT ALL ON TABLE public."UserGroups" TO postgres;


-- public."__EFMigrationsHistory" definition

-- Drop table

-- DROP TABLE public."__EFMigrationsHistory";

CREATE TABLE public."__EFMigrationsHistory" (
	"MigrationId" varchar(150) NOT NULL,
	"ProductVersion" varchar(32) NOT NULL,
	CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Permissions

ALTER TABLE public."__EFMigrationsHistory" OWNER TO postgres;
GRANT ALL ON TABLE public."__EFMigrationsHistory" TO postgres;


-- public."DeepLinkClickRecords" definition

-- Drop table

-- DROP TABLE public."DeepLinkClickRecords";

CREATE TABLE public."DeepLinkClickRecords" (
	"Id" serial4 NOT NULL,
	"LinkId" varchar(50) NOT NULL,
	"UserAgent" varchar(500) NULL,
	"IpAddress" varchar(45) NULL,
	"Platform" varchar(20) NULL,
	"DeviceId" varchar(100) NULL,
	"Referrer" varchar(500) NULL,
	"ClickDate" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"Country" varchar(100) NULL,
	"City" varchar(100) NULL,
	"Source" varchar(50) NULL,
	"DidOpenApp" bool DEFAULT false NOT NULL,
	"DidCompleteAction" bool DEFAULT false NOT NULL,
	"ActionCompletedDate" timestamp NULL,
	"ActionResult" varchar(50) NULL,
	CONSTRAINT "DeepLinkClickRecords_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_DeepLinkClickRecords_DeepLinks_LinkId" FOREIGN KEY ("LinkId") REFERENCES public."DeepLinks"("LinkId") ON DELETE CASCADE
);
CREATE INDEX "IX_DeepLinkClickRecords_ClickDate" ON public."DeepLinkClickRecords" USING btree ("ClickDate");
CREATE INDEX "IX_DeepLinkClickRecords_LinkId" ON public."DeepLinkClickRecords" USING btree ("LinkId");
CREATE INDEX "IX_DeepLinkClickRecords_Platform" ON public."DeepLinkClickRecords" USING btree ("Platform");

-- Permissions

ALTER TABLE public."DeepLinkClickRecords" OWNER TO postgres;
GRANT ALL ON TABLE public."DeepLinkClickRecords" TO postgres;


-- public."TierFeatures" definition

-- Drop table

-- DROP TABLE public."TierFeatures";

CREATE TABLE public."TierFeatures" (
	"Id" serial4 NOT NULL,
	"SubscriptionTierId" int4 NOT NULL,
	"FeatureId" int4 NOT NULL,
	"IsEnabled" bool DEFAULT true NOT NULL,
	"ConfigurationJson" varchar(2000) NULL,
	"EffectiveDate" timestamp NULL,
	"ExpiryDate" timestamp NULL,
	"CreatedDate" timestamp DEFAULT now() NOT NULL,
	"UpdatedDate" timestamp NULL,
	"CreatedByUserId" int4 NOT NULL,
	"ModifiedByUserId" int4 NULL,
	CONSTRAINT "TierFeatures_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "UQ_TierFeatures_TierId_FeatureId" UNIQUE ("SubscriptionTierId", "FeatureId"),
	CONSTRAINT "FK_TierFeatures_Features" FOREIGN KEY ("FeatureId") REFERENCES public."Features"("Id") ON DELETE CASCADE,
	CONSTRAINT "FK_TierFeatures_SubscriptionTiers" FOREIGN KEY ("SubscriptionTierId") REFERENCES public."SubscriptionTiers"("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_TierFeatures_FeatureId" ON public."TierFeatures" USING btree ("FeatureId");
CREATE INDEX "IX_TierFeatures_IsEnabled" ON public."TierFeatures" USING btree ("IsEnabled");
CREATE INDEX "IX_TierFeatures_SubscriptionTierId" ON public."TierFeatures" USING btree ("SubscriptionTierId");

-- Permissions

ALTER TABLE public."TierFeatures" OWNER TO postgres;
GRANT ALL ON TABLE public."TierFeatures" TO postgres;


-- public."Users" definition

-- Drop table

-- DROP TABLE public."Users";

CREATE TABLE public."Users" (
	"UserId" int4 GENERATED BY DEFAULT AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"CitizenId" int8 NULL, -- Turkish Citizen ID. Nullable for phone-only users.
	"FullName" varchar(100) NOT NULL,
	"Email" varchar(50) NULL, -- Email for password-based authentication. Unique when not null. Either Email or MobilePhones required.
	"RefreshToken" text NULL,
	"MobilePhones" varchar(30) NULL, -- Phone number for OTP-based authentication. Format: 05XXXXXXXXX. Unique when not null.
	"Status" bool NOT NULL,
	"BirthDate" timestamp NULL,
	"Gender" int4 NULL,
	"RecordDate" timestamp NOT NULL,
	"Address" varchar(200) NULL,
	"Notes" varchar(500) NULL,
	"UpdateContactDate" timestamp NOT NULL,
	"PasswordSalt" bytea NULL, -- Password salt for email-based auth. NULL for phone-only (OTP) users.
	"PasswordHash" bytea NULL, -- Password hash for email-based auth. NULL for phone-only (OTP) users.
	"UserType" int4 DEFAULT 2 NOT NULL,
	"RegistrationReferralCode" varchar(20) NULL, -- Referral code used during registration (if any)
	"AvatarUrl" varchar(500) NULL, -- User profile avatar URL (full size, 512x512)
	"AvatarThumbnailUrl" varchar(500) NULL, -- User profile avatar thumbnail URL (optimized, 128x128)
	"AvatarUpdatedDate" timestamp NULL, -- Timestamp when avatar was last updated
	"IsActive" bool DEFAULT true NOT NULL, -- Indicates if user account is active. False means deactivated by admin.
	"DeactivatedDate" timestamp NULL, -- Timestamp when user was deactivated by admin (null if active)
	"DeactivatedBy" int4 NULL, -- Admin user ID who deactivated this user (null if active)
	"DeactivationReason" text NULL, -- Admin-provided reason for deactivation (for audit and user communication)
	"RefreshTokenExpires" timestamp NULL,
	CONSTRAINT "CK_Users_EmailOrPhone_Required" CHECK (((("Email" IS NOT NULL) AND (("Email")::text <> ''::text)) OR (("MobilePhones" IS NOT NULL) AND (("MobilePhones")::text <> ''::text)))),
	CONSTRAINT "PK_Users" PRIMARY KEY ("UserId"),
	CONSTRAINT "FK_Users_DeactivatedBy" FOREIGN KEY ("DeactivatedBy") REFERENCES public."Users"("UserId") ON DELETE SET NULL
);
CREATE INDEX "IX_Users_CitizenId" ON public."Users" USING btree ("CitizenId");
CREATE INDEX "IX_Users_DeactivatedBy_DeactivatedDate" ON public."Users" USING btree ("DeactivatedBy", "DeactivatedDate") WHERE ("DeactivatedBy" IS NOT NULL);
CREATE UNIQUE INDEX "IX_Users_Email_Unique" ON public."Users" USING btree ("Email") WHERE (("Email" IS NOT NULL) AND (("Email")::text <> ''::text));
CREATE INDEX "IX_Users_IsActive" ON public."Users" USING btree ("IsActive");
CREATE INDEX "IX_Users_IsActive_RecordDate" ON public."Users" USING btree ("IsActive", "RecordDate" DESC);
CREATE UNIQUE INDEX "IX_Users_MobilePhones_Unique" ON public."Users" USING btree ("MobilePhones") WHERE (("MobilePhones" IS NOT NULL) AND (("MobilePhones")::text <> ''::text));
CREATE INDEX "IX_Users_RefreshTokenExpires" ON public."Users" USING btree ("RefreshTokenExpires") WHERE ("RefreshTokenExpires" IS NOT NULL);
CREATE INDEX "IX_Users_RegistrationReferralCode" ON public."Users" USING btree ("RegistrationReferralCode");
CREATE INDEX idx_users_avatar_updated ON public."Users" USING btree ("AvatarUpdatedDate");
COMMENT ON TABLE public."Users" IS 'User accounts. Supports email+password and phone+OTP authentication.';

-- Column comments

COMMENT ON COLUMN public."Users"."CitizenId" IS 'Turkish Citizen ID. Nullable for phone-only users.';
COMMENT ON COLUMN public."Users"."Email" IS 'Email for password-based authentication. Unique when not null. Either Email or MobilePhones required.';
COMMENT ON COLUMN public."Users"."MobilePhones" IS 'Phone number for OTP-based authentication. Format: 05XXXXXXXXX. Unique when not null.';
COMMENT ON COLUMN public."Users"."PasswordSalt" IS 'Password salt for email-based auth. NULL for phone-only (OTP) users.';
COMMENT ON COLUMN public."Users"."PasswordHash" IS 'Password hash for email-based auth. NULL for phone-only (OTP) users.';
COMMENT ON COLUMN public."Users"."RegistrationReferralCode" IS 'Referral code used during registration (if any)';
COMMENT ON COLUMN public."Users"."AvatarUrl" IS 'User profile avatar URL (full size, 512x512)';
COMMENT ON COLUMN public."Users"."AvatarThumbnailUrl" IS 'User profile avatar thumbnail URL (optimized, 128x128)';
COMMENT ON COLUMN public."Users"."AvatarUpdatedDate" IS 'Timestamp when avatar was last updated';
COMMENT ON COLUMN public."Users"."IsActive" IS 'Indicates if user account is active. False means deactivated by admin.';
COMMENT ON COLUMN public."Users"."DeactivatedDate" IS 'Timestamp when user was deactivated by admin (null if active)';
COMMENT ON COLUMN public."Users"."DeactivatedBy" IS 'Admin user ID who deactivated this user (null if active)';
COMMENT ON COLUMN public."Users"."DeactivationReason" IS 'Admin-provided reason for deactivation (for audit and user communication)';

-- Permissions

ALTER TABLE public."Users" OWNER TO postgres;
GRANT ALL ON TABLE public."Users" TO postgres;


-- public."AdminOperationLogs" definition

-- Drop table

-- DROP TABLE public."AdminOperationLogs";

CREATE TABLE public."AdminOperationLogs" (
	"Id" serial4 NOT NULL,
	"AdminUserId" int4 NOT NULL, -- ID of the admin user who performed the action
	"TargetUserId" int4 NULL, -- ID of the user affected by the action (null for system-wide operations)
	"Action" varchar(100) NOT NULL,
	"EntityType" varchar(50) NULL,
	"EntityId" int4 NULL,
	"IsOnBehalfOf" bool DEFAULT false NULL, -- True when admin is acting on behalf of another user (farmer/sponsor)
	"IpAddress" varchar(45) NULL,
	"UserAgent" text NULL,
	"RequestPath" varchar(500) NULL,
	"RequestPayload" text NULL,
	"ResponseStatus" int4 NULL,
	"Duration" int4 NULL,
	"Timestamp" timestamp DEFAULT now() NOT NULL,
	"Reason" text NULL,
	"BeforeState" text NULL, -- JSON snapshot of entity state before the change (for critical operations only)
	"AfterState" text NULL, -- JSON snapshot of entity state after the change (for critical operations only)
	CONSTRAINT "AdminOperationLogs_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_AdminOperationLogs_AdminUser" FOREIGN KEY ("AdminUserId") REFERENCES public."Users"("UserId") ON DELETE CASCADE,
	CONSTRAINT "FK_AdminOperationLogs_TargetUser" FOREIGN KEY ("TargetUserId") REFERENCES public."Users"("UserId") ON DELETE SET NULL
);
CREATE INDEX "IX_AdminOperationLogs_Action" ON public."AdminOperationLogs" USING btree ("Action");
CREATE INDEX "IX_AdminOperationLogs_AdminUserId" ON public."AdminOperationLogs" USING btree ("AdminUserId");
CREATE INDEX "IX_AdminOperationLogs_AdminUserId_Timestamp" ON public."AdminOperationLogs" USING btree ("AdminUserId", "Timestamp" DESC);
CREATE INDEX "IX_AdminOperationLogs_IsOnBehalfOf" ON public."AdminOperationLogs" USING btree ("IsOnBehalfOf") WHERE ("IsOnBehalfOf" = true);
CREATE INDEX "IX_AdminOperationLogs_TargetUserId" ON public."AdminOperationLogs" USING btree ("TargetUserId") WHERE ("TargetUserId" IS NOT NULL);
CREATE INDEX "IX_AdminOperationLogs_TargetUserId_Timestamp" ON public."AdminOperationLogs" USING btree ("TargetUserId", "Timestamp" DESC) WHERE ("TargetUserId" IS NOT NULL);
CREATE INDEX "IX_AdminOperationLogs_Timestamp" ON public."AdminOperationLogs" USING btree ("Timestamp" DESC);
COMMENT ON TABLE public."AdminOperationLogs" IS 'Audit trail for all admin operations including user management, on-behalf-of actions, and system changes';

-- Column comments

COMMENT ON COLUMN public."AdminOperationLogs"."AdminUserId" IS 'ID of the admin user who performed the action';
COMMENT ON COLUMN public."AdminOperationLogs"."TargetUserId" IS 'ID of the user affected by the action (null for system-wide operations)';
COMMENT ON COLUMN public."AdminOperationLogs"."IsOnBehalfOf" IS 'True when admin is acting on behalf of another user (farmer/sponsor)';
COMMENT ON COLUMN public."AdminOperationLogs"."BeforeState" IS 'JSON snapshot of entity state before the change (for critical operations only)';
COMMENT ON COLUMN public."AdminOperationLogs"."AfterState" IS 'JSON snapshot of entity state after the change (for critical operations only)';

-- Permissions

ALTER TABLE public."AdminOperationLogs" OWNER TO postgres;
GRANT ALL ON TABLE public."AdminOperationLogs" TO postgres;


-- public."BulkInvitationJobs" definition

-- Drop table

-- DROP TABLE public."BulkInvitationJobs";

CREATE TABLE public."BulkInvitationJobs" (
	"Id" serial4 NOT NULL,
	"SponsorId" int4 NOT NULL,
	"InvitationType" varchar(50) NOT NULL, -- Invite: Email/SMS invitation, AutoCreate: Automatic account creation
	"DefaultTier" varchar(10) NULL,
	"DefaultCodeCount" int4 DEFAULT 0 NOT NULL,
	"SendSms" bool DEFAULT true NOT NULL,
	"TotalDealers" int4 DEFAULT 0 NOT NULL,
	"ProcessedDealers" int4 DEFAULT 0 NOT NULL,
	"SuccessfulInvitations" int4 DEFAULT 0 NOT NULL,
	"FailedInvitations" int4 DEFAULT 0 NOT NULL,
	"Status" varchar(50) DEFAULT 'Pending'::character varying NOT NULL, -- Pending, Processing, Completed, PartialSuccess, Failed
	"CreatedDate" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"StartedDate" timestamp NULL,
	"CompletedDate" timestamp NULL,
	"OriginalFileName" varchar(500) NOT NULL,
	"FileSize" int4 NOT NULL,
	"ResultFileUrl" varchar(1000) NULL,
	"ErrorSummary" text NULL, -- JSON array: [{"rowNumber": 12, "email": "test@email.com", "error": "message", "timestamp": "2025-11-03T15:30:00Z"}]
	CONSTRAINT "BulkInvitationJobs_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_BulkInvitationJobs_Users_SponsorId" FOREIGN KEY ("SponsorId") REFERENCES public."Users"("UserId") ON DELETE CASCADE
);
CREATE INDEX "IX_BulkInvitationJobs_CreatedDate" ON public."BulkInvitationJobs" USING btree ("CreatedDate" DESC);
CREATE INDEX "IX_BulkInvitationJobs_SponsorId" ON public."BulkInvitationJobs" USING btree ("SponsorId");
CREATE INDEX "IX_BulkInvitationJobs_SponsorId_Status" ON public."BulkInvitationJobs" USING btree ("SponsorId", "Status");
CREATE INDEX "IX_BulkInvitationJobs_Status" ON public."BulkInvitationJobs" USING btree ("Status");
COMMENT ON TABLE public."BulkInvitationJobs" IS 'Tracks bulk dealer invitation jobs processed through RabbitMQ queue';

-- Column comments

COMMENT ON COLUMN public."BulkInvitationJobs"."InvitationType" IS 'Invite: Email/SMS invitation, AutoCreate: Automatic account creation';
COMMENT ON COLUMN public."BulkInvitationJobs"."Status" IS 'Pending, Processing, Completed, PartialSuccess, Failed';
COMMENT ON COLUMN public."BulkInvitationJobs"."ErrorSummary" IS 'JSON array: [{"rowNumber": 12, "email": "test@email.com", "error": "message", "timestamp": "2025-11-03T15:30:00Z"}]';

-- Permissions

ALTER TABLE public."BulkInvitationJobs" OWNER TO postgres;
GRANT ALL ON TABLE public."BulkInvitationJobs" TO postgres;


-- public."FarmerSponsorBlocks" definition

-- Drop table

-- DROP TABLE public."FarmerSponsorBlocks";

CREATE TABLE public."FarmerSponsorBlocks" (
	"Id" serial4 NOT NULL, -- Primary key
	"FarmerId" int4 NOT NULL, -- Farmer user ID who is blocking
	"SponsorId" int4 NOT NULL, -- Sponsor user ID being blocked
	"IsBlocked" bool DEFAULT false NOT NULL, -- Is the sponsor blocked (cannot send messages)
	"IsMuted" bool DEFAULT false NOT NULL, -- Is the sponsor muted (can send but farmer doesnt get notifications)
	"CreatedDate" timestamp NOT NULL, -- When the block/mute was created
	"Reason" varchar(500) NULL, -- Optional reason for blocking (e.g., Spam, Inappropriate, No longer needed)
	CONSTRAINT "FarmerSponsorBlocks_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_FarmerSponsorBlocks_Users_FarmerId" FOREIGN KEY ("FarmerId") REFERENCES public."Users"("UserId") ON DELETE RESTRICT,
	CONSTRAINT "FK_FarmerSponsorBlocks_Users_SponsorId" FOREIGN KEY ("SponsorId") REFERENCES public."Users"("UserId") ON DELETE RESTRICT
);
CREATE INDEX "IX_FarmerSponsorBlocks_FarmerId" ON public."FarmerSponsorBlocks" USING btree ("FarmerId");
CREATE UNIQUE INDEX "IX_FarmerSponsorBlocks_FarmerId_SponsorId" ON public."FarmerSponsorBlocks" USING btree ("FarmerId", "SponsorId");
CREATE INDEX "IX_FarmerSponsorBlocks_SponsorId" ON public."FarmerSponsorBlocks" USING btree ("SponsorId");
COMMENT ON TABLE public."FarmerSponsorBlocks" IS 'Farmer-initiated blocking of sponsors for messaging system. Allows farmers to prevent unwanted messages from specific sponsors.';

-- Column comments

COMMENT ON COLUMN public."FarmerSponsorBlocks"."Id" IS 'Primary key';
COMMENT ON COLUMN public."FarmerSponsorBlocks"."FarmerId" IS 'Farmer user ID who is blocking';
COMMENT ON COLUMN public."FarmerSponsorBlocks"."SponsorId" IS 'Sponsor user ID being blocked';
COMMENT ON COLUMN public."FarmerSponsorBlocks"."IsBlocked" IS 'Is the sponsor blocked (cannot send messages)';
COMMENT ON COLUMN public."FarmerSponsorBlocks"."IsMuted" IS 'Is the sponsor muted (can send but farmer doesnt get notifications)';
COMMENT ON COLUMN public."FarmerSponsorBlocks"."CreatedDate" IS 'When the block/mute was created';
COMMENT ON COLUMN public."FarmerSponsorBlocks"."Reason" IS 'Optional reason for blocking (e.g., Spam, Inappropriate, No longer needed)';

-- Permissions

ALTER TABLE public."FarmerSponsorBlocks" OWNER TO postgres;
GRANT ALL ON TABLE public."FarmerSponsorBlocks" TO postgres;


-- public."MessagingFeatures" definition

-- Drop table

-- DROP TABLE public."MessagingFeatures";

CREATE TABLE public."MessagingFeatures" (
	"Id" serial4 NOT NULL,
	"FeatureName" varchar(100) NOT NULL, -- Unique identifier (e.g., VoiceMessages, ImageAttachments)
	"DisplayName" varchar(200) NULL,
	"IsEnabled" bool DEFAULT true NOT NULL, -- Admin toggle - global on/off switch
	"RequiredTier" varchar(20) DEFAULT 'None'::character varying NOT NULL, -- Minimum subscription tier (None, S, M, L, XL)
	"MaxFileSize" int8 NULL, -- Maximum file size in bytes for attachments
	"MaxDuration" int4 NULL, -- Maximum duration in seconds for voice/video
	"AllowedMimeTypes" varchar(1000) NULL, -- Comma-separated allowed MIME types
	"TimeLimit" int4 NULL, -- Time limit in seconds for actions (edit/delete)
	"Description" varchar(500) NULL,
	"ConfigurationJson" text NULL,
	"CreatedDate" timestamp DEFAULT now() NOT NULL,
	"UpdatedDate" timestamp NULL,
	"CreatedByUserId" int4 NULL,
	"UpdatedByUserId" int4 NULL,
	CONSTRAINT "MessagingFeatures_FeatureName_key" UNIQUE ("FeatureName"),
	CONSTRAINT "MessagingFeatures_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_MessagingFeatures_CreatedBy" FOREIGN KEY ("CreatedByUserId") REFERENCES public."Users"("UserId") ON DELETE SET NULL,
	CONSTRAINT "FK_MessagingFeatures_UpdatedBy" FOREIGN KEY ("UpdatedByUserId") REFERENCES public."Users"("UserId") ON DELETE SET NULL
);
CREATE INDEX idx_messaging_features_enabled ON public."MessagingFeatures" USING btree ("IsEnabled");
CREATE INDEX idx_messaging_features_name ON public."MessagingFeatures" USING btree ("FeatureName");
CREATE INDEX idx_messaging_features_tier ON public."MessagingFeatures" USING btree ("RequiredTier");
COMMENT ON TABLE public."MessagingFeatures" IS 'Feature flags for messaging system with tier-based access control';

-- Column comments

COMMENT ON COLUMN public."MessagingFeatures"."FeatureName" IS 'Unique identifier (e.g., VoiceMessages, ImageAttachments)';
COMMENT ON COLUMN public."MessagingFeatures"."IsEnabled" IS 'Admin toggle - global on/off switch';
COMMENT ON COLUMN public."MessagingFeatures"."RequiredTier" IS 'Minimum subscription tier (None, S, M, L, XL)';
COMMENT ON COLUMN public."MessagingFeatures"."MaxFileSize" IS 'Maximum file size in bytes for attachments';
COMMENT ON COLUMN public."MessagingFeatures"."MaxDuration" IS 'Maximum duration in seconds for voice/video';
COMMENT ON COLUMN public."MessagingFeatures"."AllowedMimeTypes" IS 'Comma-separated allowed MIME types';
COMMENT ON COLUMN public."MessagingFeatures"."TimeLimit" IS 'Time limit in seconds for actions (edit/delete)';

-- Permissions

ALTER TABLE public."MessagingFeatures" OWNER TO postgres;
GRANT ALL ON TABLE public."MessagingFeatures" TO postgres;


-- public."ReferralCodes" definition

-- Drop table

-- DROP TABLE public."ReferralCodes";

CREATE TABLE public."ReferralCodes" (
	"Id" serial4 NOT NULL,
	"UserId" int4 NOT NULL,
	"Code" varchar(20) NOT NULL, -- Unique referral code in format ZIRA-XXXXXX
	"IsActive" bool DEFAULT true NOT NULL,
	"CreatedAt" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"ExpiresAt" timestamp NOT NULL,
	"Status" int4 DEFAULT 0 NOT NULL, -- 0=Active, 1=Expired, 2=Disabled
	CONSTRAINT "ReferralCodes_Code_key" UNIQUE ("Code"),
	CONSTRAINT "ReferralCodes_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_ReferralCodes_Users" FOREIGN KEY ("UserId") REFERENCES public."Users"("UserId") ON DELETE CASCADE
);
CREATE INDEX "IX_ReferralCodes_Code" ON public."ReferralCodes" USING btree ("Code");
CREATE INDEX "IX_ReferralCodes_ExpiresAt" ON public."ReferralCodes" USING btree ("ExpiresAt");
CREATE INDEX "IX_ReferralCodes_Status" ON public."ReferralCodes" USING btree ("Status");
CREATE INDEX "IX_ReferralCodes_UserId" ON public."ReferralCodes" USING btree ("UserId");
COMMENT ON TABLE public."ReferralCodes" IS 'Stores user-generated referral codes with expiry tracking';

-- Column comments

COMMENT ON COLUMN public."ReferralCodes"."Code" IS 'Unique referral code in format ZIRA-XXXXXX';
COMMENT ON COLUMN public."ReferralCodes"."Status" IS '0=Active, 1=Expired, 2=Disabled';

-- Permissions

ALTER TABLE public."ReferralCodes" OWNER TO postgres;
GRANT ALL ON TABLE public."ReferralCodes" TO postgres;


-- public."ReferralConfigurations" definition

-- Drop table

-- DROP TABLE public."ReferralConfigurations";

CREATE TABLE public."ReferralConfigurations" (
	"Id" serial4 NOT NULL,
	"Key" varchar(100) NOT NULL,
	"Value" text NOT NULL,
	"Description" text NULL,
	"DataType" varchar(20) DEFAULT 'string'::character varying NOT NULL, -- int, bool, string, decimal
	"UpdatedAt" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"UpdatedBy" int4 NULL,
	CONSTRAINT "ReferralConfigurations_Key_key" UNIQUE ("Key"),
	CONSTRAINT "ReferralConfigurations_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_ReferralConfigurations_Users" FOREIGN KEY ("UpdatedBy") REFERENCES public."Users"("UserId") ON DELETE SET NULL
);
CREATE UNIQUE INDEX "IX_ReferralConfigurations_Key" ON public."ReferralConfigurations" USING btree ("Key");
COMMENT ON TABLE public."ReferralConfigurations" IS 'Configurable system settings for referral program';

-- Column comments

COMMENT ON COLUMN public."ReferralConfigurations"."DataType" IS 'int, bool, string, decimal';

-- Permissions

ALTER TABLE public."ReferralConfigurations" OWNER TO postgres;
GRANT ALL ON TABLE public."ReferralConfigurations" TO postgres;


-- public."ReferralTracking" definition

-- Drop table

-- DROP TABLE public."ReferralTracking";

CREATE TABLE public."ReferralTracking" (
	"Id" serial4 NOT NULL,
	"ReferralCodeId" int4 NOT NULL,
	"RefereeUserId" int4 NULL,
	"ClickedAt" timestamp NULL,
	"RegisteredAt" timestamp NULL,
	"FirstAnalysisAt" timestamp NULL,
	"RewardProcessedAt" timestamp NULL,
	"Status" int4 DEFAULT 0 NOT NULL, -- 0=Clicked, 1=Registered, 2=Validated, 3=Rewarded
	"RefereeMobilePhone" varchar(15) NULL,
	"IpAddress" varchar(45) NULL,
	"DeviceId" varchar(255) NULL,
	"FailureReason" text NULL,
	CONSTRAINT "ReferralTracking_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_ReferralTracking_ReferralCodes" FOREIGN KEY ("ReferralCodeId") REFERENCES public."ReferralCodes"("Id") ON DELETE CASCADE,
	CONSTRAINT "FK_ReferralTracking_Users" FOREIGN KEY ("RefereeUserId") REFERENCES public."Users"("UserId") ON DELETE SET NULL
);
CREATE INDEX "IX_ReferralTracking_DeviceId" ON public."ReferralTracking" USING btree ("DeviceId");
CREATE INDEX "IX_ReferralTracking_RefereeUserId" ON public."ReferralTracking" USING btree ("RefereeUserId");
CREATE INDEX "IX_ReferralTracking_ReferralCodeId" ON public."ReferralTracking" USING btree ("ReferralCodeId");
CREATE INDEX "IX_ReferralTracking_Status" ON public."ReferralTracking" USING btree ("Status");
COMMENT ON TABLE public."ReferralTracking" IS 'Tracks referral journey: Click → Register → Analysis → Reward';

-- Column comments

COMMENT ON COLUMN public."ReferralTracking"."Status" IS '0=Clicked, 1=Registered, 2=Validated, 3=Rewarded';

-- Permissions

ALTER TABLE public."ReferralTracking" OWNER TO postgres;
GRANT ALL ON TABLE public."ReferralTracking" TO postgres;


-- public."Tickets" definition

-- Drop table

-- DROP TABLE public."Tickets";

CREATE TABLE public."Tickets" (
	"Id" serial4 NOT NULL,
	"UserId" int4 NOT NULL,
	"UserRole" varchar(20) NOT NULL,
	"Subject" varchar(200) NOT NULL,
	"Description" varchar(2000) NOT NULL,
	"Category" varchar(50) NOT NULL,
	"Priority" varchar(20) NOT NULL,
	"Status" varchar(20) NOT NULL,
	"AssignedToUserId" int4 NULL,
	"ResolvedDate" timestamp NULL,
	"ClosedDate" timestamp NULL,
	"ResolutionNotes" varchar(1000) NULL,
	"SatisfactionRating" int4 NULL,
	"SatisfactionFeedback" varchar(500) NULL,
	"CreatedDate" timestamp NOT NULL,
	"UpdatedDate" timestamp NOT NULL,
	"LastResponseDate" timestamp NULL,
	CONSTRAINT "Tickets_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_Tickets_Users_AssignedToUserId" FOREIGN KEY ("AssignedToUserId") REFERENCES public."Users"("UserId") ON DELETE SET NULL,
	CONSTRAINT "FK_Tickets_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("UserId") ON DELETE RESTRICT
);
CREATE INDEX "IX_Tickets_Category" ON public."Tickets" USING btree ("Category");
CREATE INDEX "IX_Tickets_CreatedDate" ON public."Tickets" USING btree ("CreatedDate");
CREATE INDEX "IX_Tickets_Priority" ON public."Tickets" USING btree ("Priority");
CREATE INDEX "IX_Tickets_Status" ON public."Tickets" USING btree ("Status");
CREATE INDEX "IX_Tickets_UserId" ON public."Tickets" USING btree ("UserId");

-- Permissions

ALTER TABLE public."Tickets" OWNER TO postgres;
GRANT ALL ON TABLE public."Tickets" TO postgres;


-- public."UserRoles" definition

-- Drop table

-- DROP TABLE public."UserRoles";

CREATE TABLE public."UserRoles" (
	"Id" serial4 NOT NULL,
	"UserId" int4 NOT NULL,
	"RoleId" int4 NOT NULL,
	"Status" bool DEFAULT true NOT NULL,
	"CreatedDate" timestamp DEFAULT now() NOT NULL,
	"UpdatedDate" timestamp NULL,
	CONSTRAINT "UserRoles_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_UserRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES public."Roles"("Id") ON DELETE CASCADE,
	CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("UserId") ON DELETE CASCADE
);

-- Permissions

ALTER TABLE public."UserRoles" OWNER TO postgres;
GRANT ALL ON TABLE public."UserRoles" TO postgres;


-- public."TicketMessages" definition

-- Drop table

-- DROP TABLE public."TicketMessages";

CREATE TABLE public."TicketMessages" (
	"Id" serial4 NOT NULL,
	"TicketId" int4 NOT NULL,
	"FromUserId" int4 NOT NULL,
	"Message" varchar(2000) NOT NULL,
	"IsAdminResponse" bool DEFAULT false NOT NULL,
	"IsInternal" bool DEFAULT false NOT NULL,
	"IsRead" bool DEFAULT false NOT NULL,
	"ReadDate" timestamp NULL,
	"CreatedDate" timestamp NOT NULL,
	CONSTRAINT "TicketMessages_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_TicketMessages_Tickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES public."Tickets"("Id") ON DELETE CASCADE,
	CONSTRAINT "FK_TicketMessages_Users_FromUserId" FOREIGN KEY ("FromUserId") REFERENCES public."Users"("UserId") ON DELETE RESTRICT
);
CREATE INDEX "IX_TicketMessages_CreatedDate" ON public."TicketMessages" USING btree ("CreatedDate");
CREATE INDEX "IX_TicketMessages_FromUserId" ON public."TicketMessages" USING btree ("FromUserId");
CREATE INDEX "IX_TicketMessages_TicketId" ON public."TicketMessages" USING btree ("TicketId");

-- Permissions

ALTER TABLE public."TicketMessages" OWNER TO postgres;
GRANT ALL ON TABLE public."TicketMessages" TO postgres;


-- public."DealerInvitations" definition

-- Drop table

-- DROP TABLE public."DealerInvitations";

CREATE TABLE public."DealerInvitations" (
	"Id" serial4 NOT NULL,
	"SponsorId" int4 NOT NULL,
	"Email" varchar(255) NULL,
	"Phone" varchar(20) NULL,
	"DealerName" varchar(255) NOT NULL,
	"Status" varchar(50) DEFAULT 'Pending'::character varying NOT NULL, -- Pending: Waiting for acceptance, Accepted: Dealer linked, Expired: Token expired, Cancelled: Sponsor cancelled
	"InvitationType" varchar(50) NOT NULL, -- Invite: Email invitation with link, AutoCreate: Automatic account creation
	"InvitationToken" varchar(255) NULL,
	"PurchaseId" int4 NULL, -- [DEPRECATED] Purchase ID - will be removed in future. Use PackageTier instead for filtering.
	"CodeCount" int4 NOT NULL,
	"CreatedDealerId" int4 NULL,
	"AcceptedDate" timestamp NULL,
	"AutoCreatedPassword" varchar(255) NULL,
	"CreatedDate" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"ExpiryDate" timestamp NOT NULL,
	"CancelledDate" timestamp NULL,
	"CancelledByUserId" int4 NULL,
	"Notes" text NULL,
	"LinkSentDate" timestamp NULL, -- When the SMS/email link was sent to the dealer
	"LinkSentVia" varchar(50) NULL, -- Communication channel: SMS, WhatsApp, Email, etc.
	"LinkDelivered" bool DEFAULT false NOT NULL, -- Whether the message was successfully delivered
	"PackageTier" varchar(10) NULL, -- Optional tier filter for code selection: S, M, L, XL. If null, codes from any tier can be selected automatically.
	CONSTRAINT "CHK_DealerInvitations_CodeCount" CHECK (("CodeCount" > 0)),
	CONSTRAINT "CHK_DealerInvitations_Contact" CHECK ((("Email" IS NOT NULL) OR ("Phone" IS NOT NULL))),
	CONSTRAINT "CHK_DealerInvitations_Status" CHECK ((("Status")::text = ANY ((ARRAY['Pending'::character varying, 'Accepted'::character varying, 'Expired'::character varying, 'Cancelled'::character varying])::text[]))),
	CONSTRAINT "CHK_DealerInvitations_Type" CHECK ((("InvitationType")::text = ANY ((ARRAY['Invite'::character varying, 'AutoCreate'::character varying])::text[]))),
	CONSTRAINT "DealerInvitations_InvitationToken_key" UNIQUE ("InvitationToken"),
	CONSTRAINT "DealerInvitations_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_DealerInvitations_CreatedDealer" ON public."DealerInvitations" USING btree ("CreatedDealerId");
CREATE INDEX "IX_DealerInvitations_Email" ON public."DealerInvitations" USING btree ("Email");
CREATE INDEX "IX_DealerInvitations_ExpiryDate" ON public."DealerInvitations" USING btree ("ExpiryDate");
CREATE INDEX "IX_DealerInvitations_LinkSentDate" ON public."DealerInvitations" USING btree ("LinkSentDate");
CREATE INDEX "IX_DealerInvitations_Phone" ON public."DealerInvitations" USING btree ("Phone");
CREATE INDEX "IX_DealerInvitations_SponsorId" ON public."DealerInvitations" USING btree ("SponsorId");
CREATE INDEX "IX_DealerInvitations_Status" ON public."DealerInvitations" USING btree ("Status");
CREATE INDEX "IX_DealerInvitations_Token" ON public."DealerInvitations" USING btree ("InvitationToken");
COMMENT ON TABLE public."DealerInvitations" IS 'Tracks dealer invitations and auto-created dealer profiles for code distribution';

-- Column comments

COMMENT ON COLUMN public."DealerInvitations"."Status" IS 'Pending: Waiting for acceptance, Accepted: Dealer linked, Expired: Token expired, Cancelled: Sponsor cancelled';
COMMENT ON COLUMN public."DealerInvitations"."InvitationType" IS 'Invite: Email invitation with link, AutoCreate: Automatic account creation';
COMMENT ON COLUMN public."DealerInvitations"."PurchaseId" IS '[DEPRECATED] Purchase ID - will be removed in future. Use PackageTier instead for filtering.';
COMMENT ON COLUMN public."DealerInvitations"."LinkSentDate" IS 'When the SMS/email link was sent to the dealer';
COMMENT ON COLUMN public."DealerInvitations"."LinkSentVia" IS 'Communication channel: SMS, WhatsApp, Email, etc.';
COMMENT ON COLUMN public."DealerInvitations"."LinkDelivered" IS 'Whether the message was successfully delivered';
COMMENT ON COLUMN public."DealerInvitations"."PackageTier" IS 'Optional tier filter for code selection: S, M, L, XL. If null, codes from any tier can be selected automatically.';

-- Permissions

ALTER TABLE public."DealerInvitations" OWNER TO postgres;
GRANT ALL ON TABLE public."DealerInvitations" TO postgres;


-- public."PaymentTransactions" definition

-- Drop table

-- DROP TABLE public."PaymentTransactions";

CREATE TABLE public."PaymentTransactions" (
	"Id" serial4 NOT NULL,
	"UserId" int4 NOT NULL,
	"FlowType" varchar(50) NOT NULL, -- Type of payment flow: SponsorBulkPurchase or FarmerSubscription
	"FlowDataJson" text NOT NULL, -- JSON containing flow-specific data (tier, quantity, etc.)
	"SponsorshipPurchaseId" int4 NULL,
	"UserSubscriptionId" int4 NULL,
	"IyzicoToken" varchar(255) NOT NULL, -- Unique token from iyzico initialize response, used to complete payment
	"IyzicoPaymentId" varchar(255) NULL,
	"ConversationId" varchar(100) NOT NULL, -- Unique identifier for tracking payment across systems
	"Amount" numeric(18, 2) NOT NULL,
	"Currency" varchar(3) DEFAULT 'TRY'::character varying NOT NULL,
	"Status" varchar(50) NOT NULL, -- Payment status: Initialized, Pending, Success, Failed, Expired
	"InitializedAt" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"CompletedAt" timestamp NULL,
	"ExpiresAt" timestamp NOT NULL, -- Token expiration time (typically 30 minutes from initialization)
	"InitializeResponse" text NULL,
	"VerifyResponse" text NULL,
	"ErrorMessage" text NULL,
	"CreatedDate" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"UpdatedDate" timestamp NULL,
	"Platform" varchar(20) DEFAULT 'iOS'::character varying NOT NULL,
	CONSTRAINT "CK_PaymentTransactions_Platform" CHECK ((("Platform")::text = ANY ((ARRAY['iOS'::character varying, 'Android'::character varying, 'Web'::character varying])::text[]))),
	CONSTRAINT "PaymentTransactions_ConversationId_key" UNIQUE ("ConversationId"),
	CONSTRAINT "PaymentTransactions_IyzicoToken_key" UNIQUE ("IyzicoToken"),
	CONSTRAINT "PaymentTransactions_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IDX_PaymentTransactions_ConversationId" ON public."PaymentTransactions" USING btree ("ConversationId");
CREATE INDEX "IDX_PaymentTransactions_CreatedDate" ON public."PaymentTransactions" USING btree ("CreatedDate");
CREATE INDEX "IDX_PaymentTransactions_FlowType" ON public."PaymentTransactions" USING btree ("FlowType");
CREATE INDEX "IDX_PaymentTransactions_IyzicoToken" ON public."PaymentTransactions" USING btree ("IyzicoToken");
CREATE INDEX "IDX_PaymentTransactions_Status" ON public."PaymentTransactions" USING btree ("Status");
CREATE INDEX "IDX_PaymentTransactions_UserId" ON public."PaymentTransactions" USING btree ("UserId");
CREATE INDEX "IX_PaymentTransactions_Platform" ON public."PaymentTransactions" USING btree ("Platform");
COMMENT ON TABLE public."PaymentTransactions" IS 'Stores all payment transactions for both sponsor bulk purchases and farmer subscriptions';

-- Column comments

COMMENT ON COLUMN public."PaymentTransactions"."FlowType" IS 'Type of payment flow: SponsorBulkPurchase or FarmerSubscription';
COMMENT ON COLUMN public."PaymentTransactions"."FlowDataJson" IS 'JSON containing flow-specific data (tier, quantity, etc.)';
COMMENT ON COLUMN public."PaymentTransactions"."IyzicoToken" IS 'Unique token from iyzico initialize response, used to complete payment';
COMMENT ON COLUMN public."PaymentTransactions"."ConversationId" IS 'Unique identifier for tracking payment across systems';
COMMENT ON COLUMN public."PaymentTransactions"."Status" IS 'Payment status: Initialized, Pending, Success, Failed, Expired';
COMMENT ON COLUMN public."PaymentTransactions"."ExpiresAt" IS 'Token expiration time (typically 30 minutes from initialization)';

-- Permissions

ALTER TABLE public."PaymentTransactions" OWNER TO postgres;
GRANT ALL ON TABLE public."PaymentTransactions" TO postgres;


-- public."PlantAnalyses" definition

-- Drop table

-- DROP TABLE public."PlantAnalyses";

CREATE TABLE public."PlantAnalyses" (
	"Id" serial4 NOT NULL,
	"AnalysisDate" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"AnalysisStatus" varchar(50) DEFAULT 'pending'::character varying NOT NULL,
	"Status" bool DEFAULT true NOT NULL,
	"CreatedDate" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"UpdatedDate" timestamp NULL,
	"AnalysisId" varchar(255) NOT NULL,
	"Timestamp" timestamp NOT NULL,
	"UserId" int4 NULL,
	"FarmerId" varchar(50) NULL,
	"SponsorId" varchar(50) NULL,
	"SponsorshipCodeId" int4 NULL,
	"SponsorUserId" int4 NULL,
	"Location" varchar(255) NULL,
	"GpsCoordinates" jsonb NULL,
	"Altitude" int4 NULL,
	"FieldId" varchar(100) NULL,
	"CropType" varchar(100) NULL,
	"PlantingDate" timestamp NULL,
	"ExpectedHarvestDate" timestamp NULL,
	"LastFertilization" timestamp NULL,
	"LastIrrigation" timestamp NULL,
	"PreviousTreatments" jsonb NULL,
	"WeatherConditions" varchar(100) NULL,
	"Temperature" numeric(5, 2) NULL,
	"Humidity" numeric(5, 2) NULL,
	"SoilType" varchar(100) NULL,
	"UrgencyLevel" varchar(50) NULL,
	"Notes" text NULL,
	"ContactInfo" text NULL,
	"AdditionalInfo" jsonb NULL,
	"PlantIdentification" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"PlantSpecies" text NULL,
	"PlantVariety" varchar(100) NULL,
	"GrowthStage" varchar(100) NULL,
	"IdentificationConfidence" int4 NULL,
	"HealthAssessment" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"VigorScore" int4 NULL,
	"HealthSeverity" varchar(50) NULL,
	"NutrientStatus" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"Nitrogen" varchar(50) NULL,
	"Phosphorus" varchar(50) NULL,
	"Potassium" varchar(50) NULL,
	"Calcium" varchar(50) NULL,
	"Magnesium" varchar(50) NULL,
	"Sulfur" varchar(50) NULL,
	"Iron" varchar(50) NULL,
	"Zinc" varchar(50) NULL,
	"Manganese" varchar(50) NULL,
	"Boron" varchar(50) NULL,
	"Copper" varchar(50) NULL,
	"Molybdenum" varchar(50) NULL,
	"Chlorine" varchar(50) NULL,
	"Nickel" varchar(50) NULL,
	"PrimaryDeficiency" text NULL,
	"NutrientSeverity" varchar(50) NULL,
	"PestDisease" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"AffectedAreaPercentage" int4 NULL,
	"SpreadRisk" varchar(50) NULL,
	"PrimaryIssue" text NULL,
	"EnvironmentalStress" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"PrimaryStressor" text NULL,
	"CrossFactorInsights" jsonb NULL,
	"RiskAssessment" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"Recommendations" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"Summary" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"OverallHealthScore" int4 DEFAULT 0 NOT NULL,
	"PrimaryConcern" text NULL,
	"CriticalIssuesCount" int4 NULL,
	"ConfidenceLevel" int4 NULL,
	"Prognosis" varchar(50) NULL,
	"EstimatedYieldImpact" varchar(50) NULL,
	"ConfidenceNotes" jsonb NULL,
	"FarmerFriendlySummary" text DEFAULT ''::text NOT NULL,
	"ImageMetadata" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"ImageUrl" text DEFAULT ''::text NOT NULL,
	"TokenUsage" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"ProcessingMetadata" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"AiModel" varchar(100) DEFAULT ''::character varying NOT NULL,
	"WorkflowVersion" varchar(50) DEFAULT ''::character varying NOT NULL,
	"TotalTokens" int4 DEFAULT 0 NOT NULL,
	"TotalCostUsd" numeric(10, 6) DEFAULT 0 NOT NULL,
	"TotalCostTry" numeric(10, 4) DEFAULT 0 NOT NULL,
	"ProcessingTimestamp" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"DetailedAnalysisData" jsonb DEFAULT '{}'::jsonb NOT NULL,
	"RequestMetadata" jsonb DEFAULT '{}'::jsonb NULL,
	"ActiveSponsorshipId" int4 NULL, -- FK to UserSubscription that was active during analysis (immutable)
	"SponsorCompanyId" int4 NULL, -- Denormalized sponsor company ID for fast logo/access queries
	"CreatedByAdminId" int4 NULL, -- Admin user ID who created this analysis on behalf of the farmer (null if farmer created directly)
	"IsOnBehalfOf" bool DEFAULT false NOT NULL, -- True when analysis was created by admin on behalf of farmer (for customer support scenarios)
	"DealerId" int4 NULL,
	"LeafTopUrl" text NULL, -- URL for top view of leaf image (optional, for detailed leaf analysis)
	"LeafBottomUrl" text NULL, -- URL for bottom view of leaf image (optional, for stomata and underside inspection)
	"PlantOverviewUrl" text NULL, -- URL for full plant overview image (optional, for structural and growth assessment)
	"RootUrl" text NULL, -- URL for root system image (optional, for root health analysis)
	CONSTRAINT "CHK_PlantAnalyses_OBO_Consistency" CHECK (((("IsOnBehalfOf" = false) AND ("CreatedByAdminId" IS NULL)) OR (("IsOnBehalfOf" = true) AND ("CreatedByAdminId" IS NOT NULL)))),
	CONSTRAINT "PlantAnalyses_AnalysisId_key" UNIQUE ("AnalysisId"),
	CONSTRAINT "PlantAnalyses_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IDX_PlantAnalyses_AnalysisDate" ON public."PlantAnalyses" USING btree ("AnalysisDate");
CREATE INDEX "IDX_PlantAnalyses_AnalysisId" ON public."PlantAnalyses" USING btree ("AnalysisId");
CREATE INDEX "IDX_PlantAnalyses_AnalysisStatus" ON public."PlantAnalyses" USING btree ("AnalysisStatus");
CREATE INDEX "IDX_PlantAnalyses_CropType" ON public."PlantAnalyses" USING btree ("CropType");
CREATE INDEX "IDX_PlantAnalyses_DetailedAnalysisData_GIN" ON public."PlantAnalyses" USING gin ("DetailedAnalysisData");
CREATE INDEX "IDX_PlantAnalyses_FarmerId" ON public."PlantAnalyses" USING btree ("FarmerId");
CREATE INDEX "IDX_PlantAnalyses_HealthAssessment_GIN" ON public."PlantAnalyses" USING gin ("HealthAssessment");
CREATE INDEX "IDX_PlantAnalyses_LeafBottomUrl" ON public."PlantAnalyses" USING btree ("LeafBottomUrl") WHERE ("LeafBottomUrl" IS NOT NULL);
CREATE INDEX "IDX_PlantAnalyses_LeafTopUrl" ON public."PlantAnalyses" USING btree ("LeafTopUrl") WHERE ("LeafTopUrl" IS NOT NULL);
CREATE INDEX "IDX_PlantAnalyses_Location" ON public."PlantAnalyses" USING btree ("Location");
CREATE INDEX "IDX_PlantAnalyses_NutrientStatus_GIN" ON public."PlantAnalyses" USING gin ("NutrientStatus");
CREATE INDEX "IDX_PlantAnalyses_OverallHealthScore" ON public."PlantAnalyses" USING btree ("OverallHealthScore");
CREATE INDEX "IDX_PlantAnalyses_PestDisease_GIN" ON public."PlantAnalyses" USING gin ("PestDisease");
CREATE INDEX "IDX_PlantAnalyses_PlantIdentification_GIN" ON public."PlantAnalyses" USING gin ("PlantIdentification");
CREATE INDEX "IDX_PlantAnalyses_PlantOverviewUrl" ON public."PlantAnalyses" USING btree ("PlantOverviewUrl") WHERE ("PlantOverviewUrl" IS NOT NULL);
CREATE INDEX "IDX_PlantAnalyses_ProcessingTimestamp" ON public."PlantAnalyses" USING btree ("ProcessingTimestamp");
CREATE INDEX "IDX_PlantAnalyses_Recommendations_GIN" ON public."PlantAnalyses" USING gin ("Recommendations");
CREATE INDEX "IDX_PlantAnalyses_RootUrl" ON public."PlantAnalyses" USING btree ("RootUrl") WHERE ("RootUrl" IS NOT NULL);
CREATE INDEX "IDX_PlantAnalyses_Timestamp" ON public."PlantAnalyses" USING btree ("Timestamp");
CREATE INDEX "IDX_PlantAnalyses_UserId" ON public."PlantAnalyses" USING btree ("UserId");
CREATE INDEX "IX_PlantAnalyses_ActiveSponsorship" ON public."PlantAnalyses" USING btree ("ActiveSponsorshipId") WHERE ("ActiveSponsorshipId" IS NOT NULL);
CREATE INDEX "IX_PlantAnalyses_CreatedByAdminId" ON public."PlantAnalyses" USING btree ("CreatedByAdminId") WHERE ("CreatedByAdminId" IS NOT NULL);
CREATE INDEX "IX_PlantAnalyses_CreatedByAdminId_CreatedDate" ON public."PlantAnalyses" USING btree ("CreatedByAdminId", "CreatedDate" DESC) WHERE ("CreatedByAdminId" IS NOT NULL);
CREATE INDEX "IX_PlantAnalyses_DealerId" ON public."PlantAnalyses" USING btree ("DealerId");
CREATE INDEX "IX_PlantAnalyses_IsOnBehalfOf" ON public."PlantAnalyses" USING btree ("IsOnBehalfOf") WHERE ("IsOnBehalfOf" = true);
CREATE INDEX "IX_PlantAnalyses_SponsorCompany" ON public."PlantAnalyses" USING btree ("SponsorCompanyId") WHERE ("SponsorCompanyId" IS NOT NULL);
CREATE INDEX "IX_PlantAnalyses_SponsorId_DealerId" ON public."PlantAnalyses" USING btree ("SponsorId", "DealerId");
CREATE INDEX "IX_PlantAnalyses_UserId_IsOnBehalfOf" ON public."PlantAnalyses" USING btree ("UserId", "IsOnBehalfOf", "CreatedDate" DESC) WHERE ("IsOnBehalfOf" = true);
CREATE INDEX "IX_PlantAnalyses_UserSponsor" ON public."PlantAnalyses" USING btree ("UserId", "SponsorCompanyId") WHERE ("SponsorCompanyId" IS NOT NULL);

-- Column comments

COMMENT ON COLUMN public."PlantAnalyses"."ActiveSponsorshipId" IS 'FK to UserSubscription that was active during analysis (immutable)';
COMMENT ON COLUMN public."PlantAnalyses"."SponsorCompanyId" IS 'Denormalized sponsor company ID for fast logo/access queries';
COMMENT ON COLUMN public."PlantAnalyses"."CreatedByAdminId" IS 'Admin user ID who created this analysis on behalf of the farmer (null if farmer created directly)';
COMMENT ON COLUMN public."PlantAnalyses"."IsOnBehalfOf" IS 'True when analysis was created by admin on behalf of farmer (for customer support scenarios)';
COMMENT ON COLUMN public."PlantAnalyses"."LeafTopUrl" IS 'URL for top view of leaf image (optional, for detailed leaf analysis)';
COMMENT ON COLUMN public."PlantAnalyses"."LeafBottomUrl" IS 'URL for bottom view of leaf image (optional, for stomata and underside inspection)';
COMMENT ON COLUMN public."PlantAnalyses"."PlantOverviewUrl" IS 'URL for full plant overview image (optional, for structural and growth assessment)';
COMMENT ON COLUMN public."PlantAnalyses"."RootUrl" IS 'URL for root system image (optional, for root health analysis)';

-- Constraint comments

COMMENT ON CONSTRAINT "CHK_PlantAnalyses_OBO_Consistency" ON public."PlantAnalyses" IS 'Ensures data consistency: if IsOnBehalfOf is TRUE, CreatedByAdminId must be set';

-- Permissions

ALTER TABLE public."PlantAnalyses" OWNER TO postgres;
GRANT ALL ON TABLE public."PlantAnalyses" TO postgres;


-- public."ReferralRewards" definition

-- Drop table

-- DROP TABLE public."ReferralRewards";

CREATE TABLE public."ReferralRewards" (
	"Id" serial4 NOT NULL,
	"ReferralTrackingId" int4 NOT NULL,
	"ReferrerUserId" int4 NOT NULL,
	"RefereeUserId" int4 NOT NULL,
	"CreditAmount" int4 NOT NULL, -- Number of analysis credits awarded (configurable, default: 10)
	"AwardedAt" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"SubscriptionId" int4 NULL,
	"ExpiresAt" timestamp NULL,
	CONSTRAINT "ReferralRewards_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_ReferralRewards_AwardedAt" ON public."ReferralRewards" USING btree ("AwardedAt");
CREATE INDEX "IX_ReferralRewards_RefereeUserId" ON public."ReferralRewards" USING btree ("RefereeUserId");
CREATE INDEX "IX_ReferralRewards_ReferralTrackingId" ON public."ReferralRewards" USING btree ("ReferralTrackingId");
CREATE INDEX "IX_ReferralRewards_ReferrerUserId" ON public."ReferralRewards" USING btree ("ReferrerUserId");
COMMENT ON TABLE public."ReferralRewards" IS 'Records of awarded referral credits';

-- Column comments

COMMENT ON COLUMN public."ReferralRewards"."CreditAmount" IS 'Number of analysis credits awarded (configurable, default: 10)';

-- Permissions

ALTER TABLE public."ReferralRewards" OWNER TO postgres;
GRANT ALL ON TABLE public."ReferralRewards" TO postgres;


-- public."SponsorAnalysisAccess" definition

-- Drop table

-- DROP TABLE public."SponsorAnalysisAccess";

CREATE TABLE public."SponsorAnalysisAccess" (
	"Id" serial4 NOT NULL,
	"SponsorId" int4 NOT NULL,
	"PlantAnalysisId" int4 NOT NULL,
	"FarmerId" int4 NOT NULL,
	"AccessLevel" varchar(50) NOT NULL, -- Access level based on sponsor tier: Basic30, Extended60, Full100
	"AccessPercentage" int4 DEFAULT 30 NOT NULL, -- Percentage of data accessible: 30, 60, or 100
	"FirstViewedDate" timestamp DEFAULT now() NOT NULL,
	"LastViewedDate" timestamp NULL,
	"ViewCount" int4 DEFAULT 0 NOT NULL, -- Number of times sponsor has viewed this analysis
	"DownloadedDate" timestamp NULL,
	"HasDownloaded" bool DEFAULT false NOT NULL,
	"AccessedFields" text NULL, -- JSON array of field names sponsor can access
	"RestrictedFields" text NULL, -- JSON array of field names sponsor cannot access
	"CanViewHealthScore" bool DEFAULT false NOT NULL,
	"CanViewDiseases" bool DEFAULT false NOT NULL,
	"CanViewPests" bool DEFAULT false NOT NULL,
	"CanViewNutrients" bool DEFAULT false NOT NULL,
	"CanViewRecommendations" bool DEFAULT false NOT NULL,
	"CanViewFarmerContact" bool DEFAULT false NOT NULL,
	"CanViewLocation" bool DEFAULT false NOT NULL,
	"CanViewImages" bool DEFAULT false NOT NULL,
	"HasContactedFarmer" bool DEFAULT false NOT NULL,
	"ContactDate" timestamp NULL,
	"ContactMethod" varchar(50) NULL,
	"Notes" varchar(1000) NULL,
	"SponsorshipCodeId" int4 NULL,
	"SponsorshipPurchaseId" int4 NULL,
	"CreatedDate" timestamp DEFAULT now() NOT NULL,
	"UpdatedDate" timestamp NULL,
	"IpAddress" varchar(50) NULL,
	"UserAgent" varchar(500) NULL,
	CONSTRAINT "SponsorAnalysisAccess_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_SponsorAnalysisAccess_AccessLevel" ON public."SponsorAnalysisAccess" USING btree ("AccessLevel");
CREATE INDEX "IX_SponsorAnalysisAccess_FarmerId" ON public."SponsorAnalysisAccess" USING btree ("FarmerId");
CREATE INDEX "IX_SponsorAnalysisAccess_FirstViewedDate" ON public."SponsorAnalysisAccess" USING btree ("FirstViewedDate");
CREATE INDEX "IX_SponsorAnalysisAccess_PlantAnalysisId" ON public."SponsorAnalysisAccess" USING btree ("PlantAnalysisId");
CREATE INDEX "IX_SponsorAnalysisAccess_SponsorId" ON public."SponsorAnalysisAccess" USING btree ("SponsorId");
CREATE UNIQUE INDEX "IX_SponsorAnalysisAccess_SponsorId_PlantAnalysisId" ON public."SponsorAnalysisAccess" USING btree ("SponsorId", "PlantAnalysisId");
COMMENT ON TABLE public."SponsorAnalysisAccess" IS 'Tracks sponsor access to plant analyses for tier-based data filtering and messaging validation';

-- Column comments

COMMENT ON COLUMN public."SponsorAnalysisAccess"."AccessLevel" IS 'Access level based on sponsor tier: Basic30, Extended60, Full100';
COMMENT ON COLUMN public."SponsorAnalysisAccess"."AccessPercentage" IS 'Percentage of data accessible: 30, 60, or 100';
COMMENT ON COLUMN public."SponsorAnalysisAccess"."ViewCount" IS 'Number of times sponsor has viewed this analysis';
COMMENT ON COLUMN public."SponsorAnalysisAccess"."AccessedFields" IS 'JSON array of field names sponsor can access';
COMMENT ON COLUMN public."SponsorAnalysisAccess"."RestrictedFields" IS 'JSON array of field names sponsor cannot access';

-- Permissions

ALTER TABLE public."SponsorAnalysisAccess" OWNER TO postgres;
GRANT ALL ON TABLE public."SponsorAnalysisAccess" TO postgres;


-- public."SponsorshipCodes" definition

-- Drop table

-- DROP TABLE public."SponsorshipCodes";

CREATE TABLE public."SponsorshipCodes" (
	"Id" int4 GENERATED BY DEFAULT AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"Code" varchar(50) NOT NULL,
	"SponsorId" int4 NOT NULL,
	"SubscriptionTierId" int4 NOT NULL,
	"SponsorshipPurchaseId" int4 NOT NULL,
	"IsUsed" bool DEFAULT false NOT NULL,
	"UsedByUserId" int4 NULL,
	"UsedDate" timestamp NULL,
	"CreatedSubscriptionId" int4 NULL,
	"CreatedDate" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"ExpiryDate" timestamp NOT NULL,
	"IsActive" bool DEFAULT true NOT NULL,
	"Notes" varchar(500) NULL,
	"DistributedTo" varchar(200) NULL,
	"DistributionChannel" varchar(50) NULL,
	"DistributionDate" timestamp NULL,
	"LastClickIpAddress" text NULL,
	"LinkClickCount" int4 DEFAULT 0 NOT NULL,
	"LinkClickDate" timestamp NULL,
	"LinkDelivered" bool DEFAULT false NOT NULL,
	"LinkSentDate" timestamp NULL,
	"LinkSentVia" text NULL,
	"RecipientName" text NULL,
	"RecipientPhone" text NULL,
	"RedemptionLink" text NULL,
	"DealerId" int4 NULL,
	"TransferredAt" timestamp NULL,
	"TransferredByUserId" int4 NULL,
	"ReclaimedAt" timestamp NULL,
	"ReclaimedByUserId" int4 NULL,
	"ReservedForInvitationId" int4 NULL, -- Invitation ID for which this code is reserved. Prevents double-allocation during pending invitations.
	"ReservedAt" timestamp NULL, -- Timestamp when the code was reserved for an invitation. Reservation expires with invitation.
	CONSTRAINT "PK_SponsorshipCodes" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_SponsorshipCodes_Code" ON public."SponsorshipCodes" USING btree ("Code");
CREATE INDEX "IX_SponsorshipCodes_DealerId" ON public."SponsorshipCodes" USING btree ("DealerId");
CREATE INDEX "IX_SponsorshipCodes_IntelligentSelection" ON public."SponsorshipCodes" USING btree ("SponsorId", "IsUsed", "DealerId", "ReservedForInvitationId", "ExpiryDate", "CreatedDate") WHERE (("IsUsed" = false) AND ("DealerId" IS NULL) AND ("ReservedForInvitationId" IS NULL));
COMMENT ON INDEX public."IX_SponsorshipCodes_IntelligentSelection" IS 'Optimizes intelligent code selection queries with FIFO ordering. Partial index for available codes only.';
CREATE INDEX "IX_SponsorshipCodes_RecipientPhone_Inbox" ON public."SponsorshipCodes" USING btree ("RecipientPhone", "LinkDelivered", "IsUsed", "ExpiryDate") WHERE (("RecipientPhone" IS NOT NULL) AND ("LinkDelivered" = true));
COMMENT ON INDEX public."IX_SponsorshipCodes_RecipientPhone_Inbox" IS 'Optimizes farmer inbox queries by phone number. Partial index for delivered codes only.';
CREATE INDEX "IX_SponsorshipCodes_Reservation" ON public."SponsorshipCodes" USING btree ("ReservedForInvitationId") WHERE ("ReservedForInvitationId" IS NOT NULL);
COMMENT ON INDEX public."IX_SponsorshipCodes_Reservation" IS 'Optimizes reservation lookup during invitation acceptance. Partial index for reserved codes only.';
CREATE INDEX "IX_SponsorshipCodes_ReservedForInvitationId" ON public."SponsorshipCodes" USING btree ("ReservedForInvitationId") WHERE ("ReservedForInvitationId" IS NOT NULL);
CREATE INDEX "IX_SponsorshipCodes_SentExpired" ON public."SponsorshipCodes" USING btree ("SponsorId", "DistributionDate", "ExpiryDate", "IsUsed") WHERE (("DistributionDate" IS NOT NULL) AND ("IsUsed" = false));
CREATE INDEX "IX_SponsorshipCodes_SentUnused" ON public."SponsorshipCodes" USING btree ("SponsorId", "DistributionDate", "IsUsed") WHERE (("DistributionDate" IS NOT NULL) AND ("IsUsed" = false));
CREATE INDEX "IX_SponsorshipCodes_SponsorId_DealerId" ON public."SponsorshipCodes" USING btree ("SponsorId", "DealerId");
CREATE INDEX "IX_SponsorshipCodes_TierSelection" ON public."SponsorshipCodes" USING btree ("SubscriptionTierId", "IsUsed", "DealerId") WHERE (("IsUsed" = false) AND ("DealerId" IS NULL));
COMMENT ON INDEX public."IX_SponsorshipCodes_TierSelection" IS 'Optimizes tier-based code filtering. Partial index for available codes only.';
CREATE INDEX "IX_SponsorshipCodes_Unsent" ON public."SponsorshipCodes" USING btree ("SponsorId", "DistributionDate", "IsUsed") WHERE ("DistributionDate" IS NULL);
CREATE INDEX ix_sponsorshipcodes_dashboard_stats ON public."SponsorshipCodes" USING btree ("DealerId", "ReclaimedAt", "IsUsed", "DistributionDate") INCLUDE ("ExpiryDate", "IsActive") WHERE ("DealerId" IS NOT NULL);
CREATE INDEX ix_sponsorshipcodes_dealerid_distributiondate ON public."SponsorshipCodes" USING btree ("DealerId", "DistributionDate", "IsUsed", "ExpiryDate", "IsActive") WHERE (("DealerId" IS NOT NULL) AND ("ReclaimedAt" IS NULL));
CREATE INDEX ix_sponsorshipcodes_dealerid_reclaimedat ON public."SponsorshipCodes" USING btree ("DealerId", "ReclaimedAt") WHERE ("DealerId" IS NOT NULL);
CREATE INDEX ix_sponsorshipcodes_dealerid_transferredat ON public."SponsorshipCodes" USING btree ("DealerId", "TransferredAt" DESC) WHERE (("DealerId" IS NOT NULL) AND ("ReclaimedAt" IS NULL));

-- Column comments

COMMENT ON COLUMN public."SponsorshipCodes"."ReservedForInvitationId" IS 'Invitation ID for which this code is reserved. Prevents double-allocation during pending invitations.';
COMMENT ON COLUMN public."SponsorshipCodes"."ReservedAt" IS 'Timestamp when the code was reserved for an invitation. Reservation expires with invitation.';

-- Permissions

ALTER TABLE public."SponsorshipCodes" OWNER TO postgres;
GRANT ALL ON TABLE public."SponsorshipCodes" TO postgres;


-- public."SponsorshipPurchases" definition

-- Drop table

-- DROP TABLE public."SponsorshipPurchases";

CREATE TABLE public."SponsorshipPurchases" (
	"Id" int4 GENERATED BY DEFAULT AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"SponsorId" int4 NOT NULL,
	"SubscriptionTierId" int4 NOT NULL,
	"Quantity" int4 NOT NULL,
	"UnitPrice" numeric(18, 2) NOT NULL,
	"TotalAmount" numeric(18, 2) NOT NULL,
	"Currency" varchar(3) DEFAULT 'TRY'::character varying NULL,
	"PurchaseDate" timestamp NOT NULL,
	"PaymentMethod" varchar(50) NULL,
	"PaymentReference" varchar(200) NULL,
	"PaymentStatus" varchar(50) DEFAULT 'Pending'::character varying NULL,
	"PaymentCompletedDate" timestamp NULL,
	"InvoiceNumber" varchar(100) NULL,
	"InvoiceAddress" varchar(500) NULL,
	"TaxNumber" varchar(50) NULL,
	"CompanyName" varchar(200) NULL,
	"CodesGenerated" int4 NOT NULL,
	"CodesUsed" int4 NOT NULL,
	"CodePrefix" varchar(10) DEFAULT 'AGRI'::character varying NULL,
	"ValidityDays" int4 DEFAULT 30 NOT NULL, -- Number of days after purchase when generated codes expire (can be redeemed). Default changed from 365 to 30 days on 2025-10-12.
	"Status" varchar(50) DEFAULT 'Active'::character varying NULL,
	"Notes" varchar(1000) NULL,
	"PurchaseReason" varchar(500) NULL,
	"CreatedDate" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	"UpdatedDate" timestamp NULL,
	"ApprovedByUserId" int4 NULL,
	"ApprovalDate" timestamp NULL,
	"PaymentTransactionId" int4 NULL, -- Foreign key to PaymentTransactions table for payment tracking
	CONSTRAINT "PK_SponsorshipPurchases" PRIMARY KEY ("Id")
);
CREATE INDEX "IDX_SponsorshipPurchases_PaymentTransactionId" ON public."SponsorshipPurchases" USING btree ("PaymentTransactionId");

-- Column comments

COMMENT ON COLUMN public."SponsorshipPurchases"."ValidityDays" IS 'Number of days after purchase when generated codes expire (can be redeemed). Default changed from 365 to 30 days on 2025-10-12.';
COMMENT ON COLUMN public."SponsorshipPurchases"."PaymentTransactionId" IS 'Foreign key to PaymentTransactions table for payment tracking';

-- Permissions

ALTER TABLE public."SponsorshipPurchases" OWNER TO postgres;
GRANT ALL ON TABLE public."SponsorshipPurchases" TO postgres;


-- public."SubscriptionUsageLogs" definition

-- Drop table

-- DROP TABLE public."SubscriptionUsageLogs";

CREATE TABLE public."SubscriptionUsageLogs" (
	"Id" serial4 NOT NULL,
	"UserId" int4 NOT NULL,
	"UserSubscriptionId" int4 NOT NULL,
	"PlantAnalysisId" int4 NULL,
	"UsageType" varchar(50) NOT NULL,
	"UsageDate" timestamptz DEFAULT CURRENT_TIMESTAMP NULL,
	"RequestEndpoint" varchar(200) NULL,
	"RequestMethod" varchar(10) NULL,
	"IsSuccessful" bool DEFAULT true NULL,
	"ResponseStatus" varchar(50) NULL,
	"ErrorMessage" text NULL,
	"ResponseTimeMs" int8 NULL,
	"DailyQuotaUsed" int4 NULL,
	"DailyQuotaLimit" int4 NULL,
	"MonthlyQuotaUsed" int4 NULL,
	"MonthlyQuotaLimit" int4 NULL,
	"IpAddress" varchar(45) NULL,
	"UserAgent" text NULL,
	"CreatedDate" timestamp DEFAULT CURRENT_TIMESTAMP NULL,
	"RequestData" varchar(4000) NULL,
	CONSTRAINT "SubscriptionUsageLogs_pkey" PRIMARY KEY ("Id")
);

-- Permissions

ALTER TABLE public."SubscriptionUsageLogs" OWNER TO postgres;
GRANT ALL ON TABLE public."SubscriptionUsageLogs" TO postgres;


-- public."UserSubscriptions" definition

-- Drop table

-- DROP TABLE public."UserSubscriptions";

CREATE TABLE public."UserSubscriptions" (
	"Id" serial4 NOT NULL,
	"UserId" int4 NOT NULL,
	"SubscriptionTierId" int4 NOT NULL,
	"StartDate" timestamptz NOT NULL,
	"EndDate" timestamptz NOT NULL,
	"IsActive" bool DEFAULT true NULL,
	"AutoRenew" bool DEFAULT false NULL,
	"PaymentMethod" varchar(50) NULL,
	"PaymentReference" varchar(100) NULL,
	"PaidAmount" numeric(10, 2) NULL,
	"Currency" varchar(3) DEFAULT 'TRY'::character varying NULL,
	"LastPaymentDate" timestamptz NULL,
	"NextPaymentDate" timestamptz NULL,
	"CurrentDailyUsage" int4 DEFAULT 0 NULL,
	"CurrentMonthlyUsage" int4 DEFAULT 0 NULL,
	"LastUsageResetDate" timestamptz NULL,
	"MonthlyUsageResetDate" timestamptz NULL,
	"Status" varchar(20) DEFAULT 'Active'::character varying NULL,
	"CancellationReason" text NULL,
	"CancellationDate" timestamptz NULL,
	"IsTrialSubscription" bool DEFAULT false NULL,
	"TrialEndDate" timestamptz NULL,
	"CreatedDate" timestamptz DEFAULT CURRENT_TIMESTAMP NULL,
	"UpdatedDate" timestamptz NULL,
	"CreatedUserId" int4 NULL,
	"UpdatedUserId" int4 NULL,
	"IsSponsoredSubscription" bool DEFAULT false NOT NULL,
	"SponsorId" int4 NULL,
	"SponsorshipCodeId" int4 NULL,
	"SponsorshipNotes" text NULL,
	"ReferralCredits" int4 DEFAULT 0 NOT NULL, -- Analysis credits earned through referrals, separate from subscription quota
	"QueueStatus" int4 DEFAULT 1 NOT NULL, -- Queue status: 0=Pending, 1=Active, 2=Expired, 3=Cancelled
	"QueuedDate" timestamp NULL, -- When sponsorship code was redeemed (if queued)
	"ActivatedDate" timestamp NULL, -- When subscription actually became active
	"PreviousSponsorshipId" int4 NULL, -- FK to sponsorship this is waiting for (queue system)
	"PaymentTransactionId" int4 NULL, -- Foreign key to PaymentTransactions table - links subscription to payment transaction
	CONSTRAINT "UserSubscriptions_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_UserSubscriptions_PaymentTransactionId" ON public."UserSubscriptions" USING btree ("PaymentTransactionId");
CREATE INDEX "IX_UserSubscriptions_QueueStatus" ON public."UserSubscriptions" USING btree ("QueueStatus");
CREATE INDEX "IX_UserSubscriptions_Queue_Lookup" ON public."UserSubscriptions" USING btree ("QueueStatus", "PreviousSponsorshipId") WHERE ("PreviousSponsorshipId" IS NOT NULL);
CREATE INDEX "IX_UserSubscriptions_ReferralCredits" ON public."UserSubscriptions" USING btree ("ReferralCredits");
CREATE INDEX "IX_UserSubscriptions_Sponsored_Active" ON public."UserSubscriptions" USING btree ("UserId", "IsSponsoredSubscription", "QueueStatus", "IsActive") WHERE ("IsSponsoredSubscription" = true);

-- Column comments

COMMENT ON COLUMN public."UserSubscriptions"."ReferralCredits" IS 'Analysis credits earned through referrals, separate from subscription quota';
COMMENT ON COLUMN public."UserSubscriptions"."QueueStatus" IS 'Queue status: 0=Pending, 1=Active, 2=Expired, 3=Cancelled';
COMMENT ON COLUMN public."UserSubscriptions"."QueuedDate" IS 'When sponsorship code was redeemed (if queued)';
COMMENT ON COLUMN public."UserSubscriptions"."ActivatedDate" IS 'When subscription actually became active';
COMMENT ON COLUMN public."UserSubscriptions"."PreviousSponsorshipId" IS 'FK to sponsorship this is waiting for (queue system)';
COMMENT ON COLUMN public."UserSubscriptions"."PaymentTransactionId" IS 'Foreign key to PaymentTransactions table - links subscription to payment transaction';

-- Permissions

ALTER TABLE public."UserSubscriptions" OWNER TO postgres;
GRANT ALL ON TABLE public."UserSubscriptions" TO postgres;


-- public."DealerInvitations" foreign keys

ALTER TABLE public."DealerInvitations" ADD CONSTRAINT "FK_DealerInvitations_CancelledBy" FOREIGN KEY ("CancelledByUserId") REFERENCES public."Users"("UserId") ON DELETE SET NULL;
ALTER TABLE public."DealerInvitations" ADD CONSTRAINT "FK_DealerInvitations_CreatedDealer" FOREIGN KEY ("CreatedDealerId") REFERENCES public."Users"("UserId") ON DELETE SET NULL;
ALTER TABLE public."DealerInvitations" ADD CONSTRAINT "FK_DealerInvitations_Purchase" FOREIGN KEY ("PurchaseId") REFERENCES public."SponsorshipPurchases"("Id") ON DELETE CASCADE;
ALTER TABLE public."DealerInvitations" ADD CONSTRAINT "FK_DealerInvitations_Sponsor" FOREIGN KEY ("SponsorId") REFERENCES public."Users"("UserId") ON DELETE CASCADE;


-- public."PaymentTransactions" foreign keys

ALTER TABLE public."PaymentTransactions" ADD CONSTRAINT "IX_PaymentTransactions_SponsorshipPurchaseId" FOREIGN KEY ("SponsorshipPurchaseId") REFERENCES public."SponsorshipPurchases"("Id");
ALTER TABLE public."PaymentTransactions" ADD CONSTRAINT "IX_PaymentTransactions_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("UserId");


-- public."PlantAnalyses" foreign keys

ALTER TABLE public."PlantAnalyses" ADD CONSTRAINT "FK_PlantAnalyses_ActiveSponsorship" FOREIGN KEY ("ActiveSponsorshipId") REFERENCES public."UserSubscriptions"("Id") ON DELETE SET NULL;
ALTER TABLE public."PlantAnalyses" ADD CONSTRAINT "FK_PlantAnalyses_CreatedByAdmin" FOREIGN KEY ("CreatedByAdminId") REFERENCES public."Users"("UserId") ON DELETE SET NULL;
ALTER TABLE public."PlantAnalyses" ADD CONSTRAINT "FK_PlantAnalyses_Dealer" FOREIGN KEY ("DealerId") REFERENCES public."Users"("UserId");
ALTER TABLE public."PlantAnalyses" ADD CONSTRAINT "FK_PlantAnalyses_SponsorCompany" FOREIGN KEY ("SponsorCompanyId") REFERENCES public."Users"("UserId") ON DELETE SET NULL;
ALTER TABLE public."PlantAnalyses" ADD CONSTRAINT "FK_PlantAnalyses_SponsorUsers" FOREIGN KEY ("SponsorUserId") REFERENCES public."Users"("UserId") ON DELETE SET NULL;
ALTER TABLE public."PlantAnalyses" ADD CONSTRAINT "FK_PlantAnalyses_SponsorshipCodes" FOREIGN KEY ("SponsorshipCodeId") REFERENCES public."SponsorshipCodes"("Id") ON DELETE SET NULL;
ALTER TABLE public."PlantAnalyses" ADD CONSTRAINT "FK_PlantAnalyses_Users" FOREIGN KEY ("UserId") REFERENCES public."Users"("UserId") ON DELETE SET NULL;


-- public."ReferralRewards" foreign keys

ALTER TABLE public."ReferralRewards" ADD CONSTRAINT "FK_ReferralRewards_RefereeUsers" FOREIGN KEY ("RefereeUserId") REFERENCES public."Users"("UserId") ON DELETE CASCADE;
ALTER TABLE public."ReferralRewards" ADD CONSTRAINT "FK_ReferralRewards_ReferralTracking" FOREIGN KEY ("ReferralTrackingId") REFERENCES public."ReferralTracking"("Id") ON DELETE CASCADE;
ALTER TABLE public."ReferralRewards" ADD CONSTRAINT "FK_ReferralRewards_ReferrerUsers" FOREIGN KEY ("ReferrerUserId") REFERENCES public."Users"("UserId") ON DELETE CASCADE;
ALTER TABLE public."ReferralRewards" ADD CONSTRAINT "FK_ReferralRewards_Subscriptions" FOREIGN KEY ("SubscriptionId") REFERENCES public."UserSubscriptions"("Id") ON DELETE SET NULL;


-- public."SponsorAnalysisAccess" foreign keys

ALTER TABLE public."SponsorAnalysisAccess" ADD CONSTRAINT "FK_SponsorAnalysisAccess_Farmer" FOREIGN KEY ("FarmerId") REFERENCES public."Users"("UserId") ON DELETE RESTRICT;
ALTER TABLE public."SponsorAnalysisAccess" ADD CONSTRAINT "FK_SponsorAnalysisAccess_PlantAnalysis" FOREIGN KEY ("PlantAnalysisId") REFERENCES public."PlantAnalyses"("Id") ON DELETE CASCADE;
ALTER TABLE public."SponsorAnalysisAccess" ADD CONSTRAINT "FK_SponsorAnalysisAccess_Sponsor" FOREIGN KEY ("SponsorId") REFERENCES public."Users"("UserId") ON DELETE RESTRICT;
ALTER TABLE public."SponsorAnalysisAccess" ADD CONSTRAINT "FK_SponsorAnalysisAccess_SponsorshipCode" FOREIGN KEY ("SponsorshipCodeId") REFERENCES public."SponsorshipCodes"("Id") ON DELETE RESTRICT;
ALTER TABLE public."SponsorAnalysisAccess" ADD CONSTRAINT "FK_SponsorAnalysisAccess_SponsorshipPurchase" FOREIGN KEY ("SponsorshipPurchaseId") REFERENCES public."SponsorshipPurchases"("Id") ON DELETE RESTRICT;


-- public."SponsorshipCodes" foreign keys

ALTER TABLE public."SponsorshipCodes" ADD CONSTRAINT "FK_SponsorshipCodes_Dealer" FOREIGN KEY ("DealerId") REFERENCES public."Users"("UserId");
ALTER TABLE public."SponsorshipCodes" ADD CONSTRAINT "FK_SponsorshipCodes_ReclaimedBy" FOREIGN KEY ("ReclaimedByUserId") REFERENCES public."Users"("UserId");
ALTER TABLE public."SponsorshipCodes" ADD CONSTRAINT "FK_SponsorshipCodes_ReservedForInvitation" FOREIGN KEY ("ReservedForInvitationId") REFERENCES public."DealerInvitations"("Id") ON DELETE SET NULL;
ALTER TABLE public."SponsorshipCodes" ADD CONSTRAINT "FK_SponsorshipCodes_TransferredBy" FOREIGN KEY ("TransferredByUserId") REFERENCES public."Users"("UserId");


-- public."SponsorshipPurchases" foreign keys

ALTER TABLE public."SponsorshipPurchases" ADD CONSTRAINT "FK_SponsorshipPurchases_PaymentTransactions" FOREIGN KEY ("PaymentTransactionId") REFERENCES public."PaymentTransactions"("Id") ON DELETE SET NULL;


-- public."SubscriptionUsageLogs" foreign keys

ALTER TABLE public."SubscriptionUsageLogs" ADD CONSTRAINT "SubscriptionUsageLogs_PlantAnalysisId_fkey" FOREIGN KEY ("PlantAnalysisId") REFERENCES public."PlantAnalyses_yedek"("Id");
ALTER TABLE public."SubscriptionUsageLogs" ADD CONSTRAINT "SubscriptionUsageLogs_UserId_fkey" FOREIGN KEY ("UserId") REFERENCES public."Users"("UserId");
ALTER TABLE public."SubscriptionUsageLogs" ADD CONSTRAINT "SubscriptionUsageLogs_UserSubscriptionId_fkey" FOREIGN KEY ("UserSubscriptionId") REFERENCES public."UserSubscriptions"("Id");


-- public."UserSubscriptions" foreign keys

ALTER TABLE public."UserSubscriptions" ADD CONSTRAINT "FK_UserSubscriptions_PaymentTransactions" FOREIGN KEY ("PaymentTransactionId") REFERENCES public."PaymentTransactions"("Id") ON DELETE SET NULL;
ALTER TABLE public."UserSubscriptions" ADD CONSTRAINT "FK_UserSubscriptions_PreviousSponsorship" FOREIGN KEY ("PreviousSponsorshipId") REFERENCES public."UserSubscriptions"("Id") ON DELETE SET NULL;
ALTER TABLE public."UserSubscriptions" ADD CONSTRAINT "UserSubscriptions_SubscriptionTierId_fkey" FOREIGN KEY ("SubscriptionTierId") REFERENCES public."SubscriptionTiers"("Id");
ALTER TABLE public."UserSubscriptions" ADD CONSTRAINT "UserSubscriptions_UserId_fkey" FOREIGN KEY ("UserId") REFERENCES public."Users"("UserId");




-- Permissions

GRANT ALL ON SCHEMA public TO pg_database_owner;