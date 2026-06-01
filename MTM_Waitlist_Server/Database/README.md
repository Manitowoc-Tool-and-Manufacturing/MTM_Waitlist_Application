# MTM Waitlist Application — Database

**Server:** `172.16.1.104` (internal work network — MySQL 5.7 — not public internet)  
**Database:** `mtm_waitlist` (lowercase — all MySQL database names must be lowercase)  
**Access pattern:** REST API only — client apps never connect to MySQL directly

---

## Quick Start

```bash
# Full initial deploy — no seed data
mysql -h 172.16.1.104 -u <admin_user> -p < migrations/V001__Initial_Schema.sql

# Full local reset from PowerShell
.\schema\00_Database.bat localhost root root

# Add dev sample data (development only — NOT for production)
.\seed\00_Seed_DevelopmentData.bat localhost root root
```

---

## Folder Structure

```
Database/
├── README.md
├── schema/
│   ├── 00_Database.bat
│   ├── data/
│   │   └── System/
│   │       ├── SchemaVersions_BaselineHistory.sql
│   │       └── SchemaVersions_PreExisting_Bootstrap.sql
│   └── tables/
│       ├── Auth/
│       │   ├── Users.sql
│       │   ├── SharedWorkstations.sql
│       │   └── RefreshTokens.sql
│       ├── System/
│       │   └── SchemaVersions.sql
│       └── Waitlist/
│           └── WaitlistEntries.sql
├── indexes/
│   ├── Auth/
│   │   ├── Users_Indexes.sql
│   │   ├── SharedWorkstations_Indexes.sql
│   │   └── RefreshTokens_Indexes.sql
│   └── Waitlist/
│       └── WaitlistEntries_Indexes.sql
├── procedures/
│   ├── Auth/
│   │   ├── usp_Auth_ValidateCredentials.sql
│   │   ├── usp_Auth_GetUserByWindowsUsername.sql
│   │   ├── usp_Auth_CheckSharedWorkstation.sql
│   │   ├── usp_Auth_RecordLogin.sql
│   │   ├── usp_Auth_SaveRefreshToken.sql
│   │   ├── usp_Auth_GetRefreshToken.sql
│   │   ├── usp_Auth_RevokeRefreshToken.sql
│   │   └── usp_Auth_RevokeAllUserTokens.sql
│   ├── SetupTech/
│   │   ├── usp_SetupTech_GetActiveJob.sql
│   │   ├── usp_SetupTech_SetActiveJob.sql
│   │   ├── usp_SetupTech_GetDunnageAssignment.sql
│   │   ├── usp_SetupTech_UpsertDunnageAssignment.sql
│   │   ├── usp_SetupTech_DeleteDunnageAssignment.sql
│   │   ├── usp_SetupTech_UpsertSubordinateParts.sql
│   │   ├── usp_SetupTech_GetJobHistory.sql
│   │   ├── usp_SetupTech_GetEnabledDunnageTypes.sql
│   │   └── usp_SetupTech_UpsertDunnageTypeConfig.sql
│   └── Waitlist/
│       ├── usp_Waitlist_GetAll.sql
│       ├── usp_Waitlist_GetById.sql
│       ├── usp_Waitlist_Insert.sql
│       ├── usp_Waitlist_Update.sql
│       └── usp_Waitlist_Delete.sql
├── triggers/
│   ├── Auth/
│   │   ├── trg_Users_BeforeInsert.sql
│   │   ├── trg_Users_BeforeUpdate.sql
│   │   ├── trg_SharedWorkstations_BeforeInsert.sql
│   │   └── trg_SharedWorkstations_BeforeUpdate.sql
│   ├── SetupTech/
│   │   ├── trg_WorkstationActiveJobs_BeforeInsert.sql
│   │   ├── trg_WorkstationActiveJobs_BeforeUpdate.sql
│   │   ├── trg_WorkstationJobHistory_BeforeInsert.sql
│   │   ├── trg_WorkstationJobHistory_BeforeUpdate.sql
│   │   ├── trg_WorkOrderDunnageAssignments_BeforeInsert.sql
│   │   ├── trg_WorkOrderDunnageAssignments_BeforeUpdate.sql
│   │   ├── trg_SetupTechDunnageTypeConfig_BeforeInsert.sql
│   │   ├── trg_SetupTechDunnageTypeConfig_BeforeUpdate.sql
│   │   ├── trg_WorkOrderSubordinateParts_BeforeInsert.sql
│   │   └── trg_WorkOrderSubordinateParts_BeforeUpdate.sql
│   └── Waitlist/
│       ├── trg_WaitlistEntries_BeforeInsert.sql
│       └── trg_WaitlistEntries_BeforeUpdate.sql
├── seed/                                    ← development only — NOT for production
│   ├── 00_Seed_DevelopmentData.bat
│   ├── 01_Seed_WaitlistEntries.sql
│   └── 02_Seed_SetupTechDunnageTypeConfig.sql
└── migrations/
    ├── V001__Initial_Schema.sql
    ├── V002__Add_SchemaVersions_Table.sql
    ├── V003__SetupTech_Schema.sql
    └── V004__SetupTech_Default_DunnageTypeConfig.sql
```

