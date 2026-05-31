-- =============================================================
-- MTM Waitlist Application — trg_SetupTechDunnageTypeConfig_BeforeUpdate
-- Domain:      SetupTech
-- Description: Refreshes UpdatedAt for SetupTech dunnage type config changes.
-- Depends on:  schema/tables/SetupTech/SetupTechDunnageTypeConfig.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_SetupTechDunnageTypeConfig_BeforeUpdate`;

DELIMITER $$

CREATE TRIGGER `trg_SetupTechDunnageTypeConfig_BeforeUpdate`
BEFORE UPDATE ON `SetupTechDunnageTypeConfig`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;