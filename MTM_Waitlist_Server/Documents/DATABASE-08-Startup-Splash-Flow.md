# Server Admin Startup Splash Flow

This document captures the planned startup flow for the server admin app before implementation.
It is the source of truth for what was discovered, what was changed, and what needs to be built.

## Scope

- Server admin only for the first rollout.
- The diagrams reflect the planned splash-first startup flow.
- Nodes marked as changed behavior indicate places where the new splash flow intentionally differs from the current startup path.

## Startup UX Rules

- The splash screen opens first and stays visible while startup work runs.
- The splash shows every major startup step with friendly status text.
- Technical details stay in logs or an expandable diagnostics area, never inline in the main UI.
- The user can cancel startup at any time; cancel must run cleanup, trigger garbage collection, and exit.
- The main server window opens only after startup completes cleanly.
- When an existing degraded-mode rule allows the degraded main window, the splash routes into it instead of blocking.
- Existing degraded-mode decision rules remain authoritative — the splash is a presentation layer, not a new decision layer.

## Code Changes Already Applied (Pre-Implementation)

Two bugs found during edge case analysis were fixed in `App.xaml.cs` before splash work begins:

### Fix 1 — `neverConfigured=true` + `probe=Ready` now bypasses the wizard

**Before:** When `UpdaterPassword` was empty (`neverConfigured=true`) the wizard opened unconditionally regardless of probe status, including when the probe returned `Ready`.

**After:** If `neverConfigured=true` but `probeResult.Status == FirstRunStatus.Ready`, the wizard is skipped and startup falls through to the Windows auth gate. The DB is clearly accessible; credentials are simply missing from the settings file (anonymous MySQL connection or adopted existing DB).

**Affected file:** `Hosts/MTM_Waitlist_Server.Admin/App.xaml.cs` — the `if (neverConfigured)` block.

### Fix 2 — Self-heal `MarkCompleteAsync()` write wrapped in `try/catch`

**Before:** When the probe returned `Ready` but `FirstRunComplete=false`, the self-heal path called `MarkCompleteAsync()` via `Task.Run(...).GetAwaiter().GetResult()` with no exception handling. A write failure (disk full, read-only `%ProgramData%`, file locked) would propagate uncaught out of `OnLaunched()`, crashing the app before any window appeared.

**After:** The write is wrapped in `try/catch`. Failure is logged as a non-fatal warning and startup continues to normal launch. The self-heal will succeed on the next launch once the file becomes writable.

**Affected file:** `Hosts/MTM_Waitlist_Server.Admin/App.xaml.cs` — the self-heal block inside the Ready path.

## Mapped Edge Cases

These edge cases were discovered during diagram review and are now documented in the diagrams below.
The two fixes above addressed the actionable ones; the remaining two are documentation-only.

| # | Edge Case | Impact | Mitigation |
|---|-----------|--------|------------|
| 1 | `server-settings.json` corrupt or unreadable | `LoadFromDisk()` silently returns defaults; `UpdaterPassword` is empty; wizard re-runs on existing install; all prior settings lost | Restore backup or recreate file manually; Diagram 1 warning node added |
| 2 | MySQL drops between probe completing and `IsAuthorisedAsync()` running | `IsAuthorisedAsync` has a blanket `catch { return false; }` — user sees Access Denied instead of Database Unreachable; root cause invisible without log | No code change; log is the only diagnostic; Diagram 3 warning node added |
| 3 | `MarkCompleteAsync()` write failure during self-heal | **Fixed** — was a silent crash; now try/catch with non-fatal warning log |
| 4 | `neverConfigured=true` + `probe=Ready` | **Fixed** — was opening wizard unnecessarily; now bypasses to Windows auth |

## Planned Startup Flow

The full workflow is easier to review when broken into smaller chunks.

### 1. Startup Sequence

