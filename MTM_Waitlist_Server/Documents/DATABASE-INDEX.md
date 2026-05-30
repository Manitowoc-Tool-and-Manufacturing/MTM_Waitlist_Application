# MTM Waitlist — Database Admin Application Index

**Last Updated:** July 2026  
**Project:** `MTM_Waitlist_Server` (implemented — `DATABASE-01` through `DATABASE-07` complete)

---

## First-Time Setup Instructions — Visual Studio 2026

Use these steps before starting the database admin application work. They are written for a non-developer setup flow inside Visual Studio 2026.

These instructions were checked against `DATABASE-01` through `DATABASE-06`, the current MAUI project rules, the database rules, and the test rules. The important guardrails are:

- The server admin application is a **separate server solution** named `MTM_Waitlist_Server.slnx`.
- The existing client solution (WinUI 3 + Android) stays separate and continues to use `MTM_Waitlist_Application.slnx`.
- The client apps must talk to the server through REST API endpoints only.
- Server modules may reference `MTM_Waitlist_Server.Core` only.
- Server modules must not reference each other directly.
- The API project may reference `MTM_Waitlist_Server.Core` only.
- The Admin host project may reference Core, API, and all server modules.

### 1. Create a Separate Server Solution Folder on Disk

1. Open **File Explorer**.
2. Browse to `C:\Users\johnk\source\repos\MTM_Waitlist_Application`.
3. Create a new folder named `MTM_Waitlist_Server`.
4. Open the new `MTM_Waitlist_Server` folder.
5. Create these folders before creating projects:
   - `Hosts`
   - `Core`
   - `Modules`
   - `Tests`
   - `database`
6. Open the `database` folder.
7. Create these folders inside `database`:
   - `migrations`
   - `procedures`
   - `triggers`
   - `indexes`
   - `schema`
   - `seed`
8. Open the `schema` folder.
9. Create these folders inside `schema`:
   - `tables`
   - `admin`

The server admin application is a new standalone solution, not a set of projects added directly into the client solution.

### 2. Create the New Server Solution in Visual Studio

1. Open **Visual Studio Community 2026**.
2. Select **Create a new project**.
3. Search for **Blank Solution**.
4. Choose **Blank Solution**.
5. Select **Next**.
6. Set the solution name to `MTM_Waitlist_Server`.
7. Set the location to `C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Server`.
8. Select **Create**.
9. Confirm that Visual Studio created `MTM_Waitlist_Server.slnx` inside the `MTM_Waitlist_Server` folder.

### 3. Add Solution Folders Before Creating Projects

1. In **Solution Explorer**, right-click the solution name `MTM_Waitlist_Server`.
2. Select **Add** > **New Solution Folder**.
3. Name the folder `Hosts`.
4. Add these additional solution folders at the solution root:
   - `Core`
   - `Modules`
   - `Tests`

These solution folders must match the server architecture in `DATABASE-01-API-Server-Admin-Architecture.md`.

### 4. Create the Admin Desktop App Project

1. Right-click the `Hosts` solution folder.
2. Select **Add** > **New Project**.
3. Search for **WinUI**.
4. Choose the **Blank App, Packaged (WinUI 3 in Desktop)** template.
5. Select **Next**.
6. Set the project name to `MTM_Waitlist_Server.Admin`.
7. Set the location to `C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Server\Hosts`.
8. Select **Create**.

This project is the desktop window and main executable that IT will use on the server.

### 5. Create the In-Process API Project

1. Right-click the `Core` solution folder.
2. Select **Add** > **New Project**.
3. Search for **ASP.NET Core**.
4. Choose **ASP.NET Core Empty**.
5. Select **Next**.
6. Set the project name to `MTM_Waitlist_Server.Api`.
7. Set the location to `C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Server\Core`.
8. Select **Next**.
9. On the **Additional information** screen, use these settings:
   - **Framework:** `.NET 10.0 (Long Term Support)`
   - **Configure for HTTPS:** checked
   - **Enable container support:** unchecked
   - **Do not use top-level statements:** unchecked
   - **Use the dev.localhost TLD in the application URL:** unchecked
   - **Enlist in Aspire orchestration:** unchecked
10. Select **Create**.

This project contains the REST API that the MAUI waitlist clients will call. During implementation, the Admin project will start this API in-process instead of running it as a separate public server app.

