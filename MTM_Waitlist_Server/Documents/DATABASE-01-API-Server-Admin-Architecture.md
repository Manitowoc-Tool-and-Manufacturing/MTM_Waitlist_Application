# DATABASE-01: API Server Admin Application — Architecture

---

## Confirmed Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Where does the admin app run? | **On the server machine** (`172.16.1.104`) — same box as MySQL |
| 2 | How does the REST API start and who manages its lifecycle? | The admin app (which hosts the API in-process) **starts with Windows** via Task Scheduler. The admin UI provides **Start / Stop / Restart** controls for the embedded Kestrel listener |
| 3 | New project or extend `MTM.DatabaseDeployment.Tooling`? | **New standalone project** — `MTM_Waitlist_Server.Admin` |
| 4 | Host the API in-process or manage externally? | **In-process** — one exe hosts both the WinUI admin window and the Kestrel API |
| 5 | Who has access to the admin app? | **MySQL Role-Based Auth** — the app queries `mtm_waitlist.Users` and permits Windows users whose `Role` is `Admin` or `Developer`. On a fresh machine where MySQL does not yet exist, the First Run wizard launches instead (see DATABASE-07) |

---

## Overview

The **MTM Waitlist Server Admin** is a new WinUI desktop application that serves dual purposes:

1. It **hosts the REST API** (ASP.NET/Kestrel, in-process) that the MAUI Waitlist Application connects to at `http://172.16.1.104:5000`.
2. It provides an **admin dashboard** for managing the MySQL database, migrations, backups, and connected client sessions.

Before the MAUI Waitlist Application can function, this admin app must be running on the server. It is the "server process" that operators and handlers depend on.

```
┌──────────────────────────────────────────────────────────┐
│  MTM Waitlist Server Admin (WinUI — runs on 172.16.1.104)│
│                                                          │
│  ┌───────────────────────────────────────────────────┐   │
│  │  In-Process REST API (ASP.NET Kestrel)            │   │
│  │  Listening: http://0.0.0.0:5000                   │   │
│  │  Endpoints: /api/waitlist, /api/auth, ...         │   │
│  └────────────────┬──────────────────────────────────┘   │
│                   │ MySqlConnection                        │
│  ┌────────────────▼──────────────────────────────────┐   │
│  │  MySQL 5.7 — mtm_waitlist (localhost:3306)        │   │
│  └───────────────────────────────────────────────────┘   │
│                                                          │
│  Admin UI Modules:                                       │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────┐   │
│  │Dashboard │ │Settings  │ │Backup/   │ │Kill Switch│   │
│  │(DB stats)│ │(DB/API)  │ │Restore   │ │(clients)  │   │
│  └──────────┘ └──────────┘ └──────────┘ └───────────┘   │
│  ┌─────────────────────────────────────────────────────┐ │
│  │  Migration Runner (intelligent, incremental)        │ │
│  └─────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
          ↑ HTTP (LAN)        ↑ HTTP (LAN)
   MAUI App — Press Kiosk    MAUI App — Floor Tablet
```

---

## Technology Stack

| Layer | Technology | Reason |
|---|---|---|
| UI framework | WinUI 3 (.NET 10, Windows App SDK) | Consistent with deployment tooling, modern Windows UI |
| MVVM | CommunityToolkit.Mvvm | Same as all other MTM apps |
| REST API hosting | ASP.NET Core / Kestrel (in-process) | No separate service installation; one exe |
| MySQL access | `MySqlConnector` (NuGet) | MySQL 5.7 compatible, async-first |
| DI container | `Microsoft.Extensions.DependencyInjection` | Shared between WinUI and ASP.NET |
| Settings storage | JSON file in `%ProgramData%\MTM\WaitlistServer\` | Survives app updates; not in install folder |
| Backup tool | `mysqldump` (called via `Process`) | Available wherever MySQL is installed |
| Migration tracking | `SchemaVersions` table in MySQL | See DATABASE-06 |

---

## Solution Structure

New solution file: `MTM_Waitlist_Server.slnx`

```
MTM_Waitlist_Server/
├── MTM_Waitlist_Server.slnx
├── Hosts/
│   └── MTM_Waitlist_Server.Admin/           ← WinUI app (entry point)
│       ├── MTM_Waitlist_Server.Admin.csproj
│       ├── MainWindow.xaml                   ← Navigation shell
│       ├── App.xaml
│       ├── MauiProgram.cs                    ← N/A — use App.xaml.cs for DI setup
│       └── appsettings.json                  ← Server config (read by both UI + API)
├── Core/
│   ├── MTM_Waitlist_Server.Core/             ← Interfaces, models, constants
│   └── MTM_Waitlist_Server.Api/              ← ASP.NET controllers, services
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── WaitlistController.cs
│       │   └── AdminController.cs            ← Kill-switch, migration status endpoints
│       ├── Services/                          ← Business logic called by controllers
│       └── Program.cs                         ← WebApplication builder (called by WinUI host)
└── Modules/
    ├── MTM_Waitlist_Server.Module.Dashboard/  ← DB status dashboard
    ├── MTM_Waitlist_Server.Module.Settings/   ← Server + DB settings
    ├── MTM_Waitlist_Server.Module.Backup/     ← Backup & restore
    ├── MTM_Waitlist_Server.Module.KillSwitch/ ← Client session management
    └── MTM_Waitlist_Server.Module.Migrations/ ← Incremental migration runner
