-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_DeleteDunnageAssignment
-- Domain:      SetupTech
-- Description: Deletes one cached dunnage assignment line by Id.
-- Called by:   IRepository_WorkOrderDunnage.DeleteDunnageAssignmentAsync()
-- Depends on:  schema/tables/SetupTech/WorkOrderDunnageAssignments.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_DeleteDunnageAssignment`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_DeleteDunnageAssignment`
(
    IN p_Id INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    DELETE FROM `WorkOrderDunnageAssignments`
    WHERE `Id` = p_Id;

    COMMIT;
END$$

DELIMITER ;