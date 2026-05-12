using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Core.Interfaces.Api;
using Core.Interfaces.Auth;
using Core.Interfaces.Sync;
using Core.Interfaces.Waitlist;
using Data.Http;
using Data.Local;
using Data.Repositories.Waitlist;
using Feature.Auth.ViewModels.Login;
using Feature.Auth.Views.Login;
using Feature.Dashboard.ViewModels.Main;
using Feature.Dashboard.Views.Main;
using Services.Auth;
using Services.Sync;
using Services.Waitlist;
#if DEBUG
using Data.Mock;
#endif

namespace MTM_Waitlist_Application
{
    /// <summary>
    /// Extension methods for <see cref="MauiAppBuilder"/> that configure the shared
    /// application setup used by all host projects (WinUI and Droid).
    /// This is the single entry point for fonts, logging, configuration, and all DI registration.
    /// Neither host project should register services independently — everything
    /// flows through <see cref="UseSharedMauiApp"/>.
    /// </summary>
    public static class MauiProgramExtensions
    {
        /// <summary>
        /// Configures fonts, logging, configuration, and all shared services on the
        /// provided builder, then builds and returns the <see cref="MauiApp"/>.
        /// Called by both MauiProgram.cs host files as the sole shared setup method.
        /// Platform-specific config is added in each host's MauiProgram.cs
        /// BEFORE this call.
        /// </summary>
        /// <param name="builder">The <see cref="MauiAppBuilder"/> provided by the host.</param>
        /// <returns>The fully built <see cref="MauiApp"/>.</returns>
        public static MauiApp UseSharedMauiApp(this MauiAppBuilder builder)
        {
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Load appsettings.json from embedded resource.
            // The using block disposes the stream immediately after AddJsonStream reads it
            // to prevent a memory leak on Android.
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MTM_Waitlist_Application.appsettings.json")!;
            builder.Configuration.AddJsonStream(stream);

#if DEBUG
            using var devStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MTM_Waitlist_Application.appsettings.Development.json");
            if (devStream is not null)
            {
                builder.Configuration.AddJsonStream(devStream);
            }
#endif

            // Register IHttpClientFactory — required by HttpApiClient.
            builder.Services.AddHttpClient();

            builder.Services.AddSharedServices();

            var app = builder.Build();

            // Eagerly resolve SyncService so its IConnectivity.ConnectivityChanged
            // subscription is active before any screen loads.
            // The service subscribes in its constructor; without this call the
            // singleton would only be created on first ViewModel access and could
            // miss the initial reconnect event.
            _ = Task.Run(() => app.Services.GetRequiredService<ISyncService>());

#if DEBUG
            // Seed local SQLite with mock data when the primary API host (172.16.1.104)
            // is unreachable and the local store is empty.
            // Runs on a background thread (fire-and-forget) so it never blocks
            // the main thread during startup. Only runs in Debug builds.
            var primaryUrl = app.Services.GetRequiredService<IConfiguration>()["Api:PrimaryBaseUrl"]
                             ?? Core.Constants.Api.Constants_Api.PrimaryBaseUrl;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    await app.Services.GetRequiredService<LocalDbContext>().InitializeAsync();

                    var isPrimaryReachable = await IsPrimaryReachableAsync(primaryUrl);
                    if (!isPrimaryReachable)
                    {
                        var localRepo = app.Services.GetRequiredService<IRepository_WaitlistEntryLocal>();
                        await MockDataSeeder.SeedIfEmptyAsync(localRepo);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Debug mock data seeding failed: {ex.Message}");
                }
            });
#endif

            return app;
        }

#if DEBUG
        /// <summary>
        /// Performs a quick HEAD probe against the primary API host.
        /// Returns <see langword="true"/> if any HTTP response is received
        /// (even 404/405 means the host is up), <see langword="false"/> on
        /// connection failure or timeout.
        /// </summary>
        private static async Task<bool> IsPrimaryReachableAsync(string primaryUrl)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(3)
                };
                using var request = new System.Net.Http.HttpRequestMessage(
                    System.Net.Http.HttpMethod.Head, $"{primaryUrl}/health");
                await client.SendAsync(
                    request, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                return true;
            }
            catch
            {
                return false;
            }
        }
#endif
    }

    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> that register all
    /// shared application services, repositories, pages, and ViewModels into the
    /// DI container.
    /// This class is internal — only <see cref="MauiProgramExtensions"/> calls it.
    /// </summary>
    internal static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all application dependencies in layered order:
        /// infrastructure → repositories → services → pages → ViewModels.
        /// Add new registrations here as features are built out.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register into.</param>
        /// <returns>The configured <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddSharedServices(this IServiceCollection services)
        {
            // ── Infrastructure ─────────────────────────────────────────
            services.AddSingleton<IConnectivity>(Connectivity.Current);

            // ── Data layer ─────────────────────────────────────────────
            services.AddSingleton<LocalDbContext>();
            services.AddSingleton<IApiClient, HttpApiClient>();
            services.AddSingleton<IRepository_WaitlistEntry, Repository_WaitlistEntry>();
            services.AddSingleton<IRepository_WaitlistEntryLocal, Repository_WaitlistEntryLocal>();

            // ── Services (Business logic layer) ───────────────────────
            services.AddSingleton<IService_Auth, Service_Auth>();
            services.AddSingleton<IService_WaitlistEntry, Service_WaitlistEntry>();
            services.AddSingleton<ISyncService, SyncService>();

            // ── Feature: Auth ──────────────────────────────────────────
            services.AddTransient<ViewModel_Auth_Login>();
            services.AddTransient<View_Auth_Login>();

            // ── Feature: Dashboard ─────────────────────────────────────
            services.AddTransient<ViewModel_Dashboard_Main>();
            services.AddTransient<View_Dashboard_Main>();

            // ── Feature: Waitlist ──────────────────────────────────────
            // Registered here as pages and ViewModels are created in
            // Feature.Waitlist
            // services.AddTransient<ViewModel_Waitlist_Entry>();
            // services.AddTransient<View_Waitlist_Entry>();

            // ── Feature: Mobile ────────────────────────────────────────
            // Registered here as pages and ViewModels are created in
            // Feature.Mobile
            // services.AddTransient<ViewModel_Mobile_Home>();
            // services.AddTransient<View_Mobile_Home>();

            return services;
        }
    }
}