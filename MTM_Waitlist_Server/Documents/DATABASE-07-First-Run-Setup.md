# DATABASE-07: First-Run Setup

---

## Confirmed Decisions

| # | Question | Decision |
|---|---|---|
| 1 | How is a fresh install detected? | **Three-step probe on every launch:** (1) can the app reach MySQL at the configured host/port, (2) does the `mtm_waitlist` schema exist, (3) does the `Users` table contain at least one active Admin or Developer row. Any failure triggers first-run mode |
| 2 | What happens to the auth gate on first run? | **Falls back to `BUILTIN\Administrators` Windows group** when the DB probe fails. This is the only time Windows-group auth is used. Once a DB user exists, the MySQL role check takes over permanently |
| 3 | Is first-run a wizard or a normal screen? | **Inline wizard — three steps surfaced inside the existing admin shell.** No separate window. Steps: (1) Configure & Test Connection, (2) Run Bootstrap Migration, (3) Create First Admin User |
| 4 | Can the user skip any wizard step? | **No.** Each step gate-checks the previous one. The main nav is locked until the wizard completes |
| 5 | What happens if MySQL is unreachable during the probe? | **Settings screen opens automatically** with a banner: "MySQL could not be reached. Configure the connection before continuing." The rest of the nav is disabled |
| 6 | Who may complete first-run setup? | **`BUILTIN\Administrators`** on the server machine (or the configured `Admin:RequiredWindowsGroup` if already set in `server-settings.json`). Standard fallback — same as DATABASE-01 original design |
| 7 | Is first-run state persisted? | **Yes — a `FirstRunComplete` flag** is written to `server-settings.json` once the wizard finishes. On subsequent launches the probe still runs but the wizard is never shown if the flag is set and the DB is reachable |

---

## Overview

DATABASE-03 through DATABASE-06 all assume MySQL is running and `mtm_waitlist` is populated. The auth gate introduced in DATABASE-07's predecessor work now queries `mtm_waitlist.Users` — creating a chicken-and-egg problem on a brand-new server:

```
Fresh server
  → No mtm_waitlist database
  → Service_AdminAuth queries Users table
  → Connection fails / no rows returned
  → Access denied
  → Admin can never get in to run migrations
  → Database never gets created  ← STUCK
```

DATABASE-07 breaks this loop with a first-run detection probe and a guided setup wizard that runs before the normal auth gate is enforced.

---

## First-Run Detection Probe

Run on every application launch, before the auth gate:

```csharp
public async Task<FirstRunStatus> ProbeAsync()
{
    // 1. Can we reach MySQL at all?
    if (!await CanConnectAsync())
        return FirstRunStatus.MySqlUnreachable;

    // 2. Does the mtm_waitlist schema exist?
    if (!await SchemaExistsAsync())
        return FirstRunStatus.SchemaMissing;

    // 3. Does at least one active Admin or Developer user exist?
    if (!await AdminUserExistsAsync())
        return FirstRunStatus.NoAdminUser;

    return FirstRunStatus.Ready;
}
```

```csharp
public enum FirstRunStatus
{
    Ready,              // Normal launch — proceed to auth gate
    MySqlUnreachable,   // Cannot connect — open Settings with banner
    SchemaMissing,      // Connected but schema missing — run bootstrap
    NoAdminUser         // Schema exists but no Admin/Developer — create first user
}
```

The probe uses the **updater credentials** from `server-settings.json`. On a brand-new machine the settings file may not exist yet — defaults are used (`localhost:3306`, empty password), which will fail to connect and correctly return `MySqlUnreachable`.

---

## Auth Gate Fallback

```csharp
// App.xaml.cs — OnLaunched
var probeStatus = await firstRunService.ProbeAsync();

if (probeStatus != FirstRunStatus.Ready)
{
    // Fall back to Windows group check — MySQL is not usable yet
    var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
    var requiredGroup = settingsStore.Get().Admin.RequiredWindowsGroup;
    if (!principal.IsInRole(requiredGroup))
    {
        _window = new MainWindow(accessDenied: true);
        _window.Activate();
        return;
    }

    // Open admin shell in first-run mode — nav locked to wizard
    _window = new MainWindow(firstRunStatus: probeStatus);
    _window.Activate();
    return;
}

// Normal launch — MySQL role check
var isAuthorised = await adminAuth.IsAuthorisedAsync(windowsUsername);
if (!isAuthorised) { /* access denied */ }
```

