-- =============================================================
-- MTM Waitlist Application — trg_WorkOrderDunnageAssignments_BeforeInsert
-- Domain:      SetupTech
-- Description: Sets CreatedAt and UpdatedAt for cached dunnage lines.
-- Depends on:  schema/tables/SetupTech/WorkOrderDunnageAssignments.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_WorkOrderDunnageAssignments_BeforeInsert`;

DELIMITER $$

CREATE TRIGGER `trg_WorkOrderDunnageAssignments_BeforeInsert`
BEFORE INSERT ON `WorkOrderDunnageAssignments`
FOR EACH ROW
BEGIN
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;