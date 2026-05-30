using MTM_Waitlist_Server.Core.Models.FirstRun;

namespace MTM_Waitlist_Server.Core.Models.Splash;

/// <summary>
/// Discriminated union describing the final outcome of the startup coordinator.
/// <c>SplashWindow</c> uses this value to route into the correct <c>MainWindow</c> launch mode
/// or to exit the application.
/// </summary>
public abstract record StartupOutcome
{
    /// <summary>All checks passed — open the normal admin shell.</summary>
    public sealed record Normal() : StartupOutcome;

    /// <summary>
    /// A non-recoverable condition was detected — open <c>MainWindow</c> in degraded mode.
    /// </summary>
    /// <param name="Reason">Human-readable explanation shown to the user.</param>
    public sealed record Degraded(string Reason) : StartupOutcome;

    /// <summary>
    /// First-run setup is required — open <c>MainWindow</c> in wizard mode.
    /// </summary>
    public sealed record FirstRunWizard(
        FirstRunStatus Status,
        Model_FirstRunProbeResult ProbeResult) : StartupOutcome;

    /// <summary>
    /// The current Windows identity is not authorised — open <c>MainWindow</c> in access-denied mode.
    /// </summary>
    /// <param name="WindowsUser">The username that was checked.</param>
    public sealed record AccessDenied(string WindowsUser) : StartupOutcome;

    /// <summary>
    /// The Kestrel API host failed to bind — app continues without the API.
    /// Routes to Normal mode after the splash shows the warning.
    /// </summary>
    /// <param name="Reason">Exception message from the bind failure.</param>
    public sealed record ApiHostFailed(string Reason) : StartupOutcome;

    /// <summary>The user cancelled startup — <c>Application.Current.Exit()</c> should be called.</summary>
    public sealed record Cancelled() : StartupOutcome;
}
