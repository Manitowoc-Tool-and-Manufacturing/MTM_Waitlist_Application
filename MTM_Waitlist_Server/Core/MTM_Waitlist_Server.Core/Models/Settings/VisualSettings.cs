namespace MTM_Waitlist_Server.Core.Models.Settings;

/// <summary>Infor Visual SQL Server proxy settings (read-only; credentials never sent to clients).</summary>
public class VisualSettings
{
    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 1433;
    public string DatabaseName { get; set; } = string.Empty;
    public string Username { get; set; } = "SHOP2";
    /// <summary>DPAPI-encrypted in JSON.</summary>
    public string Password { get; set; } = string.Empty;
    public int ConnectionTimeout { get; set; } = 15;
    public int CommandTimeout { get; set; } = 60;
}
