using MTM_Waitlist_Server.Admin.Logging;
using MTM_Waitlist_Server.Core.Interfaces.Api;
using MTM_Waitlist_Server.Core.Interfaces.Auth;
using MTM_Waitlist_Server.Core.Interfaces.FirstRun;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Interfaces.Splash;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using MTM_Waitlist_Server.Core.Models.Settings;
using MTM_Waitlist_Server.Core.Models.Splash;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.Services;

/// <summary>
/// Extracts the startup decision tree from <c>App.OnLaunched</c> into an async coordinator
/// that reports each step via <see cref="IProgress{T}"/> for display on the splash screen.
/// </summary>
internal sealed class Service_StartupCoordinator : IService_StartupCoordinator
{
    private readonly IService_FirstRun _firstRun;
    private readonly IService_SettingsStore _settingsStore;
    private readonly IService_AdminAuth _adminAuth;
    private readonly IService_ApiHost _apiHost;
    private readonly BackupSchedulerService _scheduler;

    /// <summary>Initialises the coordinator with all services required by the startup sequence.</summary>
    public Service_StartupCoordinator(
        IService_FirstRun firstRun,
        IService_SettingsStore settingsStore,
        IService_AdminAuth adminAuth,
        IService_ApiHost apiHost,
        BackupSchedulerService scheduler)
    {
        _firstRun = firstRun;
        _settingsStore = settingsStore;
        _adminAuth = adminAuth;
        _apiHost = apiHost;
        _scheduler = scheduler;
    }