### 6. Create the Shared Server Core Project

1. Right-click the `Core` solution folder.
2. Select **Add** > **New Project**.
3. Search for **Class Library**.
4. Choose **Class Library**.
5. Select **Next**.
6. Set the project name to `MTM_Waitlist_Server.Core`.
7. Set the location to `C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Server\Core`.
8. If Visual Studio asks for a framework, choose `.NET 10.0 (Long Term Support)`.
9. Select **Create**.

This project stores shared models, service interfaces, settings objects, and constants for the server solution.

### 7. Create the Feature Module Projects

Create one WinUI-capable class library project for each admin module below. Place all of them under `C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Server\Modules`.

1. Right-click the `Modules` solution folder.
2. Select **Add** > **New Project**.
3. Search for **WinUI Class Library**.
4. Choose the WinUI class library template if Visual Studio shows one.
5. If Visual Studio does not show a WinUI class library template, choose **Class Library** and make a note to add WinUI support during `DATABASE-01` implementation.
6. If Visual Studio asks for a framework, choose `.NET 10.0 (Long Term Support)`.
7. Create these projects one at a time:
   - `MTM_Waitlist_Server.Module.Dashboard`
   - `MTM_Waitlist_Server.Module.Settings`
   - `MTM_Waitlist_Server.Module.Backup`
   - `MTM_Waitlist_Server.Module.KillSwitch`
   - `MTM_Waitlist_Server.Module.Migrations`

These modules keep dashboard, settings, backup, kill-switch, and migration work separated while sharing the same server core contracts. They need WinUI support because the database documents place admin Views and ViewModels inside the module projects.

### 8. Create Test Projects

1. Right-click the `Tests` solution folder.
2. Select **Add** > **New Project**.
3. Search for **xUnit Test Project**.
4. If Visual Studio asks for a framework, choose `.NET 10.0 (Long Term Support)`.
5. Create these test projects one at a time under `C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Server\Tests`:
   - `MTM_Waitlist_Server.Core.Tests`
   - `MTM_Waitlist_Server.Api.Tests`
   - `MTM_Waitlist_Server.Module.Dashboard.Tests`
   - `MTM_Waitlist_Server.Module.Settings.Tests`
   - `MTM_Waitlist_Server.Module.Backup.Tests`
   - `MTM_Waitlist_Server.Module.KillSwitch.Tests`
   - `MTM_Waitlist_Server.Module.Migrations.Tests`

These projects are used to verify the server logic without opening the full admin window.

### 9. Add Project References

1. Right-click `MTM_Waitlist_Server.Admin`.
2. Select **Add** > **Project Reference**.
3. Check:
   - `MTM_Waitlist_Server.Core`
   - `MTM_Waitlist_Server.Api`
   - `MTM_Waitlist_Server.Module.Dashboard`
   - `MTM_Waitlist_Server.Module.Settings`
   - `MTM_Waitlist_Server.Module.Backup`
   - `MTM_Waitlist_Server.Module.KillSwitch`
   - `MTM_Waitlist_Server.Module.Migrations`
4. Select **OK**.
5. Right-click `MTM_Waitlist_Server.Api`.
6. Select **Add** > **Project Reference**.
7. Check `MTM_Waitlist_Server.Core`.
8. Select **OK**.
9. For each `MTM_Waitlist_Server.Module.*` project, add a project reference to `MTM_Waitlist_Server.Core` only.
10. For each test project, add a project reference to the project it tests.

Do not reference one module from another module. Cross-module behavior must go through shared Core interfaces and dependency injection.

### 10. Confirm the Solution Builds

1. In Visual Studio, select **Build** from the top menu.
2. Select **Build Solution**.
3. Wait for the build to finish.
4. If Visual Studio shows errors, fix those before starting `DATABASE-01`.

### 11. Keep the Client Solution Separate

The existing client solution (WinUI 3 + Android) remains at `C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Application\MTM_Waitlist_Application.slnx`.

Do not move client projects into the server solution. The client apps connect to the server through REST API endpoints only.

### 12. Start Implementation in the Correct Order

All seven DATABASE documents are now implemented. Refer to individual docs for implementation details:

1. `DATABASE-01-API-Server-Admin-Architecture.md` ✅
2. `DATABASE-06-Intelligent-Migration-System.md` ✅
3. `DATABASE-02-MySQL-Status-Dashboard.md` ✅
4. `DATABASE-03-Settings-Management.md` ✅
5. `DATABASE-04-Backup-and-Restore.md` ✅
6. `DATABASE-05-Client-Kill-Switch.md` ✅
7. `DATABASE-07-First-Run-Setup.md` ✅

---

## What This Is

A new WinUI desktop application (`MTM_Waitlist_Server.Admin`) that:

1. **Hosts the REST API** (ASP.NET/Kestrel, in-process) that all Waitlist client apps (WinUI 3 on Windows, MAUI on Android) connect to.
2. **Provides an admin dashboard** for managing the MySQL database, migrations, backups, and client sessions.

In production, the client apps **cannot function** unless this admin app is running on the server (`172.16.1.104`). During local debugging, the same admin app can run on a developer workstation, with both the API listener and the first-run MySQL host defaulting to `localhost` unless `server-settings.json` already has saved values.

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
| [05](DATABASE-05-Client-Kill-Switch.md) | Kill Switch | Remote shutdown of client apps — individual or all, instant or countdown |
| [06](DATABASE-06-Intelligent-Migration-System.md) | Migration System | Incremental migrations, `SchemaVersions` table, procedure/trigger always-rerun |
| [07](DATABASE-07-First-Run-Setup.md) | First-Run Setup | Fresh-install detection probe, auth gate fallback, 3-step wizard, degraded mode |

---

## Key Architecture Decisions

### In-Process API Hosting

The admin app hosts the REST API inside the same process using ASP.NET `WebApplication`. One executable serves both:
- The WinUI admin window (for IT use on the server)
- The Kestrel HTTP listener on `:5000` (for client apps on the LAN)

### `SchemaVersions` Table (Migration Tracking)

A new `SchemaVersions` table tracks which migration files have been applied. Migration files in `Database/migrations/` are numbered and additive (`V002`, `V003`, ...). The monolithic `V001__Initial_Schema.sql` is frozen as a bootstrap-only file. All future schema changes go in new numbered migration files.

### SQL File Roles After DATABASE-06

| File type | Purpose | Runs when |
|---|---|---|
| `Database/migrations/V*.sql` | Incremental schema changes (ALTER TABLE, new tables) | Once — tracked by `SchemaVersions` |
| `Database/procedures/**/*.sql` | Stored procedure definitions | Every migration run (idempotent) |
| `Database/triggers/**/*.sql` | Trigger definitions | Every migration run (idempotent) |
| `Database/indexes/**/*.sql` | Index definitions | Every migration run (idempotent) |
| `Database/schema/tables/**/*.sql` | Reference/documentation only | Never — manual reference only |
| `Database/seed/**/*.sql` | Development seed data | Manual only |

### Kill Switch Protocol

Client apps (WinUI 3 on Windows, MAUI on Android) poll `GET /api/admin/shutdown-signal` every 15 seconds as part of their normal session keepalive. The admin app sets an in-memory signal that clients detect on next poll. No real-time push (no SignalR) in v1 — the polling lag is acceptable for maintenance windows.

---

## New Files Created by This Feature Set

### SQL

| File | Purpose |
|---|---|
| `Database/migrations/V002__Add_SchemaVersions_Table.sql` | Adds tracking table to existing installs |
| `Database/schema/tables/System/SchemaVersions.sql` | Schema reference for the tracking table |
| `Database/schema/admin/Admin_Users.sql` | MySQL user creation script (two users) for IT to run once |

### C# (DATABASE-07)

| File | Purpose |
|---|---|
| `Core/Models/FirstRun/FirstRunStatus.cs` | Enum: `Ready`, `MySqlUnreachable`, `SchemaMissing`, `NoAdminUser` |
| `Core/Models/FirstRun/Model_FirstRunProbeResult.cs` | Probe result with status + optional error message |
| `Core/Interfaces/FirstRun/IService_FirstRun.cs` | `ProbeAsync()`, `IsFirstRunRequiredAsync()`, `MarkCompleteAsync()` |
| `Hosts/.../Services/Service_FirstRun.cs` | MySqlConnector-based implementation |
| `Hosts/.../ViewModels/ViewModel_FirstRun.cs` | Step tracking, form fields, wizard commands |
| `Hosts/.../Views/View_FirstRun.xaml` | 3-step wizard UI |
| `Hosts/.../Views/View_FirstRun.xaml.cs` | Code-behind |

