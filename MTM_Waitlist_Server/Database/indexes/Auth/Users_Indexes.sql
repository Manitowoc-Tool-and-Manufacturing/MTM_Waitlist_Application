USE `mtm_waitlist`;
DROP INDEX IF EXISTS `idx_Users_IsActive` ON `Users`;
CREATE INDEX `idx_Users_IsActive` ON `Users` (`IsActive`);
DROP INDEX IF EXISTS `idx_Users_Role` ON `Users`;
CREATE INDEX `idx_Users_Role` ON `Users` (`Role`);
