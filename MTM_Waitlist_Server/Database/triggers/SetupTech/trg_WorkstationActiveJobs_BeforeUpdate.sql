-- =============================================================
-- MTM Waitlist Application — trg_WorkstationActiveJobs_BeforeUpdate
-- Domain:      SetupTech
-- Description: Refreshes UpdatedAt on every workstation active-job change.
-- Depends on:  schema/tables/SetupTech/WorkstationActiveJobs.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_WorkstationActiveJobs_BeforeUpdate`;

DELIMITER $$

CREATE TRIGGER `trg_WorkstationActiveJobs_BeforeUpdate`
BEFORE UPDATE ON `WorkstationActiveJobs`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;