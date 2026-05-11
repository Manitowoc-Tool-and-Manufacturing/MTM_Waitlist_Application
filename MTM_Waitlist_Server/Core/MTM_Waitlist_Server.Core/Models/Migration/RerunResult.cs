namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>Result of a re-run of idempotent objects (procedures, triggers, indexes).</summary>
public record RerunResult(
    int ProceduresApplied,
    int TriggersApplied,
    int IndexesApplied,
    IReadOnlyList<string> ErrorMessages);
