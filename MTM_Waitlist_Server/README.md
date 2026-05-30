# MTM Waitlist Server Admin

The **MTM Waitlist Server Admin** is a WinUI 3 desktop application that normally runs on the server machine (`172.16.1.104`). During local development it can also be launched on a developer workstation for debugging against a user-configured database host.

1. **Hosts the REST API** — an ASP.NET Core / Kestrel listener (`:5000`) that the MAUI Waitlist Application clients connect to over the LAN.
2. **Provides an admin dashboard** — live MySQL status, active connections, backup/restore, migration management, and a client kill switch.

The MAUI application **cannot function** unless this admin app is running on the server.

During debug sessions, the in-process API defaults to `http://localhost:5000` and the first-run database host default is also `localhost`, so a developer can run the full stack on one machine without editing settings first. Persisted values in `server-settings.json` still win. When no settings file exists in a non-debug deployment, the database host default remains `172.16.1.104`.

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| UI Framework | WinUI 3 (.NET 10, Windows App SDK) |
| MVVM | CommunityToolkit.Mvvm |
| REST API Hosting | ASP.NET Core / Kestrel (in-process) |
| MySQL Access | `MySqlConnector` |
| DI Container | `Microsoft.Extensions.DependencyInjection` |
| Settings Storage | JSON file in `%ProgramData%\MTM\WaitlistServer\` |

---

## Solution Structure

```
MTM_Waitlist_Server/
├── Hosts/
│   └── MTM_Waitlist_Server.Admin/          ← WinUI entry point (exe)
├── Core/
│   ├── MTM_Waitlist_Server.Core/           ← Interfaces, models, constants
│   └── MTM_Waitlist_Server.Api/            ← ASP.NET controllers + services
├── Modules/
│   ├── MTM_Waitlist_Server.Module.Dashboard/
│   ├── MTM_Waitlist_Server.Module.Settings/
│   ├── MTM_Waitlist_Server.Module.Backup/
│   ├── MTM_Waitlist_Server.Module.KillSwitch/
│   └── MTM_Waitlist_Server.Module.Migrations/
├── Tests/                                  ← xUnit test projects (one per module)
├── Database/
│   ├── migrations/   ← V001__, V002__… incremental schema files
│   ├── procedures/   ← stored procedure definitions (always re-run)
│   ├── triggers/     ← trigger definitions (always re-run)
│   ├── indexes/      ← index definitions (always re-run)
│   ├── schema/       ← reference SQL (not executed)
│   └── seed/         ← dev-only seed data (manual)
└── Documents/        ← DATABASE-01 through DATABASE-07 specification files
```

---

## Authorization

Access is controlled by **MySQL role**. On launch the app reads the current Windows username and queries `mtm_waitlist.Users`. Roles `Admin` and `Developer` are permitted. No password prompt is shown — Windows identity is the authentication factor.

### First-Run Exception

On a brand-new server where MySQL is not yet installed, the app detects the missing database and launches the **First Run Wizard** instead of the normal dashboard. The wizard is protected by Windows `BUILTIN\Administrators` group membership. See [DATABASE-07](Documents/DATABASE-07-First-Run-Setup.md) for details.

### Degraded Mode

If `FirstRunComplete` is set in `server-settings.json` but MySQL becomes unreachable on a subsequent launch, the app opens in degraded mode — Settings is accessible; all other nav is disabled.

---

## First-Run Wizard

On a fresh server the wizard guides an IT admin through three steps:

1. **Configure & Test Connection** — enter MySQL host, port, and credentials.
2. **Run Bootstrap Migration** — creates the `mtm_waitlist` schema and all tables.
3. **Create First Admin User** — inserts the first row into `mtm_waitlist.Users`.

After the wizard completes, the app relaunches into the normal dashboard.

---

## Dashboard

The dashboard home screen shows:

- Summary cards: API uptime, database size, connection count, waitlist totals.
- **Table Status** grid: per-table row estimates, data size, and last updated time.
- **Active Connections** panel: processlist rows grouped by user. Click a user row to expand individual threads with per-thread Kill buttons.
- **Recent Activity** log: in-process ring buffer of the last 200 API requests.

### Critical Connection Protection

Connections belonging to the app's internal service accounts (`waitlist_admin_dbupdater`, `waitlist_admin_dbappuser`) or originating from `localhost` are marked **Critical**. Their Kill buttons are disabled in the UI and guarded at both the ViewModel and service layers. Killing a critical connection crashes the admin application.

---

## MySQL Users

Two dedicated MySQL users are required. Run `Database/schema/admin/Admin_Users.sql` once manually using a root-level MySQL account:

| MySQL User | Used By | Privileges |
|-----------|---------|------------|
| `waitlist_admin_dbappuser` | REST API (all client requests) | `EXECUTE`, `SELECT` on `mtm_waitlist.*` |
| `waitlist_admin_dbupdater` | Admin dashboard, backup, migrations | `ALL` on `mtm_waitlist.*` + `PROCESS`, `REPLICATION CLIENT`, `KILL` on `*.*` |

Passwords are stored in `%ProgramData%\MTM\WaitlistServer\server-settings.json` (DPAPI-encrypted) and **never** in source control.

---

## Migration System

Schema changes are applied as numbered migration files under `Database/migrations/`:

| File | Behaviour |
|------|-----------|
| `V001__Initial_Schema.sql` | Bootstrap only — creates the schema from scratch |
| `V002__*.sql`, `V003__*.sql`, … | Incremental changes — applied once, tracked in `SchemaVersions` table |
| Procedures / Triggers / Indexes | Always re-run on every migration pass (idempotent `DROP IF EXISTS` + `CREATE`) |

Auto-apply on startup is controlled by `Migrations:AutoApplyOnStartup` in settings (default `false`). Migrations can also be triggered manually from the Migrations module in the admin UI.

---

## Settings Storage

Runtime settings (connection strings, API port, backup path) are stored at:

```
%ProgramData%\MTM\WaitlistServer\server-settings.json
```

This path survives application updates. Sensitive values (MySQL passwords, JWT secret) are DPAPI-encrypted at rest. See [DATABASE-03](Documents/DATABASE-03-Settings-Management.md) for the full settings schema.

Default behavior when `server-settings.json` does not exist yet:

- Debug builds default the database host to `localhost`
- Non-debug builds default the database host to `172.16.1.104`
- Database port defaults to `3306`
- Debug builds bind the API to `http://localhost:5000`
- Non-debug builds keep the broader default listen address

