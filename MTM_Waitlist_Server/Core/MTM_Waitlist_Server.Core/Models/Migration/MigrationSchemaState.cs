namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>
/// Describes the state of the migration tracking system detected on load.
/// </summary>
public enum MigrationSchemaState
{
    /// <summary>
    /// The SchemaVersions table exists and the applied-version list was loaded successfully.
    /// The UI should show pending migrations (if any) and allow applying them.
    /// </summary>
    Ready,

    /// <summary>
    /// The SchemaVersions table does not exist but the core application tables do —
    /// the database was bootstrapped before the migration system existed.
    /// The service will auto-create the tracking table and backfill already-applied versions.
    /// The UI should reflect this bootstrap-and-ready state.
    /// </summary>
    PreExistingSchema,

    /// <summary>
    /// Neither the SchemaVersions table nor any core application tables exist.
    /// This is a genuinely empty database that needs a full migration run from V001.
    /// </summary>
    FreshDatabase,

    /// <summary>
    /// The database could not be reached or an error occurred while probing state.
    /// </summary>
    Error,
}
