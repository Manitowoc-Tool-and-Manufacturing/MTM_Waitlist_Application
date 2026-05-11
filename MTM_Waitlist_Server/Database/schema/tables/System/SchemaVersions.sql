-- =============================================================
-- MTM Waitlist Application -- SchemaVersions Table
-- Domain:      System
-- Description: Tracks which migration scripts have been applied.
--              Managed exclusively by the Server Admin migration runner.
--              Never modify manually.
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `SchemaVersions`
(
    `Id`            INT          NOT NULL AUTO_INCREMENT,
    `Version`       VARCHAR(20)  NOT NULL COMMENT 'Migration version string, e.g. V001',
    `Description`   VARCHAR(200) NOT NULL COMMENT 'Human-readable description from the filename',
    `Script`        VARCHAR(300) NOT NULL COMMENT 'Filename of the migration script',
    `Checksum`      VARCHAR(64)  NOT NULL COMMENT 'SHA-256 of the script file at time of apply',
    `AppliedAt`     DATETIME     NOT NULL COMMENT 'UTC timestamp when the migration was applied',
    `AppliedBy`     VARCHAR(100) NOT NULL COMMENT 'Windows username of the admin who applied it',
    `ExecutionMs`   INT          NOT NULL COMMENT 'How long the migration took in milliseconds',
    `Success`       TINYINT(1)   NOT NULL DEFAULT 1,
    `ErrorMessage`  TEXT         NULL     COMMENT 'NULL on success; error text on failure',
    CONSTRAINT `pk_SchemaVersions` PRIMARY KEY (`Id`),
    UNIQUE KEY `uq_SchemaVersions_Version` (`Version`)
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'Migration version history. Managed by Server Admin app only.';
