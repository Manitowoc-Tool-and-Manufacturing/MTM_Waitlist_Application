# SPLASH-03: Tasks — Splash Screen Implementation Checklist

Check off items as you complete them. All items start unchecked.

---

## Phase 1 — Core Models and Interface

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 1 | Create `StartupStepState.cs` | `Core/Models/Splash/StartupStepState.cs` — enum: `Pending`, `InProgress`, `Succeeded`, `Failed`, `Skipped` |
| [ ] | 1 | Create `StartupStepId.cs` | `Core/Models/Splash/StartupStepId.cs` — enum: `BuildingContainer`, `LoadingSettings`, `ComputingSentinel`, `ProbingMySQL`, `EvaluatingBranch`, `CheckingWindowsAuth`, `StartingApiHost`, `StartingScheduler`, `Complete` |
| [ ] | 1 | Create `StartupStep.cs` | `Core/Models/Splash/StartupStep.cs` — `record StartupStep(StartupStepId, StartupStepState, string FriendlyLabel, string? Detail = null)` |
| [ ] | 1 | Create `StartupOutcome.cs` | `Core/Models/Splash/StartupOutcome.cs` — abstract record with nested records: `Normal`, `Degraded(string Reason)`, `FirstRunWizard(FirstRunStatus, Model_FirstRunProbeResult)`, `AccessDenied(string WindowsUser)`, `ApiHostFailed(string Reason)`, `Cancelled` |
| [ ] | 1 | Create `IService_StartupCoordinator.cs` | `Core/Interfaces/Splash/IService_StartupCoordinator.cs` — `Task<StartupOutcome> RunAsync(IProgress<StartupStep>, CancellationToken)` |
| [ ] | 1 | Add `ApplySplashSize()` to `IService_WindowSizer` | `Core/Interfaces/Window/IService_WindowSizer.cs` — add method with XML doc comment |

---

## Phase 2 — Window Sizer Update

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 2 | Add splash size constants | `Service_WindowSizer.cs` — add `private const int SplashWidth = 400;` and `SplashHeight = 280;` |
| [ ] | 2 | Implement `ApplySplashSize()` | `Service_WindowSizer.cs` — `_appWindow.Resize(new SizeInt32(SplashWidth, SplashHeight)); CenterOnMonitor();` |
| [ ] | 2 | Verify build — no errors in Admin host | `dotnet build Hosts/MTM_Waitlist_Server.Admin/MTM_Waitlist_Server.Admin.csproj` |

---

## Phase 3 — `Service_StartupCoordinator`

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 3 | Create `Service_StartupCoordinator.cs` | `Services/Service_StartupCoordinator.cs` — class skeleton, implements `IService_StartupCoordinator` |
| [ ] | 3 | Add constructor injection | Inject `IService_FirstRun`, `IService_SettingsStore`, `IService_AdminAuth`, `IService_ApiHost`, `BackupSchedulerService` |
| [ ] | 3 | Implement step reporting helper | Private method `ReportAsync(IProgress<StartupStep>, StartupStepId, StartupStepState, string label, string? detail)` |
| [ ] | 3 | Step 1 — `LoadingSettings` | `settingsStore.Get()` — report InProgress → Succeeded |
| [ ] | 3 | Step 2 — `ComputingSentinel` | Compute `neverConfigured` — report InProgress → Succeeded |
| [ ] | 3 | Step 3 — `ProbingMySQL` | `await firstRunService.ProbeAsync(ct)` — report InProgress → Succeeded/Failed; wrap in try/catch → `Unreachable` on exception |
| [ ] | 3 | Step 4 — `EvaluatingBranch` — `neverConfigured` + `Ready` bypass | If `neverConfigured=true` and `probe=Ready`, log + skip wizard, continue to Windows auth step — report Succeeded |
| [ ] | 3 | Step 4 — `EvaluatingBranch` — `neverConfigured` + non-Ready | Return `StartupOutcome.FirstRunWizard(probe.Status, probe)` |
| [ ] | 3 | Step 4 — `EvaluatingBranch` — `Unreachable` | Return `StartupOutcome.Degraded(reason)` |
| [ ] | 3 | Step 4 — `EvaluatingBranch` — `SchemaMissing + FirstRunComplete=false` | Return `StartupOutcome.FirstRunWizard(SchemaMissing, probe)` |
| [ ] | 3 | Step 4 — `EvaluatingBranch` — `SchemaMissing + FirstRunComplete=true` | Return `StartupOutcome.Degraded(reason)` |
| [ ] | 3 | Step 4 — `EvaluatingBranch` — `NoAdminUser + FirstRunComplete=true` | Return `StartupOutcome.Degraded(reason)` |
| [ ] | 3 | Step 4 — `EvaluatingBranch` — `NoAdminUser + FirstRunComplete=false` | Return `StartupOutcome.FirstRunWizard(NoAdminUser, probe)` |
| [ ] | 3 | Step 4 — `EvaluatingBranch` — `Ready + FirstRunComplete=false` — self-heal | `try { await firstRunService.MarkCompleteAsync(ct); } catch { StartupLogger.Warn(...); }` |
| [ ] | 3 | Step 5 — `CheckingWindowsAuth` | `GetCurrentWindowsUsername()` + `IsAuthorisedAsync(windowsUser)` — report InProgress → Succeeded/Failed |
| [ ] | 3 | Step 5 — auth failure | Return `StartupOutcome.AccessDenied(windowsUser)` |
| [ ] | 3 | Step 6 — `StartingApiHost` | `apiHost.Start()` inside try/catch — log failure, do NOT return `Degraded`; continue to next step |
| [ ] | 3 | Step 7 — `StartingScheduler` | `_ = scheduler.StartAsync(CancellationToken.None)` — fire-and-forget, report Succeeded |
| [ ] | 3 | Step 8 — `Complete` | Report Succeeded; return `StartupOutcome.Normal()` |
| [ ] | 3 | Cancellation guard | Check `ct.IsCancellationRequested` before each step; return `StartupOutcome.Cancelled()` if cancelled |
| [ ] | 3 | No unhandled exceptions escape | Wrap entire `RunAsync` body in outer try/catch; on unhandled exception return `StartupOutcome.Degraded(ex.Message)` + log |

