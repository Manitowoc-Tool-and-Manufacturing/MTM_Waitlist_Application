-- =============================================================
-- MTM Waitlist Application — usp_Waitlist_Update
-- Domain:      Waitlist
-- Description: Updates all mutable fields of an existing waitlist entry.
--              UpdatedAt is refreshed automatically by trg_WaitlistEntries_BeforeUpdate.
--              CompletedAt is auto-set by the trigger when Status transitions
--              to 'Completed' or 'Cancelled'.
--              Pass all fields; use current values for unchanged columns.
-- Called by:   IRepository_WaitlistEntry.UpdateWaitlistEntryAsync()
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Waitlist_Update`;

DELIMITER $$

CREATE PROCEDURE `usp_Waitlist_Update`
(
    IN p_Id               INT,
    IN p_WorkcenterName   VARCHAR(100),
    IN p_RequestType      VARCHAR(30),
    IN p_Status           VARCHAR(20),
    IN p_Priority         TINYINT UNSIGNED,
    IN p_Notes            TEXT,
    IN p_RequestedAt      DATETIME,
    IN p_ScheduledAt      DATETIME,
    IN p_CompletedAt      DATETIME,
    IN p_AssignedToUserId INT,
    IN p_UpdatedByUserId  INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    UPDATE `WaitlistEntries`
    SET
        `WorkcenterName`   = p_WorkcenterName,
        `RequestType`      = p_RequestType,
        `Status`           = p_Status,
        `Priority`         = p_Priority,
        `Notes`            = p_Notes,
        `RequestedAt`      = p_RequestedAt,
        `ScheduledAt`      = p_ScheduledAt,
        `CompletedAt`      = p_CompletedAt,
        `AssignedToUserId` = p_AssignedToUserId,
        `UpdatedByUserId`  = p_UpdatedByUserId
        -- UpdatedAt is set by trg_WaitlistEntries_BeforeUpdate automatically.
    WHERE `Id` = p_Id;
END$$

DELIMITER ;
