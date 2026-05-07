namespace MTM_Waitlist_Application.Droid
{
    /// <summary>
    /// Entry point for the Android (Droid) host application.
    /// Responsible for Android-specific platform configuration only.
    /// All shared setup (fonts, logging, DI) is delegated to
    /// <see cref="MTM_Waitlist_Application.MauiProgramExtensions.UseSharedMauiApp"/>.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Creates and configures the <see cref="MauiApp"/> for the Android platform.
        /// Add Android-exclusive configuration above <c>UseSharedMauiApp()</c> only.
        /// </summary>
        /// <returns>A fully configured <see cref="MauiApp"/> instance.</returns>
        public static MauiApp CreateMauiApp()
            => MauiApp.CreateBuilder()
                   // ── Android-specific configuration only ───────────────
                   // Add anything here that is exclusive to Android:
                   // e.g. push notification setup (FCM), camera/GPS runtime
                   //      permissions, Android-specific SQLite path config,
                   //      biometric authentication handlers
                   // ─────────────────────────────────────────────────────
                   .UseSharedMauiApp()   // ← all shared DI, fonts, logging
                   .Build();
    }
}