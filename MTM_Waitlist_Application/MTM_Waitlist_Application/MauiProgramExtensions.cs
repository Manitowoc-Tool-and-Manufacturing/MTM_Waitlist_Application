using Microsoft.Extensions.Logging;
using MTM_Waitlist_Application.Feature.Dashboard.ViewModels.Main;
using MTM_Waitlist_Application.Feature.Dashboard.Views.Main;

namespace MTM_Waitlist_Application
{
    /// <summary>
    /// Extension methods for <see cref="MauiAppBuilder"/> that configure the shared
    /// application setup used by all host projects (WinUI and Droid).
    /// This is the single entry point for fonts, logging, and all DI registration.
    /// Neither host project should register services independently — everything
    /// flows through <see cref="UseSharedMauiApp"/>.
    /// </summary>
    public static class MauiProgramExtensions
    {
        /// <summary>
        /// Configures fonts, logging, and all shared services on the provided builder.
        /// Called by both MauiProgram.cs host files as the sole shared setup method.
        /// Platform-specific config is added in each host's MauiProgram.cs
        /// BEFORE this call.
        /// </summary>
        /// <param name="builder">The <see cref="MauiAppBuilder"/> provided by the host.</param>
        /// <returns>The configured <see cref="MauiAppBuilder"/> for chaining.</returns>
        public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
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

            builder.Services.AddSharedServices();

            return builder;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> that register all
    /// shared application services, repositories, pages, and ViewModels into the
    /// DI container. Uncomment each registration as the corresponding class is created.
    /// This class is internal — only <see cref="MauiProgramExtensions"/> calls it.
    /// </summary>
    internal static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all application dependencies in layered order:
        /// repositories → services → pages → ViewModels.
        /// Add new registrations here as features are built out.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register into.</param>
        /// <returns>The configured <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddSharedServices(this IServiceCollection services)
        {
            // ── Repositories (Data layer) ──────────────────────────────
            // Registered here as concrete repository classes are created in
            // MTM_Waitlist_Application.Data (see Implementation Plan Phase 2)
            // services.AddSingleton<IRepository_WaitlistEntry, Repository_WaitlistEntry>();
            // services.AddSingleton<IRepository_WaitlistEntryLocal, Repository_WaitlistEntryLocal>();

            // ── Services (Business logic layer) ───────────────────────
            // Registered here as service classes are created in
            // MTM_Waitlist_Application.Services (see Implementation Plan Phase 3)
            // services.AddSingleton<IService_Auth, Service_Auth>();
            // services.AddSingleton<IService_WaitlistEntry, Service_WaitlistEntry>();
            // services.AddSingleton<ISyncService, SyncService>();

            // ── Feature: Dashboard ─────────────────────────────────────
            services.AddTransient<ViewModel_Dashboard_Main>();
            services.AddTransient<View_Dashboard_Main>();

            // ── Feature: Waitlist ──────────────────────────────────────
            // Registered here as pages and ViewModels are created in
            // MTM_Waitlist_Application.Feature.Waitlist
            // services.AddTransient<ViewModel_Waitlist_Entry>();
            // services.AddTransient<View_Waitlist_Entry>();

            // ── Feature: Mobile ────────────────────────────────────────
            // Registered here as pages and ViewModels are created in
            // MTM_Waitlist_Application.Feature.Mobile
            // services.AddTransient<ViewModel_Mobile_Home>();
            // services.AddTransient<View_Mobile_Home>();

            return services;
        }
    }
}