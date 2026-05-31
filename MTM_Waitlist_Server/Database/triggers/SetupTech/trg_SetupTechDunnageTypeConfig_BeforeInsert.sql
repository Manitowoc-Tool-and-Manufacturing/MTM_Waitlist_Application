-- =============================================================
-- MTM Waitlist Application — trg_SetupTechDunnageTypeConfig_BeforeInsert
-- Domain:      SetupTech
-- Description: Sets CreatedAt and UpdatedAt for SetupTech dunnage type config rows.
-- Depends on:  schema/tables/SetupTech/SetupTechDunnageTypeConfig.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_SetupTechDunnageTypeConfig_BeforeInsert`;

DELIMITER $$

CREATE TRIGGER `trg_SetupTechDunnageTypeConfig_BeforeInsert`
BEFORE INSERT ON `SetupTechDunnageTypeConfig`
FOR EACH ROW
BEGIN
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;