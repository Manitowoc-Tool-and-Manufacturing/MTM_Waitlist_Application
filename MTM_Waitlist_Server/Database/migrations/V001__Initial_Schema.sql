-- =============================================================
-- MTM Waitlist Application — V001__Initial_Schema
-- Version:     V001
-- Description: Complete initial schema — creates the mtm_waitlist database,
--              all tables, indexes, stored procedures, and triggers in the
--              correct dependency order.
--              Does NOT include seed data (run seed/ files separately in dev).
--
-- Server:      172.16.1.104  (internal work network — not public)
-- MySQL:       5.7 compatible
-- Run as:      A MySQL user with CREATE, INDEX, TRIGGER, PROCEDURE privileges
--
-- Usage:
--   mysql -h 172.16.1.104 -u <admin_user> -p < migrations/V001__Initial_Schema.sql
--
-- ROLLBACK: Drop the database (destructive — confirm before running):
--   DROP DATABASE IF EXISTS `mtm_waitlist`;
-- =============================================================


-- ─────────────────────────────────────────────────────────────
-- SECTION 1 — Database
-- ─────────────────────────────────────────────────────────────

CREATE DATABASE IF NOT EXISTS `mtm_waitlist`
    CHARACTER SET utf8mb4
    COLLATE      utf8mb4_unicode_ci;

USE `mtm_waitlist`;


