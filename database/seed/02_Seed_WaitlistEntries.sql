-- =============================================================
-- MTM Waitlist Application — Seed: WaitlistEntries
-- Domain:      Waitlist
-- Environment: DEVELOPMENT ONLY — DO NOT RUN IN PRODUCTION
--
-- Provides representative sample requests covering all 8 RequestTypes and
-- all 7 Status values for UI development and testing.
-- References CreatedByUserId = 1 (admin seed user from 01_Seed_Users.sql).
-- AssignedToUserId = 6 represents the seed MaterialHandler.
--
-- Requires 01_Seed_Users.sql to have run first.
-- =============================================================

USE `mtm_waitlist`;

INSERT INTO `WaitlistEntries`
(
    `WorkcenterName`, `RequestType`,        `Status`,        `Priority`,
    `Notes`,          `RequestedAt`,        `AssignedToUserId`,
    `CreatedByUserId`, `UpdatedByUserId`
)
VALUES
    -- Coil delivery — high priority, unassigned, waiting
    ('Press 3',  'Coil',              'Waiting',      1,
     'Coil #C-4412. Running low — urgent.',
     DATE_SUB(UTC_TIMESTAMP(), INTERVAL 30 MINUTE), NULL, 1, 1),

    -- Dunnage delivery — normal priority, active
    ('Press 7',  'Dunnage',           'Active',       5,
     NULL,
     DATE_SUB(UTC_TIMESTAMP(), INTERVAL 2 HOUR),    6, 1, 1),

    -- Pick up finished goods — late
    ('Line 2',   'PickUpFinishedGoods','Late',         3,
     'Full pallet waiting since morning shift.',
     DATE_SUB(UTC_TIMESTAMP(), INTERVAL 4 HOUR),    NULL, 1, 1),

    -- Pick up unused goods — low importance
    ('Press 1',  'PickUpUnusedGoods', 'LowImportance',8,
     NULL,
     DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY),     NULL, 1, 1),

    -- Pick up dunnage — completed
    ('Line 5',   'PickUpDunnage',     'Completed',    5,
     NULL,
     DATE_SUB(UTC_TIMESTAMP(), INTERVAL 2 DAY),     6, 1, 1),

    -- Bring parts to press — waiting, high priority
    ('Press 12', 'BringPartsToPress', 'Waiting',      2,
     'Part number 8840-B. 200 pieces needed.',
     DATE_SUB(UTC_TIMESTAMP(), INTERVAL 15 MINUTE), NULL, 1, 1),

    -- Remove coil from press — active
    ('Press 5',  'RemoveCoilFromPress','Active',      4,
     'Coil change in progress.',
     DATE_SUB(UTC_TIMESTAMP(), INTERVAL 45 MINUTE), 6, 1, 1),

    -- Die change — project/planned
    ('Press 3',  'BringPickUpDie',    'Project',      7,
     'Scheduled die swap for next Tuesday.',
     DATE_SUB(UTC_TIMESTAMP(), INTERVAL 3 DAY),     NULL, 1, 1),

    -- Cancelled request
    ('Line 8',   'Dunnage',           'Cancelled',    5,
     'Request withdrawn — dunnage sourced internally.',
     DATE_SUB(UTC_TIMESTAMP(), INTERVAL 5 DAY),     NULL, 1, 1);
