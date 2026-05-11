-- =============================================================
-- MTM Waitlist Application — trg_WaitlistEntries_BeforeInsert
-- Domain:      Waitlist
-- Description: On INSERT:
--              1. Sets CreatedAt and UpdatedAt to UTC_TIMESTAMP().
--              2. Auto-sets CompletedAt when a new entry is inserted directly
--                 with Status 'Completed' or 'Cancelled' (edge case).
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_WaitlistEntries_BeforeInsert`;

DELIMITER $$

CREATE TRIGGER `trg_WaitlistEntries_BeforeInsert`
BEFORE INSERT ON `WaitlistEntries`
FOR EACH ROW
BEGIN
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();

    -- Auto-set CompletedAt when a resolved status is supplied on insert
    -- and the caller did not explicitly provide a CompletedAt value.
    IF NEW.`Status` IN ('Completed', 'Cancelled')
       AND NEW.`CompletedAt` IS NULL
    THEN
        SET NEW.`CompletedAt` = UTC_TIMESTAMP();
    END IF;
END$$

DELIMITER ;
