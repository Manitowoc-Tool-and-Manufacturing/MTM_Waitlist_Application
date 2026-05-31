-- =============================================================
-- MTM Waitlist Application — trg_WorkstationJobHistory_BeforeUpdate
-- Domain:      SetupTech
-- Description: Refreshes UpdatedAt on archived job history changes.
-- Depends on:  schema/tables/SetupTech/WorkstationJobHistory.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_WorkstationJobHistory_BeforeUpdate`;

DELIMITER $$

CREATE TRIGGER `trg_WorkstationJobHistory_BeforeUpdate`
BEFORE UPDATE ON `WorkstationJobHistory`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;