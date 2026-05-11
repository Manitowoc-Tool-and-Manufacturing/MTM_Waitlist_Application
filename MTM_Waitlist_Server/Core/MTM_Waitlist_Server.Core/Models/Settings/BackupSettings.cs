namespace MTM_Waitlist_Server.Core.Models.Settings;

/// <summary>Backup schedule and retention settings.</summary>
public class BackupSettings
{
    public string BackupFolder { get; set; } = @"C:\MTM\WaitlistBackups\";
    public string MysqlDumpPath { get; set; } = "mysqldump";
    public int RetentionDays { get; set; } = 30;
    public bool AutoBackupEnabled { get; set; } = true;
    /// <summary>24-hour HH:mm format.</summary>
    public string AutoBackupTime { get; set; } = "02:00";
}
