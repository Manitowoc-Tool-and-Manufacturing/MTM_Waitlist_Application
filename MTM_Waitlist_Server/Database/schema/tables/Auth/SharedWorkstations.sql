-- =============================================================
-- MTM Waitlist Application — SharedWorkstations Table
-- Domain:      Auth
-- Description: Stores the Windows usernames of shared PCs (kiosks, floor
--              terminals, etc.) that require users to enter app credentials
--              on login. Personal workstations whose Windows username is NOT
--              in this table trigger automatic login via Users.WindowsUsername.
--
-- Login flow:
--   1. App reads current Windows username.
--   2. Call usp_Auth_CheckSharedWorkstation(windowsUsername).
--   3a. Row returned  → show login form → usp_Auth_ValidateCredentials.
--   3b. No row        → auto-login via usp_Auth_GetUserByWindowsUsername.
--
-- Depends on:  `mtm_waitlist` already created and selected.
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `SharedWorkstations`
(
    `Id`              INT          NOT NULL AUTO_INCREMENT
                                   COMMENT 'Surrogate primary key.',

    `WindowsUsername` VARCHAR(100) NOT NULL
                                   COMMENT 'The Windows login name of the shared PC or kiosk (e.g., DOMAIN\PRESS3-PC or PRESS3-PC$). Must be unique — one row per shared machine.',

    `MachineName`     VARCHAR(150) NULL
                                   COMMENT 'Optional human-readable label for the workstation (e.g., Press 3 Floor Terminal). Used for display in the admin UI.',

    `Notes`           TEXT         NULL
                                   COMMENT 'Admin notes about this workstation (location, responsible supervisor, etc.).',

    `IsActive`        TINYINT(1)   NOT NULL
                                   DEFAULT 1
                                   COMMENT '1 = workstation is active and forces credential login. 0 = disabled; treat as a personal workstation.',

    `CreatedAt`       DATETIME     NOT NULL
                                   COMMENT 'UTC — set automatically by trg_SharedWorkstations_BeforeInsert.',

    `UpdatedAt`       DATETIME     NOT NULL
                                   COMMENT 'UTC — updated automatically by trg_SharedWorkstations_BeforeUpdate.',

    CONSTRAINT `pk_SharedWorkstations`                  PRIMARY KEY (`Id`),
    CONSTRAINT `uq_SharedWorkstations_WindowsUsername`  UNIQUE      (`WindowsUsername`)
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'Shared/kiosk PCs that require manual credential login instead of Windows auto-login.';
