-- =============================================================
-- MTM Waitlist Application — Seed: WaitlistEntries
-- Domain:      Waitlist
-- Environment: DEVELOPMENT ONLY — DO NOT RUN IN PRODUCTION
--
-- Provides representative sample requests covering all 8 RequestTypes and
-- all 7 Status values for UI development and testing.
-- Resolves CreatedByUserId dynamically from the seeded admin user when present.
-- Resolves AssignedToUserId dynamically from the first active MaterialHandler when present.
-- If either user is missing, the nullable FK columns fall back to NULL.
--
-- If the admin or MaterialHandler users are absent, the nullable user FK
-- columns fall back to NULL so the sample waitlist rows can still load.
-- =============================================================

USE `mtm_waitlist`;

SET @SeedCreatedByUserId = (
    SELECT `Id`
    FROM `Users`
    WHERE `Username` = 'admin'
    ORDER BY `Id`
    LIMIT 1
);

SET @SeedAssignedToUserId = (
    SELECT `Id`
    FROM `Users`
    WHERE `Role` = 'MaterialHandler'
      AND `IsActive` = 1
    ORDER BY `Id`
    LIMIT 1
);

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
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 30 MINUTE), NULL, @SeedCreatedByUserId, @SeedCreatedByUserId),

    -- Dunnage delivery — normal priority, active
    ('Press 7',  'Dunnage',           'Active',       5,
     NULL,
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 2 HOUR),    @SeedAssignedToUserId, @SeedCreatedByUserId, @SeedCreatedByUserId),

    -- Pick up finished goods — late
    ('Line 2',   'PickUpFinishedGoods','Late',         3,
     'Full pallet waiting since morning shift.',
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 4 HOUR),    NULL, @SeedCreatedByUserId, @SeedCreatedByUserId),

    -- Pick up unused goods — low importance
    ('Press 1',  'PickUpUnusedGoods', 'LowImportance',8,
     NULL,
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY),     NULL, @SeedCreatedByUserId, @SeedCreatedByUserId),

    -- Pick up dunnage — completed
    ('Line 5',   'PickUpDunnage',     'Completed',    5,
     NULL,
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 2 DAY),     @SeedAssignedToUserId, @SeedCreatedByUserId, @SeedCreatedByUserId),

    -- Bring parts to press — waiting, high priority
    ('Press 12', 'BringPartsToPress', 'Waiting',      2,
     'Part number 8840-B. 200 pieces needed.',
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 15 MINUTE), NULL, @SeedCreatedByUserId, @SeedCreatedByUserId),

    -- Remove coil from press — active
    ('Press 5',  'RemoveCoilFromPress','Active',      4,
     'Coil change in progress.',
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 45 MINUTE), @SeedAssignedToUserId, @SeedCreatedByUserId, @SeedCreatedByUserId),

    -- Die change — project/planned
    ('Press 3',  'BringPickUpDie',    'Project',      7,
     'Scheduled die swap for next Tuesday.',
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 3 DAY),     NULL, @SeedCreatedByUserId, @SeedCreatedByUserId),

    -- Cancelled request
    ('Line 8',   'Dunnage',           'Cancelled',    5,
     'Request withdrawn — dunnage sourced internally.',
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 5 DAY),     NULL, @SeedCreatedByUserId, @SeedCreatedByUserId);