The admin host copies `Database/migrations`, `Database/procedures`,
`Database/triggers`, and `Database/indexes` into its runtime `database/`
folder so the in-app migration runner can discover and apply them from disk.

---

## File Reference

### `schema/00_Database.bat`
Supported Windows batch entry point for a destructive full reinstall. It drops `mtm_waitlist`, runs `V001` through `V004` sequentially with the mysql client, then backfills the matching `SchemaVersions` rows.

Important: this repository has two different `Database` folders. Use the server-side one at `MTM_Waitlist_Server/Database`, not the top-level repository `Database` folder.

Windows PowerShell example:

```powershell
Set-Location C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Server\Database
.\schema\00_Database.bat localhost root root
```

If you are currently in the top-level repository `Database` folder, first move up one level before changing into the server folder:

```powershell
Set-Location ..
Set-Location .\MTM_Waitlist_Server\Database
.\schema\00_Database.bat localhost root root
```

Arguments: `[host] [user] [password] [mysqlExe]`

To avoid putting the password on the command line, set `MYSQL_PWD` first and omit the third argument:

```powershell
$env:MYSQL_PWD = 'root'
.\schema\00_Database.bat localhost root
```

Do not put a space-delimited password after `-p`. If you intentionally pass an inline password directly to `mysql`, it must use the attached form such as `-proot`.

This batch file does not rely on mysql `SOURCE`, so it works from PowerShell and `cmd.exe`.

It now uses checked-in SQL assets for the `SchemaVersions` table and baseline history backfill instead of generating inline SQL in the batch file.

### `schema/tables/System/SchemaVersions.sql`
Canonical `SchemaVersions` table definition used by the Server Admin migration service whenever it needs to bootstrap migration tracking onto a pre-existing schema.

### `schema/data/System/SchemaVersions_PreExisting_Bootstrap.sql`
Backfills the `V001` history row when the admin app discovers a database that already has the core tables but predates the migration tracking table.

### `schema/data/System/SchemaVersions_BaselineHistory.sql`
Backfills the `V002` through `V004` history rows during a full destructive reinstall driven by `schema/00_Database.bat`.

### `seed/00_Seed_DevelopmentData.bat`
Supported Windows batch entry point for dev-only seed data. It applies the current server seed files in order after the schema already exists.

Windows PowerShell example:

```powershell
Set-Location C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Server\Database
.\seed\00_Seed_DevelopmentData.bat localhost root root
```

To avoid putting the password on the command line, set `MYSQL_PWD` first and omit the third argument:

```powershell
$env:MYSQL_PWD = 'root'
.\seed\00_Seed_DevelopmentData.bat localhost root
```

Arguments: `[host] [user] [password] [mysqlExe]`

