# MTM Waitlist — Database Admin Application Index

**Last Updated:** May 10, 2026  
**Project:** `MTM_Waitlist_Server` (new — to be created)  

---

## What This Is

A new WinUI desktop application (`MTM_Waitlist_Server.Admin`) that:

1. **Hosts the REST API** (ASP.NET/Kestrel, in-process) that all MAUI Waitlist Application clients connect to.
2. **Provides an admin dashboard** for managing the MySQL database, migrations, backups, and client sessions.

The MAUI app **cannot function** unless this admin app is running on the server (`172.16.1.104`).

---

## Implementation Order

```
DATABASE-01  Architecture (new solution, projects, DI setup)   ← Design first
    ↓
DATABASE-06  Migration System (restructures SQL files)         ← Do before any new SQL changes
    ↓
DATABASE-02  MySQL Status Dashboard                            ← First visible feature
DATABASE-03  Settings Management                               ← Required for all other modules
    ↓
DATABASE-04  Backup & Restore                                  ← Depends on settings (mysqldump path)
DATABASE-05  Client Kill Switch                                 ← Depends on API hosting
```

`DATABASE-06` is listed second because restructuring the SQL migration files must happen before new SQL objects are added for FEATURE-04 through FEATURE-08. Doing it later means more files to restructure.

---

## Document Summary

| # | Document | What It Covers |
|---|---------|----------------|
| [01](DATABASE-01-API-Server-Admin-Architecture.md) | Architecture | Solution structure, in-process Kestrel hosting, WinUI nav shell, deployment |
| [02](DATABASE-02-MySQL-Status-Dashboard.md) | Status Dashboard | Live DB stats, table sizes, active connections, in-process request log |
| [03](DATABASE-03-Settings-Management.md) | Settings | DB host/port/credentials, API port/JWT, backup config — DPAPI-encrypted storage |
| [04](DATABASE-04-Backup-and-Restore.md) | Backup & Restore | `mysqldump` automation, scheduled nightly backup, restore wizard with client kill |
| [05](DATABASE-05-Client-Kill-Switch.md) | Kill Switch | Remote shutdown of MAUI clients — individual or all, instant or countdown |
| [06](DATABASE-06-Intelligent-Migration-System.md) | Migration System | Incremental migrations, `SchemaVersions` table, procedure/trigger always-rerun |

---

## Key Architecture Decisions

### In-Process API Hosting

The admin app hosts the REST API inside the same process using ASP.NET `WebApplication`. One executable serves both:
- The WinUI admin window (for IT use on the server)
- The Kestrel HTTP listener on `:5000` (for MAUI clients on the LAN)

### `SchemaVersions` Table (Migration Tracking)

A new `SchemaVersions` table tracks which migration files have been applied. Migration files in `database/migrations/` are numbered and additive (`V002`, `V003`, ...). The monolithic `V001__Initial_Schema.sql` is frozen as a bootstrap-only file. All future schema changes go in new numbered migration files.

### SQL File Roles After DATABASE-06

| File type | Purpose | Runs when |
|---|---|---|
| `database/migrations/V*.sql` | Incremental schema changes (ALTER TABLE, new tables) | Once — tracked by `SchemaVersions` |
| `database/procedures/**/*.sql` | Stored procedure definitions | Every migration run (idempotent) |
| `database/triggers/**/*.sql` | Trigger definitions | Every migration run (idempotent) |
| `database/indexes/**/*.sql` | Index definitions | Every migration run (idempotent) |
| `database/schema/tables/**/*.sql` | Reference/documentation only | Never — manual reference only |
| `database/seed/**/*.sql` | Development seed data | Manual only |

### Kill Switch Protocol

MAUI clients poll `GET /api/admin/shutdown-signal` every 15 seconds as part of their normal session keepalive. The admin app sets an in-memory signal that clients detect on next poll. No real-time push (no SignalR) in v1 — the polling lag is acceptable for maintenance windows.

---

## New Files Created by This Feature Set

### SQL

| File | Purpose |
|---|---|
| `database/migrations/V002__Add_SchemaVersions_Table.sql` | Adds tracking table to existing installs |
| `database/schema/tables/System/SchemaVersions.sql` | Schema reference for the tracking table |
| `database/schema/admin/Admin_Users.sql` | MySQL user creation script (two users) for IT to run once |

### Updated SQL

| File | Change |
|---|---|
| `database/indexes/**/*_Indexes.sql` | Wrap all `CREATE INDEX` in the MySQL 5.7 idempotency procedure pattern |

---

## Resolved Decisions (All Documents)

