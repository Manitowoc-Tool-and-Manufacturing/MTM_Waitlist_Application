using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Security.Principal;
using System.Threading.Tasks;
using MTM_Waitlist_Server.Admin.Services;
using MTM_Waitlist_Server.Admin.ViewModels;
using MTM_Waitlist_Server.Admin.Views;
using MTM_Waitlist_Server.Api;
using MTM_Waitlist_Server.Api.Services;
using MTM_Waitlist_Server.Core.Interfaces.Auth;
using MTM_Waitlist_Server.Core.Interfaces.Dashboard;
using MTM_Waitlist_Server.Core.Interfaces.FirstRun;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using MTM_Waitlist_Server.Module.Dashboard.ViewModels;
using MTM_Waitlist_Server.Module.Dashboard.Views;
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

        // --- First-run probe ---
        // Determines if MySQL is reachable and the schema + admin user exist.
        var firstRunService = provider.GetRequiredService<IService_FirstRun>();
        var probeResult = Task.Run(() => firstRunService.ProbeAsync()).GetAwaiter().GetResult();
        var settings = provider.GetRequiredService<IService_SettingsStore>().Get();

        bool showWizard = probeResult.Status is FirstRunStatus.SchemaMissing
                                                           or FirstRunStatus.NoAdminUser
            || (probeResult.Status != FirstRunStatus.Ready && !settings.FirstRunComplete);

        if (showWizard)
        {
            // Only fall back to Windows group when MySQL itself is unreachable.
            // If the DB is reachable but the schema/users are missing, go straight
            // to the wizard — no group check required or possible.
            if (probeResult.Status == FirstRunStatus.MySqlUnreachable)
            {
                var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                if (!principal.IsInRole(settings.Admin.RequiredWindowsGroup))
                {
                    _window = new MainWindow(accessDenied: true);
                    _window.Activate();
                    return;
                }
            }

            // Open the shell locked to the first-run wizard.
            _window = new MainWindow(firstRunStatus: probeResult.Status, probeResult: probeResult);
            _window.Activate();
            return;
        }

        // --- Degraded mode — first run done but MySQL is currently unreachable ---
        if (probeResult.Status == FirstRunStatus.MySqlUnreachable && settings.FirstRunComplete)
        {
            _window = new MainWindow(degraded: true);
            _window.Activate();
            return;
        }

        // --- Normal launch — MySQL role check ---
        var adminAuth = provider.GetRequiredService<IService_AdminAuth>();
        var windowsUsername = Service_AdminAuth.GetCurrentWindowsUsername();
        var isAuthorised = Task.Run(() => adminAuth.IsAuthorisedAsync(windowsUsername)).GetAwaiter().GetResult();

        if (!isAuthorised)
        {
            _window = new MainWindow(accessDenied: true);
            _window.Activate();
            return;
        }

        // --- Start Kestrel on a background thread ---
        var settingsStore = provider.GetRequiredService<IService_SettingsStore>();
        var listenUrl = settingsStore.Get().Api.ListenAddress;
        var webApp = ApiStartup.BuildApp(listenUrl, sharedServices);
        _apiHostTask = webApp.RunAsync();

        // --- Open admin window ---
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

        // ViewModels and views are Transient — new instance per navigation.
        services.AddTransient<ViewModel_Dashboard>();
        services.AddTransient<View_Dashboard>();
        services.AddTransient<ViewModel_Settings>();
        services.AddTransient<View_Settings>();
        services.AddTransient<ViewModel_FirstRun>();
        services.AddTransient<View_FirstRun>();
    }
}
