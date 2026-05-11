namespace MTM_Waitlist_Server.Core.Models.Settings;

/// <summary>Admin UI access control settings.</summary>
public class AdminSettings
{
    /// <summary>Windows group whose members may access the admin UI (e.g. BUILTIN\Administrators).</summary>
    public string RequiredWindowsGroup { get; set; } = @"BUILTIN\Administrators";
}
