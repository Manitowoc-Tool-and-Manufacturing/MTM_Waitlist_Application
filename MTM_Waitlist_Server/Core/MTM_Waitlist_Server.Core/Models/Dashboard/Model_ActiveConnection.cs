namespace MTM_Waitlist_Server.Core.Models.Dashboard;

/// <summary>A single row from SHOW FULL PROCESSLIST.</summary>
public record Model_ActiveConnection(
    long ThreadId,
    string User,
    string Host,
    string Command,
    int TimeSeconds,
    string? State,
    bool IsCritical = false)
{
    /// <summary>
    /// Users whose connections are considered critical and must not be killed.
    /// These are the admin app's own service accounts — killing them crashes the app.
    /// </summary>
    public static readonly IReadOnlySet<string> CriticalUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "waitlist_admin_dbupdater",
        "waitlist_admin_dbappuser"
    };

    /// <summary>
    /// Returns true if this connection belongs to a critical internal service account
    /// or originates from localhost (the server's own process connections).
    /// </summary>
    public static bool DetectCritical(string user, string host) =>
        CriticalUsers.Contains(user) ||
        host is "localhost" or "127.0.0.1" or "::1";

    /// <summary>Inverse of <see cref="IsCritical"/>; drives the Kill button's IsEnabled binding.</summary>
    public bool CanKill => !IsCritical;

    /// <summary>Tooltip shown on the Kill button so users understand why it may be disabled.</summary>
    public string KillTooltip => IsCritical
        ? $"Cannot kill '{User}' — this is a critical internal connection. Terminating it would crash the application."
        : $"Kill thread {ThreadId} ({User})";
}
