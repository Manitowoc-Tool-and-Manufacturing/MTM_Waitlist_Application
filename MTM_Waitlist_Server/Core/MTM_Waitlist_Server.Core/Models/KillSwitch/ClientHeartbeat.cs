namespace MTM_Waitlist_Server.Core.Models.KillSwitch;

/// <summary>Heartbeat record for a connected MAUI client.</summary>
public record ClientHeartbeat(
    string MachineName,
    string Username,
    string FullName,
    string? WorkstationName,
    DateTime LastSeenUtc);
