-- =============================================================
-- MTM Waitlist Application — usp_Auth_RevokeRefreshToken
-- Domain:      Auth
-- Description: Marks a single refresh token as revoked by setting RevokedAt
--              to the current UTC timestamp. Used on logout when the client
--              supplies its current refresh token.
-- Called by:   IService_Auth.LogoutAsync() via the REST API
-- Depends on:  schema/tables/Auth/RefreshTokens.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_RevokeRefreshToken`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_RevokeRefreshToken`
(
    IN p_TokenHash VARCHAR(256)
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    UPDATE `RefreshTokens`
    SET    `RevokedAt` = UTC_TIMESTAMP()
    WHERE  `TokenHash` = p_TokenHash
      AND  `RevokedAt`  IS NULL;
END$$

DELIMITER ;
