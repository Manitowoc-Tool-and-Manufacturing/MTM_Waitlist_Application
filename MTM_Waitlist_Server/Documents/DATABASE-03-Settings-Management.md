# DATABASE-03: Settings Management

---

## Confirmed Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Who can change settings? | **Windows Authentication** gates the entire admin app (DATABASE-01 Q5). No additional passphrase needed inside settings |
| 2 | Can the API port be changed while running? | **Yes — kill-switch countdown is triggered automatically** before any restart-required setting is applied. The banner offers "Restart Now" or "Restart in 5 min (notify clients)" |
| 3 | How do client apps get updated connection settings? | **Discovery endpoint** `GET /api/server-info/waitlist` — client apps query this on startup to get the current API host/port before any other call |
| 4 | Which MySQL users does the admin app use? | **Two named users:** `waitlist_admin_dbappuser` (REST API — SELECT/EXECUTE only) and `waitlist_admin_dbupdater` (admin operations — dashboard, backup, migrations — elevated privileges) |
| 5 | Should Visual SQL queries be proxied? | **Yes.** All Infor Visual SQL queries are proxied through this API. The Visual connection string is stored here, served at `GET /api/server-info/visual`, and uses credentials `SHOP2` / `SHOP` (configurable, DPAPI-encrypted) |

---

## Overview

The Settings module stores all runtime-configurable values for the server admin application and the hosted REST API. Settings are persisted to a JSON file outside the install folder so they survive application updates.

Settings file location:
```
%ProgramData%\MTM\WaitlistServer\server-settings.json
```

The file is read at app startup, validated, and stored in a singleton `ServerSettings` object injected throughout both the WinUI UI and the ASP.NET API.

---

## Settings Categories

### 1. Database Connection (MySQL — mtm_waitlist)

| Setting | Default | Description |
|---|---|---|
| `Database:Host` | `localhost` in Debug, `172.16.1.104` otherwise | MySQL server hostname or IP |
| `Database:Port` | `3306` | MySQL port |
| `Database:DatabaseName` | `mtm_waitlist` | Target database name (must be lowercase) |
| `Database:AppUsername` | `waitlist_admin_dbappuser` | MySQL user for the REST API (SELECT/EXECUTE only) |
| `Database:AppPassword` | _(empty)_ | Stored encrypted with DPAPI |
| `Database:UpdaterUsername` | `waitlist_admin_dbupdater` | MySQL user for admin ops — dashboard, backup, migrations (PROCESS, KILL, RELOAD) |
| `Database:UpdaterPassword` | _(empty)_ | Stored encrypted with DPAPI |
| `Database:ConnectionTimeout` | `10` | Connection timeout in seconds |
| `Database:CommandTimeout` | `30` | Query timeout in seconds |

### 2. Infor Visual SQL Server

| Setting | Default | Description |
|---|---|---|
| `Visual:Host` | _(empty)_ | SQL Server hostname or IP for the Infor Visual database |
| `Visual:Port` | `1433` | SQL Server port |
| `Visual:DatabaseName` | _(empty)_ | Visual database name (e.g., `VISUAL`) |
| `Visual:Username` | `SHOP2` | SQL Server login — all API users share this account |
| `Visual:Password` | `SHOP` | Stored encrypted with DPAPI. Default is the plant standard; change here if rotated |
| `Visual:ConnectionTimeout` | `15` | Connection timeout in seconds |
| `Visual:CommandTimeout` | `60` | Query timeout in seconds (Visual queries can be slow) |

### 3. API Server

| Setting | Default | Description |
|---|---|---|
| `Api:ListenPort` | `5000` | Kestrel listen port |
| `Api:AllowedOrigins` | `*` | CORS origins (for admin use — do not expose externally) |
| `Api:JwtSecret` | _(generated)_ | Secret key for JWT signing (auto-generated on first run, stored encrypted) |
| `Api:JwtExpiryMinutes` | `480` | JWT access token lifetime (8 hours) |
| `Api:RefreshTokenExpiryDays` | `30` | Refresh token lifetime |

