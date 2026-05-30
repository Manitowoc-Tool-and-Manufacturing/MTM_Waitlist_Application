# SPLASH-02: Plan — Splash Screen Implementation

This is the detailed plan of attack for implementing the startup splash screen in `MTM_Waitlist_Server.Admin`.
Reference [SPLASH-01-Research.md](SPLASH-01-Research.md) for all code facts, signatures, and edge cases.

---

## Overview

The splash screen replaces the invisible blocking startup sequence in `App.OnLaunched()`.
A `SplashWindow` activates immediately on launch and shows the user each startup step as it runs.
A new `Service_StartupCoordinator` extracts all decision logic from `App.OnLaunched()` and reports
progress via `IProgress<StartupStep>`. When the coordinator completes, `App.xaml.cs` routes into
the correct `MainWindow` mode based on the `StartupOutcome` result.

No existing startup decision rules change. The splash is a presentation and orchestration layer only.

---

## Architecture Diagram

```
App.OnLaunched()
  │
  ├─ Build DI (RegisterSharedServices) ─────────────────────────────────────────────────┐
  │                                                                                       │
  ├─ Resolve SplashWindow (from DI)                                                       │
  ├─ Activate SplashWindow ────────────────────────────────────► SplashWindow (UI thread)│
  │                                                                      │                │
  ├─ Resolve Service_StartupCoordinator (from DI)                        │                │
  │                                                                      │                │
  ├─ Task.Run(coordinator.RunAsync(progress, ct)) ─────────────────────►│                │
  │    │                                                                  │                │
  │    ├─ Load settings ──────────────────── Progress.Report(step) ─────►│ ViewModel_Splash
  │    ├─ Compute sentinel ────────────────── Progress.Report(step) ─────►│   [updates step list]
  │    ├─ ProbeAsync() (up to 15s) ────────── Progress.Report(step) ─────►│   [updates status text]
  │    ├─ EvaluateBranch() ─────────────────── Progress.Report(step) ────►│
  │    ├─ IsAuthorisedAsync() ──────────────── Progress.Report(step) ────►│
  │    ├─ apiHost.Start() ───────────────────── Progress.Report(step) ───►│
  │    └─ scheduler.StartAsync() ──────────────── Progress.Report(step) ─►│
  │         │                                                              │
  │         └─ return StartupOutcome ─────────────────────────────────────┘
  │                   │
  │                   ▼
  ├─ SplashWindow.OnOutcomeReceived(outcome)
  │       ├─ outcome=Normal   → fade out → new MainWindow() → activate
  │       ├─ outcome=Degraded → show issue view + action buttons → user clicks "Open Degraded" → new MainWindow(degraded:true, ...)
  │       ├─ outcome=FirstRun → show issue view + action buttons → user clicks "Open Wizard"   → new MainWindow(firstRunStatus:..., ...)
  │       ├─ outcome=AccessDenied → show issue view + action buttons → Retry / Open Log / Cancel
  │       └─ Cancelled        → GC.Collect() → Application.Current.Exit()
  │
  └─ (original App.OnLaunched ends — MainWindow was activated by SplashWindow event handler)
```

---

## Phase 1 — Core Models and Interface (`Core` project)

**Reference:** SPLASH-01 §5, §8

### 1.1 Create `StartupStep` record

**File:** `Core/MTM_Waitlist_Server.Core/Models/Splash/StartupStep.cs`

```csharp
namespace MTM_Waitlist_Server.Core.Models.Splash;

/// <summary>Progress token reported by Service_StartupCoordinator for each startup step.</summary>
public sealed record StartupStep(
    StartupStepId StepId,
    StartupStepState State,
    string FriendlyLabel,
    string? Detail = null);
```

**File:** `Core/MTM_Waitlist_Server.Core/Models/Splash/StartupStepId.cs`

```csharp
namespace MTM_Waitlist_Server.Core.Models.Splash;

public enum StartupStepId
{
    BuildingContainer,
    LoadingSettings,
    ComputingSentinel,
    ProbingMySQL,
    EvaluatingBranch,
    CheckingWindowsAuth,
    StartingApiHost,
    StartingScheduler,
    Complete
}
```

