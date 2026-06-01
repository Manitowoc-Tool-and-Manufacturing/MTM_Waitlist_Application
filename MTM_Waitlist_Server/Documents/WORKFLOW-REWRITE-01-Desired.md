# Glossary

- DDL: Data Definition Language. SQL that creates or alters tables, indexes, triggers, and procedures.
- Drift: A live database object no longer matching the SQL file that is supposed to define it.
- FK: Foreign key.
- First-run: The admin app workflow that prepares the database and first admin user.
- Source-of-truth SQL: The checked-in SQL file that defines the intended live object.

# Desired Workflow

## Goal

Replace the current first-run, migration, and danger-zone behavior with a simpler model:

1. Keep versioned migration SQL files to a minimum.
2. Treat checked-in SQL files as the source of truth for live objects.
3. Detect drift by comparing live objects to stored SQL definitions.
4. Prefer safe, data-preserving updates over destructive rebuilds.
5. Keep existing UI elements, but replace the current backing workflow logic.

## Workflow 1: First-Run Setup

### Step 1 — Configure Database Access

Purpose:
Capture working privileged MySQL access, ensure the target database exists, ensure the runtime application MySQL user exists, and persist only the runtime user credentials needed by the app.

Desired behavior:

1. The operator enters host, port, database name, privileged username, privileged password, application username, and application password.
2. The app validates the privileged connection before doing anything else.
3. The app creates the database only if it does not already exist.
4. The app creates the runtime application MySQL user only if it does not already exist.
5. If the runtime application MySQL user already exists:
   - the username stays fixed,
   - the operator may either reuse a saved password or provide a replacement password,
   - the app resets that user password only when a replacement password is explicitly supplied.
6. The app grants the runtime user the minimum required privileges on the target database.
7. The app persists only the runtime application connection settings after all step-1 work succeeds.
8. Step 1 never bootstraps schema objects and never creates the first admin row.

Failure rules:

- Root/DBA authentication failure stays in Step 1 with a clear credential error.
- Existing runtime user with no reusable password prompts for a replacement password without disabling the password field.
- No partial settings save on failed step-1 work.

### Step 2 — Bootstrap Base Schema

Purpose:
Bring the database up to the minimum viable baseline needed for the admin app to continue.

Desired behavior:

1. Step 2 runs a safe bootstrap against the target database.
2. Bootstrap must be resumable after partial success.
3. Bootstrap must never drop existing data-bearing tables just to recover from partial setup.
4. Bootstrap must not blindly drop indexes that may support foreign keys.
5. Bootstrap should only create missing objects or apply safe alters for missing columns, defaults, indexes, triggers, and procedures.
6. If bootstrap finishes with all required baseline objects present, the app advances to Step 3.

Comparison rules:

- For tables: compare a normalized signature derived from information_schema against the source SQL expectation.
- For procedures/triggers: compare normalized live definitions (`SHOW CREATE`) against normalized checked-in SQL.
- A mismatch marks the object as needing update; it does not trigger destructive rebuild by default.

### Step 3 — Create First Admin User

Purpose:
Create the first active Admin or Developer application user only after the schema baseline is ready.

Desired behavior:

1. Step 3 uses the runtime application MySQL connection saved in Step 1.
2. The operator enters Windows username, app username, display name, role, and password.
3. The app validates uniqueness and required fields before insert.
4. The app inserts exactly one active admin-capable row.
5. The app marks `FirstRunComplete = true` only after the insert succeeds.
6. The app re-probes the environment after success and exits the first-run workflow only if the probe returns Ready.

## Workflow 2: Migration Page

### Purpose

The migration page should be an object-comparison and safe-update page, not a broad "rerun everything" page.

### Desired behavior

1. Load current database state.
2. Compare live objects to checked-in SQL source-of-truth files.
3. Group results into:
   - Missing objects
   - Drifted objects
   - Matching objects
4. Show only missing or drifted objects as pending updates.
5. Allow preview of the stored SQL and the normalized live-vs-file difference summary.
6. Apply only the selected missing/drifted updates.
7. Record update attempts and results in tracking metadata.

### Migration file policy

1. Keep append-only migration files to a minimum.
2. Use versioned migrations only for true data migrations or one-time transitions that cannot be expressed safely from the source-of-truth object files.
3. Prefer updating the canonical schema/procedure/trigger/index SQL file and letting the page detect drift.
4. When schema changes are needed, write SQL so it preserves data:
   - add nullable columns first when needed,
   - backfill safely,
   - add defaults carefully,
   - avoid dropping populated columns/tables in routine updates,
   - only toggle foreign key checks when a controlled, reversible operation requires it.

### Object comparison model

1. Tables:
   - compare columns, types, nullability, defaults, primary keys, indexes, and foreign keys using a normalized metadata signature.
2. Procedures:
   - compare normalized procedure body text from the stored SQL file to normalized `SHOW CREATE PROCEDURE` output.
3. Triggers:
   - compare normalized trigger body text from the stored SQL file to normalized `SHOW CREATE TRIGGER` output.
4. Indexes:
   - compare live index definitions from information_schema to the stored SQL file signature.

## Workflow 3: Danger Zone

### Purpose

Provide intentionally destructive recovery only when the operator explicitly chooses it.

### Desired behavior

1. Danger zone stays on the migration page.
2. Wipe database requires explicit typed confirmation.
3. Wipe database stops any conflicting workflow before running.
4. Wipe database drops the configured database, recreates it empty, resets first-run state, and returns the app to Step 1.
5. Wipe database does not claim success until the database recreation and first-run reset both succeed.
6. After a wipe, the app should not automatically skip into step 2 or step 3 using stale state.

## Startup Routing Rules

1. Probe Ready + `FirstRunComplete=true` → normal startup.
2. Probe SchemaMissing + `FirstRunComplete=false` → first-run at Step 2.
3. Probe NoAdminUser + `FirstRunComplete=false` → first-run at Step 3.
4. Probe credential/unreachable failure + no valid runtime settings → first-run at Step 1.
5. Probe non-ready + `FirstRunComplete=true` → degraded mode, not silent self-heal.

## Rewrite Constraints

1. Keep the existing XAML pages and visible UI controls.
2. Replace the backing workflow logic from scratch where necessary.
3. Prefer small, explicit service boundaries.
4. Prefer deterministic comparison/update logic over replaying large SQL batches blindly.