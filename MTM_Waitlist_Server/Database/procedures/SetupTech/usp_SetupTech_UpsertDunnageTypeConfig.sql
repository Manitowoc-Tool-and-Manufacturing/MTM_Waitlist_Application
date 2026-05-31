-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_UpsertDunnageTypeConfig
-- Domain:      SetupTech
-- Description: Inserts or updates a SetupTech dunnage-type config row.
-- Called by:   Admin configuration sync for SetupTech dunnage type filters.
-- Depends on:  schema/tables/SetupTech/SetupTechDunnageTypeConfig.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_UpsertDunnageTypeConfig`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_UpsertDunnageTypeConfig`
(
    IN p_DunnageTypeId   INT,
    IN p_DunnageTypeName VARCHAR(100),
    IN p_IsEnabled       TINYINT(1),
    IN p_DisplayOrder    INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    INSERT INTO `SetupTechDunnageTypeConfig`
    (
        `DunnageTypeId`,
        `DunnageTypeName`,
        `IsEnabled`,
        `DisplayOrder`
    )
    VALUES
    (
        p_DunnageTypeId,
        p_DunnageTypeName,
        IFNULL(p_IsEnabled, 1),
        IFNULL(p_DisplayOrder, 99)
    )
    ON DUPLICATE KEY UPDATE
        `DunnageTypeName` = VALUES(`DunnageTypeName`),
        `IsEnabled`       = VALUES(`IsEnabled`),
        `DisplayOrder`    = VALUES(`DisplayOrder`);

    COMMIT;
END$$

DELIMITER ;