### C# (Window Sizer)

| File | Purpose |
|---|---|
| `Core/Interfaces/Window/IService_WindowSizer.cs` | `ApplyFirstRunSize()`, `ApplyNormalSize()`, `CenterOnMonitor()` |
| `Hosts/.../Services/Service_WindowSizer.cs` | Resizes and centers the main window for first-run vs. normal launch |

### C# (Dashboard — Active Connection Safety Guard)

| File | Changed |
|---|---|
| `Core/Models/Dashboard/Model_ActiveConnection.cs` | Added `IsCritical`, `CanKill`, `KillTooltip`, `CriticalUsers` set, `DetectCritical()` |
| `Core/Models/Dashboard/Model_ConnectionGroup.cs` | New — groups processlist rows by user for the expanded dashboard UI |
| `Core/MTM_Waitlist_Server.Api/Services/Service_Dashboard.cs` | Sets `IsCritical` at query time; `KillConnectionAsync` re-verifies before issuing `KILL` |
| `Modules/.../ViewModels/ViewModel_Dashboard.cs` | `KillConnectionCommand` returns early for critical connections; `GroupedConnections` observable |
| `Modules/.../Views/View_Dashboard.xaml` | Kill button uses `IsEnabled={x:Bind CanKill}` and `ToolTipService.ToolTip={x:Bind KillTooltip}` |

### Updated SQL

| File | Change |
|---|---|
| `Database/indexes/**/*_Indexes.sql` | Wrap all `CREATE INDEX` in the MySQL 5.7 idempotency procedure pattern |

---

## Resolved Decisions (All Documents)

| # | Decision | Answer |
|---|---|---|
| DATABASE-01 Q1 | Admin app runs on the server or remotely? | **On the server** (`172.16.1.104`) |
| DATABASE-01 Q2 | API lifecycle management | App **starts with Windows** (Task Scheduler). Admin UI has **Start / Stop / Restart** controls for the embedded Kestrel listener |
| DATABASE-01 Q3 | New project or extend deployment tooling? | **New standalone project** — `MTM_Waitlist_Server.Admin` |
| DATABASE-01 Q4 | In-process or external API hosting? | **In-process** — one exe, shared DI container |
| DATABASE-01 Q5 | Access control for admin app? | **MySQL Role-Based Auth** — `IService_AdminAuth` queries `mtm_waitlist.Users`; `Admin` and `Developer` roles are permitted. Falls back to Windows `BUILTIN\Administrators` group only during first-run when MySQL is unreachable (DATABASE-07) |
| DATABASE-07 Q1 | How is a fresh install detected? | **Three-step probe:** MySQL reachable → schema exists → active Admin/Developer user exists |
| DATABASE-07 Q2 | Auth gate on first run? | **Falls back to `BUILTIN\Administrators`** Windows group when DB probe fails |
| DATABASE-07 Q3 | First-run UI | **Inline 3-step wizard** inside the admin shell — Connect, Bootstrap, Create User |
| DATABASE-07 Q4 | Wizard steps skippable? | **No** — each step gates the next; nav is locked until wizard completes |
| DATABASE-07 Q5 | MySQL unreachable at launch? | **Settings opens automatically** with a banner; rest of nav disabled |
| DATABASE-07 Q7 | First-run state persisted? | **`FirstRunComplete` flag** in `server-settings.json` — set after Step 3 succeeds |
| DATABASE-02 Q1 | Which MySQL user for admin/dashboard ops? | **`waitlist_admin_dbupdater`** (elevated). REST API uses **`waitlist_admin_dbappuser`** (SELECT/EXECUTE only) |
| DATABASE-02 Q2 | Table stat granularity? | Dynamic query of `information_schema.TABLES` filtered to `mtm_waitlist` |
| DATABASE-02 Q3 | Auto-refresh or button? | **Auto-refresh every 30 seconds** using `SHOW GLOBAL STATUS`; `information_schema` always filtered by `TABLE_SCHEMA` |
| DATABASE-03 Q2 | API port change — kill switch? | **Yes — kill-switch countdown mandatory.** Minimum 60-second warning |
| DATABASE-03 Q3 | How do client apps get updated API settings? | **`GET /api/server-info/waitlist`** discovery endpoint on startup |
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
