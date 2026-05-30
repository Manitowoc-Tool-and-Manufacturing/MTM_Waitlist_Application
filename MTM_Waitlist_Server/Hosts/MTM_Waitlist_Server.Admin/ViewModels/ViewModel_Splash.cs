using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using MTM_Waitlist_Server.Core.Interfaces.Splash;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using MTM_Waitlist_Server.Core.Models.Splash;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.ViewModels;

/// <summary>
/// Drives the startup splash screen.
/// Implements <see cref="IProgress{T}"/> so the coordinator can report each step
/// directly to this ViewModel; all UI updates are dispatched to the UI thread.
/// </summary>
public sealed partial class ViewModel_Splash : ObservableObject, IProgress<StartupStep>
{
    private readonly IService_StartupCoordinator _coordinator;
    private DispatcherQueue? _dispatcherQueue;

    // ── Stored for action-button commands ─────────────────────────────────────
    private string? _pendingDegradedReason;
    private FirstRunStatus _pendingFirstRunStatus;
    private Model_FirstRunProbeResult? _pendingProbeResult;
    private string? _pendingAccessDeniedUser;

    // ── Observable properties ─────────────────────────────────────────────────

    /// <summary>Collection of startup steps shown in the progress list.</summary>
    [ObservableProperty]
    public partial ObservableCollection<StartupStep> Steps { get; set; }

    /// <summary>Current friendly status line displayed below the step list.</summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    /// <summary>Controls the indeterminate progress bar visibility.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusyVisibility))]
    public partial bool IsBusy { get; set; }

    /// <summary>When <c>true</c>, the issue view replaces the progress view.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIssueVisibility))]
    [NotifyPropertyChangedFor(nameof(NoIssueVisibility))]
    public partial bool HasIssue { get; set; }

    /// <summary>Heading shown on the issue view.</summary>
    [ObservableProperty]
    public partial string IssueTitle { get; set; }

    /// <summary>Explanation shown on the issue view.</summary>
    [ObservableProperty]
    public partial string IssueDetail { get; set; }

    /// <summary>Log tail content shown in the expandable diagnostics panel.</summary>
    [ObservableProperty]
    public partial string DiagnosticsText { get; set; }

    /// <summary>Controls whether the diagnostics panel is expanded.</summary>
    [ObservableProperty]
    public partial bool DiagnosticsExpanded { get; set; }

    /// <summary>Shows the Retry button.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRetryVisibility))]
    public partial bool CanRetry { get; set; }

    /// <summary>Shows the Open Degraded Mode button.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpenDegradedVisibility))]
    public partial bool CanOpenDegraded { get; set; }

    /// <summary>Shows the Open Setup Wizard button.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpenWizardVisibility))]
    public partial bool CanOpenWizard { get; set; }

    /// <summary>Shows the Open Settings button.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpenSettingsVisibility))]
    public partial bool CanOpenSettings { get; set; }

    // ── Computed Visibility properties ────────────────────────────────────────
    // Window is not a FrameworkElement in WinUI 3, so x:Bind + Converter is
    // unsupported. These computed properties expose Visibility directly so the
    // XAML can use plain x:Bind without converters.

    /// <summary>Visible when <see cref="IsBusy"/> is <c>true</c>.</summary>
    public Visibility IsBusyVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Visible when <see cref="HasIssue"/> is <c>false</c> (progress view).</summary>
    public Visibility NoIssueVisibility => HasIssue ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Visible when <see cref="HasIssue"/> is <c>true</c> (issue view).</summary>
    public Visibility HasIssueVisibility => HasIssue ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Visible when <see cref="CanRetry"/> is <c>true</c>.</summary>
    public Visibility CanRetryVisibility => CanRetry ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Visible when <see cref="CanOpenDegraded"/> is <c>true</c>.</summary>
    public Visibility CanOpenDegradedVisibility => CanOpenDegraded ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Visible when <see cref="CanOpenWizard"/> is <c>true</c>.</summary>
    public Visibility CanOpenWizardVisibility => CanOpenWizard ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Visible when <see cref="CanOpenSettings"/> is <c>true</c>.</summary>
    public Visibility CanOpenSettingsVisibility => CanOpenSettings ? Visibility.Visible : Visibility.Collapsed;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised on the UI thread when the coordinator produces a final outcome that requires
    /// navigation — either routing to <c>MainWindow</c> or exiting the application.
    /// </summary>
    public event Action<StartupOutcome>? OutcomeReady;

    // ── CancellationTokenSource ───────────────────────────────────────────────