```mermaid
flowchart TD
    classDef start fill:#0d6efd,stroke:#0a58ca,color:#fff
    classDef action fill:#17a2b8,stroke:#117a8b,color:#fff
    classDef flag fill:#495057,stroke:#343a40,color:#fff
    classDef decision fill:#ffc107,stroke:#d39e00,color:#000
    classDef error fill:#dc3545,stroke:#b02a37,color:#fff
    classDef warning fill:#fd7e14,stroke:#ca6510,color:#fff

    s1(["App.OnLaunched() — WinUI entry point"])
    s1 --> s2["RegisterSharedServices()\nBuild ServiceCollection + BuildServiceProvider()\nRegisters: IService_SettingsStore, IService_AdminAuth,\nIService_FirstRun, BackupSchedulerService, all ViewModels/Views"]
    s2 --> s3["settingsStore.Get()\nLoad appsettings.json from AppData\nRead: Database.Host/Port/Name/UpdaterUsername/UpdaterPassword,\nFirstRunComplete, Api.ListenAddress"]
    s3 --> s3_note["⚠ EDGE CASE: If server-settings.json is corrupt or unreadable\nLoadFromDisk() silently falls back to defaults (no throw)\nUpdaterPassword='' → neverConfigured=true\nResult: first-run wizard re-runs on existing install\nAll prior settings lost: API address, backup path, JWT secret\nFix: restore backup or manually recreate server-settings.json"]:::warning
    s3_note -. note .-> s4
    s3 --> s4["Compute neverConfigured sentinel\nbool neverConfigured =\nstring.IsNullOrWhiteSpace(settings.Database.UpdaterPassword)"]
    s4 --> s5["firstRunService.ProbeAsync()\nStep 1: TCP connect as UpdaterUsername\nStep 2: SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME='Users'\nStep 3: SELECT COUNT(*) FROM Users WHERE IsActive=1 AND Role IN ('Admin','Developer')"]
    s5 --> s6{"ProbeAsync() threw\nan unhandled exception?"}
    s6 -->|"Yes — treated as\nUnreachable(ex.Message)"| s7["Model_FirstRunProbeResult.Unreachable()"]
    s6 -->|"No"| s8{"probeResult.Status"}

    class s1 start
    class s2,s3,s5 action
    class s3_note warning
    class s4 flag
    class s6,s8 decision
    class s7 error
```

### 2. Probe Outcome Routing

```mermaid
flowchart TD
    classDef decision fill:#ffc107,stroke:#d39e00,color:#000
    classDef failure fill:#dc3545,stroke:#b02a37,color:#fff
    classDef wizard fill:#0d6efd,stroke:#0a58ca,color:#fff
    classDef ready fill:#198754,stroke:#146c43,color:#fff
    classDef heal fill:#495057,stroke:#343a40,color:#fff
    classDef warning fill:#fd7e14,stroke:#ca6510,color:#fff

    d0{"neverConfigured\n(UpdaterPassword is empty)"}

    d0 -->|"true"| d0a{"probeResult.Status\n(neverConfigured path)"}

    d0a -->|"Ready — DB accessible\ndespite empty password\n(anonymous MySQL or adopted DB)"| auth_bypass["Skip wizard\u2192 proceed to\nIService_AdminAuth check\n(same as normal Ready path)"]

    d0a -->|"MySqlUnreachable /\nSchemaMissing /\nNoAdminUser"| wiz["new MainWindow(firstRunStatus: probeResult.Status,\nprobeResult: probeResult)\nWizard opens at step matching probe status:\nMySqlUnreachable \u2192 Step 1 (enter credentials)\nSchemaMissing \u2192 Step 2 (run schema)\nNoAdminUser \u2192 Step 3 (create admin user)"]

    d0 -->|"false"| d1{"probeResult.Status"}

    d1 -->|"MySqlUnreachable"| deg_unreach["new MainWindow(degraded: true,\ndegradedReason: 'MySQL could not be reached\nat {Host}:{Port}. Detail: {ex.Message}')"]

    d1 -->|"SchemaMissing"| d2{"settings.FirstRunComplete"}
    d2 -->|"false — mid-setup\ncredentials saved, schema not built"| wiz
    d2 -->|"true — regression\nschema was dropped post-setup"| deg_schema["new MainWindow(degraded: true,\ndegradedReason: 'Schema for {DatabaseName}\nnot found \u2014 may have been dropped')"]

    d1 -->|"NoAdminUser"| d3{"settings.FirstRunComplete"}
    d3 -->|"false — mid-setup\nschema built, admin not created"| wiz
    d3 -->|"true — post-setup\naccount deleted or disabled"| deg_admin["new MainWindow(degraded: true,\ndegradedReason: 'No active Admin/Developer\nuser found \u2014 restore backup or re-create')"]

    d1 -->|"Ready"| d4{"settings.FirstRunComplete = false\nbut probe = Ready?\n(adopted DB / inconsistent state)"}
    auth_bypass --> d4
    d4 -->|"true — self-heal"| heal["firstRunService.MarkCompleteAsync()\nWrites FirstRunComplete=true\n\u26a0 Wrapped in try/catch \u2014 write failure\nis non-fatal, logged and skipped"]
    d4 -->|"false — normal"| auth["Proceed to IService_AdminAuth check"]
    heal --> auth

    class d0,d0a,d1,d2,d3,d4 decision
    class deg_unreach,deg_schema,deg_admin failure
    class wiz wizard
    class auth,auth_bypass ready
    class heal heal
```