```

---

## In-Process API Hosting Pattern

The WinUI `App.xaml.cs` starts both the WinUI application window and the ASP.NET Kestrel server on the same thread pool:

```csharp
// App.xaml.cs (pseudocode — exact pattern in DATABASE-02)
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    // 1. Build the DI container shared between WinUI modules and the ASP.NET API
    var host = CreateHostBuilder().Build();

    // 2. Start Kestrel (non-blocking — runs on background threads)
    _apiHostTask = host.RunAsync();

    // 3. Open the admin window
    _window = new MainWindow(host.Services.GetRequiredService<ViewModel_Shell>());
    _window.Activate();
}

private static IHostBuilder CreateHostBuilder() =>
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(web =>
        {
            web.UseStartup<ApiStartup>();
            web.UseUrls("http://0.0.0.0:5000");
        })
        .ConfigureServices(RegisterAdminServices);
```

This means:
- The same `IServiceCollection` is shared — the API controllers and the WinUI ViewModels use the same `IService_Database`, `IService_Migration`, etc.
- Shutting down the WinUI window shuts down the API gracefully (no orphaned processes).

---

## Navigation Shell

The admin app uses a `NavigationView` (WinUI) with four main sections:

| Nav item | Module | Route |
|---|---|---|
| Dashboard | Module.Dashboard | `/dashboard` |
| Migrations | Module.Migrations | `/migrations` |
| Backup & Restore | Module.Backup | `/backup` |
| Kill Switch | Module.KillSwitch | `/killswitch` |
| Settings | Module.Settings | `/settings` |

A persistent top status bar shows:
- API status: `● Running on :5000` / `● Stopped`
- Database status: `● Connected — mtm_waitlist @ localhost:3306` / `● Disconnected`
- Active client count: `3 clients connected`

---

## Dependency Rules

| Project | May Reference | Must Never Reference |
|---|---|---|
| Admin (WinUI host) | All Modules, Core, Api | Nothing extra |
| Module.* | Core | Other Modules, Api directly |
| Api | Core | Modules, Admin host |
| Core | Nothing | Everything |

---

## Deployment

The admin app is deployed as a **self-contained, single-folder publish** to the server:

```
C:\MTM\WaitlistServer\
├── MTM_Waitlist_Server.Admin.exe    ← entry point
├── appsettings.json                  ← connection string, port, backup path
├── [runtime files]
└── database\                         ← copies of migration scripts (updated on redeploy)
```

Settings that change after deployment (port, DB name, credentials) are stored in:
```
%ProgramData%\MTM\WaitlistServer\server-settings.json
```
This file survives app updates because it is outside the install folder (following the MTM Loader rule from the kickoff doc).

Startup: The admin app is registered as a **Task Scheduler task** (`Trigger: At system startup`, `Run whether user is logged on or not`) so the API starts automatically when the server boots. The admin UI window is displayed when an authorized IT user opens the exe manually — the process continues running headlessly in the background when no window is open.

The admin UI status bar and a dedicated section of the Dashboard expose **Start / Stop / Restart** controls for the embedded Kestrel API. These are the only way to stop or restart the API without killing the entire server process:

| Control | Behavior |
|---|---|
| **Start API** | Calls `host.StartAsync()` — enabled only when Kestrel is stopped |
| **Stop API** | Calls `host.StopAsync()` gracefully — triggers kill-switch countdown before stopping |
| **Restart API** | Stop + Start in sequence — useful after a port or JWT secret change |

These controls are visible in both the Dashboard status bar (compact buttons) and as a dedicated panel on the Dashboard page.

---

## Authorization Gate

On launch, `App.xaml.cs` reads the current Windows identity and authorises it against the MySQL `mtm_waitlist.Users` table:

```csharp
// App.xaml.cs — simplified
var windowsUser = WindowsIdentity.GetCurrent().Name.Split('\\').Last();
var authorised  = await _adminAuth.IsAuthorisedAsync(windowsUser);
```

`IsAuthorisedAsync` queries MySQL for a row where `Username = @windowsUser` and `Role IN ('Admin', 'Developer')`. If no matching row exists the app shows an access-denied screen and does not open the admin UI. No password prompt is shown — Windows identity is the authentication factor; MySQL role is the authorisation factor.

> **First-run edge case:** If MySQL is not yet installed or the database does not exist, `IService_FirstRun.ProbeAsync()` returns `true` and the app launches the First Run wizard instead of the normal dashboard (see DATABASE-07). The Windows group fallback is only used when MySQL is unreachable during an otherwise complete installation.

---

## Files to Create

| File | Location |
|---|---|
| `MTM_Waitlist_Server.slnx` | repo root |
| `MTM_Waitlist_Server.Admin.csproj` | `Hosts/MTM_Waitlist_Server.Admin/` |
| `App.xaml` + `App.xaml.cs` | `Hosts/MTM_Waitlist_Server.Admin/` |
| `MainWindow.xaml` + `MainWindow.xaml.cs` | `Hosts/MTM_Waitlist_Server.Admin/` |
| `appsettings.json` | `Hosts/MTM_Waitlist_Server.Admin/` |
| `MTM_Waitlist_Server.Api.csproj` | `Core/MTM_Waitlist_Server.Api/` |
| `Program.cs` (ASP.NET builder) | `Core/MTM_Waitlist_Server.Api/` |
| `Controllers/WaitlistController.cs` | `Core/MTM_Waitlist_Server.Api/` |
| `Controllers/AuthController.cs` | `Core/MTM_Waitlist_Server.Api/` |
| `Controllers/AdminController.cs` | `Core/MTM_Waitlist_Server.Api/` |
| Module project files | `Modules/MTM_Waitlist_Server.Module.*/` |
