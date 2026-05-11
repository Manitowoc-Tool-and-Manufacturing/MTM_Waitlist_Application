-- =============================================================
-- MTM Waitlist Application — usp_Auth_ValidateCredentials
-- Domain:      Auth
-- Description: Returns the stored password hash and account details for a
--              given Username so the API backend can perform bcrypt comparison.
--              Used for credential-based login on shared workstations.
--              Password comparison MUST happen in the API — never in SQL.
--              Returns no rows if the Username does not exist or IsActive = 0.
-- Called by:   IService_Auth.LoginAsync() via the REST API
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_ValidateCredentials`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_ValidateCredentials`
(
    IN  p_Username    VARCHAR(100)
)
BEGIN
    -- Return the hash and metadata; the API performs bcrypt.VerifyHash(plaintext, hash).
    SELECT
        `Id`,
        `PasswordHash`,
        `DisplayName`,
        `Role`,
        `IsActive`
    FROM  `Users`
    WHERE `Username` = p_Username
      AND `IsActive` = 1
    LIMIT 1;
END$$

DELIMITER ;
