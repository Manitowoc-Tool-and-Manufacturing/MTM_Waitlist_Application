USE `mtm_waitlist`;
DROP INDEX IF EXISTS `idx_SharedWorkstations_IsActive` ON `SharedWorkstations`;
CREATE INDEX `idx_SharedWorkstations_IsActive` ON `SharedWorkstations` (`IsActive`);
