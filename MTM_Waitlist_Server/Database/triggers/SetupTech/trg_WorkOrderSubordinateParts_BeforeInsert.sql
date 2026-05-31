-- =============================================================
-- MTM Waitlist Application — trg_WorkOrderSubordinateParts_BeforeInsert
-- Domain:      SetupTech
-- Description: Sets CachedAt, CreatedAt, and UpdatedAt for subordinate-part rows.
-- Depends on:  schema/tables/SetupTech/WorkOrderSubordinateParts.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_WorkOrderSubordinateParts_BeforeInsert`;

DELIMITER $$

CREATE TRIGGER `trg_WorkOrderSubordinateParts_BeforeInsert`
BEFORE INSERT ON `WorkOrderSubordinateParts`
FOR EACH ROW
BEGIN
    IF NEW.`CachedAt` IS NULL THEN
        SET NEW.`CachedAt` = UTC_TIMESTAMP();
    END IF;

    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;