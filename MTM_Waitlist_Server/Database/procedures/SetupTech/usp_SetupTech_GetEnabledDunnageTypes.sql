-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_GetEnabledDunnageTypes
-- Domain:      SetupTech
-- Description: Returns enabled SetupTech dunnage type config rows in display order.
-- Called by:   IRepository_SetupTechDunnageTypeConfig.GetEnabledDunnageTypesAsync()
-- Depends on:  schema/tables/SetupTech/SetupTechDunnageTypeConfig.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_GetEnabledDunnageTypes`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_GetEnabledDunnageTypes`()
BEGIN
    SELECT
        `Id`,
        `DunnageTypeId`,
        `DunnageTypeName`,
        `IsEnabled`,
        `DisplayOrder`,
        `CreatedAt`,
        `UpdatedAt`
    FROM `SetupTechDunnageTypeConfig`
    WHERE `IsEnabled` = 1
    ORDER BY `DisplayOrder`, `DunnageTypeName`, `Id`;
END$$

DELIMITER ;