-- =============================================================
-- MTM Waitlist Application — usp_Waitlist_GetAll
-- Domain:      Waitlist
-- Description: Returns all waitlist entries, optionally filtered by Status
--              and/or RequestType. Results are ordered by Priority ASC then
--              RequestedAt ASC (earliest high-priority entries first).
--              Optional p_Limit / p_Offset support pagination from the API.
--              All filter parameters are nullable — pass NULL to skip.
-- Called by:   IRepository_WaitlistEntry.GetAllWaitlistEntriesAsync()
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Waitlist_GetAll`;

DELIMITER $$

CREATE PROCEDURE `usp_Waitlist_GetAll`
(
    IN p_Status      VARCHAR(20),    -- NULL = all statuses
    IN p_RequestType VARCHAR(30),    -- NULL = all request types
    IN p_Limit       INT,            -- 0 or NULL = no limit
    IN p_Offset      INT             -- 0 or NULL = start from beginning
)
BEGIN
    -- Default no-limit so callers that pass NULL still get all rows.
    SET p_Limit  = IF(p_Limit  IS NULL OR p_Limit  = 0, 2147483647, p_Limit);
    SET p_Offset = IF(p_Offset IS NULL,                 0,          p_Offset);

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
    WHERE (p_Status      IS NULL OR `Status`      = p_Status)
      AND (p_RequestType IS NULL OR `RequestType` = p_RequestType)
    ORDER BY `Priority`    ASC,
             `RequestedAt` ASC
    LIMIT  p_Limit
    OFFSET p_Offset;
END$$

DELIMITER ;
