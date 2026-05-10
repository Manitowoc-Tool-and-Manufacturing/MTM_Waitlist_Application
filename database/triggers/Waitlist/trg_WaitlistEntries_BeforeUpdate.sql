-- =============================================================
-- MTM Waitlist Application — trg_WaitlistEntries_BeforeUpdate
-- Domain:      Waitlist
-- Description: On UPDATE:
--              1. Refreshes UpdatedAt to UTC_TIMESTAMP().
--              2. Auto-sets CompletedAt when Status transitions to 'Completed'
--                 or 'Cancelled' for the first time and CompletedAt was not
--                 explicitly provided.
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_WaitlistEntries_BeforeUpdate`;

DELIMITER $$

CREATE TRIGGER `trg_WaitlistEntries_BeforeUpdate`
BEFORE UPDATE ON `WaitlistEntries`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();

    -- Auto-set CompletedAt only on the transition into a resolved state.
    -- Prevents overwriting an existing CompletedAt when re-saving a record
    -- that was already Completed or Cancelled.
    IF NEW.`Status`     IN ('Completed', 'Cancelled')
       AND OLD.`Status` NOT IN ('Completed', 'Cancelled')
       AND NEW.`CompletedAt` IS NULL
    THEN
        SET NEW.`CompletedAt` = UTC_TIMESTAMP();
    END IF;
END$$

DELIMITER ;
