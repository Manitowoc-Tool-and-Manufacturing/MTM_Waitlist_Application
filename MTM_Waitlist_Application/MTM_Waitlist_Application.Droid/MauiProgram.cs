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
        [System.Runtime.InteropServices.DllImport("log")]
        private static extern int __android_log_print(int priority, string tag, string message);

        /// <summary>
        /// Creates and configures the <see cref="MauiApp"/> for the Android platform.
        /// Add Android-exclusive configuration above <c>UseSharedMauiApp()</c> only.
        /// </summary>
        /// <returns>A fully configured <see cref="MauiApp"/> instance.</returns>
        public static MauiApp CreateMauiApp()
        {
            _ = __android_log_print(3, "MTM_Waitlist", "Initializing Android logging.");

            var builder = MauiApp.CreateBuilder();
            // ── Android-specific configuration only ─────────────────────
            // Add anything here that is exclusive to Android:
            // e.g. push notification setup (FCM), camera/GPS runtime
            //      permissions, Android-specific SQLite path config,
            //      biometric authentication handlers
            // ─────────────────────────────────────────────────
            return builder.UseSharedMauiApp();  // ← builds the app, loads config, wires DI
        }
    }
}