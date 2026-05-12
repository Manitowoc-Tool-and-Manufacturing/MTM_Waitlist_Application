USE `mtm_waitlist`;

DROP INDEX IF EXISTS `idx_RefreshTokens_TokenHash` ON `RefreshTokens`;
CREATE INDEX `idx_RefreshTokens_TokenHash` ON `RefreshTokens` (`TokenHash`);

DROP INDEX IF EXISTS `idx_RefreshTokens_UserId` ON `RefreshTokens`;
CREATE INDEX `idx_RefreshTokens_UserId` ON `RefreshTokens` (`UserId`);

DROP INDEX IF EXISTS `idx_RefreshTokens_ExpiresAt` ON `RefreshTokens`;
CREATE INDEX `idx_RefreshTokens_ExpiresAt` ON `RefreshTokens` (`ExpiresAt`);
