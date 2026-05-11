namespace MTM_Waitlist_Server.Core.Models.Settings;

/// <summary>Root settings model — maps to server-settings.json.</summary>
public class ServerSettings
{
    /// <summary>
    /// Set to <c>true</c> after the first-run wizard completes successfully.
    /// When <c>false</c> and the DB probe fails, the first-run wizard is shown instead of the
    /// normal admin shell.
    /// </summary>
    public bool FirstRunComplete { get; set; } = false;

    public DatabaseSettings Database { get; set; } = new();
    public VisualSettings Visual { get; set; } = new();
    public ApiSettings Api { get; set; } = new();
    public BackupSettings Backup { get; set; } = new();
    public MigrationsSettings Migrations { get; set; } = new();
    public AdminSettings Admin { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
}
