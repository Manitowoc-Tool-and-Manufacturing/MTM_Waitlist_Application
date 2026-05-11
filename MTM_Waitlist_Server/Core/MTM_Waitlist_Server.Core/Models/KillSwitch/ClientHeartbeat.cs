namespace MTM_Waitlist_Server.Core.Models.KillSwitch;

/// <summary>Heartbeat record for a connected MAUI client.</summary>
public record ClientHeartbeat(
    string MachineName,
    string Username,
    DateTime LastSeenUtc);