This batch file is development-only. It should be run only after `schema/00_Database.bat` or another schema bootstrap path has already created `mtm_waitlist`.

---

### `schema/tables/Auth/Users.sql`
Application staff accounts. Supports two login flows:

| Scenario | Flow |
|----------|------|
| **Personal workstation** | Windows username NOT in `SharedWorkstations` → auto-login via `usp_Auth_GetUserByWindowsUsername` |
| **Shared workstation** | Windows username IS in `SharedWorkstations` → show login form → `usp_Auth_ValidateCredentials` |

**Role values:** `PressOperation`, `SetupTech`, `ProductionSupervisor`, `ProductionManager`, `Quality`, `Receiving`, `MaterialHandler`, `Admin`, `Developer`  
**C# enum:** `Enum_UserRole`  
**`CreatedAt`/`UpdatedAt`:** Set automatically by triggers.

---

### `schema/tables/Auth/SharedWorkstations.sql`
Windows usernames of shared kiosks/floor terminals that require manual login.
Any PC whose Windows username does NOT appear here → auto-login.

**C# model:** `Model_SharedWorkstation`  
**Depends on:** `mtm_waitlist` already created and selected, or `schema/00_Database.bat` for full reinstall.

---

### `schema/tables/Auth/RefreshTokens.sql`
JWT refresh tokens stored as SHA-256 hashes. Active when `RevokedAt IS NULL AND ExpiresAt > UTC_TIMESTAMP()`. Cascade-deletes with the owning user.

---

### `schema/tables/Waitlist/WaitlistEntries.sql`
Core business table — workcenter logistics requests.

**RequestType values:**

| Value | Meaning |
|-------|---------|
| `Coil` | Deliver a coil |
| `Dunnage` | Deliver dunnage |
| `PickUpFinishedGoods` | Pick up finished goods |
| `PickUpUnusedGoods` | Pick up unused goods |
| `PickUpDunnage` | Pick up dunnage |
| `BringPartsToPress` | Bring parts to the press |
| `RemoveCoilFromPress` | Remove a coil from the press |
| `BringPickUpDie` | Bring or pick up a die |

**Status values:** `Waiting`, `Active`, `Late`, `LowImportance`, `Project`, `Completed`, `Cancelled`

**C# mappings:** `Model_WaitlistEntry`, `Enum_WaitlistRequestType`, `Enum_WaitlistStatus`

---

### `indexes/`

| File | Covers |
|------|--------|
| `Users_Indexes.sql` | `IsActive`, `Role` |
| `SharedWorkstations_Indexes.sql` | `IsActive` |
| `RefreshTokens_Indexes.sql` | `TokenHash`, `UserId`, `ExpiresAt` |
| `WaitlistEntries_Indexes.sql` | `Status`, `(Priority,Status)`, `RequestType`, `WorkcenterName`, `RequestedAt`, all FK columns |

---

### `procedures/Auth/`

| Procedure | Purpose |
|-----------|---------|
| `usp_Auth_CheckSharedWorkstation(p_WindowsUsername)` | Determine login mode. Row returned = shared workstation = show login form. No row = auto-login. |
| `usp_Auth_ValidateCredentials(p_Username)` | Return hash for bcrypt comparison on shared-workstation login. |
| `usp_Auth_GetUserByWindowsUsername(p_WindowsUsername)` | Return user info for personal-workstation auto-login. |
| `usp_Auth_RecordLogin(p_UserId)` | Set `LastLoginAt` after successful login. |
| `usp_Auth_SaveRefreshToken(p_UserId, p_TokenHash, p_ExpiresAt)` | Persist a new refresh token hash. |
| `usp_Auth_GetRefreshToken(p_TokenHash)` | Look up an active token for token refresh. |
| `usp_Auth_RevokeRefreshToken(p_TokenHash)` | Revoke one token on logout. |
| `usp_Auth_RevokeAllUserTokens(p_UserId)` | Revoke all tokens for a user (forced sign-out). |

---

### `procedures/Waitlist/`

