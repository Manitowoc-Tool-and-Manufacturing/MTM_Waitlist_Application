namespace MTM_Waitlist_Server.Core.Models.Dashboard;

/// <summary>A single row from SHOW FULL PROCESSLIST.</summary>
public record Model_ActiveConnection(
    long ThreadId,
    string User,
    string Host,
    string Command,
    int TimeSeconds,
    string? State);
