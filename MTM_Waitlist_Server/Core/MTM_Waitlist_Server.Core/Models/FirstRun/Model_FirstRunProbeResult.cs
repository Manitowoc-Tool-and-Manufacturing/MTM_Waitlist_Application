namespace MTM_Waitlist_Server.Core.Models.FirstRun;

/// <summary>
/// The result of a first-run probe, including the status and an optional human-readable
/// error message when the status is not <see cref="FirstRunStatus.Ready"/>.
/// </summary>
public record Model_FirstRunProbeResult(FirstRunStatus Status, string? ErrorMessage = null)
{
    /// <summary>Creates a successful ready result.</summary>
    public static Model_FirstRunProbeResult Ready() =>
        new(FirstRunStatus.Ready);

    /// <summary>Creates a result indicating MySQL is unreachable.</summary>
    public static Model_FirstRunProbeResult Unreachable(string error) =>
        new(FirstRunStatus.MySqlUnreachable, error);

    /// <summary>Creates a result indicating the schema is missing.</summary>
    public static Model_FirstRunProbeResult SchemaMissing() =>
        new(FirstRunStatus.SchemaMissing);

    /// <summary>Creates a result indicating no admin user exists.</summary>
    public static Model_FirstRunProbeResult NoAdminUser() =>
        new(FirstRunStatus.NoAdminUser);
}
