namespace MTM_Waitlist_Application.WinUI
{
    /// <summary>
    /// Entry point for the Windows (WinUI) host application.
    /// Responsible for Windows-specific platform configuration only.
    /// All shared setup (fonts, logging, DI) is delegated to
    /// <see cref="MTM_Waitlist_Application.MauiProgramExtensions.UseSharedMauiApp"/>.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Creates and configures the <see cref="MauiApp"/> for the Windows platform.
        /// Add Windows-exclusive configuration above <c>UseSharedMauiApp()</c> only.
        /// </summary>
        /// <returns>A fully configured <see cref="MauiApp"/> instance.</returns>
        public static MauiApp CreateMauiApp()
            => MauiApp.CreateBuilder()
                   // ── Windows-specific configuration only ───────────────
                   // Add anything here that is exclusive to Windows:
                   // e.g. Windows notification services, WinUI theme config,
                   //      desktop file system permissions, tray icon setup
                   // ─────────────────────────────────────────────────────
                   .UseSharedMauiApp()   // ← all shared DI, fonts, logging
                   .Build();
    }
}