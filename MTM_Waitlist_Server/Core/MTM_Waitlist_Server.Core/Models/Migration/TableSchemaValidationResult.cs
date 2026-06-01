namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>
/// Result of deterministic validation for one canonical table definition file.
/// </summary>
public record TableSchemaValidationResult(
    string TableName,
    string SourcePath,
    TableValidationStatus Status,
    IReadOnlyList<TableSchemaMismatch> Mismatches,
    bool CanRepair,
    string? Summary);