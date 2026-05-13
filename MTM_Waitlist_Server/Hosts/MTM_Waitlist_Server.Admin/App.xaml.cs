using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Threading;
using System.Threading.Tasks;
using MTM_Waitlist_Server.Admin.Logging;
using MTM_Waitlist_Server.Admin.Services;
using MTM_Waitlist_Server.Admin.ViewModels;
using MTM_Waitlist_Server.Admin.Views;
using MTM_Waitlist_Server.Api.Services;
using MTM_Waitlist_Server.Core.Interfaces.Api;
using MTM_Waitlist_Server.Core.Interfaces.Auth;
using MTM_Waitlist_Server.Core.Interfaces.Backup;
using MTM_Waitlist_Server.Core.Interfaces.Dashboard;
using MTM_Waitlist_Server.Core.Interfaces.FirstRun;
using MTM_Waitlist_Server.Core.Interfaces.KillSwitch;
using MTM_Waitlist_Server.Core.Interfaces.Migration;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using MTM_Waitlist_Server.Module.Backup.ViewModels;
using MTM_Waitlist_Server.Module.Backup.Views;
using MTM_Waitlist_Server.Module.Dashboard.ViewModels;
using MTM_Waitlist_Server.Module.Dashboard.Views;
using MTM_Waitlist_Server.Module.KillSwitch.ViewModels;
using MTM_Waitlist_Server.Module.KillSwitch.Views;
using MTM_Waitlist_Server.Module.Migrations.ViewModels;
using MTM_Waitlist_Server.Module.Migrations.Views;
using MTM_Waitlist_Server.Core.Interfaces.ConnectedUsers;
using MTM_Waitlist_Server.Module.ConnectedUsers.ViewModels;
using MTM_Waitlist_Server.Module.ConnectedUsers.Views;
using MTM_Waitlist_Server.Module.Settings.ViewModels;
using MTM_Waitlist_Server.Module.Settings.Views;
namespace MTM_Waitlist_Server.Admin;

/// <summary>
/// Application entry point. Starts the in-process Kestrel API on a background thread,
/// enforces the Windows Authentication gate, then opens the admin window.
/// </summary>
public partial class App : Application
{
    private MainWindow? _window;

