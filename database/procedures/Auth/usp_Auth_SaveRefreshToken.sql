-- =============================================================
-- MTM Waitlist Application — usp_Auth_SaveRefreshToken
-- Domain:      Auth
-- Description: Persists a new refresh token hash for a user.
--              The API generates the raw token, SHA-256 hashes it, then calls
--              this procedure — plaintext tokens never reach MySQL.
-- Called by:   IService_Auth.LoginAsync() and IService_Auth.RefreshTokenAsync()
-- Depends on:  schema/tables/Auth/RefreshTokens.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_SaveRefreshToken`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_SaveRefreshToken`
(
    IN p_UserId    INT,
    IN p_TokenHash VARCHAR(256),
    IN p_ExpiresAt DATETIME
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    INSERT INTO `RefreshTokens`
        (`UserId`, `TokenHash`, `ExpiresAt`, `CreatedAt`)
    VALUES
        (p_UserId, p_TokenHash, p_ExpiresAt, UTC_TIMESTAMP());
END$$

DELIMITER ;
