-- =============================================================
-- MTM Waitlist Application — trg_WorkOrderDunnageAssignments_BeforeUpdate
-- Domain:      SetupTech
-- Description: Refreshes UpdatedAt on cached dunnage assignment changes.
-- Depends on:  schema/tables/SetupTech/WorkOrderDunnageAssignments.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP TRIGGER IF EXISTS `trg_WorkOrderDunnageAssignments_BeforeUpdate`;

DELIMITER $$

CREATE TRIGGER `trg_WorkOrderDunnageAssignments_BeforeUpdate`
BEFORE UPDATE ON `WorkOrderDunnageAssignments`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$

DELIMITER ;