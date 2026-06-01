# Deterministic Migration Validation and Safe Table Repair Specification

## Purpose

The current migration feature has repeatedly failed because table validation is based on fragile source-SQL-to-live-metadata signature comparisons. Small MySQL metadata differences such as identifier casing, default literal casing, foreign key action formatting, and parser edge cases can cause false drift after a table was successfully created.

This update replaces the fragile behavior with a deterministic migration validation and safe repair workflow:

1. Parse expected table definitions from checked-in SQL files.
2. Query live MySQL metadata for the configured database.
3. Compare exact normalized schema objects using deterministic rules only.
4. Return structured mismatch details that the app can display and act on.
5. When a table exists but mismatches, safely back up data only, rebuild the table from the canonical SQL file, restore compatible data, and delete the temporary backup.

No fuzzy matching should be used.

## Scope

### In Scope

- Table validation derived from files under `Database/schema/tables/**/*.sql`.
- Live metadata validation against `information_schema` for the configured database.
- Structured mismatch results for table, column, primary key, unique constraint, and foreign key differences.
- Safe table repair for existing mismatched tables:
  - Create a temporary data-only backup table.
  - Drop the mismatched table.
  - Recreate the table using the canonical SQL file.
  - Restore compatible data from the temporary backup.
  - Drop the temporary backup after successful restore.
- Integration with the existing migration UI through `IService_Migration` / `Service_Migration`.
- Unit/integration-style tests around the actual migration comparison and repair code.
- Deterministic SQL parsing for supported canonical SQL file patterns.

### Out of Scope

- Fuzzy matching of column names, table names, constraints, or types.
- Guessing renamed columns or trying to infer semantic equivalence.
- Destructive repair without a data-only backup.
- Cross-database schema synchronization beyond the configured `mtm_waitlist` database.
- Automatic recovery if restore fails after destructive DDL, beyond preserving the temporary backup for manual recovery.
- Full SQL grammar parsing for arbitrary SQL. Only the repository's canonical schema SQL format is required.

## Guiding Principles

1. **Deterministic only**
   - Expected schema comes from SQL files.
   - Actual schema comes from MySQL metadata.
   - Comparison uses exact normalized values.
   - No approximate, fuzzy, or heuristic matching.

2. **Structured mismatch reporting**
   - The app should not receive plain text only.
   - Each mismatch should include object type, object name, field/property, expected value, actual value, severity, and recommended action.

3. **Safe before destructive**
   - If a table exists and must be rebuilt, back up data first.
   - The backup must be data-only and temporary.
   - The backup must remain if any step after backup fails.
   - Delete the backup only after successful restore verification.

4. **Canonical SQL remains the source of truth**
   - Table creation uses the checked-in SQL file.
   - The live database is repaired to match the canonical table definition, not vice versa.

5. **Explicit restore behavior**
   - Restore only columns that exist in both backup and recreated table.
   - If a new NOT NULL column has no default and is not in backup, repair must fail with a structured error before dropping the backup.
   - If incompatible data fails constraints, repair must stop and preserve the backup.

## Current Problem Summary

The existing migration service scans SQL definitions and compares them to live database metadata. Repeated failures occurred because the comparison was too fragile:

- `Users.sql` initially failed because the SQL splitter split on a semicolon inside a quoted `COMMENT` string.
- `RefreshTokens.sql` was created correctly but mismatched due to foreign key identifier casing and referential action formatting.
- `WaitlistEntries.sql` was created correctly but mismatched due to enum default casing differences between source SQL and MySQL metadata.
- Other tables are likely to expose similar source-vs-live metadata formatting differences.

This indicates the migration feature needs a clearer contract and a robust validation/repair engine.

## Target Architecture

### Main Components

#### `Service_Migration`

Existing migration orchestration service.

Responsibilities after this update:

- Discover schema definition files.
- Call the deterministic validator for table definitions.
- Convert validation results into pending migration items.
- Apply missing tables normally.
- Repair mismatched existing tables using the safe backup/rebuild/restore workflow.
- Continue to apply indexes, triggers, and procedures through existing idempotent paths.
- Record successful and failed attempts in `SchemaVersions`.