| Procedure | Purpose |
|-----------|---------|
| `usp_Waitlist_GetAll(p_Status, p_RequestType, p_Limit, p_Offset)` | Filtered, paginated list ordered by priority then date. |
| `usp_Waitlist_GetById(p_Id)` | Single entry by primary key. |
| `usp_Waitlist_Insert(...)` | Insert new request; returns `OUT p_NewId`. |
| `usp_Waitlist_Update(...)` | Update all mutable columns. |
| `usp_Waitlist_Delete(p_Id)` | Hard-delete. Prefer cancellation for audit trail. |

---

### `schema/tables/SetupTech/`

| File | Purpose |
|------|---------|
| `WorkstationActiveJobs.sql` | Current active job per workstation. One row per workcenter. |
| `WorkstationJobHistory.sql` | Archived prior workstation jobs for analytics and audit. |
| `WorkOrderDunnageAssignments.sql` | Cached dunnage assignment lines for a work-order/sequence pair. |
| `SetupTechDunnageTypeConfig.sql` | Enabled/disabled receiving-app dunnage types and their display order in SetupTech UI. |
| `WorkOrderSubordinateParts.sql` | Cached subordinate/component parts returned from Infor Visual. |

### `indexes/SetupTech/`

| File | Covers |
|------|--------|
| `WorkstationActiveJobs_Indexes.sql` | `SetupTechUserId`, `(WorkOrderId,SequenceNo)` |
| `WorkstationJobHistory_Indexes.sql` | `(WorkcenterId,ActiveFrom)`, `SetupTechUserId`, `(WorkOrderId,SequenceNo)` |
| `WorkOrderDunnageAssignments_Indexes.sql` | `LastModifiedByUserId`, `DunnageTypeId` |
| `SetupTechDunnageTypeConfig_Indexes.sql` | `(IsEnabled,DisplayOrder)` |
| `WorkOrderSubordinateParts_Indexes.sql` | `CachedAt` |

### `procedures/SetupTech/`

| Procedure | Purpose |
|-----------|---------|
| `usp_SetupTech_GetActiveJob(p_WorkcenterId)` | Get the current active job for a workstation. |
| `usp_SetupTech_SetActiveJob(...)` | Archive the prior active job and insert the new one atomically. |
| `usp_SetupTech_GetDunnageAssignment(p_WorkOrderId, p_SequenceNo)` | Get cached dunnage assignment rows for a work-order/sequence pair. |
| `usp_SetupTech_UpsertDunnageAssignment(...)` | Insert or update one dunnage assignment line. |
| `usp_SetupTech_DeleteDunnageAssignment(p_Id)` | Remove a cached dunnage assignment line. |
| `usp_SetupTech_UpsertSubordinateParts(...)` | Insert or update one subordinate-part cache row. |
| `usp_SetupTech_GetJobHistory(p_WorkcenterId, p_PageSize)` | Get archived job history for a workstation. |
| `usp_SetupTech_GetEnabledDunnageTypes()` | Get enabled dunnage UI categories ordered by `DisplayOrder`. |
| `usp_SetupTech_UpsertDunnageTypeConfig(...)` | Insert or update a SetupTech dunnage type config row. |

### `triggers/SetupTech/`

All SetupTech triggers set UTC audit timestamps. `WorkstationActiveJobs` also normalizes `ActiveSince`, `WorkstationJobHistory` normalizes `ActiveFrom`/`ActiveUntil`, and `WorkOrderSubordinateParts` normalizes `CachedAt`.

---

### `triggers/`