-- ─────────────────────────────────────────────────────────────
-- SECTION 2 — Tables
-- ─────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS `Users`
(
    `Id`              INT          NOT NULL AUTO_INCREMENT,
    `WindowsUsername` VARCHAR(100) NULL     COMMENT 'AD/Windows login for personal-workstation auto-login. NULL for shared-workstation-only accounts.',
    `Username`        VARCHAR(100) NOT NULL COMMENT 'App login name used on shared workstations.',
    `PasswordHash`    VARCHAR(256) NOT NULL COMMENT 'bcrypt hash. API performs comparison — MySQL never sees plaintext.',
    `DisplayName`     VARCHAR(200) NOT NULL,
    `Role`  ENUM(
                'PressOperation',
                'SetupTech',
                'ProductionSupervisor',
                'ProductionManager',
                'Quality',
                'Receiving',
                'MaterialHandler',
                'Admin',
                'Developer'
            )                      NOT NULL DEFAULT 'PressOperation',
    `IsActive`        TINYINT(1)   NOT NULL DEFAULT 1,
    `LastLoginAt`     DATETIME     NULL,
    `CreatedAt`       DATETIME     NOT NULL,
    `UpdatedAt`       DATETIME     NOT NULL,
    CONSTRAINT `pk_Users`                  PRIMARY KEY (`Id`),
    CONSTRAINT `uq_Users_WindowsUsername`  UNIQUE      (`WindowsUsername`),
    CONSTRAINT `uq_Users_Username`         UNIQUE      (`Username`)
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'Application users. Supports both Windows auto-login and shared-workstation credential login.';

CREATE TABLE IF NOT EXISTS `SharedWorkstations`
(
    `Id`              INT          NOT NULL AUTO_INCREMENT,
    `WindowsUsername` VARCHAR(100) NOT NULL COMMENT 'Windows login name of the shared PC or kiosk.',
    `MachineName`     VARCHAR(150) NULL,
    `Notes`           TEXT         NULL,
    `IsActive`        TINYINT(1)   NOT NULL DEFAULT 1,
    `CreatedAt`       DATETIME     NOT NULL,
    `UpdatedAt`       DATETIME     NOT NULL,
    CONSTRAINT `pk_SharedWorkstations`                 PRIMARY KEY (`Id`),
    CONSTRAINT `uq_SharedWorkstations_WindowsUsername` UNIQUE      (`WindowsUsername`)
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'Shared/kiosk PCs that require manual credential login instead of Windows auto-login.';

CREATE TABLE IF NOT EXISTS `RefreshTokens`
(
    `Id`        INT          NOT NULL AUTO_INCREMENT,
    `UserId`    INT          NOT NULL,
    `TokenHash` VARCHAR(256) NOT NULL COMMENT 'SHA-256 hash of the raw token.',
    `ExpiresAt` DATETIME     NOT NULL,
    `CreatedAt` DATETIME     NOT NULL,
    `RevokedAt` DATETIME     NULL,
    CONSTRAINT `pk_RefreshTokens`       PRIMARY KEY (`Id`),
    CONSTRAINT `fk_RefreshTokens_Users` FOREIGN KEY (`UserId`)
        REFERENCES `Users` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'JWT refresh tokens. Active: RevokedAt IS NULL AND ExpiresAt > UTC_TIMESTAMP().';

CREATE TABLE IF NOT EXISTS `WaitlistEntries`
(
    `Id`               INT              NOT NULL AUTO_INCREMENT,
    `WorkcenterName`   VARCHAR(100)     NOT NULL,
    `RequestType`      ENUM(
                           'Coil',
                           'Dunnage',
                           'PickUpFinishedGoods',
                           'PickUpUnusedGoods',
                           'PickUpDunnage',
                           'BringPartsToPress',
                           'RemoveCoilFromPress',
                           'BringPickUpDie'
                       )               NOT NULL,
    `Status`           ENUM(
                           'Waiting',
                           'Active',
                           'Late',
                           'LowImportance',
                           'Project',
                           'Completed',
                           'Cancelled'
                       )               NOT NULL DEFAULT 'Waiting',
    `Priority`         TINYINT UNSIGNED NOT NULL DEFAULT 5,
    `Notes`            TEXT             NULL,
    `RequestedAt`      DATETIME         NOT NULL,
    `ScheduledAt`      DATETIME         NULL,
    `CompletedAt`      DATETIME         NULL,
    `AssignedToUserId` INT              NULL,
    `CreatedAt`        DATETIME         NOT NULL,
    `UpdatedAt`        DATETIME         NOT NULL,
    `CreatedByUserId`  INT              NULL,
    `UpdatedByUserId`  INT              NULL,
    CONSTRAINT `pk_WaitlistEntries` PRIMARY KEY (`Id`),
    CONSTRAINT `fk_WaitlistEntries_AssignedToUser`
        FOREIGN KEY (`AssignedToUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `fk_WaitlistEntries_CreatedByUser`
        FOREIGN KEY (`CreatedByUserId`)  REFERENCES `Users` (`Id`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `fk_WaitlistEntries_UpdatedByUser`
        FOREIGN KEY (`UpdatedByUserId`)  REFERENCES `Users` (`Id`) ON DELETE SET NULL ON UPDATE CASCADE
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'Workcenter logistics/material-handling requests queued for fulfillment.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 3 — Indexes
-- ─────────────────────────────────────────────────────────────

CREATE INDEX `idx_Users_IsActive` ON `Users` (`IsActive`);
CREATE INDEX `idx_Users_Role`     ON `Users` (`Role`);

CREATE INDEX `idx_SharedWorkstations_IsActive` ON `SharedWorkstations` (`IsActive`);

CREATE INDEX `idx_RefreshTokens_TokenHash` ON `RefreshTokens` (`TokenHash`);
CREATE INDEX `idx_RefreshTokens_UserId`    ON `RefreshTokens` (`UserId`);
CREATE INDEX `idx_RefreshTokens_ExpiresAt` ON `RefreshTokens` (`ExpiresAt`);

CREATE INDEX `idx_WaitlistEntries_Status`           ON `WaitlistEntries` (`Status`);
CREATE INDEX `idx_WaitlistEntries_Priority_Status`  ON `WaitlistEntries` (`Priority`, `Status`);
CREATE INDEX `idx_WaitlistEntries_RequestType`      ON `WaitlistEntries` (`RequestType`);
CREATE INDEX `idx_WaitlistEntries_WorkcenterName`   ON `WaitlistEntries` (`WorkcenterName`);
CREATE INDEX `idx_WaitlistEntries_RequestedAt`      ON `WaitlistEntries` (`RequestedAt`);
CREATE INDEX `idx_WaitlistEntries_AssignedToUserId` ON `WaitlistEntries` (`AssignedToUserId`);
CREATE INDEX `idx_WaitlistEntries_CreatedByUserId`  ON `WaitlistEntries` (`CreatedByUserId`);
CREATE INDEX `idx_WaitlistEntries_UpdatedByUserId`  ON `WaitlistEntries` (`UpdatedByUserId`);


-- ─────────────────────────────────────────────────────────────
-- SECTION 4 — Triggers
-- ─────────────────────────────────────────────────────────────

DROP TRIGGER IF EXISTS `trg_Users_BeforeInsert`;
DELIMITER $$
CREATE TRIGGER `trg_Users_BeforeInsert`
BEFORE INSERT ON `Users` FOR EACH ROW
BEGIN
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_Users_BeforeUpdate`;
DELIMITER $$
CREATE TRIGGER `trg_Users_BeforeUpdate`
BEFORE UPDATE ON `Users` FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_SharedWorkstations_BeforeInsert`;
DELIMITER $$
CREATE TRIGGER `trg_SharedWorkstations_BeforeInsert`
BEFORE INSERT ON `SharedWorkstations` FOR EACH ROW
BEGIN
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_SharedWorkstations_BeforeUpdate`;
DELIMITER $$
CREATE TRIGGER `trg_SharedWorkstations_BeforeUpdate`
BEFORE UPDATE ON `SharedWorkstations` FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_WaitlistEntries_BeforeInsert`;
DELIMITER $$
CREATE TRIGGER `trg_WaitlistEntries_BeforeInsert`
BEFORE INSERT ON `WaitlistEntries` FOR EACH ROW
BEGIN
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
    IF NEW.`Status` IN ('Completed', 'Cancelled') AND NEW.`CompletedAt` IS NULL THEN
        SET NEW.`CompletedAt` = UTC_TIMESTAMP();
    END IF;
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_WaitlistEntries_BeforeUpdate`;
DELIMITER $$
CREATE TRIGGER `trg_WaitlistEntries_BeforeUpdate`
BEFORE UPDATE ON `WaitlistEntries` FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
    IF  NEW.`Status`     IN ('Completed', 'Cancelled')
    AND OLD.`Status` NOT IN ('Completed', 'Cancelled')
    AND NEW.`CompletedAt` IS NULL
    THEN
        SET NEW.`CompletedAt` = UTC_TIMESTAMP();
    END IF;
END$$
DELIMITER ;


-- ─────────────────────────────────────────────────────────────
-- SECTION 5 — Stored Procedures: Auth
-- ─────────────────────────────────────────────────────────────

DROP PROCEDURE IF EXISTS `usp_Auth_ValidateCredentials`;
DELIMITER $$
CREATE PROCEDURE `usp_Auth_ValidateCredentials`(IN p_Username VARCHAR(100))
BEGIN
    SELECT `Id`, `PasswordHash`, `DisplayName`, `Role`, `IsActive`
    FROM   `Users`
    WHERE  `Username` = p_Username AND `IsActive` = 1
    LIMIT 1;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_Auth_GetUserByWindowsUsername`;
DELIMITER $$
CREATE PROCEDURE `usp_Auth_GetUserByWindowsUsername`(IN p_WindowsUsername VARCHAR(100))
BEGIN
    SELECT `Id`, `Username`, `DisplayName`, `Role`, `IsActive`
    FROM   `Users`
    WHERE  `WindowsUsername` = p_WindowsUsername AND `IsActive` = 1
    LIMIT 1;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_Auth_CheckSharedWorkstation`;
DELIMITER $$
CREATE PROCEDURE `usp_Auth_CheckSharedWorkstation`(IN p_WindowsUsername VARCHAR(100))
BEGIN
    SELECT `Id`, `WindowsUsername`, `MachineName`, `IsActive`
    FROM   `SharedWorkstations`
    WHERE  `WindowsUsername` = p_WindowsUsername AND `IsActive` = 1
    LIMIT 1;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_Auth_RecordLogin`;
DELIMITER $$
CREATE PROCEDURE `usp_Auth_RecordLogin`(IN p_UserId INT)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    UPDATE `Users` SET `LastLoginAt` = UTC_TIMESTAMP() WHERE `Id` = p_UserId;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_Auth_SaveRefreshToken`;
DELIMITER $$
CREATE PROCEDURE `usp_Auth_SaveRefreshToken`
(IN p_UserId INT, IN p_TokenHash VARCHAR(256), IN p_ExpiresAt DATETIME)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    INSERT INTO `RefreshTokens` (`UserId`, `TokenHash`, `ExpiresAt`, `CreatedAt`)
    VALUES (p_UserId, p_TokenHash, p_ExpiresAt, UTC_TIMESTAMP());
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_Auth_GetRefreshToken`;
DELIMITER $$
CREATE PROCEDURE `usp_Auth_GetRefreshToken`(IN p_TokenHash VARCHAR(256))
BEGIN
    SELECT `rt`.`Id` AS `TokenId`, `rt`.`UserId`, `rt`.`ExpiresAt`, `rt`.`RevokedAt`,
           `u`.`Username`, `u`.`DisplayName`, `u`.`Role`, `u`.`IsActive`
    FROM  `RefreshTokens` `rt`
    INNER JOIN `Users` `u` ON `u`.`Id` = `rt`.`UserId`
    WHERE `rt`.`TokenHash` = p_TokenHash
      AND `rt`.`RevokedAt`  IS NULL
      AND `rt`.`ExpiresAt`  > UTC_TIMESTAMP()
    LIMIT 1;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_Auth_RevokeRefreshToken`;
DELIMITER $$
CREATE PROCEDURE `usp_Auth_RevokeRefreshToken`(IN p_TokenHash VARCHAR(256))
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    UPDATE `RefreshTokens`
    SET    `RevokedAt` = UTC_TIMESTAMP()
    WHERE  `TokenHash` = p_TokenHash AND `RevokedAt` IS NULL;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_Auth_RevokeAllUserTokens`;
DELIMITER $$
CREATE PROCEDURE `usp_Auth_RevokeAllUserTokens`(IN p_UserId INT)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    UPDATE `RefreshTokens`
    SET    `RevokedAt` = UTC_TIMESTAMP()
    WHERE  `UserId`    = p_UserId AND `RevokedAt` IS NULL;
END$$
DELIMITER ;


-- ─────────────────────────────────────────────────────────────
-- SECTION 6 — Stored Procedures: Waitlist
-- ─────────────────────────────────────────────────────────────

DROP PROCEDURE IF EXISTS `usp_Waitlist_GetAll`;
DELIMITER $$
CREATE PROCEDURE `usp_Waitlist_GetAll`
(IN p_Status VARCHAR(20), IN p_RequestType VARCHAR(30), IN p_Limit INT, IN p_Offset INT)
BEGIN
    SET p_Limit  = IF(p_Limit  IS NULL OR p_Limit  = 0, 2147483647, p_Limit);
    SET p_Offset = IF(p_Offset IS NULL, 0, p_Offset);
    SELECT `Id`, `WorkcenterName`, `RequestType`, `Status`, `Priority`, `Notes`,
           `RequestedAt`, `ScheduledAt`, `CompletedAt`, `AssignedToUserId`,
           `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`
    FROM   `WaitlistEntries`
    WHERE  (p_Status      IS NULL OR `Status`      = p_Status)
      AND  (p_RequestType IS NULL OR `RequestType` = p_RequestType)
    ORDER BY `Priority` ASC, `RequestedAt` ASC
    LIMIT  p_Limit OFFSET p_Offset;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_Waitlist_GetById`;
DELIMITER $$
CREATE PROCEDURE `usp_Waitlist_GetById`(IN p_Id INT)
BEGIN
    SELECT `Id`, `WorkcenterName`, `RequestType`, `Status`, `Priority`, `Notes`,
           `RequestedAt`, `ScheduledAt`, `CompletedAt`, `AssignedToUserId`,
           `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`
    FROM   `WaitlistEntries`
    WHERE  `Id` = p_Id
    LIMIT 1;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_Waitlist_Insert`;
DELIMITER $$
CREATE PROCEDURE `usp_Waitlist_Insert`
(
    IN  p_WorkcenterName   VARCHAR(100),
    IN  p_RequestType      VARCHAR(30),
    IN  p_Status           VARCHAR(20),
    IN  p_Priority         TINYINT UNSIGNED,
    IN  p_Notes            TEXT,
    IN  p_RequestedAt      DATETIME,
    IN  p_ScheduledAt      DATETIME,
    IN  p_AssignedToUserId INT,
    IN  p_CreatedByUserId  INT,
    OUT p_NewId            INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN SET p_NewId = NULL; ROLLBACK; RESIGNAL; END;
    INSERT INTO `WaitlistEntries`
        (`WorkcenterName`,`RequestType`,`Status`,`Priority`,`Notes`,
         `RequestedAt`,`ScheduledAt`,`AssignedToUserId`,`CreatedByUserId`,`UpdatedByUserId`)
    VALUES
        (p_WorkcenterName, p_RequestType,
         IFNULL(p_Status,'Waiting'), IFNULL(p_Priority,5), p_Notes,
         IFNULL(p_RequestedAt, UTC_TIMESTAMP()), p_ScheduledAt,
         p_AssignedToUserId, p_CreatedByUserId, p_CreatedByUserId);
    SET p_NewId = LAST_INSERT_ID();
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_Waitlist_Update`;
DELIMITER $$
CREATE PROCEDURE `usp_Waitlist_Update`
(
    IN p_Id INT, IN p_WorkcenterName VARCHAR(100), IN p_RequestType VARCHAR(30),
    IN p_Status VARCHAR(20), IN p_Priority TINYINT UNSIGNED, IN p_Notes TEXT,
    IN p_RequestedAt DATETIME, IN p_ScheduledAt DATETIME, IN p_CompletedAt DATETIME,
    IN p_AssignedToUserId INT, IN p_UpdatedByUserId INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    UPDATE `WaitlistEntries`
    SET    `WorkcenterName`=p_WorkcenterName, `RequestType`=p_RequestType,
           `Status`=p_Status, `Priority`=p_Priority, `Notes`=p_Notes,
           `RequestedAt`=p_RequestedAt, `ScheduledAt`=p_ScheduledAt,
           `CompletedAt`=p_CompletedAt, `AssignedToUserId`=p_AssignedToUserId,
           `UpdatedByUserId`=p_UpdatedByUserId
    WHERE  `Id` = p_Id;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_Waitlist_Delete`;
DELIMITER $$
CREATE PROCEDURE `usp_Waitlist_Delete`(IN p_Id INT)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    DELETE FROM `WaitlistEntries` WHERE `Id` = p_Id;
END$$
DELIMITER ;


-- =============================================================
-- END OF V001__Initial_Schema
-- =============================================================

SELECT 'NOTE: Completed migration V001__Initial_Schema.' AS `MigrationNote`;
