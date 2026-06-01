-- =============================================================
-- MTM Waitlist Application -- V002
-- Description: Adds the SchemaVersions tracking table to databases
--              that were created with V001 before the migration
--              system was introduced.
--              This file is intentionally short -- the canonical
--              table definition lives in
--              schema/tables/System/SchemaVersions.sql.
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `SchemaVersions`
(
    `Id`            INT          NOT NULL AUTO_INCREMENT,
    `Version`       VARCHAR(20)  NOT NULL,
    `Description`   VARCHAR(200) NOT NULL,
    `Script`        VARCHAR(300) NOT NULL,
    `Checksum`      VARCHAR(64)  NOT NULL,
    `AppliedAt`     DATETIME     NOT NULL,
    `AppliedBy`     VARCHAR(100) NOT NULL,
    `ExecutionMs`   INT          NOT NULL,
    `Success`       TINYINT(1)   NOT NULL DEFAULT 1,
    `ErrorMessage`  TEXT         NULL,
    CONSTRAINT `pk_SchemaVersions` PRIMARY KEY (`Id`),
    UNIQUE KEY `uq_SchemaVersions_Version` (`Version`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Backfill: record V001 as already applied on this database.
-- V001 was applied before the tracking table existed.
INSERT IGNORE INTO `SchemaVersions`
    (`Version`, `Description`, `Script`, `Checksum`, `AppliedAt`, `AppliedBy`, `ExecutionMs`, `Success`)
VALUES
    ('V001', 'Initial_Schema', 'V001__Initial_Schema.sql',
     'bootstrapped-pre-tracking', UTC_TIMESTAMP(), 'system', 0, 1);

SELECT 'NOTE: Completed migration V002__Add_SchemaVersions_Table.' AS `MigrationNote`;