### 4. Backup

| Setting | Default | Description |
|---|---|---|
| `Backup:OutputFolder` | `C:\MTM\WaitlistBackups` | Where `.sql` dump files are saved |
| `Backup:MysqldumpPath` | _(auto-detected)_ | Path to `mysqldump.exe` |
| `Backup:AutoBackupEnabled` | `true` | Enable scheduled automatic backups |
| `Backup:AutoBackupSchedule` | `02:00` | Time to run daily auto-backup (24h format) |
| `Backup:RetentionDays` | `30` | Delete backups older than this many days (approximately 1 month) |

### 5. Migrations

| Setting | Default | Description |
|---|---|---|
| `Migrations:AutoApplyOnStartup` | `false` | Auto-apply pending migration files when the admin app starts. Set to `true` in dev for convenience; keep `false` in production so IT explicitly approves each migration by pressing "Apply Migrations" |
| `Migrations:MigrationFolder` | `database\migrations` | Path to migration files, relative to the exe. Can be absolute if files are stored elsewhere |

### 6. Admin Access

| Setting | Default | Description |
|---|---|---|
| `Admin:RequiredWindowsGroup` | `BUILTIN\Administrators` | Windows group whose members are allowed to open the admin app. Change to a domain group (e.g., `MTMFG\IT-Admins`) if the server is domain-joined |

### 7. Notifications (stubbed for v1)

| Setting | Default | Description |
|---|---|---|
| `Notifications:KillSwitchDefaultWarningSeconds` | `300` | Default countdown (5 min) before forced disconnect |

---

## Settings UI Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  SETTINGS                                          [Save]  [↺]  │
├──────────────────┬──────────────────────────────────────────────┤
│  ▶ Database      │  DATABASE CONNECTION (MySQL — mtm_waitlist)   │
│  ▶ Infor Visual  │  ┌─────────────────────────────────────────┐  │
│  ▶ API Server    │  │ Host     [localhost          ]          │  │
│  ▶ Backup        │  │ Port     [3306               ]          │  │
│  ▶ Notifications │  │ Database [mtm_waitlist        ]          │  │
│                  │  │                                          │  │
│                  │  │ App credentials (SELECT/EXECUTE only)    │  │
│                  │  │ Username [waitlist_admin_dbappuser ]     │  │
│                  │  │ Password [••••••••            ] [Test]   │  │
│                  │  │                                          │  │
│                  │  │ Updater credentials (admin/backup/mig.)  │  │
│                  │  │ Username [waitlist_admin_dbupdater ]     │  │
│                  │  │ Password [••••••••            ] [Test]   │  │
│                  │  └─────────────────────────────────────────┘  │
│                  │  ✅ Connection test passed — mtm_waitlist     │
│                  │     MySQL 5.7.41 — 4 tables                   │
└──────────────────┴──────────────────────────────────────────────┘
```

When **Infor Visual** is selected in the left nav:

```
│  ▶ Infor Visual  │  INFOR VISUAL SQL SERVER                      │
│                  │  ┌─────────────────────────────────────────┐  │
│                  │  │ Host     [192.168.1.50        ]          │  │
│                  │  │ Port     [1433               ]          │  │
│                  │  │ Database [VISUAL              ]          │  │
│                  │  │                                          │  │
│                  │  │ Shared credentials (all API users)       │  │
│                  │  │ Username [SHOP2               ]          │  │
│                  │  │ Password [••••••••            ] [Test]   │  │
│                  │  └─────────────────────────────────────────┘  │
│                  │  ✅ Connection test passed — VISUAL           │
│                  │     SQL Server 2019                           │
```

Left nav: category pills (Database / Infor Visual / API Server / Backup / Migrations / Admin Access / Notifications).  
Right panel: fields for the selected category.  
Bottom of panel: validation status / test result.

"Save" writes the JSON file and notifies affected services. Some settings (API port, JWT secret) require an API restart. When one of these is changed, saving immediately triggers the kill-switch countdown and shows a banner:

```
⚠ API restart required for port change to take effect.
  Connected clients have been notified.
   [Restart Now]  [Restart in 5 min (notify clients)]
