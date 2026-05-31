-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_GetActiveJob
-- Domain:      SetupTech
-- Description: Returns the current active job row for a workstation.
-- Called by:   IRepository_SetupTechActiveJob.GetActiveJobAsync()
-- Depends on:  schema/tables/SetupTech/WorkstationActiveJobs.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_GetActiveJob`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_GetActiveJob`
(
    IN p_WorkcenterId VARCHAR(100)
)
BEGIN
    SELECT
        `Id`,
        `WorkcenterId`,
        `WorkOrderId`,
        `SequenceNo`,
        `PartId`,
        `PartType`,
        `SetupTechUserId`,
        `ActiveSince`,
        `CreatedAt`,
        `UpdatedAt`
    FROM `WorkstationActiveJobs`
    WHERE `WorkcenterId` = p_WorkcenterId
    LIMIT 1;
END$$

DELIMITER ;