-- =============================================================
-- MTM Waitlist Application — usp_Waitlist_GetById
-- Domain:      Waitlist
-- Description: Returns a single waitlist entry by its primary key.
--              Returns no rows if the Id does not exist.
-- Called by:   IRepository_WaitlistEntry.GetWaitlistEntryByIdAsync()
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Waitlist_GetById`;

DELIMITER $$

CREATE PROCEDURE `usp_Waitlist_GetById`
(
    IN p_Id INT
)
BEGIN
    SELECT
        `Id`,
        `WorkcenterName`,
        `RequestType`,
        `Status`,
        `Priority`,
        `Notes`,
        `RequestedAt`,
        `ScheduledAt`,
        `CompletedAt`,
        `AssignedToUserId`,
        `CreatedAt`,
        `UpdatedAt`,
        `CreatedByUserId`,
        `UpdatedByUserId`
    FROM  `WaitlistEntries`
    WHERE `Id` = p_Id
    LIMIT 1;
END$$

DELIMITER ;