### 3. Ready Path

```mermaid
flowchart TD
    classDef action fill:#17a2b8,stroke:#117a8b,color:#fff
    classDef decision fill:#ffc107,stroke:#d39e00,color:#000
    classDef failure fill:#dc3545,stroke:#b02a37,color:#fff
    classDef warning fill:#fd7e14,stroke:#ca6510,color:#fff
    classDef success fill:#198754,stroke:#146c43,color:#fff

    r1["Service_AdminAuth.GetCurrentWindowsUsername()\nWindowsIdentity.GetCurrent().Name\n(returns DOMAIN\\username or plain username)"]
    r1 --> r2["adminAuth.IsAuthorisedAsync(windowsUser)\nSELECT COUNT(*) FROM Users WHERE WindowsUsername=@u\nAND IsActive=1 AND Role IN ('Admin','Developer')"]
    r2 --> r3{"isAuthorised"}
    r3 -->|"false"| r4["new MainWindow(accessDenied: true)\nShows access-denied screen\nNo further startup work"]
    r4 --> r4_note["⚠ EDGE CASE: IsAuthorisedAsync has a blanket catch { return false; }\nIf MySQL drops between probe completing and this auth query,\nthe user sees 'Access Denied' instead of 'Database unreachable'\nRoot cause is invisible — log is the only diagnostic"]:::warning
    r3 -->|"true"| r5["apiHost.Start()\nService_ApiHost — starts in-process\nKestrel on settings.Api.ListenAddress\neg. http://0.0.0.0:5000"]
    r5 --> r6{"apiHost.Start()\nthrew an exception?"}
    r6 -->|"Yes — logged, app continues\nwithout API"| r7["StartupLogger.Error(...)\nNo degraded window — app still opens\nbut API endpoints are unavailable"]
    r6 -->|"No"| r8["scheduler.StartAsync(CancellationToken.None)\nBackupSchedulerService — runs on\nbackground thread, fire-and-forget"]
    r7 --> r8
    r8 --> r9(["new MainWindow()\nNormal mode — all navigation enabled"])

    class r1,r2,r5,r8 action
    class r3,r6 decision
    class r4 failure
    class r4_note,r7 warning
    class r9 success
```

### 4. User Choice Routes For Issue States

```mermaid
flowchart TD
    classDef state fill:#fd7e14,stroke:#ca6510,color:#fff
    classDef choice fill:#6f42c1,stroke:#59359a,color:#fff
    classDef action fill:#17a2b8,stroke:#117a8b,color:#fff
    classDef decision fill:#ffc107,stroke:#d39e00,color:#000
    classDef exit fill:#6c757d,stroke:#565e64,color:#fff

    u1["Splash screen showing issue state\n(MySqlUnreachable / SchemaMissing /\nNoAdminUser / AccessDenied / ApiHostFailed)"]
    u1 --> u2["Retry\nRe-run ProbeAsync() +\nfull startup sequence"]
    u1 --> u3["Open Settings\nNavigate to View_Settings\nUser corrects Host/Port/Credentials"]
    u1 --> u4["Open First-Run Wizard\nnew MainWindow(firstRunStatus:...,\nprobeResult:...)"]
    u1 --> u5["Open Degraded Mode\nnew MainWindow(degraded: true,\ndegradedReason: ...)\nOnly available when degraded-mode rules allow"]
    u1 --> u6["View Diagnostics / Log\nOpen StartupLogger.LogFilePath\nor show expandable log panel"]
    u1 --> u7(["Cancel and Exit\nCleanup + GC.Collect()\n+ Application.Current.Exit()"])

    u2 --> u8["Restart startup coordinator\nRe-enter OnLaunched() logic"]
    u3 --> u9["Return to splash\nRestart startup coordinator"]
    u6 --> u10["Stay on splash\nLog visible in diagnostics panel"]
    u4 --> u11{"Wizard completed\nsuccessfully?\n(WizardCompleted event fired)"}
    u11 -->|"Yes — MarkCompleteAsync()\ncalled by ViewModel_FirstRun"| u8
    u11 -->|"No — user cancelled\nor wizard errored"| u7
    u9 --> u8
    u10 --> u8

    class u1 state
    class u2,u3,u4,u5,u6 choice
    class u7 exit
    class u8,u9,u10 action
    class u11 decision
```