---

## Backup & Restore

Nightly backups are created using `mysqldump` and stored in a configurable folder (default `C:\MTM\WaitlistBackups\`). Backups older than 30 days are pruned automatically. Restoring requires a kill-switch countdown (minimum 60 seconds) to gracefully disconnect MAUI clients before the restore begins. See [DATABASE-04](Documents/DATABASE-04-Backup-and-Restore.md).

---

## Client Kill Switch

The Kill Switch module lets IT remotely shut down connected MAUI clients — individually (by machine or user) or globally. Clients poll `GET /api/admin/shutdown-signal` every 15 seconds. When a signal is active, clients receive a non-dismissable 15-second countdown overlay before closing. See [DATABASE-05](Documents/DATABASE-05-Client-Kill-Switch.md).

---

## Building & Running

```powershell
# Build the solution
dotnet build MTM_Waitlist_Server.slnx

# Run all tests
dotnet test MTM_Waitlist_Server.slnx

# Launch (Windows only — set MTM_Waitlist_Server.Admin as startup project in VS 2026)
# F5 in Visual Studio 2026
```

The application targets `net10.0-windows10.0.19041.0` and requires Windows 10 version 1903 or later.

---

## Documentation

Full specification documents are in `Documents/`:

| Document | Topic |
|----------|-------|
| [DATABASE-01](Documents/DATABASE-01-API-Server-Admin-Architecture.md) | Architecture, solution structure, in-process API hosting |
| [DATABASE-02](Documents/DATABASE-02-MySQL-Status-Dashboard.md) | Status dashboard, active connections, kill-connection safety |
| [DATABASE-03](Documents/DATABASE-03-Settings-Management.md) | Settings management, DPAPI encryption |
| [DATABASE-04](Documents/DATABASE-04-Backup-and-Restore.md) | Backup and restore with `mysqldump` |
| [DATABASE-05](Documents/DATABASE-05-Client-Kill-Switch.md) | Remote MAUI client shutdown |
| [DATABASE-06](Documents/DATABASE-06-Intelligent-Migration-System.md) | Incremental migration runner |
| [DATABASE-07](Documents/DATABASE-07-First-Run-Setup.md) | First-run wizard, degraded mode, window sizing |
| [DATABASE-INDEX](Documents/DATABASE-INDEX.md) | Index, setup instructions, resolved decisions |
