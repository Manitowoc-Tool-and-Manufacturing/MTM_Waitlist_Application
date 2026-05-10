-- =============================================================
-- MTM Waitlist Application — trg_SharedWorkstations_BeforeInsert
-- Domain:      Auth
-- Description: Sets CreatedAt and UpdatedAt to UTC_TIMESTAMP() on INSERT.
-- Depends on:  schema/tables/Auth/SharedWorkstations.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_SharedWorkstations_BeforeInsert`;

DELIMITER $$

CREATE TRIGGER `trg_SharedWorkstations_BeforeInsert`
BEFORE INSERT ON `SharedWorkstations`
FOR EACH ROW
BEGIN
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;
