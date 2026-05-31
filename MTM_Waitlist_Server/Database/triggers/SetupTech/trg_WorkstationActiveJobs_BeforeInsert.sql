-- =============================================================
-- MTM Waitlist Application — trg_WorkstationActiveJobs_BeforeInsert
-- Domain:      SetupTech
-- Description: Sets ActiveSince, CreatedAt, and UpdatedAt to UTC values.
-- Depends on:  schema/tables/SetupTech/WorkstationActiveJobs.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_WorkstationActiveJobs_BeforeInsert`;

DELIMITER $$

CREATE TRIGGER `trg_WorkstationActiveJobs_BeforeInsert`
BEFORE INSERT ON `WorkstationActiveJobs`
FOR EACH ROW
BEGIN
    IF NEW.`ActiveSince` IS NULL THEN
        SET NEW.`ActiveSince` = UTC_TIMESTAMP();
    END IF;

    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;