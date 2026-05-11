namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>Progress event emitted while migrations are being applied.</summary>
public record MigrationProgress(
    string Version,
    string Message,
    bool IsComplete);
