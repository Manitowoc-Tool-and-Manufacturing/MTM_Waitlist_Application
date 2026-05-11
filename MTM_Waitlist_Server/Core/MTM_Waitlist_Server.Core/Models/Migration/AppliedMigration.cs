namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>A migration script that has already been applied to the database.</summary>
public record AppliedMigration(
    string Version,
    string Description,
    string Script,
    DateTime AppliedAt,
    string AppliedBy,
    int ExecutionMs,
    bool Success,
    string? ErrorMessage);