### 5. Failure Mapping Reference

**MySQL unreachable — first setup** *(neverConfigured=true, probe threw MySqlException network error)*
```mermaid
flowchart LR
    classDef failure fill:#dc3545,stroke:#b02a37,color:#fff
    classDef choice fill:#6f42c1,stroke:#59359a,color:#fff
    classDef exit fill:#6c757d,stroke:#565e64,color:#fff
    a0["MySqlUnreachable\nneverConfigured=true\nProbe: TCP connect failed\n(wrong host / port closed / firewall)"] --> a1["Retry\nRe-run ProbeAsync()"]
    a0 --> a2["Open Settings\nView_Settings \u2014 correct Host/Port/Credentials"]
    a0 --> a3["First-Run Wizard\nnew MainWindow(firstRunStatus: MySqlUnreachable)\nOpens at Step 1: enter credentials"]
    a0 --> a4["View Diagnostics\nOpen StartupLogger.LogFilePath"]
    a0 --> a5(["Cancel + Exit\nGC.Collect() + Application.Current.Exit()"])
    class a0 failure
    class a1,a2,a3,a4 choice
    class a5 exit
```

**MySQL unreachable — known host** *(neverConfigured=false, credentials exist but DB is down)*
```mermaid
flowchart LR
    classDef failure fill:#dc3545,stroke:#b02a37,color:#fff
    classDef choice fill:#6f42c1,stroke:#59359a,color:#fff
    classDef exit fill:#6c757d,stroke:#565e64,color:#fff
    b0["MySqlUnreachable\nneverConfigured=false\nnew MainWindow(degraded: true,\ndegradedReason: 'MySQL could not be reached\nat {Host}:{Port}')"] --> b1["Retry\nRe-run ProbeAsync()"]
    b0 --> b2["Open Settings\nView_Settings \u2014 correct Host/Port"]
    b0 --> b3["Open Degraded Mode\nnew MainWindow(degraded: true)\nSettings and Backup available"]
    b0 --> b4["View Diagnostics\nOpen StartupLogger.LogFilePath"]
    b0 --> b5(["Cancel + Exit\nGC.Collect() + Application.Current.Exit()"])
    class b0 failure
    class b1,b2,b3,b4 choice
    class b5 exit
```

**Schema missing — during setup** *(SchemaMissing + FirstRunComplete=false)*
```mermaid
flowchart LR
    classDef failure fill:#dc3545,stroke:#b02a37,color:#fff
    classDef choice fill:#6f42c1,stroke:#59359a,color:#fff
    classDef exit fill:#6c757d,stroke:#565e64,color:#fff
    c0["SchemaMissing + FirstRunComplete=false\nProbe step 2: SELECT COUNT(*) FROM\ninformation_schema.TABLES WHERE TABLE_NAME='Users'\nreturned 0\nOR auth/credential error on connect"] --> c1["Retry\nRe-run ProbeAsync()"]
    c0 --> c2["First-Run Wizard\nnew MainWindow(firstRunStatus: SchemaMissing)\nOpens at Step 2: run schema setup"]
    c0 --> c3["View Diagnostics\nOpen StartupLogger.LogFilePath"]
    c0 --> c4(["Cancel + Exit\nGC.Collect() + Application.Current.Exit()"])
    class c0 failure
    class c1,c2,c3 choice
    class c4 exit
```

