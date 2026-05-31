-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_SetActiveJob
-- Domain:      SetupTech
-- Description: Archives an existing workstation active job to history and
--              inserts the new active job in a single transaction.
-- Called by:   IRepository_SetupTechActiveJob.SetActiveJobAsync()
-- Depends on:  schema/tables/SetupTech/WorkstationActiveJobs.sql,
--              schema/tables/SetupTech/WorkstationJobHistory.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_SetActiveJob`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_SetActiveJob`
(
    IN p_WorkcenterId    VARCHAR(100),
    IN p_WorkOrderId     VARCHAR(50),
    IN p_SequenceNo      INT,
    IN p_PartId          VARCHAR(50),
    IN p_PartType        VARCHAR(50),
    IN p_SetupTechUserId INT,
    IN p_ActiveSince     DATETIME
)
BEGIN
    DECLARE v_ExistingId INT DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    SELECT `Id`
    INTO v_ExistingId
    FROM `WorkstationActiveJobs`
    WHERE `WorkcenterId` = p_WorkcenterId
    LIMIT 1;

    IF v_ExistingId IS NOT NULL THEN
        INSERT INTO `WorkstationJobHistory`
        (
            `WorkcenterId`,
            `WorkOrderId`,
            `SequenceNo`,
            `PartId`,
            `PartType`,
            `SetupTechUserId`,
            `ActiveFrom`,
            `ActiveUntil`
        )
        SELECT
            `WorkcenterId`,
            `WorkOrderId`,
            `SequenceNo`,
            `PartId`,
            `PartType`,
            `SetupTechUserId`,
            `ActiveSince`,
            UTC_TIMESTAMP()
        FROM `WorkstationActiveJobs`
        WHERE `Id` = v_ExistingId;

        DELETE FROM `WorkstationActiveJobs`
        WHERE `Id` = v_ExistingId;
    END IF;

    INSERT INTO `WorkstationActiveJobs`
    (
        `WorkcenterId`,
        `WorkOrderId`,
        `SequenceNo`,
        `PartId`,
        `PartType`,
        `SetupTechUserId`,
        `ActiveSince`
    )
    VALUES
    (
        p_WorkcenterId,
        p_WorkOrderId,
        p_SequenceNo,
        p_PartId,
        p_PartType,
        p_SetupTechUserId,
        IFNULL(p_ActiveSince, UTC_TIMESTAMP())
    );

    COMMIT;
END$$

DELIMITER ;