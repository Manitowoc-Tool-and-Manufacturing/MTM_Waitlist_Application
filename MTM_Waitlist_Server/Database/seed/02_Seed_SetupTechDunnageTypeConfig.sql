-- =============================================================
-- MTM Waitlist Application — Seed: SetupTechDunnageTypeConfig
-- Domain:      SetupTech
-- Environment: DEVELOPMENT ONLY — DO NOT RUN IN PRODUCTION
-- Description: Seeds the SetupTech dunnage-type filter table from the
--              receiving-app type list without deleting existing rows.
--              Enabled by default: 1-4, 12-13.
--              Production/default server data now ships through
--              migrations/V004__SetupTech_Default_DunnageTypeConfig.sql.
-- Depends on:  schema/tables/SetupTech/SetupTechDunnageTypeConfig.sql
-- =============================================================

USE `mtm_waitlist`;

INSERT INTO `SetupTechDunnageTypeConfig`
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
    (13, 'Returnable Baskets / Wire Containers', 1,  6)
ON DUPLICATE KEY UPDATE
    `DunnageTypeName` = VALUES(`DunnageTypeName`),
    `IsEnabled`       = VALUES(`IsEnabled`),
    `DisplayOrder`    = VALUES(`DisplayOrder`);