#### `TableSchemaParser`

New internal parser component, or internal helper region in `Service_Migration` if keeping minimal file count.

Responsibilities:

- Parse canonical `CREATE TABLE IF NOT EXISTS` SQL files.
- Extract expected:
  - Table name
  - Columns
  - Primary key
  - Unique constraints
  - Foreign keys
- Ignore non-structural metadata:
  - Comments
  - Formatting
  - Whitespace
  - Table-level comment
- Preserve structural metadata exactly after normalization:
  - Column name
  - Data type
  - Nullability
  - Default
  - Extra attributes such as `auto_increment`
  - Constraint column lists
  - Foreign key referenced table/columns/actions

#### `LiveTableSchemaReader`

New internal metadata reader, or internal helper region in `Service_Migration`.

Responsibilities:

- Query `information_schema` for the configured database.
- Return live table metadata in the same model shape as parsed expected metadata.
- Use exact metadata queries, not fuzzy searches.
- Use case-insensitive table lookup only to handle MySQL lower-case table-name behavior on Windows, but normalize the resulting names deterministically.

#### `TableSchemaComparer`

New internal comparer, or internal helper region in `Service_Migration`.

Responsibilities:

- Compare expected and live table schemas.
- Return structured mismatch objects.
- Classify result:
  - `Match`
  - `Missing`
  - `Mismatch`
  - `Unreadable`
- Do not decide repair steps; only compare.

#### `TableRepairService` / Repair Helpers

New internal repair workflow, or internal helper region in `Service_Migration`.

Responsibilities:

- Given a mismatched existing table and expected SQL file:
  1. Create a temporary data-only backup table.
  2. Copy existing table data into backup.
  3. Validate restore feasibility.
  4. Drop dependent foreign keys if required by MySQL drop order.
  5. Drop the mismatched table.
  6. Recreate the table from the SQL file.
  7. Restore compatible data.
  8. Validate row count restored.
  9. Drop the temporary backup.
- Preserve the backup table if any step fails after backup creation.
- Return structured repair result.

## Data Models

These models may live in `Core/MTM_Waitlist_Server.Core/Models/Migration` if they must cross project boundaries, or as private records in `Service_Migration` if only internal.

### `TableValidationStatus`

```text
Match
Missing
Mismatch
Unreadable
```

### `TableMismatchSeverity`

```text
Info
Warning
Repairable
Blocking
```

### `TableMismatchKind`

```text
TableMissing
ColumnMissing
ColumnUnexpected
ColumnTypeMismatch
ColumnNullabilityMismatch
ColumnDefaultMismatch
ColumnExtraMismatch
PrimaryKeyMismatch
UniqueConstraintMissing
UniqueConstraintUnexpected
ForeignKeyMissing
ForeignKeyUnexpected
ForeignKeyReferencedTableMismatch
ForeignKeyReferencedColumnsMismatch
ForeignKeyDeleteRuleMismatch
ForeignKeyUpdateRuleMismatch
ParseError
MetadataReadError
RestoreIncompatible
```

### `TableSchemaValidationResult`

Fields:

- `string TableName`
- `string SourcePath`
- `TableValidationStatus Status`
- `IReadOnlyList<TableSchemaMismatch> Mismatches`
- `bool CanRepair`
- `string? Summary`

### `TableSchemaMismatch`

Fields:

- `TableMismatchKind Kind`
- `TableMismatchSeverity Severity`
- `string ObjectName`
- `string PropertyName`
- `string? ExpectedValue`
- `string? ActualValue`
- `string Message`
- `string RecommendedAction`

### `TableRepairResult`

Fields:

- `bool IsSuccess`
- `string TableName`
- `string? BackupTableName`
- `int RowsBackedUp`
- `int RowsRestored`
- `IReadOnlyList<string> StepsCompleted`
- `string? ErrorMessage`
- `bool BackupPreserved`

## Expected Schema Model

### `ExpectedTableSchema`

Fields:

- `string TableName`
- `IReadOnlyList<ExpectedColumnSchema> Columns`
- `IReadOnlyList<string> PrimaryKeyColumns`
- `IReadOnlyList<ExpectedUniqueConstraintSchema> UniqueConstraints`
- `IReadOnlyList<ExpectedForeignKeySchema> ForeignKeys`

### `ExpectedColumnSchema`

Fields:

- `string Name`
- `string NormalizedType`
- `bool IsNullable`
- `string? NormalizedDefault`
- `string NormalizedExtra`
- `int Ordinal`

### `ExpectedUniqueConstraintSchema`

Fields:

- `string Name`
- `IReadOnlyList<string> Columns`

### `ExpectedForeignKeySchema`

Fields:

- `string Name`
- `IReadOnlyList<string> Columns`
- `string ReferencedTable`
- `IReadOnlyList<string> ReferencedColumns`
- `string DeleteRule`
- `string UpdateRule`

Live schema models can reuse the same shape.

## Normalization Rules

Normalization must be deterministic and exact.

### Identifiers

- Remove backticks.
- Trim whitespace.
- Convert to lowercase invariant.
- Do not remove underscores.
- Do not singularize/pluralize.
- Do not perform fuzzy matching.

Examples:

- `` `Users` `` -> `users`
- `WaitlistEntries` -> `waitlistentries`
- `CreatedByUserId` -> `createdbyuserid`

### Types

- Trim and collapse whitespace.
- Convert to lowercase invariant.
- Normalize integer display widths:
  - `int(11)` -> `int`
  - `tinyint(1)` remains `tinyint(1)` if source uses `TINYINT(1)` and live returns `tinyint(1)`.
- Normalize enum spacing:
  - `enum( 'A', 'B' )` -> `enum('a','b')` if enum values are case-insensitive in app usage.
  - If enum casing must be preserved, use case-sensitive normalized values and require source/live exact casing.
- Normalize comma spacing inside enum definitions.

Recommended for this project:

- Lowercase enum values for comparison because MySQL metadata and source formatting may preserve different casing while the set/order is what matters structurally.

### Nullability

- Source `NULL` -> nullable `true`.
- Source `NOT NULL` -> nullable `false`.
- Live `IS_NULLABLE = YES` -> nullable `true`.
- Live `IS_NULLABLE = NO` -> nullable `false`.

### Defaults

- Treat missing default and `DEFAULT NULL` as equivalent for nullable columns.
- Strip surrounding single quotes for literal defaults.
- Convert to lowercase invariant.
- Normalize `current_timestamp()` -> `current_timestamp`.
- Preserve numeric values as invariant strings.

Examples:

- `DEFAULT 'Waiting'` -> `waiting`
- Live `Waiting` -> `waiting`
- `DEFAULT 5` -> `5`
- `DEFAULT NULL` -> `null` or no default for nullable columns, consistently.

### Extra Attributes

- Collapse whitespace.
- Convert to lowercase invariant.
- Examples:
  - `auto_increment`

### Primary Keys

- Compare ordered normalized column list.

### Unique Constraints

- Compare by normalized column list first.
- Constraint names should be captured but should not be the only identity if MySQL changes case.
- For this project, source names should be retained for reporting.

### Foreign Keys

- Compare by normalized local column list and referenced table/columns/actions.
- Normalize referential actions:
  - `SET NULL` -> `setnull`
  - `NO ACTION` -> `noaction`
  - `CASCADE` -> `cascade`
  - `RESTRICT` -> `restrict`
- Preserve source constraint names for reporting.

## Validation Workflow

### Scan Definitions

For each SQL file under `Database/schema/tables/**/*.sql`:

1. Resolve runtime file path.
2. Parse expected table schema.
3. Check live table existence using `information_schema.TABLES`.
4. If missing:
   - Return `TableValidationStatus.Missing`.
5. If exists:
   - Read live schema metadata.
   - Compare expected vs live.
   - If no mismatches:
	 - Return `Match`.
   - If mismatches:
	 - Return `Mismatch` with structured mismatch list.

### Pending Migration Mapping