| # | Decision | Answer |
|---|---|---|
| DATABASE-01 Q1 | Admin app runs on the server or remotely? | **On the server** (`172.16.1.104`) |
| DATABASE-01 Q2 | API lifecycle management | App **starts with Windows** (Task Scheduler). Admin UI has **Start / Stop / Restart** controls for the embedded Kestrel listener |
| DATABASE-01 Q3 | New project or extend deployment tooling? | **New standalone project** — `MTM_Waitlist_Server.Admin` |
| DATABASE-01 Q4 | In-process or external API hosting? | **In-process** — one exe, shared DI container |
| DATABASE-01 Q5 | Access control for admin app? | **Windows Authentication** — checks current Windows user's group membership on launch |
| DATABASE-02 Q1 | Which MySQL user for admin/dashboard ops? | **`waitlist_admin_dbupdater`** (elevated). REST API uses **`waitlist_admin_dbappuser`** (SELECT/EXECUTE only) |
| DATABASE-02 Q2 | Table stat granularity? | Dynamic query of `information_schema.TABLES` filtered to `mtm_waitlist` |
| DATABASE-02 Q3 | Auto-refresh or button? | **Auto-refresh every 30 seconds** using `SHOW GLOBAL STATUS`; `information_schema` always filtered by `TABLE_SCHEMA` |
| DATABASE-03 Q2 | API port change — kill switch? | **Yes — kill-switch countdown mandatory.** Minimum 60-second warning |
| DATABASE-03 Q3 | How do MAUI clients get updated API settings? | **`GET /api/server-info/waitlist`** discovery endpoint on startup |
| DATABASE-03 Q4 | MySQL user naming | **`waitlist_admin_dbappuser`** (API) and **`waitlist_admin_dbupdater`** (admin/backup/migration) |
| DATABASE-03 Q5 | Infor Visual SQL proxying | **Yes** — proxied through this API. Credentials DPAPI-encrypted; served internally at `GET /api/server-info/visual` |
| DATABASE-04 Q1 | Backup format | **`mysqldump`** — user-configurable folder, default `C:\MTM\WaitlistBackups\` |
| DATABASE-04 Q5 | Restore client kill — timer mandatory? | **Yes** — minimum 60-second countdown, no skip option |
| DATABASE-04 Q6 | Backup retention limit | **30 days maximum.** Manual clear by date or clear all |
| DATABASE-05 Q1 | "Immediately" — zero-delay or grace? | **15-second grace period.** Non-dismissable countdown overlay. True zero-warning never used |
| DATABASE-05 Q2 | In-flight wizard state on kill? | **Lost.** Operator restarts the wizard after reconnect |
| DATABASE-05 Q3 | Individual targets or all-at-once? | **Both.** By machine name, by user, or global |
| DATABASE-05 Q4 | Signal delivery technology? | **Polling** — `GET /api/admin/shutdown-signal` every 15 seconds |
| DATABASE-05 Q5 | Kill buttons during restore? | **Disabled** when restore is in progress. **Debounced** — cannot re-trigger while a signal is active |
| DATABASE-06 Q1 | Roll-forward only, or rollback support? | **Roll-forward only.** Bad migrations — restore from backup and apply corrected file |
| DATABASE-06 Q2 | Who runs migrations in production? | **Both** — auto-apply on startup (`Migrations:AutoApplyOnStartup`, default `false`) + manual "Apply Migrations" button |
| DATABASE-06 Q3 | Always re-run procedures/triggers/indexes? | **Yes — always re-run.** Only table migrations are gated by `SchemaVersions` |
| DATABASE-06 Q4 | Migration file source in production? | **Disk next to the exe** — `database\migrations` folder deployed alongside the binary |
| DATABASE-06 Q5 | V001 handling? | **Retired as bootstrap only.** Frozen. All future changes go in numbered files from V003 onward |

---

## Resolved Open Items

| Item | Document | Decision |
|---|---|---|
| MySQL advisory lock for concurrent migration prevention | DATABASE-06 | **Yes — add in v1.** `SELECT GET_LOCK('mtm_migration_lock', 30)` at migration run start |
| Infor Visual query proxying architecture | DATABASE-03 | **REST API is the single gateway for all data — both MySQL and SQL Server.** MAUI app never touches either database directly. All Visual reads are via REST API calls. `IService_VisualProxy` implementation is a TODO stub until FEATURE-02 scope is defined |
| Backup file encryption at rest | DATABASE-04 | **Not required in v1.** Rely on filesystem permissions (backup folder restricted to IT). Encryption deferred to v2 |
| Kill switch from MAUI app by supervisor role | DATABASE-05 | **Not required.** Admin UI only. No supervisor role needed for v1 |
| Email alerts on backup failure | DATABASE-04 | **Not needed — ever.** Warning banner in dashboard is sufficient |
| Exact row counts vs. InnoDB estimates in dashboard | DATABASE-02 | **Estimates confirmed correct for v1.** "Refresh Exact" button runs `COUNT(*)` on demand |
| Audit log for settings changes | DATABASE-03 | **Not required.** No local log file needed; activity log in dashboard is sufficient |
