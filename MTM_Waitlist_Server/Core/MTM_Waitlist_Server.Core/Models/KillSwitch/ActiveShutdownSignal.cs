namespace MTM_Waitlist_Server.Core.Models.KillSwitch;

/// <summary>An active shutdown signal targeting one or all clients.</summary>
public record ActiveShutdownSignal(
    ShutdownTarget Target,
    string? MachineName,
    string? Username,
    int WarningSeconds,
    string Message,
    DateTime IssuedAtUtc);