    /// <summary>Shared DI provider — accessible by module ViewModels and API controllers.</summary>
    internal static IServiceProvider? Services { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        StartupLogger.Section("DI Container");
        StartupLogger.Info("Building shared DI service collection.");

        // --- Build the single shared DI container ---
        var sharedServices = new ServiceCollection();
        RegisterSharedServices(sharedServices);

        Service_ApiHost? apiHost = null;
        sharedServices.AddSingleton<IService_ApiHost>(sp =>
        {
            apiHost ??= new Service_ApiHost(
                sp.GetRequiredService<IService_SettingsStore>(),
                sharedServices,
                sp);
            return apiHost;
        });

        var provider = sharedServices.BuildServiceProvider();
        Services = provider;

        StartupLogger.Info("DI container built successfully.");

        // ── Probe ────────────────────────────────────────────────────────────
        // Run before any decision. The probe uses whatever credentials are in the
        // settings store; if the file does not exist the store returns defaults
        // (empty password) and the probe will return MySqlUnreachable.
        StartupLogger.Section("Settings Load");
        var firstRunService = provider.GetRequiredService<IService_FirstRun>();
        var settingsStore   = provider.GetRequiredService<IService_SettingsStore>();
        var settings        = settingsStore.Get();

        StartupLogger.Info($"Settings loaded. FirstRunComplete={settings.FirstRunComplete}, " +
            $"Host={settings.Database.Host}, Port={settings.Database.Port}, " +
            $"Database={settings.Database.DatabaseName}, UpdaterUsername={settings.Database.UpdaterUsername}, " +
            $"UpdaterPasswordSet={!string.IsNullOrWhiteSpace(settings.Database.UpdaterPassword)}, " +
            $"ApiListenAddress={settings.Api.ListenAddress}");

        // ── "Never configured" sentinel ───────────────────────────────────────
        // DatabaseSettings has non-empty defaults for Host/DatabaseName/Username,
        // so those fields are useless as "has the user typed anything?" checks.
        // UpdaterPassword has no default — it is the only reliable sentinel for
        // "credentials have never been entered on this machine".
        bool neverConfigured = string.IsNullOrWhiteSpace(settings.Database.UpdaterPassword);
        StartupLogger.Info($"NeverConfigured sentinel (UpdaterPassword empty) = {neverConfigured}");

        // ── MySQL Probe ───────────────────────────────────────────────────────
        StartupLogger.Section("MySQL Probe");
        StartupLogger.Info($"Starting MySQL probe against {settings.Database.Host}:{settings.Database.Port} " +
            $"as user '{settings.Database.UpdaterUsername}'.");

        Model_FirstRunProbeResult probeResult;
        try
        {
            probeResult = Task.Run(() => firstRunService.ProbeAsync()).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            StartupLogger.Error("Unhandled exception during MySQL probe — treating as MySqlUnreachable.", ex);
            probeResult = Model_FirstRunProbeResult.Unreachable(ex.Message);
        }

        StartupLogger.Info($"Probe complete. Status={probeResult.Status}, " +
            $"ErrorMessage={(string.IsNullOrWhiteSpace(probeResult.ErrorMessage) ? "<none>" : probeResult.ErrorMessage)}");

        // ── Decision tree ─────────────────────────────────────────────────────
        //
        // Precedence:
        //  1. Check persisted setup state
        //  2. Check MySQL connectivity
        //  3. Check schema existence
        //  4. Check admin user existence
        //  5. If READY, run Windows auth check
        //
        // NEVER_CONFIGURED + MySqlUnreachable              → wizard (step 1: enter credentials)
        // NEVER_CONFIGURED + SchemaMissing                 → wizard (step 2: run schema)
        // NEVER_CONFIGURED + NoAdminUser                   → wizard (step 3: create admin)
        //
        // WAS_CONFIGURED   + MySqlUnreachable              → degraded (DB is down / wrong host)
        // WAS_CONFIGURED   + SchemaMissing                 → degraded (schema was dropped)
        // WAS_CONFIGURED   + NoAdminUser                   → degraded or recovery flow (admin missing after setup)
        //
        // FirstRunComplete = false + MySqlUnreachable      → wizard or blocked setup (cannot continue until DB is reachable)
        // FirstRunComplete = false + SchemaMissing         → wizard (creds saved but schema not yet built)
        // FirstRunComplete = false + NoAdminUser           → wizard (schema built but admin not yet created)
        //
        // Invalid/inconsistent states:
        // WAS_CONFIGURED + FirstRunComplete = false        → treat as recovery/inconsistent state
        // NEVER_CONFIGURED + schema exists/admin exists    → treat as adopted existing DB or inconsistent state
        //
        // READY                                            → Windows auth check → normal launch
        // READY + Windows auth failure                     → auth failure flow (degraded / login error / block launch)

        StartupLogger.Section("Startup Decision Tree");
        StartupLogger.Info($"Evaluating branch: neverConfigured={neverConfigured}, probeStatus={probeResult.Status}, firstRunComplete={settings.FirstRunComplete}");

        if (neverConfigured)
        {
            // Nothing has been set up at all — go straight to the wizard.
            StartupLogger.Info("BRANCH: NeverConfigured=true → opening First-Run Wizard at step determined by probe status.");
            _window = new MainWindow(firstRunStatus: probeResult.Status, probeResult: probeResult);
            _window.Activate();
            StartupLogger.Info($"MainWindow activated in FirstRun-Wizard mode (probe status: {probeResult.Status}).");
            return;
        }

        if (probeResult.Status == FirstRunStatus.MySqlUnreachable)
        {
            // Credentials exist but DB is unreachable — degraded so user can fix via Settings.
            var reason = string.IsNullOrWhiteSpace(probeResult.ErrorMessage)
                ? $"MySQL could not be reached at {settings.Database.Host}:{settings.Database.Port}."
                : $"MySQL could not be reached at {settings.Database.Host}:{settings.Database.Port}.\n\nDetail: {probeResult.ErrorMessage}";
            StartupLogger.Warn($"BRANCH: WasConfigured + MySqlUnreachable → Degraded mode. Reason: {reason}");
            _window = new MainWindow(degraded: true, degradedReason: reason);
            _window.Activate();
            StartupLogger.Info("MainWindow activated in Degraded mode (MySqlUnreachable).");
            return;
        }

        if (probeResult.Status == FirstRunStatus.SchemaMissing && !settings.FirstRunComplete)
        {
            // Credentials saved, but the schema was never built — re-enter the wizard at step 2.
            StartupLogger.Info("BRANCH: SchemaMissing + FirstRunComplete=false → reopening First-Run Wizard at schema step.");
            _window = new MainWindow(firstRunStatus: probeResult.Status, probeResult: probeResult);
            _window.Activate();
            StartupLogger.Info("MainWindow activated in FirstRun-Wizard mode (SchemaMissing, not yet complete).");
            return;
        }

        if (probeResult.Status == FirstRunStatus.SchemaMissing && settings.FirstRunComplete)
        {
            // Setup ran before but the schema is now gone — degraded.
            var reason = $"The database schema for '{settings.Database.DatabaseName}' was not found." +
                " It may have been dropped or the database name changed in Settings.";
            StartupLogger.Warn($"BRANCH: SchemaMissing + FirstRunComplete=true → Degraded mode (schema was dropped post-setup). Reason: {reason}");
            _window = new MainWindow(degraded: true, degradedReason: reason);
            _window.Activate();
            StartupLogger.Info("MainWindow activated in Degraded mode (SchemaMissing post-setup).");
            return;
        }

        if (probeResult.Status == FirstRunStatus.NoAdminUser)
        {
            if (settings.FirstRunComplete)
            {
                // Setup completed before but the admin account was deleted/disabled after the
                // fact.  The wizard cannot help here — the database is configured and the schema
                // is intact.  Open in degraded mode so the user can fix things via Settings or
                // restore a backup.
                const string reason = "No active Admin or Developer user was found in the database." +
                    " The account may have been disabled or deleted after setup completed." +
                    " Restore a backup or re-create the user directly in MySQL to recover.";
                StartupLogger.Warn($"BRANCH: NoAdminUser + FirstRunComplete=true → Degraded mode (admin deleted post-setup). Reason: {reason}");
                _window = new MainWindow(degraded: true, degradedReason: reason);
            }
            else
            {
                // Still in initial setup: schema exists but the admin user was never created.
                // Resume the wizard at step 3.
                StartupLogger.Info("BRANCH: NoAdminUser + FirstRunComplete=false → reopening First-Run Wizard at admin-creation step.");
                _window = new MainWindow(firstRunStatus: probeResult.Status, probeResult: probeResult);
            }
            _window.Activate();
            StartupLogger.Info($"MainWindow activated (NoAdminUser branch, firstRunComplete={settings.FirstRunComplete}).");
            return;
        }

        // probeResult.Status == FirstRunStatus.Ready — check Windows authorisation.
        // If FirstRunComplete was never written (e.g. credentials were entered and the wizard
        // was skipped on an already-configured DB), self-heal so state is consistent.
        StartupLogger.Section("Ready Path — Windows Auth");
        StartupLogger.Info("Probe status is Ready. Proceeding to Windows authorisation check.");

        if (!settings.FirstRunComplete)
        {
            StartupLogger.Warn("FirstRunComplete=false but probe is Ready (adopted existing DB or inconsistent state). Self-healing: marking FirstRunComplete=true.");
            Task.Run(() => firstRunService.MarkCompleteAsync()).GetAwaiter().GetResult();
            StartupLogger.Info("FirstRunComplete self-heal write succeeded.");
        }

        var adminAuth     = provider.GetRequiredService<IService_AdminAuth>();
        var windowsUser   = Service_AdminAuth.GetCurrentWindowsUsername();
        StartupLogger.Info($"Current Windows user: '{windowsUser}'. Checking authorisation against database role table.");

        var isAuthorised  = Task.Run(() => adminAuth.IsAuthorisedAsync(windowsUser)).GetAwaiter().GetResult();
        StartupLogger.Info($"Windows authorisation result for '{windowsUser}': isAuthorised={isAuthorised}");

        if (!isAuthorised)
        {
            StartupLogger.Warn($"BRANCH: Access denied for Windows user '{windowsUser}' → opening access-denied screen.");
            _window = new MainWindow(accessDenied: true);
            _window.Activate();
            StartupLogger.Info("MainWindow activated in AccessDenied mode.");
            return;
        }

        // ── Normal launch ─────────────────────────────────────────────────────
        StartupLogger.Section("Normal Launch");
        StartupLogger.Info($"BRANCH: All checks passed → Normal launch. Starting Kestrel API host on '{settings.Api.ListenAddress}'.");

        apiHost = (Service_ApiHost)provider.GetRequiredService<IService_ApiHost>();
        try
        {
            apiHost.Start();
            StartupLogger.Info("Kestrel API host started successfully.");
        }
        catch (Exception ex)
        {
            StartupLogger.Error("Kestrel API host failed to start — application will continue without the API.", ex);
        }

        StartupLogger.Info("Single DI provider retained with IService_ApiHost registered.");

        var scheduler = provider.GetRequiredService<BackupSchedulerService>();
        StartupLogger.Info("Starting BackupSchedulerService.");
        _ = scheduler.StartAsync(CancellationToken.None);
        StartupLogger.Info("BackupSchedulerService started on background thread.");

        StartupLogger.Info($"Startup complete. Log file: {StartupLogger.LogFilePath}");
        _window = new MainWindow();
        _window.Activate();
        StartupLogger.Info("MainWindow activated in Normal mode.");
    }

