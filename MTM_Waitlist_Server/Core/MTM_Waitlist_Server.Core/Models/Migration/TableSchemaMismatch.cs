namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>
/// Structured description of one deterministic difference between expected and live table schema.
/// </summary>
public record TableSchemaMismatch(
    TableMismatchKind Kind,
    TableMismatchSeverity Severity,
    string ObjectName,
    string PropertyName,
    string? ExpectedValue,
    string? ActualValue,
    string Message,
    string RecommendedAction);