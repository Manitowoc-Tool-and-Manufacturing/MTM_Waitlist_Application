-- =============================================================
-- MTM Waitlist Application — trg_WorkstationJobHistory_BeforeInsert
-- Domain:      SetupTech
-- Description: Sets ActiveFrom and ActiveUntil defaults plus CreatedAt/UpdatedAt.
-- Depends on:  schema/tables/SetupTech/WorkstationJobHistory.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_WorkstationJobHistory_BeforeInsert`;

DELIMITER $$

CREATE TRIGGER `trg_WorkstationJobHistory_BeforeInsert`
BEFORE INSERT ON `WorkstationJobHistory`
FOR EACH ROW
BEGIN
    IF NEW.`ActiveFrom` IS NULL THEN
        SET NEW.`ActiveFrom` = UTC_TIMESTAMP();
    END IF;

    IF NEW.`ActiveUntil` IS NULL THEN
        SET NEW.`ActiveUntil` = UTC_TIMESTAMP();
    END IF;

    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;