-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_UpsertDunnageAssignment
-- Domain:      SetupTech
-- Description: Inserts or updates one dunnage assignment line for a
--              work-order and sequence pair.
-- Called by:   IRepository_WorkOrderDunnage.UpsertDunnageAssignmentAsync()
-- Depends on:  schema/tables/SetupTech/WorkOrderDunnageAssignments.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_UpsertDunnageAssignment`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_UpsertDunnageAssignment`
(
    IN p_WorkOrderId          VARCHAR(50),
    IN p_SequenceNo           INT,
    IN p_DunnagePartId        INT,
    IN p_DunnagePartName      VARCHAR(200),
    IN p_DunnageTypeId        INT,
    IN p_DunnageTypeName      VARCHAR(100),
    IN p_LastModifiedByUserId INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    INSERT INTO `WorkOrderDunnageAssignments`
    (
        `WorkOrderId`,
        `SequenceNo`,
        `DunnagePartId`,
        `DunnagePartName`,
        `DunnageTypeId`,
        `DunnageTypeName`,
        `LastModifiedByUserId`
    )
    VALUES
    (
        p_WorkOrderId,
        p_SequenceNo,
        p_DunnagePartId,
        p_DunnagePartName,
        p_DunnageTypeId,
        p_DunnageTypeName,
        p_LastModifiedByUserId
    )
    ON DUPLICATE KEY UPDATE
        `DunnagePartName`      = VALUES(`DunnagePartName`),
        `DunnageTypeId`        = VALUES(`DunnageTypeId`),
        `DunnageTypeName`      = VALUES(`DunnageTypeName`),
        `LastModifiedByUserId` = VALUES(`LastModifiedByUserId`);

    COMMIT;
END$$

DELIMITER ;