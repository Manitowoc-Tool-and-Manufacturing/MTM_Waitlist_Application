namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>
/// Describes how urgently a detected table mismatch needs operator attention.
/// </summary>
public enum TableMismatchSeverity
{
    /// <summary>Informational detail that does not block repair.</summary>
    Info,

    /// <summary>Non-blocking issue that should be reviewed before applying updates.</summary>
    Warning,

    /// <summary>Mismatch can be resolved by the automated safe-repair workflow.</summary>
    Repairable,

    /// <summary>Mismatch cannot be safely repaired automatically.</summary>
    Blocking,
}