using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using MTM_Waitlist_Server.Admin.Services;
using MTM_Waitlist_Server.Api;
using MTM_Waitlist_Server.Api.Services;
using MTM_Waitlist_Server.Core.Interfaces.Dashboard;
using MTM_Waitlist_Server.Core.Interfaces.Auth;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Module.Dashboard.ViewModels;
using MTM_Waitlist_Server.Module.Dashboard.Views;
using MTM_Waitlist_Server.Module.Settings.ViewModels;
using MTM_Waitlist_Server.Module.Settings.Views;
using System.Threading.Tasks;

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

        // --- Database Role Auth Gate ---
        // Checks the current Windows username against mtm_waitlist.Users.
        // Only Role = 'Admin' or 'Developer' may open the admin app.
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
        _window = new MainWindow(accessDenied: false);
        _window.Activate();
    }

    /// <summary>
    /// Registers all shared services used by both the WinUI modules and the ASP.NET controllers.
    /// </summary>
    private static void RegisterSharedServices(IServiceCollection services)
    {
        services.AddSingleton<IService_SettingsStore, Service_SettingsStore>();
        services.AddSingleton<IService_AdminAuth, Service_AdminAuth>();
        services.AddSingleton<IActivityLogBuffer, ActivityLogBuffer>();
        services.AddSingleton<IService_Dashboard, Service_Dashboard>();

        // ViewModels and views are Transient — new instance per navigation.
        services.AddTransient<ViewModel_Dashboard>();
        services.AddTransient<View_Dashboard>();
        services.AddTransient<ViewModel_Settings>();
        services.AddTransient<View_Settings>();

        // TODO: register IService_Backup, IService_Migration, IService_KillSwitch,
        //       and remaining module ViewModels/Views as implementations are created.
    }
}
