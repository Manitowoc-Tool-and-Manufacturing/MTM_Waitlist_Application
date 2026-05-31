-- =============================================================
-- MTM Waitlist Application — WorkstationActiveJobs Indexes
-- Domain:      SetupTech
-- Description: Supports workstation current-job and audit queries.
-- Depends on:  schema/tables/SetupTech/WorkstationActiveJobs.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP INDEX IF EXISTS `idx_WorkstationActiveJobs_SetupTechUserId` ON `WorkstationActiveJobs`;
CREATE INDEX `idx_WorkstationActiveJobs_SetupTechUserId` ON `WorkstationActiveJobs` (`SetupTechUserId`);

DROP INDEX IF EXISTS `idx_WorkstationActiveJobs_WorkOrder_Sequence` ON `WorkstationActiveJobs`;
CREATE INDEX `idx_WorkstationActiveJobs_WorkOrder_Sequence` ON `WorkstationActiveJobs` (`WorkOrderId`, `SequenceNo`);