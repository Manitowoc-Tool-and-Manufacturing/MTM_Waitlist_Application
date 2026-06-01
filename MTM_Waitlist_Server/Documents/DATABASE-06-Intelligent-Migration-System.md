# DATABASE-06: Intelligent Migration System

---

## Confirmed Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Roll-forward only, or rollback support? | **Roll-forward only.** Bad migrations are corrected by restoring from backup (DATABASE-04) and applying a fixed migration file. Destructive rollback is never needed if migrations are tested in dev first |
| 2 | Who runs migrations in production? | **Both** — auto-apply on startup (controlled by `Migrations:AutoApplyOnStartup` in Settings). IT admin can also press "Apply Migrations" manually at any time. `AutoApplyOnStartup` defaults to `false` in production so IT retains explicit control |
| 3 | Always re-run procedures/triggers/indexes? | **Yes — always re-run.** They are idempotent (`DROP IF EXISTS + CREATE`). Only table migrations are gated by the `SchemaVersions` check. Overhead is ~500ms and is acceptable |
| 4 | Migration file source in production? | **Disk, next to the exe** — `database\migrations` folder is deployed alongside the admin app binary. Files are read from disk so a developer can update them without recompiling |
| 5 | V001 monolithic file handling? | **Retired as bootstrap only.** V001 is frozen — never edit it. If `SchemaVersions` does not exist, V001 runs first. All future changes go in numbered files starting at V003 |

---

## Overview

The current approach produces a single `V001__Initial_Schema.sql` that creates the entire schema from scratch. This works for the first deploy but is unusable for incremental updates — every schema change would require either editing V001 (dangerous) or dropping and recreating the database (destroys data).

The intelligent migration system replaces this with:

1. A `SchemaVersions` tracking table in MySQL.
2. Numbered migration files in `Database/migrations/` that capture **only the changes** (ALTER TABLE, new columns, new tables) — never the full schema.
3. A migration runner in the admin app that applies only the unapplied migrations, in order.
4. A separate, always-re-run pass for procedures, triggers, and indexes (idempotent).

This means a developer adding a new column:
- Writes `V003__Add_CanKeepRunning_Column.sql` with a single `ALTER TABLE` statement.
- Checks it in.
- Deploys the new admin app build.
- Presses "Apply Migrations" (or it auto-applies on startup).
- Done — no data loss, no full wipe.

---

## `SchemaVersions` Table

Add to `Database/schema/tables/System/SchemaVersions.sql`:

```sql
-- =============================================================
-- MTM Waitlist Application — SchemaVersions Table
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
```

---

## Migration File Naming Convention

```
V<NNN>__<Description>.sql
```

- `V` — literal prefix, always uppercase.
- `<NNN>` — zero-padded 3-digit version (001, 002, ..., 099, 100, ...).
- `__` — double underscore separator (matches Flyway convention for familiarity).
- `<Description>` — snake_case description, no spaces.

Examples:
```
V001__Initial_Schema.sql             ← existing bootstrap (bootstrap only — never edit)
V002__Add_SchemaVersions_Table.sql   ← adds the tracking table to existing installs
V003__Add_CanKeepRunning_Column.sql  ← ALTER TABLE WaitlistEntries ADD COLUMN
V004__Add_Zones_Table.sql            ← CREATE TABLE Zones, WorkcenterZones
V005__Add_PressAssignments_Table.sql ← CREATE TABLE PressAssignments
```

---

## What Goes in a Migration File vs. Individual Object Files

| Object type | Where defined | Migration runner behavior |
|---|---|---|
| New table | New `VN__<description>.sql` in `migrations/` | Applied once, tracked in `SchemaVersions` |
| Alter existing table (new column, change type) | New `VN__<description>.sql` in `migrations/` | Applied once, tracked in `SchemaVersions` |
| Stored procedure | `procedures/<Domain>/usp_<Domain>_<Action>.sql` | Always re-run (DROP IF EXISTS + CREATE) |
| Trigger | `triggers/<Domain>/trg_<Table>_<Timing><Event>.sql` | Always re-run (DROP IF EXISTS + CREATE) |
| Index | `indexes/<Domain>/<Table>_Indexes.sql` | Always re-run (DROP INDEX IF EXISTS + CREATE) |
| Seed data | `seed/<N>_Seed_<Table>.sql` | Manual only — never auto-applied in production |

**Critical rule:** Individual table definition files in `Database/schema/tables/` are **no longer run by the migration runner**. They are the reference/documentation for what the table *should* look like, updated by the developer to reflect the current schema. Only migration files change the live database.

