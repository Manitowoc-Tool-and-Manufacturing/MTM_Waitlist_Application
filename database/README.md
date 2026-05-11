# MTM Waitlist Application — Database

**Server:** `172.16.1.104` (internal work network — MySQL 5.7 — not public internet)  
**Database:** `mtm_waitlist` (lowercase — all MySQL database names must be lowercase)  
**Access pattern:** REST API only — client apps never connect to MySQL directly

---

## Quick Start

```bash
# Full initial deploy — no seed data
mysql -h 172.16.1.104 -u <admin_user> -p < migrations/V001__Initial_Schema.sql

# Add dev sample data (development only — NOT for production)
mysql -h 172.16.1.104 -u <admin_user> -p mtm_waitlist < seed/01_Seed_Users.sql
mysql -h 172.16.1.104 -u <admin_user> -p mtm_waitlist < seed/02_Seed_WaitlistEntries.sql
```

---

## Folder Structure

```
Database/
├── README.md
├── schema/
│   ├── 00_Database.sql
│   └── tables/
│       ├── Auth/
│       │   ├── Users.sql
│       │   ├── SharedWorkstations.sql
│       │   └── RefreshTokens.sql
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
│   └── Waitlist/
│       ├── trg_WaitlistEntries_BeforeInsert.sql
│       └── trg_WaitlistEntries_BeforeUpdate.sql
├── seed/                                    ← development only — NOT for production
│   ├── 01_Seed_Users.sql
│   └── 02_Seed_WaitlistEntries.sql
└── migrations/
    └── V001__Initial_Schema.sql
```

---

## File Reference

### `schema/00_Database.sql`
Creates the `mtm_waitlist` database with `utf8mb4` / `utf8mb4_unicode_ci`. Run first.

```bash
mysql -h 172.16.1.104 -u <admin_user> -p < schema/00_Database.sql
```

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
**Depends on:** `schema/00_Database.sql`

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
| `01_Seed_Users.sql` | 9 users (one per role) + 2 shared workstation entries. ⚠️ Replace `PasswordHash` placeholders with real bcrypt hashes. |
| `02_Seed_WaitlistEntries.sql` | 9 requests covering all 8 `RequestType` and 7 `Status` values. |

---

### `migrations/V001__Initial_Schema.sql`
All-in-one deployment: database + all tables + all indexes + all triggers + all procedures.
Run this for a fresh server. Subsequent changes go in `V002__...`, `V003__...`, etc.

```bash
mysql -h 172.16.1.104 -u <admin_user> -p < migrations/V001__Initial_Schema.sql
```

---

## Execution Order (Individual Files)

```
 1.  schema/00_Database.sql
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
     — dev only —
29.  seed/01_Seed_Users.sql
30.  seed/02_Seed_WaitlistEntries.sql
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