**File:** `Core/MTM_Waitlist_Server.Core/Models/Splash/StartupStepState.cs`

```csharp
namespace MTM_Waitlist_Server.Core.Models.Splash;

public enum StartupStepState { Pending, InProgress, Succeeded, Failed, Skipped }
```

### 1.2 Create `StartupOutcome` discriminated union

**File:** `Core/MTM_Waitlist_Server.Core/Models/Splash/StartupOutcome.cs`

```csharp
namespace MTM_Waitlist_Server.Core.Models.Splash;

public abstract record StartupOutcome
{
    public sealed record Normal() : StartupOutcome;
    public sealed record Degraded(string Reason) : StartupOutcome;
    public sealed record FirstRunWizard(FirstRunStatus Status, Model_FirstRunProbeResult ProbeResult) : StartupOutcome;
    public sealed record AccessDenied(string WindowsUser) : StartupOutcome;
    public sealed record ApiHostFailed(string Reason) : StartupOutcome;
    public sealed record Cancelled() : StartupOutcome;
}
```

### 1.3 Create `IService_StartupCoordinator` interface

**File:** `Core/MTM_Waitlist_Server.Core/Interfaces/Splash/IService_StartupCoordinator.cs`

```csharp
namespace MTM_Waitlist_Server.Core.Interfaces.Splash;

public interface IService_StartupCoordinator
{
    Task<StartupOutcome> RunAsync(IProgress<StartupStep> progress, CancellationToken ct);
}
```

---

## Phase 2 — `ViewModel_Splash`

**Reference:** SPLASH-01 §12, §13, §5

**File:** `Hosts/MTM_Waitlist_Server.Admin/ViewModels/ViewModel_Splash.cs`

### Observable properties

| Property | Type | Purpose |
|----------|------|---------|
| `Steps` | `ObservableCollection<StartupStep>` | Bound to step list in XAML |
| `StatusMessage` | `string` | Current friendly status line below steps |
| `IsBusy` | `bool` | Controls spinner visibility |
| `HasIssue` | `bool` | Switches from progress view to issue view |
| `IssueTitle` | `string` | Heading on issue view |
| `IssueDetail` | `string` | Explanation on issue view |
| `DiagnosticsText` | `string` | Log tail shown in expandable panel |
| `DiagnosticsExpanded` | `bool` | Controls expand/collapse |
| `CanRetry` | `bool` | Shows Retry button |
| `CanOpenDegraded` | `bool` | Shows Open Degraded button |
| `CanOpenWizard` | `bool` | Shows Open Wizard button |
| `CanOpenSettings` | `bool` | Shows Open Settings button |
| `Outcome` | `StartupOutcome?` | Set when coordinator finishes — triggers routing |

### Commands

| Command | Signature | Effect |
|---------|-----------|--------|
| `RetryCommand` | `[RelayCommand]` | Re-runs coordinator, resets step list |
| `OpenDegradedCommand` | `[RelayCommand]` | Sets `Outcome = new Degraded(...)` |
| `OpenWizardCommand` | `[RelayCommand]` | Sets `Outcome = new FirstRunWizard(...)` |
| `OpenSettingsCommand` | `[RelayCommand]` | Opens settings panel / navigates to settings |
| `ExpandDiagnosticsCommand` | `[RelayCommand]` | Toggles `DiagnosticsExpanded` |
| `CancelCommand` | `[RelayCommand]` | Cancels coordinator token, sets `Outcome = Cancelled` |

### Progress handler (`IProgress<StartupStep>`)

The ViewModel implements `IProgress<StartupStep>`. Each `Report(step)` call:
1. Dispatches to the UI thread via `DispatcherQueue`.
2. Updates or adds the step in `Steps`.
3. Sets `StatusMessage = step.FriendlyLabel`.
4. When `step.State == Failed`, sets `HasIssue = true`, populates issue view strings.

---

## Phase 3 — `Service_StartupCoordinator`

**Reference:** SPLASH-01 §6, §7, §17, §18

**File:** `Hosts/MTM_Waitlist_Server.Admin/Services/Service_StartupCoordinator.cs`

