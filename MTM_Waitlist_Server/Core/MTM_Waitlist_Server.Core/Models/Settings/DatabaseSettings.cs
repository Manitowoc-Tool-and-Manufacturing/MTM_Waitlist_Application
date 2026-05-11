namespace MTM_Waitlist_Server.Core.Models.Settings;

/// <summary>MySQL connection settings for the application and updater users.</summary>
public class DatabaseSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string DatabaseName { get; set; } = "mtm_waitlist";
    public string AppUsername { get; set; } = "waitlist_admin_dbappuser";
    /// <summary>DPAPI-encrypted in JSON — never store plain text.</summary>
    public string AppPassword { get; set; } = string.Empty;
    public string UpdaterUsername { get; set; } = "waitlist_admin_dbupdater";
    /// <summary>DPAPI-encrypted in JSON — never store plain text.</summary>
    public string UpdaterPassword { get; set; } = string.Empty;
    public int ConnectionTimeout { get; set; } = 10;
    public int CommandTimeout { get; set; } = 30;
}
