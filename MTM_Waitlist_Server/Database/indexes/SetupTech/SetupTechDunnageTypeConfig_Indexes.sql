-- =============================================================
-- MTM Waitlist Application — SetupTechDunnageTypeConfig Indexes
-- Domain:      SetupTech
-- Description: Supports enabled-type UI queries ordered by DisplayOrder.
-- Depends on:  schema/tables/SetupTech/SetupTechDunnageTypeConfig.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP INDEX IF EXISTS `idx_SetupTechDunnageTypeConfig_IsEnabled_DisplayOrder` ON `SetupTechDunnageTypeConfig`;
CREATE INDEX `idx_SetupTechDunnageTypeConfig_IsEnabled_DisplayOrder` ON `SetupTechDunnageTypeConfig` (`IsEnabled`, `DisplayOrder`);