---

## How to Write Migration Files

### Adding a new column

```sql
-- V003__Add_CanKeepRunning_Column.sql
-- =============================================================
-- MTM Waitlist Application — V003
-- Description: Add CanKeepRunning column to WaitlistEntries.
--              Tracks whether a press operator can continue running
--              while waiting for a quality inspection.
-- Depends on:  V001 (WaitlistEntries table must exist)
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

-- Check that the column doesn't already exist before adding (idempotent guard)
SET @col_exists = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = 'mtm_waitlist'
      AND TABLE_NAME   = 'WaitlistEntries'
      AND COLUMN_NAME  = 'CanKeepRunning'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE `WaitlistEntries` ADD COLUMN `CanKeepRunning` TINYINT(1) NULL DEFAULT NULL COMMENT ''Operator can continue pressing while awaiting quality inspection. NULL = not applicable.''',
    'SELECT ''Column CanKeepRunning already exists — skipped'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
```

The idempotent guard (check `information_schema.COLUMNS` first) ensures that if a migration is accidentally run twice, it does not fail.

### Adding a new table

```sql
-- V004__Add_Zones_Table.sql
-- =============================================================
-- MTM Waitlist Application — V004
-- Description: Add Zones and WorkcenterZones tables for material
--              handler zone assignment (FEATURE-05).
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `Zones`
(
    `Id`        INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `ZoneName`  VARCHAR(50)  NOT NULL,
    `SiteId`    VARCHAR(20)  NOT NULL,
    `CreatedAt` DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    `UpdatedAt` DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    CONSTRAINT `pk_Zones` PRIMARY KEY (`Id`),
    UNIQUE KEY `uq_Zones_ZoneName_SiteId` (`ZoneName`, `SiteId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `WorkcenterZones`
