---
description: "MySQL database conventions, naming rules, folder structure, and procedure/trigger patterns for the MTM Waitlist Application."
applyTo: "database/**/*.sql"
---

# Database Instructions — MTM Waitlist Application

The MTM Waitlist backend uses a **MySQL 8.0+** database named `MTM_Waitlist` hosted
on the internal work-network server (`172.16.1.104`). All client access goes through
the REST API — client apps never connect to MySQL directly.

> **README rule:** `database/README.md` is the human-readable companion to this file.
> **Every time a SQL file is added, removed, or renamed, update `database/README.md`** —
> specifically the Folder Structure, File Reference, and Execution Order sections.
> This README is the first place developers look when working with the database.

---

## Folder Structure and Execution Order

```
database/
├── schema/
│   ├── 00_Database.sql                                  Step 1 — CREATE DATABASE
│   └── tables/
│       ├── Auth/
│       │   ├── Users.sql                                Step 2
│       │   └── RefreshTokens.sql                        Step 3
│       └── Waitlist/
│           └── WaitlistEntries.sql                      Step 4
├── indexes/
│   ├── Auth/
│   │   ├── Users_Indexes.sql                            Step 5
│   │   └── RefreshTokens_Indexes.sql                    Step 6
│   └── Waitlist/
│       └── WaitlistEntries_Indexes.sql                  Step 7
├── procedures/
│   ├── Auth/
│   │   ├── usp_Auth_ValidateCredentials.sql             Step 8
│   │   ├── usp_Auth_RecordLogin.sql
│   │   ├── usp_Auth_SaveRefreshToken.sql
│   │   ├── usp_Auth_GetRefreshToken.sql
│   │   ├── usp_Auth_RevokeRefreshToken.sql
│   │   └── usp_Auth_RevokeAllUserTokens.sql
│   └── Waitlist/
│       ├── usp_Waitlist_GetAll.sql                      Step 9
│       ├── usp_Waitlist_GetById.sql
│       ├── usp_Waitlist_Insert.sql
│       ├── usp_Waitlist_Update.sql
│       └── usp_Waitlist_Delete.sql
├── triggers/
│   ├── Auth/
│   │   ├── trg_Users_BeforeInsert.sql                   Step 10
│   │   └── trg_Users_BeforeUpdate.sql
│   └── Waitlist/
│       ├── trg_WaitlistEntries_BeforeInsert.sql         Step 11
│       └── trg_WaitlistEntries_BeforeUpdate.sql
├── seed/
│   ├── 01_Seed_Users.sql                                Dev only
│   └── 02_Seed_WaitlistEntries.sql                      Dev only
└── migrations/
    └── V001__Initial_Schema.sql                         All-in-one
```

**One-shot deployment:** run `migrations/V001__Initial_Schema.sql` — it includes all
DDL in the correct order without the seed data.

---

## Naming Conventions (mirrors C# codebase)

All identifiers use **PascalCase** to match `Model_*` and `Entity_*` C# class properties.

| Object | Pattern | Example |
|--------|---------|---------|
| Database | `MTM_<Domain>` | `` `MTM_Waitlist` `` |
| Table | PascalCase plural | `` `WaitlistEntries` ``, `` `Users` ``, `` `RefreshTokens` `` |
| Column | PascalCase | `` `Id` ``, `` `FirstName` ``, `` `CreatedAt` ``, `` `IsActive` `` |
| Primary key constraint | `pk_<Table>` | `pk_WaitlistEntries` |
| Unique constraint | `uq_<Table>_<Column>` | `uq_Users_Username` |
| Foreign key constraint | `fk_<Table>_<Reference>` | `fk_WaitlistEntries_CreatedByUser` |
| Index | `idx_<Table>_<Column(s)>` | `idx_WaitlistEntries_Status` |
| Stored procedure | `usp_<Domain>_<Action>` | `usp_Waitlist_GetAll`, `usp_Auth_RecordLogin` |
| Trigger | `trg_<Table>_<Timing><Event>` | `trg_WaitlistEntries_BeforeUpdate` |

> **The table/column names must stay in sync with the C# `Model_*` and `Entity_*` classes.**
> When a column is added to MySQL, update both `Model_WaitlistEntry` (API/MAUI) and
> `Entity_WaitlistEntry` (SQLite cache) simultaneously.

---

## Domain Groups

| Domain | Tables | Procedures prefix | Triggers prefix |
|--------|--------|-------------------|-----------------|
| `Auth` | `Users`, `RefreshTokens` | `usp_Auth_*` | `trg_Users_*` |
| `Waitlist` | `WaitlistEntries` | `usp_Waitlist_*` | `trg_WaitlistEntries_*` |

New domains follow the same pattern. Each domain has its own subfolder under
`tables/`, `indexes/`, `procedures/`, and `triggers/`.

---

## Design Rules

### Every Table Must Have
- `Id INT NOT NULL AUTO_INCREMENT` — surrogate primary key, named `pk_<Table>`
- `CreatedAt DATETIME NOT NULL` — UTC, set by `BEFORE INSERT` trigger; never set by callers
- `UpdatedAt DATETIME NOT NULL` — UTC, set by `BEFORE UPDATE` trigger on every change