    /// <summary>Token source used by <see cref="CancelCommand"/> to abort the coordinator.</summary>
    internal CancellationTokenSource Cts { get; private set; } = new();

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>Initialises the ViewModel with the coordinator service.</summary>
    public ViewModel_Splash(IService_StartupCoordinator coordinator)
    {
        _coordinator = coordinator;
        Steps = [];
        StatusMessage = "Starting\u2026";
        IsBusy = true;
        IssueTitle = string.Empty;
        IssueDetail = string.Empty;
        DiagnosticsText = string.Empty;
    }

    // ── IProgress<StartupStep> ────────────────────────────────────────────────

    void IProgress<StartupStep>.Report(StartupStep step)
    {
        // Report can be called from any thread — dispatch to UI thread.
        _dispatcherQueue?.TryEnqueue(() => ApplyStep(step));
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by <c>App.OnLaunched</c> on the UI thread before any background work starts.
    /// Must be called before <see cref="StartAsync"/>.
    /// </summary>
    public void SetDispatcherQueue(DispatcherQueue dq) => _dispatcherQueue = dq;

    /// <summary>
    /// Fires off the startup coordinator on a background thread.
    /// Call after <see cref="SetDispatcherQueue"/> and after the splash window is visible.
    /// </summary>
    public async Task StartAsync()
    {
        Reset();
        Cts = new CancellationTokenSource();
        var outcome = await Task.Run(() => _coordinator.RunAsync(this, Cts.Token)).ConfigureAwait(false);
        _dispatcherQueue?.TryEnqueue(() => ApplyOutcome(outcome));
    }

    /// <summary>
    /// Applies the coordinator's final outcome on the UI thread.
    /// Raises <see cref="OutcomeReady"/> immediately for <c>Normal</c> and <c>Cancelled</c>;
    /// populates the issue view for all other outcomes and waits for the user to act.
    /// </summary>
    public void ApplyOutcome(StartupOutcome outcome)
    {
        IsBusy = false;

        switch (outcome)
        {
            case StartupOutcome.Normal:
                OutcomeReady?.Invoke(outcome);
                break;

            case StartupOutcome.Cancelled:
                OutcomeReady?.Invoke(outcome);
                break;

            case StartupOutcome.Degraded d:
                _pendingDegradedReason = d.Reason;
                ShowIssue(
                    title: "Unable to start normally",
                    detail: d.Reason,
                    canRetry: true,
                    canOpenDegraded: true,
                    canOpenSettings: true);
                break;

            case StartupOutcome.FirstRunWizard w:
                _pendingFirstRunStatus = w.Status;
                _pendingProbeResult = w.ProbeResult;
                ShowIssue(
                    title: "First-run setup required",
                    detail: GetWizardDetail(w.Status, w.ProbeResult),
                    canRetry: false,
                    canOpenWizard: true,
                    canOpenSettings: true);
                break;

            case StartupOutcome.AccessDenied a:
                _pendingAccessDeniedUser = a.WindowsUser;
                ShowIssue(
                    title: "Access denied",
                    detail: $"Windows user \u2018{a.WindowsUser}\u2019 is not listed as an Admin or Developer in the database." +
                        "\n\nIf you believe this is an error, the database may be temporarily unreachable." +
                        " Check the diagnostics log and retry.",
                    canRetry: true,
                    canOpenSettings: false);
                break;

            case StartupOutcome.ApiHostFailed f:
                // Non-fatal — app continues without the API.  Show a warning then auto-route to Normal.
                ShowIssue(
                    title: "API host failed (non-fatal)",
                    detail: $"The REST API could not start and will be unavailable.\n\nDetail: {f.Reason}" +
                        "\n\nThe admin shell will still open.  Restart the application to retry the API.",
                    canRetry: false,
                    canOpenDegraded: false);
                // Expose a button so the user can proceed manually, or just open after a brief pause.
                CanOpenSettings = false;
                CanRetry = false;
                CanOpenDegraded = false;
                CanOpenWizard = false;
                // Surface a "Continue" path via the existing "Open Degraded" slot relabelled at the XAML layer.
                // Store the normal outcome so the button fires the right route.
                _pendingDegradedReason = null;
                CanOpenDegraded = true;   // re-used as "Continue anyway" for ApiHostFailed case
                _pendingDegradedReason = f.Reason;
                break;
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>Re-runs the coordinator from the beginning.</summary>
    [RelayCommand]
    private async Task RetryAsync()
    {
        await Task.Run(StartAsync).ConfigureAwait(false);
    }

    /// <summary>Routes to <c>MainWindow</c> in degraded mode.</summary>
    [RelayCommand]
    private void OpenDegraded()
    {
        OutcomeReady?.Invoke(new StartupOutcome.Degraded(_pendingDegradedReason ?? string.Empty));
    }

    /// <summary>Routes to <c>MainWindow</c> in first-run wizard mode.</summary>
    [RelayCommand]
    private void OpenWizard()
    {
        if (_pendingProbeResult is not null)
        {
            OutcomeReady?.Invoke(new StartupOutcome.FirstRunWizard(_pendingFirstRunStatus, _pendingProbeResult));
        }
    }

    /// <summary>Opens the settings file location in Explorer (first-pass placeholder).</summary>
    [RelayCommand]
    private void OpenSettings()
    {
        // First-pass: open the %ProgramData% settings folder so the user can edit the file manually.
        var settingsDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MTM", "WaitlistServer");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(settingsDir)
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            // If the folder cannot be opened, do nothing — the path is shown in the diagnostics text.
        }
    }

    /// <summary>Expands or collapses the diagnostics panel and loads the log tail on first open.</summary>
    [RelayCommand]
    private void ToggleDiagnostics()
    {
        DiagnosticsExpanded = !DiagnosticsExpanded;
        if (DiagnosticsExpanded && string.IsNullOrEmpty(DiagnosticsText))
        {
            LoadDiagnosticsLog();
        }
    }

    /// <summary>Cancels the running coordinator.</summary>
    [RelayCommand]
    private void Cancel()
    {
        Cts.Cancel();
        // OutcomeReady(Cancelled) will fire when the coordinator's task completes.
        // As a safety net, also fire directly in case the coordinator already returned.
        OutcomeReady?.Invoke(new StartupOutcome.Cancelled());
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void Reset()
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            Steps.Clear();
            StatusMessage = "Starting\u2026";
            IsBusy = true;
            HasIssue = false;
            IssueTitle = string.Empty;
            IssueDetail = string.Empty;
            DiagnosticsText = string.Empty;
            DiagnosticsExpanded = false;
            CanRetry = false;
            CanOpenDegraded = false;
            CanOpenWizard = false;
            CanOpenSettings = false;
        });
    }

