namespace MTM_Waitlist_Server.Core.Models.Settings;

/// <summary>Migration runner settings.</summary>
public class MigrationsSettings
{
    /// <summary>When true, pending migrations are applied automatically when the server starts.</summary>
    public bool AutoApplyOnStartup { get; set; } = false;
    public string MigrationFolder { get; set; } = @"database\migrations";
    public string ProceduresFolder { get; set; } = @"database\procedures";
    public string TriggersFolder { get; set; } = @"database\triggers";
    public string IndexesFolder { get; set; } = @"database\indexes";
}
