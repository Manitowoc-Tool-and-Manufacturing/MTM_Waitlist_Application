-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_GetJobHistory
-- Domain:      SetupTech
-- Description: Returns archived workstation job history in reverse chronological order.
-- Called by:   IRepository_SetupTechActiveJob.GetJobHistoryAsync()
-- Depends on:  schema/tables/SetupTech/WorkstationJobHistory.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_GetJobHistory`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_GetJobHistory`
(
    IN p_WorkcenterId VARCHAR(100),
    IN p_PageSize     INT
)
BEGIN
    DECLARE v_PageSize INT DEFAULT 50;

    SET v_PageSize = IFNULL(p_PageSize, 50);

    SELECT
        `Id`,
        `WorkcenterId`,
        `WorkOrderId`,
        `SequenceNo`,
        `PartId`,
        `PartType`,
        `SetupTechUserId`,
        `ActiveFrom`,
        `ActiveUntil`,
        `CreatedAt`,
        `UpdatedAt`
    FROM `WorkstationJobHistory`
    WHERE `WorkcenterId` = p_WorkcenterId
    ORDER BY `ActiveFrom` DESC, `Id` DESC
    LIMIT v_PageSize;
END$$

DELIMITER ;