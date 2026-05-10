-- =============================================================
-- MTM Waitlist Application — RefreshTokens Indexes
-- Domain:      Auth
-- Description: Indexes for token lookups, user-scoped revocation, and expiry pruning.
-- Depends on:  schema/tables/Auth/RefreshTokens.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

-- Token hash lookup — the most frequent query (usp_Auth_GetRefreshToken).
CREATE INDEX `idx_RefreshTokens_TokenHash`
    ON `RefreshTokens` (`TokenHash`);

-- User-scoped token queries — MySQL requires explicit index for FK performance.
CREATE INDEX `idx_RefreshTokens_UserId`
    ON `RefreshTokens` (`UserId`);

-- Expiry pruning — cleanup jobs filter on ExpiresAt.
CREATE INDEX `idx_RefreshTokens_ExpiresAt`
    ON `RefreshTokens` (`ExpiresAt`);
