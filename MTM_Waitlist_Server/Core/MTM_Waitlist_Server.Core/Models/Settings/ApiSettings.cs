namespace MTM_Waitlist_Server.Core.Models.Settings;

/// <summary>Kestrel API settings.</summary>
public class ApiSettings
{
#if DEBUG
    public string ListenAddress { get; set; } = "http://localhost:5000";
#else
    public string ListenAddress { get; set; } = "http://0.0.0.0:5000";
#endif
    #if DEBUG
    public string JwtSecret { get; set; } = "MTM-Waitlist-Development-Secret-Key-32-chars";
#else
    public string JwtSecret { get; set; } = string.Empty;
#endif
    public int JwtExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 30;
}