The coordinator is a direct extraction of the logic currently in `App.OnLaunched()`. It must:

1. Accept `IProgress<StartupStep>` and `CancellationToken`.
2. Report each step as it starts (`InProgress`) and finishes (`Succeeded` / `Failed`).
3. Honour cancellation between steps (check `ct.IsCancellationRequested` before each step).
4. Return a `StartupOutcome` record (never throw).
5. Preserve the exact decision tree rules from §6 of the Research file.
6. Preserve both code fixes: `neverConfigured+Ready` bypass and self-heal `try/catch`.

Constructor injection:

```csharp
public Service_StartupCoordinator(
    IService_FirstRun firstRunService,
    IService_SettingsStore settingsStore,
    IService_AdminAuth adminAuth,
    IService_ApiHost apiHost,
    BackupSchedulerService scheduler)
```

> `BackupSchedulerService` is registered as `Singleton` — constructor injection is correct.

The DI container is already built before the coordinator runs. Steps 1 (DI build) and 9 (Complete) are reported but do not perform any async work in the coordinator — they are bookend markers.

---

## Phase 4 — `SplashWindow`

**Reference:** SPLASH-01 §10, §13

**File:** `Hosts/MTM_Waitlist_Server.Admin/SplashWindow.xaml`

### XAML Structure

```
Window (MicaBackdrop, Title="MTM Waitlist Server — Starting…")
  Grid (rows: * = progress area, Auto = action area)
    StackPanel [progress view, hidden when HasIssue]
      Image (logo)
      TextBlock (StatusMessage)
      ProgressBar (IsIndeterminate, visible when IsBusy)
      ItemsControl (Steps → step row template)
    StackPanel [issue view, visible when HasIssue]
      TextBlock (IssueTitle)
      TextBlock (IssueDetail)
      Expander (DiagnosticsText)
    StackPanel [buttons, always visible when issue]
      Button (Retry, visible CanRetry)
      Button (Open Degraded Mode, visible CanOpenDegraded)
      Button (Open Setup Wizard, visible CanOpenWizard)
      Button (Open Settings, visible CanOpenSettings)
      Button (View Log File)
      Button (Cancel)
```

Window size: `SplashWidth=400, SplashHeight=280` — expand to `SplashHeight=450` when issue view is visible.

### `SplashWindow.xaml.cs` responsibilities

```csharp
public SplashWindow(ViewModel_Splash viewModel)
{
    InitializeComponent();
    ViewModel = viewModel;
    _windowSizer = new Service_WindowSizer(this);
    _windowSizer.ApplySplashSize();
    // Capture DispatcherQueue on UI thread for the ViewModel
    viewModel.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());
    // Subscribe to outcome — raised by ViewModel when coordinator finishes
    viewModel.OutcomeReady += OnOutcomeReady;
}

private void OnOutcomeReady(StartupOutcome outcome)
{
    // Called on UI thread (ViewModel dispatches)
    switch (outcome)
    {
        case StartupOutcome.Normal:
            _mainWindow = new MainWindow();
            _mainWindow.Activate();
            Close();
            break;
        case StartupOutcome.Degraded d:
            // Show issue view + enable CanOpenDegraded
            // User explicitly clicks the button — handled by ViewModel command
            break;
        case StartupOutcome.FirstRunWizard w:
            // Show issue view + enable CanOpenWizard
            break;
        case StartupOutcome.AccessDenied a:
            // Show issue view
            break;
        case StartupOutcome.Cancelled:
            GC.Collect();
            Application.Current.Exit();
            break;
    }
}
```

> `Normal` outcome closes the splash immediately. Other outcomes let the user read the issue view and choose an action button. Action buttons on `ViewModel_Splash` set `Outcome` to the routed value and fire `OutcomeReady` again, which triggers the final `MainWindow` construction.

---

## Phase 5 — Modify `App.xaml.cs`

**Reference:** SPLASH-01 §12, §13