Existing `PendingMigration` can continue to be used initially, but its description should include structured summary text.

Recommended future model addition:

- `PendingMigration` should optionally include:
  - `TableValidationStatus? ValidationStatus`
  - `IReadOnlyList<TableSchemaMismatch> Mismatches`

If changing public models is too much for the first implementation, keep mismatches internal and expose a concise parseable JSON/text summary in `Description` or a new method.

## Apply Workflow

### Missing Table

If table status is `Missing`:

1. Apply SQL file normally through `SqlScriptRunner.RunFileScriptAsync`.
2. Re-run deterministic validation.
3. If match, record success.
4. If mismatch, run repair workflow only if the table now exists and mismatch is repairable.

### Existing Mismatched Table

If table status is `Mismatch`:

1. Report progress:
   - `Validating mismatch for <table>`
   - `Creating temporary data backup for <table>`
   - `Rebuilding <table> from canonical SQL`
   - `Restoring compatible data into <table>`
   - `Cleaning up temporary backup`
2. Run safe table repair workflow.
3. Re-run deterministic validation.
4. Record success only if validation returns `Match`.
5. If any step fails, return failure with structured error and keep backup if created.

### Non-Table Objects

Procedures, triggers, and indexes can continue using current behavior:

- Missing indexes can be auto-applied idempotently.
- Procedures/triggers can be re-created from canonical SQL.
- Existing drift blocking rules can remain, unless later replaced by structured validators.

## Safe Table Repair Workflow

### Preconditions

Before repair:

- Confirm target table exists.
- Confirm expected SQL file exists and parses successfully.
- Confirm the repair is for a table object only.
- Confirm the database connection targets the configured database.
- Confirm there is no existing temporary backup table name collision.

### Backup Table Naming

Use a deterministic but unique name:

```text
__mtm_migration_backup_<normalized_table_name>_<yyyyMMddHHmmss>_<shortGuid>
```

Example:

```text
__mtm_migration_backup_waitlistentries_20260601143025_a1b2c3d4
```

The backup table should be created in the same database.

### Backup Data Only

Use:

```sql
CREATE TABLE `<backup>` AS SELECT * FROM `<table>`;
```

Notes:

- This copies data and column values.
- It does not copy constraints/indexes/triggers.
- That is intentional because this is a data-only backup.

Then count rows:

```sql
SELECT COUNT(*) FROM `<table>`;
SELECT COUNT(*) FROM `<backup>`;
```

If counts differ, fail repair and preserve backup.

### Restore Feasibility Validation

Before dropping the original table, compare backup columns to expected recreated table columns.

Determine:

- `commonColumns`: columns that exist in both backup and expected table.
- `missingRequiredColumns`: expected columns not in backup where:
  - column is `NOT NULL`, and
  - column has no default, and
  - column is not auto-increment.

If `missingRequiredColumns` is not empty:

- Fail repair before dropping original table.
- Drop the backup only if no data was at risk and user preference allows; recommended: preserve backup and report it.

### Foreign Key Dependencies

Dropping a table can fail if other tables reference it.

Before dropping:

1. Query inbound foreign keys:

```sql
SELECT TABLE_NAME, CONSTRAINT_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE REFERENCED_TABLE_SCHEMA = DATABASE()
  AND REFERENCED_TABLE_NAME = @tableName;
```

2. If inbound foreign keys exist:
   - Option A: repair tables in dependency-safe order only and avoid dropping parent tables while children exist.
   - Option B: temporarily drop inbound foreign keys and recreate them later from canonical definitions.

Recommended initial behavior:

- Do not auto-repair a table with inbound foreign keys unless all referencing tables are also scheduled for rebuild in the same migration run and can be ordered safely.
- Return a `Blocking` mismatch with recommended action.

Because many application tables reference `Users`, blindly dropping `Users` could fail or break dependent tables. The first implementation should be conservative.

### Drop and Recreate

If safe to drop:

```sql
DROP TABLE `<table>`;
```

Then run the canonical SQL file using `SqlScriptRunner.RunFileScriptAsync`.

### Restore Data

Build an insert from common columns:

```sql
INSERT INTO `<table>` (`Col1`, `Col2`, ...)
SELECT `Col1`, `Col2`, ...
FROM `<backup>`;
```

Use only exact normalized column matches.

Do not insert into auto-increment primary key unless the backup contains that column and preserving identity values is required.

Recommended for this application:

- Preserve `Id` values where present because foreign keys depend on them.
- Ensure `FOREIGN_KEY_CHECKS` remains enabled unless there is a specific validated reason to disable it.

### Restore Verification

After restore:

1. Count rows in backup and restored table.
2. If counts differ, fail and preserve backup.
3. Run deterministic table validation.
4. If validation fails, preserve backup.
5. If validation succeeds, drop backup.

### Backup Cleanup

Only after successful validation:

```sql
DROP TABLE `<backup>`;
```

### Failure Behavior

If failure occurs:

- Return `TableRepairResult.IsSuccess = false`.
- Include `BackupTableName` if created.
- Set `BackupPreserved = true` if backup remains.
- Include exact failed step and MySQL error message.
- Do not hide the backup table.

## Dependency and Ordering Rules

The existing dependency order should remain and be formalized:

1. `Users`
2. `SchemaVersions`
3. `SharedWorkstations`
4. `RefreshTokens`
5. `WaitlistEntries`
6. `SetupTechDunnageTypeConfig`
7. `WorkstationActiveJobs`
8. `WorkstationJobHistory`
9. `WorkOrderDunnageAssignments`
10. `WorkOrderSubordinateParts`

Repair ordering must consider foreign keys:

- Child tables should generally be repaired before parent tables if parent rebuild would be blocked by inbound FKs.
- Parent tables must exist before child tables are created.
- For a full rebuild of a related set, one approach is:
  1. Back up all affected tables.
  2. Drop child tables first.
  3. Drop parent tables last.
  4. Recreate parent tables first.
  5. Recreate child tables after.
  6. Restore parent data first.
  7. Restore child data after.

Recommended initial scope:

- Single-table repair only when no inbound foreign key blockers exist.
- Missing table creation continues in dependency order.
- Full dependency-set repair can be a later enhancement.

## App/UI Behavior

### Pending Updates

For each pending table item:

- Missing table:
  - Description: `Table missing - will be created from canonical SQL.`
- Mismatched table:
  - Description: `Table mismatch - safe repair available` or `Table mismatch - manual action required`.

### Apply Updates

During apply, progress should include exact steps.

Example:

```text
[TABLE] Validating database/schema/tables/Waitlist/WaitlistEntries.sql...
[TABLE] WaitlistEntries mismatch detected: 2 column differences, 1 foreign key difference.
[TABLE] Creating temporary data backup __mtm_migration_backup_waitlistentries_...
[TABLE] Backed up 123 rows.
[TABLE] Recreating WaitlistEntries from canonical SQL.
[TABLE] Restored 123 rows.
[TABLE] Validation passed after repair.
[TABLE] Removed temporary backup.
```

### Failure Message

Failures should be actionable.

Example:

```text
Update run stopped at database/schema/tables/Waitlist/WaitlistEntries.sql:
Table repair failed during restore. Backup preserved as __mtm_migration_backup_waitlistentries_20260601143025_a1b2c3d4.
Reason: Column RequestedAt cannot be restored because the canonical table requires NOT NULL and the backup has NULL values.
```

## Testing Strategy

Tests should exercise actual migration code paths as much as possible.

### Unit Tests Without Database

These can run against parser/comparer helpers.

Required tests:

1. Parse every SQL file under `Database/schema/tables/**/*.sql`.
2. Verify each parsed table has:
   - Table name
   - At least one column
   - Primary key, where expected
3. Verify parser handles:
   - Multi-line enums
   - Multi-line foreign keys
   - Inline `UNIQUE KEY`
   - `CONSTRAINT ... UNIQUE (...)`
   - Comments containing semicolons
4. Verify expected schema model for known tables:
   - `Users`
   - `RefreshTokens`
   - `WaitlistEntries`
   - `SchemaVersions`

### Integration-Style Tests With Database

