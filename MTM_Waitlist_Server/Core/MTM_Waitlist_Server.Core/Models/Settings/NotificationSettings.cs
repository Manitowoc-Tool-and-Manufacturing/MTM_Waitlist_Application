namespace MTM_Waitlist_Server.Core.Models.Settings;

/// <summary>Kill-switch and client-notification settings.</summary>
public class NotificationSettings
{
    /// <summary>Default countdown in seconds before clients are forcibly disconnected (default 5 min).</summary>
    public int KillSwitchDefaultWarningSeconds { get; set; } = 300;
}
