namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>Overall result of a migration run.</summary>
public record MigrationResult(
    bool IsSuccess,
    int MigrationsApplied,
    string? FailedVersion,
    string? ErrorMessage);
