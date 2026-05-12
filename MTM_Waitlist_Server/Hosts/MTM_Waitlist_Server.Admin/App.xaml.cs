using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Threading;
using System.Threading.Tasks;
using MTM_Waitlist_Server.Admin.Services;
using MTM_Waitlist_Server.Admin.ViewModels;
using MTM_Waitlist_Server.Admin.Views;
using MTM_Waitlist_Server.Api;
using MTM_Waitlist_Server.Api.Services;
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
    private Task? _apiHostTask;

    /// <summary>Shared DI provider — accessible by module ViewModels and API controllers.</summary>
    internal static IServiceProvider? Services { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // --- Build the single shared DI container ---
        var sharedServices = new ServiceCollection();
        RegisterSharedServices(sharedServices);
        var provider = sharedServices.BuildServiceProvider();
        Services = provider;

        // ── Probe ────────────────────────────────────────────────────────────
        // Run before any decision. The probe uses whatever credentials are in the
        // settings store; if the file does not exist the store returns defaults
        // (empty password) and the probe will return MySqlUnreachable.
        var firstRunService = provider.GetRequiredService<IService_FirstRun>();
        var settingsStore   = provider.GetRequiredService<IService_SettingsStore>();
        var probeResult     = Task.Run(() => firstRunService.ProbeAsync()).GetAwaiter().GetResult();
        var settings        = settingsStore.Get();

        // ── "Never configured" sentinel ───────────────────────────────────────
        // DatabaseSettings has non-empty defaults for Host/DatabaseName/Username,
        // so those fields are useless as "has the user typed anything?" checks.
        // UpdaterPassword has no default — it is the only reliable sentinel for
        // "credentials have never been entered on this machine".
        bool neverConfigured = string.IsNullOrWhiteSpace(settings.Database.UpdaterPassword);

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

        if (neverConfigured)
        {
            // Nothing has been set up at all — go straight to the wizard.
            _window = new MainWindow(firstRunStatus: probeResult.Status, probeResult: probeResult);
            _window.Activate();
            return;
        }

        if (probeResult.Status == FirstRunStatus.MySqlUnreachable)
        {
            // Credentials exist but DB is unreachable — degraded so user can fix via Settings.
            var reason = string.IsNullOrWhiteSpace(probeResult.ErrorMessage)
                ? $"MySQL could not be reached at {settings.Database.Host}:{settings.Database.Port}."
                : $"MySQL could not be reached at {settings.Database.Host}:{settings.Database.Port}.\n\nDetail: {probeResult.ErrorMessage}";
            _window = new MainWindow(degraded: true, degradedReason: reason);
            _window.Activate();
            return;
        }

        if (probeResult.Status == FirstRunStatus.SchemaMissing && !settings.FirstRunComplete)
        {
            // Credentials saved, but the schema was never built — re-enter the wizard at step 2.
            _window = new MainWindow(firstRunStatus: probeResult.Status, probeResult: probeResult);
            _window.Activate();
            return;
        }

        if (probeResult.Status == FirstRunStatus.SchemaMissing && settings.FirstRunComplete)
        {
            // Setup ran before but the schema is now gone — degraded.
            var reason = $"The database schema for '{settings.Database.DatabaseName}' was not found." +
                " It may have been dropped or the database name changed in Settings.";
            _window = new MainWindow(degraded: true, degradedReason: reason);
            _window.Activate();
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
                _window = new MainWindow(degraded: true, degradedReason: reason);
            }
            else
            {
                // Still in initial setup: schema exists but the admin user was never created.
                // Resume the wizard at step 3.
                _window = new MainWindow(firstRunStatus: probeResult.Status, probeResult: probeResult);
            }
            _window.Activate();
            return;
        }

        // probeResult.Status == FirstRunStatus.Ready — check Windows authorisation.
        // If FirstRunComplete was never written (e.g. credentials were entered and the wizard
        // was skipped on an already-configured DB), self-heal so state is consistent.
        if (!settings.FirstRunComplete)
        {
            Task.Run(() => firstRunService.MarkCompleteAsync()).GetAwaiter().GetResult();
        }

        var adminAuth     = provider.GetRequiredService<IService_AdminAuth>();
        var windowsUser   = Service_AdminAuth.GetCurrentWindowsUsername();
        var isAuthorised  = Task.Run(() => adminAuth.IsAuthorisedAsync(windowsUser)).GetAwaiter().GetResult();

        if (!isAuthorised)
        {
            _window = new MainWindow(accessDenied: true);
            _window.Activate();
            return;
        }

        // ── Normal launch ─────────────────────────────────────────────────────
        var listenUrl = settingsStore.Get().Api.ListenAddress;
        var webApp    = ApiStartup.BuildApp(listenUrl, sharedServices);
        _apiHostTask  = webApp.RunAsync();

        var scheduler = provider.GetRequiredService<BackupSchedulerService>();
        _ = scheduler.StartAsync(CancellationToken.None);

        _window = new MainWindow();
        _window.Activate();
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
        services.AddSingleton<BackupSchedulerService>();
        // ViewModels and views are Transient — new instance per navigation.
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
