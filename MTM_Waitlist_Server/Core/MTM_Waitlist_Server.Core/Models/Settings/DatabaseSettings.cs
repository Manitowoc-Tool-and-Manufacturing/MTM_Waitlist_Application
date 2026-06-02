namespace MTM_Waitlist_Server.Core.Models.Settings;

/// <summary>MySQL connection settings for the application and updater users.</summary>
public class DatabaseSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string DatabaseName { get; set; } = "mtm_waitlist";
    public string AppUsername { get; set; } = "waitlist_admin_dbappuser";
    /// <summary>
    /// DPAPI-encrypted in JSON — never store plain text.
    /// Default: username reversed character-by-character (resuppabd_nimda_tsiltiaw).
    /// </summary>
    public string AppPassword { get; set; } = "resuppabd_nimda_tsiltiaw";
    public string UpdaterUsername { get; set; } = "waitlist_admin_dbupdater";
    /// <summary>
    /// Default: username reversed character-by-character (retadpubd_nimda_tsiltiaw).
    /// </summary>
    public string UpdaterPassword { get; set; } = "retadpubd_nimda_tsiltiaw";
    public int ConnectionTimeout { get; set; } = 10;
    public int CommandTimeout { get; set; } = 30;
    /// <summary>
    /// Computes the default password for a MySQL user (username reversed).
    /// </summary>
    public static string ComputeReversedPassword(string username)
    {
        return new string(username.Reverse().ToArray());
    }
}