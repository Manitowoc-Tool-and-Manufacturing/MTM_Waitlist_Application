-- =============================================================
-- MTM Waitlist Application — usp_Auth_GetRefreshToken
-- Domain:      Auth
-- Description: Looks up an active refresh token by its hash and returns the
--              token record joined with the owning user's account details.
--              Returns no rows if the token is expired, revoked, or unknown.
--              The API hashes the incoming raw token and passes the hash here.
-- Called by:   IService_Auth.RefreshTokenAsync() via the REST API
-- Depends on:  schema/tables/Auth/RefreshTokens.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_GetRefreshToken`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_GetRefreshToken`
(
    IN p_TokenHash VARCHAR(256)
)
BEGIN
    SELECT
        `rt`.`Id`          AS `TokenId`,
        `rt`.`UserId`,
        `rt`.`ExpiresAt`,
        `rt`.`RevokedAt`,
        `u`.`Username`,
        `u`.`DisplayName`,
        `u`.`Role`,
        `u`.`IsActive`
    FROM  `RefreshTokens` `rt`
    INNER JOIN `Users`    `u`  ON `u`.`Id` = `rt`.`UserId`
    WHERE `rt`.`TokenHash` = p_TokenHash
      AND `rt`.`RevokedAt`  IS NULL
      AND `rt`.`ExpiresAt`  > UTC_TIMESTAMP()
    LIMIT 1;
END$$

DELIMITER ;
