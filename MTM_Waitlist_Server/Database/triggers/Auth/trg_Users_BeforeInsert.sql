-- =============================================================
-- MTM Waitlist Application — trg_Users_BeforeInsert
-- Domain:      Auth
-- Description: Sets CreatedAt and UpdatedAt to UTC_TIMESTAMP() on INSERT.
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_Users_BeforeInsert`;

DELIMITER $$

CREATE TRIGGER `trg_Users_BeforeInsert`
BEFORE INSERT ON `Users`
FOR EACH ROW
BEGIN
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;