---

## First-Run Wizard UI

The wizard replaces the normal nav content. The NavigationView menu items are **disabled** (greyed out) until `FirstRunStatus.Ready`.

```
┌─────────────────────────────────────────────────────────────────┐
│  MTM Waitlist Server Admin                                      │
├───────────────────────────────────────────────────────────────── │
│  ⚠ First-Time Setup Required                                    │
│  Complete the steps below before using the admin application.   │
│                                                                 │
│  ┌───┐  ┌───┐  ┌───┐                                           │
│  │ 1 │→ │ 2 │→ │ 3 │                                           │
│  └───┘  └───┘  └───┘                                           │
│  Connect  Bootstrap  Create User                                │
│                                                                 │
│  ════════════════════════════════════════════                   │
│  STEP 1 — Configure Database Connection                         │
│                                                                 │
│  Host     [localhost          ]                                 │
│  Port     [3306               ]                                 │
│  Database [mtm_waitlist        ]                                │
│  Username [waitlist_admin_dbupdater]                            │
│  Password [                   ]  [Test Connection]             │
│                                                                 │
│  ✅ Connected — MySQL 5.7.41                                    │
│                                          [Next →]              │
└─────────────────────────────────────────────────────────────────┘
```

### Step 1 — Configure & Test Connection

- Pre-fills from `server-settings.json` defaults.
- **Test Connection** button opens a `MySqlConnection` with the entered credentials and runs `SELECT VERSION()`.
- **Next** is disabled until a successful test result is shown.
- On success, settings are saved to `server-settings.json`.

### Step 2 — Run Bootstrap Migration

- Calls `IService_Migration.ApplyPendingMigrationsAsync()` (DATABASE-06).
- Progress bar and scrollable log output shown in real time.
- If `SchemaVersions` does not exist, V001 runs first (bootstrap path from DATABASE-06).
- **Next** is disabled until all migrations complete with ✅.

```
┌─────────────────────────────────────────────────────────────────┐
│  STEP 2 — Run Bootstrap Migration                               │
│                                                                 │
│  This creates the mtm_waitlist database schema.                 │
│  This step is required before any other operation.             │
│                                                                 │
│  [▶ Run Bootstrap]                                             │
│                                                                 │
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░  V001 — Applied ✅             │
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  V002 — Applied ✅             │
│  Procedures re-applied ✅                                       │
│  Triggers re-applied ✅                                         │
│                                                                 │
│  ✅ Bootstrap complete — schema is ready.                       │
│                                          [Next →]              │
└─────────────────────────────────────────────────────────────────┘
```

### Step 3 — Create First Admin User

- Form: Windows Username, App Username, Display Name, Password (+ confirm), Role (locked to `Admin` or `Developer`).
- **Windows Username** is pre-filled with the current `WindowsIdentity.GetCurrent().Name`.
- On submit, calls `INSERT INTO Users` directly (no stored procedure required — this is a one-time bootstrap path).
- Validates: password min 8 characters, Windows username not empty, app username unique.
- On success: writes `FirstRunComplete = true` to `server-settings.json` and re-runs the full probe.
- Admin shell nav is unlocked and the app proceeds to the normal MySQL role auth gate.

```
┌─────────────────────────────────────────────────────────────────┐
│  STEP 3 — Create First Admin User                               │
│                                                                 │
│  Windows Username  [MTMFG\john.k          ] (pre-filled)       │
│  App Username      [john.k                ]                     │
│  Display Name      [John K                ]                     │
│  Role              [Admin ▼]  (Admin or Developer only)        │
│  Password          [                      ]                     │
│  Confirm Password  [                      ]                     │
│                                                                 │
│                                    [✓ Create User & Finish]    │
└─────────────────────────────────────────────────────────────────┘
```

---

## `server-settings.json` Flag

```json
{
  "FirstRunComplete": true,
  "Database": { ... },
  ...
}
```

`FirstRunComplete` is a top-level flag in `ServerSettings`. It is set to `true` only after Step 3 completes successfully. It does **not** skip the probe on subsequent launches — the probe still runs — but if the probe returns `Ready`, the wizard is never shown regardless of this flag. The flag exists so that a database connection failure after a successful first run does not re-trigger the wizard.

```csharp
// Wizard is shown only when BOTH conditions are true:
// 1. The probe did not return Ready
// 2. FirstRunComplete is false
bool showWizard = probeStatus != FirstRunStatus.Ready && !settings.FirstRunComplete;
```

