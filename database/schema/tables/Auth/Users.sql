-- =============================================================
-- MTM Waitlist Application — Users Table
-- Domain:      Auth
-- Description: Application users who log in and manage waitlist entries.
--              Supports two login paths:
--                1. Shared Workstation — Windows username is in SharedWorkstations;
--                   user must enter app credentials (Username + PasswordHash).
--                2. Personal Workstation — Windows username is NOT in SharedWorkstations;
--                   app auto-logs in by matching WindowsUsername to this table.
-- Depends on:  schema/00_Database.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `Users`
(
    `Id`              INT          NOT NULL AUTO_INCREMENT
                                   COMMENT 'Surrogate primary key.',

    `WindowsUsername` VARCHAR(100) NULL
                                   COMMENT 'Active Directory / Windows login name (e.g. DOMAIN\jsmith or jsmith). Used for auto-login on personal workstations. NULL for users who only log in from shared workstations via credentials.',

    `Username`        VARCHAR(100) NOT NULL
                                   COMMENT 'App-level login name used on shared workstations. Always required even for auto-login users in case they sit at a shared workstation.',

    `PasswordHash`    VARCHAR(256) NOT NULL
                                   COMMENT 'bcrypt hash of the user password. Never store plaintext. The API compares hashes — MySQL never sees the raw password.',

    `DisplayName`     VARCHAR(200) NOT NULL
                                   COMMENT 'Human-readable full name shown in the UI.',

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
            )                      NOT NULL
                                   DEFAULT 'PressOperation'
                                   COMMENT 'Access level. Controls which actions and views are available in the MAUI application.',

    `IsActive`        TINYINT(1)   NOT NULL
                                   DEFAULT 1
                                   COMMENT '1 = account is enabled; 0 = deactivated (soft-delete).',

    `LastLoginAt`     DATETIME     NULL
                                   COMMENT 'UTC — updated by usp_Auth_RecordLogin on successful authentication.',

    `CreatedAt`       DATETIME     NOT NULL
                                   COMMENT 'UTC — set automatically by trg_Users_BeforeInsert.',

    `UpdatedAt`       DATETIME     NOT NULL
                                   COMMENT 'UTC — updated automatically by trg_Users_BeforeUpdate.',

    CONSTRAINT `pk_Users`                  PRIMARY KEY (`Id`),
    CONSTRAINT `uq_Users_WindowsUsername`  UNIQUE      (`WindowsUsername`),
    CONSTRAINT `uq_Users_Username`         UNIQUE      (`Username`)
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'Application users. Supports both Windows auto-login and shared-workstation credential login.';
