using System.Reflection;
using Core.Constants.Api;
using Core.Interfaces.Api;
using Core.Interfaces.Auth;
using Core.Interfaces.InforVisual;
using Core.Interfaces.KillSwitch;
using Core.Interfaces.Lifecycle;
using Core.Interfaces.SetupTech;
using Core.Interfaces.Sync;
using Core.Interfaces.Waitlist;
using Data.Http;
using Data.Local;
using Data.Repositories.InforVisual;
using Data.Repositories.SetupTech;
using Data.Repositories.Waitlist;
using Feature.Auth.ViewModels.Login;
using Feature.Dashboard.ViewModels.Main;
using Feature.Waitlist.ViewModels.SetupTech;
using Feature.Waitlist.ViewModels.SetupTechConfirmation;
using Feature.Waitlist.ViewModels.SetupTechDunnage;
using Feature.Waitlist.ViewModels.SetupTechValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
using MTM_Waitlist_Application.WinUI.Services.Auth;
using MTM_Waitlist_Application.WinUI.Services.Lifecycle;
using MTM_Waitlist_Application.WinUI.Views.Dashboard;
using MTM_Waitlist_Application.WinUI.Views.Login;
using MTM_Waitlist_Application.WinUI.Views.SetupTech;
using MTM_Waitlist_Application.WinUI.Views.SetupTechConfirmation;
using MTM_Waitlist_Application.WinUI.Views.SetupTechDunnage;
using MTM_Waitlist_Application.WinUI.Views.SetupTechValidation;
using Services.InforVisual;
using Services.Auth;
using Services.KillSwitch;
using Services.SetupTech;
using Services.Sync;
using Services.Waitlist;

#if DEBUG
using Data.Mock;
#endif

namespace MTM_Waitlist_Application.WinUI;

/// <summary>
/// Standalone WinUI 3 application entry point.
/// Owns the DI composition root, configuration loading, and the login-first startup flow.
/// All Windows views live in this project; Feature projects supply ViewModels only.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Application-wide DI service provider. Resolved after <see cref="OnLaunched"/> completes setup.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    private MainWindow? _mainWindow;

    /// <summary>
    /// Initializes the singleton application object and builds the DI container.
    /// </summary>
    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched. Creates the shell window and starts the login flow.
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();

#if DEBUG
        // Seed local SQLite with mock data on a background thread when the API host is unreachable.
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                var config = Services.GetRequiredService<IConfiguration>();
                var primaryUrl = config["Api:PrimaryBaseUrl"] ?? Constants_Api.PrimaryBaseUrl;
                var isPrimaryReachable = await IsPrimaryReachableAsync(primaryUrl);

                if (!isPrimaryReachable)
                {
                    await Services.GetRequiredService<LocalDbContext>().InitializeAsync();
                    var localRepo = Services.GetRequiredService<IRepository_WaitlistEntryLocal>();
                    await MockDataSeeder.SeedIfEmptyAsync(localRepo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[STARTUP] Mock seeder failed: {ex.Message}");
            }
        });
#endif
    }

    /// <summary>
    /// Registers all application services, repositories, and ViewModels into the DI container.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // ── Configuration ───────────────────────────────────────────────
        var assembly = Assembly.GetExecutingAssembly();

        using var settingsStream = assembly.GetManifestResourceStream(
            "MTM_Waitlist_Application.WinUI.appsettings.json")!;

        var configBuilder = new ConfigurationBuilder().AddJsonStream(settingsStream);

#if DEBUG
        var devStream = assembly.GetManifestResourceStream(
            "MTM_Waitlist_Application.WinUI.appsettings.Development.json");
        if (devStream is not null)
        {
            configBuilder.AddJsonStream(devStream);
        }
#endif

        var configuration = configBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);

        // ── Logging ─────────────────────────────────────────────────────
#if DEBUG
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));
#else
        services.AddLogging();
#endif

        // ── HTTP ────────────────────────────────────────────────────────
        services.AddHttpClient();

        // ── Infrastructure ──────────────────────────────────────────────
        services.AddSingleton<IConnectivity>(Connectivity.Current);
        services.AddSingleton<IService_AuthTokenStore, Service_AuthTokenStore_WinUI>();
        services.AddSingleton<LocalDbContext>();
        services.AddSingleton<IApiClient, HttpApiClient>();

        // ── Repositories (Singleton — stateless) ────────────────────────
        services.AddSingleton<IRepository_InforVisual, Repository_InforVisual>();
        services.AddSingleton<IRepository_InforVisualLocal, Repository_InforVisualLocal>();
        services.AddSingleton<IRepository_DunnageCatalog, Repository_DunnageCatalog>();
        services.AddSingleton<IRepository_SetupTechActiveJob, Repository_SetupTechActiveJob>();
        services.AddSingleton<IRepository_SetupTechActiveJobLocal, Repository_SetupTechActiveJobLocal>();
        services.AddSingleton<IRepository_WorkOrderDunnage, Repository_WorkOrderDunnage>();
        services.AddSingleton<IRepository_WorkOrderDunnageLocal, Repository_WorkOrderDunnageLocal>();
        services.AddSingleton<IRepository_SetupTechDunnageTypeConfig, Repository_SetupTechDunnageTypeConfig>();
        services.AddSingleton<IRepository_SetupTechDunnageTypeConfigLocal, Repository_SetupTechDunnageTypeConfigLocal>();
        services.AddSingleton<IRepository_WaitlistEntry, Repository_WaitlistEntry>();
        services.AddSingleton<IRepository_WaitlistEntryLocal, Repository_WaitlistEntryLocal>();

        // ── Services (Singleton — stateless business logic) ─────────────
        services.AddSingleton<IService_Auth, Service_Auth>();
        services.AddSingleton<IService_InforVisual, Service_InforVisual>();
        services.AddSingleton<IService_SetupTech, Service_SetupTech>();
        services.AddSingleton<IService_SetupTechWorkflowState, Service_SetupTechWorkflowState>();
        services.AddSingleton<IService_WaitlistEntry, Service_WaitlistEntry>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<IService_KillSwitch, Service_KillSwitch>();
        services.AddSingleton<IService_AppLifecycle, Service_AppLifecycle_WinUI>();

        // ── ViewModels (Transient — new instance per navigation) ─────────
        services.AddTransient<ViewModel_Auth_Login>();
        services.AddTransient<ViewModel_Dashboard_Main>();
        services.AddTransient<ViewModel_Waitlist_SetupTech>();
        services.AddTransient<ViewModel_Waitlist_SetupTechValidation>();
        services.AddTransient<ViewModel_Waitlist_SetupTechDunnage>();
        services.AddTransient<ViewModel_Waitlist_SetupTechConfirmation>();

        // ── Pages (Transient — new instance per navigation) ──────────────
        services.AddTransient<LoginPage>();
        services.AddTransient<DashboardPage>();
        services.AddTransient<SetupTechPage>();
        services.AddTransient<View_Waitlist_SetupTech>();
        services.AddTransient<View_Waitlist_SetupTechValidation>();
        services.AddTransient<View_Waitlist_SetupTechDunnage>();
        services.AddTransient<View_Waitlist_SetupTechConfirmation>();
    }

#if DEBUG
    /// <summary>Probes the primary API base URL with a short timeout.</summary>
    private static async Task<bool> IsPrimaryReachableAsync(string baseUrl)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = await client.GetAsync($"{baseUrl.TrimEnd('/')}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
#endif
}