If `FirstRunComplete` is `true` but the probe fails (e.g. MySQL went down), the app opens in **degraded mode** with a banner rather than re-running the wizard.

---

## Degraded Mode (Post-First-Run DB Failure)

After `FirstRunComplete = true`, if MySQL becomes unreachable on a subsequent launch:

```
┌─────────────────────────────────────────────────────────────────┐
│  ⚠ Database Unavailable                                         │
│  MySQL could not be reached at localhost:3306.                  │
│  The dashboard and migrations are unavailable.                  │
│  Settings are still accessible.                                 │
│                         [Open Settings]  [Retry Connection]    │
└─────────────────────────────────────────────────────────────────┘
```

The Settings module remains accessible so the admin can correct the connection string. All other nav items are disabled.

---

## New Core Models

```
Core/Models/FirstRun/
  FirstRunStatus.cs         ← enum: Ready, MySqlUnreachable, SchemaMissing, NoAdminUser
  Model_FirstRunProbeResult.cs  ← Status + optional error message
```

---

## New Interface & Service

```
Core/Interfaces/FirstRun/
  IService_FirstRun.cs      ← ProbeAsync(), IsFirstRunRequired(), MarkCompleteAsync()

Hosts/MTM_Waitlist_Server.Admin/Services/
  Service_FirstRun.cs       ← Implementation using MySqlConnector + IService_SettingsStore
```

---

## ViewModel & View

```
Module.Dashboard/ is not used — wizard lives in the Admin host shell directly.

Hosts/MTM_Waitlist_Server.Admin/
  ViewModels/
    ViewModel_FirstRun.cs   ← Step tracking, form fields, commands
  Views/
    View_FirstRun.xaml
    View_FirstRun.xaml.cs
```

Key observable properties:
```csharp
[ObservableProperty] int _currentStep           // 1, 2, or 3
[ObservableProperty] FirstRunStatus _probeStatus
[ObservableProperty] bool _isWorking
[ObservableProperty] string _statusMessage
[ObservableProperty] bool _step1Complete
[ObservableProperty] bool _step2Complete
[ObservableProperty] string _dbHost
[ObservableProperty] string _dbPort
[ObservableProperty] string _dbName
[ObservableProperty] string _dbUsername
[ObservableProperty] string _dbPassword
[ObservableProperty] string _windowsUsername
[ObservableProperty] string _appUsername
[ObservableProperty] string _displayName
[ObservableProperty] string _userPassword
[ObservableProperty] string _confirmPassword
[ObservableProperty] string _selectedRole      // "Admin" or "Developer"
```

Key commands:
```csharp
[RelayCommand] TestConnectionAsync()
[RelayCommand] RunBootstrapAsync()
[RelayCommand] CreateFirstUserAsync()
```

---

## `MainWindow` Integration

`MainWindow.xaml.cs` receives the `FirstRunStatus` on construction. When not `Ready`, the NavigationView menu items are disabled and `View_FirstRun` is loaded as the frame content. The wizard's `CreateFirstUserAsync` command, on success, fires an event that `MainWindow` handles to re-enable the nav and navigate to the Dashboard.

```csharp
// MainWindow.xaml.cs
if (firstRunStatus != FirstRunStatus.Ready)
{
    NavView.IsEnabled = false;
    var vm = App.Services!.GetRequiredService<ViewModel_FirstRun>();
    vm.WizardCompleted += OnFirstRunWizardCompleted;
    ContentFrame.Navigate(typeof(View_FirstRun), vm);
}
```

---

## Seed Data

The seed files (`Database/seed/01_Seed_Users.sql`, `Database/seed/02_Seed_WaitlistEntries.sql`) are **dev-only** and are never run by the wizard. Step 3 replaces them for production first-run user creation. Developers running locally still apply seed files manually.

---

## Open Decisions

- **Multiple admin users on first run:** Step 3 creates exactly one user. Additional users are managed post-setup via a future User Management module (not in current scope).
- **Password complexity rules:** Minimum 8 characters for v1. A future settings option for complexity policy is deferred.
- **Wizard re-entry:** If the wizard fails partway through (e.g. crash after Step 2 but before Step 3), `FirstRunComplete` is still `false` and the wizard restarts from the probe result. Steps already completed are skipped automatically based on probe state (schema exists → skip Step 2, jump to Step 3).