### New `OnLaunched()` skeleton

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    StartupLogger.Section("DI Container");

    var sharedServices = new ServiceCollection();
    RegisterSharedServices(sharedServices);

    // IService_ApiHost registration (same as before)
    Service_ApiHost? apiHost = null;
    sharedServices.AddSingleton<IService_ApiHost>(sp => { ... });

    var provider = sharedServices.BuildServiceProvider();
    Services = provider;

    // Resolve and activate splash
    var splash = provider.GetRequiredService<SplashWindow>();
    _window = splash;   // keep a reference so it is not GC'd
    splash.Activate();

    // Run coordinator on background thread
    var coordinator = provider.GetRequiredService<IService_StartupCoordinator>();
    var cts = new CancellationTokenSource();
    splash.ViewModel.CancellationTokenSource = cts;

    Task.Run(async () =>
    {
        var outcome = await coordinator.RunAsync(splash.ViewModel, cts.Token);
        splash.ViewModel.ApplyOutcome(outcome);   // dispatches to UI thread internally
    });
}
```

The `_window` field type changes from `MainWindow?` to `Window?` to hold either `SplashWindow` or `MainWindow`.

---

## Phase 6 — Update `RegisterSharedServices()`

Add to the existing registrations:

```csharp
// Splash infrastructure
services.AddSingleton<IService_StartupCoordinator, Service_StartupCoordinator>();
services.AddTransient<ViewModel_Splash>();
services.AddTransient<SplashWindow>();
```

Add new `using` statements at top of `App.xaml.cs`:

```csharp
using MTM_Waitlist_Server.Admin.Services;     // Service_StartupCoordinator
using MTM_Waitlist_Server.Core.Interfaces.Splash;  // IService_StartupCoordinator
using MTM_Waitlist_Server.Core.Models.Splash;      // StartupOutcome, StartupStep
```

---

## Phase 7 — Update `IService_WindowSizer` and `Service_WindowSizer`

Add `ApplySplashSize()` to the interface and the implementation.

**Interface addition:**

```csharp
/// <summary>Applies the compact window size for the startup splash screen.</summary>
void ApplySplashSize();
```

**Implementation addition:**

```csharp
private const int SplashWidth  = 400;
private const int SplashHeight = 280;

public void ApplySplashSize()
{
    _appWindow.Resize(new SizeInt32(SplashWidth, SplashHeight));
    CenterOnMonitor();
}
```

---

## Phase 8 — Test All Outcome Paths

**Reference:** SPLASH-01 §6

Each outcome path from the coordinator must be exercised. Test scenarios:

| # | Setup | Expected outcome |
|---|-------|-----------------|
| 1 | `UpdaterPassword` empty, probe=Unreachable | `FirstRunWizard(MySqlUnreachable, ...)` |
| 2 | `UpdaterPassword` empty, probe=Ready | `Normal` (neverConfigured+Ready bypass) |
| 3 | probe=Unreachable, `FirstRunComplete=true` | `Degraded("MySQL could not be reached...")` |
| 4 | probe=SchemaMissing, `FirstRunComplete=false` | `FirstRunWizard(SchemaMissing, ...)` |
| 5 | probe=SchemaMissing, `FirstRunComplete=true` | `Degraded("schema not found...")` |
| 6 | probe=NoAdminUser, `FirstRunComplete=true` | `Degraded("No active Admin or Developer user...")` |
| 7 | probe=NoAdminUser, `FirstRunComplete=false` | `FirstRunWizard(NoAdminUser, ...)` |
| 8 | probe=Ready, `IsAuthorisedAsync=false` | `AccessDenied` |
| 9 | probe=Ready, `IsAuthorisedAsync=true` | `Normal` |
| 10 | probe=Ready, `FirstRunComplete=false`, write succeeds | `Normal` (self-heal) |
| 11 | probe=Ready, `FirstRunComplete=false`, write fails | `Normal` (self-heal non-fatal) |
| 12 | User clicks Cancel mid-probe | `Cancelled` → `Exit()` |

---

## Out of Scope for First Pass

- Android / WinUI client splash screens.
- Animated logo or progress ring beyond `ProgressBar`.
- Settings editor embedded inside the splash (Open Settings routes to existing settings module).
- Accessibility audit.
- UI tests via Appium.
