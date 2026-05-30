# SPLASH-01: Research — Splash Screen Implementation

This file is the source of truth for all code facts gathered before implementing the splash screen.
No further codebase searches are required to begin implementation.

---

## 1. Project Location

| Fact | Value |
|------|-------|
| Host project | `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/` |
| Namespace root | `MTM_Waitlist_Server.Admin` |
| TFM | `net10.0-windows10.0.19041.0` |
| UI framework | WinUI 3 (Microsoft.WindowsAppSDK 2.0.1) |
| MVVM toolkit | CommunityToolkit.Mvvm 8.4.2 |
| Entry point | `App.OnLaunched(LaunchActivatedEventArgs args)` |

---

## 2. Existing Files That Will Be Modified

| File | Current responsibility | What changes |
|------|----------------------|--------------|
| `App.xaml.cs` | Owns all startup logic, activates `MainWindow` | Becomes thin launcher: activates `SplashWindow`, wires `Service_StartupCoordinator` events |
| `MainWindow.xaml.cs` | Navigation shell, four launch modes | No logic changes; constructor signature unchanged |
| `Services/Service_WindowSizer.cs` | `ApplyFirstRunSize()` / `ApplyNormalSize()` / `CenterOnMonitor()` | Add `ApplySplashSize()` method |
| `Core/.../IService_WindowSizer.cs` | Interface for window sizing | Add `ApplySplashSize()` to interface |
| `RegisterSharedServices()` in `App.xaml.cs` | Registers all DI services | Add `Service_StartupCoordinator` + `ViewModel_Splash` + `SplashWindow` registrations |

---

## 3. New Files To Create

| File | Type | Location |
|------|------|----------|
| `SplashWindow.xaml` | WinUI Window XAML | `Hosts/MTM_Waitlist_Server.Admin/` |
| `SplashWindow.xaml.cs` | Code-behind | `Hosts/MTM_Waitlist_Server.Admin/` |
| `ViewModels/ViewModel_Splash.cs` | CommunityToolkit ViewModel | `Hosts/MTM_Waitlist_Server.Admin/ViewModels/` |
| `Services/Service_StartupCoordinator.cs` | Coordinator service | `Hosts/MTM_Waitlist_Server.Admin/Services/` |
| `Core/.../Models/Splash/StartupStep.cs` | Progress record | `Core/MTM_Waitlist_Server.Core/Models/Splash/` |
| `Core/.../Models/Splash/StartupOutcome.cs` | Result record | `Core/MTM_Waitlist_Server.Core/Models/Splash/` |
| `Core/.../Interfaces/Splash/IService_StartupCoordinator.cs` | Interface | `Core/MTM_Waitlist_Server.Core/Interfaces/Splash/` |

---

## 4. Startup Step Sequence (Verified from `App.xaml.cs`)

The coordinator must execute these steps in order, reporting each to the splash:

| Step # | ID | Friendly label | Work performed |
|--------|----|----------------|----------------|
| 1 | `BuildingContainer` | Initialising services… | `RegisterSharedServices()` + `BuildServiceProvider()` |
| 2 | `LoadingSettings` | Loading configuration… | `settingsStore.Get()` |
| 3 | `ComputingSentinel` | Checking setup state… | `neverConfigured` sentinel computation |
| 4 | `ProbingMySQL` | Connecting to database… | `firstRunService.ProbeAsync()` (up to 15s) |
| 5 | `EvaluatingBranch` | Evaluating startup path… | Decision tree evaluation |
| 6 | `CheckingWindowsAuth` | Verifying Windows identity… | `adminAuth.IsAuthorisedAsync(windowsUser)` (Ready path only) |
| 7 | `StartingApiHost` | Starting API host… | `apiHost.Start()` (Normal path only) |
| 8 | `StartingScheduler` | Starting backup scheduler… | `scheduler.StartAsync(CancellationToken.None)` (Normal path only) |
| 9 | `Complete` | Ready | All checks passed |

---

## 5. `StartupOutcome` Values

The coordinator returns exactly one of these outcomes. The splash uses the outcome to decide which `MainWindow` constructor to call (or to show an issue view):

```
Normal                    → new MainWindow()
Degraded(reason)          → new MainWindow(degraded: true, degradedReason: reason)
FirstRunWizard(status, probe) → new MainWindow(firstRunStatus: status, probeResult: probe)
AccessDenied              → new MainWindow(accessDenied: true)
ApiHostFailed(reason)     → new MainWindow()  [app continues without API — not degraded]
```

---

## 6. Decision Tree Rules (Exact Logic from `App.xaml.cs`)

These are the exact conditions, in evaluation order:

```
neverConfigured=true AND probe=Ready
    → Outcome.Normal (bypass wizard — DB accessible despite empty password)

neverConfigured=true AND probe≠Ready
    → Outcome.FirstRunWizard(probeResult.Status, probeResult)

probe=MySqlUnreachable
    → Outcome.Degraded("MySQL could not be reached at {Host}:{Port}.\n\nDetail: {ex.Message}")

probe=SchemaMissing AND FirstRunComplete=false
    → Outcome.FirstRunWizard(SchemaMissing, probeResult)

probe=SchemaMissing AND FirstRunComplete=true
    → Outcome.Degraded("Schema for '{DatabaseName}' not found — may have been dropped...")

probe=NoAdminUser AND FirstRunComplete=true
    → Outcome.Degraded("No active Admin or Developer user found...")

probe=NoAdminUser AND FirstRunComplete=false
    → Outcome.FirstRunWizard(NoAdminUser, probeResult)

probe=Ready AND FirstRunComplete=false
    → self-heal MarkCompleteAsync() [non-fatal try/catch] → continue to Windows auth

probe=Ready → IsAuthorisedAsync(windowsUser)
    isAuthorised=false → Outcome.AccessDenied
    isAuthorised=true  → apiHost.Start() [non-fatal try/catch] → scheduler.StartAsync() → Outcome.Normal
```

---

## 7. Key Service Signatures

### `IService_FirstRun` (`Core/Interfaces/FirstRun/IService_FirstRun.cs`)

```csharp
Task<Model_FirstRunProbeResult> ProbeAsync(CancellationToken cancellationToken = default);
Task MarkCompleteAsync(CancellationToken cancellationToken = default);
```

### `Service_AdminAuth` (`Admin/Services/Service_AdminAuth.cs`)

```csharp
public static string GetCurrentWindowsUsername()   // returns WindowsIdentity.GetCurrent().Name
Task<bool> IsAuthorisedAsync(string windowsUsername)  // blanket catch → returns false on DB error
```

### `Service_ApiHost.Start()` (`Admin/Services/Service_ApiHost.cs`)

```csharp
public void Start()   // binds Kestrel to settings.Api.ListenAddress; throws on bind failure
```

### `BackupSchedulerService.StartAsync()` (`Admin/Services/`)

```csharp
Task StartAsync(CancellationToken cancellationToken)  // fire-and-forget safe
```

### `Service_SettingsStore` (`Admin/Services/Service_SettingsStore.cs`)

```csharp
ServerSettings Get()                         // loads from %ProgramData%\MTM\WaitlistServer\server-settings.json
Task SaveAsync(ServerSettings settings)      // no catch — can throw on write failure
Task ReloadAsync()
```

Key field: `settings.Database.UpdaterPassword` — the `neverConfigured` sentinel.

---

## 8. `Model_FirstRunProbeResult` Structure

```csharp
// Status values from FirstRunStatus enum
FirstRunStatus.MySqlUnreachable
FirstRunStatus.SchemaMissing
FirstRunStatus.NoAdminUser
FirstRunStatus.Ready

// Factory methods
Model_FirstRunProbeResult.Unreachable(string message)
Model_FirstRunProbeResult.SchemaMissing(string? message = null)
Model_FirstRunProbeResult.NoAdminUser()
Model_FirstRunProbeResult.Ready()

// Properties
FirstRunStatus Status { get; }
string? ErrorMessage { get; }
```

---

## 9. `MainWindow` Constructor Signature

```csharp
public MainWindow(
    bool accessDenied = false,
    bool degraded = false,
    string? degradedReason = null,
    FirstRunStatus? firstRunStatus = null,
    Model_FirstRunProbeResult? probeResult = null)
```

The constructor immediately calls `InitializeComponent()` then routes to one of:
- `ShowAccessDenied()` — `accessDenied=true`
- `ShowFirstRunWizard(firstRunStatus.Value)` — `firstRunStatus.HasValue`
- `ShowDegradedMode()` — `degraded=true`
- Normal nav — else (navigates to Dashboard via `NavView.SelectedItem = NavView.MenuItems[0]`)

Window sizing in constructor:
- `_windowSizer = new Service_WindowSizer(this)` — constructed directly, not injected
- `accessDenied` → `CenterOnMonitor()` only
- `firstRunStatus.HasValue` → `ApplyFirstRunSize()` inside `ShowFirstRunWizard()`
- `degraded` → `ApplyNormalSize()`
- normal → `ApplyNormalSize()`

---

## 10. `Service_WindowSizer` API

```csharp
// Sizes (constants)
FirstRunWidth=700,  FirstRunHeight=900
NormalWidth=1200,   NormalHeight=750

// Methods
void ApplyFirstRunSize()   // resize to 700×900 + CenterOnMonitor()
void ApplyNormalSize()     // resize to 1200×750 + CenterOnMonitor()
void CenterOnMonitor()     // centers on DisplayArea.WorkArea

// Constructor — obtains AppWindow from Window HWND
public Service_WindowSizer(Window window)
{
    var hWnd     = WinRT.Interop.WindowNative.GetWindowHandle(window);
    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
    _appWindow   = AppWindow.GetFromWindowId(windowId);
}
```

