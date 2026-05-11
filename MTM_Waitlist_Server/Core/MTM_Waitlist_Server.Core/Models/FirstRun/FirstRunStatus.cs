namespace MTM_Waitlist_Server.Core.Models.FirstRun;

/// <summary>
/// Result of the first-run probe. Determines which branch of the launch sequence is taken.
/// </summary>
public enum FirstRunStatus
{
    /// <summary>MySQL is reachable, schema exists, and an active Admin or Developer user exists. Normal launch.</summary>
    Ready,

    /// <summary>Cannot connect to MySQL with the configured credentials. Settings must be configured first.</summary>
    MySqlUnreachable,

    /// <summary>Connected to MySQL but the <c>mtm_waitlist</c> schema or <c>Users</c> table does not exist. Bootstrap migration required.</summary>
    SchemaMissing,

    /// <summary>Schema exists but no active user with Role <c>Admin</c> or <c>Developer</c> was found. First admin user must be created.</summary>
    NoAdminUser,
}
