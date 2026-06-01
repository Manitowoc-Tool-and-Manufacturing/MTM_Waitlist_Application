namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>
/// Indicates whether a canonical table definition matches the live database schema.
/// </summary>
public enum TableValidationStatus
{
    /// <summary>The table exists and its normalized schema matches the canonical SQL definition.</summary>
    Match,

    /// <summary>The table is missing from the configured database.</summary>
    Missing,

    /// <summary>The table exists but its normalized schema differs from the canonical SQL definition.</summary>
    Mismatch,

    /// <summary>The table could not be parsed or read from live metadata.</summary>
    Unreadable,
}