**Schema missing — post setup** *(SchemaMissing + FirstRunComplete=true — regression)*
```mermaid
flowchart LR
    classDef failure fill:#dc3545,stroke:#b02a37,color:#fff
    classDef choice fill:#6f42c1,stroke:#59359a,color:#fff
    classDef exit fill:#6c757d,stroke:#565e64,color:#fff
    d0["SchemaMissing + FirstRunComplete=true\nnew MainWindow(degraded: true,\ndegradedReason: 'Schema for {DatabaseName}\nnot found \u2014 may have been dropped\nor database name changed in Settings')"] --> d1["Retry\nRe-run ProbeAsync()"]
    d0 --> d2["Open Degraded Mode\nnew MainWindow(degraded: true)\nSettings and Backup available"]
    d0 --> d3["View Diagnostics\nOpen StartupLogger.LogFilePath"]
    d0 --> d4(["Cancel + Exit\nGC.Collect() + Application.Current.Exit()"])
    class d0 failure
    class d1,d2,d3 choice
    class d4 exit
```

**Admin user missing — during setup** *(NoAdminUser + FirstRunComplete=false)*
```mermaid
flowchart LR
    classDef failure fill:#dc3545,stroke:#b02a37,color:#fff
    classDef choice fill:#6f42c1,stroke:#59359a,color:#fff
    classDef exit fill:#6c757d,stroke:#565e64,color:#fff
    e0["NoAdminUser + FirstRunComplete=false\nProbe step 3: SELECT COUNT(*) FROM Users\nWHERE IsActive=1 AND Role IN ('Admin','Developer')\nreturned 0"] --> e1["Retry\nRe-run ProbeAsync()"]
    e0 --> e2["First-Run Wizard\nnew MainWindow(firstRunStatus: NoAdminUser)\nOpens at Step 3: create first admin user"]
    e0 --> e3["View Diagnostics\nOpen StartupLogger.LogFilePath"]
    e0 --> e4(["Cancel + Exit\nGC.Collect() + Application.Current.Exit()"])
    class e0 failure
    class e1,e2,e3 choice
    class e4 exit
```

**Admin user missing — post setup** *(NoAdminUser + FirstRunComplete=true — account deleted/disabled)*
```mermaid
flowchart LR
    classDef failure fill:#dc3545,stroke:#b02a37,color:#fff
    classDef choice fill:#6f42c1,stroke:#59359a,color:#fff
    classDef exit fill:#6c757d,stroke:#565e64,color:#fff
    f0["NoAdminUser + FirstRunComplete=true\nnew MainWindow(degraded: true,\ndegradedReason: 'No active Admin or Developer\nuser found \u2014 account may have been\ndisabled or deleted after setup')"] --> f1["Retry\nRe-run ProbeAsync()"]
    f0 --> f2["Open Degraded Mode\nnew MainWindow(degraded: true)\nSettings and Backup available"]
    f0 --> f3["View Diagnostics\nOpen StartupLogger.LogFilePath"]
    f0 --> f4(["Cancel + Exit\nGC.Collect() + Application.Current.Exit()"])
    class f0 failure
    class f1,f2,f3 choice
    class f4 exit
```

**API host failed** *(apiHost.Start() threw — app continues without API)*
```mermaid
flowchart LR
    classDef failure fill:#dc3545,stroke:#b02a37,color:#fff
    classDef choice fill:#6f42c1,stroke:#59359a,color:#fff
    classDef exit fill:#6c757d,stroke:#565e64,color:#fff
    g0["Service_ApiHost.Start() threw exception\nKestrel failed to bind to\nsettings.Api.ListenAddress\nStartupLogger.Error() called\nApp currently continues without API"] --> g1["Retry\nRe-run apiHost.Start()"]
    g0 --> g2["Open Degraded Mode\nnew MainWindow(degraded: true)\nAdmin UI available, REST API unavailable"]
    g0 --> g3["View Diagnostics\nOpen StartupLogger.LogFilePath"]
    g0 --> g4(["Cancel + Exit\nGC.Collect() + Application.Current.Exit()"])
    class g0 failure
    class g1,g2,g3 choice
    class g4 exit
```