---

## Phase 4 — `ViewModel_Splash`

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 4 | Create `ViewModel_Splash.cs` | `ViewModels/ViewModel_Splash.cs` — `partial class`, inherits `ObservableObject`, implements `IProgress<StartupStep>` |
| [ ] | 4 | Add `SetDispatcherQueue(DispatcherQueue)` | Store reference for dispatching `Report()` calls to UI thread |
| [ ] | 4 | `[ObservableProperty] Steps` | `ObservableCollection<StartupStep>` — bound to XAML step list |
| [ ] | 4 | `[ObservableProperty] StatusMessage` | `string` — current friendly status below steps |
| [ ] | 4 | `[ObservableProperty] IsBusy` | `bool` — controls spinner visibility |
| [ ] | 4 | `[ObservableProperty] HasIssue` | `bool` — switches progress view to issue view |
| [ ] | 4 | `[ObservableProperty] IssueTitle` | `string` |
| [ ] | 4 | `[ObservableProperty] IssueDetail` | `string` |
| [ ] | 4 | `[ObservableProperty] DiagnosticsText` | `string` — populated with tail of `StartupLogger.LogFilePath` |
| [ ] | 4 | `[ObservableProperty] DiagnosticsExpanded` | `bool` |
| [ ] | 4 | `[ObservableProperty] CanRetry` | `bool` |
| [ ] | 4 | `[ObservableProperty] CanOpenDegraded` | `bool` |
| [ ] | 4 | `[ObservableProperty] CanOpenWizard` | `bool` |
| [ ] | 4 | `[ObservableProperty] CanOpenSettings` | `bool` |
| [ ] | 4 | `OutcomeReady` event | `event Action<StartupOutcome>?` — raised on UI thread when routing decision is ready |
| [ ] | 4 | `CancellationTokenSource` property | `public CancellationTokenSource? CancellationTokenSource { get; set; }` |
| [ ] | 4 | Implement `IProgress<StartupStep>.Report()` | Dispatch to UI thread; update or add step in `Steps`; set `StatusMessage`; on `Failed` set issue view properties |
| [ ] | 4 | `ApplyOutcome(StartupOutcome)` | Called from background thread after coordinator completes; dispatch to UI thread; set action button visibility; raise `OutcomeReady` for `Normal` and `Cancelled`; for others set `HasIssue=true` |
| [ ] | 4 | `[RelayCommand] RetryCommand` | Reset `Steps`, `HasIssue=false`, re-run coordinator on new `Task.Run` |
| [ ] | 4 | `[RelayCommand] OpenDegradedCommand` | Raise `OutcomeReady(new StartupOutcome.Degraded(_lastDegradedReason!))` |
| [ ] | 4 | `[RelayCommand] OpenWizardCommand` | Raise `OutcomeReady(new StartupOutcome.FirstRunWizard(_lastStatus, _lastProbe!))` |
| [ ] | 4 | `[RelayCommand] OpenSettingsCommand` | TODO first pass: show message dialog "Edit server-settings.json at {path}" |
| [ ] | 4 | `[RelayCommand] ExpandDiagnosticsCommand` | Toggle `DiagnosticsExpanded`; load log tail on first expand |
| [ ] | 4 | `[RelayCommand] CancelCommand` | `CancellationTokenSource?.Cancel()` |

---