    /// <inheritdoc/>
    public async Task<StartupOutcome> RunAsync(IProgress<StartupStep> progress, CancellationToken ct)
    {
        try
        {
            return await RunCoreAsync(progress, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return new StartupOutcome.Cancelled();
        }
        catch (Exception ex)
        {
            StartupLogger.Error("Unhandled exception in startup coordinator.", ex);
            return new StartupOutcome.Degraded($"Unexpected startup failure: {ex.Message}");
        }
    }

    // ── Private core logic ────────────────────────────────────────────────────

    private async Task<StartupOutcome> RunCoreAsync(IProgress<StartupStep> progress, CancellationToken ct)
    {
        StartupLogger.Section("Startup Coordinator");

        // ── Step 1: Load settings ─────────────────────────────────────────────
        Report(progress, StartupStepId.LoadingSettings, StartupStepState.InProgress, "Loading configuration\u2026");
        ct.ThrowIfCancellationRequested();

        var settings = _settingsStore.Get();
        StartupLogger.Info($"Settings loaded. FirstRunComplete={settings.FirstRunComplete}, " +
            $"Host={settings.Database.Host}, Port={settings.Database.Port}, " +
            $"UpdaterPasswordSet={!string.IsNullOrWhiteSpace(settings.Database.UpdaterPassword)}");

        Report(progress, StartupStepId.LoadingSettings, StartupStepState.Succeeded, "Configuration loaded");

        // ── Step 2: Compute sentinel ──────────────────────────────────────────
        Report(progress, StartupStepId.ComputingSentinel, StartupStepState.InProgress, "Checking setup state\u2026");
        ct.ThrowIfCancellationRequested();

        bool neverConfigured = string.IsNullOrWhiteSpace(settings.Database.UpdaterPassword);
        StartupLogger.Info($"NeverConfigured sentinel (UpdaterPassword empty) = {neverConfigured}");

        Report(progress, StartupStepId.ComputingSentinel, StartupStepState.Succeeded, "Setup state verified");

        // ── Step 3: MySQL probe ───────────────────────────────────────────────
        Report(progress, StartupStepId.ProbingMySQL, StartupStepState.InProgress, "Connecting to database\u2026");
        ct.ThrowIfCancellationRequested();

        StartupLogger.Info($"Starting MySQL probe against {settings.Database.Host}:{settings.Database.Port} " +
            $"as '{settings.Database.UpdaterUsername}'.");

        Model_FirstRunProbeResult probeResult;
        try
        {
            probeResult = await _firstRun.ProbeAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            StartupLogger.Error("Unhandled exception during MySQL probe \u2014 treating as MySqlUnreachable.", ex);
            probeResult = Model_FirstRunProbeResult.Unreachable(ex.Message);
        }

        StartupLogger.Info($"Probe complete. Status={probeResult.Status}, " +
            $"ErrorMessage={(string.IsNullOrWhiteSpace(probeResult.ErrorMessage) ? "<none>" : probeResult.ErrorMessage)}");

        // ── Step 4: Evaluate decision tree ────────────────────────────────────
        StartupLogger.Section("Startup Decision Tree");
        StartupLogger.Info($"Evaluating branch: neverConfigured={neverConfigured}, probeStatus={probeResult.Status}, " +
            $"firstRunComplete={settings.FirstRunComplete}");

        Report(progress, StartupStepId.EvaluatingBranch, StartupStepState.InProgress, "Evaluating startup path\u2026");
        ct.ThrowIfCancellationRequested();

        if (neverConfigured)
        {
            if (probeResult.Status == FirstRunStatus.Ready)
            {
                // DB is accessible despite empty password — bypass wizard, go to Windows auth.
                StartupLogger.Info("BRANCH: NeverConfigured=true but probe=Ready \u2192 DB accessible despite empty password. Bypassing wizard; proceeding to Windows auth.");
                Report(progress, StartupStepId.ProbingMySQL, StartupStepState.Succeeded, "Database connection verified");
                Report(progress, StartupStepId.EvaluatingBranch, StartupStepState.Succeeded, "Startup path: Windows authorisation");
                // Fall through to Windows auth below.
            }
            else
            {
                StartupLogger.Info($"BRANCH: NeverConfigured=true \u2192 First-Run Wizard (probe={probeResult.Status}).");
                Report(progress, StartupStepId.ProbingMySQL, StartupStepState.Failed, $"Database: {probeResult.Status}", probeResult.ErrorMessage);
                Report(progress, StartupStepId.EvaluatingBranch, StartupStepState.Succeeded, "Startup path: First-Run Setup");
                return new StartupOutcome.FirstRunWizard(probeResult.Status, probeResult);
            }
        }
        else if (probeResult.Status == FirstRunStatus.MySqlUnreachable)
        {
            var reason = BuildUnreachableReason(settings, probeResult);
            StartupLogger.Warn($"BRANCH: WasConfigured + MySqlUnreachable \u2192 Degraded. Reason: {reason}");
            Report(progress, StartupStepId.ProbingMySQL, StartupStepState.Failed, "Database unreachable", reason);
            Report(progress, StartupStepId.EvaluatingBranch, StartupStepState.Succeeded, "Startup path: Degraded mode");
            return new StartupOutcome.Degraded(reason);
        }
        else if (probeResult.Status == FirstRunStatus.SchemaMissing && !settings.FirstRunComplete)
        {
            StartupLogger.Info("BRANCH: SchemaMissing + FirstRunComplete=false \u2192 First-Run Wizard at schema step.");
            Report(progress, StartupStepId.ProbingMySQL, StartupStepState.Failed, "Database schema not found");
            Report(progress, StartupStepId.EvaluatingBranch, StartupStepState.Succeeded, "Startup path: First-Run Setup");
            return new StartupOutcome.FirstRunWizard(probeResult.Status, probeResult);
        }
        else if (probeResult.Status == FirstRunStatus.SchemaMissing && settings.FirstRunComplete)
        {
            var reason = $"The database schema for '{settings.Database.DatabaseName}' was not found." +
                " It may have been dropped or the database name changed in Settings.";
            StartupLogger.Warn($"BRANCH: SchemaMissing + FirstRunComplete=true \u2192 Degraded (schema dropped post-setup). Reason: {reason}");
            Report(progress, StartupStepId.ProbingMySQL, StartupStepState.Failed, "Database schema missing");
            Report(progress, StartupStepId.EvaluatingBranch, StartupStepState.Succeeded, "Startup path: Degraded mode");
            return new StartupOutcome.Degraded(reason);
        }
        else if (probeResult.Status == FirstRunStatus.NoAdminUser && settings.FirstRunComplete)
        {
            const string reason = "No active Admin or Developer user was found in the database." +
                " The account may have been disabled or deleted after setup completed." +
                " Restore a backup or re-create the user directly in MySQL to recover.";
            StartupLogger.Warn($"BRANCH: NoAdminUser + FirstRunComplete=true \u2192 Degraded (admin deleted post-setup).");
            Report(progress, StartupStepId.ProbingMySQL, StartupStepState.Failed, "No admin user found");
            Report(progress, StartupStepId.EvaluatingBranch, StartupStepState.Succeeded, "Startup path: Degraded mode");
            return new StartupOutcome.Degraded(reason);
        }
        else if (probeResult.Status == FirstRunStatus.NoAdminUser && !settings.FirstRunComplete)
        {
            StartupLogger.Info("BRANCH: NoAdminUser + FirstRunComplete=false \u2192 First-Run Wizard at admin-creation step.");
            Report(progress, StartupStepId.ProbingMySQL, StartupStepState.Failed, "No admin user found");
            Report(progress, StartupStepId.EvaluatingBranch, StartupStepState.Succeeded, "Startup path: First-Run Setup");
            return new StartupOutcome.FirstRunWizard(probeResult.Status, probeResult);
        }
        else
        {
            // probe is Ready and neverConfigured is false — normal path.
            Report(progress, StartupStepId.ProbingMySQL, StartupStepState.Succeeded, "Database connection verified");
            Report(progress, StartupStepId.EvaluatingBranch, StartupStepState.Succeeded, "Startup path: Windows authorisation");
        }

        ct.ThrowIfCancellationRequested();

        // ── Self-heal if FirstRunComplete is false but probe is Ready ─────────
        if (!settings.FirstRunComplete)
        {
            StartupLogger.Warn("FirstRunComplete=false but probe is Ready. Self-healing: marking FirstRunComplete=true.");
            try
            {
                await _firstRun.MarkCompleteAsync(ct).ConfigureAwait(false);
                StartupLogger.Info("FirstRunComplete self-heal write succeeded.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Non-fatal: settings file may be read-only or %ProgramData% inaccessible.
                StartupLogger.Warn($"FirstRunComplete self-heal write failed (non-fatal): {ex.Message}. Continuing.");
            }
        }

        // ── Step 5: Windows authorisation ─────────────────────────────────────
        Report(progress, StartupStepId.CheckingWindowsAuth, StartupStepState.InProgress, "Verifying Windows identity\u2026");
        ct.ThrowIfCancellationRequested();

        var windowsUser = Service_AdminAuth.GetCurrentWindowsUsername();
        StartupLogger.Info($"Current Windows user: '{windowsUser}'. Checking authorisation against database.");

        bool isAuthorised;
        try
        {
            isAuthorised = await _adminAuth.IsAuthorisedAsync(windowsUser).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            StartupLogger.Error("IsAuthorisedAsync threw unexpectedly. Treating as access denied.", ex);
            isAuthorised = false;
        }

        StartupLogger.Info($"Windows authorisation result for '{windowsUser}': isAuthorised={isAuthorised}");

        if (!isAuthorised)
        {
            StartupLogger.Warn($"BRANCH: Access denied for Windows user '{windowsUser}'.");
            Report(progress, StartupStepId.CheckingWindowsAuth, StartupStepState.Failed,
                "Access denied", $"Windows user '{windowsUser}' is not authorised.");
            return new StartupOutcome.AccessDenied(windowsUser);
        }

        Report(progress, StartupStepId.CheckingWindowsAuth, StartupStepState.Succeeded, "Identity verified");
        ct.ThrowIfCancellationRequested();

        // ── Step 6: Start API host ────────────────────────────────────────────
        Report(progress, StartupStepId.StartingApiHost, StartupStepState.InProgress, "Starting API host\u2026");
        StartupLogger.Info($"Starting Kestrel API host on '{settings.Api.ListenAddress}'.");

        StartupOutcome? apiFailureOutcome = null;
        try
        {
            await _apiHost.EnsureRunningAsync(ct).ConfigureAwait(false);
            Report(progress, StartupStepId.StartingApiHost, StartupStepState.Succeeded, "API host started");
            StartupLogger.Info("Kestrel API host started successfully.");
        }
        catch (Exception ex)
        {
            StartupLogger.Error("Kestrel API host failed to start \u2014 application will continue without the API.", ex);
            Report(progress, StartupStepId.StartingApiHost, StartupStepState.Failed,
                "API host failed (non-fatal)", ex.Message);
            // Track failure so we can warn the user, but still proceed to Normal.
            apiFailureOutcome = new StartupOutcome.ApiHostFailed(ex.Message);
        }

        ct.ThrowIfCancellationRequested();

        // ── Step 7: Start backup scheduler ────────────────────────────────────
        Report(progress, StartupStepId.StartingScheduler, StartupStepState.InProgress, "Starting backup scheduler\u2026");
        _ = _scheduler.StartAsync(CancellationToken.None);
        StartupLogger.Info("BackupSchedulerService started on background thread.");
        Report(progress, StartupStepId.StartingScheduler, StartupStepState.Succeeded, "Backup scheduler started");

        // ── Complete ───────────────────────────────────────────────────────────
        Report(progress, StartupStepId.Complete, StartupStepState.Succeeded, "Ready");
        StartupLogger.Info($"Startup complete. Log file: {StartupLogger.LogFilePath}");

        // Return ApiHostFailed so the splash can warn the user before opening the main window.
        return apiFailureOutcome ?? new StartupOutcome.Normal();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Report(
        IProgress<StartupStep> progress,
        StartupStepId stepId,
        StartupStepState state,
        string label,
        string? detail = null)
    {
        progress.Report(new StartupStep(stepId, state, label, detail));
    }

    private static string BuildUnreachableReason(ServerSettings settings, Model_FirstRunProbeResult probe)
    {
        var prefix = $"MySQL could not be reached at {settings.Database.Host}:{settings.Database.Port}.";
        return string.IsNullOrWhiteSpace(probe.ErrorMessage)
            ? prefix
            : $"{prefix}\n\nDetail: {probe.ErrorMessage}";
    }
}