```

The "Restart in 5 min" option calls `IService_KillSwitch.SetShutdownSignal(new ShutdownTarget(ShutdownTargetType.All), warningSeconds: 300)` before stopping and restarting Kestrel. "Restart Now" uses the minimum 60-second countdown (same floor as the restore workflow).

---

## Password Storage

Admin and API passwords are **never stored in plaintext**. The `server-settings.json` file stores DPAPI-encrypted strings:

```csharp
// Encrypt before writing
string encrypted = Convert.ToBase64String(
    ProtectedData.Protect(
        Encoding.UTF8.GetBytes(plaintext),
        null,
        DataProtectionScope.LocalMachine));   // LocalMachine so any admin user on this server can decrypt

// Decrypt when reading
string plaintext = Encoding.UTF8.GetString(
    ProtectedData.Unprotect(
        Convert.FromBase64String(encrypted),
        null,
        DataProtectionScope.LocalMachine));
```

`DataProtectionScope.LocalMachine` is used (not `CurrentUser`) so that if the server runs the API as a Windows Service under a service account, the service account can still read the settings.

The JWT secret is auto-generated on first run using `RandomNumberGenerator.GetBytes(64)` and stored encrypted. It does not need to match between deployments (tokens issued before a secret rotation are simply invalidated — users re-login).

---

## Validation on Save

Before writing, the settings service validates:

| Validation | Rule |
|---|---|
| Host not empty | Required |
| Port range | 1–65535 |
| Database name | Lowercase only, no spaces, matches `[a-z0-9_]+` |
| Admin password not empty | Required |
| API port not in use | Attempt `TcpListener.Start()` on the new port to check availability (only if port changed) |
| Backup folder exists or is creatable | `Directory.Exists()` / `Directory.CreateDirectory()` |
| `mysqldump.exe` path valid | `File.Exists()` or auto-detect |
| Auto-backup time | `HH:mm` format |
| Visual Host not empty (if Visual enabled) | Required when `Visual:Enabled = true` |
| Visual Username not empty (if Visual enabled) | Required |

Validation errors are shown inline next to each field (red border + error text below field).

---

## Connection Test Button

"Test" next to each MySQL credential block opens a `MySqlConnection` with those credentials and runs `SELECT VERSION()`. "Test" next to the Visual credentials block opens a `SqlConnection` (Microsoft.Data.SqlClient) and runs `SELECT @@VERSION`.

```csharp
// MySQL test (App or Updater credentials)
await using var conn = new MySqlConnection(BuildMySqlConnectionString(creds));
await conn.OpenAsync();
var version = await conn.QuerySingleAsync<string>("SELECT VERSION()");
// ✅ Connected — MySQL 5.7.41

// Infor Visual test
await using var conn = new SqlConnection(BuildVisualConnectionString(visualCreds));
await conn.OpenAsync();
var version = await conn.QuerySingleAsync<string>("SELECT @@VERSION");
// ✅ Connected — SQL Server 2019
```

Result shown inline below the button. Failure shows the exception message truncated to 120 characters.

---

## `mysqldump` Auto-Detection

On first run, if `Backup:MysqldumpPath` is empty, the settings service scans common installation paths:

```csharp
private static readonly string[] MysqldumpSearchPaths =
[
    @"C:\Program Files\MySQL\MySQL Server 5.7\bin\mysqldump.exe",
    @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe",
    @"C:\MAMP\bin\mysql\bin\mysqldump.exe",
    @"C:\MAMP\bin\mariadb\bin\mysqldump.exe",
    // ... common paths
];
```

If found, path is pre-populated in the Backup settings. The user can override it with a "Browse…" folder picker.

---

## ViewModel

```
Module.Settings/
  ViewModels/
    ViewModel_Settings.cs          ← parent VM — category selection
    ViewModel_Settings_Database.cs ← MySQL App + Updater credentials
    ViewModel_Settings_Visual.cs   ← Infor Visual SQL Server connection
    ViewModel_Settings_Api.cs
    ViewModel_Settings_Backup.cs
    ViewModel_Settings_Notifications.cs
  Views/
    View_Settings.xaml
    View_Settings.xaml.cs