| Trigger | Fires on | Purpose |
|---------|----------|---------|
| `trg_Users_BeforeInsert` | `BEFORE INSERT ON Users` | Set `CreatedAt`, `UpdatedAt` |
| `trg_Users_BeforeUpdate` | `BEFORE UPDATE ON Users` | Refresh `UpdatedAt` |
| `trg_SharedWorkstations_BeforeInsert` | `BEFORE INSERT ON SharedWorkstations` | Set `CreatedAt`, `UpdatedAt` |
| `trg_SharedWorkstations_BeforeUpdate` | `BEFORE UPDATE ON SharedWorkstations` | Refresh `UpdatedAt` |
| `trg_WaitlistEntries_BeforeInsert` | `BEFORE INSERT ON WaitlistEntries` | Set `CreatedAt`, `UpdatedAt`; auto-set `CompletedAt` if inserted as resolved |
| `trg_WaitlistEntries_BeforeUpdate` | `BEFORE UPDATE ON WaitlistEntries` | Refresh `UpdatedAt`; auto-set `CompletedAt` on first transition to resolved status |

---

### `seed/`

> ❌ **Development only — never run in production.**

| File | Contents |
|------|----------|
| `00_Seed_DevelopmentData.bat` | Runs all current dev-only seed files in order. |
| `01_Seed_WaitlistEntries.sql` | 9 requests covering all 8 `RequestType` and 7 `Status` values. If no seeded users exist, nullable user FKs fall back to `NULL`. |
| `02_Seed_SetupTechDunnageTypeConfig.sql` | Development reseed for SetupTech dunnage type filters. The production/default copy is handled by `V004`. |

---

### `migrations/V001__Initial_Schema.sql`
All-in-one deployment: database + all tables + all indexes + all triggers + all procedures.
Run this for a fresh server. Subsequent changes go in `V002__...`, `V003__...`, etc.

```bash
mysql -h 172.16.1.104 -u <admin_user> -p < migrations/V001__Initial_Schema.sql
```

### `migrations/V003__SetupTech_Schema.sql`
Adds the SetupTech schema objects required by Feature 7: tables, indexes, triggers, and procedures.

### `migrations/V004__SetupTech_Default_DunnageTypeConfig.sql`
Seeds the required default SetupTech dunnage type configuration for production/runtime use. This is versioned migration data, not development sample data, and it inserts only missing rows so existing admin configuration is preserved.

---

## Execution Order (Individual Files)

For a destructive full reinstall from scratch, prefer `schema/00_Database.bat`, which runs the versioned migration files in order.

Use the list below only when you intentionally need to run the split object files one-by-one.