## Phase 5 — `SplashWindow` XAML + Code-Behind

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 5 | Create `SplashWindow.xaml` | `Hosts/MTM_Waitlist_Server.Admin/SplashWindow.xaml` — `Window` with `MicaBackdrop`, progress view, issue view, action buttons |
| [ ] | 5 | Progress view panel | `StackPanel` bound to `HasIssue=false`: logo image, `StatusMessage` TextBlock, indeterminate `ProgressBar` (visible when `IsBusy`), `ItemsControl` for `Steps` |
| [ ] | 5 | Step row `DataTemplate` | Each `StartupStep` row: state icon (✓/✗/…) + `FriendlyLabel` text |
| [ ] | 5 | Issue view panel | `StackPanel` bound to `HasIssue=true`: `IssueTitle`, `IssueDetail`, `Expander` for diagnostics |
| [ ] | 5 | Action buttons | `StackPanel` in bottom row: Retry, Open Degraded Mode, Open Setup Wizard, Open Settings, View Log File, Cancel — each visibility-bound to relevant `Can*` property |
| [ ] | 5 | Create `SplashWindow.xaml.cs` | Constructor takes `ViewModel_Splash`; calls `_windowSizer.ApplySplashSize()`; calls `viewModel.SetDispatcherQueue(...)`; subscribes to `viewModel.OutcomeReady` |
| [ ] | 5 | `OnOutcomeReady` handler | Routes to `MainWindow` constructor matching the outcome; closes splash or exits on `Cancelled` |
| [ ] | 5 | `View Log File` button handler | `Process.Start(new ProcessStartInfo(StartupLogger.LogFilePath) { UseShellExecute = true })` |

---

## Phase 6 — Modify `App.xaml.cs`

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 6 | Change `_window` field type | `Window? _window` (was `MainWindow? _window`) |
| [ ] | 6 | Refactor `OnLaunched()` | Remove all decision logic; replace with: build DI → resolve `SplashWindow` → activate → resolve coordinator → `Task.Run(RunAsync)` |
| [ ] | 6 | Remove blocking `Task.Run(...).GetAwaiter().GetResult()` calls | Probe and auth now run inside coordinator on background thread |
| [ ] | 6 | Remove `MainWindow` construction from `OnLaunched()` | `MainWindow` is now constructed exclusively by `SplashWindow.OnOutcomeReady()` |
| [ ] | 6 | Add new `using` statements | `IService_StartupCoordinator`, `StartupOutcome`, `StartupStep` |
| [ ] | 6 | Register new types in `RegisterSharedServices()` | `IService_StartupCoordinator`, `ViewModel_Splash` (Transient), `SplashWindow` (Transient) |

---

## Phase 7 — Build and Smoke Test

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 7 | Full solution build — zero errors | `dotnet build MTM_Waitlist_Server.slnx` |
| [ ] | 7 | Test path: Normal launch | DB accessible, user authorised → splash completes, `MainWindow` opens in normal mode |
| [ ] | 7 | Test path: MySQL unreachable (never configured) | Splash shows first-run issue view, Open Wizard button visible, wizard opens |
| [ ] | 7 | Test path: MySQL unreachable (was configured) | Splash shows degraded issue view, Open Degraded Mode button visible |
| [ ] | 7 | Test path: SchemaMissing + FirstRunComplete=false | First-run wizard via splash |
| [ ] | 7 | Test path: SchemaMissing + FirstRunComplete=true | Degraded via splash |
| [ ] | 7 | Test path: NoAdminUser + FirstRunComplete=false | First-run wizard via splash |
| [ ] | 7 | Test path: NoAdminUser + FirstRunComplete=true | Degraded via splash |
| [ ] | 7 | Test path: Access Denied | Windows user not in DB → access denied issue view |
| [ ] | 7 | Test path: Cancel during probe | CTS cancels → splash exits cleanly, no orphaned windows |
| [ ] | 7 | Test path: Self-heal write failure | `MarkCompleteAsync` throws → non-fatal warning, startup continues to normal |
| [ ] | 7 | Test path: `neverConfigured=true` + `probe=Ready` | Wizard NOT shown, splash proceeds to Windows auth step |
| [ ] | 7 | Verify `StartupLogger.LogFilePath` is set by time View Log File button is clicked | |

---

## Phase 8 — Unit Test Updates

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 8 | Create `Service_StartupCoordinatorTests.cs` | `Tests/Unit/MTM_Waitlist_Server.Admin.Tests/Services/Coordinator/Success/` |
| [ ] | 8 | Test — Normal path (full happy path) | All steps succeed, outcome is `Normal` |
| [ ] | 8 | Test — `neverConfigured+Ready` bypass | Wizard not opened, auth step runs |
| [ ] | 8 | Test — Probe returns `Unreachable` | Outcome is `Degraded` |
| [ ] | 8 | Test — `SchemaMissing + FirstRunComplete=false` | Outcome is `FirstRunWizard` |
| [ ] | 8 | Test — Self-heal write failure is non-fatal | `MarkCompleteAsync` throws, startup continues, outcome is `Normal` |
| [ ] | 8 | Test — Cancellation returns `Cancelled` outcome | CTS cancelled before probe |
| [ ] | 8 | Create `ViewModel_SplashTests.cs` | `Tests/Unit/MTM_Waitlist_Server.Admin.Tests/ViewModels/Splash/Properties/` |
| [ ] | 8 | Test — `ApplyOutcome(Normal)` sets `HasIssue=false` | |
| [ ] | 8 | Test — `ApplyOutcome(Degraded)` sets `HasIssue=true`, `CanOpenDegraded=true` | |
| [ ] | 8 | Test — `ApplyOutcome(AccessDenied)` sets `HasIssue=true`, no Degraded/Wizard buttons | |
