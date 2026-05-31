using MTM_Waitlist_Server.Core.Models.Migration;

namespace MTM_Waitlist_Server.Core.Interfaces.Migration;

/// <summary>Manages incremental database migrations using the SchemaVersions tracking table.</summary>
public interface IService_Migration
{
    /// <summary>
    /// Detects the current migration schema state in a single coordinated pass:
    /// <list type="bullet">
    ///   <item><see cref="MigrationSchemaState.Ready"/> — SchemaVersions exists and data loaded.</item>
    ///   <item><see cref="MigrationSchemaState.PreExistingSchema"/> — core tables exist but SchemaVersions does not; tracking table will be created and known versions backfilled automatically.</item>
    ///   <item><see cref="MigrationSchemaState.FreshDatabase"/> — no core tables found; full migration run from V001 is required.</item>
    ///   <item><see cref="MigrationSchemaState.Error"/> — database unreachable or probe failed.</item>
    /// </list>
    /// </summary>
    Task<(MigrationSchemaState State, string? ErrorMessage)> DetectSchemaStateAsync(CancellationToken ct = default);

    /// <summary>Returns true if the SchemaVersions table exists in the database.</summary>
    Task<bool> SchemaVersionsTableExistsAsync(CancellationToken ct = default);

    /// <summary>Applies all pending migration scripts in version order.</summary>
    Task<MigrationResult> ApplyPendingMigrationsAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default);

    /// <summary>Re-runs all stored procedures, triggers, and indexes (always idempotent).</summary>
    Task<RerunResult> RerunIdempotentObjectsAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default);

    /// <summary>
    /// Drops and recreates the configured application database, leaving it empty so migrations can run from a clean state.
    /// </summary>
    Task ResetDatabaseAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default);

    /// <summary>Returns the list of migrations recorded in the SchemaVersions table.</summary>
    Task<IReadOnlyList<AppliedMigration>> GetAppliedMigrationsAsync(CancellationToken ct = default);

    /// <summary>Returns migration script files on disk that have not yet been applied.</summary>
    IReadOnlyList<PendingMigration> GetPendingMigrations();

    /// <summary>Returns the SQL content of the specified migration file for preview.</summary>
    string PreviewMigrationSql(string version);
}