(
    `Id`          INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `WorkcenterId` VARCHAR(100) NOT NULL,
    `ZoneId`      INT UNSIGNED NOT NULL,
    `CreatedAt`   DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    `UpdatedAt`   DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    CONSTRAINT `pk_WorkcenterZones` PRIMARY KEY (`Id`),
    UNIQUE KEY `uq_WorkcenterZones_Workcenter_Zone` (`WorkcenterId`, `ZoneId`),
    CONSTRAINT `fk_WorkcenterZones_Zone`
        FOREIGN KEY (`ZoneId`) REFERENCES `Zones` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

`CREATE TABLE IF NOT EXISTS` is already idempotent — no extra guard needed for table creation.

---

## Migration Runner Logic

```csharp
public async Task<MigrationResult> ApplyPendingMigrationsAsync(IProgress<MigrationProgress> progress, CancellationToken ct)
{
    // Step 0 — Acquire advisory lock to prevent concurrent migration runs
    // (e.g., dev and prod admin apps both pointed at the same server)
    var lockAcquired = await AcquireMigrationLockAsync(ct);  // SELECT GET_LOCK('mtm_migration_lock', 30)
    if (!lockAcquired)
    {
        return MigrationResult.Failed("lock", "Another migration run is already in progress on this server. Try again in 30 seconds.");
    }

    try
    {
    // Step 1 — Bootstrap: if SchemaVersions doesn't exist, run V001 first
    if (!await SchemaVersionsTableExistsAsync())
    {
        await ApplyBootstrapAsync(progress, ct);  // runs V001__Initial_Schema.sql
    }

    // Step 2 — Load all migration files from disk, sorted by version
    var allMigrations = LoadMigrationFiles();  // scans Database/migrations/*.sql, sorted by V number

    // Step 3 — Load applied versions from SchemaVersions table
    var appliedVersions = await GetAppliedVersionsAsync();

    // Step 4 — Determine pending migrations
    var pending = allMigrations
        .Where(m => !appliedVersions.Contains(m.Version))
        .OrderBy(m => m.VersionNumber)
        .ToList();

    // Step 5 — Apply each pending migration in a transaction
    foreach (var migration in pending)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            progress.Report(new MigrationProgress(migration.Version, "Applying..."));
            await ExecuteMigrationScriptAsync(migration.Script, ct);
            await RecordAppliedVersionAsync(migration, sw.ElapsedMilliseconds, ct);
            progress.Report(new MigrationProgress(migration.Version, "✅ Applied"));
        }
        catch (Exception ex)
        {
            await RecordFailedVersionAsync(migration, ex.Message, sw.ElapsedMilliseconds, ct);
            progress.Report(new MigrationProgress(migration.Version, $"❌ Failed: {ex.Message}"));
            return MigrationResult.Failed(migration.Version, ex.Message);  // stop on first failure
        }
    }

    // Step 6 — Always re-run procedures, triggers, and indexes
    await RerunIdempotentObjectsAsync(progress, ct);

    return MigrationResult.Success(pending.Count);
    }
    finally
    {
        await ReleaseMigrationLockAsync();  // SELECT RELEASE_LOCK('mtm_migration_lock')
    }
}
```

### Transaction Scope

Each migration script runs in its own transaction:
- `START TRANSACTION; <script content>; COMMIT;`
- On error: `ROLLBACK;` then record failure in `SchemaVersions`.
- The migration runner stops at the first failure — does not skip ahead to later migrations.

Note: DDL statements (`ALTER TABLE`, `CREATE TABLE`) in MySQL cause an implicit commit — they cannot be truly rolled back. This is a MySQL limitation. The idempotent guards in each migration file protect against partial-apply retries.

---

## Always-Rerun Objects Pass

After all pending migrations are applied, the runner executes all procedure/trigger/index files in a fixed order:

```
Order:
1. Database/procedures/**/*.sql    (alphabetical within each domain, domain order: Auth → Waitlist → ...)
2. Database/triggers/**/*.sql      (alphabetical)
3. Database/indexes/**/*.sql       (alphabetical)
```

These files already use `DROP PROCEDURE IF EXISTS` / `DROP TRIGGER IF EXISTS` / `DROP INDEX IF EXISTS`, making them safe to run on every migration pass.

This ensures that even without a new migration file, simply redeploying the admin app with updated procedure files will apply the latest procedure definitions.

---

## Migration Admin UI

```
┌─────────────────────────────────────────────────────────────────┐
│  MIGRATIONS                          [Apply Pending]  [↻]       │
├─────────────────────────────────────────────────────────────────┤
│  PENDING MIGRATIONS (2)                                         │
│  ┌────────┬────────────────────────────────────┬──────────────┐ │
│  │ V003   │ Add_CanKeepRunning_Column          │ [Preview SQL]│ │
│  │ V004   │ Add_Zones_Table                    │ [Preview SQL]│ │
│  └────────┴────────────────────────────────────┴──────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│  APPLIED MIGRATIONS (2)                                         │
│  ┌────────┬──────────────────────────┬──────────────┬────────┐  │
│  │ V001   │ Initial_Schema           │ 05/01 09:00  │  432ms │  │
│  │ V002   │ Add_SchemaVersions_Table │ 05/05 10:15  │  12ms  │  │
│  └────────┴──────────────────────────┴──────────────┴────────┘  │
├─────────────────────────────────────────────────────────────────┤
│  ALWAYS-RERUN OBJECTS (last run: 10:14:02)                      │
│  Procedures: 13 files  ✅  Triggers: 6 files  ✅  Indexes: 4 ✅ │
├─────────────────────────────────────────────────────────────────┤
│  [View Migration Log]   [Run Idempotent Objects Only]           │
└─────────────────────────────────────────────────────────────────┘
```

**"Preview SQL"** button opens a read-only text viewer showing the content of the migration file before applying it.

**"Run Idempotent Objects Only"** re-runs all procedure/trigger/index files without touching the migration table — useful when only a stored procedure changed and no migration version is needed.

---

## Developer Workflow for Schema Changes

### Adding a new feature table (e.g., Zones from FEATURE-05)

1. **Write the migration:** Create `Database/migrations/V004__Add_Zones_Table.sql` with `CREATE TABLE IF NOT EXISTS` statements.
2. **Update the schema reference file:** Create `Database/schema/tables/Waitlist/Zones.sql` with the canonical `CREATE TABLE` definition. This is the "what it looks like now" reference — it is NOT run by the migration runner.
3. **Run in dev:** Press "Apply Migrations" in the admin app (or restart with auto-apply on).
4. **Verify:** The V004 row appears in `SchemaVersions`. The new table exists.
5. **Check in:** Commit both the migration file and the schema reference file together.

### Modifying an existing stored procedure

1. **Edit the procedure file:** `Database/procedures/Waitlist/usp_Waitlist_GetAll.sql` — update the SQL.
2. **No migration file needed** — procedure files are always re-run.
3. **Restart the admin app** (auto-apply mode) or click "Run Idempotent Objects Only".
4. **Check in:** Commit the updated procedure file.

### Adding a new column to an existing table

1. **Write the migration:** Create `Database/migrations/V005__Add_SomeColumn_To_WaitlistEntries.sql` with an idempotent `ALTER TABLE` statement (using the `information_schema` guard from the example above).
2. **Update the schema reference file:** Add the column definition to `Database/schema/tables/Waitlist/WaitlistEntries.sql` to keep the reference current.
3. **Run in dev, verify, check in.**

---

## Changes to Existing SQL File Structure

The following changes are needed to existing database files to support this system:

| Change | Action |
|---|---|
| `V001__Initial_Schema.sql` | Freeze — never edit again. It is the bootstrap for brand-new installs. |
| New file: `V002__Add_SchemaVersions_Table.sql` | For existing installs that have V001 applied but no `SchemaVersions` table. |
| All procedure files | Verify each starts with `DROP PROCEDURE IF EXISTS` — already done. |
| All trigger files | Verify each starts with `DROP TRIGGER IF EXISTS` — already done. |
| All index files | Convert any `CREATE INDEX` to `CREATE INDEX IF NOT EXISTS` (MySQL 5.7 does not support `IF NOT EXISTS` on indexes — use a stored procedure workaround or check `information_schema` first). |
| New file: `Database/schema/tables/System/SchemaVersions.sql` | Add the tracking table definition. |

### Index Idempotency Workaround (MySQL 5.7)

MySQL 5.7 does not support `CREATE INDEX IF NOT EXISTS`. The existing index files must use a procedure-based workaround:

```sql
-- Example: Users_Indexes.sql (updated pattern)
DROP PROCEDURE IF EXISTS `_tmp_AddIndex`;
DELIMITER $$
CREATE PROCEDURE `_tmp_AddIndex`()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE table_schema = 'mtm_waitlist'
          AND table_name   = 'Users'
          AND index_name   = 'idx_Users_Role'
    ) THEN
        ALTER TABLE `Users` ADD INDEX `idx_Users_Role` (`Role`);
    END IF;
END$$
DELIMITER ;
CALL `_tmp_AddIndex`();
DROP PROCEDURE IF EXISTS `_tmp_AddIndex`;
```

This pattern already exists in `MTM.DatabaseDeployment.Tooling` — carry it forward.

---

## `IService_Migration` Interface

```csharp
// Core/Interfaces/Migration/IService_Migration.cs
public interface IService_Migration
{
    Task<bool> SchemaVersionsTableExistsAsync(CancellationToken ct = default);
    Task<MigrationResult> ApplyPendingMigrationsAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default);
    Task<RerunResult> RerunIdempotentObjectsAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default);
    Task<IReadOnlyList<AppliedMigration>> GetAppliedMigrationsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PendingMigration>> GetPendingMigrationsAsync(CancellationToken ct = default);
    string PreviewMigrationSql(string version);
}
```

---

## New Core Models

```
Core/Models/Migration/
  AppliedMigration.cs      ← Version, Description, Script, AppliedAt, AppliedBy, ExecutionMs, Success
  PendingMigration.cs      ← Version, VersionNumber, Description, Script, FilePath
  MigrationResult.cs       ← IsSuccess, MigrationsApplied, FailedVersion, ErrorMessage
  MigrationProgress.cs     ← Version, Message, IsComplete
  RerunResult.cs           ← ProceduresApplied, TriggersApplied, IndexesApplied, ErrorMessages[]
```

---

## ViewModel

```
Module.Migrations/
  ViewModels/
    ViewModel_Migrations.cs
  Views/
    View_Migrations.xaml
    View_Migrations.xaml.cs
```

---

## V002 File — Bootstrap for Existing Installs

This file must be created now and committed with the migration system:

```sql
-- V002__Add_SchemaVersions_Table.sql
-- =============================================================
-- MTM Waitlist Application — V002
-- Description: Adds the SchemaVersions tracking table to databases
--              that were created with V001 before the migration
--              system was introduced.
--              This file is intentionally short — the full table
--              definition lives in schema/tables/System/SchemaVersions.sql.
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

-- Backfill: record V001 as already applied on this database
-- (It was applied before the tracking table existed)
INSERT IGNORE INTO `SchemaVersions`
    (`Version`, `Description`, `Script`, `Checksum`, `AppliedAt`, `AppliedBy`, `ExecutionMs`, `Success`)
VALUES
    ('V001', 'Initial_Schema', 'V001__Initial_Schema.sql',
     'bootstrapped-pre-tracking', UTC_TIMESTAMP(), 'system', 0, 1);
```

---

## Open Decisions

- **Maximum migration file size:** Very large migrations (e.g., backfilling data into a new column for thousands of rows) may time out. For v1, use the default `CommandTimeout` from Settings (30 seconds). If a migration is known to be slow, it should be split into smaller versions.