```

---

## `ServerSettings` Model (Core)

```csharp
// Core/Models/Settings/ServerSettings.cs
public class ServerSettings
{
    public DatabaseSettings Database { get; set; } = new();
    public VisualSettings Visual { get; set; } = new();
    public ApiSettings Api { get; set; } = new();
    public BackupSettings Backup { get; set; } = new();
    public MigrationsSettings Migrations { get; set; } = new();
    public AdminSettings Admin { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
}

// Core/Models/Settings/DatabaseSettings.cs
public class DatabaseSettings
{
#if DEBUG
    public string Host { get; set; } = "localhost";
#else
    public string Host { get; set; } = "172.16.1.104";
#endif
    public int Port { get; set; } = 3306;
    public string DatabaseName { get; set; } = "mtm_waitlist";
    public string AppUsername { get; set; } = "waitlist_admin_dbappuser";
    public string AppPassword { get; set; } = string.Empty;    // DPAPI-encrypted in JSON
    public string UpdaterUsername { get; set; } = "waitlist_admin_dbupdater";
    public string UpdaterPassword { get; set; } = string.Empty; // DPAPI-encrypted in JSON
    public int ConnectionTimeout { get; set; } = 10;
    public int CommandTimeout { get; set; } = 30;
}

// Core/Models/Settings/VisualSettings.cs
public class VisualSettings
{
    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 1433;
    public string DatabaseName { get; set; } = string.Empty;
    public string Username { get; set; } = "SHOP2";          // shared by all API users
    public string Password { get; set; } = string.Empty;      // DPAPI-encrypted in JSON (default: SHOP)
    public int ConnectionTimeout { get; set; } = 15;
    public int CommandTimeout { get; set; } = 60;
}

// Core/Models/Settings/MigrationsSettings.cs
public class MigrationsSettings
{
    public bool AutoApplyOnStartup { get; set; } = false;   // default false for production safety
    public string MigrationFolder { get; set; } = @"database\migrations";
}
```

`IService_SettingsStore` reads/writes this model. It is registered as a singleton and injected into all services that need configuration values (connection string builder, backup service, JWT token handler, Visual proxy service, etc.).

---

## Discovery Endpoints

These read-only endpoints are served by the API and called by client apps on startup. They require a valid JWT.

### `GET /api/server-info/waitlist`

Returns the current Waitlist API host and port. Client apps call this first to resolve the API base URL dynamically, so a port change in Settings does not require redeploying the client apps.

```json
{
  "host": "172.16.1.104",
  "port": 5000,
  "apiBaseUrl": "http://172.16.1.104:5000"
}
```

### `GET /api/server-info/visual`

Returns the Infor Visual SQL Server connection info for the API's proxy service. **This endpoint is server-internal only** — called by the API's `IService_VisualProxy` to build its connection string. It is not exposed to client apps directly; clients call Visual-specific endpoints (e.g., `GET /api/visual/workcenter/{id}`) and the API proxies the SQL query.

The Visual credentials (`SHOP2` / `SHOP`) are never transmitted to client apps. They are stored encrypted in `server-settings.json` and only used server-side.

---

## Open Decisions

- **Multi-environment profiles:** *For v1: No — single-environment. Dev uses local `appsettings.json`.*
- **Visual proxy — which queries are supported?** The REST API is the **single gateway for all data access** — both MySQL (`mtm_waitlist`) and Infor Visual (SQL Server). The MAUI app never connects to either database directly; all reads and writes go through REST API calls. Visual is read-only from the MAUI app's perspective. The exact Visual endpoints and SQL queries are a `// TODO` stub in `IService_VisualProxy` until FEATURE-02 scope is defined. The `Visual:Enabled` setting allows the app to run without a Visual connection during initial setup.
- **Visual `Enabled` toggle:** If `Visual:Enabled = false`, all `GET /api/visual/*` endpoints return `503 Service Unavailable`.