**Background service warning** *(BackupSchedulerService.StartAsync() error — non-blocking)*
```mermaid
flowchart LR
    classDef warning fill:#fd7e14,stroke:#ca6510,color:#fff
    classDef choice fill:#6f42c1,stroke:#59359a,color:#fff
    classDef exit fill:#6c757d,stroke:#565e64,color:#fff
    h0["BackupSchedulerService.StartAsync() error\nFire-and-forget \u2014 failure does not block\nMainWindow.Activate()\nBackup scheduler will not run"] --> h1["Retry\nRe-run scheduler.StartAsync()"]
    h0 --> h2["Open Degraded Mode\nMainWindow still opens normally\nBackup tab shows scheduler-offline warning"]
    h0 --> h3["View Diagnostics\nOpen StartupLogger.LogFilePath"]
    h0 --> h4(["Cancel + Exit\nGC.Collect() + Application.Current.Exit()"])
    class h0 warning
    class h1,h2,h3 choice
    class h4 exit
```

## What Changes From The Current Startup

### Current behavior (before splash work)

- All startup decisions run synchronously inside `App.OnLaunched()` before the user sees any UI.
- `RegisterSharedServices()` + `BuildServiceProvider()` — no window visible.
- `settingsStore.Get()` file read — no window visible.
- `firstRunService.ProbeAsync()` MySQL round trip — no window visible. On a slow network this can take the full `ConnectionTimeout` seconds (default 15s) with no feedback.
- `IsAuthorisedAsync()` second MySQL round trip on the Ready path — no window visible.
- `apiHost.Start()` Kestrel bind — no window visible.
- First window that appears is already `MainWindow` in its final mode (normal / degraded / wizard / access-denied).
- Failure routing is implicit in startup branches; the user has no choices and sees no intermediate state.

### Planned behavior (after splash work)

- `SplashWindow` activates first, before any blocking work begins.
- `Service_StartupCoordinator` runs all startup steps asynchronously, reporting progress to `ViewModel_Splash` via `IProgress<StartupStep>`.
- Each step updates a friendly status message on the splash.
- The probe timeout (up to 15s) is visible as a progress step rather than a frozen launch.
- When any step fails, the splash transitions to an issue view with action buttons matching the failure type.
- On success, the splash fades out and `MainWindow` activates in the correct mode.
- Existing degraded-mode rules are preserved — the coordinator emits the same branch outcomes that `App.xaml.cs` currently emits; the splash just presents them visually.
- Cancel at any point runs `GC.Collect()` + `Application.Current.Exit()`.

## Startup Inputs Shown To The User

Startup-time user inputs that can appear in the planned flow:

- **Retry** — re-runs the full startup coordinator sequence from the beginning.
- **Open Settings** — navigates to `View_Settings` inside a lightweight settings-only window or inline panel so the user can correct host/port/credentials; returns to splash on close.
- **Open Wizard** — routes into `MainWindow` in first-run wizard mode when setup work is required.
- **Open Degraded Mode** — routes into `MainWindow` in degraded mode when existing degraded logic allows it.
- **View Diagnostics** — expands an inline log panel or opens `StartupLogger.LogFilePath`.
- **Cancel** — runs cleanup, triggers `GC.Collect()`, then calls `Application.Current.Exit()`.

## Implementation Notes

- The splash is server-admin specific in the first pass. WinUI client and Android can follow with app-specific step lists.
- `Service_StartupCoordinator` extracts the decision logic currently in `App.OnLaunched()`. `App.xaml.cs` becomes a thin launcher that activates `SplashWindow` and wires up coordinator events.
- The coordinator returns a `StartupOutcome` record that the splash uses to decide which `MainWindow` constructor to call.
- `ViewModel_Splash` must be `Transient` in DI (one instance per launch attempt).
- All coordinator steps run on a background thread; UI updates must be dispatched to the WinUI `DispatcherQueue`.
- `SplashWindow` owns a `DispatcherQueue` reference obtained via `DispatcherQueue.GetForCurrentThread()` called during `OnLaunched()` before any background work starts.
- `Service_StartupCoordinator` lives in the Admin host (`MTM_Waitlist_Server.Admin`) — it is not a shared Core service because it owns WinUI-specific orchestration.
- Window sizing for the splash: use `Service_WindowSizer` with a new `ApplySplashSize()` method (400×280px, centered). `SplashWindow` constructs its own `Service_WindowSizer` instance the same way `MainWindow` does.
- The splash XAML should use `MicaBackdrop` to match `MainWindow`.
