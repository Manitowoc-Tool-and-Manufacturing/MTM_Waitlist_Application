using MTM_Waitlist_Server.Core.Models.Migration;

namespace MTM_Waitlist_Server.Core.Interfaces.Migration;

/// <summary>
/// Compares the checked-in SQL object files to the live database and applies the
/// updates that can be executed safely without destructive rebuilds.
/// </summary>
public interface IService_Migration
{
    /// <summary>
    /// Detects the current database-update state in a single coordinated pass:
    /// <list type="bullet">
    ///   <item><see cref="MigrationSchemaState.Ready"/> — the baseline schema exists and the update-history table is available.</item>
    ///   <item><see cref="MigrationSchemaState.PreExistingSchema"/> — core tables exist but the update-history table does not; the tracking table will be created automatically.</item>
    ///   <item><see cref="MigrationSchemaState.FreshDatabase"/> — no core tables were found, so the stored baseline definitions still need to be applied.</item>
    ///   <item><see cref="MigrationSchemaState.Error"/> — database unreachable or probe failed.</item>
    /// </list>
    /// </summary>
    Task<(MigrationSchemaState State, string? ErrorMessage)> DetectSchemaStateAsync(CancellationToken ct = default);

    /// <summary>Returns true if the SchemaVersions tracking table exists in the database.</summary>
    Task<bool> SchemaVersionsTableExistsAsync(CancellationToken ct = default);

    /// <summary>Applies the pending stored SQL definitions that are safe to update automatically.</summary>
    Task<MigrationResult> ApplyPendingMigrationsAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default);

    /// <summary>Applies pending replaceable objects such as procedures, triggers, and missing indexes.</summary>
    Task<RerunResult> RerunIdempotentObjectsAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default);

    /// <summary>
    /// Drops and recreates the configured application database, leaving it empty so migrations can run from a clean state.
    /// </summary>
    Task ResetDatabaseAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default);

    /// <summary>Returns the list of update attempts recorded in the SchemaVersions table.</summary>
    Task<IReadOnlyList<AppliedMigration>> GetAppliedMigrationsAsync(CancellationToken ct = default);

    /// <summary>Returns the stored SQL definitions on disk that are missing or drifted in the live database.</summary>
    Task<IReadOnlyList<PendingMigration>> GetPendingMigrationsAsync(CancellationToken ct = default);

    /// <summary>Returns the SQL content of the specified stored definition for preview.</summary>
    string PreviewMigrationSql(string version);
}
