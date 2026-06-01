-- =============================================================
-- MTM Waitlist Application -- SchemaVersions PreExisting Bootstrap
-- Domain:      System
-- Description: Backfills the V001 history row when the Server Admin app
--              discovers a pre-existing schema that predates the migration
--              tracking table.
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

INSERT IGNORE INTO `SchemaVersions`
    (`Version`, `Description`, `Script`, `Checksum`, `AppliedAt`, `AppliedBy`, `ExecutionMs`, `Success`, `ErrorMessage`)
VALUES
    ('V001', 'Initial_Schema', 'V001__Initial_Schema.sql',
     'bootstrapped-pre-tracking', UTC_TIMESTAMP(), 'system', 0, 1, NULL);

SELECT 'NOTE: Completed SchemaVersions pre-existing bootstrap for V001.' AS `MigrationNote`;