    private void ApplyStep(StartupStep step)
    {
        // Replace existing step entry for this ID, or append if new.
        for (int i = 0; i < Steps.Count; i++)
        {
            if (Steps[i].StepId == step.StepId)
            {
                Steps[i] = step;
                StatusMessage = step.FriendlyLabel;
                return;
            }
        }

        Steps.Add(step);
        StatusMessage = step.FriendlyLabel;
    }

    private void ShowIssue(
        string title,
        string detail,
        bool canRetry = false,
        bool canOpenDegraded = false,
        bool canOpenWizard = false,
        bool canOpenSettings = false)
    {
        HasIssue = true;
        IssueTitle = title;
        IssueDetail = detail;
        CanRetry = canRetry;
        CanOpenDegraded = canOpenDegraded;
        CanOpenWizard = canOpenWizard;
        CanOpenSettings = canOpenSettings;
    }

    private void LoadDiagnosticsLog()
    {
        try
        {
            var logPath = Logging.StartupLogger.LogFilePath;
            if (System.IO.File.Exists(logPath))
            {
                // Read the last 80 lines to keep the panel compact.
                var lines = System.IO.File.ReadAllLines(logPath);
                var tail = lines.Length <= 80
                    ? lines
                    : lines[(lines.Length - 80)..];
                DiagnosticsText = string.Join(Environment.NewLine, tail);
            }
            else
            {
                DiagnosticsText = $"Log file not found:\n{logPath}";
            }
        }
        catch (Exception ex)
        {
            DiagnosticsText = $"Could not read log file: {ex.Message}";
        }
    }

    private static string GetWizardDetail(FirstRunStatus status, Model_FirstRunProbeResult probe)
    {
        return status switch
        {
            FirstRunStatus.MySqlUnreachable =>
                $"MySQL could not be reached.\n\n{probe.ErrorMessage}\n\nOpen the setup wizard to enter correct database credentials.",

            FirstRunStatus.SchemaMissing =>
                "MySQL is reachable but the application database schema was not found.\n\n" +
                "Open the setup wizard to run the initial schema migration.",

            FirstRunStatus.NoAdminUser =>
                "The database schema exists but no Admin or Developer user has been created.\n\n" +
                "Open the setup wizard to create the first admin account.",

            _ => "First-run setup is required before the admin shell can open.",
        };
    }
}