### All Datetimes Are UTC
All `DATETIME` columns store UTC. **Never use `TIMESTAMP`** (MySQL auto-converts
`TIMESTAMP` to the server's local timezone, which breaks UTC consistency).
Use `UTC_TIMESTAMP()` in all SQL — never `NOW()`.

### Stored Procedures
- **Never compare passwords in SQL.** `usp_Auth_ValidateCredentials` returns the hash
  so the API backend (C#) can perform `BCrypt.VerifyHash()`. MySQL never receives the
  raw password.
- Procedures that write data use `DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;`
- Read-only procedures do not require explicit error handlers.
- Use `IN` / `OUT` parameters consistently — avoid `INOUT` for clarity.
- Include `LIMIT 1` on any query that logically returns at most one row.
- Every file begins with `DROP PROCEDURE IF EXISTS` before `CREATE PROCEDURE` so the
  file is safely re-runnable.

### Triggers
- Triggers set `CreatedAt`, `UpdatedAt`, and auto-derive fields like `CompletedAt`.
- No business logic that duplicates API validation belongs in a trigger.
- All trigger timestamps use `UTC_TIMESTAMP()`.
- Every file begins with `DROP TRIGGER IF EXISTS` before `CREATE TRIGGER`.

### Indexes
- Every foreign key column must have an explicit index (MySQL does not auto-create them).
- Every column used in a `WHERE` clause inside a stored procedure gets an index.
- Composite indexes: place the most selective column first.

### Character Set
All objects use `utf8mb4` / `utf8mb4_unicode_ci` — required for full Unicode and emoji support.

---

## File Header Standard

Every SQL file must begin with this block:

```sql
-- =============================================================
-- MTM Waitlist Application — <Object type and name>
-- Domain:      <Auth | Waitlist>
-- Description: <one-line description>
-- Called by:   <C# interface method, if applicable>
-- Depends on:  <prerequisite file(s)>
-- =============================================================

USE `MTM_Waitlist`;
```

---

## Relationship to C# Codebase

| MySQL object | C# interface / class |
|---|---|
| `WaitlistEntries` table | `Entity_WaitlistEntry` (SQLite) · `Model_WaitlistEntry` (model) |
| `usp_Waitlist_GetAll` | `IRepository_WaitlistEntry.GetAllWaitlistEntriesAsync()` |
| `usp_Waitlist_GetById` | `IRepository_WaitlistEntry.GetWaitlistEntryByIdAsync()` |
| `usp_Waitlist_Insert` | `IRepository_WaitlistEntry.InsertWaitlistEntryAsync()` |
| `usp_Waitlist_Update` | `IRepository_WaitlistEntry.UpdateWaitlistEntryAsync()` |
| `usp_Waitlist_Delete` | `IRepository_WaitlistEntry.DeleteWaitlistEntryAsync()` |
| `Users` table | Managed via `IService_Auth` / API — no C# entity |
| `usp_Auth_ValidateCredentials` | `IService_Auth.LoginAsync()` |
| `usp_Auth_SaveRefreshToken` | `IService_Auth.LoginAsync()` · `IService_Auth.RefreshTokenAsync()` |
| `usp_Auth_GetRefreshToken` | `IService_Auth.RefreshTokenAsync()` |
| `usp_Auth_RevokeRefreshToken` | `IService_Auth.LogoutAsync()` |
| `usp_Auth_RevokeAllUserTokens` | `IService_Auth.LogoutAsync()` (full sign-out) |

> When `Model_WaitlistEntry` gains new fields, update **all three** simultaneously:
> 1. `WaitlistEntries` MySQL table (add column + new migration)
> 2. `Entity_WaitlistEntry` SQLite entity (add property + `[Column]` attribute)
> 3. `usp_Waitlist_Insert` and `usp_Waitlist_Update` procedure parameter lists

---

## Adding a New Table

1. Create `database/schema/tables/<Domain>/<TableName>.sql`
2. Create `database/indexes/<Domain>/<TableName>_Indexes.sql`
3. Create `database/triggers/<Domain>/trg_<TableName>_BeforeInsert.sql`
4. Create `database/triggers/<Domain>/trg_<TableName>_BeforeUpdate.sql`
5. Create `database/procedures/<Domain>/usp_<Domain>_*.sql` for each CRUD operation
6. Add a new `database/migrations/V00N__<Description>.sql` containing only the new objects
7. **Update `database/README.md`** — add the new files to the Folder Structure, File Reference, and Execution Order sections
8. `migrations/V001__Initial_Schema.sql` is **not modified** — migrations are append-only
9. Update `Entity_*` and `Model_*` C# classes to match any new/changed columns

---

## Migration Files

The `migrations/` folder uses Flyway naming: `V###__Description.sql`

| Rule | Detail |
|------|--------|
| Version increments | `V001`, `V002`, `V003` — never reuse a number |
| Never edit applied | Once a migration is on the production server, never modify it |
| Self-contained | Each migration runs independently — include `USE \`MTM_Waitlist\`;` |
| Rollback comments | Include a `-- ROLLBACK:` comment block at the top for manual reversal |

---

## Running the Schema

```bash
# Full initial deploy (no seed data)
mysql -h 172.16.1.104 -u <admin_user> -p MTM_Waitlist < database/migrations/V001__Initial_Schema.sql

# Seed dev data (development only)
mysql -h 172.16.1.104 -u <admin_user> -p MTM_Waitlist < database/seed/01_Seed_Users.sql
mysql -h 172.16.1.104 -u <admin_user> -p MTM_Waitlist < database/seed/02_Seed_WaitlistEntries.sql

# Apply a new migration
mysql -h 172.16.1.104 -u <admin_user> -p MTM_Waitlist < database/migrations/V002__<Description>.sql
```

---

## Assumptions Pending Confirmation

See [`.github/assumptions/05102026-1000AM-Assumptions.md`](.github/assumptions/05102026-1000AM-Assumptions.md)
for the full list of schema decisions that require business confirmation.

Key open items:
- `WaitlistEntry` business columns (`FirstName`, `LastName`, `Status` ENUM values, etc.) are placeholders
- Authentication may use an external provider (AD/Entra ID) instead of the `Users` table
- `Model_WaitlistEntry` in C# must be expanded once the API schema is confirmed