If a test MySQL database is available/configured:

1. Create isolated test database.
2. Apply canonical SQL file.
3. Validate table returns `Match`.
4. Modify live table to introduce controlled drift.
5. Validate result returns structured `Mismatch`.
6. Insert data.
7. Run repair.
8. Validate:
   - Table returns `Match`.
   - Row count preserved.
   - Data preserved in compatible columns.
   - Temporary backup deleted on success.
9. Force restore failure.
10. Validate:
   - Repair returns failure.
   - Backup table remains.
   - Error result includes backup name.

### App-Level Tests

Existing migration tests should be expanded to verify:

- `Service_Migration` uses deterministic validation for tables.
- Mismatch descriptions contain structured details.
- `CanAutoApply` allows repairable table mismatches.
- Non-repairable mismatches are blocked with actionable message.
- Existing procedures/triggers/indexes behavior remains intact.

## Implementation Plan

### Phase 1 — Models and Parser

1. Add schema validation models.
2. Add canonical SQL table parser.
3. Add parser tests over all table SQL files.
4. Ensure parser rejects unsupported SQL patterns with structured parse errors.

### Phase 2 — Live Metadata Reader and Comparer

1. Add live metadata reader using `information_schema`.
2. Add deterministic comparer.
3. Replace current ad hoc table signature comparison with schema model comparison.
4. Return structured mismatch details.
5. Add unit tests for comparer using expected/live model fixtures.

### Phase 3 — Migration Service Integration

1. Update `CompareTableDefinitionAsync` to use parser + live metadata + comparer.
2. Update pending migration descriptions to include mismatch summary.
3. Update `CanAutoApply`:
   - Missing table: auto apply.
   - Repairable mismatch: auto repair.
   - Blocking mismatch: block with structured message.
4. Update `ApplyDefinitionAsync`:
   - Missing table: run SQL file.
   - Mismatched table: run repair workflow.
5. Record structured success/failure details in `SchemaVersions.ErrorMessage` where possible.

### Phase 4 — Safe Repair Workflow

1. Add backup table creation.
2. Add backup row-count verification.
3. Add restore feasibility validation.
4. Add drop/recreate from SQL file.
5. Add data restore with common columns.
6. Add post-restore validation.
7. Add backup cleanup on success.
8. Preserve backup on failure.
9. Add tests for success and failure behavior.

### Phase 5 — UI Improvements

1. Show mismatch summary in pending list.
2. Show detailed mismatch list in SQL preview or a new details panel.
3. Show backup table name if repair fails.
4. Add warning copy for destructive-but-backed-up repair.

## Acceptance Criteria

The update is complete when:

1. Every SQL table file can be parsed deterministically.
2. A fresh database can apply all table definitions without false post-apply drift failures.
3. Existing matching tables return `Match`.
4. Existing mismatched tables return structured mismatch results.
5. Repairable mismatched tables are safely backed up, rebuilt, restored, validated, and cleaned up.
6. Failed repairs preserve the temporary backup table and report its name.
7. The migration UI can display actionable mismatch/repair results.
8. Tests cover parser, comparer, migration integration, and repair workflow.
9. Full solution build passes.

## Open Design Decisions

1. Should table repair be enabled automatically when clicking `Apply All`, or should the UI require explicit confirmation for rebuild/restore actions?
2. Should backup tables be hidden by naming convention only, or tracked in `SchemaVersions` / a new repair history table?
3. Should full dependency-set repair be implemented now or deferred?
4. Should `PendingMigration` be expanded with structured mismatch fields, or should a new migration details API be added?
5. Should enum value comparison preserve casing or compare case-insensitively? Current recommendation is case-insensitive normalized comparison.

## Recommended Initial Safe Defaults

- Auto-create missing tables.
- Auto-repair mismatched tables only when:
  - A data backup succeeds.
  - No inbound foreign key blockers exist.
  - Restore feasibility passes.
  - Common-column restore can preserve all existing rows.
- Block and report otherwise.
- Preserve backups on any failure after backup creation.
- Avoid fuzzy matching entirely.