    /// <summary>
    /// Registers all shared services used by both the WinUI modules and the ASP.NET controllers.
    /// </summary>
    private static void RegisterSharedServices(IServiceCollection services)
    {
        services.AddSingleton<IService_SettingsStore, Service_SettingsStore>();
        services.AddSingleton<IService_AdminAuth, Service_AdminAuth>();
        services.AddSingleton<IService_FirstRun, Service_FirstRun>();
        services.AddSingleton<IActivityLogBuffer, ActivityLogBuffer>();
        services.AddSingleton<IService_Dashboard, Service_Dashboard>();
        services.AddSingleton<IService_Backup, Service_Backup>();
        services.AddSingleton<IService_KillSwitch, Service_KillSwitch>();
        services.AddSingleton<IService_Migration, Service_Migration>();
        services.AddSingleton<IService_ConnectedUsers, Service_ConnectedUsers>();
        services.AddSingleton<BackupSchedulerService>();
        // ViewModels and views are Transient — new instance per navigation.
        services.AddTransient<ViewModel_ConnectedUsers>();
        services.AddTransient<View_ConnectedUsers>();
        services.AddTransient<ViewModel_Dashboard>();
        services.AddTransient<View_Dashboard>();
        services.AddTransient<ViewModel_Settings>();
        services.AddTransient<View_Settings>();
        services.AddTransient<ViewModel_FirstRun>();
        services.AddTransient<View_FirstRun>();
        services.AddTransient<ViewModel_Backup>();
        services.AddTransient<View_Backup>();
        services.AddTransient<ViewModel_KillSwitch>();
        services.AddTransient<View_KillSwitch>();
        services.AddTransient<ViewModel_Migrations>();
        services.AddTransient<View_Migrations>();
    }
}