New size needed for splash: **400×280** (compact centered progress card).
New method to add: `void ApplySplashSize()` → `_appWindow.Resize(new SizeInt32(400, 280)) + CenterOnMonitor()`.

---

## 11. `StartupLogger` API (Admin host)

```csharp
// Static methods — no instance needed
StartupLogger.Info(string message)
StartupLogger.Warn(string message)
StartupLogger.Error(string message, Exception ex)
StartupLogger.Section(string heading)       // writes a visual section divider to the log
string StartupLogger.LogFilePath { get; }   // path to the active log file
```

---

## 12. DI Registration Pattern (`RegisterSharedServices()` in `App.xaml.cs`)

Existing registrations are all `Singleton` (services/repos) or `Transient` (views/viewmodels).

```csharp
// Example pattern for new registrations to add:
services.AddSingleton<IService_StartupCoordinator, Service_StartupCoordinator>();
services.AddTransient<ViewModel_Splash>();
services.AddTransient<SplashWindow>();
```

`App.Services` is the static `IServiceProvider?` property populated after `BuildServiceProvider()`.

---

## 13. WinUI Threading Model

- `App.OnLaunched()` runs on the **UI thread**.
- `DispatcherQueue.GetForCurrentThread()` must be called on the UI thread — capture it before any `Task.Run` or `await`.
- All `ObservableProperty` writes that trigger XAML bindings must execute on the UI thread via `_dispatcherQueue.TryEnqueue(...)`.
- `Window.Activate()` must be called on the UI thread.
- `Application.Current.Exit()` must be called on the UI thread.
- `GC.Collect()` can be called from any thread.

---

## 14. `AppInstance.Restart()` Usage

```csharp
// Called from MainWindow.OnFirstRunCompleted() after wizard completes
Microsoft.Windows.AppLifecycle.AppInstance.Restart(string.Empty);
```

This is unchanged. The splash does not restart on wizard completion — `MainWindow` still owns that.

---

## 15. `MainWindow.xaml` Structure

Key named XAML elements used by code-behind:
- `NavView` — `NavigationView` in Row 0
- `ContentFrame` — `Frame` inside `NavigationView`
- `DbStatusDot` — `Ellipse` in status bar (fill color indicates DB state)
- `TxtDbStatus` — `TextBlock` in status bar

Status bar colors used:
- Green `#FF00FF00` — connected
- Amber `#FFFFB900` — degraded / unreachable
- Gray — not configured / access denied

---

## 16. `OnFirstRunCompleted` Event Chain

```
View_FirstRun.SetupCompleted event
  → MainWindow.OnFirstRunCompleted()
    → Microsoft.Windows.AppLifecycle.AppInstance.Restart(string.Empty)
```

The splash does not interfere with this chain. After the wizard, `AppInstance.Restart()` fires before `SplashWindow` is ever relevant again (the next launch will go through the normal splash flow).

---

## 17. Edge Cases Already Handled in Code

These are fixed in `App.xaml.cs` and must be preserved in `Service_StartupCoordinator`:

1. **`neverConfigured=true` + `probe=Ready`** — bypass wizard, proceed to Windows auth.
2. **`MarkCompleteAsync()` write failure** — `try/catch`, log non-fatal warning, continue.

---

## 18. Edge Cases That Remain (Documentation Only)

These are not fixed in code but are logged:

1. **Corrupt `server-settings.json`** — `LoadFromDisk()` falls back to defaults silently. Result: wizard re-runs on existing install. Mitigation: restore backup.
2. **`IsAuthorisedAsync` silent `catch`** — DB drop between probe and auth query returns `false`, which routes to Access Denied instead of Degraded. Mitigation: log is the only diagnostic.

---

## 19. Imports Required in New Files

### `SplashWindow.xaml.cs`

```csharp
using Microsoft.UI.Xaml;
using MTM_Waitlist_Server.Admin.ViewModels;
using MTM_Waitlist_Server.Admin.Services;
using MTM_Waitlist_Server.Core.Models.Splash;
using MTM_Waitlist_Server.Core.Models.FirstRun;
```

### `Service_StartupCoordinator.cs`

```csharp
using MTM_Waitlist_Server.Admin.Logging;
using MTM_Waitlist_Server.Core.Interfaces.Auth;
using MTM_Waitlist_Server.Core.Interfaces.FirstRun;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using MTM_Waitlist_Server.Core.Models.Splash;
using MTM_Waitlist_Server.Api.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
```

### `ViewModel_Splash.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist_Server.Core.Models.Splash;
using System;
using System.Threading;
using System.Threading.Tasks;
```
