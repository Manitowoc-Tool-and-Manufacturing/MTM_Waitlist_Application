-- =============================================================
-- MTM Waitlist Application — V004__SetupTech_Default_DunnageTypeConfig
-- Version:     V004
-- Description: Seeds the default SetupTech dunnage-type configuration used by
--              the production server app. This is reference data required for
--              the SetupTech dunnage picker, not development-only sample data.
--              Existing rows are preserved so current server-side admin
--              configuration is never overwritten by reapplying the migration.
--
-- Usage:
--   mysql -h 172.16.1.104 -u <admin_user> -p < migrations/V004__SetupTech_Default_DunnageTypeConfig.sql
--
-- ROLLBACK: Delete only the seeded SetupTechDunnageTypeConfig rows by
--           DunnageTypeId after confirming no admin customizations should be kept.
-- =============================================================

USE `mtm_waitlist`;

INSERT IGNORE INTO `SetupTechDunnageTypeConfig`
    (`DunnageTypeId`, `DunnageTypeName`,                    `IsEnabled`, `DisplayOrder`)
VALUES
    (1,  'Pallets / Skids',                      1,  1),
    (2,  'Cardboard Sheets / Slip Sheets',       1,  2),
    (3,  'Corrugated Boxes',                     1,  3),
    (4,  'Gaylords / Bulk Bins',                 1,  4),
    (5,  'Stretch Film / Shrink Wrap',           0, 105),
    (6,  'Bags',                                 0, 106),
    (7,  'Tape / Strapping / Banding',           0, 107),
    (8,  'Edge Protectors',                      0, 108),
    (9,  'Foam / Molded Inserts',                0, 109),
    (10, 'Returnable Racks - John Deere',        0, 110),
    (11, 'Returnable Racks - Other',             0, 111),
    (12, 'Returnable Totes',                     1,  5),
    (13, 'Returnable Baskets / Wire Containers', 1,  6);