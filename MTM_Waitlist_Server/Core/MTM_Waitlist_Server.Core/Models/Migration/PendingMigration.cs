namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>A migration script file that has not yet been applied.</summary>
public record PendingMigration(
    string Version,
    int VersionNumber,
    string Description,
    string Script,
    string FilePath);
