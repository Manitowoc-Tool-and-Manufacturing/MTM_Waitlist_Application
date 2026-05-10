-- =============================================================
-- MTM Waitlist Application — usp_Auth_RevokeAllUserTokens
-- Domain:      Auth
-- Description: Marks ALL active refresh tokens for a given user as revoked.
--              Used when an admin deactivates an account, a user changes their
--              password, or a security event requires forced sign-out.
-- Called by:   IService_Auth.LogoutAsync() (full sign-out) or admin revocation
-- Depends on:  schema/tables/Auth/RefreshTokens.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_RevokeAllUserTokens`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_RevokeAllUserTokens`
(
    IN p_UserId INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    UPDATE `RefreshTokens`
    SET    `RevokedAt` = UTC_TIMESTAMP()
    WHERE  `UserId`    = p_UserId
      AND  `RevokedAt`  IS NULL;
END$$

DELIMITER ;
