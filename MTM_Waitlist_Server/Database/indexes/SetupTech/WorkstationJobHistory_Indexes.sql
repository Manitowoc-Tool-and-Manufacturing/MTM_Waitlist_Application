-- =============================================================
-- MTM Waitlist Application — WorkstationJobHistory Indexes
-- Domain:      SetupTech
-- Description: Supports workstation history and analytics queries.
-- Depends on:  schema/tables/SetupTech/WorkstationJobHistory.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP INDEX IF EXISTS `idx_WorkstationJobHistory_Workcenter_ActiveFrom` ON `WorkstationJobHistory`;
CREATE INDEX `idx_WorkstationJobHistory_Workcenter_ActiveFrom` ON `WorkstationJobHistory` (`WorkcenterId`, `ActiveFrom`);

DROP INDEX IF EXISTS `idx_WorkstationJobHistory_SetupTechUserId` ON `WorkstationJobHistory`;
CREATE INDEX `idx_WorkstationJobHistory_SetupTechUserId` ON `WorkstationJobHistory` (`SetupTechUserId`);

DROP INDEX IF EXISTS `idx_WorkstationJobHistory_WorkOrder_Sequence` ON `WorkstationJobHistory`;
CREATE INDEX `idx_WorkstationJobHistory_WorkOrder_Sequence` ON `WorkstationJobHistory` (`WorkOrderId`, `SequenceNo`);