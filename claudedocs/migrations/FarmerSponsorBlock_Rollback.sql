-- =====================================================
-- Rollback FarmerSponsorBlock Table Migration
-- Created: 2025-01-17
-- Purpose: Rollback farmer-sponsor blocking table
-- Branch: feature/sponsor-farmer-messaging
-- Database: PostgreSQL
-- =====================================================

-- Drop FarmerSponsorBlocks table and all related objects
DROP TABLE IF EXISTS "FarmerSponsorBlocks" CASCADE;
