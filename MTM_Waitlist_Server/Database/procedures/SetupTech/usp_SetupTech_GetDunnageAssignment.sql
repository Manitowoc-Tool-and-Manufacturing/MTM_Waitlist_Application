-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_GetDunnageAssignment
-- Domain:      SetupTech
-- Description: Returns cached dunnage assignment rows for a work-order and sequence.
-- Called by:   IRepository_WorkOrderDunnage.GetDunnageAssignmentAsync()
-- Depends on:  schema/tables/SetupTech/WorkOrderDunnageAssignments.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_GetDunnageAssignment`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_GetDunnageAssignment`
(
    IN p_WorkOrderId VARCHAR(50),
    IN p_SequenceNo  INT
)
BEGIN
    SELECT
        `Id`,
        `WorkOrderId`,
        `SequenceNo`,
        `DunnagePartId`,
        `DunnagePartName`,
        `DunnageTypeId`,
        `DunnageTypeName`,
        `LastModifiedByUserId`,
        `CreatedAt`,
        `UpdatedAt`
    FROM `WorkOrderDunnageAssignments`
    WHERE `WorkOrderId` = p_WorkOrderId
      AND `SequenceNo`  = p_SequenceNo
    ORDER BY `DunnageTypeName`, `DunnagePartName`, `Id`;
END$$

DELIMITER ;