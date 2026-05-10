-- =============================================================
-- MTM Waitlist Application — trg_SharedWorkstations_BeforeUpdate
-- Domain:      Auth
-- Description: Refreshes UpdatedAt to UTC_TIMESTAMP() on every UPDATE.
-- Depends on:  schema/tables/Auth/SharedWorkstations.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_SharedWorkstations_BeforeUpdate`;

DELIMITER $$

CREATE TRIGGER `trg_SharedWorkstations_BeforeUpdate`
BEFORE UPDATE ON `SharedWorkstations`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;
