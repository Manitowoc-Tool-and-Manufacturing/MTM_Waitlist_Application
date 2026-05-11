-- =============================================================
-- MTM Waitlist Application — usp_Auth_RecordLogin
-- Domain:      Auth
-- Description: Updates LastLoginAt to the current UTC timestamp after a
--              successful authentication (credential login or auto-login).
--              Called once per successful login — not on token refresh.
-- Called by:   IService_Auth.LoginAsync() via the REST API
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_RecordLogin`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_RecordLogin`
(
    IN p_UserId INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    UPDATE `Users`
    SET    `LastLoginAt` = UTC_TIMESTAMP()
    WHERE  `Id`         = p_UserId;
END$$

DELIMITER ;
