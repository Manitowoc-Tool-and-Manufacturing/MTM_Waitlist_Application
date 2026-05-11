-- =============================================================
-- MTM Waitlist Application — usp_Auth_CheckSharedWorkstation
-- Domain:      Auth
-- Description: Checks whether a given Windows username belongs to a shared
--              workstation that requires manual credential login.
--              Returns the SharedWorkstations row if found and IsActive = 1;
--              returns no rows if the workstation is personal (auto-login).
--
-- Login flow:
--   Row returned  → show login form → call usp_Auth_ValidateCredentials
--   No row        → call usp_Auth_GetUserByWindowsUsername for auto-login
--
-- Called by:   IService_Auth.DetermineLoginModeAsync() via the REST API
-- Depends on:  schema/tables/Auth/SharedWorkstations.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_CheckSharedWorkstation`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_CheckSharedWorkstation`
(
    IN p_WindowsUsername VARCHAR(100)
)
BEGIN
    SELECT
        `Id`,
        `WindowsUsername`,
        `MachineName`,
        `IsActive`
    FROM  `SharedWorkstations`
    WHERE `WindowsUsername` = p_WindowsUsername
      AND `IsActive`        = 1
    LIMIT 1;
END$$

DELIMITER ;
