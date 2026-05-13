namespace Core.Models.KillSwitch;

/// <summary>
/// Scope of a kill-switch shutdown signal.
/// </summary>
public enum Enum_KillSwitch_Target
{
    /// <summary>Targets all connected clients.</summary>
    All,

    /// <summary>Targets a specific machine.</summary>
    ByMachine,

    /// <summary>Targets a specific user.</summary>
    ByUser,
}

/// <summary>
/// Represents an active shutdown signal returned by GET /api/admin/shutdown-signal.
/// The client receives this when an admin has issued a kill-switch command targeting
/// all clients or specifically this machine/user.
/// </summary>
public sealed class Model_KillSwitch_Signal
{
    /// <summary>
    /// Who the signal targets.
    /// </summary>
    public Enum_KillSwitch_Target Target { get; init; }

    /// <summary>
    /// Optional machine name filter. Only populated when <see cref="Target"/> is machine-specific.
    /// </summary>
    public string? MachineName { get; init; }

    /// <summary>
    /// Optional username filter. Only populated when <see cref="Target"/> is user-specific.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Number of seconds to display the countdown warning before the app closes.
    /// Zero means close immediately.
    /// </summary>
    public int WarningSeconds { get; init; }

    /// <summary>
    /// Admin-supplied message shown in the countdown dialog.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the signal was issued.</summary>
    public DateTime IssuedAtUtc { get; init; }
}
