namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>A migration script file that has not yet been applied.</summary>
public record PendingMigration(
    string Version,
    int VersionNumber,
    string Description,
    string Script,
    string FilePath,
    TableValidationStatus? ValidationStatus = null,
    IReadOnlyList<TableSchemaMismatch>? Mismatches = null,
    bool CanAutoApply = false,
    string? DetailSummary = null)
{
    /// <summary>Returns the structured mismatch details associated with this pending item.</summary>
    public IReadOnlyList<TableSchemaMismatch> MismatchDetails => Mismatches ?? [];

    /// <summary>True when the item includes structured mismatch details.</summary>
    public bool HasMismatchDetails => MismatchDetails.Count > 0;

    /// <summary>Short UI label that tells the operator whether this item can be applied automatically.</summary>
    public string AutoApplySummary => CanAutoApply
        ? "Safe update available"
        : "Manual action required";
}
