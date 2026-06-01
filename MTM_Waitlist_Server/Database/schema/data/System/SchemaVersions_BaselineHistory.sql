-- =============================================================
-- MTM Waitlist Application -- SchemaVersions Baseline History
-- Domain:      System
-- Description: Backfills the baseline migration history rows after a full
--              destructive reinstall driven by schema/00_Database.bat.
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

INSERT INTO `SchemaVersions`
    (`Version`, `Description`, `Script`, `Checksum`, `AppliedAt`, `AppliedBy`, `ExecutionMs`, `Success`, `ErrorMessage`)
VALUES
    ('V002', 'Add_SchemaVersions_Table', 'V002__Add_SchemaVersions_Table.sql', 'manual-master-reinstall', UTC_TIMESTAMP(), 'manual-master-script', 0, 1, NULL),
    ('V003', 'SetupTech_Schema', 'V003__SetupTech_Schema.sql', 'manual-master-reinstall', UTC_TIMESTAMP(), 'manual-master-script', 0, 1, NULL),
    ('V004', 'SetupTech_Default_DunnageTypeConfig', 'V004__SetupTech_Default_DunnageTypeConfig.sql', 'manual-master-reinstall', UTC_TIMESTAMP(), 'manual-master-script', 0, 1, NULL)
ON DUPLICATE KEY UPDATE
    `Description` = VALUES(`Description`),
    `Script` = VALUES(`Script`),
    `Checksum` = VALUES(`Checksum`),
    `AppliedAt` = VALUES(`AppliedAt`),
    `AppliedBy` = VALUES(`AppliedBy`),
    `ExecutionMs` = VALUES(`ExecutionMs`),
    `Success` = VALUES(`Success`),
    `ErrorMessage` = VALUES(`ErrorMessage`);

SELECT 'NOTE: Completed SchemaVersions baseline history backfill.' AS `MigrationNote`;