-- =============================================================
-- MTM Waitlist Application — WorkOrderDunnageAssignments Indexes
-- Domain:      SetupTech
-- Description: Supports dunnage assignment lookup and modification queries.
-- Depends on:  schema/tables/SetupTech/WorkOrderDunnageAssignments.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP INDEX IF EXISTS `idx_WorkOrderDunnageAssignments_LastModifiedByUserId` ON `WorkOrderDunnageAssignments`;
CREATE INDEX `idx_WorkOrderDunnageAssignments_LastModifiedByUserId` ON `WorkOrderDunnageAssignments` (`LastModifiedByUserId`);

DROP INDEX IF EXISTS `idx_WorkOrderDunnageAssignments_DunnageTypeId` ON `WorkOrderDunnageAssignments`;
CREATE INDEX `idx_WorkOrderDunnageAssignments_DunnageTypeId` ON `WorkOrderDunnageAssignments` (`DunnageTypeId`);