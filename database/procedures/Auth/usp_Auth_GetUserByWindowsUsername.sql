-- =============================================================
-- MTM Waitlist Application — usp_Auth_GetUserByWindowsUsername
-- Domain:      Auth
-- Description: Looks up an active user by their Windows domain username for
--              automatic login on personal (non-shared) workstations.
--              Returns no rows if the Windows username is not mapped to any
--              user or if the account is inactive.
--              No password check is performed — Windows authentication
--              already verified identity before this is called.
-- Called by:   IService_Auth.AutoLoginAsync() via the REST API
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_GetUserByWindowsUsername`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_GetUserByWindowsUsername`
(
    IN p_WindowsUsername VARCHAR(100)
)
BEGIN
    SELECT
        `Id`,
        `Username`,
        `DisplayName`,
        `Role`,
        `IsActive`
    FROM  `Users`
    WHERE `WindowsUsername` = p_WindowsUsername
      AND `IsActive`        = 1
    LIMIT 1;
END$$

DELIMITER ;
