using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using MTM_Waitlist_Server.Admin.Logging;
using MTM_Waitlist_Server.Admin.Services;
using MTM_Waitlist_Server.Admin.ViewModels;
using MTM_Waitlist_Server.Admin.Views;
using MTM_Waitlist_Server.Core.Interfaces.Splash;
using System;
using System.Threading;
using System.Threading.Tasks;
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
    private Window? _window;

    /// <summary>Shared DI provider — accessible by module ViewModels and API controllers.</summary>
    internal static IServiceProvider? Services { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Capture early startup crashes that happen before the UI is stable.
        StartupLogger.Error("Global UnhandledException caught in App.xaml.cs", e.Exception);
        e.Handled = true; // Prevent immediate quiet crash to allow log flush

        // If the window exists, we might be able to show something, but usually 
        // a quiet crash means we should just ensure the log is written and exit.
        if (_window is not null)
        {
            // Try to notify the user if possible, otherwise just log.
        }
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

        // Capture UI dispatcher now (must be called on the UI thread before going async).
        var dq = DispatcherQueue.GetForCurrentThread();

        // Activate the splash window — gives the user immediate visual feedback.
        var splash = provider.GetRequiredService<SplashWindow>();
        _window = splash;
        splash.ViewModel.SetDispatcherQueue(dq);
        splash.MainWindowCreated += w => _window = w;
        splash.Activate();
        StartupLogger.Info("SplashWindow activated. Handing off to startup coordinator.");

        // Fire-and-forget: coordinator runs on a background thread and raises
        // OutcomeReady on the UI thread when done; SplashWindow handles the transition.
        _ = splash.ViewModel.StartAsync();
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
        // Splash screen
        services.AddSingleton<IService_StartupCoordinator, Service_StartupCoordinator>();
        services.AddTransient<ViewModel_Splash>();
        services.AddTransient<SplashWindow>();
    }
}
