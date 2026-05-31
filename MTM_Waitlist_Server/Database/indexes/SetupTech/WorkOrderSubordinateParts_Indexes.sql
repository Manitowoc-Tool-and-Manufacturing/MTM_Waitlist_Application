-- =============================================================
-- MTM Waitlist Application — WorkOrderSubordinateParts Indexes
-- Domain:      SetupTech
-- Description: Supports work-order validation and subordinate-part cache queries.
-- Depends on:  schema/tables/SetupTech/WorkOrderSubordinateParts.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP INDEX IF EXISTS `idx_WorkOrderSubordinateParts_CachedAt` ON `WorkOrderSubordinateParts`;
CREATE INDEX `idx_WorkOrderSubordinateParts_CachedAt` ON `WorkOrderSubordinateParts` (`CachedAt`);