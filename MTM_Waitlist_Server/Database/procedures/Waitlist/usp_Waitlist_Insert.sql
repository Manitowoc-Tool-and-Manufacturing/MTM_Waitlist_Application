-- =============================================================
-- MTM Waitlist Application — usp_Waitlist_Insert
-- Domain:      Waitlist
-- Description: Inserts a new waitlist entry and returns the generated Id.
--              CreatedAt and UpdatedAt are set by trg_WaitlistEntries_BeforeInsert.
--              RequestedAt defaults to UTC_TIMESTAMP() if not supplied.
--              Status defaults to 'Waiting' and Priority defaults to 5 if not supplied.
-- Called by:   IRepository_WaitlistEntry.InsertWaitlistEntryAsync()
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Waitlist_Insert`;

DELIMITER $$

CREATE PROCEDURE `usp_Waitlist_Insert`
(
    IN  p_WorkcenterName   VARCHAR(100),
    IN  p_RequestType      VARCHAR(30),
    IN  p_Status           VARCHAR(20),
    IN  p_Priority         TINYINT UNSIGNED,
    IN  p_Notes            TEXT,
    IN  p_RequestedAt      DATETIME,
    IN  p_ScheduledAt      DATETIME,
    IN  p_AssignedToUserId INT,
    IN  p_CreatedByUserId  INT,
    OUT p_NewId            INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        SET p_NewId = NULL;
        ROLLBACK;
        RESIGNAL;
    END;

    INSERT INTO `WaitlistEntries`
    (
        `WorkcenterName`,
        `RequestType`,
        `Status`,
        `Priority`,
        `Notes`,
        `RequestedAt`,
        `ScheduledAt`,
        `AssignedToUserId`,
        `CreatedByUserId`,
        `UpdatedByUserId`
    )
    VALUES
    (
        p_WorkcenterName,
        p_RequestType,
        IFNULL(p_Status,   'Waiting'),
        IFNULL(p_Priority, 5),
        p_Notes,
        IFNULL(p_RequestedAt, UTC_TIMESTAMP()),
        p_ScheduledAt,
        p_AssignedToUserId,
        p_CreatedByUserId,
        p_CreatedByUserId   -- same user on initial insert
    );

    SET p_NewId = LAST_INSERT_ID();
END$$

DELIMITER ;
