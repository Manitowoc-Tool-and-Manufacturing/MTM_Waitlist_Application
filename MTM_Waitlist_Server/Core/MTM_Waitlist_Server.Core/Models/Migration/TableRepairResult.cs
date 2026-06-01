namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>
/// Captures the outcome of a safe table backup, rebuild, and restore attempt.
/// </summary>
public record TableRepairResult(
    bool IsSuccess,
    string TableName,
    string? BackupTableName,
    int RowsBackedUp,
    int RowsRestored,
    IReadOnlyList<string> StepsCompleted,
    string? ErrorMessage,
    bool BackupPreserved);