```
 1.  schema/00_Database.bat
 2.  schema/tables/Auth/Users.sql
 3.  schema/tables/Auth/SharedWorkstations.sql
 4.  schema/tables/Auth/RefreshTokens.sql
 5.  schema/tables/Waitlist/WaitlistEntries.sql
 6.  indexes/Auth/Users_Indexes.sql
 7.  indexes/Auth/SharedWorkstations_Indexes.sql
 8.  indexes/Auth/RefreshTokens_Indexes.sql
 9.  indexes/Waitlist/WaitlistEntries_Indexes.sql
10.  triggers/Auth/trg_Users_BeforeInsert.sql
11.  triggers/Auth/trg_Users_BeforeUpdate.sql
12.  triggers/Auth/trg_SharedWorkstations_BeforeInsert.sql
13.  triggers/Auth/trg_SharedWorkstations_BeforeUpdate.sql
14.  triggers/Waitlist/trg_WaitlistEntries_BeforeInsert.sql
15.  triggers/Waitlist/trg_WaitlistEntries_BeforeUpdate.sql
16.  procedures/Auth/usp_Auth_ValidateCredentials.sql
17.  procedures/Auth/usp_Auth_GetUserByWindowsUsername.sql
18.  procedures/Auth/usp_Auth_CheckSharedWorkstation.sql
19.  procedures/Auth/usp_Auth_RecordLogin.sql
20.  procedures/Auth/usp_Auth_SaveRefreshToken.sql
21.  procedures/Auth/usp_Auth_GetRefreshToken.sql
22.  procedures/Auth/usp_Auth_RevokeRefreshToken.sql
23.  procedures/Auth/usp_Auth_RevokeAllUserTokens.sql
24.  procedures/Waitlist/usp_Waitlist_GetAll.sql
25.  procedures/Waitlist/usp_Waitlist_GetById.sql
26.  procedures/Waitlist/usp_Waitlist_Insert.sql
27.  procedures/Waitlist/usp_Waitlist_Update.sql
28.  procedures/Waitlist/usp_Waitlist_Delete.sql
29.  schema/tables/SetupTech/WorkstationActiveJobs.sql
30.  schema/tables/SetupTech/WorkstationJobHistory.sql
31.  schema/tables/SetupTech/WorkOrderDunnageAssignments.sql
32.  schema/tables/SetupTech/SetupTechDunnageTypeConfig.sql
33.  schema/tables/SetupTech/WorkOrderSubordinateParts.sql
34.  indexes/SetupTech/WorkstationActiveJobs_Indexes.sql
35.  indexes/SetupTech/WorkstationJobHistory_Indexes.sql
36.  indexes/SetupTech/WorkOrderDunnageAssignments_Indexes.sql
37.  indexes/SetupTech/SetupTechDunnageTypeConfig_Indexes.sql
38.  indexes/SetupTech/WorkOrderSubordinateParts_Indexes.sql
39.  triggers/SetupTech/trg_WorkstationActiveJobs_BeforeInsert.sql
40.  triggers/SetupTech/trg_WorkstationActiveJobs_BeforeUpdate.sql
41.  triggers/SetupTech/trg_WorkstationJobHistory_BeforeInsert.sql
42.  triggers/SetupTech/trg_WorkstationJobHistory_BeforeUpdate.sql
43.  triggers/SetupTech/trg_WorkOrderDunnageAssignments_BeforeInsert.sql
44.  triggers/SetupTech/trg_WorkOrderDunnageAssignments_BeforeUpdate.sql
45.  triggers/SetupTech/trg_SetupTechDunnageTypeConfig_BeforeInsert.sql
46.  triggers/SetupTech/trg_SetupTechDunnageTypeConfig_BeforeUpdate.sql
47.  triggers/SetupTech/trg_WorkOrderSubordinateParts_BeforeInsert.sql
48.  triggers/SetupTech/trg_WorkOrderSubordinateParts_BeforeUpdate.sql
49.  procedures/SetupTech/usp_SetupTech_GetActiveJob.sql
50.  procedures/SetupTech/usp_SetupTech_SetActiveJob.sql
51.  procedures/SetupTech/usp_SetupTech_GetDunnageAssignment.sql
52.  procedures/SetupTech/usp_SetupTech_UpsertDunnageAssignment.sql
53.  procedures/SetupTech/usp_SetupTech_DeleteDunnageAssignment.sql
54.  procedures/SetupTech/usp_SetupTech_UpsertSubordinateParts.sql
55.  procedures/SetupTech/usp_SetupTech_GetJobHistory.sql
56.  procedures/SetupTech/usp_SetupTech_GetEnabledDunnageTypes.sql
57.  procedures/SetupTech/usp_SetupTech_UpsertDunnageTypeConfig.sql
58.  migrations/V003__SetupTech_Schema.sql
59.  migrations/V004__SetupTech_Default_DunnageTypeConfig.sql
     — dev only —
60.  seed/00_Seed_DevelopmentData.bat
61.  seed/01_Seed_WaitlistEntries.sql
62.  seed/02_Seed_SetupTechDunnageTypeConfig.sql
```

---

## Adding a New Domain or Table

1. Create `schema/tables/<Domain>/<TableName>.sql`
2. Create `indexes/<Domain>/<TableName>_Indexes.sql`
3. Create `triggers/<Domain>/trg_<TableName>_BeforeInsert.sql`
4. Create `triggers/<Domain>/trg_<TableName>_BeforeUpdate.sql`
5. Create one `procedures/<Domain>/usp_<Domain>_<Action>.sql` per CRUD operation
6. Add a new `migrations/V00N__<Description>.sql` containing only the new objects
7. **Update this README** — Folder Structure, File Reference, and Execution Order
8. Update matching C# `Model_*`, `Entity_*`, and `Enum_*` files in Core
