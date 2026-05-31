-- =============================================================
-- MTM Waitlist Application — trg_WorkOrderSubordinateParts_BeforeUpdate
-- Domain:      SetupTech
-- Description: Refreshes UpdatedAt for subordinate-part cache changes.
-- Depends on:  schema/tables/SetupTech/WorkOrderSubordinateParts.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_WorkOrderSubordinateParts_BeforeUpdate`;

DELIMITER $$

CREATE TRIGGER `trg_WorkOrderSubordinateParts_BeforeUpdate`
BEFORE UPDATE ON `WorkOrderSubordinateParts`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;