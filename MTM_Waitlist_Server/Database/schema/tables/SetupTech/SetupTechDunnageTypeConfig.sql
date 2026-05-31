-- =============================================================
-- MTM Waitlist Application — SetupTechDunnageTypeConfig Table
-- Domain:      SetupTech
-- Description: Controls which receiving-app dunnage types appear in the
--              SetupTech UI and the order in which they are displayed.
-- Depends on:  schema/00_Database.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `SetupTechDunnageTypeConfig`
(
    `Id`              INT          NOT NULL AUTO_INCREMENT
                                     COMMENT 'Surrogate primary key.',

    `DunnageTypeId`   INT          NOT NULL
                                     COMMENT 'Mirror of mtm_receiving_application.dunnage_types.id.',

    `DunnageTypeName` VARCHAR(100) NOT NULL
                                     COMMENT 'Denormalized receiving-app type name for offline display.',

    `IsEnabled`       TINYINT(1)   NOT NULL DEFAULT 1
                                     COMMENT '1 = show in SetupTech UI, 0 = hide.',

    `DisplayOrder`    INT          NOT NULL DEFAULT 99
                                     COMMENT 'Sort order for category tabs in the SetupTech dunnage picker.',

    `CreatedAt`       DATETIME     NOT NULL
                                     COMMENT 'UTC - set by trg_SetupTechDunnageTypeConfig_BeforeInsert.',

    `UpdatedAt`       DATETIME     NOT NULL
                                     COMMENT 'UTC - updated by trg_SetupTechDunnageTypeConfig_BeforeUpdate.',

    CONSTRAINT `pk_SetupTechDunnageTypeConfig`
        PRIMARY KEY (`Id`),

    CONSTRAINT `uq_SetupTechDunnageTypeConfig_TypeId`
        UNIQUE (`DunnageTypeId`)
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'SetupTech UI filter configuration for receiving-app dunnage types.';