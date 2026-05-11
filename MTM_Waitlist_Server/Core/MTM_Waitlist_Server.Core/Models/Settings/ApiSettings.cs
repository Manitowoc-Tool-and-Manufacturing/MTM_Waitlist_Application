namespace MTM_Waitlist_Server.Core.Models.Settings;

/// <summary>Kestrel API settings.</summary>
public class ApiSettings
{
    public string ListenAddress { get; set; } = "http://0.0.0.0:5000";
    public string JwtSecret { get; set; } = string.Empty;
    public int JwtExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 30;
}
