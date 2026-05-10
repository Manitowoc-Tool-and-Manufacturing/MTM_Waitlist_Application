-- =============================================================
-- MTM Waitlist Application — RefreshTokens Table
-- Domain:      Auth
-- Description: Persisted JWT refresh tokens for session continuation.
--              Active tokens have RevokedAt = NULL and ExpiresAt > UTC_TIMESTAMP().
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `RefreshTokens`
(
    `Id`        INT          NOT NULL AUTO_INCREMENT
                             COMMENT 'Surrogate primary key.',

    `UserId`    INT          NOT NULL
                             COMMENT 'FK → Users.Id. Cascade-deleted when the user is removed.',

    `TokenHash` VARCHAR(256) NOT NULL
                             COMMENT 'SHA-256 hex digest of the raw refresh token. Stored hashed — the API verifies by hashing the incoming token and comparing.',

    `ExpiresAt` DATETIME     NOT NULL
                             COMMENT 'UTC — when this token can no longer be used to obtain a new access token.',

    `CreatedAt` DATETIME     NOT NULL
                             COMMENT 'UTC — set on INSERT by the API.',

    `RevokedAt` DATETIME     NULL
                             COMMENT 'UTC — set by usp_Auth_RevokeRefreshToken or usp_Auth_RevokeAllUserTokens on logout. NULL = token is still valid.',

    CONSTRAINT `pk_RefreshTokens`        PRIMARY KEY (`Id`),
    CONSTRAINT `fk_RefreshTokens_Users`  FOREIGN KEY (`UserId`)
        REFERENCES `Users` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'JWT refresh tokens. A token is active when RevokedAt IS NULL and ExpiresAt > UTC_TIMESTAMP().';
